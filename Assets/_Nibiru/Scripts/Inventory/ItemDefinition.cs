using Nibiru.Localization;
using UnityEngine;

namespace Nibiru.Inventory
{
    public enum ItemCategory
    {
        Arma,
        Municao,
        Cura,
        Recurso,
        Chave,
    }

    /// <summary>
    /// Um tipo de item (Curativo, Munição 9 mm...). Cada item do jogo é um arquivo destes
    /// em Data/Items; criar um item novo não exige mudar código.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Itens/Item", fileName = "NovoItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Chaves dos textos na tabela de idiomas (item.<nome>.name, .desc, .hint).")]
        [LocKey] public string nameKey;
        [LocKey] public string descriptionKey;
        [LocKey] public string hintKey;

        [Tooltip("Nome mostrado na tela. Depois da extração fica vazio: o texto mora na tabela de idiomas.")]
        public string displayName;

        [Tooltip("Texto curto mostrado no inventário. Depois da extração fica vazio.")]
        [TextArea(2, 4)] public string description;

        [Tooltip("Aviso extra mostrado na tela quando o item é pego (opcional), por exemplo como usá-lo. Depois da extração fica vazio.")]
        [TextArea(1, 2)] public string pickupHint;

        public string LocalizedName => Loc.Get(nameKey, displayName);

        public string LocalizedDescription => Loc.Get(descriptionKey, description);

        public string LocalizedHint => Loc.Get(hintKey, pickupHint);

        [Tooltip("Ícone para o inventário (opcional por enquanto).")]
        public Sprite icon;

        [Tooltip("Itens da categoria Chave não ocupam espaço no inventário.")]
        public ItemCategory category;

        [Tooltip("Quantos cabem num mesmo espaço do inventário.")]
        [Min(1)] public int maxStack = 1;

        [Tooltip("Só para itens de Cura: quanta vida devolve ao usar.")]
        [Min(0f)] public float healAmount;

        [Tooltip("Só para itens da categoria Arma: os dados da arma de fogo. Usar o item equipa a arma.")]
        public Combat.FirearmDefinition firearm;

        [Tooltip("Só para tabuletas: os dados da tabuleta. Usar o item decifra (ou relê) a tabuleta.")]
        public Tablets.TabletDefinition tablet;

        public bool IsKeyItem => category == ItemCategory.Chave;
    }
}
