using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Nibiru.Localization;
using Nibiru.Powers;
using Nibiru.Save;
using Nibiru.Tablets;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.PowerTools
{
    /// <summary>
    /// Só no editor, sem Play: Nibiru > Testes > Poderes > Teste de dados (ME1). Confere os arquivos dos poderes (códigos,
    /// textos, vagas, recarga, registro, lista de códigos), a posse (PowerBook) e a ida e volta do "mundo" pelo texto do
    /// slot, inclusive o arquivo de antes do ME1. Não grava nada em disco e devolve o estado do mundo como estava.
    /// </summary>
    static class PowerDataTests
    {
        const string EcoCode = "poder.eco_do_abzu";
        const string RegistryPath = "Assets/_Nibiru/Resources/Save/Registro.asset";
        const string ElenaPrefab = "Assets/_Nibiru/Prefabs/Player/Elena.prefab";

        [MenuItem("Nibiru/Testes/Poderes/Teste de dados (sem Play)")]
        static void Menu()
        {
            (bool ok, string report) = Run();
            if (ok) Debug.Log(report);
            else Debug.LogError(report);
        }

        public static (bool ok, string report) Run()
        {
            var report = new StringBuilder("Poderes, teste de dados:\n");
            int passed = 0, failed = 0;
            void Result(string name, IEnumerable<string> problems)
            {
                List<string> list = problems.ToList();
                if (list.Count == 0) passed++; else failed++;
                report.AppendLine(list.Count == 0 ? $"OK  {name}" : $"FALHOU  {name} ({list.Count})\n      - " + string.Join("\n      - ", list.Take(15)));
            }

            List<PowerDefinition> powers = AssetDatabase.FindAssets("t:PowerDefinition", new[] { "Assets/_Nibiru/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<PowerDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).ToList();
            List<PowerDefinition> real = powers.Where(p => !p.name.StartsWith("Teste_")).ToList();

            Result("Existem o Eco, o Sopro e a Égide", new[] { "EcoDoAbzu", "SoproDeEnlil", "EgideDeEnki" }
                .Where(n => real.All(p => p.name != n)).Select(n => "falta " + n));

            Result("Todo poder tem código poder.* e nenhum se repete",
                powers.Where(p => string.IsNullOrEmpty(p.id) || !p.id.StartsWith("poder.")).Select(p => $"{p.name}: \"{p.id}\"")
                    .Concat(powers.GroupBy(p => p.id).Where(g => g.Count() > 1).Select(g => $"{g.Key} ({string.Join(", ", g.Select(p => p.name))})")));

            var registry = AssetDatabase.LoadAssetAtPath<SaveRegistry>(RegistryPath);
            Result("Todo poder está no registro do salvamento",
                registry == null ? new[] { "o registro não existe" } : powers.Where(p => !registry.powers.Contains(p)).Select(p => p.name));

            string listPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Codigos-Salvamento.txt"));
            var known = new HashSet<string>(File.Exists(listPath) ? File.ReadAllLines(listPath).Select(l => l.Trim()) : new string[0]);
            Result("Todo código de poder está na lista de códigos conhecidos", powers.Where(p => !known.Contains("poder " + p.id)).Select(p => p.id));

            Result("Nome e descrição com chave e texto no pt-BR; a dica, se houver, existe e só usa {0}",
                powers.SelectMany(p =>
                {
                    var problems = new List<string>();
                    if (string.IsNullOrEmpty(p.nameKey) || !Loc.Has(p.nameKey)) problems.Add($"{p.name}: nome ({p.nameKey})");
                    if (string.IsNullOrEmpty(p.descriptionKey) || !Loc.Has(p.descriptionKey)) problems.Add($"{p.name}: descrição ({p.descriptionKey})");
                    if (!string.IsNullOrEmpty(p.hintKey) && (!Loc.Has(p.hintKey) || Loc.Get(p.hintKey).Contains("{1}"))) problems.Add($"{p.name}: dica ({p.hintKey})");
                    if (!string.IsNullOrEmpty(p.displayName) || !string.IsNullOrEmpty(p.description)) problems.Add($"{p.name}: ainda tem texto antigo no arquivo");
                    return problems;
                }));

            Result("Vaga válida, recarga e duração não negativas",
                powers.Where(p => !Enum.IsDefined(typeof(PowerSlot), p.slot) || p.cooldown < 0f || p.duration < 0f)
                    .Select(p => $"{p.name}: vaga {(int)p.slot}, recarga {p.cooldown}, duração {p.duration}"));

            Result("Na demo, uma vaga por poder (sem contar os de teste)",
                real.GroupBy(p => p.slot).Where(g => g.Count() > 1).Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.name))}"));

            Result("Valores aprovados: Eco na vaga 1; Sopro na 2, recarga 90 s; Égide na 3, dura 8 s, recarga 60 s",
                Expect(real, "EcoDoAbzu", PowerSlot.Poder1, null, 0f)
                    .Concat(Expect(real, "SoproDeEnlil", PowerSlot.Poder2, 90f, 0f))
                    .Concat(Expect(real, "EgideDeEnki", PowerSlot.Poder3, 60f, 8f)));

            List<TabletDefinition> tablets = AssetDatabase.FindAssets("t:TabletDefinition", new[] { "Assets/_Nibiru/Data" })
                .Select(g => AssetDatabase.LoadAssetAtPath<TabletDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(t => t != null).ToList();
            Result("A Tabuleta do Junco dá o Eco do Abzu",
                tablets.Any(t => t.name == "TabuletaDoJunco" && t.grantsPower != null && t.grantsPower.Id == EcoCode) ? new string[0] : new[] { "a tabuleta não aponta para o Eco" });

            var elena = AssetDatabase.LoadAssetAtPath<GameObject>(ElenaPrefab);
            Result("Todo poder dado por uma tabuleta tem efeito no prefab da Elena",
                elena == null ? new[] { "o prefab da Elena não existe" } :
                tablets.Where(t => t.grantsPower != null).Select(t => t.grantsPower).Distinct()
                    .Where(p => !HasBehaviour(elena, p)).Select(p => p.name));

            Result("O prefab da Elena tem o PlayerPowers (no mesmo objeto do Eco) e nenhum efeito de teste",
                elena == null ? new[] { "o prefab da Elena não existe" } :
                (elena.GetComponentInChildren<PlayerPowers>(true) == null ? new[] { "sem PlayerPowers: rode Nibiru > Poderes > Montar poderes na Elena" } : new string[0])
                    .Concat(elena.GetComponentsInChildren<Nibiru.Testes.TestPowerBehaviour>(true).Select(b => "efeito de teste no prefab: " + b.gameObject.name)));

            // ---------- Posse e save (só na memória; o mundo volta como estava no fim) ----------
            WorldState.Snapshot before = WorldState.Capture();
            try
            {
                PowerDefinition eco = registry != null ? registry.Power(EcoCode) : null;
                PowerDefinition breath = real.FirstOrDefault(p => p.name == "SoproDeEnlil");
                if (eco == null || breath == null)
                {
                    Result("Posse e save", new[] { "sem o Eco ou o Sopro no registro: rode Nibiru > Poderes > Criar e atualizar os dados dos poderes" });
                }
                else
                {
                    Result("Posse: ganhar, perguntar, ganhar de novo, perder e o aviso de mudança", Ownership(eco));
                    Result("Recarga: começa cheia, anda com o tempo de jogo e some ao chegar a zero; uma vez por quadro", Cooldowns(eco));
                    Result("Checkpoint: o snapshot congela poderes, recargas e despertares e a volta restaura", CheckpointRoundTrip(eco, breath));
                    Result("Save: ida e volta pelo texto do slot (poderes, recargaDosPoderes, despertares), sempre gravados", SaveRoundTrip(eco, breath));
                    Result("Save de antes do ME1: sem os campos, a posse vem do evento Poder:EcoDoAbzu, recargas zeradas, despertares vazio", OldSave(eco));
                    Result("Save com código desconhecido fica ilegível; recarga inválida vira 0 e nunca passa da recarga do poder", BadSave(eco));
                    Result("Atalhos antigos: o evento Poder:<arquivo> vira posse e sai dos eventos", LegacyEvents(eco));
                }
            }
            finally
            {
                WorldState.Restore(before);
            }

            report.AppendLine($"Resumo: {passed} OK, {failed} FALHOU.");
            return (failed == 0, report.ToString());
        }

        static IEnumerable<string> Expect(List<PowerDefinition> powers, string name, PowerSlot slot, float? cooldown, float duration)
        {
            PowerDefinition p = powers.FirstOrDefault(x => x.name == name);
            if (p == null) yield break;
            if (p.slot != slot) yield return $"{name}: vaga {(int)p.slot}";
            if (cooldown.HasValue && !Mathf.Approximately(p.cooldown, cooldown.Value)) yield return $"{name}: recarga {p.cooldown}";
            if (!Mathf.Approximately(p.duration, duration)) yield return $"{name}: duração {p.duration}";
        }

        /// <summary>O efeito de um poder é um PowerBehaviour no mesmo objeto do PlayerPowers, que sabe qual arquivo executa.</summary>
        static bool HasBehaviour(GameObject elena, PowerDefinition power)
        {
            PlayerPowers powers = elena.GetComponentInChildren<PlayerPowers>(true);
            return powers != null && powers.GetComponents<PowerBehaviour>().Any(b => b.Definition == power);
        }

        static IEnumerable<string> Ownership(PowerDefinition eco)
        {
            WorldState.Clear();
            int changes = 0;
            Action count = () => changes++;
            PowerBook.Changed += count;
            try
            {
                if (PowerBook.Has(eco) || eco.IsUnlocked) yield return "começa com o Eco";
                if (!PowerBook.Grant(eco)) yield return "ganhar a primeira vez devolveu falso";
                if (!PowerBook.Has(eco) || !eco.IsUnlocked || !PowerBook.HasCode(EcoCode)) yield return "depois de ganhar, não tem";
                if (PowerBook.Grant(eco)) yield return "ganhar de novo devolveu verdadeiro";
                if (PowerBook.Owned.Count != 1 || PowerBook.Owned[0] != eco) yield return $"lista de poderes: {PowerBook.Owned.Count}";
                if (changes != 1) yield return $"avisos depois de ganhar duas vezes: {changes} (esperado 1)";
                PowerBook.StartCooldown(eco);
                if (!PowerBook.Revoke(eco)) yield return "perder devolveu falso";
                if (PowerBook.Has(eco) || PowerBook.CooldownLeft(eco) > 0f) yield return "depois de perder, ainda tem o poder ou a recarga";
                if (PowerBook.Revoke(eco)) yield return "perder de novo devolveu verdadeiro";
                if (changes != 2) yield return $"avisos no fim: {changes} (esperado 2)";
            }
            finally
            {
                PowerBook.Changed -= count;
            }
        }

        static IEnumerable<string> Cooldowns(PowerDefinition eco)
        {
            WorldState.Clear();
            PowerBook.Grant(eco);
            if (PowerBook.CooldownLeft(eco) != 0f) yield return "começa recarregando";
            PowerBook.StartCooldown(eco);
            if (!Mathf.Approximately(PowerBook.CooldownLeft(eco), eco.cooldown)) yield return $"recarga cheia {PowerBook.CooldownLeft(eco)} (esperado {eco.cooldown})";
            WorldState.TickPowerCooldowns(eco.cooldown - 2f);
            if (!Mathf.Approximately(PowerBook.CooldownLeft(eco), 2f)) yield return $"depois de andar, faltam {PowerBook.CooldownLeft(eco)} (esperado 2)";
            WorldState.TickPowerCooldowns(5f);
            if (PowerBook.CooldownLeft(eco) != 0f) yield return "não zerou";
            // Duas chamadas no mesmo quadro: só a primeira conta.
            PowerBook.StartCooldown(eco);
            PowerBook.Tick(1f);
            PowerBook.Tick(1f);
            float left = PowerBook.CooldownLeft(eco);
            if (left < eco.cooldown - 1.01f) yield return $"Tick duas vezes no mesmo quadro descontou {eco.cooldown - left:0.##} s";
        }

        static IEnumerable<string> CheckpointRoundTrip(PowerDefinition eco, PowerDefinition breath)
        {
            WorldState.Clear();
            PowerBook.Grant(eco);
            WorldState.SetPowerCooldown(eco.Id, 4.5f);
            Awakenings.Mark("despertar.teste");
            WorldState.Snapshot frozen = WorldState.Capture();
            string expected = WorldState.Describe(frozen);
            PowerBook.Grant(breath);
            WorldState.SetPowerCooldown(eco.Id, 1f);
            Awakenings.Mark("despertar.outro");
            WorldState.Restore(frozen);
            string after = WorldState.Describe(WorldState.Capture());
            if (after != expected) yield return "depois de voltar:\n" + after + "\nesperado:\n" + expected;
            if (!expected.Contains("poder " + EcoCode) || !expected.Contains("recarga " + EcoCode + " = 4.50") || !expected.Contains("despertar despertar.teste"))
                yield return "a descrição do estado não lista poder, recarga e despertar: " + expected;
        }

        static IEnumerable<string> SaveRoundTrip(PowerDefinition eco, PowerDefinition breath)
        {
            WorldState.Clear();
            string emptyJson = SaveSlots.WorldToJson(WorldState.Capture());
            JObject empty = JObject.Parse(emptyJson);
            foreach (string field in new[] { "poderes", "recargaDosPoderes", "despertares" })
                if (empty[field] == null) yield return $"mundo vazio sem o campo {field}";

            PowerBook.Grant(eco);
            PowerBook.Grant(breath);
            WorldState.SetPowerCooldown(eco.Id, 4.5f);
            Awakenings.Mark("despertar.teste");
            WorldState.Snapshot snapshot = WorldState.Capture();
            string json = SaveSlots.WorldToJson(snapshot);
            JObject file = JObject.Parse(json);
            if (file["poderes"]?.ToString(Newtonsoft.Json.Formatting.None) != $"[\"{EcoCode}\",\"{breath.Id}\"]")
                yield return "poderes no arquivo: " + file["poderes"];
            if (Math.Abs((float?)file["recargaDosPoderes"]?[EcoCode] - 4.5f ?? 99f) > 0.001f) yield return "recarga no arquivo: " + file["recargaDosPoderes"];
            if (file["despertares"]?.ToString(Newtonsoft.Json.Formatting.None) != "[\"despertar.teste\"]") yield return "despertares no arquivo: " + file["despertares"];
            WorldState.Snapshot back = SaveSlots.WorldFromJson(json);
            if (WorldState.Describe(back) != WorldState.Describe(snapshot)) yield return "voltou diferente:\n" + WorldState.Describe(back);
        }

        static IEnumerable<string> OldSave(PowerDefinition eco)
        {
            WorldState.Clear();
            JObject file = JObject.Parse(SaveSlots.WorldToJson(WorldState.Capture()));
            file.Remove("poderes");
            file.Remove("recargaDosPoderes");
            file.Remove("despertares");
            ((JArray)file["eventos"]).Add(eco.LegacyEventId);
            ((JArray)file["eventos"]).Add("DecifrouTabuletaDoJunco");
            WorldState.Snapshot back = SaveSlots.WorldFromJson(file.ToString());
            WorldState.Restore(back);
            if (!PowerBook.Has(eco)) yield return "o Eco não veio do evento antigo";
            if (WorldState.HasStoryEvent(eco.LegacyEventId)) yield return "o evento antigo ficou nos eventos";
            if (!WorldState.HasStoryEvent("DecifrouTabuletaDoJunco")) yield return "um evento comum sumiu";
            if (PowerBook.CooldownLeft(eco) != 0f) yield return "recarga não zerada";
            if (Awakenings.Count != 0) yield return "despertares não vazio";

            WorldState.Clear();
            JObject older = JObject.Parse(SaveSlots.WorldToJson(WorldState.Capture()));
            older.Remove("poderes");
            older.Remove("recargaDosPoderes");
            older.Remove("despertares");
            WorldState.Restore(SaveSlots.WorldFromJson(older.ToString()));
            if (PowerBook.Owned.Count != 0) yield return "save de antes do passo 8 voltou com poder";
        }

        static IEnumerable<string> BadSave(PowerDefinition eco)
        {
            WorldState.Clear();
            JObject file = JObject.Parse(SaveSlots.WorldToJson(WorldState.Capture()));
            file["poderes"] = new JArray("poder.que_nao_existe");
            if (!Throws(file.ToString())) yield return "poder desconhecido não deixou ilegível";

            file = JObject.Parse(SaveSlots.WorldToJson(WorldState.Capture()));
            file["recargaDosPoderes"] = new JObject { ["poder.que_nao_existe"] = 3f };
            if (!Throws(file.ToString())) yield return "recarga de poder desconhecido não deixou ilegível";

            file = JObject.Parse(SaveSlots.WorldToJson(WorldState.Capture()));
            file["poderes"] = new JArray(EcoCode);
            file["recargaDosPoderes"] = new JObject { [EcoCode] = 9999f };
            WorldState.Restore(SaveSlots.WorldFromJson(file.ToString()));
            if (!Mathf.Approximately(PowerBook.CooldownLeft(eco), eco.cooldown)) yield return $"recarga grande virou {PowerBook.CooldownLeft(eco)} (esperado {eco.cooldown})";

            file["recargaDosPoderes"] = new JObject { [EcoCode] = -5f };
            WorldState.Restore(SaveSlots.WorldFromJson(file.ToString()));
            if (PowerBook.CooldownLeft(eco) != 0f) yield return $"recarga negativa virou {PowerBook.CooldownLeft(eco)}";
        }

        static bool Throws(string json)
        {
            try
            {
                SaveSlots.WorldFromJson(json);
                return false;
            }
            catch (InvalidDataException)
            {
                return true;
            }
        }

        static IEnumerable<string> LegacyEvents(PowerDefinition eco)
        {
            WorldState.Clear();
            WorldState.MarkStoryEvent(eco.LegacyEventId);
            int converted = PowerBook.ConvertLegacyEvents();
            if (converted != 1) yield return $"converteu {converted} (esperado 1)";
            if (!PowerBook.Has(eco)) yield return "o Eco não veio";
            if (WorldState.HasStoryEvent(eco.LegacyEventId)) yield return "o evento ficou";
            if (PowerBook.ConvertLegacyEvents() != 0) yield return "converteu de novo";
        }
    }
}
