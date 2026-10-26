using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: atalhos de depuração da tela de slots do menu principal (Nibiru > Testes > Salvamento > Slots).
    /// Geram slot ocupado falso, "demo concluída" e ilegível, preenchem os 3 slots, limpam todos e avançam o tempo jogado.
    /// O slot falso é gravado como o do "Novo jogo" (sem checkpoint), então carregá-lo abre o começo da região: para testar
    /// o Carregar de verdade, jogue até um checkpoint. <see cref="RunTest"/> confere todos os atalhos numa pasta de teste
    /// (sem Play) e roda no teste geral dos atalhos.
    /// </summary>
    static class SlotShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Salvamento/Slots/";
        const string Scene = "Regiao1_SitioDeUr";

        static readonly string[] Areas = { "acampamento", "trincheiras", "aldeia", "base", "poco", "wadi", "fuga" };

        // ---------- Menu ----------

        [MenuItem(MenuRoot + "Slot ocupado falso no slot 1", priority = 1)] static void Occupied1() => Log(MakeOccupied(1));
        [MenuItem(MenuRoot + "Slot ocupado falso no slot 2", priority = 2)] static void Occupied2() => Log(MakeOccupied(2));
        [MenuItem(MenuRoot + "Slot ocupado falso no slot 3", priority = 3)] static void Occupied3() => Log(MakeOccupied(3));

        [MenuItem(MenuRoot + "Slot demo concluída no slot 1", priority = 11)] static void Demo1() => Log(MakeDemo(1));
        [MenuItem(MenuRoot + "Slot demo concluída no slot 2", priority = 12)] static void Demo2() => Log(MakeDemo(2));
        [MenuItem(MenuRoot + "Slot demo concluída no slot 3", priority = 13)] static void Demo3() => Log(MakeDemo(3));

        [MenuItem(MenuRoot + "Slot ilegível no slot 1", priority = 21)] static void Unreadable1() => Log(MakeUnreadable(1));
        [MenuItem(MenuRoot + "Slot ilegível no slot 2", priority = 22)] static void Unreadable2() => Log(MakeUnreadable(2));
        [MenuItem(MenuRoot + "Slot ilegível no slot 3", priority = 23)] static void Unreadable3() => Log(MakeUnreadable(3));

        [MenuItem(MenuRoot + "Preencher os 3 slots (ocupado, demo concluída e ilegível)", priority = 31)]
        static void FillMenu()
        {
            if (!Confirm("Sobrescrever os 3 slots reais com slots falsos?")) return;
            Log(FillAll());
        }

        [MenuItem(MenuRoot + "Limpar todos os slots", priority = 32)]
        static void ClearMenu()
        {
            if (!Confirm("Apagar os 3 slots reais?")) return;
            ClearAll();
            Log("Salvamento: os 3 slots foram apagados.");
        }

        [MenuItem(MenuRoot + "Avançar o tempo jogado em 10 minutos", priority = 41)] static void Advance10() => Log(AdvancePlayTime(600f));
        [MenuItem(MenuRoot + "Avançar o tempo jogado em 1 hora", priority = 42)] static void Advance60() => Log(AdvancePlayTime(3600f));

        static bool Confirm(string text) => EditorUtility.DisplayDialog("Slots de teste", text + "\n" + SaveSlots.Folder, "Sim", "Cancelar");

        static void Log(string text) => Debug.Log(text);

        // ---------- Ações (as mesmas que o teste confere) ----------

        public static string MakeOccupied(int slot)
        {
            string area = Areas[(slot * 2) % Areas.Length];
            SaveSlots.WriteTestSlot(slot, area, DateTime.Now.AddMinutes(-7 * slot), 1800f * slot + 125f, false, Scene);
            return $"Salvamento: slot {slot} ocupado falso ({area}, tempo {1800 * slot + 125} s). Abre o começo da região ao carregar.";
        }

        public static string MakeDemo(int slot)
        {
            SaveSlots.WriteTestSlot(slot, "fuga", DateTime.Now.AddMinutes(-3 * slot), 3 * 3600f + 600f, true, Scene);
            return $"Salvamento: slot {slot} com a demo concluída (não carrega; só sobrescreve ou apaga).";
        }

        public static string MakeUnreadable(int slot)
        {
            Directory.CreateDirectory(SaveSlots.Folder);
            File.WriteAllText(SaveSlots.PathOf(slot), "{ slot ilegível de teste");
            return $"Salvamento: slot {slot} ilegível (arquivo corrompido de propósito).";
        }

        public static string FillAll()
        {
            MakeOccupied(1);
            MakeDemo(2);
            MakeUnreadable(3);
            return "Salvamento: slot 1 ocupado, slot 2 demo concluída, slot 3 ilegível.";
        }

        public static void ClearAll()
        {
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++) SaveSlots.Delete(slot);
        }

        /// <summary>Soma segundos ao tempo jogado: no relógio (com o Play ligado) e nos arquivos legíveis que têm resumo.</summary>
        public static string AdvancePlayTime(float seconds)
        {
            int changed = 0;
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++)
            {
                if (!File.Exists(SaveSlots.PathOf(slot))) continue;
                try
                {
                    JObject json = JObject.Parse(File.ReadAllText(SaveSlots.PathOf(slot)));
                    if (json["resumo"] is not JObject summary) continue;
                    float now = summary["tempoJogadoSegundos"]?.Value<float>() ?? 0f;
                    summary["tempoJogadoSegundos"] = now + seconds;
                    File.WriteAllText(SaveSlots.PathOf(slot), json.ToString(Formatting.Indented));
                    changed++;
                }
                catch (Exception)
                {
                    // Slot ilegível: não mexe.
                }
            }
            string clock = "";
            if (Application.isPlaying)
            {
                PlayTime.Set(PlayTime.Seconds + seconds);
                clock = $" Relógio da partida agora em {PlayTime.Seconds:0} s.";
            }
            return $"Salvamento: +{seconds:0} s de tempo jogado em {changed} slot(s).{clock}";
        }

        // ---------- Teste (sem Play) ----------

        /// <summary>Confere todos os atalhos numa pasta de teste (nunca nos slots reais). Roda no teste geral dos atalhos.</summary>
        public static (bool ok, string report) RunTest()
        {
            string folder = Path.Combine(Application.persistentDataPath, "Teste_AtalhosDeSlot");
            var problems = new System.Text.StringBuilder();
            void Check(bool condition, string what)
            {
                if (!condition) problems.AppendLine("  - " + what);
            }

            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                SaveSlots.UseFolder(folder);
                ClearAll();

                for (int slot = 1; slot <= 3; slot++)
                {
                    MakeOccupied(slot);
                    SaveSlots.SlotInfo info = SaveSlots.Inspect(slot);
                    Check(info.state == SaveSlots.SlotState.Occupied && info.hasPlayTime && info.AreaName != SaveSlots.AreaName(SaveSlots.DefaultArea),
                        $"slot ocupado falso no {slot}: estado {info.state}");

                    MakeDemo(slot);
                    Check(SaveSlots.Inspect(slot).state == SaveSlots.SlotState.DemoCompleted, $"demo concluída no {slot}");

                    MakeUnreadable(slot);
                    Check(SaveSlots.Inspect(slot).state == SaveSlots.SlotState.Unreadable, $"slot ilegível no {slot}");
                }

                FillAll();
                Check(SaveSlots.Inspect(1).state == SaveSlots.SlotState.Occupied, "preencher: slot 1 ocupado");
                Check(SaveSlots.Inspect(2).state == SaveSlots.SlotState.DemoCompleted, "preencher: slot 2 demo concluída");
                Check(SaveSlots.Inspect(3).state == SaveSlots.SlotState.Unreadable, "preencher: slot 3 ilegível");
                Check(SaveSlots.LatestSlot() == 1, "preencher: \"Continuar\" deveria abrir o slot 1");

                float before = SaveSlots.Inspect(1).playSeconds;
                string unreadable = File.ReadAllText(SaveSlots.PathOf(3));
                AdvancePlayTime(600f);
                Check(Math.Abs(SaveSlots.Inspect(1).playSeconds - (before + 600f)) < 1f, "avançar o tempo: o slot 1 deveria ganhar 10 min");
                Check(SaveSlots.Inspect(2).hasPlayTime && Math.Abs(SaveSlots.Inspect(2).playSeconds - (3 * 3600f + 1200f)) < 1f, "avançar o tempo: o slot demo também ganha");
                Check(File.ReadAllText(SaveSlots.PathOf(3)) == unreadable, "avançar o tempo não deveria mexer no slot ilegível");

                ClearAll();
                Check(Enumerable.Range(1, 3).All(s => SaveSlots.Inspect(s).state == SaveSlots.SlotState.Empty), "limpar: os 3 slots deveriam ficar vazios");
            }
            catch (Exception e)
            {
                problems.AppendLine("  - exceção: " + e.Message);
            }
            finally
            {
                SaveSlots.UseFolder(null);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            return (problems.Length == 0, problems.ToString());
        }
    }
}
