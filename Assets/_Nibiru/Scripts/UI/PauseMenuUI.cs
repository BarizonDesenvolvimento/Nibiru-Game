using System.Text;
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
        }

        static readonly string[] MainItems = { "Continuar", "Opções", "Voltar ao último checkpoint", "Sair do jogo" };
        const int OptionItemCount = 4;
        const float SensitivityStep = 0.1f;
        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        readonly StringBuilder builder = new StringBuilder();
        Page page;
        int selected;
        float timeScaleBefore = 1f;
        int openedFrame = -10;

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

            if (input.PausePressed)
            {
                Close();
                return;
            }
            if (input.BackPressed)
            {
                if (page == Page.Opcoes) ShowPage(Page.Principal, 1);
                else Close();
                return;
            }

            int count = page == Page.Principal ? MainItems.Length : OptionItemCount;
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
                    case 2:
                        Close();
                        Checkpoints.Restart(gameObject.scene.name);
                        break;
                    case 3: Quit(); break;
                }
                return;
            }

            if (selected == 2) AdjustOption(1);
            else if (selected == 3) ShowPage(Page.Principal, 1);
        }

        void AdjustOption(int direction)
        {
            switch (selected)
            {
                case 0: GameOptions.MouseSensitivity = Round(GameOptions.MouseSensitivity + direction * SensitivityStep); break;
                case 1: GameOptions.GamepadSensitivity = Round(GameOptions.GamepadSensitivity + direction * SensitivityStep); break;
                case 2: GameOptions.InvertY = !GameOptions.InvertY; break;
                default: return;
            }
            Refresh();
        }

        static float Round(float value) => Mathf.Round(value * 10f) / 10f;

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
                for (int i = 0; i < MainItems.Length; i++) AppendItem(MainItems[i], i);
            }
            else
            {
                builder.AppendLine("<size=56><b>OPÇÕES</b></size>").AppendLine();
                AppendItem($"Sensibilidade do mouse      <  {GameOptions.MouseSensitivity:0.0}  >", 0);
                AppendItem($"Sensibilidade do controle   <  {GameOptions.GamepadSensitivity:0.0}  >", 1);
                AppendItem($"Inverter eixo vertical      <  {(GameOptions.InvertY ? "Sim" : "Não")}  >", 2);
                AppendItem("Voltar", 3);
            }

            builder.AppendLine().Append("<size=24><color=").Append(Muted).Append(">[").Append(input.ConfirmButtonLabel).Append("] Escolher     [")
                .Append(input.BackButtonLabel).Append("] Voltar</color></size>");
            content.text = builder.ToString();
        }

        void AppendItem(string text, int index)
        {
            if (index == selected) builder.Append("<color=").Append(Gold).Append(">> ").Append(text).AppendLine("</color>");
            else builder.Append("   ").AppendLine(text);
        }
    }
}
