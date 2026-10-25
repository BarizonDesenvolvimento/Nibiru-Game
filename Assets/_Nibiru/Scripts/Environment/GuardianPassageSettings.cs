using UnityEngine;

namespace Nibiru.Environment
{
    /// <summary>
    /// Valores da passagem dos Guardiões (passo 13): a parede do barco de junco, a luz da tabuleta
    /// e as lamparinas. Fica num arquivo para ajustar durante o Play sem perder as mudanças.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Passagem dos Guardiões/Configuração", fileName = "PassagemDosGuardioes")]
    public class GuardianPassageSettings : ScriptableObject
    {
        [Header("Parede do barco de junco")]
        [Tooltip("Quanto tempo a parede leva para afundar no chão, em segundos.")]
        [Min(0.2f)] public float wallOpenSeconds = 4f;

        [Tooltip("Quanto a parede desce, em metros (mais que a altura dela, para sumir no chão).")]
        [Min(0.5f)] public float wallSinkDepth = 4.4f;

        [Header("A tabuleta pulsando")]
        [Tooltip("Cor da luz que acende na inscrição quando a tabuleta pulsa.")]
        public Color tabletPulseColor = new Color(0.35f, 0.55f, 1f);

        [Tooltip("Intensidade da luz no pico de cada pulso.")]
        [Min(0f)] public float tabletPulseIntensity = 5f;

        [Tooltip("Duração de cada pulso (sobe e desce), em segundos.")]
        [Min(0.2f)] public float tabletPulseSeconds = 1.2f;

        [Tooltip("Quantos pulsos antes de a parede abrir.")]
        [Range(1, 5)] public int tabletPulses = 2;

        [Header("Lamparinas dos Guardiões (ficam acesas: alguém cuida da passagem)")]
        [Tooltip("Cor da chama. Sempre quente: o azul é de Kur.")]
        public Color lampColor = new Color(1f, 0.72f, 0.4f);

        [Tooltip("Intensidade da luz de cada lamparina.")]
        [Min(0f)] public float lampIntensity = 1.6f;

        [Tooltip("Alcance da luz de cada lamparina, em metros.")]
        [Min(0.5f)] public float lampRange = 6f;

        [Tooltip("Quanto a chama treme, de 0 (firme) a 1.")]
        [Range(0f, 1f)] public float lampFlicker = 0.12f;

        [Header("Som")]
        [Tooltip("Volume do som ambiente do túnel (vento baixo e pingos).")]
        [Range(0f, 1f)] public float tunnelAmbienceVolume = 0.5f;
    }
}
