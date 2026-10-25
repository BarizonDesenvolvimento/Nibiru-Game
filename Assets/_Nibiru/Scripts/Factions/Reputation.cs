using System;
using System.Collections.Generic;
using Nibiru.Save;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Factions
{
    /// <summary>
    /// Reputação da Elena com cada facção, de -100 (hostil) a 100 (aliada). Fica no WorldState,
    /// então volta certa de um checkpoint. Toda mudança aparece num aviso, como "Guardiões +15".
    /// </summary>
    public static class Reputation
    {
        public const int Min = -100;
        public const int Max = 100;

        /// <summary>Avisa a tela quando alguma reputação muda.</summary>
        public static event Action<FactionDefinition, int> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Changed = null;

        public static int Get(FactionDefinition faction) =>
            WorldState.TryGetReputation(faction.Id, out int value) ? value : faction.startingReputation;

        /// <summary>True se a reputação com esta facção já mudou alguma vez.</summary>
        public static bool IsKnown(FactionDefinition faction) => WorldState.TryGetReputation(faction.Id, out _);

        public static void Apply(ReputationChange change)
        {
            if (change.faction == null || change.amount == 0) return;

            int before = Get(change.faction);
            int after = Mathf.Clamp(before + change.amount, Min, Max);
            WorldState.SetReputation(change.faction.Id, after);

            string color = ColorUtility.ToHtmlStringRGB(change.faction.color);
            string sign = change.amount > 0 ? "+" : string.Empty;
            string effect = change.amount > 0 ? "passa a confiar mais em você" : "passa a confiar menos em você";
            Notifications.PostImportant($"<color=#{color}>{change.faction.displayName}</color> {effect}  ({sign}{change.amount})");
            Changed?.Invoke(change.faction, after);
        }

        public static void Apply(IEnumerable<ReputationChange> changes)
        {
            if (changes == null) return;
            foreach (ReputationChange change in changes) Apply(change);
        }

        /// <summary>Nome da faixa de reputação: Hostil, Desconfiada, Neutra, Simpática ou Aliada.</summary>
        public static string LevelName(int value)
        {
            if (value <= -50) return "Hostil";
            if (value <= -10) return "Desconfiada";
            if (value < 10) return "Neutra";
            if (value < 50) return "Simpática";
            return "Aliada";
        }
    }
}
