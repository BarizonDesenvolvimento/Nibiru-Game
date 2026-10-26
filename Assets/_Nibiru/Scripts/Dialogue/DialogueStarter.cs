using Nibiru.Interaction;
using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Dialogue
{
    /// <summary>
    /// Começa uma conversa. Num personagem, a Elena fala com ele pelo botão Interagir.
    /// Também pode ser chamado por um evento (por exemplo, um StoryEventListener que chama
    /// StartDialogue quando a Elena entra numa área).
    /// </summary>
    public class DialogueStarter : MonoBehaviour, IInteractable
    {
        [SerializeField] DialogueDefinition dialogue;

        [Tooltip("Quem conduz as conversas na cena. Vazio = procura sozinho.")]
        [SerializeField] DialogueRunner runner;

        [Tooltip("Permite começar a conversa com o botão Interagir.")]
        [SerializeField] bool interactable = true;

        [Tooltip("Texto do aviso, por exemplo \"Falar com Samir\".")]
        [SerializeField] string prompt = "Falar";

        [Tooltip("Chave do texto do aviso na tabela de idiomas (ui.interact...). Vazio = usa o texto acima.")]
        [SerializeField, LocKey] string promptKey;

        [Tooltip("A conversa acontece uma vez só por partida.")]
        [SerializeField] bool onlyOnce;

        [Header("Conversa alternativa (opcional)")]
        [Tooltip("Vale no lugar da conversa normal enquanto a Elena tiver o item abaixo e nenhum dos eventos abaixo tiver acontecido.")]
        [SerializeField] DialogueDefinition alternateDialogue;
        [SerializeField] ItemDefinition alternateItem;
        [SerializeField, Min(1)] int alternateItemCount = 1;
        [SerializeField] StoryEvent[] alternateUnlessRaised = new StoryEvent[0];

        PlayerInventory inventory;

        public string Prompt => Loc.Get(promptKey, prompt);
        public bool CanInteract => interactable && isActiveAndEnabled && CanPlay;

        // A conversa que vale agora: a alternativa, se as condições batem, ou a normal.
        DialogueDefinition Chosen => UseAlternate ? alternateDialogue : dialogue;

        bool UseAlternate
        {
            get
            {
                if (alternateDialogue == null || alternateItem == null) return false;
                foreach (StoryEvent blocker in alternateUnlessRaised)
                    if (StoryEvents.HasBeenRaised(blocker)) return false;
                if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
                return inventory != null && inventory.Has(alternateItem, alternateItemCount);
            }
        }

        bool CanPlay
        {
            get
            {
                DialogueDefinition chosen = Chosen;
                return chosen != null && !(onlyOnce && DialogueRunner.HasSeen(chosen));
            }
        }

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
        }

        public void Interact(PlayerInteractor interactor) => StartDialogue();

        /// <summary>Troca a conversa (a fala de um personagem muda conforme a história avança).</summary>
        public void SetDialogue(DialogueDefinition newDialogue) => dialogue = newDialogue;

        public void StartDialogue()
        {
            if (CanPlay) runner.Play(Chosen);
        }
    }
}
