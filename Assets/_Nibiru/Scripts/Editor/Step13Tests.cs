using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.AI;
using Nibiru.Cameras;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Environment;
using Nibiru.Interaction;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 13 (a passagem dos Guardiões). Confere a leitura da
    /// inscrição, a tabuleta pulsando e a parede afundando, a volta do checkpoint com a parede já
    /// aberta, o túnel caminhável até o topo da rampa (lamparinas acesas, um só ponto de retorno e o
    /// salvamento automático), a fita (pegar, ouvir andando sem travar o controle, não ser cortada ao
    /// sair), a fita retomada ao voltar do salvamento, a colisão da câmera no túnel estreito e a volta
    /// à antecâmara. Fecha sozinho as conversas. No fim, escreve um resumo curto no Console (OK ou FALHOU).
    /// Menu: Nibiru > Testes > Passo 13 > 7. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step13Tests
    {
        // Altura da origem da Elena acima dos pés.
        const float FeetToCenter = 1.05f;

        class Check
        {
            public string name, moment;
            public Func<bool> step;
        }

        static readonly List<Check> checks = new List<Check>();
        static readonly StringBuilder report = new StringBuilder();
        static int index, phase, oldPlayer;
        static double startedAt;
        static float mark, wallStartY;
        static bool sawPulse, sawSinking, sawDialogue;
        static (float chars, float min, float pause)? tapeBackup;

        static Step13Tests()
        {
            Register("passo13.teste", Begin);
            checks.Add(new Check { name = "Inscrição: a Elena lê, a tabuleta pulsa, a parede afunda e o ponto de retorno é marcado", moment = "passo13.parede", step = OpenWall });
            checks.Add(new Check { name = "Volta do ponto de retorno: parede já aberta, sem repetir leitura, pulso nem conversa", moment = "passo13.aberta", step = Reload });
            checks.Add(new Check { name = "Túnel caminhável até o topo da rampa; lamparinas acesas; só dois pontos de retorno; salvamento e fim do passo", moment = "passo13.aberta", step = Route });
            checks.Add(new Check { name = "Fita: pega, toca andando sem travar o controle, não é cortada ao sair e termina", moment = "passo13.fita", step = Tape });
            checks.Add(new Check { name = "Fita pega e não terminada: ao voltar do salvamento ela toca de novo", moment = "passo13.rampa", step = TapeResume });
            checks.Add(new Check { name = "Câmera no túnel estreito: nunca dentro de parede nem atravessando teto ou parede", moment = "passo13.aberta", step = CameraCollision });
            checks.Add(new Check { name = "Voltar à antecâmara com a passagem aberta: nada trava", moment = "passo13.antecamara", step = BackToHall });
        }

        [MenuItem("Nibiru/Testes/Passo 13/7. Teste automático (resumo no Console)")]
        static void Run() => Go("passo13.teste");

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
            RestoreTape();
            index++;
            phase = 0;
            if (index >= checks.Count)
            {
                EditorApplication.update -= Tick;
                Debug.Log("Passo 13, teste automático:\n" + report);
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            sawPulse = sawSinking = sawDialogue = false;
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
                RestoreTape();
                EditorApplication.update -= Tick;
                return;
            }
            float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
            if (elapsed > 60f)
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
                if (CloseDialogue()) return;
                checks[index].step();
            }
            catch (Exception e)
            {
                Result(false, $"erro {e.GetType().Name}: {e.Message}");
            }
        }

        // Olha a cada quadro, também durante as conversas (quando a etapa do teste não roda).
        static void Watch()
        {
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            if (runner != null && runner.IsRunning) sawDialogue = true;

            var passage = Object.FindFirstObjectByType<GuardianPassage>();
            if (passage == null) return;
            Light pulse = GetPulseLight(passage);
            if (pulse != null && pulse.enabled && pulse.intensity > 0.5f) sawPulse = true;
            if (passage.Wall != null && passage.Wall.gameObject.activeSelf && passage.Wall.position.y < wallStartY - 0.5f) sawSinking = true;
        }

        // ---------- Os testes ----------

        // Ler a inscrição, a tabuleta pulsar, a parede afundar e o ponto de retorno ser marcado.
        static bool OpenWall()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (phase == 1)
            {
                if (!Objective().StartsWith("Leia a inscrição")) { Result(false, $"objetivo errado: \"{Objective()}\""); return true; }
                GameObject wall = GameObject.Find("Parede_Barco_Junco");
                if (wall == null) { Result(false, "a parede não está na cena (rode o blockout e o menu do Poço)"); return true; }
                wallStartY = Object.FindFirstObjectByType<GuardianPassage>().Wall.position.y;

                // Fechada, a parede é sólida.
                Teleport(new Vector3(40f, -14f + FeetToCenter, 96f), 270f);
                player.enabled = false;
                for (int s = 0; s < 150; s++) controller.Move(Vector3.left * 0.1f + Vector3.down * 0.1f);
                float x = player.transform.position.x;
                player.enabled = true;
                if (x < 36.2f) { Result(false, $"a parede fechada não segurou a Elena (x={x:0.0})"); return true; }

                Teleport(new Vector3(37.8f, -14f + FeetToCenter, 96f), 270f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 0.4f) return false;
                var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
                if (!(interactor.Current is DialogueStarter) || interactor.Current.Prompt != "Ler a inscrição")
                {
                    Result(false, $"o botão de interagir não mostra \"Ler a inscrição\" (mostra \"{interactor.Current?.Prompt ?? "nada"}\")");
                    return true;
                }
                interactor.Current.Interact(interactor);
                phase = 3;
                return false;
            }
            if (phase == 3)
            {
                if (!Raised("AbriuAPassagem")) return false;
                mark = Time.time;
                phase = 4;
                return false;
            }
            // Depois de aberta: a fala da Elena e o ponto de retorno (a conversa pausa e o teste a fecha).
            if (!WorldState.IsCheckpointActivated("poco.checkpoint_passagem_aberta") && Time.time < mark + 6f) return false;
            var passage = Object.FindFirstObjectByType<GuardianPassage>();
            var input = player.GetComponent<PlayerInputReader>();
            bool ok = sawPulse && sawSinking && passage.IsOpen && !passage.Wall.gameObject.activeSelf
                && WorldState.IsCheckpointActivated("poco.checkpoint_passagem_aberta")
                && Raised("LeuAInscricaoDaPassagem") && Objective().StartsWith("Siga a passagem") && input.GameplayEnabled;
            Result(ok, $"pulsou={sawPulse}, afundou={sawSinking}, aberta={passage.IsOpen}, retorno={WorldState.IsCheckpointActivated("poco.checkpoint_passagem_aberta")}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Volta de um ponto de retorno depois da abertura: já aberta, sem leitura, pulso nem conversa.
        static bool Reload()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 1.5f) return false;
            var passage = Object.FindFirstObjectByType<GuardianPassage>();
            bool inscriptionGone = GameObject.Find("Ler_Inscricao") == null;
            Light pulse = GetPulseLight(passage);

            Teleport(new Vector3(38f, -14f + FeetToCenter, 96f), 270f);
            player.enabled = false;
            for (int s = 0; s < 100; s++) controller.Move(Vector3.left * 0.1f + Vector3.down * 0.1f);
            float x = player.transform.position.x;
            player.enabled = true;

            bool ok = passage.IsOpen && !passage.Wall.gameObject.activeSelf && inscriptionGone && !sawDialogue && !sawPulse
                && (pulse == null || !pulse.enabled) && x < 33f;
            Result(ok, $"aberta={passage.IsOpen}, inscrição sumiu={inscriptionGone}, conversa repetida={sawDialogue}, pulso repetido={sawPulse}, atravessou o vão até x={x:0.0}");
            return true;
        }

        // O túnel inteiro a pé, da parede ao topo da rampa; lamparinas, pontos de retorno e fim do passo.
        static bool Route()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                // O fim da primeira caminhada fica dentro da área do topo, para a física registrar a passagem
                // (ela só vê o gatilho entre quadros); depois a Elena segue até a beira do wadi.
                string stuck = WalkRoute(new[]
                {
                    new Vector3(38f, -14f, 96f), new Vector3(32f, -14f, 96f), new Vector3(32f, -14f, 104f), new Vector3(32f, -14f, 110f),
                    new Vector3(32f, -7f, 125f), new Vector3(32f, -0.5f, 139.2f),
                });
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 0.4f) return false;
                string stuck = WalkRoute(new[] { new Vector3(32f, -0.5f, 139.2f), new Vector3(32f, 0f, 141.5f) }, teleportFirst: false);
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (Time.time < mark + 0.6f) return false;

            var passage = Object.FindFirstObjectByType<GuardianPassage>();
            Light[] lamps = GetLamps(passage);
            bool lit = lamps.Length >= 3 && lamps.All(l => l != null && l.enabled && l.gameObject.activeInHierarchy && l.intensity > 0.5f);
            // Só dois pontos de retorno na passagem: o da abertura e o salvamento do topo (nenhum no meio do túnel).
            string[] names = GameObject.Find("Passagem_Dos_Guardioes") == null ? new string[0]
                : GameObject.Find("Passagem_Dos_Guardioes").GetComponentsInChildren<Checkpoint>(true).Select(c => c.name).OrderBy(n => n).ToArray();
            bool onlyTwo = names.SequenceEqual(new[] { "Autosave_Topo_Da_Rampa", "Checkpoint_Passagem_Aberta" });
            bool autosave = WorldState.IsCheckpointActivated("poco.autosave_topo_da_rampa");
            bool done = Raised("SaiuDaPassagem") && (Objective().StartsWith("nenhum") || Objective().StartsWith("Siga o wadi"));
            float y = player.transform.position.y;
            Result(lit && onlyTwo && autosave && done && y > -1f,
                $"lamparinas acesas={lamps.Length} ({lit}), pontos de retorno=[{string.Join(", ", names)}], salvamento={autosave}, saiu={Raised("SaiuDaPassagem")}, objetivo=\"{Objective()}\", y={y:0.0}");
            return true;
        }

        // Pegar a fita junto do degrau, ouvir enquanto anda, passar do topo da rampa com ela tocando.
        static bool Tape()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var input = player.GetComponent<PlayerInputReader>();
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>();
            if (phase == 1)
            {
                ShortenTape();
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 0.4f) return false;
                var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
                if (!(interactor.Current is TapePickup) || interactor.Current.Prompt != "Pegar a fita")
                {
                    Result(false, $"o botão de interagir não mostra \"Pegar a fita\" (mostra \"{interactor.Current?.Prompt ?? "nada"}\")");
                    return true;
                }
                interactor.Current.Interact(interactor);
                mark = Time.time;
                phase = 3;
                return false;
            }
            var subtitles = Object.FindFirstObjectByType<SubtitleUI>(FindObjectsInactive.Include);
            if (phase == 3)
            {
                if (Time.time < mark + 1.4f) return false;
                var runner = Object.FindFirstObjectByType<DialogueRunner>();
                bool playing = tapePlayer.IsPlaying && subtitles != null && subtitles.IsShowing;
                bool free = input.GameplayEnabled && !runner.IsRunning && Mathf.Approximately(Time.timeScale, 1f);
                bool gone = GameObject.Find("Pegar_Fita") == null;
                bool collected = Raised("ColetouAFitaDeIrena1") && Load<TapeDefinition>("Tapes/Fita_Irena_1").IsCollected;
                if (!(playing && free && gone && collected))
                {
                    Result(false, $"tocando com legenda={playing}, controle livre={free}, fita sumiu={gone}, coleta registrada={collected}");
                    return true;
                }
                // Anda até o topo da rampa com a fita ainda tocando.
                string stuck = WalkRoute(new[]
                {
                    new Vector3(32f, -14f, 106f), new Vector3(32f, -14f, 110f), new Vector3(32f, -7f, 125f), new Vector3(32f, -0.5f, 139.2f),
                });
                if (stuck != null) { Result(false, stuck); return true; }
                mark = Time.time;
                phase = 4;
                return false;
            }
            if (phase == 4)
            {
                if (Time.time < mark + 0.4f) return false;
                string beyond = WalkRoute(new[] { new Vector3(32f, -0.5f, 139.2f), new Vector3(32f, 0f, 141.5f) }, teleportFirst: false);
                if (beyond != null) { Result(false, beyond); return true; }
                mark = Time.time;
                phase = 45;
                return false;
            }
            if (phase == 45)
            {
                if (Time.time < mark + 0.4f) return false;
                if (!Raised("SaiuDaPassagem") || !tapePlayer.IsPlaying)
                {
                    Result(false, $"saiu={Raised("SaiuDaPassagem")}, a fita ainda toca={tapePlayer.IsPlaying}: ela foi cortada ao sair");
                    return true;
                }
                phase = 5;
                return false;
            }
            if (!Raised("FitaDeIrena1Terminou")) return false;
            bool hidden = subtitles == null || !subtitles.IsShowing;
            Result(!tapePlayer.IsPlaying && hidden, $"a fita terminou depois da saída, tocando={tapePlayer.IsPlaying}, legenda escondida={hidden}");
            return true;
        }

        // Fita pega e não terminada, guardada num salvamento: ao voltar, toca de novo do começo.
        static bool TapeResume()
        {
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 2f) return false;
            var tapePlayer = Object.FindFirstObjectByType<TapePlayer>();
            var tape = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            bool ok = tapePlayer.IsPlaying && tape.IsCollected && !tape.IsFinished;
            Result(ok, $"tocando={tapePlayer.IsPlaying}, coletada={tape.IsCollected}, terminada={tape.IsFinished}");
            return true;
        }

        // A câmera de verdade (ThirdPersonCameraMode), em pontos do túnel e da rampa, olhando para todos os lados.
        static bool CameraCollision()
        {
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 0.5f) return false;

            var mode = Object.FindFirstObjectByType<ThirdPersonCameraMode>();
            var controller = Object.FindFirstObjectByType<PlayerCameraController>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (mode == null) { Result(false, "câmera em terceira pessoa não encontrada"); return true; }
            var tmp = new GameObject("CameraDeTeste").transform;
            var violations = new List<string>();
            int samples = 0;
            float RampY(float z) => -14f + (z - 110f) * 14f / 30f;

            // Meio, junto das paredes, nos cantos e nas lamparinas, na rampa e sob a parede aberta.
            var stations = new List<Vector3>
            {
                new Vector3(36.5f, -14f, 96f), new Vector3(35.4f, -14f, 94.6f), new Vector3(35.4f, -14f, 97.4f), new Vector3(31f, -14f, 94.7f),
                new Vector3(30.7f, -14f, 97.4f), new Vector3(32f, -14f, 100f), new Vector3(30.7f, -14f, 100.5f), new Vector3(33.3f, -14f, 104.5f),
                new Vector3(32f, -14f, 106f), new Vector3(30.7f, -14f, 108f), new Vector3(33.3f, -14f, 109.4f),
                new Vector3(30.7f, RampY(112f), 112f), new Vector3(33.3f, RampY(118f), 118f), new Vector3(32f, RampY(125f), 125f),
                new Vector3(30.7f, RampY(132f), 132f), new Vector3(33.3f, RampY(138f), 138f),
            };
            Vector3 pivotOffset = Vector3.up * 0.6f;
            foreach (Vector3 station in stations)
            {
                Teleport(station + Vector3.up * FeetToCenter, 0f);
                Physics.SyncTransforms();
                Vector3 pivot = player.transform.position + pivotOffset;
                for (int yaw = 0; yaw < 360; yaw += 30)
                foreach (int pitch in new[] { -35, -10, 20, 60 })
                foreach (bool aiming in new[] { false, true })
                {
                    mode.Enter();
                    mode.SetAiming(aiming);
                    for (int i = 0; i < 20; i++) mode.UpdateCamera(tmp, yaw, pitch, 0.1f);
                    samples++;

                    Vector3 cam = tmp.position;
                    Collider inside = Physics.OverlapSphere(cam, 0.12f, ~0, QueryTriggerInteraction.Ignore)
                        .FirstOrDefault(c => !c.transform.IsChildOf(player.transform.root));
                    // Tudo o que está entre o ombro e a câmera (menos a própria Elena) é parede ou teto atravessado.
                    RaycastHit[] between = Physics.RaycastAll(pivot, cam - pivot, Vector3.Distance(pivot, cam), ~0, QueryTriggerInteraction.Ignore)
                        .Where(h => !h.collider.transform.IsChildOf(player.transform.root)).ToArray();
                    bool crossed = between.Length > 0;
                    RaycastHit hit = crossed ? between[0] : default;
                    if (inside != null || crossed)
                        violations.Add($"{station} yaw {yaw} pitch {pitch}{(aiming ? " mirando" : "")}: {(inside != null ? "dentro de " + inside.name : "atravessou " + hit.collider.name)}");
                }
            }
            mode.SetAiming(controller != null && controller.IsAiming);
            Object.Destroy(tmp.gameObject);
            Result(violations.Count == 0, violations.Count == 0
                ? $"{samples} posições de câmera em {stations.Count} pontos, nenhuma dentro de parede ou atravessando"
                : $"{violations.Count} de {samples} posições ruins, por exemplo: {string.Join("; ", violations.Take(4))}");
            return true;
        }

        // Do túnel de volta à antecâmara, com a parede aberta: sem travas, sem conversas nem eventos repetidos.
        static bool BackToHall()
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 1f) return false;
                player.enabled = false;
                for (int s = 0; s < 150; s++) controller.Move(Vector3.right * 0.1f + Vector3.down * 0.1f);
                float x = player.transform.position.x;
                player.enabled = true;
                if (x < 43f) { Result(false, $"parou em x={x:0.0} voltando para a antecâmara"); return true; }
                // De volta junto do vão da parede, nada para interagir (a inscrição já foi lida).
                Teleport(new Vector3(37.8f, -14f + FeetToCenter, 96f), 270f);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (Time.time < mark + 1.5f) return false;
            var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            var input = player.GetComponent<PlayerInputReader>();
            var servant = Object.FindFirstObjectByType<FirstServantFight>();
            bool servantDown = servant != null && servant.IsDefeated;
            bool free = input.GameplayEnabled && Mathf.Approximately(Time.timeScale, 1f);
            bool ok = interactor.Current == null && !sawDialogue && free && servantDown && Objective().StartsWith("Siga a passagem");
            Result(ok, $"interação perto do vão={interactor.Current?.Prompt ?? "nenhuma"}, conversa nova={sawDialogue}, controle livre={free}, Servo caído={servantDown}, objetivo=\"{Objective()}\"");
            return true;
        }

        // ---------- Ajudas ----------

        // Anda de ponto em ponto como a Elena (CharacterController); devolve onde parou se ficou presa.
        static string WalkRoute(Vector3[] route, bool teleportFirst = true)
        {
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = player.GetComponent<CharacterController>();
            if (teleportFirst) Teleport(route[0] + Vector3.up * FeetToCenter, 0f);
            player.enabled = false;
            string stuck = null;
            for (int i = 1; i < route.Length && stuck == null; i++)
            {
                for (int s = 0; s < 800; s++)
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
            return stuck;
        }

        // A fita com tempos curtos, só para o teste: a de verdade dura quase um minuto. Volta ao normal ao fim.
        static void ShortenTape()
        {
            var tape = Load<TapeDefinition>("Tapes/Fita_Irena_1");
            if (tapeBackup == null) tapeBackup = (tape.secondsPerCharacter, tape.minSecondsPerLine, tape.pauseBetweenLines);
            tape.secondsPerCharacter = 0.02f;
            tape.minSecondsPerLine = 0.8f;
            tape.pauseBetweenLines = 0.3f;
        }

        static void RestoreTape()
        {
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

        static Light GetPulseLight(GuardianPassage passage) =>
            (Light)typeof(GuardianPassage).GetField("pulseLight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(passage);

        static Light[] GetLamps(GuardianPassage passage) =>
            (Light[])typeof(GuardianPassage).GetField("lamps", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(passage);

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
                if (quest.kind == QuestKind.Principal) return step.LocalizedObjective;
            return "nenhum (missão principal concluída)";
        }
    }
}
