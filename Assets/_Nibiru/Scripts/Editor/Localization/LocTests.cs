using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Localization;
using Nibiru.Quests;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Teste automático da localização (não precisa do Play): toda chave usada por um arquivo de dados existe em pt-BR;
    /// nenhum campo ficou com texto sem chave nem com o texto antigo; não há códigos de fala, resposta, linha de fita
    /// ou passo repetidos ou vazios; nenhuma chave serve a dois campos; o inglês só tem chaves que existem no pt-BR e
    /// guarda as mesmas tags e {0} do pt-BR; a troca de idioma cai no pt-BR. Escreve um resumo no Console.
    /// Menu: Nibiru > Testes > Localização.
    /// </summary>
    static class LocTests
    {
        static readonly Regex Placeholder = new Regex(@"\{(\d+)\}");
        static readonly Regex Tag = new Regex(@"</?[a-zA-Z]+(=[^>]*)?>");

        [MenuItem("Nibiru/Testes/Localização/Teste de chaves e códigos (resumo no Console)")]
        static void Run() => Debug.Log(RunAll());

        public static string RunAll()
        {
            var report = new StringBuilder();
            Loc.SetLanguage(Loc.DefaultLanguage, save: false);
            Loc.InvalidateCache();

            List<LocField> fields = LocFields.All();
            SortedDictionary<string, string> pt = LocTables.Read("pt-BR");
            SortedDictionary<string, string> en = LocTables.Read("en");

            Result(report, "Toda chave usada existe em pt-BR e não está vazia",
                fields.Where(f => !string.IsNullOrEmpty(f.getKey()) && !Loc.Has(f.getKey())).Select(f => $"{f.getKey()} ({f.where})"));

            Result(report, "Nenhum campo com texto e sem chave",
                fields.Where(f => string.IsNullOrEmpty(f.getKey())).Select(f => f.where));

            Result(report, "Nenhum campo ainda com o texto antigo no arquivo (rode a extração)",
                fields.Where(f => !f.identity && !string.IsNullOrEmpty(f.getLegacy())).Select(f => f.where));

            Result(report, "Chaves seguem a convenção (dlg., doc., tape., item., quest., tablet., memory., spk.)",
                fields.Where(f => !string.IsNullOrEmpty(f.getKey()) && !f.getKey().StartsWith(f.suggestedKey.Substring(0, f.suggestedKey.IndexOf('.') + 1))).Select(f => $"{f.getKey()} ({f.where})"));

            Result(report, "Nenhuma chave serve a dois campos (exceto o nome de quem fala, que é compartilhado)",
                fields.Where(f => !f.identity && !string.IsNullOrEmpty(f.getKey())).GroupBy(f => f.getKey()).Where(g => g.Count() > 1)
                    .Select(g => $"{g.Key} ({string.Join(" e ", g.Select(f => f.where))})"));

            Result(report, "Códigos das falas: sem repetição e sem vazio", DuplicateIds(
                LocFields.Assets<DialogueDefinition>().SelectMany(d => new[] { (d.name, d.nodes.Select(n => n.id).ToList()) })));

            Result(report, "Códigos das respostas: sem repetição e sem vazio", DuplicateIds(
                LocFields.Assets<DialogueDefinition>().SelectMany(d => d.nodes.Select(n => ($"{d.name}/{n.id}", n.choices.Select(c => c.id).ToList())))));

            Result(report, "Códigos das linhas de fita: sem repetição e sem vazio", DuplicateIds(
                LocFields.Assets<TapeDefinition>().Select(t => (t.name, t.lines.Select(l => l.id).ToList()))));

            Result(report, "Códigos dos passos de missão: sem repetição e sem vazio", DuplicateIds(
                LocFields.Assets<QuestDefinition>().Select(q => (q.name, q.steps.Select(s => s.id).ToList()))));

            Result(report, "Tags de cor e outras fecham, e os {0} são bem formados (pt-BR)",
                pt.Where(p => !WellFormed(p.Value)).Select(p => p.Key));

            Result(report, "O inglês só tem chaves que existem no pt-BR",
                en.Keys.Where(k => !pt.ContainsKey(k)));

            Result(report, "O inglês guarda as mesmas tags e {0} do pt-BR",
                en.Where(p => pt.TryGetValue(p.Key, out string source) && !string.IsNullOrEmpty(p.Value) && Signature(source) != Signature(p.Value)).Select(p => p.Key));

            Result(report, "Troca de idioma: o inglês vazio cai no pt-BR, o texto antigo e a chave sem texto aparecem",
                LanguageFallback(pt));

            Result(report, "Scripts de conteúdo: um arquivo novo recebe as chaves padrão e o aviso diz que texto falta", ContentKitKeys());

            int used = fields.Select(f => f.getKey()).Where(k => !string.IsNullOrEmpty(k)).Distinct().Count();
            int unused = pt.Keys.Count(k => !fields.Any(f => f.getKey() == k));
            int translated = en.Count(p => !string.IsNullOrEmpty(p.Value) && pt.ContainsKey(p.Key));
            report.AppendLine($"INFO  {pt.Count} textos em pt-BR ({used} chaves usadas pelos arquivos, {unused} na tabela sem uso nos arquivos); inglês: {translated} de {pt.Count} traduzidos (o resto cai no pt-BR).");

            Loc.SetLanguage(Loc.DefaultLanguage, save: false);
            return "Localização, teste de chaves:\n" + report;
        }

        static void Result(StringBuilder report, string name, IEnumerable<string> problems)
        {
            List<string> list = problems.ToList();
            report.AppendLine(list.Count == 0 ? $"OK  {name}" : $"FALHOU  {name} ({list.Count})\n      - " + string.Join("\n      - ", list.Take(10)));
        }

        static IEnumerable<string> DuplicateIds(IEnumerable<(string owner, List<string> ids)> groups)
        {
            foreach ((string owner, List<string> ids) in groups)
            {
                if (ids.Any(string.IsNullOrEmpty)) yield return $"{owner}: código vazio";
                foreach (var dup in ids.Where(i => !string.IsNullOrEmpty(i)).GroupBy(i => i).Where(g => g.Count() > 1))
                    yield return $"{owner}: código repetido \"{dup.Key}\"";
            }
        }

        // As tags abertas fecham na mesma quantidade e as chaves {} só aparecem como {número}.
        static bool WellFormed(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var open = new Dictionary<string, int>();
            foreach (Match m in Tag.Matches(text))
            {
                bool closing = m.Value.StartsWith("</");
                string name = Regex.Match(m.Value, @"[a-zA-Z]+").Value.ToLowerInvariant();
                open.TryGetValue(name, out int n);
                open[name] = n + (closing ? -1 : 1);
            }
            if (open.Values.Any(v => v != 0)) return false;
            string withoutPlaceholders = Placeholder.Replace(text, string.Empty);
            return !withoutPlaceholders.Contains("{") && !withoutPlaceholders.Contains("}");
        }

        // As tags (sem os valores) e os {n} de um texto, em ordem: o inglês precisa ter os mesmos.
        static string Signature(string text) =>
            string.Join(",", Tag.Matches(text).Cast<Match>().Select(m => Regex.Match(m.Value, @"</?[a-zA-Z]+").Value.ToLowerInvariant()).OrderBy(s => s, StringComparer.Ordinal))
            + "|" + string.Join(",", Placeholder.Matches(text).Cast<Match>().Select(m => m.Value).OrderBy(s => s, StringComparer.Ordinal));

        // Cria arquivos só na memória, com as ferramentas dos scripts de conteúdo, e confere as chaves ligadas.
        static IEnumerable<string> ContentKitKeys()
        {
            var table = new Dictionary<string, string>
            {
                ["dlg.Teste_Loc.a"] = "fala a", ["dlg.Teste_Loc.a.c1"] = "resposta 1", ["spk.elena"] = "Elena", ["spk.samir"] = "Samir",
                ["quest.Teste_Loc.s1"] = "passo 1", ["quest.Teste_Loc.name"] = "Missão",
            };

            var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
            dialogue.name = "Teste_Loc";
            dialogue.nodes = new[]
            {
                Content.ContentKit.N("a", "Elena", "b", Content.ContentKit.C("c1", "b", null), Content.ContentKit.C("c2", "b", null)),
                Content.ContentKit.N("b", "Samir"),
            };
            List<string> missing = LocKeys.AssignDefaults(dialogue, table);
            if (dialogue.nodes[0].textKey != "dlg.Teste_Loc.a") yield return "a fala não recebeu dlg.Teste_Loc.a";
            if (dialogue.nodes[0].speakerKey != "spk.elena") yield return "quem fala não recebeu spk.elena";
            if (dialogue.nodes[0].choices[0].textKey != "dlg.Teste_Loc.a.c1") yield return "a resposta não recebeu dlg.Teste_Loc.a.c1";
            if (missing.Count != 2 || !missing.Any(m => m.StartsWith("dlg.Teste_Loc.b ")) || !missing.Any(m => m.StartsWith("dlg.Teste_Loc.a.c2 ")))
                yield return "o aviso de texto faltando deveria citar dlg.Teste_Loc.b e dlg.Teste_Loc.a.c2: " + string.Join("; ", missing);

            var quest = ScriptableObject.CreateInstance<QuestDefinition>();
            quest.name = "Teste_Loc";
            quest.steps = new[] { Content.ContentKit.Step("s1"), Content.ContentKit.Step("s2") };
            missing = LocKeys.AssignDefaults(quest, table);
            if (quest.steps[0].objectiveKey != "quest.Teste_Loc.s1") yield return "o passo não recebeu quest.Teste_Loc.s1";
            if (missing.Count != 1 || !missing[0].StartsWith("quest.Teste_Loc.s2 ")) yield return "o aviso deveria citar só quest.Teste_Loc.s2: " + string.Join("; ", missing);

            var document = ScriptableObject.CreateInstance<DocumentDefinition>();
            document.name = "Teste_Loc";
            missing = LocKeys.AssignDefaults(document, new Dictionary<string, string> { ["doc.Teste_Loc.title"] = "t", ["doc.Teste_Loc.text"] = "x" });
            if (missing.Count != 0 || !string.IsNullOrEmpty(document.footerKey)) yield return "o rodapé opcional sem texto deveria ficar sem chave e sem aviso";
        }

        static IEnumerable<string> LanguageFallback(SortedDictionary<string, string> pt)
        {
            string sample = pt.Keys.FirstOrDefault();
            if (sample == null)
            {
                yield return "tabela pt-BR vazia";
                yield break;
            }
            Loc.SetLanguage("en", save: false);
            if (Loc.Language != "en") yield return "não trocou para o inglês";
            if (!Loc.HasIn("en", sample) && Loc.Get(sample) != pt[sample]) yield return "inglês sem a chave não caiu no pt-BR";
            if (Loc.Get("teste.chave.inexistente", "texto antigo") != "texto antigo") yield return "chave inexistente não devolveu o texto antigo";
            if (Loc.Get("teste.chave.inexistente") != "[teste.chave.inexistente]") yield return "chave inexistente sem texto antigo não devolveu a chave entre colchetes";
            if (Loc.Get(null, "antigo") != "antigo" || Loc.Get("") != string.Empty) yield return "chave vazia não devolveu o texto antigo";
            Loc.SetLanguage(Loc.DefaultLanguage, save: false);
            if (Loc.Language != Loc.DefaultLanguage) yield return "não voltou para o pt-BR";
            if (Loc.Get(sample) != pt[sample]) yield return "pt-BR não devolveu o texto da tabela";
        }
    }
}
