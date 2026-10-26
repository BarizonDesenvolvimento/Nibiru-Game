using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Nibiru.Combat;
using Nibiru.Environment;
using Nibiru.Localization;
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
    /// Teste automático do salvamento em disco (A4a) e da tela de slots do menu principal (A4b). Grava numa pasta só de teste (Teste_Salvamento, dentro de
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
        static int exceptions;
        static string previousLanguage;
        static string realSlots;

        static SaveTests() => Register("salvamento.teste", Begin);

        [MenuItem("Nibiru/Testes/Salvamento/Teste automático (resumo no Console)", priority = 101)]
        static void Run() => Go("salvamento.teste");

        static void Begin()
        {
            string folder = Path.Combine(Application.persistentDataPath, "Teste_Salvamento");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            SaveSlots.UseFolder(folder);
            warnings.Clear();
            exceptions = 0;
            // Execução automática: nada pode mexer nos slots reais (confere no fim, na "Limpeza").
            realSlots = SlotGuard.RealFingerprint();
            SlotGuard.Begin();
            // Os textos conferidos são os do pt-BR, seja qual for o idioma escolhido nas Opções.
            previousLanguage = Loc.Language;
            Loc.SetLanguage("pt-BR", false);
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;

            Start("Salvamento, teste automático", new List<(string, Func<IEnumerator>)>
            {
                ("Cada checkpoint da região: gravar, limpar a memória, carregar e comparar com a volta depois de morrer", EveryCheckpoint),
                ("Três slots independentes: gravar num não mexe nos outros, e cada um carrega o seu", ThreeSlots),
                ("Carregar um slot, avançar até outro checkpoint: o slot é atualizado e os outros dois ficam iguais", LoadAndAdvance),
                ("Morrer depois de carregar volta ao mesmo checkpoint, com o mesmo estado", DieAfterLoad),
                ("Menu principal: \"Continuar\" abre o slot mais recente; \"Novo jogo\" grava o começo no slot 1", MainMenu),
                ("Arquivo ausente é vazio; corrompido, cortado, de outra versão ou com código desconhecido é ilegível: não carrega, não trava, não é apagado", BrokenFiles),
                ("Metadados do slot: área, data, tempo hh:mm, \"—\" sem tempo, os quatro estados e o save v1 sem tempo", SlotMetadata),
                ("Continuar: o jogável mais recente (empate: o menor número); demo e ilegível não contam; some sem jogável", LatestPlayable),
                ("Tela de slots: foco inicial, ilegível e demo não carregam, apagar e sobrescrever pedem confirmação com foco em Cancelar", SlotScreen),
                ("Sair para o menu: confirmação, estado limpo; depois Continuar, Carregar e Novo jogo valem como numa sessão nova", ExitToMenu),
                ("Dentro da memória de Shuruppak nada é gravado; vale o checkpoint de antes", MemoryDoesNotSave),
                ("Fim da demo: o slot fica concluído e não carrega; pode ser sobrescrito e apagado", DemoEnd),
                ("Tempo jogado: anda no jogo, para na pausa, é gravado e volta ao carregar", PlayTimeScenario),
                ("Nomes das áreas: todo checkpoint tem área; sem prefixo conhecido mostra \"Sítio de Ur\" e avisa", AreaNames),
                ("Proteção dos slots reais: ocupado pede confirmação, cancelar não grava, vazio grava direto, execução automática nunca toca", SlotProtection),
                ("Flag da execução automática: volta ao valor de antes mesmo se o teste quebrar, e vence pelo prazo", AutomatedFlag),
                ("Limpeza: apaga a pasta do teste", Cleanup),
            });
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception) exceptions++;
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
                yield return new Delay(0.05f);
                Cp(codes[slot - 1]).ActivateNow();
                for (int other = 1; other < slot; other++)
                    Check(File.ReadAllText(SaveSlots.PathOf(other)) == before[other], $"gravar o slot {slot} mudou o slot {other}");
                before[slot] = File.ReadAllText(SaveSlots.PathOf(slot));
                Check(before[slot].Contains($"\"codigo\": \"{codes[slot - 1]}\""), $"o slot {slot} deveria guardar o checkpoint {codes[slot - 1]}");
            }
            Check(SaveSlots.LatestSlot() == 3, $"o slot mais recente deveria ser o 3, é o {SaveSlots.LatestSlot()}");

            foreach (int slot in new[] { 2, 1, 3 })
            {
                yield return Reload(() => SaveSlots.Load(slot));
                Check(Near(Motor().transform.position, SpawnOf(codes[slot - 1]), 1f),$"o slot {slot} deveria pôr a Elena em {codes[slot - 1]}");
                Check(Rep("Guardioes") == slot * 7 * 4 - 30, $"o slot {slot} deveria ter reputação {slot * 7 * 4 - 30} com os Guardiões, tem {Rep("Guardioes")}");
                Check(SaveSlots.LatestSlot() == 3, $"carregar o slot {slot} não deveria mudar qual é o mais recente (o 3), é o {SaveSlots.LatestSlot()}");
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
            yield return new Delay(0.05f);
            Cp("base.checkpoint_tenda_comando").ActivateNow();
            string after2 = File.ReadAllText(SaveSlots.PathOf(2));
            Check(after2.Contains("\"codigo\": \"base.checkpoint_tenda_comando\""), "o slot 2 deveria ter o checkpoint novo");
            Check(SaveSlots.Info(2)?.area == "base", $"a área do slot 2 deveria ser a base, é {SaveSlots.Info(2)?.area}");
            Check(File.ReadAllText(SaveSlots.PathOf(1)) == before1, "o slot 1 não deveria mudar");
            Check(File.ReadAllText(SaveSlots.PathOf(3)) == before3, "o slot 3 não deveria mudar");
            Check(SaveSlots.LatestSlot() == 2, "o slot mais recente deveria ser o 2");
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
            yield return new Delay(0.05f);
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
            menu.StartGame(1);
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

            (string what, string text)[] cases =
            {
                ("corrompido", "{ isto não é json"),
                ("cortado pela metade", good.Substring(0, good.Length / 2)),
                ("de versão antiga (0)", good.Replace("\"versao\": 1", "\"versao\": 0")),
                ("de versão futura (2)", good.Replace("\"versao\": 1", "\"versao\": 2")),
                ("com 0 bytes", ""),
                ("com item desconhecido", good.Replace("\"item\": \"curativo\"", "\"item\": \"item_que_nao_existe\"")),
                ("com missão desconhecida", good.Replace("\"missao.sobrevivente\"", "\"missao.que_nao_existe\"")),
            };
            foreach (var (what, text) in cases)
            {
                Check(text != good, $"o caso \"{what}\" deveria mudar o arquivo");
                File.WriteAllText(SaveSlots.PathOf(3), text);
                int warned = warnings.Count;
                int thrown = exceptions;
                SaveSlots.SlotInfo info = null;
                bool loaded = true;
                try
                {
                    info = SaveSlots.Inspect(3);
                    loaded = SaveSlots.Load(3);
                }
                catch (Exception e)
                {
                    Check(false, $"arquivo {what}: o jogo não deveria travar ({e.Message})");
                }
                Check(info != null && info.state == SaveSlots.SlotState.Unreadable, $"arquivo {what}: o slot deveria ser ilegível, é {info?.state}");
                Check(SaveSlots.Info(3) != null, $"arquivo {what}: o slot ilegível não é vazio");
                Check(!loaded, $"arquivo {what}: não deveria carregar");
                Check(warnings.Count > warned, $"arquivo {what}: deveria avisar no Console");
                Check(exceptions == thrown, $"arquivo {what}: nenhuma exceção deveria chegar ao Console");
                Check(File.Exists(SaveSlots.PathOf(3)) && File.ReadAllText(SaveSlots.PathOf(3)) == text, $"arquivo {what}: o jogo não deveria apagar nem mexer no arquivo ilegível");
                Check(SaveSlots.LatestSlot() == 1, $"arquivo {what}: o ilegível não deveria entrar em \"Continuar\"");
            }

            // O ultimo_slot.json deixou de ser usado: nem lido (lixo ali não atrapalha), nem gravado.
            string last = Path.Combine(SaveSlots.Folder, "ultimo_slot.json");
            File.WriteAllText(last, "{\"slot\": 3}");
            Check(SaveSlots.LatestSlot() == 1 && SaveSlots.CanContinue, "o ultimo_slot.json não deveria mais decidir o \"Continuar\"");
            File.WriteAllText(last, "{ quebrado");
            Check(SaveSlots.LatestSlot() == 1, "ultimo_slot.json quebrado não deveria atrapalhar");
            File.Delete(last);
            SaveSlots.Delete(3);
            Check(!File.Exists(last), "nada deveria gravar o ultimo_slot.json");
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
            ClearSlots();
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

            // Um quadro longo (como o carregamento de uma cena) conta no máximo PlayTime.MaxStep.
            float t2 = PlayTime.Seconds;
            typeof(PlayTime).GetMethod("Tick", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 6f });
            Check(PlayTime.Seconds - t2 <= PlayTime.MaxStep + 0.001f, $"um quadro de 6 s deveria contar no máximo {PlayTime.MaxStep} s, contou {PlayTime.Seconds - t2:0.00}");

            PlayTime.Set(3725f);
            SaveSlots.Attach(2);
            Cp("aldeia.checkpoint_patio").ActivateNow();
            Check(Mathf.Abs((SaveSlots.Info(2)?.playSeconds ?? 0f) - 3725f) < 1f, "o slot deveria gravar o tempo jogado");
            PlayTime.Set(0f);
            yield return Reload(() => SaveSlots.Load(2));
            Check(PlayTime.Seconds >= 3725f && PlayTime.Seconds < 3735f, $"carregar deveria devolver o tempo jogado (está em {PlayTime.Seconds:0} s)");
        }

        static IEnumerator SlotMetadata()
        {
            yield return new Delay(0.1f);
            ClearSlots();
            var when = new DateTime(2026, 10, 26, 14, 32, 5);
            SaveSlots.WriteTestSlot(1, "aldeia", when, 3725f, false, RegionSceneName);
            SaveSlots.WriteTestSlot(2, "base", when, 59f, false, RegionSceneName);
            SaveSlots.WriteTestSlot(3, "wadi", when, -1f, false, RegionSceneName);

            SaveSlots.SlotInfo one = SaveSlots.Inspect(1);
            Check(one.state == SaveSlots.SlotState.Occupied && one.CanLoad, $"slot ocupado: estado {one.state}");
            Check(one.AreaName == "Aldeia", $"nome da área: \"{one.AreaName}\"");
            Check(SlotRows.PlayTimeText(one) == "01:02", $"3725 s deveriam mostrar 01:02, mostram {SlotRows.PlayTimeText(one)}");
            Check(one.savedAt == when, $"data gravada {one.savedAt} x {when}");
            Check(SlotRows.DateText(one) == when.ToString("g", CultureInfo.CurrentCulture), "a data deveria sair no formato local");
            string row = SlotRows.Format(one);
            Check(row.Contains("Aldeia") && row.Contains("01:02") && row.Contains(SlotRows.DateText(one)), $"linha do slot: \"{row}\"");
            Check(SlotRows.PlayTimeText(SaveSlots.Inspect(2)) == "00:00", "59 s deveriam mostrar 00:00");
            SaveSlots.SlotInfo three = SaveSlots.Inspect(3);
            Check(three.state == SaveSlots.SlotState.Occupied && !three.hasPlayTime && SlotRows.PlayTimeText(three) == "—", "sem tempo no arquivo: ocupado e mostra \"—\"");

            // Save v1 de antes do tempo jogado: o campo some do JSON e o slot continua carregável.
            string before = File.ReadAllText(SaveSlots.PathOf(1));
            string old = Regex.Replace(before, "\\s*\"tempoJogadoSegundos\": [^,\\n]*,?", "");
            Check(old != before && !old.Contains("tempoJogadoSegundos") && old.Contains("\"versao\": 1"), "o JSON v1 sem tempo deveria ser montado");
            File.WriteAllText(SaveSlots.PathOf(2), old);
            SaveSlots.SlotInfo legacy = SaveSlots.Inspect(2);
            Check(legacy.state == SaveSlots.SlotState.Occupied && !legacy.hasPlayTime && SlotRows.PlayTimeText(legacy) == "—" && SlotRows.Format(legacy).Contains("—"),
                $"JSON v1 sem tempoJogadoSegundos: deveria carregar e mostrar \"—\" (estado {legacy.state})");

            // Os quatro estados.
            SaveSlots.WriteTestSlot(1, "aldeia", when, 100f, true, RegionSceneName);
            SaveSlots.SlotInfo demo = SaveSlots.Inspect(1);
            Check(demo.state == SaveSlots.SlotState.DemoCompleted && !demo.CanLoad && SlotRows.Format(demo).Contains("Demo concluída"), "demo concluída: estado e texto");
            Broken(2);
            SaveSlots.SlotInfo broken = SaveSlots.Inspect(2);
            Check(broken.state == SaveSlots.SlotState.Unreadable && !broken.CanLoad && SlotRows.Format(broken).Contains("Dados do slot ilegíveis"), "ilegível: estado e texto");
            SaveSlots.Delete(3);
            SaveSlots.SlotInfo empty = SaveSlots.Inspect(3);
            Check(empty.state == SaveSlots.SlotState.Empty && SaveSlots.Info(3) == null && SlotRows.Format(empty).Contains("Vazio"), "vazio: estado e texto");
            ClearSlots();
        }

        static IEnumerator LatestPlayable()
        {
            yield return new Delay(0.1f);
            ClearSlots();
            string last = Path.Combine(SaveSlots.Folder, "ultimo_slot.json");
            if (File.Exists(last)) File.Delete(last);
            var t = new DateTime(2026, 10, 26, 10, 0, 0);

            FakeSlot(1, t);
            FakeSlot(2, t.AddHours(1), demo: true);
            Broken(3);
            Check(SaveSlots.LatestSlot() == 1 && SaveSlots.CanContinue, $"demo (mais nova) e ilegível não contam: deveria ser o 1, é {SaveSlots.LatestSlot()}");

            FakeSlot(2, t);
            Check(SaveSlots.LatestSlot() == 1, "empate de data: vence o menor número");
            FakeSlot(2, t.AddMinutes(1));
            Check(SaveSlots.LatestSlot() == 2, "o slot 2 é mais novo");
            FakeSlot(3, t.AddMinutes(2));
            Check(SaveSlots.LatestSlot() == 3, "o slot 3 é mais novo");
            Check(!File.Exists(last), "nada deveria gravar o ultimo_slot.json");

            // Sem jogável: Continuar some.
            FakeSlot(1, t, demo: true);
            Broken(2);
            SaveSlots.Delete(3);
            Check(SaveSlots.LatestSlot() == 0 && !SaveSlots.CanContinue && !SaveSlots.ContinueLatest(), "só demo e ilegível: sem \"Continuar\"");
            MainMenuUI menu = null;
            yield return OpenMenu(m => menu = m);
            Check(!menu.ShowsContinue, "o menu não deveria mostrar \"Continuar\" sem slot jogável");
            Check(menu.ShowsLoad, "o menu deveria mostrar \"Carregar\" (há slots a apagar)");

            ClearSlots();
            yield return OpenMenu(m => menu = m);
            Check(!menu.ShowsContinue && !menu.ShowsLoad, "sem nenhum arquivo: nem \"Continuar\" nem \"Carregar\"");

            FakeSlot(2, t);
            yield return OpenMenu(m => menu = m);
            Check(menu.ShowsContinue && menu.ShowsLoad, "com um slot jogável: \"Continuar\" e \"Carregar\"");
        }

        static IEnumerator SlotScreen()
        {
            yield return new Delay(0.1f);
            ClearSlots();
            var t = new DateTime(2026, 10, 26, 9, 0, 0);
            FakeSlot(2, t);
            Broken(3);
            MainMenuUI menu = null;
            yield return OpenMenu(m => menu = m);

            // Foco inicial.
            menu.OpenSlots(true);
            Check(menu.InSlotScreen && menu.IsNewGameScreen && menu.FocusedSlot == 1, $"Novo jogo: o foco deveria começar no primeiro slot vazio (1), está em {menu.FocusedSlot}");
            menu.OpenSlots(false);
            Check(menu.InSlotScreen && !menu.IsNewGameScreen && menu.FocusedSlot == 2, $"Carregar: o foco deveria começar no slot de \"Continuar\" (2), está em {menu.FocusedSlot}");
            Check(menu.ScreenText.Contains("Dados do slot ilegíveis") && menu.ScreenText.Contains("Vazio") && menu.ScreenText.Contains("Aldeia"), "a tela deveria mostrar os três estados");

            // Ilegível, demo e vazio não carregam, sem exceção nem troca de cena.
            int thrown = exceptions;
            menu.FocusSlot(3);
            menu.Press();
            yield return new Delay(0.4f);
            Check(menu.InSlotScreen && SceneManager.GetActiveScene().name == MenuSceneName, "o slot ilegível não deveria carregar");
            Check(File.Exists(SaveSlots.PathOf(3)), "o slot ilegível não deveria ser apagado");
            FakeSlot(1, t, demo: true);
            menu.OpenSlots(false);
            menu.FocusSlot(1);
            menu.Press();
            yield return new Delay(0.4f);
            Check(menu.InSlotScreen && SceneManager.GetActiveScene().name == MenuSceneName, "o slot \"demo concluída\" não deveria carregar");
            SaveSlots.Delete(1);
            menu.OpenSlots(false);
            menu.FocusSlot(1);
            menu.Press();
            yield return new Delay(0.4f);
            Check(menu.InSlotScreen && SceneManager.GetActiveScene().name == MenuSceneName, "o slot vazio não deveria carregar");
            Check(exceptions == thrown, "nenhuma exceção deveria chegar ao Console");

            // Apagar: sempre com confirmação, foco em Cancelar; o botão de voltar cancela.
            foreach (int slot in new[] { 3, 2 })
            {
                menu.OpenSlots(false);
                menu.FocusSlot(slot);
                menu.PressDelete();
                Check(menu.IsDialogOpen && menu.DialogFocusOnCancel, $"apagar o slot {slot}: a confirmação deveria abrir com o foco em Cancelar");
                menu.Press();
                Check(!menu.IsDialogOpen && File.Exists(SaveSlots.PathOf(slot)), $"apagar o slot {slot}: Cancelar não deveria apagar");
                menu.PressDelete();
                menu.PressBack();
                Check(!menu.IsDialogOpen && File.Exists(SaveSlots.PathOf(slot)) && menu.InSlotScreen, $"apagar o slot {slot}: o botão de voltar deveria fechar só o diálogo");
                menu.PressDelete();
                menu.Move(1);
                Check(!menu.DialogFocusOnCancel, "descer deveria passar o foco para o botão de apagar");
                menu.Press();
                Check(!File.Exists(SaveSlots.PathOf(slot)) && SaveSlots.Info(slot) == null && !menu.IsDialogOpen, $"apagar o slot {slot}: confirmar deveria remover o arquivo");
            }
            menu.FocusSlot(1);
            menu.PressDelete();
            Check(!menu.IsDialogOpen, "apagar um slot vazio não deveria pedir nada");
            menu.PressBack();
            Check(!menu.InSlotScreen, "o botão de voltar deveria sair da tela de slots");

            // Sobrescrever (Novo jogo em slot ocupado, demo e ilegível) pede confirmação, foco em Cancelar.
            FakeSlot(1, t);
            FakeSlot(2, t, demo: true);
            Broken(3);
            var before = new[] { File.ReadAllText(SaveSlots.PathOf(1)), File.ReadAllText(SaveSlots.PathOf(2)), File.ReadAllText(SaveSlots.PathOf(3)) };
            menu.OpenSlots(true);
            for (int slot = 1; slot <= 3; slot++)
            {
                menu.FocusSlot(slot);
                menu.Press();
                Check(menu.IsDialogOpen && menu.DialogFocusOnCancel, $"Novo jogo no slot {slot}: deveria pedir confirmação com o foco em Cancelar");
                menu.PressBack();
                Check(!menu.IsDialogOpen && File.ReadAllText(SaveSlots.PathOf(slot)) == before[slot - 1], $"Novo jogo no slot {slot}: voltar não deveria mexer no arquivo");
                menu.Press();
                menu.Press();
                Check(!menu.IsDialogOpen && File.ReadAllText(SaveSlots.PathOf(slot)) == before[slot - 1], $"Novo jogo no slot {slot}: Cancelar não deveria mexer no arquivo");
            }
            yield return new Delay(0.4f);
            Check(SceneManager.GetActiveScene().name == MenuSceneName, "cancelar não deveria sair do menu");

            // Confirmar sobrescreve só aquele slot e começa a partida.
            menu.FocusSlot(3);
            menu.Press();
            menu.Move(1);
            menu.Press();
            yield return WaitRegion();
            SaveSlots.SlotInfo info = SaveSlots.Info(3);
            Check(info != null && info.state == SaveSlots.SlotState.Occupied && info.area == SaveSlots.NewGameArea, "sobrescrever o slot ilegível deveria gravar o começo");
            Check(SaveSlots.CurrentSlot == 3, "a partida deveria gravar no slot 3");
            Check(File.ReadAllText(SaveSlots.PathOf(1)) == before[0] && File.ReadAllText(SaveSlots.PathOf(2)) == before[1], "os outros slots não deveriam mudar");
        }

        static IEnumerator ExitToMenu()
        {
            yield return new Delay(0.1f);
            ClearSlots();
            WorldState.Clear();
            string emptyWorld = WorldState.Describe(WorldState.Capture());
            MainMenuUI menu = null;
            yield return OpenMenu(m => menu = m);

            // Novo jogo em slot vazio começa na hora, sem confirmação; esta é a "sessão nova" de referência.
            menu.OpenSlots(true);
            menu.FocusSlot(3);
            menu.Press();
            Check(!menu.IsDialogOpen, "Novo jogo em slot vazio não deveria pedir confirmação");
            yield return WaitRegion();
            string fresh = Full();
            Vector3 start = Motor().transform.position;
            Check(SaveSlots.CurrentSlot == 3, "o Novo jogo deveria gravar no slot escolhido (3)");

            // Joga até um checkpoint real do slot 2.
            Prepare(5);
            SaveSlots.Attach(2);
            Cp("aldeia.checkpoint_patio").ActivateNow();
            Checkpoints.Data saved = Checkpoints.Last;
            string expectedWorld = WorldState.Describe(saved.world);
            yield return Reload(() => Checkpoints.Restart(RegionSceneName));
            string afterDeath = Full();
            // O checkpoint pode começar uma conversa (que pausa o tempo): o que vale é voltar igual a depois de morrer.
            float scaleAfterDeath = Time.timeScale;
            float savedTime = SaveSlots.Info(2).playSeconds;

            for (int round = 1; round <= 2; round++)
            {
                // Pausa e "Sair para o menu": confirmação com foco em Cancelar; cancelar fica no jogo.
                var pause = Object.FindFirstObjectByType<PauseMenuUI>();
                typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(pause, null);
                pause.RequestExitToMenu();
                Check(pause.ExitDialog.IsOpen && pause.ExitDialog.FocusOnCancel, "Sair para o menu: a confirmação deveria abrir com o foco em Cancelar");
                pause.ExitDialog.Handle(0, true, false);
                Check(!pause.ExitDialog.IsOpen && SceneManager.GetActiveScene().name == RegionSceneName, "Cancelar deveria ficar no jogo");
                pause.RequestExitToMenu();
                pause.ExitDialog.Handle(0, false, true);
                Check(!pause.ExitDialog.IsOpen && SceneManager.GetActiveScene().name == RegionSceneName, "o botão de voltar deveria cancelar");

                // Estado sujo de propósito, para ver se a saída limpa tudo.
                typeof(Storm).GetMethod("Set", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { null, 0.8f });
                PlayTime.Set(500f);
                AudioListener.pause = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                pause.RequestExitToMenu();
                pause.ExitDialog.Handle(1, false, false);
                Check(!pause.ExitDialog.FocusOnCancel, "descer deveria passar o foco para sair");
                pause.ExitDialog.Handle(0, true, false);
                yield return WaitMenu();

                Check(Mathf.Approximately(Time.timeScale, 1f), $"no menu o tempo deveria ser normal ({Time.timeScale})");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "no menu o cursor deveria estar livre");
                Check(!AudioListener.pause, "no menu o áudio não deveria estar pausado");
                Check(SaveSlots.CurrentSlot == 0, "no menu nenhum slot deveria estar em partida");
                Check(Checkpoints.Last == null, "no menu não deveria sobrar ponto de retorno");
                Same("mundo no menu", WorldState.Describe(WorldState.Capture()), emptyWorld);
                Check(Storm.Intensity == 0f && Storm.MoveSpeedMultiplier == 1f, "no menu não deveria sobrar tempestade");
                Check(Motor() == null, "no menu não deveria sobrar a Elena");
                Check(!PlayTime.Running && PlayTime.Seconds < 1f, $"no menu o relógio deveria estar parado e zerado ({PlayTime.Seconds:0.0} s)");
                menu = Object.FindFirstObjectByType<MainMenuUI>();
                Check(menu.ShowsContinue, "o slot 2 deveria deixar \"Continuar\" no menu");
                Check(Mathf.Abs(SaveSlots.Info(2).playSeconds - savedTime) < 1f, "sair para o menu não deveria regravar o slot 2");

                // Continuar (rodada 1) e Carregar (rodada 2) valem como uma sessão nova.
                if (round == 1)
                {
                    menu.Continue();
                }
                else
                {
                    menu.OpenSlots(false);
                    Check(menu.FocusedSlot == 2, "Carregar deveria começar no slot de \"Continuar\" (2)");
                    menu.Press();
                }
                yield return WaitRegion();
                string who = round == 1 ? "Continuar" : "Carregar";
                Same($"{who} depois de sair para o menu x voltar depois de morrer", Full(), afterDeath);
                Check(Near(Motor().transform.position, saved.position, 1f), $"{who}: a Elena deveria voltar ao checkpoint");
                Check(SaveSlots.CurrentSlot == 2, $"{who}: a partida deveria gravar no slot 2");
                Check(Mathf.Abs(PlayTime.Seconds - savedTime) < 2f, $"{who}: o tempo jogado deveria voltar com o slot ({PlayTime.Seconds:0.0} x {savedTime:0.0})");
                Check(Mathf.Approximately(Time.timeScale, scaleAfterDeath), $"{who}: o tempo deveria ser o de depois de morrer ({scaleAfterDeath}), é {Time.timeScale}");
            }

            // Sair uma última vez e começar um Novo jogo na mesma sessão: igual à sessão nova de referência.
            var lastPause = Object.FindFirstObjectByType<PauseMenuUI>();
            typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(lastPause, null);
            lastPause.RequestExitToMenu();
            lastPause.ExitDialog.Handle(1, false, false);
            lastPause.ExitDialog.Handle(0, true, false);
            yield return WaitMenu();
            menu = Object.FindFirstObjectByType<MainMenuUI>();
            SaveSlots.Delete(1);
            menu = null;
            yield return OpenMenu(m => menu = m);
            menu.OpenSlots(true);
            menu.FocusSlot(1);
            menu.Press();
            yield return WaitRegion();
            Same("Novo jogo depois de sair para o menu x Novo jogo de uma sessão nova", Full(), fresh);
            Check(Near(Motor().transform.position, start, 0.5f), "Novo jogo: a Elena deveria nascer no mesmo lugar da sessão nova");
            Check(SaveSlots.CurrentSlot == 1 && PlayTime.Seconds < 5f, "Novo jogo: slot 1 e relógio no começo");
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

        static IEnumerator SlotProtection()
        {
            var asked = new List<string>();
            bool answer = false;
            Func<string, string, bool> oldAsk = SlotGuard.Ask;
            SlotGuard.Ask = (title, message) =>
            {
                asked.Add(message);
                return answer;
            };
            SlotGuard.ForceReal = true; // a pasta do teste faz o papel dos slots reais
            try
            {
                SlotGuard.Run(false, () =>
                {
                    SlotShortcuts.ClearAll();

                    // Slot vazio grava direto, sem janela.
                    Check(SlotShortcuts.GuardedOccupied(1) != null, "slot vazio: deveria gravar");
                    Check(asked.Count == 0, "slot vazio: não deveria perguntar nada");
                    Check(SaveSlots.Inspect(1).state == SaveSlots.SlotState.Occupied, "slot vazio: deveria ficar ocupado");

                    // Slot ocupado: pergunta com número, estado, área e data; cancelar não grava.
                    string before = File.ReadAllText(SaveSlots.PathOf(1));
                    SaveSlots.SlotInfo info = SaveSlots.Inspect(1);
                    answer = false;
                    Check(SlotShortcuts.GuardedDemo(1) == null, "slot ocupado, cancelado: não deveria gravar");
                    Check(asked.Count == 1, $"slot ocupado: deveria perguntar uma vez, perguntou {asked.Count}");
                    string text = asked.Count > 0 ? asked[0] : "";
                    Check(text.Contains("Slot 1") && text.Contains("ocupado") && text.Contains(info.AreaName) && text.Contains(info.savedAt.ToString("dd/MM/yyyy")),
                        $"a janela deveria mostrar o número, o estado, a área e a data: \"{text}\"");
                    Check(File.ReadAllText(SaveSlots.PathOf(1)) == before, "cancelar não deveria mudar o arquivo");

                    // Confirmar grava.
                    answer = true;
                    Check(SlotShortcuts.GuardedDemo(1) != null, "slot ocupado, confirmado: deveria gravar");
                    Check(SaveSlots.Inspect(1).state == SaveSlots.SlotState.DemoCompleted, "confirmado: o slot deveria virar demo concluída");

                    // Demo concluída e ilegível também contam como ocupados.
                    asked.Clear();
                    answer = false;
                    Check(SlotShortcuts.GuardedOccupied(1) == null && asked.Count == 1 && asked[0].Contains("demo concluída"), "slot de demo concluída também deveria pedir confirmação");
                    File.WriteAllText(SaveSlots.PathOf(2), "{ ilegível");
                    asked.Clear();
                    Check(SlotShortcuts.GuardedOccupied(2) == null && asked.Count == 1 && asked[0].Contains("ilegível"), "slot ilegível também deveria pedir confirmação");

                    // Preencher, limpar e avançar o tempo: cancelar não mexe em nada.
                    string[] files = Enumerable.Range(1, 3).Select(n => File.Exists(SaveSlots.PathOf(n)) ? File.ReadAllText(SaveSlots.PathOf(n)) : null).ToArray();
                    Check(SlotShortcuts.GuardedFill() == null && SlotShortcuts.GuardedClear() == null && SlotShortcuts.GuardedAdvance(600f) == null, "cancelado: preencher, limpar e avançar não deveriam rodar");
                    for (int n = 1; n <= 3; n++)
                    {
                        string now = File.Exists(SaveSlots.PathOf(n)) ? File.ReadAllText(SaveSlots.PathOf(n)) : null;
                        Check(now == files[n - 1], $"cancelado: o slot {n} não deveria mudar");
                    }
                });

                // Execução automática: nunca toca em slot real, nem vazio, e a janela nunca abre.
                SlotGuard.Run(true, () =>
                {
                    asked.Clear();
                    answer = true;
                    string slot1 = File.ReadAllText(SaveSlots.PathOf(1));
                    Check(SlotShortcuts.GuardedOccupied(3) == null, "execução automática: não deveria gravar nem em slot vazio");
                    Check(SlotShortcuts.GuardedDemo(1) == null && SlotShortcuts.GuardedClear() == null && SlotShortcuts.GuardedFill() == null,
                        "execução automática: nenhuma ação deveria rodar");
                    Check(asked.Count == 0, "execução automática: nenhuma janela deveria abrir");
                    Check(!File.Exists(SaveSlots.PathOf(3)), "execução automática: o slot 3 deveria continuar vazio");
                    Check(File.ReadAllText(SaveSlots.PathOf(1)) == slot1, "execução automática: o slot 1 não deveria mudar");

                    // Pasta de teste não é slot real: o próprio teste continua podendo gravar nela.
                    SlotGuard.ForceReal = false;
                    Check(SlotShortcuts.GuardedOccupied(3) != null && asked.Count == 0, "pasta de teste: grava sem perguntar, mesmo em execução automática");
                });
            }
            finally
            {
                SlotGuard.Ask = oldAsk;
                SlotGuard.ForceReal = null;
            }
            yield break;
        }

        static IEnumerator AutomatedFlag()
        {
            bool original = SlotGuard.Automated;
            foreach (bool value in new[] { true, false })
            {
                bool threw = false;
                try
                {
                    SlotGuard.Run(value, () =>
                    {
                        Check(SlotGuard.Automated == value, $"dentro do Run({value}) o flag deveria ser {value}");
                        throw new InvalidOperationException("quebra de propósito");
                    });
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                Check(threw, "a exceção deveria passar por cima do Run");
                Check(SlotGuard.Automated == original, $"depois de um Run({value}) que quebrou o flag deveria voltar a {original}");
            }

            // Prazo vencido desliga sozinho; renovar mantém.
            SlotGuard.Run(false, () =>
            {
                SlotGuard.Begin(-1.0);
                Check(!SlotGuard.Automated, "com o prazo vencido o flag deveria estar desligado");
                SlotGuard.Begin(30.0);
                Check(SlotGuard.Automated, "dentro do prazo o flag deveria estar ligado");
                SlotGuard.End();
                SlotGuard.Renew();
                Check(!SlotGuard.Automated, "renovar não deveria religar um flag desligado");
            });
            Check(SlotGuard.Automated == original, "no fim o flag deveria estar como estava");
            yield break;
        }

        static IEnumerator Cleanup()
        {
            yield return new Delay(0.1f);
            Check(exceptions == 0, $"nenhuma exceção deveria chegar ao Console durante o teste ({exceptions})");
            Check(SlotGuard.RealFingerprint() == realSlots, "os 3 slots reais não deveriam mudar durante o teste");
            Application.logMessageReceived -= OnLog;
            if (!string.IsNullOrEmpty(previousLanguage)) Loc.SetLanguage(previousLanguage, false);
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

        static IEnumerator WaitMenu()
        {
            yield return new Until(() => SceneManager.GetActiveScene().name == MenuSceneName && Object.FindFirstObjectByType<MainMenuUI>() != null, 30f, "o menu principal abrir");
            int frame = Time.frameCount + 3;
            yield return new Until(() => Time.frameCount >= frame, 10f, "3 quadros");
        }

        static void ClearSlots()
        {
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++) SaveSlots.Delete(slot);
        }

        static void FakeSlot(int slot, DateTime when, bool demo = false, float seconds = 600f) =>
            SaveSlots.WriteTestSlot(slot, "aldeia", when, seconds, demo, RegionSceneName);

        static void Broken(int slot) => File.WriteAllText(SaveSlots.PathOf(slot), "{ isto não é um salvamento");

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
