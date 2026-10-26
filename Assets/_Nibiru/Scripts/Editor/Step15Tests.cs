using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Factions;
using Nibiru.Interaction;
using Nibiru.Inventory;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Sequences;
using Nibiru.Tablets;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 15 (a fuga na caminhonete). Confere, para cada uma das três
    /// escolhas do wadi, que a fuga segue o ramo certo (objetivo, Samir, conversa "A estrada", nicho do túnel
    /// para quem devolve); que morrer e voltar ao ponto de retorno mantém a escolha; que a viagem dura pelo
    /// menos 60 s, espera a última fala sem cortá-la, prende a Elena na caminhonete e congela na pausa; e
    /// que o fim da viagem leva ao diário, ao FimDaFatia, à tela de fim da demo e ao menu principal.
    /// No fim, escreve um resumo curto no Console (OK ou FALHOU).
    /// Menu: Nibiru > Testes > Passo 15 > 9. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step15Tests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        class Check
        {
            public string name, moment;
            public Func<bool> step;
            public float limit = 100f;
        }

        static readonly List<Check> checks = new List<Check>();
        static readonly StringBuilder report = new StringBuilder();
        static readonly Dictionary<string, float> raisedAt = new Dictionary<string, float>();
        static int index, phase, oldPlayer;
        static double startedAt;
        static float mark, lastTick;
        static bool hooked;
        static string detail;
        static int baseFoundation, baseGuardians, baseTransformation, linesSeen;
        static Vector3 truckBefore;
        static float progressBefore, pauseBefore;
        static bool pauseDone;
        static string pauseNote = "";

        static float Now => Time.realtimeSinceStartup;

        static Step15Tests()
        {
            Register("passo15.teste", Begin);

            checks.Add(new Check
            {
                name = "Ficou com a tabuleta: ramo certo, morrer volta ao embarque com a escolha, a conversa mais longa que o trilho espera a última fala",
                moment = "passo15.inicio_ficou", step = () => Pick(Step15Shortcuts.Pick.Ficou, fullLength: false), limit = 120f,
            });
            checks.Add(new Check
            {
                name = "Entregou a tabuleta: ramo certo, morrer volta ao embarque com a escolha, a viagem dura 60 s, a pausa congela o trilho",
                moment = "passo15.inicio_entregou", step = () => Pick(Step15Shortcuts.Pick.Entregou, fullLength: true), limit = 190f,
            });
            checks.Add(new Check
            {
                name = "Devolve ao Poço: nicho do túnel, tabuleta enterrada, volta ao ponto de retorno, viagem, diário, fim da demo e menu principal",
                moment = "passo15.nicho", step = Devolveu, limit = 160f,
            });
        }

        [MenuItem("Nibiru/Testes/Passo 15/9. Teste automático (resumo no Console)")]
        static void Run() => Go("passo15.teste");

        static void Begin()
        {
            report.Clear();
            index = -1;
            if (!hooked)
            {
                StoryEvents.Raised += e => raisedAt[e.Id] = Time.realtimeSinceStartup;
                hooked = true;
            }
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
                Debug.Log("Passo 15, teste automático:\n" + report);
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            raisedAt.Clear();
            pauseDone = false;
            pauseNote = "";
            Go(checks[index].moment);
        }

        static void Result(bool ok, string text)
        {
            report.AppendLine($"{(ok ? "OK" : "FALHOU")}  {checks[index].name}: {text}");
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
            if (elapsed > checks[index].limit)
            {
                Result(false, $"tempo esgotado (fase {phase}; {detail})");
                return;
            }
            try
            {
                var player = Object.FindFirstObjectByType<PlayerMotor>();
                if (phase == 0)
                {
                    if (player == null || player.GetInstanceID() == oldPlayer || elapsed < 1.5f) return;
                    phase = 1;
                    startedAt = EditorApplication.timeSinceStartup;
                    return;
                }
                checks[index].step();
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }

        // ---------- Os ramos Ficou e Entregou ----------

        static bool Pick(Step15Shortcuts.Pick pick, bool fullLength)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            string choiceEvent = pick == Step15Shortcuts.Pick.Ficou ? "FicouComATabuleta" : "EntregouATabuleta";
            string otherEvent = pick == Step15Shortcuts.Pick.Ficou ? "EntregouATabuleta" : "FicouComATabuleta";
            string dialogueName = pick == Step15Shortcuts.Pick.Ficou ? "Fuga_Estrada_Ficou" : "Fuga_Estrada_Entregou";
            int wantFoundation = pick == Step15Shortcuts.Pick.Ficou ? -5 : 15;
            int wantGuardians = pick == Step15Shortcuts.Pick.Ficou ? -5 : -15;

            if (phase == 1)
            {
                baseFoundation = Reputation.Get(Load<FactionDefinition>("Factions/FundacaoEnlil"));
                baseGuardians = Reputation.Get(Load<FactionDefinition>("Factions/Guardioes"));
                baseTransformation = WorldState.Transformation;
                bool ok = Raised(choiceEvent) && !Raised(otherEvent) && !Raised("DevolveuATabuleta") && Raised("TabuletaResolvida")
                    && Objective().StartsWith("Entre na caminhonete") && Active("Samir_Na_Caminhonete") && Active("Entrar_Na_Caminhonete")
                    && !Active("Nicho_Dos_Guardioes") && baseFoundation == wantFoundation && baseGuardians == wantGuardians;
                detail = $"{choiceEvent}={Raised(choiceEvent)}, objetivo=\"{Objective()}\", Samir na caminhonete={Active("Samir_Na_Caminhonete")}, " +
                    $"zona de embarque={Active("Entrar_Na_Caminhonete")}, nicho ligado={Active("Nicho_Dos_Guardioes")}, Fundação {baseFoundation}, Guardiões {baseGuardians}";
                if (!ok) { Result(false, detail); return true; }
                string stuck = WalkTo(new Vector3(60f, 0f, 172f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Now;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Now < mark + 0.8f) return false;
                bool saved = WorldState.IsCheckpointActivated("fuga.autosave_embarque");
                if (!saved) { Result(false, "o salvamento do embarque não foi marcado ao chegar na traseira"); return true; }
                Teleport(new Vector3(60f, 0f, 171.9f), 0f);
                mark = Now;
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                if (Now < mark + 0.6f) return false;
                var interactor = player.GetComponent<PlayerInteractor>();
                string prompt = interactor.Current?.Prompt;
                if (prompt != "Entrar na caminhonete") { Result(false, $"a interação de embarque não apareceu (aviso atual: \"{prompt}\")"); return true; }

                // Morre antes de entrar: volta ao ponto de retorno do embarque com a escolha, a reputação e a tabuleta.
                var health = player.GetComponent<Health>();
                health.Invincible = false;
                health.TakeDamage(new DamageInfo(1000f, player.transform.position));
                mark = Now;
                phase = 4;
                return false;
            }
            if (phase == 4)
            {
                if (Now < mark + 0.3f) return false;
                if (!player.GetComponent<Health>().IsDead) { Result(false, "a Elena não morreu"); return true; }
                oldPlayer = player.GetInstanceID();
                player.GetComponent<PlayerDeathHandler>().Restart();
                startedAt = EditorApplication.timeSinceStartup;
                phase = 10;
                return false;
            }
            if (phase == 10)
            {
                var now = Object.FindFirstObjectByType<PlayerMotor>();
                if (now == null || now.GetInstanceID() == oldPlayer || EditorApplication.timeSinceStartup - startedAt < 1.5f) return false;
                mark = Now;
                phase = 11;
                return false;
            }
            if (phase == 11)
            {
                if (Now < mark + 1.5f) return false;
                var tablet = Load<ItemDefinition>("Items/TabuletaDoJunco");
                bool kept = Raised(choiceEvent) && Raised("TabuletaResolvida") && !Raised("EmbarcouNaCaminhonete")
                    && Reputation.Get(Load<FactionDefinition>("Factions/FundacaoEnlil")) == baseFoundation
                    && Reputation.Get(Load<FactionDefinition>("Factions/Guardioes")) == baseGuardians
                    && WorldState.Transformation == baseTransformation
                    && Object.FindFirstObjectByType<PlayerInventory>().Has(tablet)
                    && Objective().StartsWith("Entre na caminhonete");
                Vector3 at = Object.FindFirstObjectByType<PlayerMotor>().transform.position;
                bool backAtSave = Vector3.ProjectOnPlane(at - new Vector3(60f, 0f, 171.6f), Vector3.up).magnitude < 2.5f;
                detail += $"; depois de morrer: escolha mantida={kept}, de volta ao embarque={backAtSave}";
                if (!kept || !backAtSave) { Result(false, detail); return true; }

                // Embarca (pelo próprio evento da interação). Curta ou de verdade, conforme o caso.
                var ride = Ride();
                if (!fullLength) SetPrivate(ride, "minSeconds", 5f);
                Teleport(new Vector3(60f, 0f, 171.9f), 0f);
                Obj("Entrar_Na_Caminhonete").GetComponent<InteractableEvent>().Interact(Object.FindFirstObjectByType<PlayerInteractor>());
                linesSeen = 0;
                mark = Now;
                lastTick = Time.realtimeSinceStartup;
                phase = 20;
                return false;
            }
            return RideFlow(pick == Step15Shortcuts.Pick.Ficou ? "Ficou" : "Entregou", dialogueName, fullLength, holdDialogue: !fullLength, final: false);
        }

        // ---------- O ramo Devolve ----------

        static bool Devolveu()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            const string dialogueName = "Fuga_Estrada_Devolveu";

            if (phase == 1)
            {
                baseFoundation = Reputation.Get(Load<FactionDefinition>("Factions/FundacaoEnlil"));
                baseGuardians = Reputation.Get(Load<FactionDefinition>("Factions/Guardioes"));
                baseTransformation = WorldState.Transformation;
                var tablet = Load<ItemDefinition>("Items/TabuletaDoJunco");
                bool ok = Raised("DevolveuATabuleta") && !Raised("TabuletaResolvida") && !Raised("EnterrouATabuleta")
                    && Objective().StartsWith("Leve a tabuleta ao nicho") && Active("Nicho_Dos_Guardioes") && Active("Enterrar_A_Tabuleta")
                    && Active("Samir_No_Nicho") && !Active("Samir_Na_Caminhonete") && !Active("Entrar_Na_Caminhonete")
                    && baseFoundation == -15 && baseGuardians == 15 && Object.FindFirstObjectByType<PlayerInventory>().Has(tablet);
                detail = $"objetivo=\"{Objective()}\", nicho={Active("Nicho_Dos_Guardioes")}, Samir no nicho={Active("Samir_No_Nicho")}, " +
                    $"Samir na caminhonete={Active("Samir_Na_Caminhonete")}, embarque={Active("Entrar_Na_Caminhonete")}, Fundação {baseFoundation}, Guardiões {baseGuardians}";
                if (!ok) { Result(false, detail); return true; }
                mark = Now;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Now < mark + 0.6f) return false;
                string prompt = player.GetComponent<PlayerInteractor>().Current?.Prompt;
                if (prompt != "Enterrar a tabuleta") { Result(false, $"a interação do nicho não apareceu (aviso atual: \"{prompt}\")"); return true; }
                Obj("Enterrar_A_Tabuleta").GetComponent<DialogueStarter>().StartDialogue();
                mark = Now;
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                var runner = Object.FindFirstObjectByType<DialogueRunner>();
                if (runner.IsRunning)
                {
                    if (Time.realtimeSinceStartup - lastTick > 0.2f) { runner.Continue(); lastTick = Time.realtimeSinceStartup; }
                    return false;
                }
                if (!Raised("EnterrouATabuleta") || Now < mark + 0.5f) return false;
                // O ponto de retorno vem 0,9 s depois da fala.
                if (!WorldState.IsCheckpointActivated("fuga.autosave_depois_do_nicho")) return false;
                var tablet = Load<ItemDefinition>("Items/TabuletaDoJunco");
                bool ok = Raised("TabuletaResolvida") && !Object.FindFirstObjectByType<PlayerInventory>().Has(tablet)
                    && Objective().StartsWith("Entre na caminhonete") && !Active("Samir_No_Nicho") && Active("Samir_Na_Caminhonete")
                    && Active("Entrar_Na_Caminhonete") && !Active("Enterrar_A_Tabuleta") && Active("Tabuleta_Enterrada")
                    && WorldState.Transformation == baseTransformation;
                detail += $"; depois de enterrar: tabuleta fora do inventário={!Object.FindFirstObjectByType<PlayerInventory>().Has(tablet)}, " +
                    $"objetivo=\"{Objective()}\", Samir no nicho={Active("Samir_No_Nicho")}, na caminhonete={Active("Samir_Na_Caminhonete")}, embarque={Active("Entrar_Na_Caminhonete")}";
                if (!ok) { Result(false, detail); return true; }

                // Volta do ponto de retorno (como depois de morrer): a tabuleta continua enterrada.
                oldPlayer = player.GetInstanceID();
                Checkpoints.Restart(RegionSceneName);
                startedAt = EditorApplication.timeSinceStartup;
                phase = 10;
                return false;
            }
            if (phase == 10)
            {
                var now = Object.FindFirstObjectByType<PlayerMotor>();
                if (now == null || now.GetInstanceID() == oldPlayer || EditorApplication.timeSinceStartup - startedAt < 1.5f) return false;
                mark = Now;
                phase = 11;
                return false;
            }
            if (phase == 11)
            {
                if (Now < mark + 1.5f) return false;
                var tablet = Load<ItemDefinition>("Items/TabuletaDoJunco");
                var runner = Object.FindFirstObjectByType<DialogueRunner>();
                Vector3 at = Object.FindFirstObjectByType<PlayerMotor>().transform.position;
                bool nearNiche = Vector3.ProjectOnPlane(at - Step15Shortcuts.AtNiche, Vector3.up).magnitude < 2.5f;
                bool kept = Raised("DevolveuATabuleta") && Raised("EnterrouATabuleta") && Raised("TabuletaResolvida")
                    && !Object.FindFirstObjectByType<PlayerInventory>().Has(tablet)
                    && Reputation.Get(Load<FactionDefinition>("Factions/FundacaoEnlil")) == baseFoundation
                    && Reputation.Get(Load<FactionDefinition>("Factions/Guardioes")) == baseGuardians
                    && !runner.IsRunning && !Active("Samir_No_Nicho") && Active("Tabuleta_Enterrada") && Active("Samir_Na_Caminhonete")
                    && Active("Entrar_Na_Caminhonete") && Objective().StartsWith("Entre na caminhonete");
                detail += $"; depois de voltar do ponto de retorno: escolha e nicho mantidos={kept}, ao lado do nicho={nearNiche}";
                if (!kept || !nearNiche) { Result(false, detail); return true; }

                // Volta andando pelo túnel até a traseira da caminhonete.
                string stuck = WalkPoints(new Vector3(32f, -13.5f, 108f), new Vector3(32f, 0f, 141f)) ?? WalkTo(new Vector3(60f, 0f, 172f));
                if (stuck != null) { Result(false, stuck); return true; }
                Teleport(new Vector3(60f, 0f, 171.9f), 0f);
                mark = Now;
                phase = 12;
                return false;
            }
            if (phase == 12)
            {
                if (Now < mark + 0.6f) return false;
                string prompt = Object.FindFirstObjectByType<PlayerMotor>().GetComponent<PlayerInteractor>().Current?.Prompt;
                if (prompt != "Entrar na caminhonete") { Result(false, $"a interação de embarque não apareceu (aviso atual: \"{prompt}\")"); return true; }
                SetPrivate(Ride(), "minSeconds", 25f);
                Obj("Entrar_Na_Caminhonete").GetComponent<InteractableEvent>().Interact(Object.FindFirstObjectByType<PlayerInteractor>());
                linesSeen = 0;
                mark = Now;
                lastTick = Time.realtimeSinceStartup;
                phase = 20;
                return false;
            }
            return RideFlow("Devolveu", dialogueName, fullLength: false, holdDialogue: false, final: true);
        }

        // ---------- A viagem, do embarque à tela de fim ----------

        // Fases 20 em diante, comuns aos três ramos.
        static bool RideFlow(string who, string dialogueName, bool fullLength, bool holdDialogue, bool final)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var ride = Ride();
            var runner = Object.FindFirstObjectByType<DialogueRunner>();

            if (phase == 20)
            {
                // Depois do fade (1,6 s) e do atraso da conversa, o trilho anda e a conversa certa começa.
                if (!ride.IsRiding || !runner.IsRunning) { if (Now > mark + 12f) { Result(false, $"a viagem não começou (riding={ride.IsRiding}, conversa={runner.IsRunning})"); return true; } return false; }
                var current = Field<DialogueDefinition>(runner, "current");
                var seat = Field<Transform>(ride, "elenaSeat");
                var interactor = player.GetComponent<PlayerInteractor>();
                var input = player.GetComponent<PlayerInputReader>();
                bool locked = !player.enabled && !interactor.enabled && !input.CombatEnabled && player.transform.parent == seat;
                bool right = current != null && current.name == dialogueName;
                detail += $"; viagem ({who}): conversa={current?.name}, Elena presa na caminhonete={locked}, embarque dispara o evento={Raised("EmbarcouNaCaminhonete")}";
                if (!locked || !right || !Raised("EmbarcouNaCaminhonete")) { Result(false, detail); return true; }

                // Boarding de novo ou uma interação não reiniciam nada.
                truckBefore = ride.transform.position;
                ride.Begin();
                progressBefore = ride.Progress;
                mark = Now;
                phase = 21;
                return false;
            }
            if (phase == 21)
            {
                // Passa a conversa de uma em uma fala (rápido), sem nunca deixar o fim da viagem cortar a última.
                if (holdDialogue)
                {
                    // Conversa mais longa que o trilho: segura a conversa até o trilho chegar ao fim e esperar.
                    if (Now < mark + 9f)
                    {
                        if (!runner.IsRunning) { Result(false, "a conversa terminou sozinha enquanto estava segura"); return true; }
                        return false;
                    }
                    bool waiting = ride.IsRiding && ride.Progress >= 0.999f && !Raised("FimDaViagem") && runner.IsRunning;
                    Vector3 stopped = ride.transform.position;
                    detail += $"; trilho no fim ({ride.Progress:0.00}) esperando a conversa={waiting}";
                    if (!waiting) { Result(false, detail); return true; }
                    mark = Now;
                    truckBefore = stopped;
                    phase = 22;
                    return false;
                }
                AdvanceDialogue(runner, 1.2f);
                // A pausa é testada no meio do trilho, com a conversa parada (durante uma conversa não se pausa).
                if (!pauseDone && !runner.IsRunning && ride.IsRiding && ride.Progress > 0.2f && ride.Progress < 0.85f)
                {
                    pauseBefore = ride.Progress;
                    typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), null);
                    mark = Now;
                    phase = 24;
                    return false;
                }
                if (ride.IsDone || Raised("FimDaViagem"))
                {
                    phase = 23;
                    mark = Now;
                }
                else if (!ride.IsRiding && !Raised("FimDaViagem")) { Result(false, "a viagem parou sem terminar"); return true; }
                return false;
            }
            if (phase == 22)
            {
                // Libera a conversa fala por fala: a viagem só termina depois da última.
                AdvanceDialogue(runner, 0.4f);
                if (runner.IsRunning && Raised("FimDaViagem")) { Result(false, "a viagem terminou com a conversa ainda aberta (fala cortada)"); return true; }
                bool still = Vector3.Distance(truckBefore, ride.transform.position) < 0.05f;
                if (!still && runner.IsRunning) { Result(false, "a caminhonete andou depois do fim do trilho"); return true; }
                if (!runner.IsRunning && Raised("FimDaViagem")) { phase = 23; mark = Now; }
                return false;
            }
            if (phase == 24)
            {
                // Pausado: o trilho tem de ficar parado por 1 s de tempo real.
                if (Now < mark + 1f) return false;
                float frozen = ride.Progress;
                pauseNote = Mathf.Abs(frozen - pauseBefore) < 0.002f ? "" : $"pausa NÃO congelou o trilho ({pauseBefore:0.000} -> {frozen:0.000})";
                pauseBefore = frozen;
                typeof(PauseMenuUI).GetMethod("Close", Any).Invoke(Object.FindFirstObjectByType<PauseMenuUI>(), null);
                mark = Now;
                phase = 25;
                return false;
            }
            if (phase == 25)
            {
                if (Now < mark + 1f) return false;
                bool resumed = ride.Progress > pauseBefore + 0.004f;
                if (!resumed) pauseNote += " e continuar NÃO retomou o trilho";
                detail += pauseNote.Length == 0 ? "; a pausa congelou o trilho e continuar o retomou" : "; " + pauseNote;
                pauseDone = true;
                if (pauseNote.Length > 0 || !resumed) { Result(false, detail); return true; }
                phase = 21;
                return false;
            }
            if (phase == 23)
            {
                // Chegou: o diário abre e, quando é fechado, o fim da fatia dispara e a tela de fim abre.
                var reading = Object.FindFirstObjectByType<ReadingScreenUI>();
                var screen = Object.FindFirstObjectByType<DemoEndScreen>();
                if (reading.IsOpen)
                {
                    if (!Raised("LeuODiario")) { Result(false, "o diário abriu sem disparar LeuODiario"); return true; }
                    if (Raised("FimDaFatia")) { Result(false, "o fim da fatia disparou com o diário ainda aberto"); return true; }
                    typeof(ReadingScreenUI).GetMethod("Close", Any).Invoke(reading, null);
                    mark = Now;
                    return false;
                }
                if (!Raised("FimDaFatia"))
                {
                    if (Now > mark + 8f && !Raised("LeuODiario")) { Result(false, "o diário não abriu depois do fim da viagem"); return true; }
                    return false;
                }
                bool shown = screen.IsOpen && Time.timeScale == 0f;
                float duration = At("FimDaViagem", 0f) - At("EmbarcouNaCaminhonete", 0f);
                float spread = Time.realtimeSinceStartup;
                detail += $"; do embarque ao fim da viagem: {duration:0.0} s; diário lido={Raised("LeuODiario")}; FimDaFatia={Raised("FimDaFatia")}; tela de fim aberta={shown}";
                bool longEnough = !fullLength || duration >= 60f;
                if (!shown || !longEnough) { Result(false, detail); return true; }

                if (!final) { Result(true, detail); return true; }

                // Volta ao menu principal e começa um jogo novo.
                screen.ReturnToMenu();
                startedAt = EditorApplication.timeSinceStartup;
                mark = Now;
                phase = 30;
                return false;
            }
            if (phase == 30)
            {
                if (SceneManager.GetActiveScene().name != "MenuPrincipal")
                {
                    if (EditorApplication.timeSinceStartup - startedAt > 15) { Result(false, "o menu principal não abriu"); return true; }
                    return false;
                }
                var menu = Object.FindFirstObjectByType<MainMenuUI>();
                bool clean = Time.timeScale == 1f && !Raised("EscolheuONoWadi") && !Raised("FimDaFatia") && !Checkpoints.TryGetFor(RegionSceneName, out _);
                detail += $"; menu principal aberto={menu != null}, jogo zerado={clean}";
                if (menu == null || !clean) { Result(false, detail); return true; }
                menu.StartGame();
                startedAt = EditorApplication.timeSinceStartup;
                phase = 31;
                return false;
            }
            if (phase == 31)
            {
                if (SceneManager.GetActiveScene().name != RegionSceneName || Object.FindFirstObjectByType<PlayerMotor>() == null)
                {
                    if (EditorApplication.timeSinceStartup - startedAt > 20) { Result(false, "a região não abriu pelo menu principal"); return true; }
                    return false;
                }
                bool fresh = !Raised("DevolveuATabuleta") && !Raised("AbriuAPassagem");
                Result(fresh, detail + $"; Novo jogo abre a região do começo={fresh}");
                return true;
            }
            return false;
        }


        static void AdvanceDialogue(DialogueRunner runner, float every)
        {
            if (!runner.IsRunning || Time.realtimeSinceStartup - lastTick < every) return;
            lastTick = Time.realtimeSinceStartup;
            runner.Continue();
            linesSeen++;
        }

        // ---------- Ajudas ----------

        static PlayerMotor Player() => Object.FindFirstObjectByType<PlayerMotor>();

        static TruckRide Ride() => Object.FindFirstObjectByType<TruckRide>();

        // Procura também entre os objetos desligados (os grupos da fuga ligam e desligam por eventos).
        static GameObject Obj(string name) =>
            Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == name && g.scene.IsValid() && g.scene.isLoaded);

        static bool Active(string name)
        {
            GameObject go = Obj(name);
            return go != null && go.activeInHierarchy;
        }

        static T Field<T>(object target, string name) where T : class =>
            target.GetType().GetField(name, Any).GetValue(target) as T;

        static void SetPrivate(object target, string name, object value) => target.GetType().GetField(name, Any).SetValue(target, value);

        static float At(string eventName, float fallback) => raisedAt.TryGetValue(eventName, out float t) ? t : fallback;

        static bool Raised(string eventName) => StoryEvents.HasBeenRaised(Load<StoryEvent>("Events/" + eventName));

        static string Objective()
        {
            var quests = Object.FindFirstObjectByType<QuestManager>();
            foreach (var (quest, step) in quests.ActiveObjectives())
                if (quest.kind == QuestKind.Principal) return step.LocalizedObjective;
            return "nenhum (missão principal concluída)";
        }

        // Anda em linha reta por pontos dados (o túnel e a rampa, onde a malha de navegação tem cantos nas paredes).
        static string WalkPoints(params Vector3[] points)
        {
            var player = Player();
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            string stuck = null;
            foreach (Vector3 point in points)
            {
                for (int s = 0; s < 2000; s++)
                {
                    Vector3 flat = Vector3.ProjectOnPlane(point - player.transform.position, Vector3.up);
                    if (flat.magnitude < 0.3f) break;
                    controller.Move(flat.normalized * 0.1f + Vector3.down * 0.1f);
                }
                Vector3 at = player.transform.position;
                if (Vector3.ProjectOnPlane(point - at, Vector3.up).magnitude > 0.8f) { stuck = $"parou em {at} a caminho de {point}"; break; }
            }
            player.enabled = true;
            return stuck;
        }

        // Anda como a Elena (CharacterController) pelos cantos do caminho da malha de navegação até o ponto.
        static string WalkTo(Vector3 target)
        {
            var player = Player();
            Vector3 from = player.transform.position;
            from.y = 0f;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 4f, NavMesh.AllAreas) || !NavMesh.SamplePosition(target, out NavMeshHit b, 3f, NavMesh.AllAreas))
                return $"fora da malha de navegação ({from} -> {target})";
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                return $"sem caminho na malha de {a.position} até {b.position}";
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            string stuck = null;
            for (int i = 1; i < path.corners.Length && stuck == null; i++)
            {
                for (int s = 0; s < 2000; s++)
                {
                    Vector3 flat = Vector3.ProjectOnPlane(path.corners[i] - player.transform.position, Vector3.up);
                    if (flat.magnitude < 0.3f) break;
                    controller.Move(flat.normalized * 0.1f + Vector3.down * 0.1f);
                }
                Vector3 at = player.transform.position;
                if (Vector3.ProjectOnPlane(path.corners[i] - at, Vector3.up).magnitude > 0.8f)
                    stuck = $"parou em {at} a caminho de {path.corners[i]}";
            }
            player.enabled = true;
            return stuck;
        }
    }
}
