using Nibiru.Interaction;
using Nibiru.Save;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Item no cenário que a Elena pode pegar. Se não couber tudo, pega o que der
    /// e o resto fica no chão, esperando espaço. Lembra o que já foi pego ao voltar de um checkpoint.
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition item;
        [SerializeField, Min(1)] int amount = 1;

        [Tooltip("Evento da história disparado quando o item é pego por inteiro (opcional).")]
        [SerializeField] Quests.StoryEvent raiseOnPickup;

        PersistentId persistentId;

        void Awake()
        {
            persistentId = GetComponent<PersistentId>();
            if (WorldState.TryGetPickupRemaining(persistentId.Id, out int remaining))
            {
                amount = remaining;
                if (amount == 0) gameObject.SetActive(false);
            }
        }

        public string Prompt
        {
            get
            {
                if (item == null) return "Pegar";
                return amount > 1 ? $"Pegar {item.LocalizedName} ({amount})" : $"Pegar {item.LocalizedName}";
            }
        }

        public bool CanInteract => isActiveAndEnabled && item != null && amount > 0;

        public void Interact(PlayerInteractor interactor)
        {
            PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            int added = inventory.Add(item, amount);
            if (added == 0)
            {
                Notifications.Post("Inventário cheio");
                return;
            }

            amount -= added;
            WorldState.SetPickupRemaining(persistentId.Id, amount);
            Notifications.Post(added > 1 ? $"{item.LocalizedName} x{added}" : item.LocalizedName);
            if (!string.IsNullOrEmpty(item.LocalizedHint)) Notifications.Post(item.LocalizedHint);

            if (amount == 0)
            {
                gameObject.SetActive(false);
                Quests.StoryEvents.Raise(raiseOnPickup);
            }
            else Notifications.Post($"Sem espaço para mais {amount}");
        }
    }
}
