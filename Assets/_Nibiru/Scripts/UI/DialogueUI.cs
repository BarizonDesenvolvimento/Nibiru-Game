using System.Collections.Generic;
using System.Text;
using Nibiru.Dialogue;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Caixa de diálogo na parte de baixo da tela: nome de quem fala, o texto aparecendo
    /// letra a letra e as respostas para escolher. Confirmar mostra o texto todo de uma vez
    /// ou avança; com respostas, Navegar escolhe e Confirmar responde.
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] DialogueRunner runner;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Painel da caixa de diálogo.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text speakerLabel;
        [SerializeField] Text bodyLabel;
        [SerializeField] Text choicesLabel;
        [SerializeField] Text hintLabel;

        [Tooltip("Letras por segundo ao escrever a fala.")]
        [SerializeField, Min(1f)] float charactersPerSecond = 45f;

        const string Gold = "#E8C46A";
        const string Muted = "#B8BECB";

        readonly List<DialogueChoice> choices = new List<DialogueChoice>();
        readonly StringBuilder builder = new StringBuilder();
        string fullText = string.Empty;
        float typedCharacters;
        int selected;

        bool Typing => typedCharacters < fullText.Length;

        void Awake() => panel.SetActive(false);

        void OnEnable()
        {
            runner.NodeShown += OnNodeShown;
            runner.Ended += OnEnded;
        }

        void OnDisable()
        {
            runner.NodeShown -= OnNodeShown;
            runner.Ended -= OnEnded;
        }

        void OnNodeShown(DialogueNode node, IReadOnlyList<DialogueChoice> available)
        {
            panel.SetActive(true);
            speakerLabel.text = node.LocalizedSpeaker;
            fullText = node.LocalizedText ?? string.Empty;
            typedCharacters = 0f;
            choices.Clear();
            choices.AddRange(available);
            selected = 0;
            RefreshText();
            RefreshChoices();
        }

        void OnEnded() => panel.SetActive(false);

        void Update()
        {
            if (!runner.IsRunning) return;

            // O jogo fica pausado na conversa, então o tempo usado é o sem escala.
            if (Typing)
            {
                typedCharacters = Mathf.Min(fullText.Length, typedCharacters + charactersPerSecond * Time.unscaledDeltaTime);
                RefreshText();
                if (!Typing) RefreshChoices();
            }

            if (!Typing && choices.Count > 0)
            {
                int step = input.VerticalNavStep;
                if (step != 0)
                {
                    selected = (selected + step + choices.Count) % choices.Count;
                    RefreshChoices();
                }
            }

            // O mesmo apertar que começou a conversa não conta.
            if (!input.ConfirmPressed || Time.frameCount == runner.StartedFrame) return;

            if (Typing)
            {
                typedCharacters = fullText.Length;
                RefreshText();
                RefreshChoices();
            }
            else if (choices.Count > 0) runner.Choose(selected);
            else runner.Continue();
        }

        void RefreshText() => bodyLabel.text = fullText.Substring(0, Mathf.FloorToInt(typedCharacters));

        void RefreshChoices()
        {
            builder.Clear();
            if (!Typing)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    if (i == selected) builder.Append("<color=").Append(Gold).Append(">> ").Append(choices[i].LocalizedText).AppendLine("</color>");
                    else builder.Append("<color=").Append(Muted).Append(">   ").Append(choices[i].LocalizedText).AppendLine("</color>");
                }
            }
            choicesLabel.text = builder.ToString();

            string confirm = input.ConfirmButtonLabel;
            hintLabel.text = Typing ? string.Empty
                : choices.Count > 0 ? $"[{confirm}] Responder"
                : $"[{confirm}] Continuar";
        }
    }
}
