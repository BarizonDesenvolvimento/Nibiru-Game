using Nibiru.Interaction;
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

        [Tooltip("A conversa acontece uma vez só por partida.")]
        [SerializeField] bool onlyOnce;

        public string Prompt => prompt;
        public bool CanInteract => interactable && isActiveAndEnabled && CanPlay;

        bool CanPlay => dialogue != null && !(onlyOnce && DialogueRunner.HasSeen(dialogue));

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
        }

        public void Interact(PlayerInteractor interactor) => StartDialogue();

        public void StartDialogue()
        {
            if (CanPlay) runner.Play(dialogue);
        }
    }
}
