using Nibiru.Dialogue;
using Nibiru.Interaction;
using Nibiru.Localization;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Uma troca com um personagem: a Elena entrega um item (curativos, antibióticos) e, se for o
    /// caso, recebe outro. Sem o item, o mesmo personagem só conversa. A troca acontece uma vez só;
    /// depois dela, o personagem repete uma fala curta. Tudo o que muda na história sai por eventos.
    ///
    /// Regra do presente: o item recebido só é dado se a Elena ainda não o tem e se o evento
    /// "não dar se já aconteceu" ainda não aconteceu (por exemplo, a Weiss só dá os antibióticos a quem
    /// ainda não os conseguiu nem entregou). A troca acontece do mesmo jeito, só sem o presente.
    /// </summary>
    public class ItemTrade : MonoBehaviour, IInteractable
    {
        [Header("Troca")]
        [Tooltip("Aviso quando a Elena tem o item a entregar, por exemplo \"Dar os curativos à Dra. Weiss\".")]
        [SerializeField] string tradePrompt = "Entregar";
        [Tooltip("Chave do aviso na tabela de idiomas (ui.interact...). Vazio = usa o texto acima.")]
        [SerializeField, LocKey] string tradePromptKey;
        [SerializeField] ItemDefinition takeItem;
        [SerializeField, Min(1)] int takeCount = 1;

        [Header("Presente (opcional)")]
        [SerializeField] ItemDefinition giveItem;
        [SerializeField, Min(1)] int giveCount = 1;
        [Tooltip("Não dá o presente se este evento já aconteceu (além de já ter o item).")]
        [SerializeField] StoryEvent skipGiveIfRaised;

        [Header("Eventos")]
        [Tooltip("Disparado quando a troca acontece, com ou sem presente. Também marca que a troca já foi feita.")]
        [SerializeField] StoryEvent raiseOnTrade;
        [Tooltip("Disparado só quando o presente foi dado.")]
        [SerializeField] StoryEvent raiseOnGift;
        [Tooltip("A troca não pode mais acontecer depois deste evento (opcional).")]
        [SerializeField] StoryEvent blockedIfRaised;

        [Header("Conversas")]
        [Tooltip("Conversa da troca com presente.")]
        [SerializeField] DialogueDefinition dialogueWithGift;
        [Tooltip("Conversa da troca sem presente (ou quando não há presente).")]
        [SerializeField] DialogueDefinition dialogueWithoutGift;
        [Tooltip("Sem o item a entregar, o personagem diz isto (repetível).")]
        [SerializeField] DialogueDefinition waitingDialogue;
        [SerializeField] string talkPrompt = "Falar";
        [SerializeField, LocKey] string talkPromptKey;
        [Tooltip("Depois da troca, o personagem diz isto (repetível).")]
        [SerializeField] DialogueDefinition afterDialogue;

        [SerializeField] DialogueRunner runner;

        PlayerInventory inventory;
        bool tradedLocally;

        /// <summary>A troca já foi feita nesta partida.</summary>
        public bool Done => raiseOnTrade != null ? StoryEvents.HasBeenRaised(raiseOnTrade) : tradedLocally;

        bool Blocked => blockedIfRaised != null && StoryEvents.HasBeenRaised(blockedIfRaised);

        bool CanTrade => !Done && !Blocked && Inventory != null && Inventory.Has(takeItem, takeCount);

        PlayerInventory Inventory
        {
            get
            {
                if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
                return inventory;
            }
        }

        public string Prompt => CanTrade ? Loc.Get(tradePromptKey, tradePrompt) : Loc.Get(talkPromptKey, talkPrompt);

        public bool CanInteract
        {
            get
            {
                if (!isActiveAndEnabled) return false;
                if (CanTrade) return true;
                if (Done) return afterDialogue != null;
                return !Blocked && waitingDialogue != null;
            }
        }

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (CanTrade) Trade();
            else if (Done) runner.Play(afterDialogue);
            else runner.Play(waitingDialogue);
        }

        void Trade()
        {
            PlayerInventory bag = Inventory;
            bag.Remove(takeItem, takeCount);

            bool gift = giveItem != null && !bag.Has(giveItem) && !StoryEvents.HasBeenRaised(skipGiveIfRaised);
            if (gift && bag.Add(giveItem, giveCount) == 0)
            {
                // Sem espaço: desfaz a entrega e deixa a troca para depois.
                bag.Add(takeItem, takeCount);
                Notifications.Post("Inventário cheio");
                return;
            }

            tradedLocally = true;
            if (gift) Notifications.Post(giveItem.LocalizedName);
            StoryEvents.Raise(raiseOnTrade);
            if (gift) StoryEvents.Raise(raiseOnGift);
            runner.Play(gift ? dialogueWithGift : dialogueWithoutGift != null ? dialogueWithoutGift : dialogueWithGift);
        }
    }
}
