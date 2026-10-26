using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Inventory
{
    /// <summary>
    /// Escolhe entre duas reações conforme a Elena ter ou não um item (como o StoryEventBranch faz
    /// com eventos). Serve, por exemplo, para a Elena lembrar do remédio só se ainda o carrega.
    /// </summary>
    public class ItemBranch : MonoBehaviour
    {
        [SerializeField] ItemDefinition item;

        [Tooltip("O que acontece se a Elena tem o item.")]
        [SerializeField] UnityEvent ifHas = new UnityEvent();

        [Tooltip("O que acontece se ela não tem.")]
        [SerializeField] UnityEvent ifNot = new UnityEvent();

        public void Run()
        {
            var inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null && inventory.Has(item)) ifHas.Invoke();
            else ifNot.Invoke();
        }
    }
}
