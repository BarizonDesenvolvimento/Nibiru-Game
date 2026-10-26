using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using Nibiru.AI;
using Nibiru.Save;
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
            checks.Add(new Check { name = "2º líder: ID estável, na malha, ronda e rota livre com timing", moment = "passo10.lider_leste", step = EastLeader });
            checks.Add(new Check { name = "Helicóptero: Elena coberta no gatilho não é avistada", moment = "passo10.conteiner_gatilho", step = HeliCovered });
            checks.Add(new Check { name = "Helicóptero: o gatilho do começo da avenida dispara a passagem", moment = "passo10.gatilho_avenida", step = t => HeliTrigger("Gatilho_Cobertura_Avenida", new Vector3(-11f, 0.1f, 8.5f)) });
            checks.Add(new Check { name = "Helicóptero: o gatilho do pé da escada da rota alta dispara a passagem", moment = "passo10.gatilho_escada", step = t => HeliTrigger("Gatilho_Cobertura_Escada", new Vector3(-20.6f, 0.1f, -2f)) });
            checks.Add(new Check { name = "Helicóptero: o gatilho do corredor dos contêineres dispara a passagem", moment = "passo10.gatilho_avenida", step = t => HeliTrigger("Gatilho_Cobertura_Conteiner", new Vector3(14f, 0.1f, -2.5f)) });
            checks.Add(new Check { name = "Helicóptero: cruzando dois gatilhos, a passagem dispara uma vez só", moment = "passo10.gatilho_avenida", step = HeliOnce });
            checks.Add(new Check { name = "Helicóptero: Elena exposta na avenida é avistada e o atirador fica de fora", moment = "passo10.conteiner_gatilho", step = HeliExposed });
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

        // ---------- 2º líder e helicóptero ----------

        static Vector3 leaderStart;

        static bool EastLeader(float elapsed)
        {
            var leader = GameObject.Find("Lider_Leste_Heliponto");
            if (leader == null) { Result(false, "2º líder não encontrado"); return true; }
            if (phase == 1)
            {
                leaderStart = leader.transform.position;
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 10f) return false;

            var id = leader.GetComponent<PersistentId>();
            bool stableId = id != null && id.Id == "base.lider_leste_heliponto";
            var ai = leader.GetComponent<SoldierAI>();
            bool moved = Vector3.Distance(leaderStart, leader.transform.position) > 1f;
            var agent = leader.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool onMesh = agent != null && agent.enabled && agent.isOnNavMesh;

            // A rota por baixo segue livre: há caminho da tenda de comando à saída, e com ele no ponto
            // sul da ronda (olhando para o sul) o lado oeste do corredor fica às costas dele.
            var path = new UnityEngine.AI.NavMeshPath();
            bool route = UnityEngine.AI.NavMesh.CalculatePath(new Vector3(24f, 0f, 7f), new Vector3(50f, 0f, 34f), UnityEngine.AI.NavMesh.AllAreas, path)
                && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
            Vector3 toEdge = new Vector3(44f, 0f, 24f) - new Vector3(53f, 0f, 9f);
            float angleSouthEnd = Vector3.Angle(Vector3.back, toEdge);
            var settings = Load<SoldierSettings>("AI/LiderEsquadrao");
            bool timing = angleSouthEnd > settings.viewAngle * 0.5f;

            Result(stableId && ai != null && ai.IsLeader && ai.enabled && moved && onMesh && route && timing,
                $"id estável={stableId}, líder={ai != null && ai.IsLeader}, andou={moved}, na malha={onMesh}, caminho tenda-saída={route}, " +
                $"ponto da saída fora do cone no sul da ronda={timing} ({angleSouthEnd:0}° contra {settings.viewAngle * 0.5f:0}°)");
            return true;
        }

        static HelicopterSpotter Spotter() =>
            Object.FindObjectsByType<HelicopterSpotter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(s => s.name == "Helicoptero_Passagem");

        static bool Spotted(HelicopterSpotter spotter) =>
            (bool)typeof(HelicopterSpotter).GetField("spotted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spotter);

        // Sem soldados atirando na Elena durante o teste (só o helicóptero é testado).
        static void SilenceGuards(bool perceptionOnly)
        {
            foreach (var p in Object.FindObjectsByType<SoldierPerception>(FindObjectsSortMode.None)) p.enabled = false;
            foreach (var s in Object.FindObjectsByType<SniperAI>(FindObjectsSortMode.None)) s.enabled = false;
            if (!perceptionOnly)
                foreach (var a in Object.FindObjectsByType<SoldierAI>(FindObjectsSortMode.None)) a.enabled = false;
        }

        static GameObject ActivePassHelicopter() =>
            Object.FindObjectsByType<HelicopterSpotter>(FindObjectsSortMode.None).Select(s => s.gameObject).FirstOrDefault(g => g.name == "Helicoptero_Passagem");

        // Entra no gatilho de uma rota (a partir de fora) e confere que a passagem começou. O evento não
        // pode ter sido levantado antes, e a Elena sozinha no gatilho é o que o dispara.
        static bool HeliTrigger(string zoneName, Vector3 inside)
        {
            if (phase == 1)
            {
                SilenceGuards(perceptionOnly: false);
                if (Raised("ChegouAoFimDaCoberturaDaBase") || ActivePassHelicopter() != null || GameObject.Find(zoneName) == null)
                { Result(false, $"estado inicial errado (evento levantado={Raised("ChegouAoFimDaCoberturaDaBase")}, gatilho existe={GameObject.Find(zoneName) != null})"); return true; }
                Teleport(inside, 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 2.5f) return false;
            var heli = ActivePassHelicopter();
            bool raised = Raised("ChegouAoFimDaCoberturaDaBase");
            bool moving = heli != null && heli.transform.position.x < 169f;
            Result(raised && moving, $"evento={raised}, helicóptero em x={(heli != null ? heli.transform.position.x : float.NaN):0}");
            return true;
        }

        // A Elena cruza o gatilho da avenida e, em seguida, o do corredor e o da escada: só uma passagem,
        // que não recomeça (o helicóptero só avança) e termina com o evento da passagem.
        static bool HeliOnce(float elapsed)
        {
            if (phase == 1)
            {
                SilenceGuards(perceptionOnly: false);
                Teleport(new Vector3(-11f, 0.1f, 8.5f), 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            var heli = ActivePassHelicopter();
            if (phase == 2)
            {
                if (Time.time < mark + 3f) return false;
                if (heli == null) { Result(false, "a passagem não começou no primeiro gatilho"); return true; }
                leaderStart = heli.transform.position;
                Teleport(new Vector3(14f, 0.1f, -2.5f), 0f);
                phase = 3;
                mark = Time.time;
                return false;
            }
            if (phase == 3)
            {
                if (Time.time < mark + 1.5f) return false;
                Teleport(new Vector3(-20.6f, 0.1f, -2f), 0f);
                phase = 4;
                mark = Time.time;
                return false;
            }
            if (Time.time < mark + 1.5f) return false;
            int helis = Object.FindObjectsByType<HelicopterSpotter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(s => s.name == "Helicoptero_Passagem");
            bool restarted = heli != null && heli.transform.position.x > leaderStart.x + 0.5f;
            Result(!restarted && helis == 1, $"helicópteros={helis}, recomeçou={restarted} (x {leaderStart.x:0} → {(heli != null ? heli.transform.position.x : float.NaN):0})");
            return true;
        }

        static bool HeliCovered(float elapsed)
        {
            if (phase == 1)
            {
                SilenceGuards(perceptionOnly: false);
                phase = 2;
                return false;
            }
            var spotter = Spotter();
            if (spotter == null) { Result(false, "helicóptero da passagem não encontrado"); return true; }
            if (phase == 2)
            {
                // A Elena já nasce dentro do gatilho; se o evento não disparou, sai e entra de novo.
                if (Raised("ChegouAoFimDaCoberturaDaBase")) { phase = 3; return false; }
                if (elapsed > 3f) { Teleport(new Vector3(14f, 0.1f, -2.5f), 0f); phase = 3; }
                return false;
            }
            if (Spotted(spotter)) { Result(false, "avistou a Elena atrás do contêiner"); return true; }
            if (!Raised("HelicopteroPassouNaBase")) return false;
            Result(true, $"o helicóptero cruzou a base (evento HelicopteroPassouNaBase) sem avistar a Elena coberta");
            return true;
        }

        static bool HeliExposed(float elapsed)
        {
            if (phase == 1)
            {
                SilenceGuards(perceptionOnly: true);
                phase = 2;
                return false;
            }
            var spotter = Spotter();
            if (spotter == null) { Result(false, "helicóptero da passagem não encontrado"); return true; }
            if (phase == 2)
            {
                if (!Raised("ChegouAoFimDaCoberturaDaBase") && elapsed > 3f) Teleport(new Vector3(14f, 0.1f, -2.5f), 0f);
                // Quando o helicóptero chega perto, a Elena pisa na avenida, a céu aberto.
                if (spotter.gameObject.activeInHierarchy && spotter.transform.position.x < 40f)
                {
                    Teleport(new Vector3(8f, 0.1f, 8.5f), 0f);
                    phase = 3;
                }
                return false;
            }
            if (!Spotted(spotter)) return false;

            var alerted = SoldierAI.All.Where(s => s.CurrentState == SoldierAI.State.Combate).Select(s => s.name).ToList();
            bool sniperCalled = SoldierAI.All.Any(s => s.GetComponent<SniperAI>() != null);
            Result(alerted.Count > 0 && !sniperCalled, $"avistou a Elena na avenida; em combate=[{string.Join(", ", alerted)}], atirador na lista do helicóptero={sniperCalled}");
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
