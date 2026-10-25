using Nibiru.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Menu principal provisório (cena MenuPrincipal): Novo jogo e Sair. Setas ou direcional escolhem,
    /// Enter, Espaço ou o botão A confirmam. Montado por código; lê o teclado e o controle direto, pois
    /// o jogador (e o leitor de controles dele) só existe na cena da região.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameScene = "Regiao1_SitioDeUr";

        static readonly string[] Items = { "Novo jogo", "Sair do jogo" };
        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        Text list;
        int selected;

        public string GameScene => gameScene;

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Build();
            Refresh();
        }

        void Update()
        {
            int move = 0;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) move++;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) move--;
            }
            if (pad != null)
            {
                if (pad.dpad.down.wasPressedThisFrame) move++;
                if (pad.dpad.up.wasPressedThisFrame) move--;
            }
            if (move != 0)
            {
                selected = (selected + move + Items.Length) % Items.Length;
                Refresh();
            }

            bool confirm = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
            if (confirm) Activate();
        }

        void Activate()
        {
            if (selected == 0) StartGame();
            else Quit();
        }

        /// <summary>Começa uma partida nova: zera o estado do mundo e os pontos de retorno e abre a região.</summary>
        public void StartGame()
        {
            Checkpoints.Clear();
            WorldState.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameScene);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Refresh()
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < Items.Length; i++)
                text.AppendLine(i == selected ? $"<color={Gold}>›  {Items[i]}</color>" : $"<color={Muted}>   {Items[i]}</color>");
            list.text = text.ToString();
        }

        void Build()
        {
            var canvasGo = new GameObject("Menu_Principal");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            MakeText(canvasGo.transform, "Titulo", "NIBIRU", 120, new Color(0.91f, 0.77f, 0.42f), 220f, 300f);
            list = MakeText(canvasGo.transform, "Itens", "", 46, Color.white, -80f, 300f);
            list.supportRichText = true;
            MakeText(canvasGo.transform, "Dica", "Setas e Enter  |  direcional e A", 26, new Color(0.6f, 0.63f, 0.68f), -380f, 80f);
        }

        static Text MakeText(Transform parent, string name, string content, int size, Color color, float y, float height)
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
            rect.sizeDelta = new Vector2(1200f, height);
            rect.anchoredPosition = new Vector2(0f, y);
            return text;
        }
    }
}
