using System;
using Nibiru.Inventory;
using Nibiru.Localization;
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
        [Tooltip("Código permanente do passo (s1, s2...). Nunca muda nem se repete na missão: a chave de tradução sai dele, então acrescentar ou tirar passos não mexe nos outros.")]
        public string id;

        [Tooltip("Chave do texto do objetivo na tabela de idiomas (quest.<nome>.<passo>).")]
        public string objectiveKey;

        [Tooltip("Texto do objetivo mostrado na tela. Depois da extração fica vazio: o texto mora na tabela de idiomas.")]
        [TextArea(1, 3)] public string objective;

        /// <summary>O objetivo no idioma atual.</summary>
        public string LocalizedObjective => Loc.Get(objectiveKey, objective);

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
        [Tooltip("Chave do nome da missão na tabela de idiomas (quest.<nome>.name).")]
        public string nameKey;

        [Tooltip("Nome da missão mostrado na tela. Depois da extração fica vazio.")]
        public string displayName;

        public string LocalizedName => Loc.Get(nameKey, displayName);

        public QuestKind kind = QuestKind.Principal;

        [Tooltip("Evento que começa a missão. Vazio = começa quando a cena carrega (se estiver na lista do QuestManager).")]
        public StoryEvent startOn;

        [Tooltip("Passos em ordem.")]
        public QuestStep[] steps = new QuestStep[0];

        [Tooltip("Evento disparado quando a missão termina (opcional). Serve para ligar a próxima coisa.")]
        public StoryEvent raiseOnComplete;

        [Tooltip("Evento que faz a missão falhar, se ela já começou e ainda não terminou (opcional). Uma missão que falhou sai da lista de objetivos.")]
        public StoryEvent failOn;

        [Tooltip("Mudanças de reputação quando a missão termina (opcional).")]
        public Factions.ReputationChange[] reputationOnComplete = new Factions.ReputationChange[0];

        public string Id => name;
    }
}
