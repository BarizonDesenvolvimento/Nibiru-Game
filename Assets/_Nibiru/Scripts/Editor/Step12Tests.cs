using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Player;
using Nibiru.Powers;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Stealth;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático do passo 12 (a luta com o Primeiro Servo). Roda os atalhos em
    /// sequência e confere o despertar (urro, o Lullu fugindo, o ponto de retorno), a volta do
    /// checkpoint sem repetir nada, a fase 1 presa ao chão, cada troca de fase com os sinais, a subida
    /// das rampas na fase 2, a distração do holofote, a armadura e o Eco, e a vitória até a parede do
    /// barco. Fecha sozinho as conversas. No fim, escreve um resumo curto no Console (OK ou FALHOU).
    /// Menu: Nibiru > Testes > Passo 12 > 7. Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class Step12Tests
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
        static float mark, startY, maxY, sealBase, lightBase;
        static bool sawA, sawB, sawC, sawD;

        static Step12Tests()
        {
            Register("passo12.teste", Begin);
            checks.Add(new Check { name = "Despertar: o Servo sai do Selo urrando, o Lullu foge e o retorno fica na boca", moment = "passo12.flash", step = Wake });
            checks.Add(new Check { name = "Volta do checkpoint da luta: Servo acordado junto ao Selo, sem flash nem urro, sem Lullu", moment = "passo12.luta", step = Reload });
            checks.Add(new Check { name = "Fase 1: preso ao chão (a Elena na plataforma oeste)", moment = "passo12.luta", step = () => Climb(false) });
            checks.Add(new Check { name = "Troca para a fase 2: urro parado, lascas da máscara, olhos, Selo e holofotes", moment = "passo12.luta", step = PhaseTwo });
            checks.Add(new Check { name = "Fase 2: sobe a rampa atrás da Elena", moment = "passo12.luta", step = () => Climb(true) });
            checks.Add(new Check { name = "Holofote derrubado: o estrondo distrai o Servo", moment = "passo12.luta", step = Floodlight });
            checks.Add(new Check { name = "Armadura e Eco: tiro tira 25%, atordoado tira 150%; o Eco tem recarga", moment = "passo12.luta", step = Armor });
            checks.Add(new Check { name = "Troca para a fase 3: avisa menos e cansa mais", moment = "passo12.luta", step = PhaseThree });
            checks.Add(new Check { name = "Vitória: tomba, o Selo enfraquece e o objetivo leva à parede do barco", moment = "passo12.luta", step = Victory });
            checks.Add(new Check { name = "Volta do checkpoint depois da vitória: o Servo continua caído", moment = "passo12.vitoria", step = AfterVictory });
        }

        [MenuItem("Nibiru/Testes/Passo 12/7. Teste automático (resumo no Console)")]
        static void Run() => Go("passo12.teste");

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
                Debug.Log("Passo 12, teste automático:\n" + report);
                return;
            }
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            oldPlayer = player != null ? player.GetInstanceID() : 0;
            startedAt = EditorApplication.timeSinceStartup;
            sawA = sawB = sawC = sawD = false;
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
            if (elapsed > 50f)
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
            var fight = Servant();
            if (fight == null || !fight.gameObject.activeInHierarchy) return;
            var brain = fight.GetComponent<LulluAI>();
            if (fight.IsRoaring && !brain.enabled) sawA = true;
            if (fight.GetComponentsInChildren<AudioSource>().Any(s => s.name == "Som_Urro" && s.isPlaying)) sawB = true;
            if (GameObject.Find("Lasca_Mascara") != null) sawC = true;
            var seal = GameObject.Find("Luz_Selo");
            if (seal != null && sealBase > 0f && seal.GetComponent<Light>().intensity > sealBase * 1.3f) sawD = true;
        }

        // ---------- Os testes ----------

        // Do flash ao despertar: o Servo aparece urrando (parado), o Lullu foge e some, o retorno é marcado.
        static bool Wake()
        {
            if (phase == 1)
            {
                if (!Raised("PassouPeloLullu")) return false;
                sealBase = GameObject.Find("Luz_Selo").GetComponent<Light>().intensity;
                Teleport(new Vector3(45f, -14f + FeetToCenter, 90.6f), 0f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (!Raised("PrimeiroServoAcordou")) return false;
                mark = Time.time;
                phase = 3;
                return false;
            }
            var fight = Servant();
            var lulluLeft = Object.FindObjectsByType<LulluAI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l.GetComponent<FirstServantFight>() == null).ToArray();
            bool lulluGone = lulluLeft.All(l => !l.gameObject.activeInHierarchy);
            if ((!lulluGone || fight.IsRoaring) && Time.time < mark + 15f) return false;

            bool checkpoint = WorldState.IsCheckpointActivated("poco.checkpoint_luta");
            bool bar = GameObject.Find("Barra_Primeiro_Servo") != null;
            bool seen = DialogueRunner.HasSeen(Load<DialogueDefinition>("Dialogues/Poco_OPrimeiroServo"));
            bool ok = fight.IsAwake && sawA && sawB && sawD && lulluGone && checkpoint && bar && seen && Objective() == "Sobreviva ao Primeiro Servo";
            Result(ok, $"acordou={fight.IsAwake}, urrou parado={sawA}, som={sawB}, Selo clareou={sawD}, Lullu sumiu={lulluGone}, " +
                $"retorno na boca={checkpoint}, barra={bar}, fala={seen}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Voltando do checkpoint da luta: o Servo já acordado perto do Selo, sem urro nem flash, e sem o Lullu.
        static bool Reload()
        {
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 2f) return false;
            var fight = Servant();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var lullu = Object.FindObjectsByType<LulluAI>(FindObjectsSortMode.None).Where(l => l.GetComponent<FirstServantFight>() == null).ToArray();
            var flash = Object.FindFirstObjectByType<ScreenFlash>();
            float fromSeal = Vector3.Distance(Flat(fight.transform.position), new Vector3(45f, 0f, 101.8f));
            float fromSpot = Vector3.Distance(Flat(player.transform.position), new Vector3(45f, 0f, 83f));
            bool ok = fight.IsAwake && !sawA && lullu.Length == 0 && (flash == null || !flash.IsCovering) && fromSeal < 8f && fromSpot < 1f;
            Result(ok, $"acordado={fight.IsAwake}, urrou={sawA}, Lullu na cena={lullu.Length}, flash={(flash != null && flash.IsCovering)}, " +
                $"Servo a {fromSeal:0.0} m do Selo, Elena a {fromSpot:0.0} m da boca");
            return true;
        }

        // A Elena na plataforma oeste (3 m) fazendo barulho. Na fase 1 ele não sobe; na fase 2 sobe a rampa.
        static bool Climb(bool phaseTwo)
        {
            var fight = Servant();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                if (phaseTwo) fight.StartAtPhase(2);
                startY = maxY = fight.transform.position.y;
                Teleport(new Vector3(39.5f, -11f + FeetToCenter, 87f), 90f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            var brain = fight.GetComponent<LulluAI>();
            maxY = Mathf.Max(maxY, fight.transform.position.y);
            if (brain.CurrentState == LulluAI.State.Golpe || brain.CurrentState == LulluAI.State.Investida) sawB = true;
            bool climbed = maxY > startY + 2f;
            if (Time.time < mark + (phaseTwo ? 20f : 9f) && !(phaseTwo && climbed))
            {
                if (Mathf.Repeat(Time.time - mark, 0.5f) < Time.deltaTime + 0.02f)
                    Noise.Emit(player.transform.position, 20f, player.gameObject);
                return false;
            }
            bool ok = phaseTwo ? climbed && brain.MaxClimb == 0f : !climbed && !sawB;
            Result(ok, $"subiu {maxY - startY:0.0} m em {Time.time - mark:0} s, golpeou de baixo={sawB}, limite de subida={brain.MaxClimb:0.#} m");
            return true;
        }

        // A vida cai abaixo de 60%: ele para e urra, a máscara solta lascas, os olhos acendem mais, o Selo
        // clareia e os holofotes piscam. Depois: duas investidas seguidas e sobe rampas.
        static bool PhaseTwo()
        {
            var fight = Servant();
            if (phase == 1)
            {
                sealBase = GameObject.Find("Luz_Selo").GetComponent<Light>().intensity;
                lightBase = Lamp("Holofote_Plataforma_Oeste").intensity;
                var health = fight.GetComponent<Health>();
                health.Restore(health.Max * 0.59f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            // Holofote piscando: o brilho saiu do normal em algum quadro (depois disso, lightBase = -1).
            if (Mathf.Abs(Lamp("Holofote_Plataforma_Oeste").intensity - lightBase) > 0.01f) lightBase = -1f;
            if (fight.IsRoaring || Time.time < mark + 0.5f)
            {
                if (Time.time > mark + 10f) Result(false, "o urro não terminou");
                return false;
            }
            var brain = fight.GetComponent<LulluAI>();
            var eyes = fight.GetComponentsInChildren<Light>().FirstOrDefault(l => l.name == "Olhos_LapisLazuli");
            bool flickered = lightBase < 0f;
            bool ok = fight.Phase == 2 && sawA && sawB && sawC && sawD && flickered && brain.enabled && brain.LungeChain == 2 && brain.MaxClimb == 0f && eyes != null && eyes.intensity > 2f;
            Result(ok, $"fase={fight.Phase}, parou urrando={sawA}, som={sawB}, lascas={sawC}, Selo clareou={sawD}, holofotes piscaram={flickered}, " +
                $"olhos={(eyes != null ? eyes.intensity : 0f):0.0}, investidas seguidas={brain.LungeChain}, sobe rampas={brain.MaxClimb == 0f}, voltou a caçar={brain.enabled}");
            return true;
        }

        // A Elena na plataforma leste, fazendo barulho; o holofote do canto noroeste cai. O Servo vai até o
        // estrondo e, distraído, não segue os passos dela.
        static bool Floodlight()
        {
            var fight = Servant();
            var brain = fight.GetComponent<LulluAI>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            var light = GameObject.Find("Holofote_Canto_Noroeste").GetComponent<FallingFloodlight>();
            if (phase == 1)
            {
                Teleport(new Vector3(51f, -10f + FeetToCenter, 97f), 270f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            bool noisy = Mathf.Repeat(Time.time - mark, 0.5f) < Time.deltaTime + 0.02f;
            if (phase == 2)
            {
                if (noisy) Noise.Emit(player.transform.position, 20f, player.gameObject);
                if (Time.time < mark + 3f) return false;
                light.GetComponent<Health>().TakeDamage(new DamageInfo(34f, light.transform.position, player.gameObject));
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (noisy) Noise.Emit(player.transform.position, 20f, player.gameObject);
            if (brain.IsDistracted) sawA = true;
            if (sawA && brain.CurrentState == LulluAI.State.Cacando) sawB = true;
            if (Time.time < mark + 4f) return false;
            float toLight = Vector3.Distance(Flat(fight.transform.position), Flat(light.transform.position));
            bool ok = light.IsDown && sawA && !sawB;
            Result(ok, $"caiu={light.IsDown}, distraído={sawA}, voltou a caçar a Elena durante a distração={sawB}, Servo a {toLight:0.0} m do holofote");
            return true;
        }

        // Um tiro (100 de dano) tira 25 sem atordoar; atordoado pelo Eco, tira 150. O Eco tem recarga.
        static bool Armor()
        {
            var fight = Servant();
            var health = fight.GetComponent<Health>();
            var brain = fight.GetComponent<LulluAI>();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            float before = health.Current;
            health.TakeDamage(new DamageInfo(100f, fight.transform.position, player.gameObject));
            float normal = before - health.Current;
            brain.OnEcho(player.transform.position);
            bool stunned = brain.CurrentState == LulluAI.State.Atordoado;
            before = health.Current;
            health.TakeDamage(new DamageInfo(100f, fight.transform.position, player.gameObject));
            float onStun = before - health.Current;
            float cooldown = Load<EchoPowerDefinition>("Powers/EcoDoAbzu").cooldown;
            bool ok = Mathf.Approximately(normal, 25f) && stunned && Mathf.Approximately(onStun, 150f) && cooldown >= 8f;
            Result(ok, $"tiro tirou {normal:0}, atordoado={stunned}, atordoado tirou {onStun:0}, recarga do Eco={cooldown:0} s");
            return true;
        }

        // A vida cai abaixo de 25% na fase 2: urro e fase 3, com preparação mais curta e mais cansaço.
        static bool PhaseThree()
        {
            var fight = Servant();
            if (phase == 1)
            {
                fight.StartAtPhase(2);
                var health = fight.GetComponent<Health>();
                health.Restore(health.Max * 0.24f);
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (fight.IsRoaring || Time.time < mark + 0.5f)
            {
                if (Time.time > mark + 10f) Result(false, "o urro não terminou");
                return false;
            }
            var brain = fight.GetComponent<LulluAI>();
            bool ok = fight.Phase == 3 && sawA && sawC && brain.WindupScale < 1f && brain.RecoveryScale > 1f;
            Result(ok, $"fase={fight.Phase}, urrou={sawA}, lascas={sawC}, preparação x{brain.WindupScale:0.0#}, cansaço x{brain.RecoveryScale:0.0#}");
            return true;
        }

        // Vencido: tomba, o azul do Selo enfraquece, a barra some, a Elena fala e o objetivo leva à parede
        // do barco. Na parede, o passo 12 fecha.
        static bool Victory()
        {
            var fight = Servant();
            var player = Object.FindFirstObjectByType<PlayerMotor>();
            if (phase == 1)
            {
                sealBase = GameObject.Find("Luz_Selo").GetComponent<Light>().intensity;
                var health = fight.GetComponent<Health>();
                health.Restore(5f);
                health.TakeDamage(new DamageInfo(100f, fight.transform.position, player.gameObject));
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (phase == 2)
            {
                if (Time.time < mark + 4f) return false;
                float seal = GameObject.Find("Luz_Selo").GetComponent<Light>().intensity;
                bool bar = GameObject.Find("Barra_Primeiro_Servo") != null;
                bool seen = DialogueRunner.HasSeen(Load<DialogueDefinition>("Dialogues/Poco_OServoCai"));
                bool checkpoint = WorldState.IsCheckpointActivated("poco.checkpoint_depois_do_servo");
                string objective = Objective();
                bool ok = fight.IsDefeated && Raised("VenceuOPrimeiroServo") && seal < sealBase * 0.5f && !bar && seen && checkpoint
                    && objective.StartsWith("Procure a passagem");
                if (!ok)
                {
                    Result(false, $"caiu={fight.IsDefeated}, venceu={Raised("VenceuOPrimeiroServo")}, Selo {seal:0.0} de {sealBase:0.0}, barra={bar}, fala={seen}, " +
                        $"retorno={checkpoint}, objetivo=\"{objective}\"");
                    return true;
                }
                Teleport(new Vector3(38.5f, -14f + FeetToCenter, 96f), 270f);
                mark = Time.time;
                phase = 3;
                return false;
            }
            if (Time.time < mark + 1f) return false;
            Result(Raised("ChegouAParedeDoBarco"), $"Selo enfraqueceu, barra sumiu, fala e retorno OK; na parede do barco={Raised("ChegouAParedeDoBarco")}, objetivo=\"{Objective()}\"");
            return true;
        }

        // Voltando depois da vitória: o Servo caído diante do Selo, sem barra, e o objetivo da parede.
        static bool AfterVictory()
        {
            if (phase == 1)
            {
                mark = Time.time;
                phase = 2;
                return false;
            }
            if (Time.time < mark + 2f) return false;
            var fight = Servant();
            bool lying = fight != null && fight.IsDefeated && fight.GetComponent<Health>().IsDead;
            bool bar = GameObject.Find("Barra_Primeiro_Servo") != null;
            bool ok = lying && !bar && Objective().StartsWith("Procure a passagem");
            Result(ok, $"caído={lying}, barra={bar}, objetivo=\"{Objective()}\"");
            return true;
        }

        // ---------- Ajudas ----------

        static FirstServantFight Servant() => Object.FindFirstObjectByType<FirstServantFight>();

        static Light Lamp(string floodlight) => GameObject.Find(floodlight).GetComponentInChildren<Light>();

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

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
