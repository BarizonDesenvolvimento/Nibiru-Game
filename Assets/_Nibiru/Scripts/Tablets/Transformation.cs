using System;
using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Tablets
{
    /// <summary>
    /// Transformação da Elena, de 0 (humana) a 100 (divina). Sobe ao decifrar tabuletas e
    /// com algumas escolhas. Fica no WorldState, então o checkpoint restaura.
    /// </summary>
    public static class Transformation
    {
        public const int Max = 100;

        public static event Action<int> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Changed = null;

        public static int Value => WorldState.Transformation;
        public static float Fraction => Value / (float)Max;

        public static void Add(int amount)
        {
            if (amount == 0) return;
            WorldState.Transformation = Mathf.Clamp(WorldState.Transformation + amount, 0, Max);
            Changed?.Invoke(Value);
        }
    }
}
