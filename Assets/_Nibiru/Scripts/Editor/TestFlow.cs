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
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: base dos testes automáticos escritos como roteiro. Um cenário é um método que usa "yield return"
    /// para esperar (um tempo, uma condição, outro trecho do roteiro); falhas de conferência ficam numa lista e o
    /// resumo sai no Console (OK ou FALHOU por cenário). O jogo roda de verdade no Play, como no teste do Remédio
    /// para Yusuf, mas aqui o motor e as ajudas são compartilhados.
    /// </summary>
    static class TestFlow
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // ---------- Esperas ----------

        public abstract class Wait
        {
            public abstract bool Done();
        }

        public sealed class Delay : Wait
        {
            readonly double until;
            public Delay(float seconds) => until = Now + seconds;
            public override bool Done() => Now >= until;
        }

        public sealed class Until : Wait
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

        public static double Now => EditorApplication.timeSinceStartup;

        // ---------- Motor ----------

        static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        static readonly List<string> failures = new List<string>();
        static readonly StringBuilder report = new StringBuilder();
        static List<(string name, Func<IEnumerator> body)> scenarios;
        static string title;
        static Wait waiting;
        static int index;
        static bool running;

        /// <summary>Avisos postados na tela desde o começo do teste.</summary>
        public static readonly List<string> Posted = new List<string>();

        public static void Check(bool ok, string what)
        {
            if (!ok) failures.Add(what);
        }

        public static void Start(string testTitle, List<(string name, Func<IEnumerator> body)> list)
        {
            title = testTitle;
            scenarios = list;
            report.Clear();
            Posted.Clear();
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

        static void OnPosted(string message) => Posted.Add(message);

        static void NextScenario()
        {
            index++;
            stack.Clear();
            waiting = null;
            failures.Clear();
            if (index >= scenarios.Count)
            {
                Stop();
                Debug.Log(title + ":\n" + report);
                return;
            }
            stack.Push(scenarios[index].body());
        }

        static void Stop()
        {
            running = false;
            EditorApplication.update -= Tick;
            Notifications.Posted -= OnPosted;
            Notifications.PostedImportant -= OnPosted;
            RemedyShortcuts.AutoChoose = true;
        }

        static void EndScenario(Exception error)
        {
            if (error != null) failures.Add($"erro {error.GetType().Name}: {error.Message}");
            string name = scenarios[index].name;
            report.AppendLine(failures.Count == 0 ? $"OK  {name}" : $"FALHOU  {name}\n      - " + string.Join("\n      - ", failures));
            NextScenario();
        }

        static void Tick()
        {
            if (!running) return;
            if (!EditorApplication.isPlaying)
            {
                Stop();
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

        // ---------- Ajudas de roteiro ----------

        /// <summary>Aplica o atalho e espera ele terminar de arrumar o mundo.</summary>
        public static IEnumerator Restart(string moment)
        {
            RemedyShortcuts.Ready = false;
            RemedyShortcuts.Launch(moment);
            yield return new Until(() => RemedyShortcuts.Ready, 60f, $"o atalho {moment} ficar pronto");
            yield return new Delay(1f);
        }

        public static IEnumerator Face(Vector3 position, float yaw)
        {
            Teleport(position, yaw);
            yield return new Delay(0.7f);
        }

        /// <summary>Aperta o botão de interagir no objeto em foco, conferindo antes o aviso na tela.</summary>
        public static IEnumerator Interact(string prompt)
        {
            PlayerInteractor interactor = Motor().GetComponent<PlayerInteractor>();
            IInteractable current = interactor.Current;
            bool ok = current != null && current.Prompt == prompt;
            Check(ok, $"aviso esperado \"{prompt}\", atual \"{current?.Prompt}\"");
            if (ok) current.Interact(interactor);
            yield return new Delay(0.3f);
        }

        /// <summary>Passa as falas da conversa aberta, conferindo qual é.</summary>
        public static IEnumerator FinishDialogue(string expected = null)
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

        /// <summary>Guarda um ponto de retorno aqui e recarrega a cena a partir dele (como morrer e voltar).</summary>
        public static IEnumerator SaveAndReload()
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

        public static PlayerMotor Motor() => Object.FindFirstObjectByType<PlayerMotor>();

        public static DialogueRunner Runner() => Object.FindFirstObjectByType<DialogueRunner>();

        public static PlayerInventory Bag() => Object.FindFirstObjectByType<PlayerInventory>();

        public static ReadingScreenUI Reading() => Object.FindFirstObjectByType<ReadingScreenUI>();

        public static void CloseReading()
        {
            ReadingScreenUI reading = Reading();
            if (reading != null && reading.IsOpen) typeof(ReadingScreenUI).GetMethod("Close", Any).Invoke(reading, null);
        }

        /// <summary>Texto que a tela de leitura mostra agora (campo: titleLabel, bodyLabel ou footerLabel).</summary>
        public static string ReadingText(string field) =>
            ((UnityEngine.UI.Text)typeof(ReadingScreenUI).GetField(field, Any).GetValue(Reading())).text;

        public static bool Raised(string eventName) => StoryEvents.HasBeenRaised(Load<StoryEvent>("Events/" + eventName));

        public static void RaiseEv(string eventName) => StoryEvents.Raise(Load<StoryEvent>("Events/" + eventName));

        public static int Inv(string item) => Bag().CountOf(Load<ItemDefinition>("Items/" + item));

        public static int Rep(string faction) => Reputation.Get(Load<FactionDefinition>("Factions/" + faction));

        public static int QuestStep(string quest) => WorldState.TryGetQuestStep(quest, out int step) ? step : -1;

        public static int MainStep() => QuestStep("Sobrevivente");

        public static string Prompt() => Motor().GetComponent<PlayerInteractor>().Current?.Prompt;

        public static string CurrentDialogue() =>
            (typeof(DialogueRunner).GetField("current", Any).GetValue(Runner()) as DialogueDefinition)?.name;

        /// <summary>Um objeto da cena pelo nome, mesmo desligado.</summary>
        public static GameObject Obj(string name) => Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(g => g.name == name && g.scene.IsValid() && g.scene.isLoaded && !EditorUtility.IsPersistent(g));
    }
}
