using Nibiru.Interaction;
using Nibiru.Localization;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Uma porta trancada que abre com um item-chave. Com a chave na bolsa, "Abrir" gasta a chave e dispara o
    /// evento de abertura; quem muda a cena é <see cref="Open"/>, chamado por um StoryEventListener desse mesmo
    /// evento (assim a porta também aparece aberta quando o jogo volta de um checkpoint). Sem a chave, avisa
    /// que está trancada. Fica no próprio objeto da porta (que precisa de um colisor).
    /// </summary>
    public class LockedDoor : MonoBehaviour, IInteractable
    {
        [Tooltip("Item que abre a porta.")]
        [SerializeField] ItemDefinition key;

        [Tooltip("Evento disparado ao abrir. Um listener dele chama Open.")]
        [SerializeField] StoryEvent raiseOnOpen;

        [SerializeField, LocKey] string openPromptKey = "ui.interact.porta_abrir";
        [SerializeField, LocKey] string lockedPromptKey = "ui.interact.porta_trancada";
        [SerializeField, LocKey] string lockedNoticeKey = "ui.interact.porta_trancada_aviso";

        PlayerInventory inventory;

        public bool IsOpen { get; private set; }

        PlayerInventory Inventory
        {
            get
            {
                if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
                return inventory;
            }
        }

        bool HasKey => key != null && Inventory != null && Inventory.Has(key);

        public string Prompt => HasKey
            ? Loc.Get(openPromptKey, "Abrir a porta")
            : Loc.Get(lockedPromptKey, "Porta trancada");

        public bool CanInteract => isActiveAndEnabled && !IsOpen;

        public void Interact(PlayerInteractor interactor)
        {
            if (IsOpen) return;
            if (!HasKey)
            {
                Notifications.Post(Loc.Get(lockedNoticeKey, "Está trancada."));
                return;
            }
            Inventory.Remove(key, 1);
            if (raiseOnOpen != null) StoryEvents.Raise(raiseOnOpen);
            else Open();
        }

        /// <summary>Abre a porta: some o desenho e o colisor. Pode ser chamado mais de uma vez.</summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
    }
}
