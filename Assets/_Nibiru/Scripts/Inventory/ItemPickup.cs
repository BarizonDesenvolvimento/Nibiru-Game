using Nibiru.Interaction;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Item no cenário que a Elena pode pegar. Se não couber tudo, pega o que der
    /// e o resto fica no chão, esperando espaço.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] ItemDefinition item;
        [SerializeField, Min(1)] int amount = 1;

        public string Prompt
        {
            get
            {
                if (item == null) return "Pegar";
                return amount > 1 ? $"Pegar {item.displayName} ({amount})" : $"Pegar {item.displayName}";
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
            Notifications.Post(added > 1 ? $"{item.displayName} x{added}" : item.displayName);

            if (amount == 0) gameObject.SetActive(false);
            else Notifications.Post($"Sem espaço para mais {amount}");
        }
    }
}
