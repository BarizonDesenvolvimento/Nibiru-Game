using UnityEngine;

namespace Nibiru.Environment
{
    /// <summary>
    /// Valores da tempestade de areia do wadi (passo 14). Só o que mexe na jogabilidade: a névoa que
    /// esconde, a lentidão da Elena e o vento que cobre os passos. Efeitos definitivos (partículas,
    /// céu laranja, cor) ficam para a fase de arte. Mudanças com o Play ligado continuam valendo.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Tempestade/Configuração", fileName = "TempestadeDoWadi")]
    public class StormSettings : ScriptableObject
    {
        [Header("Onde ela começa e fica cheia (pelo z da Elena)")]
        [Tooltip("Do z em que a tempestade começa a subir (no topo da rampa)...")]
        public float rampStartZ = 138f;

        [Tooltip("...até o z em que ela está no máximo.")]
        public float rampFullZ = 150f;

        [Tooltip("Quão rápido a tempestade acompanha a mudança, em segundos até mudar por completo.")]
        [Min(0.1f)] public float fadeSeconds = 3f;

        [Header("O que ela faz (valores no máximo da tempestade)")]
        [Tooltip("Multiplicador da velocidade da Elena (1 = nenhuma lentidão).")]
        [Range(0.5f, 1f)] public float moveSpeedMultiplier = 0.9f;

        [Tooltip("Multiplicador do alcance em que os soldados ouvem passos e golpes da Elena (1 = nenhum). Tiros não são cobertos.")]
        [Range(0.1f, 1f)] public float hearingMultiplier = 0.5f;

        [Tooltip("Multiplicador de até onde os soldados enxergam (1 = nenhum).")]
        [Range(0.2f, 1f)] public float viewDistanceMultiplier = 0.55f;

        [Header("Névoa de areia (provisória)")]
        public Color fogColor = new Color(0.85f, 0.6f, 0.32f);

        [Tooltip("Densidade da névoa no máximo da tempestade.")]
        [Min(0f)] public float fogDensity = 0.035f;

        [Header("Vento")]
        [Range(0f, 1f)] public float windVolume = 0.7f;
    }
}
