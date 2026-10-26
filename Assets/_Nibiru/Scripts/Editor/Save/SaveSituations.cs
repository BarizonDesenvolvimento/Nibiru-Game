using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nibiru.Combat;
using Nibiru.Inventory;
using Nibiru.Memories;
using Nibiru.Player;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: situações para o dono testar o salvamento no Play (menu Nibiru > Testes > Salvamento > Situações).
    /// Cada uma prepara os slots de verdade (na pasta de persistentDataPath), põe a Elena no momento certo e escreve no
    /// Console o que fazer e o que deve acontecer. O roteiro completo está no ESTADO.md ("Como testar o salvamento").
    /// </summary>
    [InitializeOnLoad]
    static class SaveSituations
    {
        const string MenuRoot = "Nibiru/Testes/Salvamento/Situações/";
        const string MenuSceneName = "MenuPrincipal";
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Mundo do começo das Trincheiras (os 8 primeiros acontecimentos até o passo 7).
        static readonly string[] TrenchEvents = Step8Shortcuts.EventsUntilStep7.Take(8).ToArray();

        // Passo "Leia a tabuleta no nicho" da missão Sobrevivente, como no atalho do passo 8.
        const int ReadTabletStep = 12;

        static Func<bool> waitCondition;
        static Action waitThen;

        static SaveSituations()
        {
            Register("salvamento.sit_sem_slots", NoSlots);
            Register("salvamento.sit_menu", () => OpenMenu("Menu principal como está. Se há um slot recente que carrega, \"Continuar\" aparece em cima."));
            Register("salvamento.sit_tres_slots", ThreeSlots);
            Register("salvamento.sit_avancar", LoadAndAdvance);
            Register("salvamento.sit_morrer", DieAfterLoad);
            Register("salvamento.sit_memoria", Memory);
            Register("salvamento.sit_fim", DemoEnd);
            Register("salvamento.sit_corrompido", () => BrokenSlot("{ isto não é um arquivo de salvamento", "corrompido"));
            Register("salvamento.sit_versao_antiga", () => BrokenSlot(
                "{\"versao\": 0, \"resumo\": {\"area\": \"aldeia\", \"dataHora\": \"2026-10-01T10:00:00\"}, \"cena\": \"Regiao1_SitioDeUr\", \"mundo\": {}}", "de versão antiga (0)"));
            Register("salvamento.sit_tempo", PlayTimeSituation);
        }

        [MenuItem(MenuRoot + "1. Menu sem nenhum slot (Novo jogo e Continuar antes do primeiro ponto de retorno)", priority = 1)]
        static void M1() => Go("salvamento.sit_sem_slots");

        [MenuItem(MenuRoot + "2. Menu principal como está", priority = 2)]
        static void M2() => Go("salvamento.sit_menu");

        [MenuItem(MenuRoot + "3. Três slots em lugares diferentes (Continuar abre o mais recente)", priority = 3)]
        static void M3() => Go("salvamento.sit_tres_slots");

        [MenuItem(MenuRoot + "4. Carregar o slot 1 e avançar até outro ponto de retorno", priority = 4)]
        static void M4() => Go("salvamento.sit_avancar");

        [MenuItem(MenuRoot + "5. Carregar o slot 3 e morrer", priority = 5)]
        static void M5() => Go("salvamento.sit_morrer");

        [MenuItem(MenuRoot + "6. Dentro da memória de Shuruppak (não grava)", priority = 6)]
        static void M6() => Go("salvamento.sit_memoria");

        [MenuItem(MenuRoot + "7. Fim da demo (o slot fica concluído)", priority = 7)]
        static void M7() => Go("salvamento.sit_fim");

        [MenuItem(MenuRoot + "8. Slot 3 corrompido", priority = 8)]
        static void M8() => Go("salvamento.sit_corrompido");

        [MenuItem(MenuRoot + "9. Slot 3 de versão antiga", priority = 9)]
        static void M9() => Go("salvamento.sit_versao_antiga");

        [MenuItem(MenuRoot + "10. Tempo jogado e pausa", priority = 10)]
        static void M10() => Go("salvamento.sit_tempo");

        [MenuItem(MenuRoot + "Mostrar o tempo jogado agora (Console)", priority = 30)]
        static void ShowPlayTime()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Salvamento: o tempo jogado só existe com o Play ligado.");
                return;
            }
            int seconds = Mathf.FloorToInt(PlayTime.Seconds);
            Debug.Log($"Salvamento: tempo jogado {seconds / 60}m{seconds % 60:00}s ({PlayTime.Seconds:0.0} s), relógio {(PlayTime.Running ? "andando" : "PARADO")}, " +
                $"slot da partida {(SaveSlots.CurrentSlot == 0 ? "nenhum" : SaveSlots.CurrentSlot.ToString())}.");
        }

        [MenuItem(MenuRoot + "Matar a Elena agora", priority = 31)]
        static void KillMenu()
        {
            if (EditorApplication.isPlaying) Kill();
        }

        // ---------- Situações ----------

        static void NoSlots()
        {
            DeleteAll();
            OpenMenu("1. Não há slots: o menu NÃO mostra \"Continuar\". Escolha \"Novo jogo\" (grava o começo no slot 1). " +
                "Logo no começo, ainda dentro da barraca, saia do Play. Ligue o Play no menu (Situação 2): \"Continuar\" aparece e abre o começo. " +
                "Depois saia da barraca: \"Ponto de retorno marcado\" grava o slot 1 (veja o Resumo de todos os slots: Acampamento).");
        }

        static void ThreeSlots()
        {
            DeleteAll();
            Say("3. Gravando os 3 slots (uns 10 s, a tela vai recarregar três vezes)...");
            Trenches(() =>
            {
                Save(1, "trincheiras.checkpoint_entrada");
                Niche(() =>
                {
                    Save(2, "aldeia.altar_nicho");
                    RemedyShortcuts.Ready = false;
                    Go("arquivo.cheio");
                    WaitUntilReady(() => RemedyShortcuts.Ready, () =>
                    {
                        Save(3, "poco.checkpoint_pe_da_descida");
                        SaveSlots.Detach();
                        OpenMenu("3. Slot 1 nas Trincheiras (começo), slot 2 no nicho da Aldeia (fim do passo 7) e slot 3 no pé da descida do Poço (gravado por último). " +
                            "\"Continuar\" deve abrir o slot 3, no Poço. Depois use Salvamento > Carregar o slot 1 e o 2: cada um no seu lugar, com o seu inventário. " +
                            "Salvamento > Resumo mostra os três.\n" + Summary());
                    });
                });
            });
        }

        static void LoadAndAdvance()
        {
            if (!Loadable(1)) return;
            LoadThen(1, () => Say("4. Slot 1 carregado: entrada das Trincheiras, sem a pistola. Pegue a pistola no corpo do chefe de segurança (centro-leste, cuidado com a patrulha): " +
                "\"Ponto de retorno marcado\" grava o slot 1 ali. Salvamento > Resumo: só o slot 1 mudou de hora; o 2 e o 3 continuam iguais.\n" + Summary()));
        }

        static void DieAfterLoad()
        {
            if (!Loadable(3)) return;
            LoadThen(3, () =>
            {
                Say("5. Slot 3 carregado (pé da descida do Poço). Veja a vida, as balas e o inventário: a Elena vai morrer em 4 s. Na tela de morte, tente de novo: " +
                    "ela volta ao mesmo ponto, com o mesmo inventário e as mesmas balas, e o slot não muda (veja o Resumo).");
                float at = Time.time + 4f;
                WaitUntilReady(() => Time.time >= at, Kill);
            });
        }

        static void Memory()
        {
            Say("6. Preparando: nicho da Aldeia, slot 1 gravado no altar, e a memória começando...");
            Niche(() =>
            {
                Save(1, "aldeia.altar_nicho");
                Find("Conteudo_Aldeia/Primeira_Tabuleta").transform.Find("Ler_Tabuleta").gameObject.SetActive(false);
                Raise("TocouATabuleta");
                Object.FindFirstObjectByType<PlayableMemory>().Enter();
                WaitUntilReady(() => Object.FindObjectsByType<PlayableMemory>(FindObjectsSortMode.None).Any(m => m.IsInMemory), () =>
                    Say("6. Você está na memória (Ziusudra), e o slot 1 é o nicho de antes dela. Use Salvamento > Gravar aqui no slot 1: o Console diz que dentro da " +
                        "memória não grava. Depois use Salvamento > Fechar e continuar: a Elena volta ao nicho, ANTES da memória."));
            });
        }

        static void DemoEnd()
        {
            Say("7. Preparando o fim da viagem (passo 15) com a partida no slot 1...");
            int old = PlayerId();
            Go("passo15.chegada");
            WaitUntilReady(() => PlayerId() != 0 && PlayerId() != old && Time.timeSinceLevelLoad > 2f, () =>
            {
                SaveSlots.Attach(1);
                SaveSlots.WriteCurrent();
                Say("7. Slot 1 gravado aqui (Estrada para Tell Amar). Espere a última fala, feche o diário: aparece a tela de fim, e o slot 1 fica \"DEMO CONCLUÍDA\" " +
                    "(Salvamento > Resumo). Confirme para ir ao menu: sem \"Continuar\" (o slot 1 era o mais recente). Salvamento > Carregar o slot 1: recusa, com aviso no Console.\n" + Summary());
            });
        }

        static void BrokenSlot(string content, string what)
        {
            Directory.CreateDirectory(SaveSlots.Folder);
            File.WriteAllText(SaveSlots.PathOf(3), content);
            File.WriteAllText(Path.Combine(SaveSlots.Folder, "ultimo_slot.json"), "{\"slot\": 3}");
            OpenMenu($"Slot 3 {what}, e ele é o \"último usado\". O jogo não trava: o menu NÃO mostra \"Continuar\" e o Console avisa (\"Salvamento: o slot 3 ...\"). " +
                "Salvamento > Resumo: o slot 3 aparece vazio. Salvamento > Carregar o slot 3: nada acontece, só o aviso. Gravar no slot 3 de novo o conserta.");
        }

        static void PlayTimeSituation()
        {
            Niche(() =>
            {
                Save(2, "aldeia.altar_nicho");
                Say("10. Partida no slot 2 (nicho da Aldeia). Use Situações > Mostrar o tempo jogado agora; pause o jogo (Esc) por uns 30 s, abra o Arquivo, volte; " +
                    "mostre o tempo de novo: ele NÃO andou na pausa. Jogue um pouco e descanse no nicho: o Resumo mostra o tempo gravado no slot 2. " +
                    "Fechar e continuar: o tempo continua de onde parou.");
            });
        }

        // ---------- Mundos ----------

        static void Trenches(Action then)
        {
            string code = "trincheiras.checkpoint_entrada";
            StartFrom(TrenchEvents, new[] { ("Sobrevivente", 0), ("OQueOTomViu", 0) }, 0, SpawnOf(code), YawOf(code), then,
                () => SetBag(("FacaDeCampo", 1), ("TabuletaDoJunco", 1), ("RoloDeFilmeDoTom", 1)));
        }

        static void Niche(Action then) =>
            StartFrom(Step8Shortcuts.EventsUntilStep7, new[] { ("Sobrevivente", ReadTabletStep), ("RemedioParaYusuf", 0), ("OQueOTomViu", 0) }, 0,
                Step8Shortcuts.NicheSpot, 180f, then,
                () => SetBag(("FacaDeCampo", 1), ("PistolaMakarov", 1), ("Municao9mm", 6), ("Curativo", 1), ("TabuletaDoJunco", 1), ("RoloDeFilmeDoTom", 2)));

        // ---------- Ajudas ----------

        static void Save(int slot, string checkpoint)
        {
            SaveSlots.Attach(slot);
            Cp(checkpoint).ActivateNow();
        }

        static void DeleteAll()
        {
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++) SaveSlots.Delete(slot);
            string last = Path.Combine(SaveSlots.Folder, "ultimo_slot.json");
            if (File.Exists(last)) File.Delete(last);
        }

        static bool Loadable(int slot)
        {
            if (SaveSlots.Info(slot)?.CanLoad == true) return true;
            Debug.LogWarning($"Salvamento: o slot {slot} está vazio ou não carrega. Rode antes a Situação 3 (três slots).");
            return false;
        }

        static void LoadThen(int slot, Action then)
        {
            int old = PlayerId();
            if (!SaveSlots.Load(slot)) return;
            WaitUntilReady(() => PlayerId() != 0 && PlayerId() != old && Time.timeSinceLevelLoad > 1f, then);
        }

        static void Kill()
        {
            Health health = Object.FindFirstObjectByType<PlayerMotor>()?.GetComponent<Health>();
            if (health == null) return;
            health.Invincible = false;
            health.TakeDamage(new DamageInfo(health.Max * 10f, health.transform.position));
        }

        static void OpenMenu(string message)
        {
            SaveSlots.Detach();
            SceneManager.LoadScene(MenuSceneName);
            Say(message);
        }

        static void SetBag(params (string item, int count)[] items)
        {
            var stacks = items.Select(i => new ItemStack(Load<ItemDefinition>("Items/" + i.item), i.count)).ToList();
            Object.FindFirstObjectByType<PlayerInventory>().RestoreContents(
                stacks.Where(s => s.item.category != ItemCategory.Chave).ToList(), stacks.Where(s => s.item.category == ItemCategory.Chave).ToList());
        }

        static Checkpoint Cp(string code) =>
            Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.GetComponent<PersistentId>().Id == code);

        static Transform SpawnPoint(string code)
        {
            Checkpoint cp = Cp(code);
            var point = (Transform)typeof(Checkpoint).GetField("spawnPoint", Any).GetValue(cp);
            return point != null ? point : cp.transform;
        }

        static Vector3 SpawnOf(string code) => SpawnPoint(code).position;

        static float YawOf(string code) => SpawnPoint(code).eulerAngles.y;

        static int PlayerId()
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            return motor != null ? motor.GetInstanceID() : 0;
        }

        static string Summary()
        {
            var lines = new List<string>();
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++)
            {
                SaveSlots.SlotInfo info = SaveSlots.Info(slot);
                lines.Add(info == null ? $"Slot {slot}: vazio." : $"Slot {slot}: {info.AreaName}, {info.savedAt:HH:mm:ss}{(info.demoCompleted ? ", DEMO CONCLUÍDA" : "")}.");
            }
            lines.Add($"Usado por último: {SaveSlots.LatestSlot()}.");
            return string.Join("\n", lines);
        }

        static void Say(string message) => Debug.Log("Salvamento, situação " + message);

        // Espera própria (a dos atalhos de passo é uma só e seria trocada por esta).
        static void WaitUntilReady(Func<bool> condition, Action then)
        {
            waitCondition = condition;
            waitThen = then;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || waitCondition == null)
            {
                EditorApplication.update -= Tick;
                waitCondition = null;
                return;
            }
            if (!waitCondition()) return;
            Action then = waitThen;
            waitCondition = null;
            EditorApplication.update -= Tick;
            then();
        }
    }
}
