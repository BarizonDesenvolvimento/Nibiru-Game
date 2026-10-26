using System.Collections.Generic;
using Nibiru.Combat;
using Nibiru.Inventory;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// Registro central do salvamento: liga cada código permanente (item, arma, missão) ao arquivo de dados.
    /// O arquivo de salvamento guarda só os códigos; ao carregar, este registro devolve o arquivo certo.
    /// Fica em Resources/Save/Registro; refeito por Nibiru > Salvamento > Criar registro e códigos.
    /// Eventos e facções não entram aqui: o código deles é o nome do arquivo, e o salvamento os grava como texto.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Registro do Salvamento", fileName = "Registro")]
    public class SaveRegistry : ScriptableObject
    {
        public const string ResourcePath = "Save/Registro";

        public List<ItemDefinition> items = new List<ItemDefinition>();
        public List<FirearmDefinition> firearms = new List<FirearmDefinition>();
        public List<QuestDefinition> quests = new List<QuestDefinition>();

        static SaveRegistry instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => instance = null;

        public static SaveRegistry Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<SaveRegistry>(ResourcePath);
                if (instance == null) Debug.LogError("Salvamento: o registro Resources/Save/Registro não existe. Rode Nibiru > Salvamento > Criar registro e códigos.");
                return instance;
            }
        }

        public ItemDefinition Item(string code) => items.Find(i => i != null && i.Id == code);

        public FirearmDefinition Firearm(string code) => firearms.Find(f => f != null && f.Id == code);

        public QuestDefinition QuestByCode(string code) => quests.Find(q => q != null && q.StableId == code);

        /// <summary>A missão pelo nome do arquivo (a chave que o WorldState usa).</summary>
        public QuestDefinition QuestByName(string fileName) => quests.Find(q => q != null && q.Id == fileName);
    }
}
