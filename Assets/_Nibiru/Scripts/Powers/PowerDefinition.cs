using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// Um poder Anunnaki, como arquivo em Data/Powers. Cada poder tem seu próprio tipo de
    /// arquivo com os valores dele (o Eco do Abzu usa EchoPowerDefinition).
    /// </summary>
    public abstract class PowerDefinition : ScriptableObject
    {
        [Tooltip("Nome mostrado na tela.")]
        public string displayName;

        [Tooltip("Texto curto explicando o que o poder faz.")]
        [TextArea(2, 4)] public string description;

        [Tooltip("Tempo de recarga entre usos, em segundos.")]
        [Min(0f)] public float cooldown = 12f;

        public string Id => name;

        /// <summary>True se a Elena já tem este poder nesta partida.</summary>
        public bool IsUnlocked => WorldState.HasStoryEvent("Poder:" + Id);

        public void Unlock() => WorldState.MarkStoryEvent("Poder:" + Id);
    }
}
