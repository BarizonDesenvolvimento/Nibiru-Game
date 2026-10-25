using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Tira um item do inventário da Elena, chamado por um evento do Inspector. Serve para a tabuleta
    /// enterrada no nicho dos Guardiões.
    /// </summary>
    public class ItemRemover : MonoBehaviour
    {
        [SerializeField] ItemDefinition item;
        [SerializeField, Min(1)] int amount = 1;

        public void Remove()
        {
            var inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null) inventory.Remove(item, amount);
        }
    }
}
