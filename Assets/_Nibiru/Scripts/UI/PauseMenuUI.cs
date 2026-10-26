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
        const int MainItemCount = 5;

        static string MainItem(int index)
        {
            switch (index)
            {
                case 0: return "Continuar";
                case 1: return "Opções";
                case ArchiveItem: return Loc.Get("ui.arquivo.item_menu", "Arquivo");
                case 3: return "Voltar ao último checkpoint";
                default: return "Sair do jogo";
            }
        }
        const int OptionItemCount = 5;
        const float SensitivityStep = 0.1f;
        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        readonly StringBuilder builder = new StringBuilder();
        Page page;
        int selected;
        float timeScaleBefore = 1f;
        int openedFrame = -10;

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

        void Awake() => panel.SetActive(false);

        void Update()
        {
            if (!IsOpen)
            {
                // O mesmo Esc que fechou outro menu neste quadro não abre a pausa.
                if (input.PausePressed && input.GameplayEnabled && Time.frameCount != input.GameplayToggledFrame) Open();
                return;
            }
            if (Time.frameCount == openedFrame) return;
            // Com uma entrada do Arquivo aberta na tela de leitura, a leitura é quem recebe os botões.
            if (openReading != null && openReading.IsOpen) return;

            if (input.PausePressed)
            {
                Close();
                return;
            }
            if (input.BackPressed)
            {
                if (page == Page.Opcoes) ShowPage(Page.Principal, 1);
                else if (page == Page.Arquivo) ShowPage(Page.Principal, ArchiveItem);
                else Close();
                return;
            }

            int count = page == Page.Principal ? MainItemCount : page == Page.Arquivo ? Mathf.Max(1, archiveRows.Count) : OptionItemCount;
            int vertical = input.VerticalNavStep;
            if (vertical != 0)
            {
                selected = (selected + vertical + count) % count;
                if (page == Page.Arquivo) KeepArchiveSelectionVisible();
                Refresh();
            }

            if (page == Page.Arquivo && Mouse.current != null)
            {
                float wheel = Mouse.current.scroll.ReadValue().y;
                if (wheel != 0f) ScrollArchive(wheel > 0f ? -1 : 1);
            }

            if (page == Page.Opcoes)
            {
                int horizontal = input.HorizontalNavStep;
                if (horizontal != 0) AdjustOption(horizontal);
            }

            if (input.ConfirmPressed) Activate();
        }

        void Open()
        {
            panel.SetActive(true);
            openedFrame = Time.frameCount;
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            input.SetGameplayEnabled(false);
            ShowPage(Page.Principal, 0);
        }

        void Close()
        {
            GameOptions.Save();
            panel.SetActive(false);
            Time.timeScale = timeScaleBefore;
            input.SetGameplayEnabled(true);
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
                    case 4: Quit(); break;
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
            if (page == Page.Principal)
            {
                builder.AppendLine("<size=56><b>PAUSA</b></size>").AppendLine();
                for (int i = 0; i < MainItemCount; i++) AppendItem(MainItem(i), i);
            }
            else if (page == Page.Arquivo) AppendArchive();
            else
            {
                builder.AppendLine("<size=56><b>OPÇÕES</b></size>").AppendLine();
                AppendItem($"Sensibilidade do mouse      <  {GameOptions.MouseSensitivity:0.0}  >", 0);
                AppendItem($"Sensibilidade do controle   <  {GameOptions.GamepadSensitivity:0.0}  >", 1);
                AppendItem($"Inverter eixo vertical      <  {(GameOptions.InvertY ? "Sim" : "Não")}  >", 2);
                AppendItem($"Idioma                     <  {Localization.Loc.DisplayName(Localization.Loc.Language)}  >", 3);
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
            AppendScrollMarker(archiveTop > 0, "▲");
            int end = Mathf.Min(archiveLines.Count, archiveTop + ArchiveWindow);
            for (int i = archiveTop; i < end; i++)
            {
                ArchiveLine line = archiveLines[i];
                if (line.rowIndex < 0) builder.Append("<size=26><color=").Append(Muted).Append(">").Append(line.text).AppendLine("</color></size>");
                else AppendItem(line.text, line.rowIndex);
            }
            for (int i = end - archiveTop; i < ArchiveWindow; i++) builder.AppendLine();
            AppendScrollMarker(end < archiveLines.Count, "▼");
        }

        void AppendScrollMarker(bool visible, string glyph)
        {
            if (visible) builder.Append("<size=22><color=").Append(Muted).Append(">   ").Append(glyph).AppendLine("</color></size>");
            else builder.AppendLine("<size=22> </size>");
        }

        void AppendItem(string text, int index)
        {
            if (index == selected) builder.Append("<color=").Append(Gold).Append(">> ").Append(text).AppendLine("</color>");
            else builder.Append("   ").AppendLine(text);
        }
    }
}
