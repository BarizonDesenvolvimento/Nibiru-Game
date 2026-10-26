using System.Collections.Generic;
using System.Text;
using Nibiru.Localization;
using Nibiru.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Menu principal (cena MenuPrincipal, blockout): Continuar (o slot jogável mais recente; escondido se não há),
    /// Novo jogo e Carregar (os dois abrem a tela de 3 slots), Sair. Na tela de slots, escolher um slot começa ou carrega
    /// a partida, e Delete (ou o botão X do controle) apaga o slot. Sobrescrever e apagar pedem confirmação, com o foco
    /// em "Cancelar". Setas ou direcional escolhem, Enter, Espaço ou o botão A confirmam, Esc ou o botão B voltam.
    /// Montado por código; lê o teclado e o controle direto, pois o jogador (e o leitor de controles dele) só existe na
    /// cena da região. Não usa o mouse.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameScene = "Regiao1_SitioDeUr";

        enum Item
        {
            Continuar,
            NovoJogo,
            Carregar,
            Sair,
        }

        enum Screen
        {
            Principal,
            Slots,
        }

        // Na tela de slots: 0 a 2 são os slots, 3 é "Voltar".
        const int BackRow = SaveSlots.SlotCount;

        readonly List<Item> items = new List<Item>();
        readonly ConfirmDialog dialog = new ConfirmDialog();
        readonly SaveSlots.SlotInfo[] infos = new SaveSlots.SlotInfo[SaveSlots.SlotCount];
        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";
        const string Warning = "#D9776A";

        Text list;
        Text screenText;
        GameObject title;
        GameObject hint;
        Screen screen;
        bool newGameMode;
        int selected;
        string message = "";

        public string GameScene => gameScene;

        /// <summary>True enquanto a tela de slots está aberta.</summary>
        public bool InSlotScreen => screen == Screen.Slots;

        /// <summary>True se a tela de slots é a do "Novo jogo" (e não a do "Carregar").</summary>
        public bool IsNewGameScreen => newGameMode;

        public bool IsDialogOpen => dialog.IsOpen;

        /// <summary>True se o foco do diálogo está em "Cancelar".</summary>
        public bool DialogFocusOnCancel => dialog.FocusOnCancel;

        /// <summary>O que o menu escreve na tela agora (para os testes).</summary>
        public string ScreenText => dialog.IsOpen || screen == Screen.Slots ? screenText.text : list.text;

        /// <summary>Slot em foco na tela de slots (1 a 3), ou 0 se o foco está em "Voltar" ou fora da tela.</summary>
        public int FocusedSlot => screen == Screen.Slots && selected < BackRow ? selected + 1 : 0;

        /// <summary>True se o menu mostra "Continuar" (há um slot jogável).</summary>
        public bool ShowsContinue => items.Contains(Item.Continuar);

        public bool ShowsLoad => items.Contains(Item.Carregar);

        void Start()
        {
            SessionReset.Clean();
            PlayTime.Hold(this);
            Build();
            ShowMain();
        }

        void Update()
        {
            int move = 0;
            bool confirm = false, back = false, delete = false;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (keyboard != null)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) move++;
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) move--;
                confirm |= keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
                back |= keyboard.escapeKey.wasPressedThisFrame;
                delete |= keyboard.deleteKey.wasPressedThisFrame;
            }
            if (pad != null)
            {
                if (pad.dpad.down.wasPressedThisFrame) move++;
                if (pad.dpad.up.wasPressedThisFrame) move--;
                confirm |= pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
                back |= pad.buttonEast.wasPressedThisFrame;
                delete |= pad.buttonWest.wasPressedThisFrame;
            }
            if (move != 0 || confirm || back || delete) Step(move, confirm, back, delete);
        }

        /// <summary>Um quadro de entrada. Também é por aqui que os testes "apertam" os botões.</summary>
        public void Step(int vertical, bool confirm, bool back, bool delete)
        {
            if (dialog.Handle(vertical, confirm, back))
            {
                Refresh();
                return;
            }

            if (screen == Screen.Principal)
            {
                if (vertical != 0) selected = (selected + vertical + items.Count) % items.Count;
                else if (confirm) Activate();
                Refresh();
                return;
            }

            if (back)
            {
                ShowMain();
                return;
            }
            if (vertical != 0)
            {
                selected = (selected + vertical + BackRow + 1) % (BackRow + 1);
                message = "";
            }
            else if (delete) RequestDelete(selected + 1);
            else if (confirm) ActivateSlotRow();
            Refresh();
        }

        /// <summary>Põe o foco no slot (1 a 3) da tela de slots.</summary>
        public void FocusSlot(int slot)
        {
            if (screen != Screen.Slots || slot < 1 || slot > SaveSlots.SlotCount) return;
            selected = slot - 1;
            Refresh();
        }

        public void Press() => Step(0, true, false, false);

        public void PressBack() => Step(0, false, true, false);

        public void PressDelete() => Step(0, false, false, true);

        public void Move(int vertical) => Step(vertical, false, false, false);

        // ---------- Tela principal ----------

        void ShowMain()
        {
            screen = Screen.Principal;
            RefreshSlots();
            items.Clear();
            if (SaveSlots.LatestSlot(infos) != 0) items.Add(Item.Continuar);
            items.Add(Item.NovoJogo);
            if (HasAnySlot()) items.Add(Item.Carregar);
            items.Add(Item.Sair);
            selected = 0;
            Refresh();
        }

        bool HasAnySlot()
        {
            foreach (SaveSlots.SlotInfo info in infos)
                if (info.state != SaveSlots.SlotState.Empty) return true;
            return false;
        }

        void RefreshSlots()
        {
            for (int i = 0; i < infos.Length; i++) infos[i] = SaveSlots.Inspect(i + 1);
        }

        void Activate()
        {
            switch (items[selected])
            {
                case Item.Continuar: Continue(); break;
                case Item.NovoJogo: OpenSlots(true); break;
                case Item.Carregar: OpenSlots(false); break;
                default: Quit(); break;
            }
        }

        /// <summary>Abre a tela de slots do "Novo jogo" (true) ou do "Carregar" (false), com o foco inicial definido.</summary>
        public void OpenSlots(bool forNewGame)
        {
            screen = Screen.Slots;
            newGameMode = forNewGame;
            message = "";
            RefreshSlots();
            selected = InitialSlotRow();
            Refresh();
        }

        // Novo jogo: o primeiro slot vazio (ou o 1). Carregar: o slot de "Continuar", ou o primeiro que tem arquivo.
        int InitialSlotRow()
        {
            if (newGameMode)
            {
                for (int i = 0; i < infos.Length; i++)
                    if (infos[i].state == SaveSlots.SlotState.Empty) return i;
                return 0;
            }
            int latest = SaveSlots.LatestSlot(infos);
            if (latest != 0) return latest - 1;
            for (int i = 0; i < infos.Length; i++)
                if (infos[i].state != SaveSlots.SlotState.Empty) return i;
            return 0;
        }

        void ActivateSlotRow()
        {
            if (selected == BackRow)
            {
                ShowMain();
                return;
            }
            int slot = selected + 1;
            if (newGameMode)
            {
                ChooseNewGameSlot(slot);
                return;
            }

            SaveSlots.SlotInfo info = infos[selected];
            if (info.state == SaveSlots.SlotState.Occupied)
            {
                if (!SaveSlots.Load(slot)) message = Loc.Get("ui.slots.aviso_nao_carrega", "Este slot não pode ser carregado. Você pode apagá-lo.");
            }
            else if (info.state == SaveSlots.SlotState.Empty) message = Loc.Get("ui.slots.aviso_vazio", "Slot vazio.");
            else message = Loc.Get("ui.slots.aviso_nao_carrega", "Este slot não pode ser carregado. Você pode apagá-lo.");
        }

        // ---------- Novo jogo, continuar, apagar ----------

        /// <summary>
        /// Novo jogo no slot: se ele está vazio começa na hora; se tem arquivo (ocupado, demo concluída ou ilegível),
        /// pede confirmação para sobrescrever.
        /// </summary>
        public void ChooseNewGameSlot(int slot)
        {
            if (SaveSlots.Inspect(slot).state == SaveSlots.SlotState.Empty)
            {
                StartGame(slot);
                return;
            }
            dialog.Open(
                string.Format(Loc.Get("ui.confirmar.sobrescrever.titulo", "Sobrescrever o slot {0}?"), slot),
                Loc.Get("ui.confirmar.sobrescrever.texto", "O jogo salvo neste slot será perdido."),
                Loc.Get("ui.confirmar.sobrescrever.botao", "Sobrescrever"),
                () => StartGame(slot));
        }

        /// <summary>
        /// Começa uma partida nova no slot: zera o estado do mundo e os pontos de retorno, grava o começo no slot
        /// (para "Continuar" funcionar mesmo antes do primeiro checkpoint) e abre a região.
        /// </summary>
        public void StartGame(int slot)
        {
            SaveSlots.BeginNewGame(slot, gameScene);
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameScene);
        }

        /// <summary>Abre o slot jogável mais recente, no último checkpoint dele.</summary>
        public void Continue()
        {
            if (!SaveSlots.ContinueLatest()) Debug.LogWarning("Menu principal: não há partida para continuar.");
        }

        /// <summary>Pede confirmação e apaga o slot (nada acontece se ele está vazio).</summary>
        public void RequestDelete(int slot)
        {
            if (infos[slot - 1].state == SaveSlots.SlotState.Empty) return;
            dialog.Open(
                string.Format(Loc.Get("ui.confirmar.apagar.titulo", "Apagar o slot {0}?"), slot),
                Loc.Get("ui.confirmar.apagar.texto", "Esta ação não pode ser desfeita."),
                Loc.Get("ui.confirmar.apagar.botao", "Apagar"),
                () =>
                {
                    SaveSlots.Delete(slot);
                    RefreshSlots();
                    message = "";
                    Refresh();
                });
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Desenho ----------

        static string Label(Item item) => item switch
        {
            Item.Continuar => Loc.Get("ui.menu.continuar", "Continuar"),
            Item.NovoJogo => Loc.Get("ui.menu.novo_jogo", "Novo jogo"),
            Item.Carregar => Loc.Get("ui.menu.carregar", "Carregar"),
            _ => Loc.Get("ui.menu.sair", "Sair do jogo"),
        };

        void Refresh()
        {
            bool showMain = screen == Screen.Principal && !dialog.IsOpen;
            list.gameObject.SetActive(showMain);
            title.SetActive(showMain);
            hint.SetActive(showMain);
            screenText.gameObject.SetActive(!showMain);

            if (dialog.IsOpen) screenText.text = dialog.Render(Gold, Muted);
            else if (screen == Screen.Slots) screenText.text = RenderSlots();
            else
            {
                var text = new StringBuilder();
                for (int i = 0; i < items.Count; i++)
                    text.AppendLine(i == selected ? $"<color={Gold}>›  {Label(items[i])}</color>" : $"<color={Muted}>   {Label(items[i])}</color>");
                list.text = text.ToString();
            }
        }

        string RenderSlots()
        {
            var text = new StringBuilder();
            string titleKey = newGameMode ? Loc.Get("ui.slots.titulo_novo", "NOVO JOGO") : Loc.Get("ui.slots.titulo_carregar", "CARREGAR");
            text.Append("<size=56><b>").Append(titleKey).AppendLine("</b></size>").AppendLine();
            for (int i = 0; i < infos.Length; i++)
            {
                string row = SlotRows.Format(infos[i]);
                if (i == selected) text.Append("<color=").Append(Gold).Append(">›  ").Append(row).AppendLine("</color>");
                else text.Append("<color=").Append(infos[i].state == SaveSlots.SlotState.Unreadable ? Warning : infos[i].state == SaveSlots.SlotState.Empty ? Muted : "#FFFFFF")
                    .Append(">   ").Append(row).AppendLine("</color>");
                text.AppendLine();
            }
            string back = Loc.Get("ui.menu.voltar", "Voltar");
            if (selected == BackRow) text.Append("<color=").Append(Gold).Append(">›  ").Append(back).AppendLine("</color>");
            else text.Append("<color=").Append(Muted).Append(">   ").Append(back).AppendLine("</color>");
            text.AppendLine();
            text.Append("<color=").Append(Warning).Append(">").Append(message).AppendLine("</color>");
            bool pad = Gamepad.current != null;
            text.Append("<size=24><color=").Append(Muted).Append(">")
                .Append(string.Format(Loc.Get("ui.slots.dica", "[{0}] Escolher     [{1}] Voltar     [{2}] Apagar"),
                    pad ? "A" : "Enter", pad ? "B" : "Esc", pad ? "X" : "Delete"))
                .Append("</color></size>");
            return text.ToString();
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

            title = MakeText(canvasGo.transform, "Titulo", "NIBIRU", 120, new Color(0.91f, 0.77f, 0.42f), 220f, 300f, 1200f).gameObject;
            list = MakeText(canvasGo.transform, "Itens", "", 46, Color.white, -80f, 300f, 1200f);
            list.supportRichText = true;
            hint = MakeText(canvasGo.transform, "Dica", Loc.Get("ui.menu.dica", "Setas e Enter  |  direcional e A"), 26, new Color(0.6f, 0.63f, 0.68f), -380f, 80f, 1200f).gameObject;
            screenText = MakeText(canvasGo.transform, "Tela", "", 34, Color.white, 0f, 760f, 1700f);
            screenText.supportRichText = true;
            screenText.alignment = TextAnchor.MiddleLeft;
        }

        static Text MakeText(Transform parent, string name, string content, int size, Color color, float y, float height, float width)
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
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
            return text;
        }
    }
}
