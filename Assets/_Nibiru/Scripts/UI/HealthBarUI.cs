using Nibiru.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Barra de vida. O preenchimento encolhe da direita para a esquerda e muda de cor:
    /// verde (bem), amarelo (cuidado) e vermelho (perigo).
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] Health health;

        [Tooltip("Retângulo do preenchimento, ancorado à esquerda da barra.")]
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;

        [Header("Cores")]
        [SerializeField] Color fineColor = new Color(0.35f, 0.85f, 0.45f);
        [SerializeField] Color cautionColor = new Color(0.98f, 0.76f, 0.22f);
        [SerializeField] Color dangerColor = new Color(0.95f, 0.30f, 0.25f);

        [Tooltip("Abaixo desta fração da vida a barra fica amarela.")]
        [SerializeField, Range(0f, 1f)] float cautionBelow = 0.6f;

        [Tooltip("Abaixo desta fração da vida a barra fica vermelha.")]
        [SerializeField, Range(0f, 1f)] float dangerBelow = 0.3f;

        void Update()
        {
            float fraction = health.Fraction;
            fill.anchorMax = new Vector2(fraction, 1f);
            fillImage.color = fraction < dangerBelow ? dangerColor
                : fraction < cautionBelow ? cautionColor
                : fineColor;
        }
    }
}
