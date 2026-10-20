namespace Nibiru.Interaction
{
    /// <summary>
    /// Qualquer coisa com que o jogador pode interagir: item, porta, tabuleta, alavanca.
    /// O objeto precisa ter um Collider (pode ser trigger) para ser encontrado.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Verbo mostrado na tela, por exemplo "Pegar", "Abrir", "Ler".</summary>
        string Prompt { get; }

        /// <summary>False quando o objeto existe mas não aceita interação agora (já usado, trancado...).</summary>
        bool CanInteract { get; }

        void Interact(PlayerInteractor interactor);
    }
}
