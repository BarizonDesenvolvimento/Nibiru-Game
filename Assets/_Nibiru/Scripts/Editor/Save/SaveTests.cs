using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nibiru.Combat;
using Nibiru.Inventory;
using Nibiru.Memories;
using Nibiru.Player;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Nibiru.EditorTools.TestFlow;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Teste automático do salvamento em disco (A4a). Grava numa pasta só de teste (Teste_Salvamento, dentro de
    /// persistentDataPath), sem mexer nos slots de quem joga. Para cada checkpoint da região: ativa, grava, limpa a
    /// memória, carrega e compara com a volta depois de morrer (eventos, missões, reputação, transformação, itens pegos,
    /// checkpoints, inventário, munição, vida e posição). Mais: três slots independentes, carregar e avançar, morrer
    /// depois de carregar, "Continuar" e "Novo jogo" pelo menu principal, arquivo ausente, corrompido e de outra
    /// versão, a memória de Shuruppak sem gravar, o fim da demo, o tempo jogado na pausa e os nomes das áreas.
    /// Resumo no Console. Menu: Nibiru > Testes > Salvamento > Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class SaveTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string MenuSceneName = "MenuPrincipal";

        static readonly List<string> warnings = new List<string>();

        static SaveTests() => Register("salvamento.teste", Begin);

        [MenuItem("Nibiru/Testes/Salvamento/Teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("salvamento.teste");

        static void Begin()
        {
            string folder = Path.Combine(Application.persistentDataPath, "Teste_Salvamento");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            SaveSlots.UseFolder(folder);
            warnings.Clear();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;

            Start("Salvamento, teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Cada checkpoint da região: gravar, limpar a memória, carregar e comparar com a volta depois de morrer", EveryCheckpoint),
                ("Três slots independentes: gravar num não mexe nos outros, e cada um carrega o seu", ThreeSlots),
                ("Carregar um slot, avançar até outro checkpoint: o slot é atualizado e os outros dois ficam iguais", LoadAndAdvance),
                ("Morrer depois de carregar volta ao mesmo checkpoint, com o mesmo estado", DieAfterLoad),
                ("Menu principal: \"Continuar\" abre o slot mais recente; \"Novo jogo\" grava o começo no slot 1", MainMenu),
                ("Arquivo ausente, corrompido, de outra versão ou com código desconhecido: slot vazio, sem travar", BrokenFiles),
                ("Dentro da memória de Shuruppak nada é gravado; vale o checkpoint de antes", MemoryDoesNotSave),
                ("Fim da demo: o slot fica concluído e não carrega; pode ser sobrescrito e apagado", DemoEnd),
                ("Tempo jogado: anda no jogo, para na pausa, é gravado e volta ao carregar", PlayTimeScenario),
                ("Nomes das áreas: todo checkpoint tem área; sem prefixo conhecido mostra \"Sítio de Ur\" e avisa", AreaNames),
                ("Limpeza: apaga a pasta do teste", Cleanup),
            });
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (message.StartsWith("Salvamento:") && type != LogType.Log) warnings.Add(message);
        }

        // ---------- Cenários ----------

        static IEnumerator EveryCheckpoint()
        {
            yield return Restart("arquivo.cheio");
            List<string> codes = CheckpointCodes();
            Check(codes.Count >= 19, $"a região deveria ter pelo menos 19 checkpoints, tem {codes.Count}");
            int index = 0;
            foreach (string code in codes)
            {
                index++;
                Prepare(index);
                SaveSlots.Attach(1);
                Cp(code).ActivateNow();
                Checkpoints.Data saved = Checkpoints.Last;
                Check(saved != null && saved.checkpointId == code, $"{code}: o checkpoint deveria ficar guardado com o próprio código");
                SaveSlots.SlotInfo info = SaveSlots.Info(1);
                Check(info != null && Mathf.Abs(info.playSeconds - PlayTime.Seconds) < 1f, $"{code}: o slot 1 deveria ter sido gravado agora");
                Check(info != null && info.area == SaveSlots.AreaOf(code), $"{code}: área do resumo \"{info?.area}\", esperada \"{SaveSlots.AreaOf(code)}\"");
                string expectedWorld = WorldState.Describe(saved.world);
                string expectedElena = Expected(saved);

                // Como morrer: volta ao checkpoint pela memória.
                yield return Reload(() => Checkpoints.Restart(RegionSceneName));
                string afterDeath = Full();
                Vector3 deathPosition = Motor().transform.position;

                // Limpa a memória (como fechar o jogo) e carrega do disco.
                WorldState.Clear();
                Checkpoints.Clear();
                SaveSlots.Detach();
                float savedTime = info?.playSeconds ?? -1f;
                PlayTime.Set(0f);
                bool loaded = false;
                string worldOnLoad = null;
                yield return Reload(() =>
                {
                    loaded = SaveSlots.Load(1);
                    worldOnLoad = WorldState.Describe(WorldState.Capture());
                });
                Check(loaded, $"{code}: carregar o slot 1 deveria funcionar");
                Same($"{code}: mundo lido do disco x mundo do checkpoint", worldOnLoad, expectedWorld);
                Same($"{code}: Elena carregada x Elena do checkpoint", ElenaNow(), expectedElena);
                Same($"{code}: carregar x voltar depois de morrer", Full(), afterDeath);
                // O que vale é voltar igual a depois de morrer (conferido abaixo). Se mesmo assim ela não fica no ponto
                // (um colisor a empurra), é o comportamento de hoje: vai como observação no Console, não como falha.
                Vector3 now = Motor().transform.position;
                Check(Near(now, saved.position, 1f), $"{code}: a Elena deveria voltar ao ponto do checkpoint {saved.position}, está em {now}");
                if (!Near(now, saved.position))
                    Debug.Log($"Salvamento (teste), observação: ao voltar de {code} a Elena fica a {new Vector2(now.x - saved.position.x, now.z - saved.position.z).magnitude:0.00} m do ponto de retorno (igual a depois de morrer).");
                Check(Vector3.Distance(Motor().transform.position, deathPosition) < 0.05f, $"{code}: posição ao carregar x ao morrer: {Motor().transform.position} x {deathPosition}");
                Check(SaveSlots.CurrentSlot == 1, $"{code}: o slot da partida deveria ser o 1");
                Check(Mathf.Abs(PlayTime.Seconds - savedTime) < 1f, $"{code}: o tempo jogado deveria voltar com o slot");
            }
        }

        static IEnumerator ThreeSlots()
        {
            yield return Restart("arquivo.cheio");
            string[] codes = { "trincheiras.checkpoint_entrada", "aldeia.checkpoint_patio", "poco.checkpoint_pe_da_descida" };
            for (int slot = 1; slot <= 3; slot++) SaveSlots.Delete(slot);

            var before = new Dictionary<int, string>();
            for (int slot = 1; slot <= 3; slot++)
            {
                Prepare(slot * 7);
                SaveSlots.Attach(slot);
                Cp(codes[slot - 1]).ActivateNow();
                for (int other = 1; other < slot; other++)
                    Check(File.ReadAllText(SaveSlots.PathOf(other)) == before[other], $"gravar o slot {slot} mudou o slot {other}");
                before[slot] = File.ReadAllText(SaveSlots.PathOf(slot));
                Check(before[slot].Contains($"\"codigo\": \"{codes[slot - 1]}\""), $"o slot {slot} deveria guardar o checkpoint {codes[slot - 1]}");
            }
            Check(SaveSlots.LatestSlot() == 3, $"o último usado deveria ser o 3, é o {SaveSlots.LatestSlot()}");

            foreach (int slot in new[] { 2, 1, 3 })
            {
                yield return Reload(() => SaveSlots.Load(slot));
                Check(Near(Motor().transform.position, SpawnOf(codes[slot - 1]), 1f),$"o slot {slot} deveria pôr a Elena em {codes[slot - 1]}");
                Check(Rep("Guardioes") == slot * 7 * 4 - 30, $"o slot {slot} deveria ter reputação {slot * 7 * 4 - 30} com os Guardiões, tem {Rep("Guardioes")}");
                Check(SaveSlots.LatestSlot() == slot, $"carregar o slot {slot} deveria fazer dele o último usado");
                for (int other = 1; other <= 3; other++)
                    Check(File.ReadAllText(SaveSlots.PathOf(other)) == before[other], $"carregar o slot {slot} mudou o arquivo do slot {other}");
            }
        }

        static IEnumerator LoadAndAdvance()
        {
            string before1 = File.ReadAllText(SaveSlots.PathOf(1));
            string before3 = File.ReadAllText(SaveSlots.PathOf(3));
            yield return Reload(() => SaveSlots.Load(2));
            Check(SaveSlots.CurrentSlot == 2, "o slot da partida deveria ser o 2");
            Cp("base.checkpoint_tenda_comando").ActivateNow();
            string after2 = File.ReadAllText(SaveSlots.PathOf(2));
            Check(after2.Contains("\"codigo\": \"base.checkpoint_tenda_comando\""), "o slot 2 deveria ter o checkpoint novo");
            Check(SaveSlots.Info(2)?.area == "base", $"a área do slot 2 deveria ser a base, é {SaveSlots.Info(2)?.area}");
            Check(File.ReadAllText(SaveSlots.PathOf(1)) == before1, "o slot 1 não deveria mudar");
            Check(File.ReadAllText(SaveSlots.PathOf(3)) == before3, "o slot 3 não deveria mudar");
            Check(SaveSlots.LatestSlot() == 2, "o último usado deveria ser o 2");
        }

        static IEnumerator DieAfterLoad()
        {
            yield return Restart("arquivo.cheio");
            Prepare(9);
            SaveSlots.Attach(2);
            Cp("poco.checkpoint_antecamara").ActivateNow();
            string file = File.ReadAllText(SaveSlots.PathOf(2));
            WorldState.Clear();
            Checkpoints.Clear();
            SaveSlots.Detach();
            yield return Reload(() => SaveSlots.Load(2));
            string loaded = Full();

            Health health = Motor().GetComponent<Health>();
            health.Invincible = false;
            health.TakeDamage(new DamageInfo(health.Max * 10f, Motor().transform.position));
            yield return new Until(() => health == null || health.IsDead, 5f, "a Elena morrer");
            yield return new Delay(1f);
            yield return Reload(() => Motor().GetComponent<PlayerDeathHandler>().Restart());
            Same("morrer depois de carregar x estado carregado", Full(), loaded);
            Check(File.ReadAllText(SaveSlots.PathOf(2)) == file, "morrer não deveria mexer no slot");
        }

        static IEnumerator MainMenu()
        {
            // Continuar: abre o último usado (o 1, gravado por último).
            yield return Restart("arquivo.cheio");
            Prepare(4);
            SaveSlots.Attach(3);
            Cp("wadi.checkpoint_antes_do_radio").ActivateNow();
            SaveSlots.Attach(1);
            Cp("aldeia.checkpoint_beira").ActivateNow();
            string world = WorldState.Describe(Checkpoints.Last.world);
            WorldState.Clear();
            Checkpoints.Clear();
            SaveSlots.Detach();

            MainMenuUI menu = null;
            yield return OpenMenu(m => menu = m);
            Check(menu.ShowsContinue, "o menu deveria mostrar \"Continuar\"");
            Check(!PlayTime.Running, "o tempo jogado deveria parar no menu principal");
            menu.Continue();
            Same("mundo do \"Continuar\" x mundo gravado", WorldState.Describe(WorldState.Capture()), world);
            yield return WaitRegion();
            Check(SaveSlots.CurrentSlot == 1,"\"Continuar\" deveria abrir o slot 1");
            Check(Near(Motor().transform.position, SpawnOf("aldeia.checkpoint_beira")), "\"Continuar\" deveria pôr a Elena na beira da aldeia");

            // Sem slots: sem "Continuar".
            for (int slot = 1; slot <= 3; slot++) SaveSlots.Delete(slot);
            yield return OpenMenu(m => menu = m);
            Check(!menu.ShowsContinue, "sem slots o menu não deveria mostrar \"Continuar\"");

            // Novo jogo: grava o começo no slot 1, e "Continuar" funciona antes do primeiro checkpoint.
            menu.StartGame();
            yield return WaitRegion();
            SaveSlots.SlotInfo info = SaveSlots.Info(1);
            Check(info != null && info.area == SaveSlots.NewGameArea && !info.demoCompleted, "\"Novo jogo\" deveria gravar o começo no slot 1, no Acampamento");
            Check(info != null && info.AreaName == "Acampamento", $"nome da área do começo: \"{info?.AreaName}\"");
            Check(SaveSlots.CurrentSlot == 1, "a partida nova deveria gravar no slot 1");
            Vector3 start = Motor().transform.position;
            string fresh = WorldState.Describe(WorldState.Capture());
            yield return OpenMenu(m => menu = m);
            Check(menu.ShowsContinue, "depois do \"Novo jogo\" o menu deveria mostrar \"Continuar\"");
            menu.Continue();
            yield return WaitRegion();
            Check(Near(start, Motor().transform.position), "\"Continuar\" antes do primeiro checkpoint deveria pôr a Elena no começo");
            Same("mundo do começo", WorldState.Describe(WorldState.Capture()), fresh);
        }

        static IEnumerator BrokenFiles()
        {
            yield return Restart("arquivo.cheio");
            Prepare(3);
            SaveSlots.Attach(1);
            Cp("trincheiras.checkpoint_pistola").ActivateNow();
            string good = File.ReadAllText(SaveSlots.PathOf(1));
            int player = Motor().GetInstanceID();

            SaveSlots.Delete(3);
            Check(SaveSlots.Info(3) == null && !SaveSlots.Load(3), "slot ausente: vazio e não carrega");

            (string what, string text, bool infoNull)[] cases =
            {
                ("corrompido", "{ isto não é json", true),
                ("cortado pela metade", good.Substring(0, good.Length / 2), true),
                ("de versão antiga (0)", good.Replace("\"versao\": 1", "\"versao\": 0"), true),
                ("de versão futura (2)", good.Replace("\"versao\": 1", "\"versao\": 2"), true),
                ("vazio", "", true),
                ("com item desconhecido", good.Replace("\"item\": \"curativo\"", "\"item\": \"item_que_nao_existe\""), false),
                ("com missão desconhecida", good.Replace("\"missao.sobrevivente\"", "\"missao.que_nao_existe\""), false),
            };
            foreach (var (what, text, infoNull) in cases)
            {
                Check(text != good, $"o caso \"{what}\" deveria mudar o arquivo");
                File.WriteAllText(SaveSlots.PathOf(3), text);
                int warned = warnings.Count;
                SaveSlots.SlotInfo info = null;
                bool loaded = true;
                try
                {
                    info = SaveSlots.Info(3);
                    loaded = SaveSlots.Load(3);
                }
                catch (Exception e)
                {
                    Check(false, $"arquivo {what}: o jogo não deveria travar ({e.Message})");
                }
                Check((info == null) == infoNull, $"arquivo {what}: o resumo deveria ser {(infoNull ? "vazio" : "lido")}");
                Check(!loaded, $"arquivo {what}: não deveria carregar");
                Check(warnings.Count > warned, $"arquivo {what}: deveria avisar no Console");
            }

            File.WriteAllText(Path.Combine(SaveSlots.Folder, "ultimo_slot.json"), "{ quebrado");
            Check(SaveSlots.LatestSlot() == 0 && !SaveSlots.CanContinue, "\"último slot\" corrompido: sem \"Continuar\"");
            File.WriteAllText(Path.Combine(SaveSlots.Folder, "ultimo_slot.json"), "{\"slot\": 7}");
            Check(SaveSlots.LatestSlot() == 0, "\"último slot\" fora de 1 a 3: nenhum");
            SaveSlots.Delete(3);
            yield return new Delay(0.5f);
            Check(Motor() != null && Motor().GetInstanceID() == player, "nenhum arquivo ruim deveria recarregar a cena");
            Check(SaveSlots.Info(1) != null && File.ReadAllText(SaveSlots.PathOf(1)) == good, "os arquivos ruins do slot 3 não deveriam mexer no slot 1");
        }

        static IEnumerator MemoryDoesNotSave()
        {
            yield return Restart("arquivo.cheio");
            SaveSlots.Attach(1);
            Cp("aldeia.altar_nicho").ActivateNow();
            string before = File.ReadAllText(SaveSlots.PathOf(1));
            Vector3 niche = SpawnOf("aldeia.altar_nicho");

            Go("passo8.memoria");
            yield return new Until(() => Object.FindObjectsByType<PlayableMemory>(FindObjectsSortMode.None).Any(m => m.IsInMemory), 60f, "entrar na memória");
            yield return new Delay(1f);
            SaveSlots.Attach(1);
            PlayerRespawn respawn = Object.FindFirstObjectByType<PlayerRespawn>();
            respawn.SaveCheckpoint(respawn.transform, "memoria_shuruppak.teste");
            SaveSlots.WriteCurrent();
            Check(File.ReadAllText(SaveSlots.PathOf(1)) == before, "dentro da memória o slot não deveria mudar");

            yield return Reload(() => SaveSlots.Load(1));
            Check(Near(Motor().transform.position, niche), "carregar deveria voltar ao checkpoint de antes da memória (o nicho)");
            Check(!Object.FindObjectsByType<PlayableMemory>(FindObjectsSortMode.None).Any(m => m.IsInMemory), "carregar não deveria voltar dentro da memória");
        }

        static IEnumerator DemoEnd()
        {
            yield return Restart("arquivo.cheio");
            SaveSlots.Attach(1);
            Cp("fuga.autosave_embarque").ActivateNow();
            Check(SaveSlots.Info(1)?.demoCompleted == false, "antes do fim o slot não deveria estar concluído");

            var screen = Object.FindFirstObjectByType<DemoEndScreen>(FindObjectsInactive.Include);
            Check(screen != null, "a tela de fim da demo deveria estar na região");
            if (screen == null) yield break;
            screen.Show();
            SaveSlots.SlotInfo info = SaveSlots.Info(1);
            Check(info != null && info.demoCompleted && !info.CanLoad, "o fim da demo deveria marcar o slot como concluído");
            Check(!PlayTime.Running, "o tempo jogado deveria parar na tela de fim");
            int player = Motor().GetInstanceID();
            Check(!SaveSlots.Load(1), "um slot concluído não deveria carregar");
            Check(!SaveSlots.CanContinue, "um slot concluído não deveria aparecer em \"Continuar\"");
            yield return new Delay(0.5f);
            Check(Motor().GetInstanceID() == player, "recusar o slot concluído não deveria recarregar a cena");

            // Sobrescrever e apagar.
            Cp("fuga.autosave_depois_do_nicho").ActivateNow();
            Check(SaveSlots.Info(1)?.demoCompleted == false, "gravar de novo no slot deveria sobrescrever o \"concluído\"");
            SaveSlots.Delete(1);
            Check(SaveSlots.Info(1) == null, "apagar deveria esvaziar o slot");
            Time.timeScale = 1f;
        }

        static IEnumerator PlayTimeScenario()
        {
            yield return Restart("arquivo.cheio");
            Check(PlayTime.Running, "o tempo jogado deveria andar no jogo");
            float t0 = PlayTime.Seconds;
            yield return new Delay(1f);
            Check(PlayTime.Seconds > t0 + 0.5f, "o tempo jogado deveria andar");

            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(pause, null);
            float t1 = PlayTime.Seconds;
            yield return new Delay(1.5f);
            Check(!PlayTime.Running && Mathf.Abs(PlayTime.Seconds - t1) < 0.01f, $"o tempo jogado deveria parar na pausa (andou {PlayTime.Seconds - t1:0.00} s)");
            typeof(PauseMenuUI).GetMethod("Close", Any).Invoke(pause, null);
            yield return new Delay(1f);
            Check(PlayTime.Seconds > t1 + 0.5f, "o tempo jogado deveria voltar a andar ao fechar a pausa");

            PlayTime.Set(3725f);
            SaveSlots.Attach(2);
            Cp("aldeia.checkpoint_patio").ActivateNow();
            Check(Mathf.Abs((SaveSlots.Info(2)?.playSeconds ?? 0f) - 3725f) < 1f, "o slot deveria gravar o tempo jogado");
            PlayTime.Set(0f);
            yield return Reload(() => SaveSlots.Load(2));
            Check(PlayTime.Seconds >= 3725f && PlayTime.Seconds < 3735f, $"carregar deveria devolver o tempo jogado (está em {PlayTime.Seconds:0} s)");
        }

        static IEnumerator AreaNames()
        {
            yield return new Delay(0.1f);
            foreach (string code in CheckpointCodes())
            {
                string area = SaveSlots.AreaOf(code);
                Check(SaveSlots.AreaName(area) != SaveSlots.AreaName(SaveSlots.DefaultArea), $"{code}: deveria ter nome de área próprio");
            }
            Check(SaveSlots.AreaName("fuga") == "Estrada para Tell Amar", $"fuga: \"{SaveSlots.AreaName("fuga")}\"");

            var (slots, keyItems) = Bag().CopyContents();
            Checkpoints.Save(new Checkpoints.Data
            {
                sceneName = RegionSceneName, checkpointId = "semprefixo", position = Motor().transform.position, rotation = Quaternion.identity,
                health = 100f, slots = slots, keyItems = keyItems, world = WorldState.Capture(),
            });
            int warned = warnings.Count;
            SaveSlots.Attach(3);
            SaveSlots.WriteCurrent();
            Check(SaveSlots.Info(3)?.area == SaveSlots.DefaultArea, $"sem prefixo conhecido a área deveria ser a padrão, é {SaveSlots.Info(3)?.area}");
            Check(SaveSlots.Info(3)?.AreaName == "Sítio de Ur", $"nome padrão: \"{SaveSlots.Info(3)?.AreaName}\"");
            Check(warnings.Count > warned, "sem prefixo conhecido deveria avisar no Console");
        }

        static IEnumerator Cleanup()
        {
            yield return new Delay(0.1f);
            Application.logMessageReceived -= OnLog;
            string folder = SaveSlots.Folder;
            SaveSlots.UseFolder(null);
            SaveSlots.Detach();
            if (folder.EndsWith("Teste_Salvamento") && Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        // ---------- Ajudas ----------

        /// <summary>Muda o estado de um jeito próprio deste número: reputação, transformação, itens, munição, vida.</summary>
        static void Prepare(int n)
        {
            WorldState.SetReputation("Guardioes", n * 4 - 30);
            WorldState.SetReputation("FundacaoEnlil", -n);
            WorldState.Transformation = Mathf.Clamp(n, 0, 100);
            WorldState.SetPickupRemaining("trincheiras.curativo_1", 0);
            WorldState.SetPickupRemaining("poco.municao_plataforma_oeste", n % 2);
            WorldState.SetQuestStep("OQueOTomViu", n % 3);
            if (n % 3 == 0) WorldState.MarkQuestFailed("RemedioParaYusuf");

            var items = new List<ItemStack>
            {
                new ItemStack(Load<ItemDefinition>("Items/PistolaMakarov"), 1),
                new ItemStack(Load<ItemDefinition>("Items/Municao9mm"), 3 + n),
                new ItemStack(Load<ItemDefinition>("Items/Curativo"), 1 + n % 3),
                new ItemStack(Load<ItemDefinition>("Items/TabuletaDoJunco"), 1),
                new ItemStack(Load<ItemDefinition>("Items/RoloDeFilmeDoTom"), 1 + n % 3),
            };
            Bag().RestoreContents(items.Where(s => s.item.category != ItemCategory.Chave).ToList(), items.Where(s => s.item.category == ItemCategory.Chave).ToList());

            var pistol = Load<FirearmDefinition>("Combat/Pistola");
            var shotgun = Load<FirearmDefinition>("Combat/Espingarda");
            Motor().GetComponent<PlayerWeapons>().Restore(new PlayerWeapons.Snapshot
            {
                equipped = pistol,
                loaded = new Dictionary<FirearmDefinition, int> { [pistol] = n % 9, [shotgun] = n % 3 },
            });
            Health health = Motor().GetComponent<Health>();
            health.Restore(health.Max * (0.55f + 0.02f * (n % 20)));
        }

        static List<string> CheckpointCodes() =>
            Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene.name == RegionSceneName)
                .Select(c => c.GetComponent<PersistentId>().Id).OrderBy(c => c, StringComparer.Ordinal).ToList();

        static Checkpoint Cp(string code) =>
            Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.GetComponent<PersistentId>().Id == code);

        static Vector3 SpawnOf(string code)
        {
            Checkpoint cp = Cp(code);
            Transform point = (Transform)typeof(Checkpoint).GetField("spawnPoint", Any).GetValue(cp);
            return point != null ? point.position : cp.transform.position;
        }

        // O ponto de retorno fica no chão e o centro da Elena, uns 1,05 m acima dele.
        static bool Near(Vector3 a, Vector3 b, float horizontal = 0.1f) => new Vector2(a.x - b.x, a.z - b.z).magnitude < horizontal && Mathf.Abs(a.y - b.y) < 1.5f;

        static IEnumerator Reload(Action act)
        {
            int old = Motor().GetInstanceID();
            act();
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 40f, "a cena recarregar");
            int frame = Time.frameCount + 5;
            yield return new Until(() => Time.frameCount >= frame, 10f, "5 quadros");
        }

        static IEnumerator OpenMenu(Action<MainMenuUI> found)
        {
            SceneManager.LoadScene(MenuSceneName);
            yield return new Until(() => Object.FindFirstObjectByType<MainMenuUI>() != null, 30f, "o menu principal abrir");
            int frame = Time.frameCount + 3;
            yield return new Until(() => Time.frameCount >= frame, 10f, "3 quadros");
            found(Object.FindFirstObjectByType<MainMenuUI>());
        }

        static IEnumerator WaitRegion()
        {
            yield return new Until(() => SceneManager.GetActiveScene().name == RegionSceneName && Motor() != null, 40f, "a região abrir");
            int frame = Time.frameCount + 5;
            yield return new Until(() => Time.frameCount >= frame, 10f, "5 quadros");
        }

        /// <summary>O mundo e a Elena, em texto.</summary>
        static string Full() => WorldState.Describe(WorldState.Capture()) + "\n" + ElenaNow();

        static string ElenaNow()
        {
            PlayerMotor motor = Motor();
            var (slots, keyItems) = Bag().CopyContents();
            return Elena(motor.transform.eulerAngles.y, motor.GetComponent<Health>().Current, slots, keyItems,
                motor.GetComponent<PlayerWeapons>().Capture());
        }

        // O que a Elena deveria ter ao voltar deste checkpoint (a vida volta com pelo menos 50%, como depois de morrer).
        static string Expected(Checkpoints.Data data)
        {
            Health health = Motor().GetComponent<Health>();
            return Elena(data.rotation.eulerAngles.y, Mathf.Max(data.health, health.Max * 0.5f), data.slots, data.keyItems, data.weapons);
        }

        // A posição fica fora do texto (é conferida à parte, com tolerância).
        static string Elena(float yaw, float health, List<ItemStack> slots, List<ItemStack> keyItems, PlayerWeapons.Snapshot weapons) =>
            $"direção {Mathf.Round(Mathf.Repeat(yaw, 360f)):0}\nvida {health:0.0}\n" +
            $"inventário {string.Join(", ", slots.Select(s => $"{s.item.Id} x{s.count}"))}\n" +
            $"itens-chave {string.Join(", ", keyItems.Select(s => $"{s.item.Id} x{s.count}"))}\n" +
            $"arma {(weapons?.equipped != null ? weapons.equipped.Id : "nenhuma")}\n" +
            $"balas {string.Join(", ", (weapons?.loaded ?? new Dictionary<FirearmDefinition, int>()).OrderBy(p => p.Key.Id).Select(p => $"{p.Key.Id} {p.Value}"))}";

        static void Same(string what, string actual, string expected)
        {
            if (actual == expected) return;
            string[] a = actual.Split('\n'), e = expected.Split('\n');
            IEnumerable<string> diff = e.Except(a).Select(l => "falta: " + l).Concat(a.Except(e).Select(l => "sobra: " + l)).Take(6);
            Check(false, $"{what} diferem: {string.Join(" | ", diff)}");
        }
    }
}
