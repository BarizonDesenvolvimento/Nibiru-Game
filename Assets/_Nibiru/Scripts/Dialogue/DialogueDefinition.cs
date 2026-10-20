using System;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Dialogue
{
    /// <summary>Uma resposta que a Elena pode escolher.</summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Texto da resposta.")]
        [TextArea(1, 2)] public string text;

        [Tooltip("Código da fala que vem depois. Vazio = encerra a conversa.")]
        public string next;

        [Tooltip("A resposta só aparece se este evento já aconteceu (opcional).")]
        public StoryEvent requires;

        [Tooltip("Evento disparado ao escolher esta resposta (opcional).")]
        public StoryEvent raise;
    }

    /// <summary>Uma fala da conversa.</summary>
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("Código curto desta fala, usado pelas outras para apontar para ela (ex.: inicio, samir2).")]
        public string id;

        [Tooltip("Quem fala (Samir, Elena, Kast pelo rádio, Voz...).")]
        public string speaker;

        [TextArea(2, 5)] public string text;

        [Tooltip("Código da próxima fala, se não houver respostas. Vazio = encerra a conversa.")]
        public string next;

        [Tooltip("Evento disparado quando esta fala aparece (opcional).")]
        public StoryEvent raise;

        [Tooltip("Respostas da Elena. Se houver, a conversa espera a escolha.")]
        public DialogueChoice[] choices = new DialogueChoice[0];
    }

    /// <summary>
    /// Uma conversa, como arquivo em Data/Dialogues. A primeira fala da lista é o começo.
    /// Escrever ou mudar diálogos não exige mexer em código.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Diálogos/Diálogo", fileName = "NovoDialogo")]
    public class DialogueDefinition : ScriptableObject
    {
        public DialogueNode[] nodes = new DialogueNode[0];

        [Tooltip("Evento disparado quando a conversa termina (opcional).")]
        public StoryEvent raiseOnEnd;

        public string Id => name;
        public DialogueNode First => nodes.Length > 0 ? nodes[0] : null;

        public DialogueNode Find(string id)
        {
            foreach (DialogueNode node in nodes)
                if (node.id == id) return node;
            return null;
        }

        // Avisa no Console se alguma fala aponta para um código que não existe.
        void OnValidate()
        {
            foreach (DialogueNode node in nodes)
            {
                CheckLink(node.id, node.next);
                foreach (DialogueChoice choice in node.choices) CheckLink(node.id, choice.next);
            }
        }

        void CheckLink(string from, string to)
        {
            if (!string.IsNullOrEmpty(to) && Find(to) == null)
                Debug.LogWarning($"Diálogo {name}: a fala \"{from}\" aponta para \"{to}\", que não existe.", this);
        }
    }
}
