using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Environment;
using Nibiru.Factions;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Stealth;
using Nibiru.Tablets;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 14 (o wadi). Confere o percurso a pé, a tempestade (névoa,
    /// lentidão), o líder de esquadra que dá para evitar, o vento que cobre passos mas não tiros, o rádio do
    /// Kast esperando a fita de Irena (tocando, já terminada, nunca pega e o teto de espera) e a escolha de
    /// facção (as três opções: reputação, evento, transformação escondida, salvamento, recarga).
    /// No fim, escreve um resumo curto no Console (OK ou FALHOU).
    /// Menu: Nibiru > Testes > Passo 14 > 8. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step14Tests
    {
        // Altura da origem da Elena acima dos pés.
        const float FeetToCenter = 1.05f;
        const string AutosaveId = "wadi.autosave_depois_da_escolha";

        class Check
        {
            public string name, moment;
            public Func<bool> step;
        }

        static readonly List<Check> checks = new List<Check>();
        static readonly StringBuilder report = new StringBuilder();
        static readonly Dictionary<string, float> raisedAt = new Dictionary<string, float>();
        static int index, phase, oldPlayer;
        static double startedAt;
        static float mark, beganAt, capLimit;
        static bool tapeWasPlaying, radioStartedWhileTapePlaying, sawWaiting, transformationEventFired, hooked;
        static (float chars, float min, float pause)? tapeBackup;
        static string detail;
        static int baseFoundation, baseGuardians, baseTransformation;

        static Step14Tests()
        {
            Register("passo14.teste", Begin);
            // Momentos só dos testes (os atalhos do menu estão em Step14Shortcuts).
            Register("passo14.radio_fita_terminada", () => Step11Shortcuts.Begin(Step14Shortcuts.TapeDone, Step14Shortcuts.FollowStep, Step14Shortcuts.BeforeArrival, 0f, "Passo 14: gatilho do rádio com a fita já ouvida."));

            checks.Add(new Check { name = "Wadi a pé até o soldado; tempestade sobe e a Elena fica mais lenta", moment = "passo14.inicio", step = Walk });
            checks.Add(new Check { name = "Líder de esquadra patrulhando; a margem sul fica fora da vista dele na tempestade", moment = "passo14.inicio", step = Leader });
            checks.Add(new Check { name = "Vento cobre passos, não tiros", moment = "passo14.inicio", step = Wind });
            checks.Add(new Check { name = "Rádio com a fita tocando: espera e começa assim que ela termina, sem voltar ao gatilho", moment = "passo14.radio_fita_tocando", step = RadioTapePlaying });
            checks.Add(new Check { name = "Rádio com a fita já terminada: começa na hora", moment = "passo14.radio_fita_terminada", step = RadioTapeDone });
            checks.Add(new Check { name = "Rádio com a fita nunca pega: começa na hora e a fita não toca", moment = "passo14.radio_sem_fita", step = RadioNoTape });
            checks.Add(new Check { name = "Rádio com uma fita que não acaba: o teto (duração + 5 s) solta o rádio", moment = "passo14.radio_fita_tocando", step = RadioCap });
            checks.Add(new Check { name = "Escolha: entregar a tabuleta à Fundação (salva, recarrega e dispara o evento)", moment = "passo14.escolha", step = () => Choice(0) });
            checks.Add(new Check { name = "Escolha: devolver ao Poço com o Samir (salva, recarrega e dispara o evento)", moment = "passo14.escolha", step = () => Choice(1) });
            checks.Add(new Check { name = "Escolha: ficar com a tabuleta (salva, recarrega e dispara o evento; transformação +3 escondida)", moment = "passo14.escolha", step = () => Choice(2) });
        }

        [MenuItem("Nibiru/Testes/Passo 14/8. Teste automático (resumo no Console)")]
        static void Run() => Go("passo14.teste");

        static void Begin()
        {
            report.Clear();
            index = -1;
            if (!hooked)
            {
                StoryEvents.Raised += OnRaised;
                Transformation.Changed += _ => transformationEventFired = true;
                hooked = true;
            }
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            NextCheck();
        }

        static void OnRaised(StoryEvent e) => raisedAt[e.Id] = Time.realtimeSinceStartup;

        static void NextCheck()
        {
            RestoreTape();
            ReleaseStorm();
            index++;
            phase = 0;
            if (index >= checks.Count)
            {
                EditorApplication.update -= Tick;
                Debug.Log("Passo 14, teste automático:\n" + report);
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            raisedAt.Clear();
            tapeWasPlaying = radioStartedWhileTapePlaying = sawWaiting = transformationEventFired = false;
            string moment = checks[index].moment;
            // As fitas curtas valem desde o carregamento (a fita retomada lê os tempos ao tocar).
            if (moment.Contains("fita")) ShortenTape();
            Go(moment);
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
                RestoreTape();
                EditorApplication.update -= Tick;
                return;
            }
            float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
            if (elapsed > 90f)
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
                Watch();
                checks[index].step();
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}");
            }
        }

        // Olha a cada quadro: a fita e o rádio nunca podem se sobrepor.
        static void Watch()
        {
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>(FindObjectsInactive.Include);
            var radio = Object.FindFirstObjectByType<RadioAfterTape>();
            bool playing = tapePlayer != null && tapePlayer.IsPlaying;
            if (playing) tapeWasPlaying = true;
            if (radio != null && radio.IsWaiting) sawWaiting = true;
            if (playing && Raised("RadioDoKastComecou") && !radioStartedWhileTapePlaying && !capLifted) radioStartedWhileTapePlaying = true;
        }

        static bool capLifted;

        // ---------- 1. Percurso a pé e tempestade ----------

        static bool Walk()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                GameObject active = GameObject.Find("Wadi_Ativo");
                if (active == null || !active.activeInHierarchy) { Result(false, "o grupo Wadi_Ativo não está ligado (rode o menu do Wadi)"); return true; }
                if (!Objective().StartsWith("Siga o wadi")) { Result(false, $"objetivo errado: \"{Objective()}\""); return true; }
                detail = $"início z={player.transform.position.z:0.0}, tempestade inicial={Storm.Intensity:0.00}";
                string stuck = WalkTo(new Vector3(32f, 0f, 153f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 4.5f) return false;
                float speed = Storm.MoveSpeedMultiplier;
                bool ok = Storm.Intensity > 0.95f && speed < 0.95f && RenderSettings.fog;
                detail += $"; no trecho B: tempestade={Storm.Intensity:0.00}, velocidade x{speed:0.00}, névoa={RenderSettings.fog}";
                if (!ok) { Result(false, detail); return true; }
                string stuck = WalkTo(new Vector3(60f, 0f, 160f));
                if (stuck != null) { Result(false, stuck); return true; }
                Vector3 at = player.transform.position;
                bool reached = Vector3.ProjectOnPlane(at - new Vector3(60f, 0f, 160f), Vector3.up).magnitude < 1.5f;
                Result(reached, $"{detail}; chegou perto do soldado em ({at.x:0.0}, {at.z:0.0})");
                return true;
            }
            return true;
        }

        // ---------- 2. O líder de esquadra ----------

        static bool Leader()
        {
            GameObject go = GameObject.Find("Lider_Do_Wadi");
            if (go == null) { Result(false, "falta o Lider_Do_Wadi"); return true; }
            var ai = go.GetComponent<SoldierAI>();
            var perception = go.GetComponent<SoldierPerception>();
            if (ai == null || !ai.enabled || perception == null || !perception.enabled || !ai.IsLeader)
            {
                Result(false, $"o líder não está ligado (IA={ai != null && ai.enabled}, percepção={perception != null && perception.enabled}, líder={ai != null && ai.IsLeader})");
                return true;
            }

            // A vista do líder na tempestade cheia: 20 m x 0,55. O caminho agachado pela margem sul, do topo da
            // rampa até a entrada do trecho C, tem de ficar sempre fora disso (por distância ou por pedra).
            var settings = (SoldierSettings)typeof(SoldierAI).GetField("settings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(ai);
            float sight = settings.viewDistance * 0.55f;
            Physics.SyncTransforms();
            var patrol = new List<Vector3>();
            for (float x = 38f; x <= 46f; x += 1f) patrol.Add(new Vector3(x, settings.eyeHeight, 162.2f));
            var lane = new List<Vector3>();
            for (float z = 141.5f; z <= 150.8f; z += 1f) lane.Add(new Vector3(32f, 0.3f, z));
            for (float x = 32f; x <= 60f; x += 1f) lane.Add(new Vector3(x, 0.3f, 150.8f));
            for (float z = 150.8f; z <= 160f; z += 1f) lane.Add(new Vector3(60f, 0.3f, z));
            float closest = float.MaxValue;
            Vector3 worst = default;
            foreach (Vector3 eye in patrol)
                foreach (Vector3 point in lane)
                {
                    float distance = Vector3.Distance(eye, point);
                    bool seen = distance <= sight && !Physics.Linecast(eye, point, ~0, QueryTriggerInteraction.Ignore);
                    if (seen && distance < closest) { closest = distance; worst = point; }
                }
            bool hidden = closest == float.MaxValue;
            Result(hidden, hidden
                ? $"líder ligado e patrulhando; vista dele na tempestade={sight:0.0} m; margem sul fora da vista em {lane.Count} pontos x {patrol.Count} posições"
                : $"o líder veria a Elena em ({worst.x:0.0}, {worst.z:0.0}) a {closest:0.0} m (vista={sight:0.0} m)");
            return true;
        }

        // ---------- 3. O vento cobre passos, não tiros ----------

        static bool Wind()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var storm = Object.FindFirstObjectByType<StormController>();
            GameObject go = GameObject.Find("Lider_Do_Wadi");
            var perception = go != null ? go.GetComponent<SoldierPerception>() : null;
            if (storm == null || perception == null) { Result(false, "falta a tempestade ou o líder"); return true; }

            if (phase == 1)
            {
                storm.Force(0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 4f) return false;
                // Sem tempestade: um passo de raio 6 a 4,5 m é ouvido.
                bool heard = EmitAndListen(perception, go, player, 6f, 4.5f);
                detail = $"sem tempestade: passo a 4,5 m ouvido={heard}";
                if (!heard) { Result(false, detail); return true; }
                storm.Force(1f);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (Time.time < mark + 4.5f) return false;
            bool stepHeard = EmitAndListen(perception, go, player, 6f, 4.5f);
            bool shotHeard = EmitAndListen(perception, go, player, 20f, 15f);
            Result(!stepHeard && shotHeard, $"{detail}; na tempestade cheia: passo a 4,5 m ouvido={stepHeard}, tiro a 15 m ouvido={shotHeard}, audição x{Storm.HearingMultiplier:0.00}");
            return true;
        }

        static bool EmitAndListen(SoldierPerception perception, GameObject leader, PlayerMotor player, float radius, float distance)
        {
            perception.ConsumeNoise(out _);
            Noise.Emit(leader.transform.position + Vector3.right * distance, radius, player.gameObject);
            return perception.ConsumeNoise(out _);
        }

        // ---------- 4 a 7. O rádio e a fita ----------

        // Fita tocando: o rádio espera e começa só depois que ela termina, sem a Elena voltar ao gatilho.
        static bool RadioTapePlaying()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>(FindObjectsInactive.Include);
            if (phase == 1)
            {
                if (tapePlayer == null || !tapePlayer.IsPlaying) { Result(false, "a fita não está tocando ao carregar (era para ter sido retomada)"); return true; }
                string stuck = WalkTo(new Vector3(60f, 0f, 162f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                // O gatilho dispara; depois a Elena volta para fora dele.
                if (!Raised("ChegouAoSoldadoFerido") && Time.time < mark + 3f) return false;
                if (!Raised("ChegouAoSoldadoFerido")) { Result(false, "o gatilho do rádio não disparou"); return true; }
                Teleport(new Vector3(60f, FeetToCenter, 156f), 0f);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                if (Time.time < mark + 1.5f) return false;
                if (Raised("RadioDoKastComecou")) { Result(false, "o rádio começou com a fita ainda tocando"); return true; }
                phase = 4;
                return false;
            }
            // Espera a fita terminar e o rádio começar sozinho.
            if (!Raised("RadioDoKastComecou"))
            {
                if (EditorApplication.timeSinceStartup - startedAt > 60) { Result(false, "o rádio nunca começou"); return true; }
                return false;
            }
            bool finishedFirst = raisedAt.TryGetValue("FitaDeIrena1Terminou", out float tapeEnd) && raisedAt.TryGetValue("RadioDoKastComecou", out float radioStart) && radioStart >= tapeEnd;
            float gap = raisedAt["RadioDoKastComecou"] - tapeEnd;
            bool ok = finishedFirst && !radioStartedWhileTapePlaying && sawWaiting && gap < 1.5f;
            string text = $"esperou a fita={sawWaiting}, rádio {gap:0.00} s depois do fim da fita, sobreposição={radioStartedWhileTapePlaying}, Elena fora do gatilho em z={player.transform.position.z:0.0}";
            return FinishTalk(ok, text);
        }

        static bool RadioTapeDone()
        {
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>(FindObjectsInactive.Include);
            if (phase == 1)
            {
                string stuck = WalkTo(new Vector3(60f, 0f, 162f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (!Raised("RadioDoKastComecou"))
            {
                if (Time.time > mark + 4f) Result(false, "o rádio não começou depois de a fita já ter terminado");
                return true;
            }
            float delay = raisedAt["RadioDoKastComecou"] - At("ChegouAoSoldadoFerido", raisedAt["RadioDoKastComecou"]);
            bool ok = !sawWaiting || delay < 1f;
            return FinishTalk(ok && !tapeWasPlaying, $"começou {delay:0.00} s depois de chegar, a fita tocou={tapeWasPlaying}, fila de espera={sawWaiting}");
        }

        static bool RadioNoTape()
        {
            if (phase == 1)
            {
                string stuck = WalkTo(new Vector3(60f, 0f, 162f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (!Raised("RadioDoKastComecou"))
            {
                if (Time.time > mark + 4f) Result(false, "o rádio não começou com a fita nunca pega");
                return true;
            }
            float delay = raisedAt["RadioDoKastComecou"] - At("ChegouAoSoldadoFerido", raisedAt["RadioDoKastComecou"]);
            bool ok = delay < 1f && !tapeWasPlaying && !Raised("ColetouAFitaDeIrena1");
            return FinishTalk(ok, $"começou {delay:0.00} s depois de chegar, a fita tocou={tapeWasPlaying}, pega={Raised("ColetouAFitaDeIrena1")}");
        }

        // O teto: com uma fita que passa muito da duração prevista, o rádio começa em "duração + 5 s" e não espera para sempre.
        static bool RadioCap()
        {
            var tapeAsset = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            var radio = Object.FindFirstObjectByType<RadioAfterTape>();
            if (phase == 1)
            {
                string stuck = WalkTo(new Vector3(60f, 0f, 162f));
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (radio == null || !radio.IsWaiting)
                {
                    if (Time.time > mark + 4f) Result(false, "o rádio não ficou esperando a fita");
                    return true;
                }
                // O teto foi calculado ao começar a espera. Daqui em diante cada fala da fita dura 40 s: a fita passa muito do previsto.
                capLimit = radio.MaxWaitSeconds;
                tapeAsset.minSecondsPerLine = 40f;
                capLifted = true;
                beganAt = Time.time;
                phase = 3;
                return false;
            }
            if (!Raised("RadioDoKastComecou"))
            {
                if (Time.time - beganAt > 40f) { capLifted = false; Result(false, "o rádio esperou mais que o teto"); }
                return true;
            }
            capLifted = false;
            float waited = Time.time - beganAt;
            float limit = capLimit;
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>(FindObjectsInactive.Include);
            bool ok = waited >= limit - 1.5f && waited <= limit + 2.5f && tapePlayer.IsPlaying;
            return FinishTalk(ok, $"esperou {waited:0.0} s (teto {limit:0.0} s = duração da fita + 5 s), a fita ainda tocava={tapePlayer.IsPlaying}");
        }

        // Fecha a conversa do rádio escolhendo a primeira opção, para devolver o controle e o tempo ao jogo.
        static bool FinishTalk(bool ok, string text)
        {
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner != null && runner.IsRunning)
            {
                if (Step14Shortcuts.ChoiceOnScreen(runner)) runner.Choose(0);
                else runner.Continue();
                return false;
            }
            Result(ok, text);
            return true;
        }

        // ---------- 8 a 10. A escolha de facção ----------

        static readonly string[] ChoiceEvents = { "EntregouATabuleta", "DevolveuATabuleta", "FicouComATabuleta" };
        static readonly int[] FoundationDelta = { 15, -15, -5 };
        static readonly int[] GuardiansDelta = { -15, 15, -5 };
        static readonly int[] TransformationDelta = { 0, 0, 3 };

        static bool Choice(int pick)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            FactionDefinition foundation = Load<FactionDefinition>("Factions/FundacaoEnlil");
            FactionDefinition guardians = Load<FactionDefinition>("Factions/Guardioes");

            if (phase == 1)
            {
                baseFoundation = Reputation.Get(foundation);
                baseGuardians = Reputation.Get(guardians);
                baseTransformation = WorldState.Transformation;
                transformationEventFired = false;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                // O rádio abre sozinho (a chegada foi dada ao carregar) e as falas avançam até as opções.
                if (runner == null || !runner.IsRunning || !Step14Shortcuts.ChoiceOnScreen(runner))
                {
                    if (EditorApplication.timeSinceStartup - startedAt > 30) Result(false, "as opções da escolha não apareceram");
                    return true;
                }
                if (Raised("EscolheuONoWadi")) { Result(false, "a escolha já estava feita antes de o jogador escolher"); return true; }
                runner.Choose(pick);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                if (runner.IsRunning) { if (!Step14Shortcuts.ChoiceOnScreen(runner)) runner.Continue(); return false; }
                if (!Raised("EscolheuONoWadi") || Time.time < mark + 0.5f) return false;
                // Depois da escolha: o salvamento automático liga sozinho.
                if (!WorldState.IsCheckpointActivated(AutosaveId) && Time.time < mark + 6f) return false;

                bool onlyThis = ChoiceEvents.Select((name, i) => Raised(name) == (i == pick)).All(b => b);
                int dF = Reputation.Get(foundation) - baseFoundation;
                int dG = Reputation.Get(guardians) - baseGuardians;
                int dT = WorldState.Transformation - baseTransformation;
                bool silent = !transformationEventFired;
                bool saved = WorldState.IsCheckpointActivated(AutosaveId);
                bool ok = onlyThis && dF == FoundationDelta[pick] && dG == GuardiansDelta[pick] && dT == TransformationDelta[pick]
                    && silent && saved && Objective().StartsWith(pick == 1 ? "Leve a tabuleta" : "Entre na caminhonete") && player.GetComponent<PlayerInputReader>().GameplayEnabled;
                detail = $"{ChoiceEvents[pick]}={Raised(ChoiceEvents[pick])}, só esta opção={onlyThis}, Fundação {dF:+0;-0;0}, Guardiões {dG:+0;-0;0}, transformação {dT:+0;-0;0} sem aviso={silent}, salvou={saved}";
                if (!ok) { Result(false, detail); return true; }

                // Recarrega do salvamento automático: a escolha, a reputação e a transformação continuam, e nada se repete.
                oldPlayer = player.GetInstanceID();
                Checkpoints.Restart(RegionSceneName);
                startedAt = EditorApplication.timeSinceStartup;
                phase = 10;
                return false;
            }
            if (phase == 10)
            {
                var now = Object.FindFirstObjectByType<PlayerMotor>();
                float waitedSeconds = (float)(EditorApplication.timeSinceStartup - startedAt);
                if (now == null || now.GetInstanceID() == oldPlayer || waitedSeconds < 1.5f) return false;
                mark = Time.time;
                phase = 11;
                return false;
            }
            if (Time.time < mark + 1.5f) return false;
            bool eventKept = Raised(ChoiceEvents[pick]) && Raised("EscolheuONoWadi");
            int fAfter = Reputation.Get(foundation) - baseFoundation;
            int gAfter = Reputation.Get(guardians) - baseGuardians;
            int tAfter = WorldState.Transformation - baseTransformation;
            bool noRepeat = runner == null || !runner.IsRunning;
            Vector3 at = Object.FindFirstObjectByType<PlayerMotor>().transform.position;
            bool atSave = Vector3.Distance(new Vector3(at.x, 0f, at.z), new Vector3(60f, 0f, 172.5f)) < 3f;
            GameObject radioGo = GameObject.Find("Radio_Caido");
            bool radioQuiet = radioGo != null && !radioGo.GetComponent<AudioSource>().enabled;
            bool reloaded = eventKept && fAfter == FoundationDelta[pick] && gAfter == GuardiansDelta[pick] && tAfter == TransformationDelta[pick]
                && noRepeat && atSave && radioQuiet && Objective().StartsWith(pick == 1 ? "Leve a tabuleta" : "Entre na caminhonete");
            Result(reloaded, $"{detail}; depois de recarregar: evento mantido={eventKept}, Fundação {fAfter:+0;-0;0}, Guardiões {gAfter:+0;-0;0}, transformação {tAfter:+0;-0;0}, conversa repetida={!noRepeat}, de volta ao salvamento={atSave}, rádio calado={radioQuiet}");
            return true;
        }

        // ---------- Ajudas ----------

        // Anda como a Elena (CharacterController) pelos cantos do caminho da malha de navegação até o ponto.
        static string WalkTo(Vector3 target)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            Vector3 from = player.transform.position;
            from.y = 0f;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 3f, NavMesh.AllAreas) || !NavMesh.SamplePosition(target, out NavMeshHit b, 3f, NavMesh.AllAreas))
                return $"fora da malha de navegação ({from} -> {target})";
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                return $"sem caminho na malha de {a.position} até {b.position}";
            return WalkRoute(path.corners);
        }

        static string WalkRoute(Vector3[] route)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            string stuck = null;
            for (int i = 1; i < route.Length && stuck == null; i++)
            {
                for (int s = 0; s < 1200; s++)
                {
                    Vector3 flat = Vector3.ProjectOnPlane(route[i] - player.transform.position, Vector3.up);
                    if (flat.magnitude < 0.3f) break;
                    controller.Move(flat.normalized * 0.1f + Vector3.down * 0.1f);
                }
                Vector3 at = player.transform.position;
                if (Vector3.ProjectOnPlane(route[i] - at, Vector3.up).magnitude > 0.8f)
                    stuck = $"parou em {at} a caminho de {route[i]}";
            }
            player.enabled = true;
            return stuck;
        }

        // A fita com tempos curtos, só para o teste: a de verdade dura quase um minuto. Volta ao normal ao fim.
        static void ShortenTape()
        {
            var tape = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            if (tapeBackup == null) tapeBackup = (tape.secondsPerCharacter, tape.minSecondsPerLine, tape.pauseBetweenLines);
            tape.secondsPerCharacter = 0.008f;
            tape.minSecondsPerLine = 0.5f;
            tape.pauseBetweenLines = 0.2f;
        }

        static void RestoreTape()
        {
            capLifted = false;
            if (tapeBackup == null) return;
            var tape = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            if (tape != null)
            {
                tape.secondsPerCharacter = tapeBackup.Value.chars;
                tape.minSecondsPerLine = tapeBackup.Value.min;
                tape.pauseBetweenLines = tapeBackup.Value.pause;
            }
            tapeBackup = null;
        }

        static void ReleaseStorm()
        {
            var storm = Object.FindFirstObjectByType<StormController>();
            if (storm != null) storm.ReleaseForce();
        }

        static float At(string eventName, float fallback) => raisedAt.TryGetValue(eventName, out float t) ? t : fallback;

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
