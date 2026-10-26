using System.Collections.Generic;
using System.Text;
using Nibiru.Documents;
using Nibiru.Localization;
using Nibiru.Player;
using Nibiru.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Menu de pausa (Esc ou Start): continuar, opções, voltar ao último checkpoint e sair.
    /// Nas opções, esquerda e direita ajustam sensibilidades e inverter o eixo vertical.
    /// Só abre com a Elena no controle (não abre por cima de outro menu, diálogo ou morte).
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// O mouse entra por cima do teclado e do controle: passar sobre um item move o foco, o clique esquerdo ativa (nas Opções,
    /// o lado esquerdo ou direito da seta ajusta), o direito volta, e a roda rola o Arquivo. Enquanto a pausa está aberta o
    /// cursor fica livre.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] GameObject panel;
        [SerializeField] Text content;

        enum Page
        {
            Principal,
            Opcoes,
            Arquivo,
        }

        // O item "Arquivo" (documentos e fitas já lidos) fica logo depois das opções.
        const int ArchiveItem = 2;
        const int MainItemCount = 6;
        const int ExitToMenuItem = 4;

        static string MainItem(int index)
        {
            switch (index)
            {
                case 0: return "Continuar";
                case 1: return "Opções";
                case ArchiveItem: return Loc.Get("ui.arquivo.item_menu", "Arquivo");
                case 3: return "Voltar ao último checkpoint";
                case ExitToMenuItem: return Loc.Get("ui.pausa.sair_menu", "Sair para o menu");
                default: return "Sair do jogo";
            }
        }
        const int OptionItemCount = 5;
        const float SensitivityStep = 0.1f;
        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        readonly StringBuilder builder = new StringBuilder();
        readonly ConfirmDialog dialog = new ConfirmDialog();
        Page page;
        int selected;
        float timeScaleBefore = 1f;
        int openedFrame = -10;

        // Onde cada item está no texto desenhado (linha), para o mouse. Refeito a cada Refresh.
        readonly Dictionary<int, int> rowLines = new Dictionary<int, int>();
        readonly string[] optionBefore = new string[OptionItemCount];
        readonly string[] optionGroup = new string[OptionItemCount];
        int archiveUpLine = -1;
        int archiveDownLine = -1;
        int enableGameplayFrame = -1;
        bool pointerAction;

        // Arquivo: as entradas já lidas ou ouvidas, na ordem do catálogo, e a leitura aberta por cima do menu.
        readonly List<ArchiveCatalog.Entry> archiveRows = new List<ArchiveCatalog.Entry>();
        ReadingScreenUI openReading;

        // A lista rola: cada linha é um título de grupo (rowIndex -1) ou uma entrada (rowIndex = posição em archiveRows).
        // A janela mostra ArchiveWindow linhas a partir de archiveTop, com a letra normal; ▲ e ▼ dizem que há mais.
        struct ArchiveLine
        {
            public string text;
            public int rowIndex;
        }

        const int ArchiveWindow = 7;
        readonly List<ArchiveLine> archiveLines = new List<ArchiveLine>();
        int archiveTop;

        public bool IsOpen => panel.activeSelf;

        /// <summary>Para os testes: a página aberta (Principal, Opcoes ou Arquivo), o item em foco e o texto desenhado.</summary>
        public string PageName => page.ToString();

        public int FocusedRow => selected;

        public string ScreenText => content.text;

        /// <summary>A confirmação de "Sair para o menu" (para os testes).</summary>
        public ConfirmDialog ExitDialog => dialog;

        void Awake() => panel.SetActive(false);

        void OnDestroy() => CursorPolicy.Release(this);

        void Update()
        {
            // Fechar com o clique do mouse devolve o jogo no quadro seguinte, para o mesmo clique não atirar nem atacar.
            if (enableGameplayFrame >= 0 && Time.frameCount > enableGameplayFrame)
            {
                enableGameplayFrame = -1;
                if (!IsOpen) input.SetGameplayEnabled(true);
            }
            if (!IsOpen)
            {
                // O mesmo Esc que fechou outro menu neste quadro não abre a pausa.
                if (input.PausePressed && input.GameplayEnabled && Time.frameCount != input.GameplayToggledFrame) Open();
                return;
            }
            if (Time.frameCount == openedFrame) return;
            // Com uma entrada do Arquivo aberta na tela de leitura, a leitura é quem recebe os botões.
            if (openReading != null && openReading.IsOpen) return;

            // A confirmação de "Sair para o menu" consome os botões (Esc e voltar cancelam).
            if (dialog.IsOpen)
            {
                int step = input.VerticalNavStep;
                bool confirmPressed = input.ConfirmPressed;
                bool backPressed = input.PausePressed || input.BackPressed;
                if (step != 0 || confirmPressed || backPressed)
                {
                    MenuPointer.NoteKeyboardOrPad();
                    dialog.Handle(step, confirmPressed, backPressed);
                    Refresh();
                    return;
                }
                PointerStep(MenuPointer.Read());
                return;
            }

            if (input.PausePressed)
            {
                MenuPointer.NoteKeyboardOrPad();
                Close();
                return;
            }
            if (input.BackPressed)
            {
                MenuPointer.NoteKeyboardOrPad();
                GoBack(false);
                return;
            }

            int count = page == Page.Principal ? MainItemCount : page == Page.Arquivo ? Mathf.Max(1, archiveRows.Count) : OptionItemCount;
            int vertical = input.VerticalNavStep;
            int horizontal = page == Page.Opcoes ? input.HorizontalNavStep : 0;
            bool confirm = input.ConfirmPressed;
            if (vertical != 0 || horizontal != 0 || confirm)
            {
                MenuPointer.NoteKeyboardOrPad();
                if (vertical != 0)
                {
                    selected = (selected + vertical + count) % count;
                    if (page == Page.Arquivo) KeepArchiveSelectionVisible();
                    Refresh();
                }
                if (horizontal != 0) AdjustOption(horizontal);
                if (confirm) Activate();
                return;
            }

            PointerStep(MenuPointer.Read());
        }

        // Voltar (Esc, B ou clique direito): das Opções e do Arquivo para a página principal; da principal, fecha a pausa.
        void GoBack(bool byPointer)
        {
            if (page == Page.Opcoes) ShowPage(Page.Principal, 1);
            else if (page == Page.Arquivo) ShowPage(Page.Principal, ArchiveItem);
            else Close(byPointer);
        }

        /// <summary>
        /// Um quadro do mouse, por cima do teclado e do controle (também é por aqui que os testes "mexem" o mouse): passar
        /// sobre um item move o foco, o clique esquerdo ativa, o direito volta ou cancela e a roda rola o Arquivo.
        /// </summary>
        public void PointerStep(MenuPointer.State pointer)
        {
            if (!IsOpen || !pointer.Any || Time.frameCount == openedFrame) return;
            if (openReading != null && openReading.IsOpen) return;

            if (dialog.IsOpen)
            {
                if (pointer.rightClick)
                {
                    dialog.Cancel();
                    Refresh();
                    return;
                }
                int button = ButtonAt(pointer.position);
                if (button < 0 || !(pointer.moved || pointer.leftClick)) return;
                dialog.SetFocus(button == 0);
                if (pointer.leftClick)
                {
                    if (button == 0) dialog.Cancel();
                    else dialog.Confirm();
                }
                Refresh();
                return;
            }

            if (pointer.rightClick)
            {
                GoBack(true);
                return;
            }
            if (page == Page.Arquivo && pointer.wheel != 0f) ScrollArchive(pointer.wheel > 0f ? -1 : 1);

            int line = TextLines.LineAt(content, pointer.position);
            if (line < 0) return;
            if (page == Page.Arquivo && pointer.leftClick && (line == archiveUpLine || line == archiveDownLine))
            {
                ScrollArchive(line == archiveUpLine ? -1 : 1);
                return;
            }
            int row = RowOfLine(line);
            if (row < 0 || !(pointer.moved || pointer.leftClick)) return;
            selected = row;
            if (pointer.leftClick)
            {
                pointerAction = true;
                try
                {
                    if (page == Page.Opcoes && row < 4) ClickOption(row, pointer.position);
                    else Activate();
                }
                finally
                {
                    pointerAction = false;
                }
            }
            Refresh();
        }

        int RowOfLine(int line)
        {
            foreach (KeyValuePair<int, int> pair in rowLines)
                if (pair.Value == line) return pair.Key;
            return -1;
        }

        // 0 = "Cancelar", 1 = botão de confirmar, -1 = nenhum.
        int ButtonAt(Vector2 position)
        {
            int line = TextLines.LineAt(content, position);
            if (line < 0) return -1;
            if (line == dialog.CancelLine) return 0;
            return line == dialog.ConfirmLine ? 1 : -1;
        }

        // Nas Opções, o clique do lado esquerdo do "‹ valor ›" diminui e o do lado direito aumenta.
        void ClickOption(int row, Vector2 position)
        {
            int direction = 1;
            if (TextLines.TryGetSpan(content, rowLines[row], optionBefore[row], optionGroup[row], out Rect group))
                direction = position.x < group.center.x ? -1 : 1;
            AdjustOption(direction);
        }

        /// <summary>Só para os testes: a faixa de tela de um item (linha inteira).</summary>
        public bool TryGetRowRect(int row, out Rect rect)
        {
            rect = default;
            if (dialog.IsOpen) return TextLines.TryGetBand(content, row == 0 ? dialog.CancelLine : dialog.ConfirmLine, out rect);
            return rowLines.TryGetValue(row, out int line) && TextLines.TryGetBand(content, line, out rect);
        }

        /// <summary>Só para os testes: a faixa da seta esquerda (-1) ou direita (+1) de uma opção, dividida ao meio do "‹ valor ›".</summary>
        public bool TryGetOptionHalf(int row, int direction, out Rect rect)
        {
            rect = default;
            if (page != Page.Opcoes || !rowLines.TryGetValue(row, out int line) || row >= 4) return false;
            if (!TextLines.TryGetSpan(content, line, optionBefore[row], optionGroup[row], out Rect group)) return false;
            rect = direction < 0 ? Rect.MinMaxRect(group.xMin, group.yMin, group.center.x, group.yMax) : Rect.MinMaxRect(group.center.x, group.yMin, group.xMax, group.yMax);
            return true;
        }

        /// <summary>Linha de tela do marcador ▲ (-1) ou ▼ (+1) do Arquivo, para os testes.</summary>
        public int ArchiveMarkerLine(int direction) => direction < 0 ? archiveUpLine : archiveDownLine;

        /// <summary>Só para os testes: a faixa de tela do marcador ▲ (-1) ou ▼ (+1) do Arquivo.</summary>
        public bool TryGetArchiveMarkerRect(int direction, out Rect rect)
        {
            rect = default;
            int line = ArchiveMarkerLine(direction);
            return line >= 0 && TextLines.TryGetBand(content, line, out rect);
        }

        void Open()
        {
            enableGameplayFrame = -1;
            CursorPolicy.RequestFree(this, false);
            panel.SetActive(true);
            openedFrame = Time.frameCount;
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            PlayTime.Hold(this);
            input.SetGameplayEnabled(false);
            ShowPage(Page.Principal, 0);
        }

        void Close() => Close(pointerAction);

        // byPointer: fechou com o clique do mouse; o jogo só volta a ouvir o controle no quadro seguinte.
        void Close(bool byPointer)
        {
            dialog.Cancel();
            GameOptions.Save();
            panel.SetActive(false);
            Time.timeScale = timeScaleBefore;
            PlayTime.Release(this);
            CursorPolicy.Release(this);
            if (byPointer) enableGameplayFrame = Time.frameCount;
            else input.SetGameplayEnabled(true);
        }

        void ShowPage(Page next, int selection)
        {
            page = next;
            selected = selection;
            if (next == Page.Arquivo)
            {
                CollectArchiveRows();
                archiveTop = 0;
                KeepArchiveSelectionVisible();
            }
            Refresh();
        }

        void CollectArchiveRows()
        {
            archiveRows.Clear();
            archiveLines.Clear();
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            if (catalog == null) return;
            foreach (ArchiveCatalog.Group group in catalog.groups)
            {
                List<ArchiveCatalog.Entry> unlocked = catalog.UnlockedIn(group);
                if (unlocked.Count == 0) continue;
                archiveLines.Add(new ArchiveLine { text = group.LocalizedTitle, rowIndex = -1 });
                foreach (ArchiveCatalog.Entry entry in unlocked)
                {
                    archiveLines.Add(new ArchiveLine { text = entry.Title, rowIndex = archiveRows.Count });
                    archiveRows.Add(entry);
                }
            }
        }

        int LineOfRow(int row)
        {
            for (int i = 0; i < archiveLines.Count; i++)
                if (archiveLines[i].rowIndex == row) return i;
            return 0;
        }

        // Ajusta a janela para o item escolhido aparecer (e o título do grupo dele, se for o primeiro do grupo).
        void KeepArchiveSelectionVisible()
        {
            if (archiveLines.Count == 0) return;
            int line = LineOfRow(selected);
            int first = line > 0 && archiveLines[line - 1].rowIndex < 0 ? line - 1 : line;
            if (first < archiveTop) archiveTop = first;
            if (line > archiveTop + ArchiveWindow - 1) archiveTop = line - ArchiveWindow + 1;
            ClampArchiveTop();
        }

        void ClampArchiveTop() => archiveTop = Mathf.Clamp(archiveTop, 0, Mathf.Max(0, archiveLines.Count - ArchiveWindow));

        // Roda do mouse: rola a janela e leva a seleção junto se ela sair de vista.
        void ScrollArchive(int lines)
        {
            if (archiveLines.Count <= ArchiveWindow) return;
            int before = archiveTop;
            archiveTop += lines;
            ClampArchiveTop();
            if (archiveTop == before) return;
            int line = LineOfRow(selected);
            if (line < archiveTop || line > archiveTop + ArchiveWindow - 1)
            {
                int target = Mathf.Clamp(line, archiveTop, archiveTop + ArchiveWindow - 1);
                // Escolhe a entrada visível mais perto (os títulos de grupo não são escolhíveis).
                int best = -1;
                for (int i = archiveTop; i < archiveTop + ArchiveWindow && i < archiveLines.Count; i++)
                    if (archiveLines[i].rowIndex >= 0 && (best < 0 || Mathf.Abs(i - target) < Mathf.Abs(best - target))) best = i;
                if (best >= 0) selected = archiveLines[best].rowIndex;
            }
            Refresh();
        }

        // Rever uma entrada: abre na tela de leitura, sem disparar evento nem recompensa (só o que foi lido aparece aqui).
        void OpenArchiveEntry()
        {
            if (archiveRows.Count == 0)
            {
                ShowPage(Page.Principal, ArchiveItem);
                return;
            }
            openReading = FindFirstObjectByType<ReadingScreenUI>();
            if (openReading == null) return;
            ArchiveCatalog.Entry entry = archiveRows[Mathf.Clamp(selected, 0, archiveRows.Count - 1)];
            content.gameObject.SetActive(false);
            openReading.Show(entry.Title, entry.Body, entry.Footer, OnArchiveReadingClosed);
        }

        void OnArchiveReadingClosed()
        {
            openReading = null;
            content.gameObject.SetActive(true);
            // O mesmo apertar que fechou a leitura não age no menu.
            openedFrame = Time.frameCount;
            Refresh();
        }

        void Activate()
        {
            if (page == Page.Principal)
            {
                switch (selected)
                {
                    case 0: Close(); break;
                    case 1: ShowPage(Page.Opcoes, 0); break;
                    case ArchiveItem: ShowPage(Page.Arquivo, 0); break;
                    case 3:
                        Close();
                        Checkpoints.Restart(gameObject.scene.name);
                        break;
                    case ExitToMenuItem: RequestExitToMenu(); break;
                    case 5: Quit(); break;
                }
                return;
            }

            if (page == Page.Arquivo)
            {
                OpenArchiveEntry();
                return;
            }

            if (selected == 2 || selected == 3) AdjustOption(1);
            else if (selected == 4) ShowPage(Page.Principal, 1);
        }

        /// <summary>Pede confirmação (o foco começa em "Cancelar") e, confirmado, volta ao menu principal com o estado limpo.</summary>
        public void RequestExitToMenu()
        {
            dialog.Open(
                Loc.Get("ui.confirmar.sair_menu.titulo", "Sair para o menu?"),
                Loc.Get("ui.confirmar.sair_menu.texto", "O progresso desde o último checkpoint será perdido."),
                Loc.Get("ui.confirmar.sair_menu.botao", "Sair para o menu"),
                () =>
                {
                    GameOptions.Save();
                    SessionReset.ToMenu();
                });
            Refresh();
        }

        void AdjustOption(int direction)
        {
            switch (selected)
            {
                case 0: GameOptions.MouseSensitivity = Round(GameOptions.MouseSensitivity + direction * SensitivityStep); break;
                case 1: GameOptions.GamepadSensitivity = Round(GameOptions.GamepadSensitivity + direction * SensitivityStep); break;
                case 2: GameOptions.InvertY = !GameOptions.InvertY; break;
                case 3: NextLanguage(direction); break;
                default: return;
            }
            Refresh();
        }

        static float Round(float value) => Mathf.Round(value * 10f) / 10f;

        // Troca o idioma dos textos (esquerda e direita, ou confirmar). Os textos fixos do menu ainda são em português:
        // eles entram na tradução na parte 2 da localização.
        static void NextLanguage(int direction)
        {
            string[] languages = Localization.Loc.Languages;
            int index = System.Array.IndexOf(languages, Localization.Loc.Language);
            Localization.Loc.SetLanguage(languages[(index + direction + languages.Length) % languages.Length]);
        }

        static void Quit()
        {
            GameOptions.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Refresh()
        {
            builder.Clear();
            rowLines.Clear();
            archiveUpLine = archiveDownLine = -1;
            if (dialog.IsOpen)
            {
                content.text = dialog.Render(Gold, Muted);
                return;
            }
            if (page == Page.Principal)
            {
                builder.AppendLine("<size=56><b>PAUSA</b></size>").AppendLine();
                for (int i = 0; i < MainItemCount; i++) AppendItem(MainItem(i), i);
            }
            else if (page == Page.Arquivo) AppendArchive();
            else
            {
                builder.AppendLine("<size=56><b>OPÇÕES</b></size>").AppendLine();
                AppendOption("Sensibilidade do mouse      ", $"{GameOptions.MouseSensitivity:0.0}", 0);
                AppendOption("Sensibilidade do controle   ", $"{GameOptions.GamepadSensitivity:0.0}", 1);
                AppendOption("Inverter eixo vertical      ", GameOptions.InvertY ? "Sim" : "Não", 2);
                AppendOption("Idioma                      ", Localization.Loc.DisplayName(Localization.Loc.Language), 3);
                AppendItem("Voltar", 4);
            }

            if (page != Page.Arquivo) builder.AppendLine();
            builder.Append("<size=24><color=").Append(Muted).Append(">[").Append(input.ConfirmButtonLabel).Append("] Escolher     [")
                .Append(input.BackButtonLabel).Append("] Voltar</color></size>");
            content.text = builder.ToString();
        }

        void AppendArchive()
        {
            builder.Append("<size=56><b>").Append(Loc.Get("ui.arquivo.titulo", "ARQUIVO")).AppendLine("</b></size>");
            if (archiveLines.Count == 0)
            {
                builder.Append("<color=").Append(Muted).Append(">").Append(Loc.Get("ui.arquivo.vazio", "Nada no arquivo ainda.")).AppendLine("</color>");
                return;
            }

            // ▲ e ▼ ficam sempre reservados (em branco quando não há mais nada) para a lista não pular de lugar.
            ClampArchiveTop();
            archiveUpLine = TextLines.NextLine(builder);
            AppendScrollMarker(archiveTop > 0, "▲");
            int end = Mathf.Min(archiveLines.Count, archiveTop + ArchiveWindow);
            for (int i = archiveTop; i < end; i++)
            {
                ArchiveLine line = archiveLines[i];
                if (line.rowIndex < 0) builder.Append("<size=26><color=").Append(Muted).Append(">").Append(line.text).AppendLine("</color></size>");
                else AppendItem(line.text, line.rowIndex);
            }
            for (int i = end - archiveTop; i < ArchiveWindow; i++) builder.AppendLine();
            archiveDownLine = TextLines.NextLine(builder);
            AppendScrollMarker(end < archiveLines.Count, "▼");
        }

        void AppendScrollMarker(bool visible, string glyph)
        {
            if (visible) builder.Append("<size=22><color=").Append(Muted).Append(">   ").Append(glyph).AppendLine("</color></size>");
            else builder.AppendLine("<size=22> </size>");
        }

        // Opção com valor: "rótulo ‹  valor  ›". O mouse divide o clique ao meio do trecho "‹  valor  ›".
        void AppendOption(string label, string value, int index)
        {
            optionBefore[index] = (index == selected ? "<color=" + Gold + ">> " : "   ") + label;
            optionGroup[index] = $"‹  {value}  ›";
            AppendItem(label + optionGroup[index], index);
        }

        void AppendItem(string text, int index)
        {
            rowLines[index] = TextLines.NextLine(builder);
            if (index == selected) builder.Append("<color=").Append(Gold).Append(">> ").Append(text).AppendLine("</color>");
            else builder.Append("   ").AppendLine(text);
        }
    }
}
