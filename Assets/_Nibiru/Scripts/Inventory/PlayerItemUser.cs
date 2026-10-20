using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Usa itens do inventário. Por enquanto só os de cura; armas e itens-chave
    /// ganham uso quando os sistemas deles existirem.
    /// </summary>
    public class PlayerItemUser : MonoBehaviour
    {
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Health health;

        void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (health == null) health = GetComponent<Health>();
        }

        /// <summary>Tenta usar uma unidade do item. A mensagem explica o resultado para o jogador.</summary>
        public bool TryUse(ItemDefinition item, out string message)
        {
            if (item == null || !inventory.Has(item))
            {
                message = "Item indisponível";
                return false;
            }

            if (item.category == ItemCategory.Cura && item.healAmount > 0f)
            {
                if (health.IsDead)
                {
                    message = "Não dá para usar isso agora";
                    return false;
                }
                if (health.Current >= health.Max)
                {
                    message = "A vida já está cheia";
                    return false;
                }
                health.Heal(item.healAmount);
                inventory.Remove(item, 1);
                message = $"{item.displayName} usado";
                return true;
            }

            message = "Não dá para usar isso agora";
            return false;
        }
    }
}
