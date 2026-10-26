using UnityEngine;

namespace Nibiru.Environment
{
    /// <summary>
    /// A tempestade de areia do momento, de 0 (nenhuma) a 1 (cheia). Quem mexe na jogabilidade lê os
    /// multiplicadores daqui: a Elena anda mais devagar e os soldados enxergam e ouvem menos. Sem
    /// tempestade (intensidade 0), todos valem 1 e o jogo é o de sempre.
    /// </summary>
    public static class Storm
    {
        static StormSettings settings;

        /// <summary>De 0 a 1. Quem controla é o <see cref="StormController"/>.</summary>
        public static float Intensity { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            settings = null;
            Intensity = 0f;
        }

        /// <summary>Acaba com a tempestade (ao voltar ao menu principal, para ela não passar para a partida seguinte).</summary>
        public static void Clear()
        {
            settings = null;
            Intensity = 0f;
        }

        internal static void Set(StormSettings newSettings, float intensity)
        {
            settings = newSettings;
            Intensity = Mathf.Clamp01(intensity);
        }

        /// <summary>Velocidade da Elena (1 = normal).</summary>
        public static float MoveSpeedMultiplier => Blend(settings != null ? settings.moveSpeedMultiplier : 1f);

        /// <summary>Alcance em que passos e golpes são ouvidos (1 = normal).</summary>
        public static float HearingMultiplier => Blend(settings != null ? settings.hearingMultiplier : 1f);

        /// <summary>Até onde os soldados enxergam (1 = normal).</summary>
        public static float ViewDistanceMultiplier => Blend(settings != null ? settings.viewDistanceMultiplier : 1f);

        static float Blend(float atFull) => Mathf.Lerp(1f, atFull, Intensity);
    }
}
