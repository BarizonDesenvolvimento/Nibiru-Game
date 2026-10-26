using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Interaction;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 10. Roda os atalhos em sequência e confere o golpe
    /// pelas costas no líder e no atirador, a mesa da tenda (e-mail, foto, ponto de retorno,
    /// painel), o portão abrindo e a saída para o Poço. Fecha sozinho as leituras e as conversas.
    /// No fim, escreve um resumo curto no Console (OK ou FALHOU em cada item).
    /// Menu: Nibiru > Testes > Passo 10 > 9. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step10Tests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // Um teste: o atalho que põe a Elena no lugar e o que fazer depois, quadro a quadro.
        // O passo devolve true quando terminou.
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

        static Step10Tests()
        {
            Register("passo10.teste", Begin);
            checks.Add(new Check { name = "Chegada ao vivo, vindo do passo 9", moment = "passo9.muro", step = Arrival });
            checks.Add(new Check { name = "Rota alta caminhável até atrás do atirador", moment = "passo10.entrada", step = HighRoute });
            checks.Add(new Check { name = "Golpe pelas costas no líder", moment = "passo10.lider", step = t => Takedown("Lider_Tenda_Comando", false) });
            checks.Add(new Check { name = "Mesa da tenda: e-mail, foto, painel e ponto de retorno", moment = "passo10.tenda", step = Desk });
            checks.Add(new Check { name = "Painel abre o portão e libera a saída", moment = "passo10.painel", step = Gate });
            checks.Add(new Check { name = "Golpe pelas costas no atirador (rota alta)", moment = "passo10.rota_alta", step = t => Takedown("Atirador_De_Elite", true) });
            checks.Add(new Check { name = "Saída nordeste fecha o passo 10", moment = "passo10.saida", step = Exit });
        }

        [MenuItem("Nibiru/Testes/Passo 10/9. Teste automático (resumo no Console)")]
        static void Run() => Go("passo10.teste");

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
                Debug.Log("Passo 10, teste automático:\n" + report);
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
                if (CloseReadingAndDialogue()) return;
                checks[index].step(elapsed);
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}");
            }
        }

        // ---------- Os testes ----------

        static bool Takedown(string soldierName, bool teleportBehind)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var melee = player.GetComponent<PlayerMeleeAttack>();
            var target = GameObject.Find(soldierName);
            float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);

            if (phase == 1)
            {
                if (teleportBehind)
                {
                    Vector3 behind = target.transform.position - target.transform.forward * 1.2f;
                    behind.y = player.transform.position.y;
                    Teleport(behind, target.transform.eulerAngles.y);
                }
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (melee.TakedownTarget != null && !melee.IsAttacking)
                {
                    typeof(PlayerMeleeAttack).GetMethod("StartAttack", Any).Invoke(melee, new object[] { melee.TakedownTarget });
                    mark = Time.time;
                    phase = 3;
                }
                else if (elapsed > 6f) Result(false, "o aviso de golpe não apareceu");
                return false;
            }
            if (Time.time < mark + 1.5f) return false;

            var health = target.GetComponent<Health>();
            bool forward = target.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName(SoldierAnimator.ForwardDeathState);
            var alerted = new List<string>();
            foreach (var soldier in Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None))
                if (soldier.CurrentState == SoldierAI.State.Combate) alerted.Add(soldier.name);
            var sniper = target.GetComponent<SniperAI>();
            bool laserOff = sniper == null || !sniper.transform.Find("Mira_Laser").GetComponent<LineRenderer>().enabled;
            bool ok = health.IsDead && forward && alerted.Count == 0 && laserOff;
            Result(ok, $"morto={health.IsDead}, caiu de frente={forward}, em combate=[{string.Join(", ", alerted)}]" + (sniper != null ? $", laser apagado={laserOff}" : ""));
            return true;
        }

        static bool Desk(float elapsed)
        {
            if (phase == 1)
            {
                var desk = GameObject.Find("Examinar_Mesa");
                if (desk == null) { Result(false, "mesa não encontrada"); return true; }
                desk.GetComponent<InteractableEvent>().Interact(null);
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (!Raised("LeuOsDocumentosDoKast")) return false;
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (Time.time < mark + 0.5f) return false;
            bool ok = Raised("LeuOEmailDoKast") && Raised("ViuAFotoDe1979") && GameObject.Find("Usar_Painel") != null
                && GameObject.Find("Checkpoint_Tenda_Comando") != null && GameObject.Find("Examinar_Mesa") == null;
            Result(ok, $"e-mail={Raised("LeuOEmailDoKast")}, foto={Raised("ViuAFotoDe1979")}, painel aceso={GameObject.Find("Usar_Painel") != null}, " +
                $"ponto de retorno={GameObject.Find("Checkpoint_Tenda_Comando") != null}, objetivo=\"{Objective()}\"");
            return true;
        }

        static bool Gate(float elapsed)
        {
            var gate = GameObject.Find("Conteudo_Base/Portao_Aldeia/Portao");
            if (phase == 1)
            {
                GameObject.Find("Usar_Painel").GetComponent<InteractableEvent>().Interact(null);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 5f) return false;
            bool open = gate.transform.position.z > 14.5f;
            bool exit = GameObject.Find("Gatilho_Saida_Nordeste") != null;
            Result(open && exit, $"portão em z={gate.transform.position.z:0.0} (aberto em 15), saída liberada={exit}, objetivo=\"{Objective()}\"");
            return true;
        }

        static bool Exit(float elapsed)
        {
            if (phase == 1)
            {
                bool gateOpen = GameObject.Find("Conteudo_Base/Portao_Aldeia/Portao").transform.position.z > 14.5f;
                if (!gateOpen) { Result(false, "o portão devia estar aberto ao voltar do checkpoint"); return true; }
                Teleport(new Vector3(50f, 0.1f, 34f), 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 1f) return false;
            Result(Raised("SaiuDaBaseRumoAoPoco"), $"saiu={Raised("SaiuDaBaseRumoAoPoco")}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Do passo 9, a Elena entra pelo vão entre o muro e a cerca: a base acorda e ela comenta o laser.
        static bool Arrival(float elapsed)
        {
            if (phase == 1)
            {
                Teleport(new Vector3(-17.5f, 0.1f, -2f), 45f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            // A conversa fecha sozinha (CloseReadingAndDialogue); aqui só se confere se ela aconteceu.
            if (Time.time < mark + 3f) return false;
            var sniper = Object.FindFirstObjectByType<SniperAI>();
            bool active = GameObject.Find("Conteudo_Base/Base_Ativa") != null;
            bool talked = DialogueRunner.HasSeen(Load<DialogueDefinition>("Dialogues/Base_Chegada"));
            Result(Raised("EntrouNaBaseDaFundacao") && active && sniper != null && sniper.enabled && talked,
                $"entrou={Raised("EntrouNaBaseDaFundacao")}, base ativa={active}, atirador ligado={sniper != null && sniper.enabled}, fala da chegada={talked}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Altura da origem da Elena acima dos pés.
        const float FeetToCenter = 1.05f;

        // Anda com o colisor da Elena pela rota alta, trecho por trecho, e confere a altura no fim.
        static bool HighRoute(float elapsed)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            Vector3[] route =
            {
                new Vector3(-21.2f, 0f, -3f), new Vector3(-21.2f, 3f, 3.6f), new Vector3(-23f, 3f, 3.8f), new Vector3(-23f, 3f, 25f),
                new Vector3(-23f, 8f, 37f), new Vector3(-18f, 8f, 37.5f), new Vector3(-11f, 8f, 42.5f), new Vector3(-11f, 14f, 55.6f),
                new Vector3(-8f, 14f, 55.6f), new Vector3(-8f, 14f, 44.5f), new Vector3(14f, 14f, 44.5f),
            };
            Teleport(route[0] + Vector3.up * FeetToCenter, 0f);
            player.enabled = false;
            string stuck = null;
            for (int i = 1; i < route.Length && stuck == null; i++)
            {
                for (int s = 0; s < 400; s++)
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
            Result(stuck == null, stuck ?? $"chegou ao 2º terraço em {player.transform.position}");
            return true;
        }

        // ---------- Ajudas ----------

        // Fecha a tela de leitura e avança as conversas, como se o jogador apertasse Confirmar.
        static bool CloseReadingAndDialogue()
        {
            var reading = Object.FindFirstObjectByType<ReadingScreenUI>();
            if (reading != null && reading.IsOpen)
            {
                typeof(ReadingScreenUI).GetMethod("Close", Any).Invoke(reading, null);
                return true;
            }
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
                if (quest.kind == QuestKind.Principal) return step.LocalizedObjective;
            return "nenhum (missão principal concluída)";
        }
    }
}
