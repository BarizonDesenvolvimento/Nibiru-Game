using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Clareia a tela de uma cor (um flash de memória, uma luz forte) e a devolve ao normal. Chamado
    /// por eventos do Inspector (<see cref="FadeIn"/> e <see cref="FadeOut"/>). Usa o tempo real, então
    /// continua com o jogo pausado numa conversa. A tela fica atrás das conversas e dos avisos.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {
        [SerializeField] Color color = new Color(0.82f, 0.9f, 1f);

        [Tooltip("Quanto a tela cobre quando está cheia (0 a 1). Abaixo de 1, ainda se vê o chão por baixo.")]
        [Range(0f, 1f)] [SerializeField] float maxAlpha = 0.92f;

        [Min(0.05f)] [SerializeField] float fadeInTime = 1.2f;
        [Min(0.05f)] [SerializeField] float fadeOutTime = 1.8f;

        Image image;
        Coroutine running;
        float alpha;

        public bool IsCovering => alpha > 0.5f;

        public void FadeIn() => Go(1f, fadeInTime);

        public void FadeOut() => Go(0f, fadeOutTime);

        void Go(float target, float time)
        {
            EnsureOverlay();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade(target, time));
        }

        IEnumerator Fade(float target, float time)
        {
            float from = alpha;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                Apply(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / time)));
                yield return null;
            }
            Apply(target);
            running = null;
        }

        void Apply(float value)
        {
            alpha = value;
            Color c = color;
            c.a = value * maxAlpha;
            image.color = c;
            image.enabled = value > 0f;
        }

        void EnsureOverlay()
        {
            if (image != null) return;

            var go = new GameObject("TelaFlash");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Atrás do resto da interface (as conversas e os avisos ficam por cima da luz).
            canvas.sortingOrder = -50;

            var fillGo = new GameObject("Luz", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)fillGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image = fillGo.AddComponent<Image>();
            image.raycastTarget = false;
            Apply(0f);
        }
    }
}
