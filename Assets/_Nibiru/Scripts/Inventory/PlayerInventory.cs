using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Inventory
{
    /// <summary>Uma pilha de itens iguais ocupando um espaço.</summary>
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        [Min(1)] public int count = 1;

        public ItemStack() { }

        public ItemStack(ItemDefinition item, int count)
        {
            this.item = item;
            this.count = count;
        }
    }

    /// <summary>
    /// Inventário da Elena. Itens comuns ocupam espaços limitados (cada espaço guarda
    /// até maxStack do mesmo item). Itens-chave ficam numa lista separada, sem limite.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Tooltip("Quantos espaços para itens comuns. Itens-chave não ocupam espaço.")]
        [SerializeField, Min(1)] int slotCount = 8;

        [Tooltip("Itens com que a Elena começa.")]
        [SerializeField] List<ItemStack> startingItems = new List<ItemStack>();

        readonly List<ItemStack> slots = new List<ItemStack>();
        readonly List<ItemStack> keyItems = new List<ItemStack>();

        /// <summary>Avisa a interface sempre que algo entra ou sai.</summary>
        public event Action Changed;

        public int SlotCount => slotCount;
        public IReadOnlyList<ItemStack> Slots => slots;
        public IReadOnlyList<ItemStack> KeyItems => keyItems;

        void Awake()
        {
            foreach (ItemStack stack in startingItems)
                if (stack.item != null) Add(stack.item, stack.count);
        }

        /// <summary>Guarda o que couber e devolve quantos entraram (0 se o inventário estiver cheio).</summary>
        public int Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            int left = amount;
            if (item.IsKeyItem)
            {
                ItemStack existing = keyItems.Find(s => s.item == item);
                if (existing != null) existing.count += left;
                else keyItems.Add(new ItemStack(item, left));
                left = 0;
            }
            else
            {
                // Primeiro completa as pilhas que já existem, depois ocupa espaços vazios.
                foreach (ItemStack stack in slots)
                {
                    if (left == 0) break;
                    if (stack.item != item) continue;
                    int put = Mathf.Min(item.maxStack - stack.count, left);
                    stack.count += put;
                    left -= put;
                }
                while (left > 0 && slots.Count < slotCount)
                {
                    int put = Mathf.Min(item.maxStack, left);
                    slots.Add(new ItemStack(item, put));
                    left -= put;
                }
            }

            int added = amount - left;
            if (added > 0) Changed?.Invoke();
            return added;
        }

        /// <summary>Tira até amount unidades, começando pelas últimas pilhas. Devolve quantas saíram.</summary>
        public int Remove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            List<ItemStack> list = item.IsKeyItem ? keyItems : slots;
            int left = amount;
            for (int i = list.Count - 1; i >= 0 && left > 0; i--)
            {
                if (list[i].item != item) continue;
                int take = Mathf.Min(list[i].count, left);
                list[i].count -= take;
                left -= take;
                if (list[i].count == 0) list.RemoveAt(i);
            }

            int removed = amount - left;
            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        public int CountOf(ItemDefinition item)
        {
            int total = 0;
            foreach (ItemStack stack in item != null && item.IsKeyItem ? keyItems : slots)
                if (stack.item == item) total += stack.count;
            return total;
        }

        public bool Has(ItemDefinition item, int amount = 1) => CountOf(item) >= amount;

        /// <summary>Cópia independente do conteúdo, para o checkpoint guardar.</summary>
        public (List<ItemStack> slots, List<ItemStack> keyItems) CopyContents() =>
            (slots.ConvertAll(s => new ItemStack(s.item, s.count)), keyItems.ConvertAll(s => new ItemStack(s.item, s.count)));

        /// <summary>Troca todo o conteúdo pelo que foi guardado.</summary>
        public void RestoreContents(List<ItemStack> savedSlots, List<ItemStack> savedKeyItems)
        {
            slots.Clear();
            keyItems.Clear();
            foreach (ItemStack s in savedSlots) slots.Add(new ItemStack(s.item, s.count));
            foreach (ItemStack s in savedKeyItems) keyItems.Add(new ItemStack(s.item, s.count));
            Changed?.Invoke();
        }
    }
}
