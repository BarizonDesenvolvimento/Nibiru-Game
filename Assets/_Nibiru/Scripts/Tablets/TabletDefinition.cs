using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.Powers;
using Nibiru.Quests;
using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Tablets
{
    /// <summary>
    /// Uma tabuleta (um ME dos Anunnaki), como arquivo em Data/Tablets. Decifrar dá um poder,
    /// sobe a transformação e conta um pedaço da história na versão dos deuses.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Tabuletas/Tabuleta", fileName = "NovaTabuleta")]
    public class TabletDefinition : ScriptableObject
    {
        [Tooltip("Chaves dos textos na tabela de idiomas (tablet.<nome>.name e .text).")]
        [LocKey] public string nameKey;
        [LocKey] public string textKey;

        [Tooltip("Nome mostrado na tela. Depois da extração fica vazio: o texto mora na tabela de idiomas.")]
        public string displayName;

        public string LocalizedName => Loc.Get(nameKey, displayName);

        public string LocalizedText => Loc.Get(textKey, text);

        [Tooltip("Item da tabuleta no inventário. Usar o item decifra a tabuleta.")]
        public ItemDefinition item;

        [Tooltip("Texto da tabuleta, como os deuses contaram. Depois da extração fica vazio.")]
        [TextArea(6, 16)] public string text;

        [Tooltip("Poder ganho ao decifrar (opcional).")]
        public PowerDefinition grantsPower;

        [Tooltip("Quanto a transformação sobe ao decifrar (de 0 a 100).")]
        [Range(0, 100)] public int transformationGain = 5;

        [Tooltip("Evento disparado ao decifrar pela primeira vez (opcional).")]
        public StoryEvent raiseOnDecipher;

        public string Id => name;

        public bool IsDeciphered => WorldState.HasStoryEvent("Tabuleta:" + Id);

        public void MarkDeciphered() => WorldState.MarkStoryEvent("Tabuleta:" + Id);
    }
}
