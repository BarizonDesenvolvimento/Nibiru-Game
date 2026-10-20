using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Um golpe corpo a corpo (faca, mais tarde outros). Cada golpe é um arquivo destes
    /// em Data/Combat, para ajustar dano, alcance e ritmo sem mexer em código.
    /// O golpe tem três fases: preparação, acerto e recuperação.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Combate/Ataque Corpo a Corpo", fileName = "NovoAtaque")]
    public class MeleeAttackDefinition : ScriptableObject
    {
        [Tooltip("Nome do golpe.")]
        public string displayName = "Faca";

        [Tooltip("Dano por acerto.")]
        [Min(0f)] public float damage = 25f;

        [Header("Ritmo (segundos)")]
        [Tooltip("Preparação: do botão até o golpe começar a acertar.")]
        [Min(0f)] public float windup = 0.12f;

        [Tooltip("Acerto: por quanto tempo o golpe pega quem estiver na frente.")]
        [Min(0.01f)] public float activeTime = 0.1f;

        [Tooltip("Recuperação: depois do golpe, antes de poder agir de novo. A esquiva pode cancelar esta parte.")]
        [Min(0f)] public float recovery = 0.25f;

        [Header("Área do golpe (metros)")]
        [Tooltip("Distância à frente do centro do personagem.")]
        public float reach = 0.9f;

        [Tooltip("Raio da área que acerta.")]
        [Min(0.05f)] public float radius = 0.6f;

        [Tooltip("Altura em relação ao centro do personagem.")]
        public float height = 0.3f;

        [Header("Som")]
        [Tooltip("Até onde o golpe é ouvido, em metros.")]
        [Min(0f)] public float noiseRadius = 2f;

        public float TotalTime => windup + activeTime + recovery;
    }
}
