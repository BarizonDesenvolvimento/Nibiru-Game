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
    /// cena da região. O mouse entra por cima: passar o mouse move o foco (o mesmo destaque do teclado), o clique esquerdo
    /// ativa, o direito volta (como Esc ou B), e cada slot que tem arquivo ganha o botão "Apagar" na ponta da linha.
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

        // Onde cada item está no texto desenhado (número da linha), para o mouse. Refeito a cada Refresh.
        readonly List<int> mainLines = new List<int>();
        readonly int[] slotLines = new int[SaveSlots.SlotCount + 1];
        readonly string[] deleteBefore = new string[SaveSlots.SlotCount];
        const string DeleteSegment = "[Apagar]";
        string deleteSegment = DeleteSegment;

        public string GameScene => gameScene;

        /// <summary>True enquanto a tela de slots está aberta.</summary>
        public bool InSlotScreen => screen == Screen.Slots;

        /// <summary>True se a tela de slots é a do "Novo jogo" (e não a do "Carregar").</summary>
        public bool IsNewGameScreen => newGameMode;

        public bool IsDialogOpen => dialog.IsOpen;

        /// <summary>Para os testes: quantas linhas o texto desenhado tem (igual às quebras de linha do texto, sem quebra automática).</summary>
        public int RenderedLineCount => TextLines.Count(CurrentText());

        /// <summary>O item em foco (na tela principal) ou a linha em foco (na tela de slots: 0 a 2 são os slots, 3 é "Voltar").</summary>
        public int FocusedRow => selected;

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
            CursorPolicy.RequestFree(this, true);
            Build();
            ShowMain();
        }

        void OnDestroy() => CursorPolicy.Release(this);

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
            if (move != 0 || confirm || back || delete)
            {
                MenuPointer.NoteKeyboardOrPad();
                Step(move, confirm, back, delete);
                return;
            }
            PointerStep(MenuPointer.Read());
        }

        /// <summary>
        /// Um quadro do mouse, por cima do teclado e do controle: passar sobre um item move o foco, o clique esquerdo ativa
        /// (e o clique em "Apagar" apaga), o direito volta ou cancela. Também é por aqui que os testes "mexem" o mouse.
        /// </summary>
        public void PointerStep(MenuPointer.State pointer)
        {
            if (!pointer.Any) return;
            Text text = CurrentText();

            if (dialog.IsOpen)
            {
                if (pointer.rightClick)
                {
                    Step(0, false, true, false);
                    return;
                }
                int button = RowAt(text, pointer.position, dialog.CancelLine, dialog.ConfirmLine);
                if (button < 0 || !(pointer.moved || pointer.leftClick)) return;
                dialog.SetFocus(button == 0);
                if (pointer.leftClick) Step(0, true, false, false);
                else Refresh();
                return;
            }

            if (screen == Screen.Principal)
            {
                int row = RowAt(text, pointer.position, mainLines);
                if (row < 0 || !(pointer.moved || pointer.leftClick)) return;
                selected = row;
                if (pointer.leftClick) Step(0, true, false, false);
                else Refresh();
                return;
            }

            if (pointer.rightClick)
            {
                Step(0, false, true, false);
                return;
            }
            int slotRow = RowAt(text, pointer.position, slotLines);
            if (slotRow < 0 || !(pointer.moved || pointer.leftClick)) return;
            if (selected != slotRow) message = "";
            selected = slotRow;
            if (pointer.leftClick)
            {
                if (OverDeleteButton(text, slotRow, pointer.position)) RequestDelete(slotRow + 1);
                else Step(0, true, false, false);
            }
            Refresh();
        }

        Text CurrentText() => dialog.IsOpen || screen == Screen.Slots ? screenText : list;

        // O botão "Apagar" fica na ponta da linha de um slot que tem arquivo.
        bool OverDeleteButton(Text text, int slotRow, Vector2 position)
        {
            if (slotRow >= SaveSlots.SlotCount || infos[slotRow].state == SaveSlots.SlotState.Empty || string.IsNullOrEmpty(deleteBefore[slotRow])) return false;
            return TextLines.TryGetSpan(text, slotLines[slotRow], deleteBefore[slotRow], deleteSegment, out Rect span) && span.Contains(position);
        }

        static int RowAt(Text text, Vector2 position, params int[] lines) => RowAt(text, position, (IList<int>)lines);

        // Qual das linhas (na ordem dada) está sob o ponteiro, ou -1.
        static int RowAt(Text text, Vector2 position, IList<int> lines)
        {
            int line = TextLines.LineAt(text, position);
            if (line < 0) return -1;
            for (int i = 0; i < lines.Count; i++)
                if (lines[i] == line) return i;
            return -1;
        }

        /// <summary>Só para os testes: a faixa de tela de um item da lista (principal) ou de um slot/voltar (tela de slots).</summary>
        public bool TryGetRowRect(int row, out Rect rect)
        {
            rect = default;
            Text text = CurrentText();
            int line;
            if (dialog.IsOpen) line = row == 0 ? dialog.CancelLine : dialog.ConfirmLine;
            else if (screen == Screen.Principal) line = row < mainLines.Count ? mainLines[row] : -1;
            else line = row < slotLines.Length ? slotLines[row] : -1;
            return line >= 0 && TextLines.TryGetBand(text, line, out rect);
        }

        /// <summary>Só para os testes: a faixa de tela do botão "Apagar" de um slot (0 a 2).</summary>
        public bool TryGetDeleteRect(int slotRow, out Rect rect)
        {
            rect = default;
            if (screen != Screen.Slots || dialog.IsOpen || slotRow >= SaveSlots.SlotCount || string.IsNullOrEmpty(deleteBefore[slotRow])) return false;
            return TextLines.TryGetSpan(screenText, slotLines[slotRow], deleteBefore[slotRow], deleteSegment, out rect);
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
                mainLines.Clear();
                for (int i = 0; i < items.Count; i++)
                {
                    mainLines.Add(TextLines.NextLine(text));
                    text.AppendLine(i == selected ? $"<color={Gold}>›  {Label(items[i])}</color>" : $"<color={Muted}>   {Label(items[i])}</color>");
                }
                list.text = text.ToString();
            }
        }

        string RenderSlots()
        {
            var text = new StringBuilder();
            string titleKey = newGameMode ? Loc.Get("ui.slots.titulo_novo", "NOVO JOGO") : Loc.Get("ui.slots.titulo_carregar", "CARREGAR");
            text.Append("<size=56><b>").Append(titleKey).AppendLine("</b></size>").AppendLine();
            deleteSegment = "[" + Loc.Get("ui.slots.apagar", "Apagar") + "]";
            for (int i = 0; i < infos.Length; i++)
            {
                string row = SlotRows.Format(infos[i]);
                slotLines[i] = TextLines.NextLine(text);
                bool filled = infos[i].state != SaveSlots.SlotState.Empty;
                string color = i == selected ? Gold : infos[i].state == SaveSlots.SlotState.Unreadable ? Warning : infos[i].state == SaveSlots.SlotState.Empty ? Muted : "#FFFFFF";
                string before = "<color=" + color + ">" + (i == selected ? "›  " : "   ") + row + "   ";
                deleteBefore[i] = filled ? before : null;
                text.Append(filled ? before + deleteSegment : before.TrimEnd()).AppendLine("</color>");
                text.AppendLine();
            }
            string back = Loc.Get("ui.menu.voltar", "Voltar");
            slotLines[BackRow] = TextLines.NextLine(text);
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
            screenText = MakeText(canvasGo.transform, "Tela", "", 34, Color.white, 0f, 760f, 1800f);
            screenText.supportRichText = true;
            // Sem quebra automática: uma linha de slot comprida não pode virar duas (o mouse conta as linhas pelas quebras do texto).
            screenText.horizontalOverflow = HorizontalWrapMode.Overflow;
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
