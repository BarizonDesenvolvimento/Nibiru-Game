using System;
using UnityEngine;

namespace Nibiru.Factions
{
    /// <summary>Uma facção (Fundação Enlil, Guardiões, Despertos), como arquivo em Data/Factions.</summary>
    [CreateAssetMenu(menuName = "Nibiru/Facções/Facção", fileName = "NovaFaccao")]
    public class FactionDefinition : ScriptableObject
    {
        [Tooltip("Nome mostrado na tela.")]
        public string displayName;

        [Tooltip("Cor do nome nos avisos e na tela de inventário.")]
        public Color color = Color.white;

        [Tooltip("Reputação no começo do jogo (de -100 a 100).")]
        [Range(-100, 100)] public int startingReputation;

        [Tooltip("Só aparece na tela depois da primeira mudança de reputação (para facções que a Elena ainda não conhece).")]
        public bool hiddenUntilChanged;

        public string Id => name;
    }

    /// <summary>Uma mudança de reputação: qual facção e quanto (positivo sobe, negativo desce).</summary>
    [Serializable]
    public struct ReputationChange
    {
        public FactionDefinition faction;
        [Range(-100, 100)] public int amount;
    }
}
