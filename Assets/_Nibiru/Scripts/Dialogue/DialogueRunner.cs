using System;
using System.Collections.Generic;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Dialogue
{
    /// <summary>
    /// Conduz uma conversa: mostra as falas em ordem, filtra as respostas disponíveis e
    /// dispara os eventos. Enquanto a conversa dura, o jogo pausa e a Elena fica parada.
    /// A caixa de texto (DialogueUI) só mostra o que este componente manda.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;

        DialogueDefinition current;
        DialogueNode node;
        readonly List<DialogueChoice> visibleChoices = new List<DialogueChoice>();
        float timeScaleBefore = 1f;
        int endedFrame = -10;

        /// <summary>Uma fala nova apareceu (com as respostas disponíveis, se houver).</summary>
        public event Action<DialogueNode, IReadOnlyList<DialogueChoice>> NodeShown;
        public event Action Ended;

        public bool IsRunning => current != null;

        /// <summary>Quadro em que a conversa começou, para o mesmo apertar de botão não pular a primeira fala.</summary>
        public int StartedFrame { get; private set; } = -10;

        public bool Play(DialogueDefinition dialogue)
        {
            // O botão que encerrou uma conversa não pode começar outra no mesmo instante.
            if (IsRunning || dialogue == null || dialogue.First == null) return false;
            if (!input.GameplayEnabled || Time.frameCount - endedFrame < 2) return false;

            current = dialogue;
            StartedFrame = Time.frameCount;
            input.SetGameplayEnabled(false);
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            Show(dialogue.First);
            return true;
        }

        /// <summary>Avança para a próxima fala (quando não há respostas para escolher).</summary>
        public void Continue()
        {
            if (!IsRunning || visibleChoices.Count > 0) return;
            GoTo(node.next);
        }

        public void Choose(int index)
        {
            if (!IsRunning || index < 0 || index >= visibleChoices.Count) return;
            DialogueChoice choice = visibleChoices[index];
            StoryEvents.Raise(choice.raise);
            GoTo(choice.next);
        }

        /// <summary>True se esta conversa já foi vista nesta partida.</summary>
        public static bool HasSeen(DialogueDefinition dialogue) =>
            dialogue != null && WorldState.HasStoryEvent(SeenKey(dialogue));

        static string SeenKey(DialogueDefinition dialogue) => "Dialogo:" + dialogue.Id;

        void Show(DialogueNode next)
        {
            node = next;
            StoryEvents.Raise(next.raise);
            visibleChoices.Clear();
            foreach (DialogueChoice choice in next.choices)
                if (choice.requires == null || StoryEvents.HasBeenRaised(choice.requires))
                    visibleChoices.Add(choice);
            NodeShown?.Invoke(next, visibleChoices);
        }

        void GoTo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                End();
                return;
            }
            DialogueNode next = current.Find(id);
            if (next == null)
            {
                Debug.LogWarning($"Diálogo {current.name}: fala \"{id}\" não existe; encerrando a conversa.", current);
                End();
                return;
            }
            Show(next);
        }

        void End()
        {
            DialogueDefinition finished = current;
            current = null;
            node = null;
            visibleChoices.Clear();
            endedFrame = Time.frameCount;
            Time.timeScale = timeScaleBefore;
            input.SetGameplayEnabled(true);

            WorldState.MarkStoryEvent(SeenKey(finished));
            StoryEvents.Raise(finished.raiseOnEnd);
            Ended?.Invoke();
        }
    }
}
