using Nibiru.Player;
using Nibiru.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela provisória de fim da demo: aparece quando o evento do fim da fatia dispara e, ao confirmar,
    /// zera a partida e volta ao menu principal. Montada por código (sem Instantiate), no tempo real.
    /// </summary>
    public class DemoEndScreen : MonoBehaviour
    {
        [SerializeField] string menuScene = "MenuPrincipal";

        PlayerInputReader input;
        GameObject panel;
        Text hint;
        int shownFrame = -10;

        public bool IsOpen => panel != null && panel.activeSelf;
        public string MenuScene => menuScene;

        public void Show()
        {
            if (IsOpen) return;
            input = FindFirstObjectByType<PlayerInputReader>();
            if (panel == null) Build();
            Time.timeScale = 0f;
            if (input != null)
            {
                input.SetGameplayEnabled(false);
                hint.text = $"[{input.ConfirmButtonLabel}]  Voltar ao menu principal";
            }
            shownFrame = Time.frameCount;
            panel.SetActive(true);
        }

        void Update()
        {
            // O mesmo apertar que fechou a leitura do diário não confirma esta tela.
            if (!IsOpen || input == null || Time.frameCount - shownFrame < 20) return;
            if (input.ConfirmPressed) ReturnToMenu();
        }

        /// <summary>Zera a partida e carrega o menu principal.</summary>
        public void ReturnToMenu()
        {
            Checkpoints.Clear();
            WorldState.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuScene);
        }

        void Build()
        {
            var canvasGo = new GameObject("Fim_Da_Demo");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            panel = canvasGo;

            var back = new GameObject("Fundo").AddComponent<Image>();
            back.transform.SetParent(canvasGo.transform, false);
            back.color = Color.black;
            back.raycastTarget = false;
            Stretch(back.rectTransform);

            MakeText(canvasGo.transform, "Titulo", "Fim da demo", 72, new Color(0.91f, 0.77f, 0.42f), 120f);
            MakeText(canvasGo.transform, "Texto",
                "Elena e Samir fogem para Tell Amar.\nObrigado por jogar esta fatia de Nibiru.", 34, new Color(0.85f, 0.87f, 0.9f), -20f);
            hint = MakeText(canvasGo.transform, "Dica", "", 30, new Color(0.6f, 0.63f, 0.68f), -200f);
        }

        static Text MakeText(Transform parent, string name, string content, int size, Color color, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1500f, 220f);
            rect.anchoredPosition = new Vector2(0f, y);
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
