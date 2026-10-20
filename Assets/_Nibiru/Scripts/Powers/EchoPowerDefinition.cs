using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>Valores do Eco do Abzu: o pulso que revela inimigos e marcas escondidas.</summary>
    [CreateAssetMenu(menuName = "Nibiru/Poderes/Eco do Abzu", fileName = "EcoDoAbzu")]
    public class EchoPowerDefinition : PowerDefinition
    {
        [Tooltip("Alcance do pulso, em metros.")]
        [Min(1f)] public float radius = 18f;

        [Tooltip("Quanto tempo inimigos e marcas ficam visíveis, em segundos.")]
        [Min(0.5f)] public float revealDuration = 6f;

        [Tooltip("Tempo para o anel do pulso abrir até o alcance total, em segundos.")]
        [Min(0.05f)] public float expandTime = 0.6f;

        [Tooltip("Material da silhueta vista através das paredes (shader Nibiru/RaioX).")]
        public Material revealMaterial;

        [Tooltip("Material do anel do pulso.")]
        public Material ringMaterial;
    }
}
