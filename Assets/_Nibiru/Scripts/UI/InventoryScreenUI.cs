using System.Text;
using Nibiru.Inventory;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela simples de inventário: lista os espaços e os itens-chave.
    /// Abre e fecha com o botão de inventário e pausa o jogo enquanto está aberta.
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class InventoryScreenUI : MonoBehaviour
    {
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Painel que aparece e some com a tela de inventário.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text content;

        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        readonly StringBuilder builder = new StringBuilder();
        float timeScaleBeforeOpen = 1f;

        public bool IsOpen => panel.activeSelf;

        void Awake() => panel.SetActive(false);

        void OnEnable() => inventory.Changed += Refresh;

        void OnDisable()
        {
            inventory.Changed -= Refresh;
            if (IsOpen) SetOpen(false);
        }

        void Update()
        {
            if (input.InventoryTogglePressed) SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (open == IsOpen) return;

            panel.SetActive(open);
            input.SetGameplayEnabled(!open);
            if (open)
            {
                timeScaleBeforeOpen = Time.timeScale;
                Time.timeScale = 0f;
                Refresh();
            }
            else
            {
                Time.timeScale = timeScaleBeforeOpen;
            }
        }

        void Refresh()
        {
            if (!IsOpen) return;

            builder.Clear();
            builder.Append("<b>INVENTÁRIO</b>   <color=").Append(Gold).Append('>')
                .Append(inventory.Slots.Count).Append('/').Append(inventory.SlotCount).AppendLine(" espaços</color>")
                .AppendLine();

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (i < inventory.Slots.Count) AppendStack(inventory.Slots[i]);
                else builder.Append("<color=").Append(Muted).AppendLine(">  — vazio —</color>");
            }

            builder.AppendLine().AppendLine("<b>ITENS-CHAVE</b>");
            if (inventory.KeyItems.Count == 0)
                builder.Append("<color=").Append(Muted).AppendLine(">  nenhum</color>");
            foreach (ItemStack stack in inventory.KeyItems) AppendStack(stack);

            builder.AppendLine().Append("<color=").Append(Muted).Append(">Fechar: ")
                .Append(input.InventoryButtonLabel).Append("</color>");

            content.text = builder.ToString();
        }

        void AppendStack(ItemStack stack)
        {
            builder.Append("  ").Append(stack.item.displayName);
            if (stack.count > 1) builder.Append("  <color=").Append(Gold).Append(">x").Append(stack.count).Append("</color>");
            if (!string.IsNullOrEmpty(stack.item.description))
                builder.Append("   <size=20><color=").Append(Muted).Append('>').Append(stack.item.description).Append("</color></size>");
            builder.AppendLine();
        }
    }
}
