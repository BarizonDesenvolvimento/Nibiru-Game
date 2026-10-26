using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.Dialogue;
using Nibiru.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste geral dos atalhos de Nibiru > Testes. Roda cada atalho registrado em
    /// <see cref="TestShortcuts"/> (os novos entram sozinhos), espera o mundo se arrumar e confere que a
    /// Elena está na malha de navegação (pé a menos de 0,8 m dela), apoiada no chão e sem cair (não
    /// perde mais de 1 m de altura em 1,5 s). Os atalhos da região rodam numa partida só; os do
    /// laboratório, numa segunda. Atalhos que não põem a Elena andando num lugar (testes automáticos,
    /// vitrines, a viagem presa na caçamba e a memória de Shuruppak) são pulados, com o motivo no resumo.
    /// Antes dos atalhos, roda o teste de códigos do salvamento (sem Play), para que um código renomeado ou apagado
    /// seja sempre acusado; ele conta como um item a mais no resumo.
    /// No fim, escreve um resumo no Console (OK, FALHOU ou PULADO em cada atalho).
    /// Menu: Nibiru > Testes > Teste geral dos atalhos (Elena na malha).
    /// </summary>
    [InitializeOnLoad]
    static class ShortcutsCheck
    {
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        const string ReportKey = "Nibiru.ShortcutsCheck.Report";
        const string CountsKey = "Nibiru.ShortcutsCheck.Counts";

        // Quanto o pé da Elena pode estar longe da malha de navegação, e o quanto ela pode descer em 1,5 s.
        const float MeshTolerance = 0.8f;
        const float MaxDrop = 1f;
        const float AbsoluteFloor = -30f;
        const float SettleSeconds = 2f;
        const float WatchSeconds = 1.5f;

        // Atalhos que abrem o laboratório (outra cena, outra partida).
        static readonly string[] LabMoments = { "elena.laboratorio", "soldados.laboratorio", "samir.laboratorio" };

        static List<string> queue;
        static bool labPass;
        static int index, phase, oldPlayer, ok, failed, skipped, tick;
        static double startedAt;
        static float mark, startY, minY;
        static bool grounded;
        static StringBuilder report;

        static ShortcutsCheck()
        {
            Register("geral.regiao", () => StartPass(false));
            Register("geral.laboratorio", () => StartPass(true));
        }

        [MenuItem("Nibiru/Testes/Teste geral dos atalhos (Elena na malha)")]
        static void Run()
        {
            // Primeiro os códigos do salvamento (sem Play).
            (bool codesOk, string codes) = Saving.SaveCodes.RunTest();
            string text = codesOk ? "OK  códigos do salvamento (lista de códigos conhecidos)\n" : "FALHOU  códigos do salvamento:\n" + codes + "\n";
            // Os atalhos da tela de slots (slot falso, demo, ilegível, preencher, limpar, avançar o tempo), numa pasta de teste.
            (bool slotsOk, string slots) = Saving.SlotShortcuts.RunTest();
            text += slotsOk ? "OK  atalhos da tela de slots (ocupado, demo, ilegível, preencher, limpar, avançar o tempo)\n"
                : "FALHOU  atalhos da tela de slots:\n" + slots + "\n";
            SessionState.SetString(ReportKey, text);
            SessionState.SetString(CountsKey, $"{(codesOk ? 1 : 0) + (slotsOk ? 1 : 0)},{(codesOk ? 0 : 1) + (slotsOk ? 0 : 1)},0");
            Go("geral.regiao");
        }

        // Por que um atalho não entra na conferência (null = entra).
        static string SkipReason(string id)
        {
            if (id.StartsWith("geral.")) return "é este teste";
            if (id.StartsWith("salvamento.")) return "carrega um slot do disco (o teste do salvamento cobre)";
            if (id.Contains(".teste")) return "é um teste automático, não um lugar";
            if (id.StartsWith("animacoes.") || id.EndsWith(".vitrine")) return "vitrine de animações, num chão sem malha";
            if (id.StartsWith("passo15.meio_") || id == "passo15.chegada" || id.StartsWith("remedio.viagem"))
                return "a Elena viaja presa na caçamba, numa estrada de trilho sem malha";
            if (id == "passo8.memoria" || id == "passo8.cabana") return "a Elena é Ziusudra na cena da memória, sem malha";
            return null;
        }

        static void StartPass(bool lab)
        {
            labPass = lab;
            report = new StringBuilder(SessionState.GetString(ReportKey, ""));
            int[] counts = SessionState.GetString(CountsKey, "0,0,0").Split(',').Select(int.Parse).ToArray();
            ok = counts[0];
            failed = counts[1];
            skipped = counts[2];

            queue = new List<string>();
            foreach (string id in MomentIds)
            {
                string why = SkipReason(id);
                if (why != null)
                {
                    if (!lab)
                    {
                        report.AppendLine($"PULADO  {id}: {why}");
                        skipped++;
                    }
                    continue;
                }
                if (LabMoments.Contains(id) == lab) queue.Add(id);
            }

            index = -1;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Next();
        }

        static void Next()
        {
            index++;
            phase = 0;
            tick = 0;
            if (index >= queue.Count)
            {
                Finish();
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            if (labPass) Go(queue[index], LabScenePath);
            else Go(queue[index]);
        }

        static void Result(bool pass, string detail)
        {
            report.AppendLine($"{(pass ? "OK" : "FALHOU")}  {queue[index]}: {detail}");
            if (pass) ok++;
            else failed++;
            Next();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }
            tick++;
            float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
            if (elapsed > 40f)
            {
                Result(false, $"tempo esgotado (fase {phase})");
                return;
            }
            try
            {
                var player = Object.FindFirstObjectByType<PlayerMotor>();
                if (phase == 0)
                {
                    // Espera a cena recarregar com a Elena nova (se o atalho não recarrega, segue depois de 6 s).
                    bool reloaded = player != null && player.GetInstanceID() != oldPlayer && elapsed >= 1.5f;
                    if (!reloaded && !(elapsed > 6f && player != null)) return;
                    phase = 1;
                    mark = Time.time;
                    return;
                }
                if (CloseDialogue()) return;
                if (player == null)
                {
                    Result(false, "a Elena não está na cena");
                    return;
                }
                var controller = player.GetComponent<CharacterController>();
                if (phase == 1)
                {
                    if (Time.time < mark + SettleSeconds) return;
                    startY = minY = player.transform.position.y;
                    grounded = false;
                    mark = Time.time;
                    phase = 2;
                    return;
                }
                minY = Mathf.Min(minY, player.transform.position.y);
                grounded |= controller.isGrounded;
                if (Time.time < mark + WatchSeconds) return;
                Evaluate(player, controller);
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}");
            }
        }

        static void Evaluate(PlayerMotor player, CharacterController controller)
        {
            Vector3 position = player.transform.position;
            Vector3 feet = new Vector3(position.x, controller.bounds.min.y, position.z);
            bool onMesh = NavMesh.SamplePosition(feet, out NavMeshHit hit, MeshTolerance, NavMesh.AllAreas);
            float meshDistance = onMesh ? Vector3.Distance(feet, hit.position) : -1f;
            float drop = startY - minY;
            bool fell = drop > MaxDrop || position.y < AbsoluteFloor;
            var health = player.GetComponent<Combat.Health>();
            bool dead = health != null && health.IsDead;

            var problems = new List<string>();
            if (!onMesh) problems.Add($"fora da malha de navegação (nada a {MeshTolerance:0.0} m do pé)");
            if (fell) problems.Add($"caiu {drop:0.0} m");
            if (!grounded) problems.Add("não está apoiada no chão");
            if (dead) problems.Add("está morta");

            string where = $"pé em ({feet.x:0.0}; {feet.y:0.0}; {feet.z:0.0})";
            if (problems.Count == 0) Result(true, $"{where}, a {meshDistance:0.00} m da malha");
            else Result(false, $"{where}: {string.Join(", ", problems)}");
        }

        // Avança as conversas, como se o jogador apertasse Confirmar (e, se parar numa escolha, a primeira),
        // e fecha a tela de leitura (tabuleta, documento), que também pausa o jogo.
        static bool CloseDialogue()
        {
            var reading = Object.FindFirstObjectByType<UI.ReadingScreenUI>();
            if (reading != null && reading.IsOpen)
            {
                TestFlow.CloseReading();
                return true;
            }
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner == null || !runner.IsRunning) return false;
            if (tick % 2 == 0) runner.Continue();
            else runner.Choose(0);
            return true;
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetString(ReportKey, report.ToString());
            SessionState.SetString(CountsKey, $"{ok},{failed},{skipped}");

            bool labNeeded = !labPass && LabMoments.Any(id => MomentIds.Contains(id) && SkipReason(id) == null);
            if (labNeeded)
            {
                // Os atalhos do laboratório pedem outra cena: sai do Play e liga o Play nela.
                EditorApplication.isPlaying = false;
                EditorApplication.update += WaitForEditMode;
                return;
            }

            Debug.Log($"Teste geral dos atalhos: {ok} OK, {failed} FALHOU, {skipped} pulados\n{report}");
            EditorApplication.isPlaying = false;
            if (labPass) EditorApplication.update += ReopenRegion;
        }

        static void WaitForEditMode()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= WaitForEditMode;
            Go("geral.laboratorio", LabScenePath);
        }

        static void ReopenRegion()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= ReopenRegion;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != RegionScenePath) EditorSceneManager.OpenScene(RegionScenePath);
        }
    }
}
