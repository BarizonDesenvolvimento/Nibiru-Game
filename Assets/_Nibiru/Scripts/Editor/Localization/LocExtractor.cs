using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Nibiru.Localization;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Passa os textos dos arquivos de dados (diálogos, escolhas, documentos, fita, itens, missões, tabuleta, memória)
    /// para a tabela pt-BR e liga cada campo à sua chave. Pode rodar quantas vezes quiser: um campo que já tem
    /// chave e nenhum texto antigo não muda; um campo com texto antigo (alguém digitou no Inspector) atualiza a
    /// tabela e esvazia o campo. Antes da primeira extração, guarda uma "foto" de todos os textos para a conferência
    /// de equivalência provar que nada mudou.
    /// Menus: Nibiru > Localização.
    /// </summary>
    static class LocExtractor
    {
        // Fora do Git: serve só para provar a migração na máquina onde ela foi feita.
        static string SnapshotPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "Nibiru_LocSnapshot.json");

        [MenuItem("Nibiru/Localização/Extrair textos dos arquivos de dados")]
        static void ExtractMenu() => Extract(log: true);

        [MenuItem("Nibiru/Localização/Conferir equivalência da extração")]
        static void CheckMenu() => Debug.Log(CheckEquivalence());

        /// <summary>Extrai os textos. Devolve um resumo curto.</summary>
        public static string Extract(bool log)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de extrair os textos.");
                return "no Play";
            }

            Loc.SetLanguage(Loc.DefaultLanguage, save: false);
            Loc.InvalidateCache();
            int newIds = LocFields.EnsureIds();
            List<LocField> fields = LocFields.All();
            if (!File.Exists(SnapshotPath)) WriteSnapshot(fields);

            SortedDictionary<string, string> table = LocTables.Read(Loc.DefaultLanguage);
            int newKeys = 0, updated = 0, cleared = 0, missing = 0;
            var warnings = new List<string>();
            var written = new Dictionary<string, string>();

            foreach (LocField f in fields)
            {
                string key = f.getKey();
                if (string.IsNullOrEmpty(key))
                {
                    key = f.suggestedKey;
                    f.setKey(key);
                    newKeys++;
                }
                string legacy = f.getLegacy();
                if (!string.IsNullOrEmpty(legacy))
                {
                    if (written.TryGetValue(key, out string other) && other != legacy)
                        warnings.Add($"Chave repetida com textos diferentes: {key} ({f.where})");
                    written[key] = legacy;
                    if (table.TryGetValue(key, out string old) && old != legacy) updated++;
                    table[key] = legacy;
                    if (!f.identity)
                    {
                        f.setLegacy(string.Empty);
                        cleared++;
                    }
                }
                else if (!table.ContainsKey(key))
                {
                    missing++;
                    warnings.Add($"Chave sem texto na tabela: {key} ({f.where})");
                }
                EditorUtility.SetDirty(f.asset);
            }

            LocTables.Write(Loc.DefaultLanguage, table);
            if (!File.Exists(LocTables.PathOf("en"))) LocTables.Write("en", new SortedDictionary<string, string>(StringComparer.Ordinal));
            AssetDatabase.SaveAssets();
            Loc.InvalidateCache();

            string summary = $"Extração: {fields.Count} campos, {newKeys} chaves novas, {newIds} códigos novos, {updated} textos atualizados, " +
                $"{cleared} campos esvaziados, {missing} chaves sem texto; {table.Count} textos no pt-BR.";
            if (log)
            {
                foreach (string w in warnings.Take(20)) Debug.LogWarning(w);
                Debug.Log(summary);
            }
            return summary;
        }

        /// <summary>Compara o que o jogo mostra agora com a foto tirada antes da extração.</summary>
        public static string CheckEquivalence()
        {
            if (!File.Exists(SnapshotPath)) return "Equivalência: não há foto anterior à extração (Library/Nibiru_LocSnapshot.json). Nada a comparar.";
            Loc.SetLanguage(Loc.DefaultLanguage, save: false);
            Loc.InvalidateCache();
            var before = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(SnapshotPath, Encoding.UTF8));
            var now = new Dictionary<string, string>();
            foreach (LocField f in LocFields.All()) now[f.snapshotId] = f.getResolved();

            var diffs = new List<string>();
            foreach (KeyValuePair<string, string> pair in before)
            {
                if (!now.TryGetValue(pair.Key, out string value)) diffs.Add($"sumiu: {pair.Key}");
                else if (value != pair.Value) diffs.Add($"mudou: {pair.Key}\n   antes: {Short(pair.Value)}\n   agora: {Short(value)}");
            }
            int extra = now.Keys.Count(k => !before.ContainsKey(k));
            return diffs.Count == 0
                ? $"Equivalência OK: {before.Count} textos iguais ao de antes da extração ({extra} campos novos desde então)."
                : $"Equivalência FALHOU: {diffs.Count} de {before.Count} textos diferentes.\n" + string.Join("\n", diffs.Take(15));
        }

        static void WriteSnapshot(List<LocField> fields)
        {
            var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (LocField f in fields) snapshot[f.snapshotId] = f.getResolved();
            File.WriteAllText(SnapshotPath, JsonConvert.SerializeObject(snapshot, Formatting.Indented), new UTF8Encoding(false));
        }

        static string Short(string text) => text == null ? "(nulo)" : text.Length <= 80 ? text : text.Substring(0, 80) + "...";
    }
}
