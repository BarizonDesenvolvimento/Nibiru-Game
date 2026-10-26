using System.Collections.Generic;
using System.Text;
using Nibiru.Documents;
using Nibiru.Localization;
using Nibiru.Player;
using Nibiru.Save;
using UnityEngine;
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
                Refresh();
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
            if (next == Page.Arquivo) CollectArchiveRows();
            Refresh();
        }

        void CollectArchiveRows()
        {
            archiveRows.Clear();
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            if (catalog == null) return;
            foreach (ArchiveCatalog.Group group in catalog.groups)
                archiveRows.AddRange(catalog.UnlockedIn(group));
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
            builder.Append("<size=40><b>").Append(Loc.Get("ui.arquivo.titulo", "ARQUIVO")).AppendLine("</b></size>");
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            if (catalog == null || archiveRows.Count == 0)
            {
                builder.Append("<color=").Append(Muted).Append(">").Append(Loc.Get("ui.arquivo.vazio", "Nada no arquivo ainda.")).AppendLine("</color>");
                return;
            }

            int row = 0;
            foreach (ArchiveCatalog.Group group in catalog.groups)
            {
                List<ArchiveCatalog.Entry> unlocked = catalog.UnlockedIn(group);
                if (unlocked.Count == 0) continue;
                builder.Append("<size=20><color=").Append(Muted).Append(">").Append(group.LocalizedTitle).AppendLine("</color></size>");
                foreach (ArchiveCatalog.Entry entry in unlocked) AppendItem("<size=26>" + entry.Title + "</size>", row++);
            }
        }

        void AppendItem(string text, int index)
        {
            if (index == selected) builder.Append("<color=").Append(Gold).Append(">> ").Append(text).AppendLine("</color>");
            else builder.Append("   ").AppendLine(text);
        }
    }
}
