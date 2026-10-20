using System;
using UnityEngine;

namespace Nibiru.Stealth
{
    /// <summary>Um som no mundo: onde aconteceu, até onde é ouvido e quem fez.</summary>
    public struct NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public GameObject source;
    }

    /// <summary>
    /// Central de ruídos. Passos, tiros e golpes chamam Noise.Emit; inimigos
    /// (soldados, o Lullu) escutam o evento Emitted e decidem se ouviram.
    /// </summary>
    public static class Noise
    {
        public static event Action<NoiseEvent> Emitted;

        // Começa limpo a cada Play, mesmo com o recarregamento de domínio desligado no editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Emitted = null;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            if (radius <= 0f) return;
            Emitted?.Invoke(new NoiseEvent { position = position, radius = radius, source = source });
        }
    }
}
