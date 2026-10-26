using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Nibiru.Localization;
using UnityEditor;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Só no editor: lê e grava as tabelas de textos (Resources/Localization/pt-BR.json e en.json). A gravação sai
    /// com as chaves em ordem fixa, uma por linha, para o Git mostrar só o que mudou.
    /// </summary>
    static class LocTables
    {
        public const string Folder = "Assets/_Nibiru/Resources/Localization";

        public static string PathOf(string code) => $"{Folder}/{code}.json";

        /// <summary>A tabela do idioma, direto do arquivo (vazia se o arquivo não existe).</summary>
        public static SortedDictionary<string, string> Read(string code)
        {
            var table = new SortedDictionary<string, string>(StringComparer.Ordinal);
            string path = PathOf(code);
            if (!File.Exists(path)) return table;
            var raw = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
            if (raw != null)
                foreach (KeyValuePair<string, string> pair in raw) table[pair.Key] = pair.Value;
            return table;
        }

        /// <summary>Grava a tabela e avisa o editor e o jogo para lerem de novo.</summary>
        public static void Write(string code, SortedDictionary<string, string> table)
        {
            string path = PathOf(code);
            Directory.CreateDirectory(Folder);
            var text = new StringBuilder("{\n");
            int i = 0;
            foreach (KeyValuePair<string, string> pair in table)
            {
                text.Append("  ").Append(JsonConvert.ToString(pair.Key)).Append(": ").Append(JsonConvert.ToString(pair.Value));
                text.Append(++i < table.Count ? ",\n" : "\n");
            }
            text.Append("}\n");
            string result = text.ToString();
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == result) return;
            File.WriteAllText(path, result, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            Loc.InvalidateCache();
        }

        /// <summary>
        /// Texto sem acento, minúsculo, com "_" no lugar de tudo que não é letra ou número. Serve para fazer
        /// chaves a partir de nomes ("Rádio dos soldados" vira "radio_dos_soldados").
        /// </summary>
        public static string Slug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "sem_nome";
            string normalized = text.Trim().Normalize(NormalizationForm.FormD);
            var result = new StringBuilder();
            foreach (char c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) result.Append(char.ToLowerInvariant(c));
                else if (result.Length > 0 && result[result.Length - 1] != '_') result.Append('_');
            }
            return result.ToString().Trim('_');
        }
    }
}
