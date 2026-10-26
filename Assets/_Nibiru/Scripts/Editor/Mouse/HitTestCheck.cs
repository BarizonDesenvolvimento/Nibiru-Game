using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Nibiru.EditorTools.Saving;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Mouse
{
    /// <summary>
    /// Verificação do hit-test do mouse pelo gerador de texto (A4c): para o menu principal de verdade (lista, tela de slots
    /// do Novo jogo e do Carregar, confirmação), em 1280x800, 1920x1080 e 3840x2160, desenha o Canvas numa textura com a escala
    /// real do CanvasScaler e confere que a faixa que <see cref="TextLines"/> calcula para cada linha com texto contém a
    /// tinta daquela linha, sem sobrar nem faltar linha. Roda numa pasta de teste (não toca nos slots reais).
    /// Menu: Nibiru > Testes > Mouse > Verificar o hit-test do texto.
    /// </summary>
    [InitializeOnLoad]
    static class HitTestCheck
    {
        const string MenuScenePath = "Assets/_Nibiru/Scenes/MenuPrincipal.unity";
        const int SpareLayer = 31;
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly (int w, int h)[] Sizes = { (1280, 800), (1920, 1080), (3840, 2160) };

        static string folder;

        static HitTestCheck() => Register("mouse.hittest", Begin);

        [MenuItem("Nibiru/Testes/Mouse/Verificar o hit-test do texto (3 resoluções)", priority = 1)]
        static void Run() => Go("mouse.hittest", MenuScenePath);

        static void Begin()
        {
            folder = Path.Combine(Application.persistentDataPath, "Teste_HitTest");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            SaveSlots.UseFolder(folder);
            SlotShortcuts.FillAll();
            SlotShortcuts.MakeOccupied(1);
            SlotGuard.Begin();

            var list = new List<(string, Func<IEnumerator>)>();
            foreach ((int w, int h) in Sizes)
            {
                int width = w, height = h;
                list.Add(($"Hit-test do texto em {w}x{h}", () => Resolution(width, height)));
            }
            list.Add(("Limpeza", Cleanup));
            Start("Mouse, verificação do hit-test", list);
        }

        static IEnumerator Cleanup()
        {
            SaveSlots.UseFolder(null);
            if (folder != null && folder.EndsWith("Teste_HitTest") && Directory.Exists(folder)) Directory.Delete(folder, true);
            yield break;
        }

        static IEnumerator Resolution(int w, int h)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuPrincipal");
            yield return new Until(() => Object.FindFirstObjectByType<MainMenuUI>() != null, 30f, "o menu abrir");
            int frame = Time.frameCount + 3;
            yield return new Until(() => Time.frameCount >= frame, 10f, "3 quadros");

            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            Canvas canvas = menu.GetComponentInChildren<Canvas>(true);
            var cam = new GameObject("HitTestCam").AddComponent<Camera>();
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            int[] layers = null;
            try
            {
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.cullingMask = 1 << SpareLayer;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 10f;
                cam.targetTexture = rt;
                layers = SetLayers(canvas.transform, SpareLayer);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                yield return new Delay(0.3f);

                // 1) menu principal; 2) Novo jogo; 3) Carregar; 4) confirmação de sobrescrever.
                menu.PressBack();
                Check(Verify(menu, canvas, cam, rt, $"{w}x{h} principal"), "principal");
                menu.OpenSlots(true);
                yield return new Delay(0.1f);
                Check(Verify(menu, canvas, cam, rt, $"{w}x{h} novo jogo"), "novo jogo");
                menu.ChooseNewGameSlot(1);
                menu.Move(0); // redesenha com a confirmação aberta
                yield return new Delay(0.1f);
                Check(Verify(menu, canvas, cam, rt, $"{w}x{h} confirmação"), "confirmação");
                menu.PressBack();
                menu.PressBack();
                menu.OpenSlots(false);
                yield return new Delay(0.1f);
                Check(Verify(menu, canvas, cam, rt, $"{w}x{h} carregar"), "carregar");
            }
            finally
            {
                if (layers != null) RestoreLayers(canvas.transform, layers);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cam.targetTexture = null;
                rt.Release();
                Object.Destroy(rt);
                Object.Destroy(cam.gameObject);
            }
        }

        // Desenha, lê os pixels e compara com as faixas calculadas. Registra o detalhe no Console.
        static bool Verify(MainMenuUI menu, Canvas canvas, Camera cam, RenderTexture rt, string label)
        {
            Text text = ActiveText(menu);
            // Só o texto medido aparece (o título e a dica da tela principal são outros textos).
            var hidden = canvas.GetComponentsInChildren<Text>(true).Where(t => t != text && t.enabled).ToList();
            foreach (Text other in hidden) other.enabled = false;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            foreach (Text other in hidden) other.enabled = true;
            int w = rt.width, h = rt.height;
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            Color32[] pixels = tex.GetPixels32();
            Object.Destroy(tex);

            int lineCount = TextLines.Count(text);
            string[] raw = text.text.Split('\n');
            var report = new StringBuilder($"Hit-test {label} ({(text.text.Contains("Cancelar") ? "diálogo" : "texto")}, {(text.name)}): escala {canvas.scaleFactor:0.###}, {lineCount} linhas calculadas, {raw.Length} no texto");

            // As faixas de cada linha com texto, de cima para baixo.
            var bands = new List<(int line, Rect rect)>();
            for (int i = 0; i < lineCount && i < raw.Length; i++)
            {
                string plain = Regex.Replace(raw[i], "<[^>]+>", "");
                if (plain.Trim().Length == 0) continue;
                if (TextLines.TryGetBand(text, i, out Rect rect)) bands.Add((i, rect));
            }
            bands = bands.OrderByDescending(b => b.rect.yMax).ToList();

            // As faixas de tinta: linhas de pixels com algo desenhado dentro da largura do texto.
            Rect area = bands.Count > 0 ? bands[0].rect : new Rect(0, 0, w, h);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(area.xMin), 0, w - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(area.xMax), 0, w);
            var ink = new List<(int min, int max)>();
            int start = -1, last = -10;
            for (int y = 0; y < h; y++)
            {
                bool inked = false;
                for (int x = x0; x < x1 && !inked; x++)
                {
                    Color32 c = pixels[y * w + x];
                    inked = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) > 60;
                }
                if (!inked) continue;
                if (start >= 0 && y - last > 2)
                {
                    ink.Add((start, last));
                    start = -1;
                }
                if (start < 0) start = y;
                last = y;
            }
            if (start >= 0) ink.Add((start, last));
            ink = ink.OrderByDescending(i => i.max).ToList();

            bool ok = true;
            if (ink.Count != bands.Count)
            {
                report.Append($"\n  FALHOU: {ink.Count} faixas de tinta x {bands.Count} linhas com texto");
                ok = false;
            }
            else
            {
                for (int i = 0; i < bands.Count; i++)
                {
                    Rect band = bands[i].rect;
                    float slack = band.height * 0.15f;
                    float center = (ink[i].min + ink[i].max) / 2f;
                    bool inside = center >= band.yMin && center <= band.yMax && ink[i].min >= band.yMin - slack && ink[i].max <= band.yMax + slack;
                    if (!inside)
                    {
                        ok = false;
                        report.Append($"\n  FALHOU linha {bands[i].line}: tinta {ink[i].min}-{ink[i].max}, faixa {band.yMin:0}-{band.yMax:0}");
                    }
                }
            }
            report.Append(ok ? "  → OK" : "");
            Debug.Log(report.ToString());
            return ok;
        }

        static Text ActiveText(MainMenuUI menu)
        {
            foreach (string field in new[] { "screenText", "list" })
            {
                var text = (Text)typeof(MainMenuUI).GetField(field, Any).GetValue(menu);
                if (text != null && text.gameObject.activeInHierarchy) return text;
            }
            throw new InvalidOperationException("nenhum texto do menu está ativo");
        }

        static int[] SetLayers(Transform root, int layer)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            int[] before = all.Select(t => t.gameObject.layer).ToArray();
            foreach (Transform t in all) t.gameObject.layer = layer;
            return before;
        }

        static void RestoreLayers(Transform root, int[] before)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length && i < before.Length; i++) all[i].gameObject.layer = before[i];
        }
    }
}
