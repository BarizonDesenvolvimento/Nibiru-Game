using System.Text;
using Nibiru.Inventory;
using Nibiru.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Objetivos atuais no canto da tela: a missão principal em destaque e as secundárias
    /// menores. Passos que pedem itens mostram a contagem, por exemplo (1/3).
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        [Tooltip("Missões da cena. Vazio = procura sozinho (cada cena de região tem o seu).")]
        [SerializeField] QuestManager quests;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] Text label;

        const string Gold = "#E8C46A";
        const string Muted = "#C9CDD6";

        readonly StringBuilder builder = new StringBuilder();

        void Awake()
        {
            if (quests == null) quests = FindFirstObjectByType<QuestManager>();
        }

        void OnEnable()
        {
            if (quests != null) quests.Changed += Refresh;
            if (inventory != null) inventory.Changed += Refresh;
            Localization.Loc.LanguageChanged += Refresh;
        }

        void OnDisable()
        {
            if (quests != null) quests.Changed -= Refresh;
            if (inventory != null) inventory.Changed -= Refresh;
            Localization.Loc.LanguageChanged -= Refresh;
        }

        void Start() => Refresh();

        void Refresh()
        {
            builder.Clear();
            if (quests == null)
            {
                label.text = string.Empty;
                return;
            }
            foreach ((QuestDefinition quest, QuestStep step) in quests.ActiveObjectives())
            {
                if (quest.kind == QuestKind.Principal)
                {
                    builder.Append("<size=22><color=").Append(Gold).Append('>').Append(quest.LocalizedName.ToUpperInvariant()).AppendLine("</color></size>");
                    builder.Append(step.LocalizedObjective).Append(Progress(step)).AppendLine().AppendLine();
                }
                else
                {
                    builder.Append("<size=22><color=").Append(Muted).Append(">• ").Append(quest.LocalizedName).Append(": ")
                        .Append(step.LocalizedObjective).Append(Progress(step)).AppendLine("</color></size>");
                }
            }
            label.text = builder.ToString();
        }

        string Progress(QuestStep step)
        {
            if (step.requiredItem == null || inventory == null || step.requiredCount <= 1) return string.Empty;
            int have = Mathf.Min(inventory.CountOf(step.requiredItem), step.requiredCount);
            return $" ({have}/{step.requiredCount})";
        }
    }
}
