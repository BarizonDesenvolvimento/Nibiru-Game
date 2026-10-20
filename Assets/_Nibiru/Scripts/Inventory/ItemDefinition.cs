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
        [Tooltip("Nome mostrado na tela.")]
        public string displayName;

        [Tooltip("Texto curto mostrado no inventário.")]
        [TextArea(2, 4)] public string description;

        [Tooltip("Ícone para o inventário (opcional por enquanto).")]
        public Sprite icon;

        [Tooltip("Itens da categoria Chave não ocupam espaço no inventário.")]
        public ItemCategory category;

        [Tooltip("Quantos cabem num mesmo espaço do inventário.")]
        [Min(1)] public int maxStack = 1;

        public bool IsKeyItem => category == ItemCategory.Chave;
    }
}
