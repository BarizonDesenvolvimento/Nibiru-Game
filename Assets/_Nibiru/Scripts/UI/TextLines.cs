using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

        /// <summary>O número da próxima linha de um texto que está sendo montado (as linhas já escritas, contadas pelo barra-n).</summary>
        public static int NextLine(StringBuilder builder)
        {
            int lines = 0;
            for (int i = 0; i < builder.Length; i++)
                if (builder[i] == '\n') lines++;
            return lines;
        }

        /// <summary>
        /// A faixa de tela de um trecho dentro de uma linha: <paramref name="before"/> é tudo o que vem antes dele na linha e
        /// <paramref name="segment"/> o trecho (os dois com as mesmas marcações do texto). A altura é a da linha.
        /// </summary>
        public static bool TryGetSpan(Text text, int line, string before, string segment, out Rect screenRect)
        {
            screenRect = default;
            if (!TryGetBand(text, line, out Rect band)) return false;
            string[] lines = text.text.Split('\n');
            if (line >= lines.Length) return false;

            RectTransform rect = text.rectTransform;
            float lineWidth = Width(text, lines[line]);
            float start;
            switch (text.alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    start = rect.rect.center.x - lineWidth / 2f;
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    start = rect.rect.xMax - lineWidth;
                    break;
                default:
                    start = rect.rect.xMin;
                    break;
            }
            float x0 = start + Width(text, before);
            float x1 = start + Width(text, before + segment);

            // A faixa já está em pixels de tela; converte o trecho pela proporção entre a largura da tela e a do retângulo.
            float pixelsPerLocal = band.width / Mathf.Max(0.0001f, rect.rect.width);
            float screenLeft = band.xMin + (x0 - rect.rect.xMin) * pixelsPerLocal;
            float screenRight = band.xMin + (x1 - rect.rect.xMin) * pixelsPerLocal;
            screenRect = Rect.MinMaxRect(screenLeft, band.yMin, screenRight, band.yMax);
            return true;
        }

        // Largura de um trecho em unidades do retângulo. Mede com uma barra no fim: espaços no fim não contariam sozinhos.
        static float Width(Text text, string content)
        {
            TextGenerationSettings settings = text.GetGenerationSettings(Vector2.zero);
            var generator = new TextGenerator();
            float bar = generator.GetPreferredWidth("|", settings);
            // Uma marcação aberta sem fechar (um trecho cortado no meio da cor) seria medida como texto: fecha antes.
            float with = generator.GetPreferredWidth(CloseOpenTags(content) + "|", settings);
            return (with - bar) / Mathf.Max(0.0001f, text.pixelsPerUnit);
        }

        static readonly Regex Tag = new Regex("<(/?)(color|size|b|i)(=[^>]*)?>", RegexOptions.IgnoreCase);

        // Fecha, no fim do trecho, as marcações que ele abriu e não fechou.
        static string CloseOpenTags(string content)
        {
            var open = new List<string>();
            foreach (Match match in Tag.Matches(content))
            {
                string name = match.Groups[2].Value.ToLowerInvariant();
                if (match.Groups[1].Value.Length > 0) open.Remove(name);
                else open.Add(name);
            }
            var text = new StringBuilder(content);
            for (int i = open.Count - 1; i >= 0; i--) text.Append("</").Append(open[i]).Append('>');
            return text.ToString();
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
