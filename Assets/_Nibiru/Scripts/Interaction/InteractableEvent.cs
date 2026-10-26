using Nibiru.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Interaction
{
    /// <summary>
    /// Objeto interativo genérico: ao interagir, dispara o evento configurado no Inspector.
    /// Serve para coisas simples (sumir, tocar um som, abrir algo) sem escrever código.
    /// Itens, portas e tabuletas terão componentes próprios.
    /// </summary>
    public class InteractableEvent : MonoBehaviour, IInteractable
    {
        [Tooltip("Verbo mostrado na tela, por exemplo \"Pegar\", \"Abrir\", \"Ler\".")]
        [SerializeField] string prompt = "Usar";

        [Tooltip("Chave do verbo na tabela de textos. Vazio, usa o texto acima.")]
        [SerializeField, LocKey] string promptKey;

        [Tooltip("Se marcado, só funciona uma vez.")]
        [SerializeField] bool singleUse;

        [Tooltip("O que acontece ao interagir.")]
        [SerializeField] UnityEvent onInteract = new UnityEvent();

        bool used;

        public string Prompt => Loc.Get(promptKey, prompt);
        public bool CanInteract => isActiveAndEnabled && !(singleUse && used);

        public void Interact(PlayerInteractor interactor)
        {
            used = true;
            onInteract.Invoke();
        }
    }
}
