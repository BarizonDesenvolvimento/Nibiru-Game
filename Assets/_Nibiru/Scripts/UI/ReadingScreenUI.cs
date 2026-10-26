using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela cheia de leitura: tabuletas agora, documentos e cartas depois. Pausa o jogo
    /// enquanto está aberta e fecha com Confirmar ou Voltar.
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class ReadingScreenUI : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] GameObject panel;
        [SerializeField] Text titleLabel;
        [SerializeField] Text bodyLabel;
        [SerializeField] Text footerLabel;
        [SerializeField] Text hintLabel;

        float timeScaleBefore = 1f;
        int shownFrame = -10;

        public bool IsOpen => panel.activeSelf;

        // Textos longos (as seis fotos do Tom, as tabuletas) diminuem a letra até caber na área do texto, sem passar por
        // cima do rodapé; textos curtos continuam no tamanho de sempre.
        const int MinBodySize = 18;

        void Awake()
        {
            bodyLabel.resizeTextForBestFit = true;
            bodyLabel.resizeTextMaxSize = bodyLabel.fontSize;
            bodyLabel.resizeTextMinSize = MinBodySize;
            bodyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            panel.SetActive(false);
        }

        public void Show(string title, string body, string footer)
        {
            if (!IsOpen)
            {
                timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
                input.SetGameplayEnabled(false);
                panel.SetActive(true);
            }
            shownFrame = Time.frameCount;
            titleLabel.text = title;
            bodyLabel.text = body;
            footerLabel.text = footer;
            hintLabel.text = $"[{input.ConfirmButtonLabel}] Fechar";
        }

        void Update()
        {
            // O mesmo apertar que abriu a leitura não a fecha.
            if (!IsOpen || Time.frameCount == shownFrame) return;
            if (input.ConfirmPressed || input.BackPressed) Close();
        }

        void Close()
        {
            panel.SetActive(false);
            Time.timeScale = timeScaleBefore;
            input.SetGameplayEnabled(true);
        }
    }
}
