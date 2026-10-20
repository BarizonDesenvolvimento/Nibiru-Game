using System;
using Nibiru.Inventory;
using UnityEngine;

namespace Nibiru.Quests
{
    public enum QuestKind
    {
        Principal,
        Secundaria,
    }

    /// <summary>
    /// Um passo de missão: o texto do objetivo e o que o completa.
    /// Completa quando o evento acontece OU quando a Elena tem o item na quantidade pedida
    /// (o que estiver preenchido).
    /// </summary>
    [Serializable]
    public class QuestStep
    {
        [Tooltip("Texto do objetivo mostrado na tela.")]
        [TextArea(1, 3)] public string objective;

        [Tooltip("Evento que completa este passo (opcional).")]
        public StoryEvent completeOn;

        [Tooltip("Item que completa este passo ao estar no inventário (opcional).")]
        public ItemDefinition requiredItem;

        [Tooltip("Quantidade do item necessária.")]
        [Min(1)] public int requiredCount = 1;
    }

    /// <summary>
    /// Uma missão (principal ou secundária) definida como arquivo em Data/Quests.
    /// Criar ou mudar uma missão não exige mexer em código.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Missões/Missão", fileName = "NovaMissao")]
    public class QuestDefinition : ScriptableObject
    {
        [Tooltip("Nome da missão mostrado na tela.")]
        public string displayName;

        public QuestKind kind = QuestKind.Principal;

        [Tooltip("Evento que começa a missão. Vazio = começa quando a cena carrega (se estiver na lista do QuestManager).")]
        public StoryEvent startOn;

        [Tooltip("Passos em ordem.")]
        public QuestStep[] steps = new QuestStep[0];

        [Tooltip("Evento disparado quando a missão termina (opcional). Serve para ligar a próxima coisa.")]
        public StoryEvent raiseOnComplete;

        public string Id => name;
    }
}
