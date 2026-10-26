using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nibiru.Combat;
using Nibiru.Documents;
using Nibiru.Factions;
using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.Quests;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: os códigos permanentes do salvamento em disco.
    /// - Criar registro e códigos: dá código a item, arma e missão que ainda não tem (nunca muda um que já existe),
    ///   refaz o registro (Resources/Save/Registro) e acrescenta os códigos novos à lista de códigos conhecidos
    ///   (Docs/Codigos-Salvamento.txt), que só cresce.
    /// - Teste de códigos (sem Play): código vazio, repetido, fora do registro, fora da lista, ou que estava na lista e
    ///   sumiu (arquivo renomeado ou apagado). Também roda no começo do teste geral dos atalhos.
    /// Para tirar de propósito um código da lista (um evento apagado de vez), apague a linha dele no arquivo.
    /// </summary>
    static class SaveCodes
    {
        const string DataRoot = "Assets/_Nibiru/Data";
        const string RegistryPath = "Assets/_Nibiru/Resources/Save/Registro.asset";
        const string RegionScenePath = "Assets/_Nibiru/Scenes/Regiao1_SitioDeUr.unity";
        const string PersistentIdScript = "Assets/_Nibiru/Scripts/Save/PersistentId.cs";
        const string CheckpointScript = "Assets/_Nibiru/Scripts/Save/Checkpoint.cs";

        static string KnownListPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Codigos-Salvamento.txt"));

        // ---------- Criar ----------

        [MenuItem("Nibiru/Salvamento/Criar registro e códigos")]
        static void CreateMenu()
        {
            int assigned = AssignMissingCodes();
            SaveRegistry registry = BuildRegistry();
            int added = AppendKnownCodes();
            Debug.Log($"Salvamento: {assigned} códigos novos em itens, armas e missões; registro com {registry.items.Count} itens, " +
                $"{registry.firearms.Count} armas e {registry.quests.Count} missões; {added} códigos novos na lista de códigos conhecidos.");
        }

        static int AssignMissingCodes()
        {
            int count = 0;
            foreach (ItemDefinition item in Assets<ItemDefinition>().Where(i => string.IsNullOrEmpty(i.id)))
            {
                item.id = Snake(item.name);
                EditorUtility.SetDirty(item);
                count++;
            }
            foreach (FirearmDefinition firearm in Assets<FirearmDefinition>().Where(f => string.IsNullOrEmpty(f.id)))
            {
                firearm.id = "arma." + Snake(firearm.name);
                EditorUtility.SetDirty(firearm);
                count++;
            }
            foreach (QuestDefinition quest in Assets<QuestDefinition>().Where(q => string.IsNullOrEmpty(q.stableId)))
            {
                quest.stableId = "missao." + Snake(quest.name);
                EditorUtility.SetDirty(quest);
                count++;
            }
            AssetDatabase.SaveAssets();
            return count;
        }

        static SaveRegistry BuildRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<SaveRegistry>(RegistryPath);
            if (registry == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath));
                AssetDatabase.Refresh();
                registry = ScriptableObject.CreateInstance<SaveRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.items = Assets<ItemDefinition>().OrderBy(i => i.name).ToList();
            registry.firearms = Assets<FirearmDefinition>().OrderBy(f => f.name).ToList();
            registry.quests = Assets<QuestDefinition>().OrderBy(q => q.name).ToList();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return registry;
        }

        static int AppendKnownCodes()
        {
            List<string> known = ReadKnown();
            var knownSet = new HashSet<string>(known);
            List<string> fresh = CurrentCodes().Select(c => c.line).Where(l => !knownSet.Contains(l)).Distinct().ToList();
            if (fresh.Count == 0 && File.Exists(KnownListPath)) return 0;

            var text = new StringBuilder();
            if (!File.Exists(KnownListPath))
            {
                text.AppendLine("# Códigos permanentes do salvamento em disco (Nibiru). Uma linha por código: categoria e código.");
                text.AppendLine("# Esta lista só cresce: Nibiru > Salvamento > Criar registro e códigos acrescenta os novos.");
                text.AppendLine("# O teste de códigos acusa um código daqui que sumiu (arquivo renomeado ou apagado): um save antigo");
                text.AppendLine("# deixaria de achar o que ele guardou. Para tirar um código de propósito, apague a linha dele.");
            }
            foreach (string line in fresh) text.AppendLine(line);
            File.AppendAllText(KnownListPath, text.ToString(), new UTF8Encoding(false));
            return fresh.Count;
        }

        // ---------- Teste ----------

        [MenuItem("Nibiru/Testes/Salvamento/Teste de códigos (resumo no Console)")]
        static void TestMenu()
        {
            (bool ok, string report) = RunTest();
            if (ok) Debug.Log(report);
            else Debug.LogError(report);
        }

        /// <summary>Roda todas as conferências. ok = nenhuma falhou.</summary>
        public static (bool ok, string report) RunTest()
        {
            var report = new StringBuilder("Salvamento, teste de códigos:\n");
            bool ok = true;
            void Result(string name, IEnumerable<string> problems)
            {
                List<string> list = problems.ToList();
                if (list.Count > 0) ok = false;
                report.AppendLine(list.Count == 0 ? $"OK  {name}" : $"FALHOU  {name} ({list.Count})\n      - " + string.Join("\n      - ", list.Take(15)));
            }

            List<ItemDefinition> items = Assets<ItemDefinition>();
            List<FirearmDefinition> firearms = Assets<FirearmDefinition>();
            List<QuestDefinition> quests = Assets<QuestDefinition>();

            Result("Todo item, arma e missão tem código (o campo, não o nome do arquivo)",
                items.Where(i => string.IsNullOrEmpty(i.id)).Select(i => $"item {i.name}")
                    .Concat(firearms.Where(f => string.IsNullOrEmpty(f.id)).Select(f => $"arma {f.name}"))
                    .Concat(quests.Where(q => string.IsNullOrEmpty(q.stableId)).Select(q => $"missão {q.name}")));

            List<(string category, string code, string line, string owner)> current = CurrentCodes();
            Result("Nenhum código repetido (por categoria)",
                current.GroupBy(c => c.line).Where(g => g.Count() > 1).Select(g => $"{g.Key} ({string.Join(", ", g.Select(c => c.owner))})"));

            var registry = AssetDatabase.LoadAssetAtPath<SaveRegistry>(RegistryPath);
            Result("O registro (Resources/Save/Registro) tem todo item, arma e missão",
                registry == null ? new[] { "o registro não existe" } :
                items.Where(i => !registry.items.Contains(i)).Select(i => $"item {i.name}")
                    .Concat(firearms.Where(f => !registry.firearms.Contains(f)).Select(f => $"arma {f.name}"))
                    .Concat(quests.Where(q => !registry.quests.Contains(q)).Select(q => $"missão {q.name}"))
                    .Concat(registry.items.Where(i => i == null).Select(_ => "item vazio no registro"))
                    .Concat(registry.firearms.Where(f => f == null).Select(_ => "arma vazia no registro"))
                    .Concat(registry.quests.Where(q => q == null).Select(_ => "missão vazia no registro")));

            List<string> known = ReadKnown();
            var currentLines = new HashSet<string>(current.Select(c => c.line));
            Result("Nenhum código da lista de códigos conhecidos sumiu (renomeado ou apagado)",
                known.Count == 0 ? new[] { "a lista Docs/Codigos-Salvamento.txt não existe ou está vazia" } :
                known.Where(k => !currentLines.Contains(k)));

            var knownSet = new HashSet<string>(known);
            Result("Todo código atual está na lista de códigos conhecidos (senão: Nibiru > Salvamento > Criar registro e códigos)",
                current.Select(c => c.line).Distinct().Where(l => !knownSet.Contains(l)));

            Result("Códigos dos objetos da região (checkpoints, itens do chão): sem vazio",
                SceneCodes(RegionScenePath).Where(c => string.IsNullOrEmpty(c.code)).Select(c => $"objeto {c.gameObject}"));

            Result("Todo checkpoint da região tem nome de área (ui.area.<prefixo> no pt-BR)",
                SceneCodes(RegionScenePath).Where(c => c.isCheckpoint && !Loc.Has(SaveSlots.AreaKey(SaveSlots.AreaOf(c.code))))
                    .Select(c => $"{c.code} (falta {SaveSlots.AreaKey(SaveSlots.AreaOf(c.code))})"));

            Result("Nome da área padrão existe (ui.area.sitio_de_ur)",
                Loc.Has(SaveSlots.AreaKey(SaveSlots.DefaultArea)) ? new string[0] : new[] { SaveSlots.AreaKey(SaveSlots.DefaultArea) });

            int checkpoints = SceneCodes(RegionScenePath).Count(c => c.isCheckpoint);
            report.AppendLine($"INFO  {current.Count} códigos ({items.Count} itens, {firearms.Count} armas, {quests.Count} missões, " +
                $"{current.Count(c => c.category == "evento")} eventos, {current.Count(c => c.category == "cena")} objetos da região, {checkpoints} deles checkpoints); " +
                $"{known.Count} na lista de códigos conhecidos.");
            return (ok, report.ToString());
        }

        // ---------- Códigos atuais ----------

        static List<(string category, string code, string line, string owner)> CurrentCodes()
        {
            var list = new List<(string category, string code, string line, string owner)>();
            void Add(string category, string code, string owner)
            {
                if (!string.IsNullOrEmpty(code)) list.Add((category, code, category + " " + code, owner));
            }

            foreach (ItemDefinition item in Assets<ItemDefinition>()) Add("item", item.id, item.name);
            foreach (FirearmDefinition firearm in Assets<FirearmDefinition>()) Add("arma", firearm.id, firearm.name);
            foreach (QuestDefinition quest in Assets<QuestDefinition>())
            {
                Add("missao", quest.stableId, quest.name);
                if (string.IsNullOrEmpty(quest.stableId)) continue;
                foreach (QuestStep step in quest.steps) Add("passo", quest.stableId + "/" + step.id, quest.name);
            }
            foreach (DocumentDefinition document in Assets<DocumentDefinition>()) Add("doc", document.id, document.name);
            foreach (TapeDefinition tape in Assets<TapeDefinition>()) Add("fita", tape.id, tape.name);
            foreach (StoryEvent storyEvent in Assets<StoryEvent>()) Add("evento", storyEvent.Id, AssetDatabase.GetAssetPath(storyEvent));
            foreach (FactionDefinition faction in Assets<FactionDefinition>()) Add("faccao", faction.Id, faction.name);
            foreach (var code in SceneCodes(RegionScenePath)) Add("cena", code.code, code.gameObject);
            return list;
        }

        /// <summary>Os códigos fixos (PersistentId) de uma cena, lidos do arquivo dela (sem abrir a cena).</summary>
        public static List<(string code, string gameObject, bool isCheckpoint)> SceneCodes(string scenePath)
        {
            string idGuid = AssetDatabase.AssetPathToGUID(PersistentIdScript);
            string checkpointGuid = AssetDatabase.AssetPathToGUID(CheckpointScript);
            string text = File.ReadAllText(Path.GetFullPath(scenePath));
            var codes = new List<(string code, string gameObject)>();
            var checkpointObjects = new HashSet<string>();
            var gameObjectRegex = new Regex(@"m_GameObject: \{fileID: (-?\d+)\}");
            var idRegex = new Regex(@"\n  id: ?(.*)");

            foreach (string block in text.Split(new[] { "\n--- !u!" }, System.StringSplitOptions.None))
            {
                if (!block.StartsWith("114 ")) continue;
                string owner = gameObjectRegex.Match(block).Groups[1].Value;
                if (block.Contains("guid: " + idGuid))
                    codes.Add((idRegex.Match(block).Groups[1].Value.Trim(), owner));
                else if (block.Contains("guid: " + checkpointGuid))
                    checkpointObjects.Add(owner);
            }
            return codes.Select(c => (c.code, c.gameObject, checkpointObjects.Contains(c.gameObject))).ToList();
        }

        static List<string> ReadKnown()
        {
            if (!File.Exists(KnownListPath)) return new List<string>();
            return File.ReadAllLines(KnownListPath).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
        }

        static List<T> Assets<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { DataRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null).ToList();

        // "FacaDeCampo" vira "faca_de_campo"; "Municao9mm", "municao_9mm".
        static string Snake(string name) =>
            Regex.Replace(Regex.Replace(name, @"(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_"), @"[^A-Za-z0-9_]", "_").ToLowerInvariant();
    }
}
