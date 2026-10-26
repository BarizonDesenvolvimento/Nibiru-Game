using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nibiru.Dialogue;
using Nibiru.Factions;
using Nibiru.Interaction;
using Nibiru.Inventory;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: teste automático da secundária "Remédio para Yusuf". Cobre o caminho da Dra. Weiss e a entrega
    /// ao Yusuf, o caminho da tenda médica seguido dos curativos para a Weiss (só o documento), a Weiss depois da
    /// entrega, o prazo (o Yusuf morre, a missão falha e isso sobrevive ao checkpoint), o lembrete da Elena, a
    /// pergunta do Samir na viagem com as duas escolhas (Yusuf vivo e morto) e a ausência dela sem a ajuda à Weiss.
    /// Em todos, confere os eventos e as flags gravados, a reputação, a missão principal livre e que nada é
    /// recompensado duas vezes. Cada cenário escreve uma linha no resumo final do Console (OK ou FALHOU).
    /// Menu: Nibiru > Testes > Secundárias > Remédio para Yusuf > Teste automático.
    /// </summary>
    [InitializeOnLoad]
    static class RemedyTests
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // ---------- Motor de passos (um cenário é uma sequência de "yield return") ----------

        abstract class Wait
        {
            public abstract bool Done();
        }

        sealed class Delay : Wait
        {
            readonly double until;
            public Delay(float seconds) => until = Now + seconds;
            public override bool Done() => Now >= until;
        }

        sealed class Until : Wait
        {
            readonly Func<bool> condition;
            readonly double limit;
            readonly string label;

            public Until(Func<bool> condition, float seconds, string label)
            {
                this.condition = condition;
                this.label = label;
                limit = Now + seconds;
            }

            public override bool Done()
            {
                if (condition()) return true;
                if (Now > limit) throw new TimeoutException("tempo esgotado: " + label);
                return false;
            }
        }

        static double Now => EditorApplication.timeSinceStartup;

        static readonly List<(string name, Func<IEnumerator> body)> scenarios = new List<(string, Func<IEnumerator>)>();
        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static readonly List<string> failures = new List<string>();
        static readonly List<string> posted = new List<string>();
        static readonly StringBuilder report = new StringBuilder();
        static Wait waiting;
        static int index;
        static bool running;

        static void Check(bool ok, string what)
        {
            if (!ok) failures.Add(what);
        }

        static RemedyTests()
        {
            Register("remedio.teste", Begin);

            scenarios.Add(("Caminho da Weiss: curativos, antibióticos e documento; entrega ao Yusuf; nada em dobro; tudo volta do checkpoint", WeissRoute));
            scenarios.Add(("Caminho da tenda: antibióticos, curativos à Weiss (só o documento), viagem e escolha de contar (Yusuf morto)", TentThenWeissThenRide));
            scenarios.Add(("Antibióticos já entregues antes: a Weiss troca os curativos só pelo documento", WeissAfterDelivery));
            scenarios.Add(("Prazo: sem entrega o Yusuf morre, a missão falha, a missão principal segue livre e a falha volta do checkpoint", Deadline));
            scenarios.Add(("Lembrete da Elena ao sair da passagem: só com antibióticos não entregues", Reminder));
            scenarios.Add(("Viagem com o Yusuf vivo: a Elena cobre a Weiss", () => Ride("remedio.viagem_vivo_cobrir", 0, true, false)));
            scenarios.Add(("Viagem com o Yusuf morto: a Elena cobre a Weiss", () => Ride("remedio.viagem_morto_cobrir", 0, true, true)));
            scenarios.Add(("Viagem sem ter ajudado a Weiss: a pergunta não aparece", () => Ride("remedio.viagem_sem_weiss", -1, false, true)));
        }

        [MenuItem("Nibiru/Testes/Secundárias/Remédio para Yusuf/15. Teste automático (resumo no Console)")]
        static void Run() => Go("remedio.teste");

        static void Begin()
        {
            report.Clear();
            posted.Clear();
            index = -1;
            running = true;
            RemedyShortcuts.AutoChoose = false;
            Notifications.Posted -= OnPosted;
            Notifications.PostedImportant -= OnPosted;
            Notifications.Posted += OnPosted;
            Notifications.PostedImportant += OnPosted;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            NextScenario();
        }

        static void OnPosted(string message) => posted.Add(message);

        static void NextScenario()
        {
            index++;
            stack.Clear();
            waiting = null;
            failures.Clear();
            if (index >= scenarios.Count)
            {
                Finish();
                return;
            }
            stack.Push(scenarios[index].body());
        }

        static void Finish()
        {
            running = false;
            EditorApplication.update -= Tick;
            Notifications.Posted -= OnPosted;
            Notifications.PostedImportant -= OnPosted;
            RemedyShortcuts.AutoChoose = true;
            Debug.Log("Remédio para Yusuf, teste automático:\n" + report);
        }

        static void EndScenario(Exception error)
        {
            string name = scenarios[index].name;
            if (error != null) failures.Add($"erro {error.GetType().Name}: {error.Message}");
            report.AppendLine(failures.Count == 0 ? $"OK  {name}" : $"FALHOU  {name}\n      - " + string.Join("\n      - ", failures));
            NextScenario();
        }

        static void Tick()
        {
            if (!running) return;
            if (!EditorApplication.isPlaying)
            {
                running = false;
                EditorApplication.update -= Tick;
                RemedyShortcuts.AutoChoose = true;
                return;
            }
            try
            {
                if (waiting != null)
                {
                    if (!waiting.Done()) return;
                    waiting = null;
                }
                while (stack.Count > 0)
                {
                    IEnumerator top = stack.Peek();
                    if (!top.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }
                    if (top.Current is IEnumerator inner)
                    {
                        stack.Push(inner);
                        continue;
                    }
                    if (top.Current is Wait wait) waiting = wait;
                    return;
                }
                EndScenario(null);
            }
            catch (Exception e)
            {
                EndScenario(e);
            }
        }

        // ---------- Cenários ----------

        static IEnumerator WeissRoute()
        {
            yield return Restart("remedio.weiss_curativo");
            int mainStep = MainStep();
            int foundation = Rep("FundacaoEnlil"), guardians = Rep("Guardioes");
            Check(Inv("Curativo") == 1 && Inv("Antibioticos") == 0, "o atalho deveria dar 1 curativo e nenhum antibiótico");
            Check(QuestStep("RemedioParaYusuf") == 0, "a missão deveria estar no passo 0");

            yield return Face(RemedyShortcuts.WeissFront, RemedyShortcuts.WeissYaw);
            yield return Interact("Dar os curativos à Dra. Weiss");
            Check(Raised("AjudouAWeiss") && Raised("ConseguiuAntibioticosComAWeiss"), "a troca deveria disparar AjudouAWeiss e ConseguiuAntibioticosComAWeiss");
            Check(!Raised("ConseguiuAntibioticosNaTenda"), "o caminho da tenda não deveria estar marcado");
            Check(Inv("Antibioticos") == 1 && Inv("Curativo") == 0, "a Weiss deveria levar o curativo e dar 1 antibiótico");
            yield return FinishDialogue("Remedio_Troca_Weiss");
            Check(Raised("WeissEntregouODocumento"), "o fim da conversa deveria disparar WeissEntregouODocumento");
            yield return new Until(() => Reading() != null && Reading().IsOpen, 3f, "o documento da Weiss na tela de leitura");
            Check(Raised("LeuODocumentoDaWeiss"), "ler o documento deveria disparar LeuODocumentoDaWeiss");
            CloseReading();
            yield return new Delay(0.5f);
            Check(QuestStep("RemedioParaYusuf") == 1, "com os antibióticos na bolsa, a missão deveria estar no passo 1 (levar ao Yusuf)");

            // A segunda troca não dá nada: nem outros antibióticos, nem outro documento.
            Give("Curativo", 1);
            yield return Face(RemedyShortcuts.WeissFront, RemedyShortcuts.WeissYaw);
            Check(Prompt() == "Falar com a Dra. Weiss", $"depois da troca a Weiss só conversa (aviso atual: \"{Prompt()}\")");
            yield return Interact("Falar com a Dra. Weiss");
            yield return FinishDialogue("Trincheiras_DraWeissDepois");
            Check(Inv("Antibioticos") == 1 && Inv("Curativo") == 1, "uma segunda visita não deveria trocar nem dar nada");

            yield return Face(RemedyShortcuts.YusufFront, 270f);
            yield return Interact("Dar os antibióticos ao Yusuf");
            Check(Raised("EntregouAntibioticos"), "a entrega deveria disparar EntregouAntibioticos");
            Check(Raised("YusufSobreviveu") && !Raised("YusufMorreu"), "o Yusuf deveria sobreviver");
            Check(Inv("Antibioticos") == 0, "os antibióticos deveriam sair da bolsa na entrega");
            Check(QuestStep("RemedioParaYusuf") == 2, "a missão deveria estar concluída (passo 2)");
            yield return FinishDialogue("Remedio_Entrega_Yusuf");
            Check(posted.Count(p => p.Contains("Missão concluída: Remédio para Yusuf")) == 1, "o aviso de missão concluída deveria sair uma vez");

            // Nada em dobro: o mesmo evento outra vez não conclui nem recompensa de novo; o Yusuf só conversa.
            RaiseEv("EntregouAntibioticos");
            Give("Antibioticos", 1);
            yield return new Delay(0.4f);
            yield return Face(RemedyShortcuts.YusufFront, 270f);
            Check(posted.Count(p => p.Contains("Missão concluída: Remédio para Yusuf")) == 1, "a missão não deveria concluir duas vezes");
            Check(Prompt() == "Falar com Yusuf", $"depois da entrega o Yusuf só conversa (aviso atual: \"{Prompt()}\")");
            Check(Rep("FundacaoEnlil") == foundation && Rep("Guardioes") == guardians, "a missão não deveria mexer na reputação");

            // O prazo do wadi já foi cumprido: nada acontece.
            RaiseEv("EscolheuONoWadi");
            yield return new Delay(0.4f);
            Check(!Raised("YusufMorreu") && !Raised("UmmKarimFria"), "com a entrega feita, o prazo não pode matar o Yusuf");
            Check(!Quests().IsFailed(QuestRemedy()), "a missão concluída não pode falhar");
            Check(MainStep() >= mainStep, "a missão principal não pode andar para trás");

            yield return SaveAndReload();
            Check(Raised("YusufSobreviveu") && Raised("AjudouAWeiss") && Raised("EntregouAntibioticos"), "as flags deveriam voltar do checkpoint");
            Check(StarterDialogue("Falar_UmmKarim") == "Aldeia_UmmKarimGrata", $"a Umm Karim deveria agradecer (fala atual: {StarterDialogue("Falar_UmmKarim")})");
            Check(Quests().IsCompleted(QuestRemedy()), "a missão deveria continuar concluída");
        }

        static IEnumerator TentThenWeissThenRide()
        {
            yield return Restart("remedio.tenda");
            Check(QuestStep("RemedioParaYusuf") == 0, "a missão deveria começar no passo 0");
            yield return Face(RemedyShortcuts.TentFront, 0f);
            yield return Interact("Pegar Antibióticos");
            Check(Raised("ConseguiuAntibioticosNaTenda"), "pegar na tenda deveria disparar ConseguiuAntibioticosNaTenda");
            Check(!Raised("ConseguiuAntibioticosComAWeiss"), "o caminho da Weiss não deveria estar marcado");
            yield return new Delay(0.4f);
            Check(Inv("Antibioticos") == 1 && QuestStep("RemedioParaYusuf") == 1, "com os antibióticos na bolsa, a missão deveria estar no passo 1");

            // Depois, curativos para a Weiss: só o documento, os antibióticos não vêm em dobro.
            RaiseEv("ConheceuDraWeiss");
            Give("Curativo", 1);
            yield return Face(RemedyShortcuts.WeissFront, RemedyShortcuts.WeissYaw);
            yield return Interact("Dar os curativos à Dra. Weiss");
            Check(Raised("AjudouAWeiss"), "a troca deveria disparar AjudouAWeiss mesmo sem antibióticos de presente");
            Check(!Raised("ConseguiuAntibioticosComAWeiss"), "quem já tem os antibióticos não os recebe de novo");
            Check(Inv("Antibioticos") == 1 && Inv("Curativo") == 0, "a Weiss leva o curativo e não dá antibióticos");
            yield return FinishDialogue("Remedio_Troca_Weiss_SoDocumento");
            yield return new Until(() => Reading() != null && Reading().IsOpen, 3f, "o documento da Weiss na tela de leitura");
            Check(Raised("LeuODocumentoDaWeiss"), "ler o documento deveria disparar LeuODocumentoDaWeiss");
            CloseReading();

            // Esta é a mesma situação dos atalhos da viagem "caminho da tenda": as flags são as mesmas.
            foreach (string e in RemedyShortcuts.TentRoute)
                Check(Raised(e), $"faltou o evento {e}");
            yield return Ride("remedio.viagem_morto_contar", 1, true, true);
        }

        static IEnumerator WeissAfterDelivery()
        {
            yield return Restart("remedio.passo7_com_antibioticos");
            yield return Face(RemedyShortcuts.YusufFront, 270f);
            yield return Interact("Dar os antibióticos ao Yusuf");
            yield return FinishDialogue("Remedio_Entrega_Yusuf");
            Check(Raised("YusufSobreviveu"), "o Yusuf deveria sobreviver");

            RaiseEv("ConheceuDraWeiss");
            Give("Curativo", 1);
            yield return Face(RemedyShortcuts.WeissFront, RemedyShortcuts.WeissYaw);
            yield return Interact("Dar os curativos à Dra. Weiss");
            Check(Raised("AjudouAWeiss"), "a troca deveria disparar AjudouAWeiss");
            Check(!Raised("ConseguiuAntibioticosComAWeiss") && Inv("Antibioticos") == 0, "quem já entregou o remédio não ganha outro");
            yield return FinishDialogue("Remedio_Troca_Weiss_SoDocumento");
            yield return new Until(() => Reading() != null && Reading().IsOpen, 3f, "o documento da Weiss na tela de leitura");
            CloseReading();
            Check(Raised("LeuODocumentoDaWeiss"), "o documento deveria ser entregue mesmo assim");
        }

        static IEnumerator Deadline()
        {
            yield return Restart("remedio.prazo");
            QuestDefinition quest = QuestRemedy();
            Check(Inv("Antibioticos") == 1, "o atalho deveria dar os antibióticos");
            Check(!Quests().IsFailed(quest) && !Raised("YusufMorreu"), "antes do prazo nada deveria ter falhado");
            int mainStep = MainStep();
            int foundation = Rep("FundacaoEnlil"), guardians = Rep("Guardioes");
            posted.Clear();

            RaiseEv("EscolheuONoWadi");
            yield return new Delay(0.6f);
            Check(Raised("YusufMorreu") && Raised("UmmKarimFria"), "o prazo deveria disparar YusufMorreu e UmmKarimFria");
            Check(Quests().IsFailed(quest), "a missão deveria estar em falha");
            Check(!Quests().ActiveObjectives().Any(o => o.quest == quest), "a missão que falhou não pode continuar na lista de objetivos");
            Check(posted.Count(p => p.Contains("Missão falhou: Remédio para Yusuf")) == 1, "o aviso de falha deveria sair uma vez");
            Check(MainStep() > mainStep, "a missão principal deveria seguir normalmente");
            Check(Rep("FundacaoEnlil") == foundation && Rep("Guardioes") == guardians, "o prazo não mexe na reputação");

            yield return SaveAndReload();
            Check(Raised("YusufMorreu") && Raised("UmmKarimFria"), "as flags da falha deveriam voltar do checkpoint");
            Check(Quests().IsFailed(quest), "o estado de falha deveria voltar do checkpoint");
            Check(!Quests().ActiveObjectives().Any(o => o.quest == quest), "depois do checkpoint a missão não volta à lista");
            Check(StarterDialogue("Falar_UmmKarim") == "Aldeia_UmmKarimFria", $"a Umm Karim deveria estar fria (fala atual: {StarterDialogue("Falar_UmmKarim")})");
            yield return Face(RemedyShortcuts.YusufFront, 270f);
            Give("Antibioticos", 1);
            yield return new Delay(0.3f);
            Check(Prompt() != "Dar os antibióticos ao Yusuf", "depois do prazo a entrega não pode mais acontecer");
            Check(!Raised("EntregouAntibioticos"), "a entrega não pode ter acontecido");
        }

        static IEnumerator Reminder()
        {
            yield return Restart("remedio.lembrete");
            Check(Inv("Antibioticos") == 1, "o atalho deveria dar os antibióticos");
            RaiseEv("SaiuDaPassagem");
            yield return new Until(() => Runner().IsRunning, 3f, "a fala da Elena lembrando do Yusuf");
            Check(CurrentDialogue() == "Remedio_Lembrete_Yusuf", $"a fala deveria ser o lembrete (atual: {CurrentDialogue()})");
            yield return FinishDialogue();

            yield return Restart("remedio.lembrete_sem");
            Check(Inv("Antibioticos") == 0, "o atalho não deveria dar antibióticos");
            RaiseEv("SaiuDaPassagem");
            yield return new Delay(1.5f);
            Check(!Runner().IsRunning, "sem antibióticos a Elena não deveria lembrar do Yusuf");
        }

        // A viagem: com a pergunta (e a escolha pick: 0 cobrir, 1 contar) ou sem ela. dead: o Yusuf deveria ter morrido.
        static IEnumerator Ride(string moment, int pick, bool question, bool dead)
        {
            yield return Restart(moment);
            int foundation = Rep("FundacaoEnlil"), guardians = Rep("Guardioes");
            Check(Raised("YusufMorreu") == dead, dead ? "o Yusuf deveria estar morto (sem entrega até o fim do wadi)" : "o Yusuf deveria estar vivo");
            Check(Raised("UmmKarimFria") == dead, "a Umm Karim fria deveria acompanhar a morte do Yusuf");
            Check(Raised("AjudouAWeiss") == question, "a pergunta depende só de AjudouAWeiss");

            TruckRide ride = Object.FindFirstObjectByType<TruckRide>(FindObjectsInactive.Include);
            typeof(TruckRide).GetField("minSeconds", Any).SetValue(ride, 8f);

            var nodes = new List<string>();
            int choices = 0;
            DialogueRunner runner = Runner();
            Action<DialogueNode, IReadOnlyList<DialogueChoice>> onNode = (node, visible) =>
            {
                nodes.Add(node.id);
                if (node.id == "s1") choices = visible.Count;
            };
            runner.NodeShown += onNode;

            double limit = Now + 150;
            while (!Raised("FimDaFatia") && Now < limit)
            {
                CloseReading();
                if (runner.IsRunning)
                {
                    if (VisibleChoices(runner) > 0) runner.Choose(Mathf.Max(pick, 0));
                    else runner.Continue();
                }
                yield return null;
            }
            runner.NodeShown -= onNode;

            Check(Raised("FimDaFatia"), "a viagem deveria terminar no fim da fatia");
            Check(nodes.Contains("s1") == question, question ? "a pergunta do Samir deveria aparecer" : "a pergunta do Samir não deveria aparecer");
            if (question)
            {
                Check(choices == 2, $"a pergunta deveria ter 2 respostas (tem {choices})");
                bool cover = pick == 0;
                Check(Raised("CobriuAWeiss") == cover && Raised("DenunciouAWeiss") == !cover, cover ? "deveria ter gravado CobriuAWeiss" : "deveria ter gravado DenunciouAWeiss");
                Check(Rep("FundacaoEnlil") == foundation + (cover ? 5 : -5), $"Fundação deveria mudar {(cover ? "+5" : "-5")} (era {foundation}, ficou {Rep("FundacaoEnlil")})");
                Check(Rep("Guardioes") == guardians + (cover ? -5 : 5), $"Guardiões deveriam mudar {(cover ? "-5" : "+5")} (era {guardians}, ficou {Rep("Guardioes")})");
            }
            else
            {
                Check(!Raised("CobriuAWeiss") && !Raised("DenunciouAWeiss"), "sem a pergunta, nenhuma das duas flags");
                Check(Rep("FundacaoEnlil") == foundation && Rep("Guardioes") == guardians, "sem a pergunta, a reputação não muda");
            }
        }

        // ---------- Ajudas ----------

        // Aplica o atalho e espera ele terminar de arrumar o mundo.
        static IEnumerator Restart(string moment)
        {
            RemedyShortcuts.Ready = false;
            RemedyShortcuts.Launch(moment);
            yield return new Until(() => RemedyShortcuts.Ready, 60f, $"o atalho {moment} ficar pronto");
            yield return new Delay(1f);
        }

        static IEnumerator Face(Vector3 position, float yaw)
        {
            Teleport(position, yaw);
            yield return new Delay(0.7f);
        }

        // Aperta o botão de interagir no objeto em foco, conferindo antes o aviso na tela.
        static IEnumerator Interact(string prompt)
        {
            PlayerInteractor interactor = Motor().GetComponent<PlayerInteractor>();
            IInteractable current = interactor.Current;
            bool ok = current != null && current.Prompt == prompt;
            Check(ok, $"aviso esperado \"{prompt}\", atual \"{current?.Prompt}\"");
            if (ok) current.Interact(interactor);
            yield return new Delay(0.3f);
        }

        // Passa as falas da conversa aberta, conferindo qual é.
        static IEnumerator FinishDialogue(string expected = null)
        {
            DialogueRunner runner = Runner();
            Check(runner.IsRunning, "uma conversa deveria estar aberta");
            if (expected != null) Check(CurrentDialogue() == expected, $"conversa esperada {expected}, atual {CurrentDialogue()}");
            double limit = Now + 30;
            while (runner.IsRunning && Now < limit)
            {
                runner.Continue();
                yield return null;
            }
            Check(!runner.IsRunning, "a conversa deveria ter acabado");
            yield return new Delay(0.3f);
        }

        // Guarda um ponto de retorno aqui e recarrega a cena a partir dele (como morrer e voltar).
        static IEnumerator SaveAndReload()
        {
            PlayerMotor motor = Motor();
            int old = motor.GetInstanceID();
            var point = new GameObject("Teste_Retorno").transform;
            point.SetPositionAndRotation(motor.transform.position, motor.transform.rotation);
            motor.GetComponent<PlayerRespawn>().SaveCheckpoint(point);
            Object.Destroy(point.gameObject);
            Checkpoints.Restart(RegionSceneName);
            yield return new Until(() => Motor() != null && Motor().GetInstanceID() != old, 30f, "a cena recarregar");
            yield return new Delay(1.5f);
        }

        static PlayerMotor Motor() => Object.FindFirstObjectByType<PlayerMotor>();

        static DialogueRunner Runner() => Object.FindFirstObjectByType<DialogueRunner>();

        static QuestManager Quests() => Object.FindFirstObjectByType<QuestManager>();

        static QuestDefinition QuestRemedy() => Load<QuestDefinition>("Quests/RemedioParaYusuf");

        static ReadingScreenUI Reading() => Object.FindFirstObjectByType<ReadingScreenUI>();

        static void CloseReading()
        {
            ReadingScreenUI reading = Reading();
            if (reading != null && reading.IsOpen) typeof(ReadingScreenUI).GetMethod("Close", Any).Invoke(reading, null);
        }

        static bool Raised(string eventName) => StoryEvents.HasBeenRaised(Load<StoryEvent>("Events/" + eventName));

        static void RaiseEv(string eventName) => StoryEvents.Raise(Load<StoryEvent>("Events/" + eventName));

        static int Inv(string item) => Object.FindFirstObjectByType<PlayerInventory>().CountOf(Load<ItemDefinition>("Items/" + item));

        static int Rep(string faction) => Reputation.Get(Load<FactionDefinition>("Factions/" + faction));

        static int QuestStep(string quest) => WorldState.TryGetQuestStep(quest, out int step) ? step : -1;

        static int MainStep() => QuestStep("Sobrevivente");

        static string Prompt() => Motor().GetComponent<PlayerInteractor>().Current?.Prompt;

        static void Give(string item, int count) => RemedyShortcuts.Give(item, count);

        static string CurrentDialogue() =>
            (typeof(DialogueRunner).GetField("current", Any).GetValue(Runner()) as DialogueDefinition)?.name;

        static int VisibleChoices(DialogueRunner runner) =>
            ((IList)typeof(DialogueRunner).GetField("visibleChoices", Any).GetValue(runner)).Count;

        // A conversa que um personagem (a zona dele) vai tocar agora.
        static string StarterDialogue(string zone)
        {
            GameObject go = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == zone && g.scene.IsValid() && g.scene.isLoaded);
            var starter = go != null ? go.GetComponent<DialogueStarter>() : null;
            return starter == null ? "(sem DialogueStarter)" : (typeof(DialogueStarter).GetField("dialogue", Any).GetValue(starter) as DialogueDefinition)?.name;
        }
    }
}
