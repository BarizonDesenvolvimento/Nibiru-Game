using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Stealth;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 11. Roda os atalhos em sequência e confere a chegada
    /// à boca do Poço, a descida caminhável, o ponto de retorno em silêncio, o Lullu preso ao chão,
    /// a parede do barco de junco e o flash (com o Lullu vivo, que congela, e sem ele). Fecha sozinho
    /// as conversas. No fim, escreve um resumo curto no Console (OK ou FALHOU em cada item).
    /// Menu: Nibiru > Testes > Passo 11 > 7. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step11Tests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // Altura da origem da Elena acima dos pés.
        const float FeetToCenter = 1.05f;

        class Check
        {
            public string name, moment;
            public Func<float, bool> step;
        }

        static readonly List<Check> checks = new List<Check>();
        static readonly StringBuilder report = new StringBuilder();
        static int index, phase, oldPlayer;
        static double startedAt;
        static float mark;

        // Acompanhamento do Lullu nos testes.
        static LulluAI lullu;
        static float lulluStartY, lulluMaxY, lulluSilentDistance;
        static bool lulluHunted, lulluStruck, lulluFrozen;

        static Step11Tests()
        {
            Register("passo11.teste", Begin);
            checks.Add(new Check { name = "Corredor até a boca do Poço: fala e objetivo", moment = "passo11.corredor", step = Mouth });
            checks.Add(new Check { name = "Descida caminhável até o fundo", moment = "passo11.boca", step = Descent });
            checks.Add(new Check { name = "Ponto de retorno do pé da descida: o Lullu não ouve", moment = "passo11.pe", step = Silent });
            checks.Add(new Check { name = "Lullu preso ao chão (não sobe os andaimes)", moment = "passo11.pe", step = Grounded });
            checks.Add(new Check { name = "Parede do barco de junco fecha a passagem", moment = "passo11.depois_lullu", step = BoatWall });
            checks.Add(new Check { name = "Flash com o Lullu vivo: ele congela e depois solta", moment = "passo11.flash_vivo", step = t => Flash(true) });
            checks.Add(new Check { name = "Flash depois do Lullu (sem ele)", moment = "passo11.depois_lullu", step = t => Flash(false) });
        }

        [MenuItem("Nibiru/Testes/Passo 11/7. Teste automático (resumo no Console)")]
        static void Run() => Go("passo11.teste");

        static void Begin()
        {
            report.Clear();
            index = -1;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            NextCheck();
        }

        static void NextCheck()
        {
            index++;
            phase = 0;
            if (index >= checks.Count)
            {
                EditorApplication.update -= Tick;
                Debug.Log("Passo 11, teste automático:\n" + report);
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            Go(checks[index].moment);
        }

        static void Result(bool ok, string detail)
        {
            report.AppendLine($"{(ok ? "OK" : "FALHOU")}  {checks[index].name}: {detail}");
            NextCheck();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }
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
                    // Espera a cena recarregar com a Elena nova.
                    if (player == null || player.GetInstanceID() == oldPlayer || elapsed < 1.5f) return;
                    phase = 1;
                    startedAt = EditorApplication.timeSinceStartup;
                    return;
                }
                if (CloseDialogue()) return;
                checks[index].step(elapsed);
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}");
            }
        }

        // ---------- Os testes ----------

        // Chega à boca do poço vindo do corredor: o Poço acorda, a Elena fala e o objetivo muda.
        static bool Mouth(float elapsed)
        {
            if (phase == 1)
            {
                Teleport(new Vector3(50f, FeetToCenter, 60f), 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 3f) return false;
            bool active = GameObject.Find("Conteudo_Poco/Poco_Ativo") != null;
            bool talked = DialogueRunner.HasSeen(Load<DialogueDefinition>("Dialogues/Poco_Boca"));
            bool ok = active && Raised("ChegouABocaDoPoco") && talked && Objective().StartsWith("Desça o Poço");
            Result(ok, $"poço ativo={active}, chegou={Raised("ChegouABocaDoPoco")}, fala={talked}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Anda com o colisor da Elena pelos quatro lances de andaime, do alto até o chão do fundo.
        static bool Descent(float elapsed)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (phase == 1)
            {
                Vector3[] route =
                {
                    new Vector3(41f, 0f, 70f), new Vector3(41f, -3.5f, 78.5f), new Vector3(42f, -3.5f, 79f), new Vector3(48.5f, -7f, 79f),
                    new Vector3(49f, -7f, 78f), new Vector3(49f, -10.5f, 71.5f), new Vector3(48f, -10.5f, 71f), new Vector3(41.5f, -14f, 71f),
                    new Vector3(44f, -14f, 73.5f),
                };
                Teleport(route[0] + Vector3.up * FeetToCenter, 0f);
                player.enabled = false;
                string stuck = null;
                for (int i = 1; i < route.Length && stuck == null; i++)
                {
                    for (int s = 0; s < 500; s++)
                    {
                        Vector3 flat = Vector3.ProjectOnPlane(route[i] - player.transform.position, Vector3.up);
                        if (flat.magnitude < 0.3f) break;
                        controller.Move(flat.normalized * 0.1f + Vector3.down * 0.1f);
                    }
                    Vector3 at = player.transform.position;
                    if (Vector3.ProjectOnPlane(route[i] - at, Vector3.up).magnitude > 0.6f || Mathf.Abs(at.y - FeetToCenter - route[i].y) > 1.2f)
                        stuck = $"parou em {at} a caminho de {route[i]}";
                }
                player.enabled = true;
                if (stuck != null)
                {
                    Result(false, stuck);
                    return true;
                }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 1f) return false;
            Result(Raised("DesceuOPoco"), $"chegou ao fundo em {player.transform.position}, desceu={Raised("DesceuOPoco")}, objetivo=\"{Objective()}\"");
            return true;
        }

        // O ponto de retorno do pé da descida fica fora da audição do Lullu: parada ali, ele segue vagando.
        static bool Silent(float elapsed)
        {
            if (phase == 1)
            {
                lullu = Object.FindFirstObjectByType<LulluAI>();
                if (lullu == null) { Result(false, "Lullu não encontrado"); return true; }
                var player = Object.FindFirstObjectByType<PlayerMotor>();
                lulluSilentDistance = Vector3.Distance(player.transform.position, lullu.transform.position);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 4f) return false;
            // O passo mais alto que a Elena faz sem agachar chega a 6 m (5 m de passo vezes 1,2 do Lullu).
            bool ok = lullu.CurrentState == LulluAI.State.Vagando && lulluSilentDistance > 6f;
            Result(ok, $"distância={lulluSilentDistance:0.0} m (passo audível até 6 m), Lullu={lullu.CurrentState}");
            return true;
        }

        // Um barulho forte vindo do andaime: o Lullu caça, mas pelo chão, e não alcança a Elena no alto.
        static bool Grounded(float elapsed)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                lullu = Object.FindFirstObjectByType<LulluAI>();
                if (lullu == null) { Result(false, "Lullu não encontrado"); return true; }
                lulluStartY = lullu.transform.position.y;
                lulluMaxY = lulluStartY;
                lulluHunted = lulluStruck = false;
                Teleport(new Vector3(49f, -7f + FeetToCenter, 79f), 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }

            lulluMaxY = Mathf.Max(lulluMaxY, lullu.transform.position.y);
            lulluHunted |= lullu.CurrentState == LulluAI.State.Cacando || lullu.CurrentState == LulluAI.State.Investigando;
            lulluStruck |= lullu.CurrentState == LulluAI.State.Golpe;
            if (Time.time < mark + 7f)
            {
                if (Mathf.Repeat(Time.time - mark, 0.5f) < Time.deltaTime + 0.02f)
                    Noise.Emit(player.transform.position, 25f, player.gameObject);
                return false;
            }
            bool grounded = lulluMaxY < lulluStartY + 1f;
            var flat = Vector3.ProjectOnPlane(lullu.transform.position - player.transform.position, Vector3.up).magnitude;
            Result(grounded && lulluHunted && !lulluStruck,
                $"caçou={lulluHunted}, golpeou={lulluStruck}, subiu {lulluMaxY - lulluStartY:0.00} m, ficou a {flat:0.0} m (em planta) da Elena, que estava 7 m acima");
            return true;
        }

        // A parede do barco fecha o vão do túnel: a Elena anda para o oeste e para na parede.
        static bool BoatWall(float elapsed)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (GameObject.Find("Parede_Barco_Junco") == null) { Result(false, "a parede não está na cena (rode o blockout)"); return true; }
            Teleport(new Vector3(40f, -14f + FeetToCenter, 96f), 270f);
            player.enabled = false;
            for (int s = 0; s < 150; s++) controller.Move(Vector3.left * 0.1f + Vector3.down * 0.1f);
            float x = player.transform.position.x;
            player.enabled = true;
            Result(x > 36.2f, $"parou em x={x:0.0} (a parede fica em x = 36)");
            return true;
        }

        // O flash a uns 12 m do Selo. Com o Lullu vivo, ele congela nesse tempo e solta no fim.
        static bool Flash(bool withLullu)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                lullu = Object.FindFirstObjectByType<LulluAI>();
                if (withLullu && lullu == null) { Result(false, "Lullu não encontrado"); return true; }
                if (!withLullu && lullu != null) { Result(false, "o Lullu devia ter sumido depois de passar por ele"); return true; }
                // O atalho põe a Elena antes da área do flash: se ele já começou, foi antes da hora.
                if (Raised("ChegouPertoDoSelo")) { Result(false, $"o flash começou sozinho no ponto do atalho, em {player.transform.position}"); return true; }
                lulluFrozen = false;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                // "Passou pelo Lullu" vem do atalho, logo depois de recarregar.
                if (!Raised("PassouPeloLullu")) return false;
                Teleport(new Vector3(45f, -14f + FeetToCenter, 90.6f), 0f);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                // Esta etapa só roda enquanto não há conversa: a espera de 1,4 s antes da Voz.
                if (Raised("ChegouPertoDoSelo") && !Raised("FlashDoPoco")) lulluFrozen |= lullu != null && !lullu.enabled;
                if (Raised("FlashDoPoco")) { mark = Time.time; phase = 4; }
                else if (Time.time > mark + 20f) Result(false, "o flash não terminou");
                return false;
            }
            if (Time.time < mark + 3f) return false;

            bool released = lullu == null || lullu.enabled;
            var flash = Object.FindFirstObjectByType<ScreenFlash>();
            bool cleared = flash != null && !flash.IsCovering;
            bool seen = DialogueRunner.HasSeen(Load<DialogueDefinition>("Dialogues/Poco_Flash"));
            bool ok = Raised("FlashDoPoco") && seen && released && cleared && (!withLullu || lulluFrozen);
            Result(ok, $"Voz falou={seen}, fim do passo={Raised("FlashDoPoco")}, Lullu congelado no flash={(withLullu ? lulluFrozen.ToString() : "n/a")}, " +
                $"solto no fim={released}, luz apagada={cleared}, objetivo=\"{Objective()}\"");
            return true;
        }

        // ---------- Ajudas ----------

        // Avança as conversas, como se o jogador apertasse Confirmar.
        static bool CloseDialogue()
        {
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner != null && runner.IsRunning)
            {
                runner.Continue();
                return true;
            }
            return false;
        }

        static bool Raised(string eventName) => StoryEvents.HasBeenRaised(Load<StoryEvent>("Events/" + eventName));

        static string Objective()
        {
            var quests = Object.FindFirstObjectByType<QuestManager>();
            foreach (var (quest, step) in quests.ActiveObjectives())
                if (quest.kind == QuestKind.Principal) return step.objective;
            return "nenhum (missão principal concluída)";
        }
    }
}
