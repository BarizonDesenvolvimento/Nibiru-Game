using Nibiru.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>Pisca a tela em vermelho quando a Elena leva dano, e some aos poucos.</summary>
    public class DamageFlashUI : MonoBehaviour
    {
        [SerializeField] Health health;

        [Tooltip("Imagem que cobre a tela inteira.")]
        [SerializeField] Image overlay;

        [Tooltip("Opacidade máxima do flash (0 a 1).")]
        [SerializeField, Range(0f, 1f)] float maxAlpha = 0.35f;

        [Tooltip("Quanto tempo o flash leva para sumir, em segundos.")]
        [SerializeField, Min(0.05f)] float fadeTime = 0.5f;

        float alpha;

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            SetAlpha(0f);
        }

        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(DamageInfo info) => SetAlpha(maxAlpha);

        void Update()
        {
            if (alpha > 0f)
                SetAlpha(Mathf.MoveTowards(alpha, 0f, maxAlpha / fadeTime * Time.unscaledDeltaTime));
        }

        void SetAlpha(float value)
        {
            alpha = value;
            Color color = overlay.color;
            color.a = value;
            overlay.color = color;
            overlay.enabled = value > 0f;
        }
    }
}
