using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela cheia de leitura: tabuletas agora, documentos e cartas depois. Pausa o jogo
    /// enquanto está aberta e fecha com Confirmar ou Voltar, ou com o clique do mouse (esquerdo ou direito).
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
        int enableGameplayFrame = -1;

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

        // Quando a leitura foi aberta por dentro de outro menu (o Arquivo, no menu de pausa): ao fechar, devolve a
        // vez a ele em vez de soltar o controle da Elena e o tempo do jogo.
        System.Action onClosed;

        public void Show(string title, string body, string footer) => Show(title, body, footer, null);

        public void Show(string title, string body, string footer, System.Action closedCallback)
        {
            if (!IsOpen)
            {
                timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
                input.SetGameplayEnabled(false);
                enableGameplayFrame = -1;
                CursorPolicy.RequestFree(this, false);
                panel.SetActive(true);
            }
            onClosed = closedCallback;
            shownFrame = Time.frameCount;
            titleLabel.text = title;
            bodyLabel.text = body;
            footerLabel.text = footer;
            hintLabel.text = $"[{input.ConfirmButtonLabel}] Fechar";
        }

        void OnDestroy() => CursorPolicy.Release(this);

        void Update()
        {
            // Fechada com o clique do mouse, a leitura devolve o jogo no quadro seguinte (o mesmo clique não atira nem ataca).
            if (enableGameplayFrame >= 0 && Time.frameCount > enableGameplayFrame)
            {
                enableGameplayFrame = -1;
                if (!IsOpen) input.SetGameplayEnabled(true);
            }
            // O mesmo apertar que abriu a leitura não a fecha.
            if (!IsOpen || Time.frameCount == shownFrame) return;
            if (input.ConfirmPressed || input.BackPressed)
            {
                MenuPointer.NoteKeyboardOrPad();
                CloseScreen(false);
                return;
            }
            PointerStep(MenuPointer.Read());
        }

        /// <summary>Um quadro do mouse: o clique esquerdo ou o direito fecham a leitura (também usado pelos testes).</summary>
        public void PointerStep(MenuPointer.State pointer)
        {
            if (!IsOpen || Time.frameCount == shownFrame) return;
            if (pointer.leftClick || pointer.rightClick) CloseScreen(true);
        }

        void Close() => CloseScreen(false);

        void CloseScreen(bool byPointer)
        {
            CursorPolicy.Release(this);
            panel.SetActive(false);
            System.Action callback = onClosed;
            onClosed = null;
            if (callback != null)
            {
                // Quem abriu continua com o jogo parado e o controle da Elena desligado.
                callback();
                return;
            }
            Time.timeScale = timeScaleBefore;
            if (byPointer) enableGameplayFrame = Time.frameCount;
            else input.SetGameplayEnabled(true);
        }
    }
}
