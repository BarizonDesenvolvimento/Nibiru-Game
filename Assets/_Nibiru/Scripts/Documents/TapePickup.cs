using Nibiru.Interaction;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Uma fita no cenário. "Pegar a fita" a recolhe (registrando no evento de coleta da fita) e
    /// começa a tocar, sem menu e sem travar o controle. Some depois de pega, inclusive ao voltar
    /// de um ponto de retorno guardado depois da coleta.
    /// </summary>
    public class TapePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] TapeDefinition tape;

        [Tooltip("Quem toca a fita. Vazio = procura sozinho.")]
        [SerializeField] TapePlayer player;

        [Tooltip("Texto do aviso, por exemplo \"Pegar a fita\".")]
        [SerializeField] string prompt = "Pegar a fita";

        public string Prompt => prompt;
        public bool CanInteract => isActiveAndEnabled && tape != null && !tape.IsCollected;

        void Start()
        {
            if (tape != null && tape.IsCollected) gameObject.SetActive(false);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract) return;
            if (player == null) player = FindFirstObjectByType<TapePlayer>();
            Notifications.Post(tape.LocalizedName);
            StoryEvents.Raise(tape.raiseOnCollected);
            gameObject.SetActive(false);
            if (player != null) player.Play(tape);
            else Debug.LogWarning("TapePickup: não há TapePlayer na cena.", this);
        }
    }
}
