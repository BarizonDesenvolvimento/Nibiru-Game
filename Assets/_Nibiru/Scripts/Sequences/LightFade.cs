using UnityEngine;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Acende ou apaga uma luz aos poucos, com um tremor opcional (a tabuleta acordando, a
    /// chama de uma lamparina encolhendo). Chame FadeTo por um evento do Inspector.
    /// Usa tempo sem escala: a luz continua mudando durante uma conversa, com o jogo pausado.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFade : MonoBehaviour
    {
        [Tooltip("Tempo para chegar à nova intensidade, em segundos.")]
        [SerializeField, Min(0f)] float duration = 1.5f;

        [Tooltip("Quanto a luz treme, de 0 (firme) a 1 (vai de apagada ao dobro).")]
        [SerializeField, Range(0f, 1f)] float flicker;

        [Tooltip("Velocidade do tremor.")]
        [SerializeField, Min(0f)] float flickerSpeed = 6f;

        Light source;
        float from, target, startTime = -1f, current;

        void Awake()
        {
            source = GetComponent<Light>();
            current = target = source.intensity;
        }

        /// <summary>Leva a luz até esta intensidade, no tempo configurado.</summary>
        public void FadeTo(float intensity)
        {
            from = current;
            target = intensity;
            startTime = Time.unscaledTime;
        }

        void Update()
        {
            if (startTime >= 0f)
            {
                float t = duration > 0f ? (Time.unscaledTime - startTime) / duration : 1f;
                current = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                if (t >= 1f) startTime = -1f;
            }

            float noise = flicker > 0f ? (Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0.37f) - 0.5f) * 2f * flicker : 0f;
            source.intensity = Mathf.Max(0f, current * (1f + noise));
            source.enabled = source.intensity > 0.001f;
        }
    }
}
