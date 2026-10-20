using Nibiru.Tablets;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Barra de transformação, de humana a divina. Some enquanto a Elena ainda é
    /// totalmente humana (zero) e aparece depois da primeira tabuleta.
    /// </summary>
    public class TransformationHUD : MonoBehaviour
    {
        [Tooltip("Objeto com a barra inteira (some no zero).")]
        [SerializeField] GameObject root;

        [Tooltip("Retângulo do preenchimento, ancorado à esquerda.")]
        [SerializeField] RectTransform fill;

        [SerializeField] Text label;

        void Update()
        {
            int value = Transformation.Value;
            bool show = value > 0;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            fill.anchorMax = new Vector2(Transformation.Fraction, 1f);
            label.text = $"TRANSFORMAÇÃO {value}%";
        }
    }
}
