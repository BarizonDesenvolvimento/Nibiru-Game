using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Onde cada linha de um <see cref="Text"/> aparece na tela, lida do gerador de texto da própria Unity. Os menus são um
    /// <c>Text</c> só com uma linha por item (com tamanhos de letra diferentes); o mouse precisa saber em que faixa de tela
    /// cada linha foi desenhada.
    /// </summary>
    public static class TextLines
    {
        /// <summary>Quantas linhas o texto tem agora (já considerando quebras automáticas).</summary>
        public static int Count(Text text)
        {
            Populate(text);
            return text.cachedTextGenerator.lineCount;
        }

        /// <summary>
        /// A faixa da linha na tela, em pixels (origem embaixo à esquerda, como o mouse): a largura toda do retângulo do texto
        /// e a altura da linha. False se a linha não existe.
        /// </summary>
        public static bool TryGetBand(Text text, int line, out Rect screenRect)
        {
            screenRect = default;
            Populate(text);
            TextGenerator generator = text.cachedTextGenerator;
            if (line < 0 || line >= generator.lineCount) return false;

            UILineInfo info = generator.lines[line];
            float unitsPerPixel = 1f / Mathf.Max(0.0001f, text.pixelsPerUnit);
            float top = info.topY * unitsPerPixel;
            float bottom = (info.topY - info.height) * unitsPerPixel;

            // topY é medido a partir do pivô do retângulo, para cima.
            RectTransform rect = text.rectTransform;
            Rect local = rect.rect;
            Camera camera = CameraOf(text);
            Vector2 a = Project(rect, camera, new Vector2(local.xMin, top));
            Vector2 b = Project(rect, camera, new Vector2(local.xMax, bottom));
            screenRect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        /// <summary>O número da linha sob o ponteiro (posição de tela do mouse), ou -1.</summary>
        public static int LineAt(Text text, Vector2 screenPoint)
        {
            int count = Count(text);
            for (int i = 0; i < count; i++)
                if (TryGetBand(text, i, out Rect band) && band.Contains(screenPoint)) return i;
            return -1;
        }

        static Vector2 Project(RectTransform rect, Camera camera, Vector2 local)
        {
            // As coordenadas locais do RectTransform têm origem no pivô, então o ponto já está no sistema certo.
            Vector3 world = rect.TransformPoint(new Vector3(local.x, local.y, 0f));
            return RectTransformUtility.WorldToScreenPoint(camera, world);
        }

        static Camera CameraOf(Text text)
        {
            Canvas canvas = text.canvas != null ? text.canvas.rootCanvas : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera;
        }

        // Garante que as linhas são as do texto de agora (o desenho só atualiza no fim do quadro).
        static void Populate(Text text)
        {
            Vector2 size = text.rectTransform.rect.size;
            text.cachedTextGenerator.Populate(text.text, text.GetGenerationSettings(size));
        }
    }
}
