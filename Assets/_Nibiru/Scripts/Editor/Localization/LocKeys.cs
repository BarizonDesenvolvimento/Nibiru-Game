using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Para os scripts de conteúdo (Editor/Content): dá a um arquivo de dados recém-criado as chaves de tradução
    /// padrão, desde que o texto já esteja na tabela pt-BR. O texto nunca fica nos scripts: ele mora só em
    /// Resources/Localization/pt-BR.json, e o script só diz os códigos (da fala, da resposta, do passo, da linha).
    /// </summary>
    static class LocKeys
    {
        /// <summary>
        /// Liga os campos do arquivo às chaves padrão (dlg.&lt;conversa&gt;.&lt;fala&gt;, doc.&lt;nome&gt;.title...).
        /// Um campo opcional sem texto na tabela fica sem chave. Devolve os campos obrigatórios cujo texto falta na
        /// tabela (a chave fica ligada mesmo assim, para o aviso e o teste dizerem qual texto escrever).
        /// <paramref name="table"/> é a tabela pt-BR; sem ela, lê o arquivo.
        /// </summary>
        public static List<string> AssignDefaults(Object asset, IDictionary<string, string> table = null)
        {
            table = table ?? LocTables.Read("pt-BR");
            LocFields.EnsureIds(asset);
            var missing = new List<string>();
            foreach (LocField f in LocFields.Of(asset, includeEmpty: true))
            {
                if (!string.IsNullOrEmpty(f.getKey())) continue;
                bool has = table.TryGetValue(f.suggestedKey, out string text) && !string.IsNullOrEmpty(text);
                if (has || f.required) f.setKey(f.suggestedKey);
                if (!has && f.required) missing.Add($"{f.suggestedKey} ({f.where})");
            }
            UnityEditor.EditorUtility.SetDirty(asset);
            return missing;
        }
    }
}
