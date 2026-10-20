using System.Collections.Generic;
using System.Text;
using Nibiru.Combat;
using Nibiru.Factions;
using Nibiru.Inventory;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela simples de inventário: lista os espaços e os itens-chave, deixa escolher
    /// um item e usar. Abre e fecha com o botão de inventário e pausa o jogo enquanto está aberta.
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class InventoryScreenUI : MonoBehaviour
    {
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerItemUser itemUser;
        [SerializeField] PlayerWeapons weapons;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Painel que aparece e some com a tela de inventário.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text content;

        [Tooltip("Facções mostradas na tela, em ordem.")]
        [SerializeField] FactionDefinition[] factions = new FactionDefinition[0];

        const string Gold = "#E8C46A";
        const string Muted = "#9AA0AA";

        readonly StringBuilder builder = new StringBuilder();
        // Itens que podem ser escolhidos: primeiro os espaços, depois os itens-chave.
        readonly List<ItemStack> entries = new List<ItemStack>();
        int selected;
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
            if (input.InventoryTogglePressed)
            {
                // Só abre se a Elena estiver no controle (não abre morta nem com outro menu aberto).
                if (IsOpen) SetOpen(false);
                else if (input.GameplayEnabled) SetOpen(true);
                return;
            }
            if (!IsOpen) return;

            if (input.BackPressed)
            {
                SetOpen(false);
                return;
            }

            int step = input.VerticalNavStep;
            if (step != 0 && entries.Count > 0)
            {
                selected = (selected + step + entries.Count) % entries.Count;
                Refresh();
            }

            if (input.ConfirmPressed && selected < entries.Count)
            {
                ItemDefinition item = entries[selected].item;
                // Ler uma tabuleta abre outra tela: fecha o inventário antes.
                if (item.tablet != null) SetOpen(false);
                itemUser.TryUse(item, out string message);
                if (!string.IsNullOrEmpty(message)) Notifications.Post(message);
                Refresh();
            }
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
                selected = 0;
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

            entries.Clear();
            entries.AddRange(inventory.Slots);
            entries.AddRange(inventory.KeyItems);
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, entries.Count - 1));

            builder.Clear();
            builder.Append("<b>INVENTÁRIO</b>   <color=").Append(Gold).Append('>')
                .Append(inventory.Slots.Count).Append('/').Append(inventory.SlotCount).AppendLine(" espaços</color>")
                .AppendLine();

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (i < inventory.Slots.Count) AppendEntry(inventory.Slots[i], i);
                else builder.Append("<color=").Append(Muted).AppendLine(">    — vazio —</color>");
            }

            builder.AppendLine().AppendLine("<b>ITENS-CHAVE</b>");
            if (inventory.KeyItems.Count == 0)
                builder.Append("<color=").Append(Muted).AppendLine(">    nenhum</color>");
            for (int i = 0; i < inventory.KeyItems.Count; i++)
                AppendEntry(inventory.KeyItems[i], inventory.Slots.Count + i);

            AppendFactions();

            builder.AppendLine();
            if (selected < entries.Count && !string.IsNullOrEmpty(entries[selected].item.description))
                builder.Append("<color=").Append(Muted).Append('>').Append(entries[selected].item.description).AppendLine("</color>");

            builder.Append("<size=24><color=").Append(Muted).Append(">[").Append(input.ConfirmButtonLabel).Append("] Usar     [")
                .Append(input.BackButtonLabel).Append("] Fechar</color></size>");

            content.text = builder.ToString();
        }

        // Facções que a Elena ainda não conhece só aparecem depois da primeira mudança.
        void AppendFactions()
        {
            bool header = false;
            foreach (FactionDefinition faction in factions)
            {
                if (faction == null || (faction.hiddenUntilChanged && !Reputation.IsKnown(faction))) continue;
                if (!header)
                {
                    builder.AppendLine().AppendLine("<b>FACÇÕES</b>");
                    header = true;
                }
                int value = Reputation.Get(faction);
                builder.Append("    <color=#").Append(ColorUtility.ToHtmlStringRGB(faction.color)).Append('>').Append(faction.displayName).Append("</color>   ")
                    .Append(Reputation.LevelName(value)).Append(" <color=").Append(Muted).Append(">(").Append(value).AppendLine(")</color>");
            }
        }

        void AppendEntry(ItemStack stack, int index)
        {
            bool isSelected = index == selected;
            builder.Append(isSelected ? "<color=" + Gold + ">  > " : "    ");
            builder.Append(stack.item.displayName);
            if (stack.count > 1) builder.Append("  x").Append(stack.count);
            if (weapons != null && stack.item.firearm != null && stack.item.firearm == weapons.Equipped)
                builder.Append("  (equipada, ").Append(weapons.LoadedRounds).Append('/').Append(stack.item.firearm.magazineSize).Append(')');
            if (isSelected) builder.Append("</color>");
            builder.AppendLine();
        }
    }
}
