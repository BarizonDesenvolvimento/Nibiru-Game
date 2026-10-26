using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Usa itens do inventário: cura com itens de cura e equipa armas.
    /// Itens-chave ganham uso quando os sistemas deles existirem.
    /// </summary>
    public class PlayerItemUser : MonoBehaviour
    {
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Health health;
        [SerializeField] PlayerWeapons weapons;
        [SerializeField] Tablets.TabletReader tabletReader;

        void Awake()
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (health == null) health = GetComponent<Health>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();
            if (tabletReader == null) tabletReader = GetComponent<Tablets.TabletReader>();
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
                message = $"{item.LocalizedName} usado";
                return true;
            }

            if (item.tablet != null && tabletReader != null)
            {
                // A tela de leitura já mostra o resultado (ou a mensagem diz por que ainda não).
                return tabletReader.TryReadFromInventory(item.tablet, out message);
            }

            if (item.firearm != null && weapons != null)
            {
                if (weapons.Equipped == item.firearm)
                {
                    message = $"{item.LocalizedName} já está equipada";
                    return false;
                }
                // Equip já mostra o aviso "equipada".
                message = null;
                return weapons.Equip(item.firearm);
            }

            message = "Não dá para usar isso agora";
            return false;
        }
    }
}
