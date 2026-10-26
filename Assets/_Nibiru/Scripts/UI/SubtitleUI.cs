using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Legenda no pé da tela para falas que tocam enquanto a Elena anda (fitas gravadas). Não pausa o
    /// jogo nem toma os controles, ao contrário das conversas. Monta a própria tela na primeira vez.
    /// </summary>
    public class SubtitleUI : MonoBehaviour
    {
        Canvas canvas;
        Text label;

        public bool IsShowing => canvas != null && canvas.enabled;

        public string CurrentText => label != null ? label.text : string.Empty;

        public void Show(string heading, string text)
        {
            Ensure();
            label.text = string.IsNullOrEmpty(heading) ? text : $"<size=18><i>{heading}</i></size>\n{text}";
            canvas.enabled = true;
        }

        public void Hide()
        {
            if (canvas != null) canvas.enabled = false;
        }

        void Ensure()
        {
            if (canvas != null) return;

            var go = new GameObject("LegendaFita");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("Texto", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = new Vector2(0.15f, 0.05f);
            rect.anchorMax = new Vector2(0.85f, 0.22f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            label = textGo.AddComponent<Text>();
            label.font = Nibiru.UI.UiFont.Current;
            label.fontSize = 26;
            label.alignment = TextAnchor.LowerCenter;
            label.color = new Color(1f, 0.95f, 0.82f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
            canvas.enabled = false;
        }
    }
}
