using System.Collections.Generic;
using Nibiru.EditorTools.Localization;
using Nibiru.Powers;
using Nibiru.Testes;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.PowerTools
{
    /// <summary>
    /// Só no editor: cria e atualiza os arquivos dos poderes (Data/Powers). Pode rodar de novo: só cria o que falta, dá as
    /// chaves de texto padrão (power.&lt;nome&gt;.*, com o texto já no pt-BR.json) e limpa o texto antigo do arquivo.
    /// Os valores que já existem (recarga, vaga) não são mexidos, a não ser na primeira criação.
    /// Depois, roda Nibiru > Salvamento > Criar registro e códigos (o código poder.* e o registro).
    /// </summary>
    static class PowerSetup
    {
        public const string Folder = "Assets/_Nibiru/Data/Powers";

        [MenuItem("Nibiru/Poderes/Criar e atualizar os dados dos poderes")]
        public static void CreateOrUpdate()
        {
            var report = new List<string>();
            var eco = AssetDatabase.LoadAssetAtPath<EchoPowerDefinition>(Folder + "/EcoDoAbzu.asset");
            if (eco == null) report.Add("FALTA o arquivo Data/Powers/EcoDoAbzu (não é criado aqui)");
            else Update(eco, report);

            Update(GetOrCreate<BreathPowerDefinition>("SoproDeEnlil", p => { p.slot = PowerSlot.Poder2; p.cooldown = 90f; p.duration = 0f; }, report), report);
            Update(GetOrCreate<AegisPowerDefinition>("EgideDeEnki", p => { p.slot = PowerSlot.Poder3; p.cooldown = 60f; p.duration = 8f; }, report), report);
            Update(GetOrCreate<TestPowerDefinition>("Teste_PoderVaga2", p => { p.slot = PowerSlot.Poder2; p.cooldown = 3f; }, report), report);
            Update(GetOrCreate<TestPowerDefinition>("Teste_PoderVaga3", p => { p.slot = PowerSlot.Poder3; p.cooldown = 3f; p.duration = 1f; }, report), report);

            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("Nibiru/Salvamento/Criar registro e códigos");
            Debug.Log("Poderes: dados atualizados.\n" + string.Join("\n", report));
        }

        static T GetOrCreate<T>(string name, System.Action<T> firstValues, List<string> report) where T : PowerDefinition
        {
            string path = $"{Folder}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            firstValues(asset);
            AssetDatabase.CreateAsset(asset, path);
            report.Add("criado " + path);
            return asset;
        }

        static void Update(PowerDefinition power, List<string> report)
        {
            List<string> missing = LocKeys.AssignDefaults(power);
            foreach (string m in missing) report.Add("FALTA texto no pt-BR.json: " + m);
            // O texto mora na tabela: o arquivo fica só com as chaves.
            if (missing.Count == 0)
            {
                power.displayName = string.Empty;
                power.description = string.Empty;
            }
            EditorUtility.SetDirty(power);
            report.Add($"{power.name}: vaga {(int)power.slot}, recarga {power.cooldown:0.#} s, duração {power.duration:0.#} s");
        }
    }
}
