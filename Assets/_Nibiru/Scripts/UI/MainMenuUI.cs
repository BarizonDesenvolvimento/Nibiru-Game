using System.Collections.Generic;
using Nibiru.Localization;
using Nibiru.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Menu principal provisório (cena MenuPrincipal): Continuar (o slot mais recente, se ele puder ser carregado),
    /// Novo jogo (no slot 1, enquanto a tela de slots não existe) e Sair. Setas ou direcional escolhem,
    /// Enter, Espaço ou o botão A confirmam. Montado por código; lê o teclado e o controle direto, pois
    /// o jogador (e o leitor de controles dele) só existe na cena da região.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameScene = "Regiao1_SitioDeUr";

        enum Item
        {
            Continuar,
            NovoJogo,
            Sair,
        }

        /// <summary>Slot do "Novo jogo" enquanto a tela de slots (A4b) não existe.</summary>
        const int NewGameSlot = 1;

        readonly List<Item> items = new List<Item>();
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
            PlayTime.Hold(this);
            if (SaveSlots.CanContinue) items.Add(Item.Continuar);
            items.Add(Item.NovoJogo);
            items.Add(Item.Sair);
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
                selected = (selected + move + items.Count) % items.Count;
                Refresh();
            }

            bool confirm = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame));
            if (confirm) Activate();
        }

        void Activate()
        {
            switch (items[selected])
            {
                case Item.Continuar: Continue(); break;
                case Item.NovoJogo: StartGame(); break;
                default: Quit(); break;
            }
        }

        /// <summary>True se o menu mostra "Continuar" (há um slot recente que pode ser carregado).</summary>
        public bool ShowsContinue => items.Contains(Item.Continuar);

        /// <summary>
        /// Começa uma partida nova no slot 1: zera o estado do mundo e os pontos de retorno, grava o começo no slot
        /// (para "Continuar" funcionar mesmo antes do primeiro checkpoint) e abre a região.
        /// </summary>
        public void StartGame()
        {
            SaveSlots.BeginNewGame(NewGameSlot, gameScene);
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameScene);
        }

        /// <summary>Abre o slot usado por último, no último checkpoint dele.</summary>
        public void Continue()
        {
            if (!SaveSlots.ContinueLatest()) Debug.LogWarning("Menu principal: não há partida para continuar.");
        }

        static string Label(Item item) => item switch
        {
            Item.Continuar => Loc.Get("ui.menu.continuar"),
            Item.NovoJogo => "Novo jogo",
            _ => "Sair do jogo",
        };

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
            for (int i = 0; i < items.Count; i++)
                text.AppendLine(i == selected ? $"<color={Gold}>›  {Label(items[i])}</color>" : $"<color={Muted}>   {Label(items[i])}</color>");
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
            text.font = Nibiru.UI.UiFont.Current;
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
