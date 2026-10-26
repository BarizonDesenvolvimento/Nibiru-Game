using System;
using System.Collections.Generic;
using Nibiru.Inventory;
using Nibiru.Save;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Acompanha as missões da região: começa as missões, avança os passos quando os eventos
    /// acontecem ou quando a Elena pega os itens pedidos, e avisa a tela. O progresso fica no
    /// WorldState, então volta certo de um checkpoint.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Tooltip("Todas as missões desta região, inclusive as que começam por evento.")]
        [SerializeField] QuestDefinition[] quests = new QuestDefinition[0];

        [Tooltip("Inventário da Elena, para os passos que pedem itens.")]
        [SerializeField] PlayerInventory inventory;

        const int NotStarted = -1;

        bool inventoryCheckPending;

        /// <summary>Avisa a tela quando algum objetivo muda.</summary>
        public event Action Changed;

        void OnEnable()
        {
            StoryEvents.Raised += OnStoryEvent;
            if (inventory != null) inventory.Changed += OnInventoryChanged;
        }

        void OnDisable()
        {
            StoryEvents.Raised -= OnStoryEvent;
            if (inventory != null) inventory.Changed -= OnInventoryChanged;
        }

        // Confere no quadro seguinte, para o aviso do item pego aparecer antes do aviso da missão.
        void OnInventoryChanged() => inventoryCheckPending = true;

        void Update()
        {
            if (!inventoryCheckPending) return;
            inventoryCheckPending = false;
            CheckAllQuests();
        }

        // No Start (e não no Awake) para o inventário já ter sido restaurado do checkpoint.
        void Start()
        {
            foreach (QuestDefinition quest in quests)
            {
                bool startsNow = quest.startOn == null || StoryEvents.HasBeenRaised(quest.startOn);
                if (startsNow && StepOf(quest) == NotStarted) StartQuest(quest, announce: quest.startOn != null);
            }
            // Um evento de falha que aconteceu antes de este gerente existir (jogo carregado de depois) também vale.
            foreach (QuestDefinition quest in quests)
                if (quest.failOn != null && StoryEvents.HasBeenRaised(quest.failOn)) FailQuest(quest);
            CheckAllQuests();
            Changed?.Invoke();
        }

        /// <summary>Objetivos atuais, principais primeiro.</summary>
        public IEnumerable<(QuestDefinition quest, QuestStep step)> ActiveObjectives()
        {
            foreach (QuestKind kind in new[] { QuestKind.Principal, QuestKind.Secundaria })
            {
                foreach (QuestDefinition quest in quests)
                {
                    if (quest.kind != kind || IsFailed(quest)) continue;
                    int step = StepOf(quest);
                    if (step >= 0 && step < quest.steps.Length) yield return (quest, quest.steps[step]);
                }
            }
        }

        public bool IsCompleted(QuestDefinition quest) => !IsFailed(quest) && StepOf(quest) >= quest.steps.Length;

        public bool IsFailed(QuestDefinition quest) => WorldState.IsQuestFailed(quest.Id);

        /// <summary>
        /// Faz a missão falhar, se ela já começou e ainda não terminou. Uma missão que falhou sai dos
        /// objetivos e não avança mais. Fica no WorldState, então volta certo de um checkpoint.
        /// </summary>
        public void FailQuest(QuestDefinition quest)
        {
            int step = StepOf(quest);
            if (IsFailed(quest) || step == NotStarted || step >= quest.steps.Length) return;
            WorldState.MarkQuestFailed(quest.Id);
            Notifications.PostImportant($"Missão falhou: {quest.displayName}");
            Changed?.Invoke();
        }

        public void StartQuest(QuestDefinition quest, bool announce = true)
        {
            if (StepOf(quest) != NotStarted) return;
            WorldState.SetQuestStep(quest.Id, 0);
            if (announce) Notifications.Post($"Nova missão: {quest.displayName}");
            CheckQuest(quest);
            Changed?.Invoke();
        }

        void OnStoryEvent(StoryEvent storyEvent)
        {
            foreach (QuestDefinition quest in quests)
            {
                if (quest.failOn != null && quest.failOn == storyEvent) FailQuest(quest);
                else if (quest.startOn == storyEvent) StartQuest(quest);
                else CheckQuest(quest);
            }
        }

        void CheckAllQuests()
        {
            foreach (QuestDefinition quest in quests) CheckQuest(quest);
        }

        // Avança enquanto o passo atual já estiver cumprido (vários passos podem se completar de uma vez).
        void CheckQuest(QuestDefinition quest)
        {
            if (IsFailed(quest)) return;
            int step = StepOf(quest);
            bool advanced = false;
            while (step >= 0 && step < quest.steps.Length && IsDone(quest.steps[step]))
            {
                step++;
                WorldState.SetQuestStep(quest.Id, step);
                advanced = true;
            }
            if (!advanced) return;

            if (step >= quest.steps.Length)
            {
                Notifications.Post($"Missão concluída: {quest.displayName}");
                StoryEvents.Raise(quest.raiseOnComplete);
                Factions.Reputation.Apply(quest.reputationOnComplete);
            }
            else
            {
                Notifications.Post($"Objetivo: {quest.steps[step].objective}");
            }
            Changed?.Invoke();
        }

        bool IsDone(QuestStep step)
        {
            if (step.completeOn != null && StoryEvents.HasBeenRaised(step.completeOn)) return true;
            if (step.requiredItem != null && inventory != null && inventory.CountOf(step.requiredItem) >= step.requiredCount) return true;
            return false;
        }

        static int StepOf(QuestDefinition quest) =>
            WorldState.TryGetQuestStep(quest.Id, out int step) ? step : NotStarted;
    }
}
