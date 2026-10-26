using System;
using System.Text;
using Nibiru.Localization;

namespace Nibiru.UI
{
    /// <summary>
    /// Confirmação de ação destrutiva (sobrescrever, apagar, sair para o menu), reutilizada pelo menu principal e pela
    /// pausa. Não desenha nada sozinha: o menu dono pergunta <see cref="Render"/> e põe o texto na tela, e repassa os
    /// botões em <see cref="Handle"/>. O foco começa sempre em "Cancelar"; o botão de voltar (ou Esc) cancela.
    /// </summary>
    public class ConfirmDialog
    {
        string title;
        string message;
        string confirmLabel;
        Action onConfirm;
        Action onCancel;

        public bool IsOpen { get; private set; }

        /// <summary>True enquanto o foco está em "Cancelar".</summary>
        public bool FocusOnCancel { get; private set; } = true;

        public void Open(string title, string message, string confirmLabel, Action onConfirm, Action onCancel = null)
        {
            this.title = title;
            this.message = message;
            this.confirmLabel = confirmLabel;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            FocusOnCancel = true;
            IsOpen = true;
        }

        /// <summary>
        /// Repassa um quadro de entrada. Devolve true se o diálogo estava aberto (ele consome tudo): vertical é o passo das
        /// setas (-1, 0, 1), confirm o botão de escolher e back o de voltar.
        /// </summary>
        public bool Handle(int vertical, bool confirm, bool back)
        {
            if (!IsOpen) return false;
            if (back)
            {
                Cancel();
                return true;
            }
            if (vertical != 0) FocusOnCancel = !FocusOnCancel;
            if (confirm)
            {
                if (FocusOnCancel) Cancel();
                else Confirm();
            }
            return true;
        }

        public void Cancel()
        {
            if (!IsOpen) return;
            IsOpen = false;
            onCancel?.Invoke();
        }

        public void Confirm()
        {
            if (!IsOpen) return;
            IsOpen = false;
            onConfirm?.Invoke();
        }

        /// <summary>O texto do diálogo (com as cores do menu), para pôr no lugar da lista.</summary>
        public string Render(string gold, string muted)
        {
            var text = new StringBuilder();
            text.Append("<size=52><b>").Append(title).AppendLine("</b></size>").AppendLine();
            text.AppendLine(message).AppendLine();
            AppendOption(text, Loc.Get("ui.confirmar.cancelar", "Cancelar"), FocusOnCancel, gold, muted);
            AppendOption(text, confirmLabel, !FocusOnCancel, gold, muted);
            return text.ToString();
        }

        static void AppendOption(StringBuilder text, string label, bool focused, string gold, string muted)
        {
            if (focused) text.Append("<color=").Append(gold).Append(">›  ").Append(label).AppendLine("</color>");
            else text.Append("<color=").Append(muted).Append(">   ").Append(label).AppendLine("</color>");
        }
    }
}
