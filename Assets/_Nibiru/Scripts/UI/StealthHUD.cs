using Nibiru.Player;
using Nibiru.Stealth;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra se a Elena está agachada e um medidor do barulho que ela está fazendo,
    /// que sobe a cada passo ou tiro e desce aos poucos.
    /// </summary>
    public class StealthHUD : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        [Tooltip("Texto que aparece quando ela está agachada.")]
        [SerializeField] Text stanceLabel;

        [Tooltip("Retângulo do preenchimento do medidor de ruído, ancorado à esquerda.")]
        [SerializeField] RectTransform noiseFill;

        [Tooltip("Ruído que enche o medidor, em metros de alcance.")]
        [SerializeField, Min(1f)] float fullNoiseRadius = 15f;

        [Tooltip("Quanto do medidor esvazia por segundo (1 = tudo em um segundo).")]
        [SerializeField, Min(0.1f)] float decayPerSecond = 1.2f;

        float level;

        void OnEnable() => Noise.Emitted += OnNoise;

        void OnDisable() => Noise.Emitted -= OnNoise;

        void OnNoise(NoiseEvent noise)
        {
            if (noise.source != motor.gameObject) return;
            level = Mathf.Max(level, Mathf.Clamp01(noise.radius / fullNoiseRadius));
        }

        void Update()
        {
            stanceLabel.enabled = motor.IsCrouching;
            level = Mathf.MoveTowards(level, 0f, decayPerSecond * Time.deltaTime);
            noiseFill.anchorMax = new Vector2(level, 1f);
        }
    }
}
