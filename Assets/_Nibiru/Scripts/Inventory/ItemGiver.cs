using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Entrega um item à Elena quando o <see cref="Give"/> é chamado por um evento da história (a Umm Karim
    /// dá a chave da casa quando o Yusuf recebe o remédio). Só entrega se ela ainda não tem o item, e uma vez
    /// só nesta partida (quem volta de um checkpoint recebe a bolsa salva, então não precisa entregar de novo).
    /// </summary>
    public class ItemGiver : MonoBehaviour
    {
        [SerializeField] ItemDefinition item;
        [SerializeField, Min(1)] int amount = 1;

        PlayerInventory inventory;
        bool given;

        public bool Given => given;

        public void Give()
        {
            if (given || item == null) return;
            if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null || inventory.Has(item)) return;
            if (inventory.Add(item, amount) == 0)
            {
                Notifications.Post("Inventário cheio");
                return;
            }
            given = true;
            Notifications.Post(item.LocalizedName);
        }
    }
}
