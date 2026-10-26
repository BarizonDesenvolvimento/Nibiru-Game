using System;
using System.Collections.Generic;
using Nibiru.Cameras;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: a base dos atalhos de teste de cada passo da missão (menus em
    /// Nibiru > Testes > Passo N). Um atalho abre a região, liga o Play (se ainda não estiver
    /// ligado) e recomeça a cena com o mundo como se a Elena tivesse jogado até ali, a partir de
    /// um checkpoint no lugar pedido. Depois de recarregar, pode fazer mais alguma coisa
    /// (entrar numa memória, levar a Elena até uma porta...).
    /// Cada passo tem a sua classe (Step8Shortcuts, Step9Shortcuts...), que registra os momentos aqui.
    /// </summary>
    [InitializeOnLoad]
    static class TestShortcuts
    {
        public const string RegionScenePath = "Assets/_Nibiru/Scenes/Regiao1_SitioDeUr.unity";
        public const string RegionSceneName = "Regiao1_SitioDeUr";
        const string PendingKey = "Nibiru.TestShortcuts.Pending";

        static readonly Dictionary<string, Action> moments = new Dictionary<string, Action>();
        static Func<bool> waitCondition;
        static Action waitThen;

        static TestShortcuts() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        public static void Register(string id, Action begin) => moments[id] = begin;

        /// <summary>Os ids de todos os atalhos registrados (para o teste geral dos atalhos).</summary>
        public static IReadOnlyList<string> MomentIds => new List<string>(moments.Keys);

        /// <summary>
        /// Roda o momento: na hora, se o Play já está ligado na cena pedida; senão, abre a cena
        /// (a região, se nenhuma for dada) e liga o Play.
        /// </summary>
        public static void Go(string id, string scenePath = RegionScenePath)
        {
            if (EditorApplication.isPlaying && SceneManager.GetActiveScene().path == scenePath)
            {
                Run(id);
                return;
            }
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning($"Este atalho é da cena {System.IO.Path.GetFileNameWithoutExtension(scenePath)}. Saia do Play e use o atalho de novo.");
                return;
            }

            if (SceneManager.GetActiveScene().path != scenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(scenePath);
            }
            SessionState.SetString(PendingKey, id);
            EditorApplication.isPlaying = true;
        }

        static void Run(string id)
        {
            if (moments.TryGetValue(id, out Action begin)) begin();
            else Debug.LogWarning($"Atalho de teste \"{id}\" não encontrado.");
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                StopWaiting();
                return;
            }
            if (change != PlayModeStateChange.EnteredPlayMode) return;

            string id = SessionState.GetString(PendingKey, "");
            SessionState.EraseString(PendingKey);
            if (!string.IsNullOrEmpty(id)) Run(id);
        }

        /// <summary>
        /// Recomeça a região com estes eventos já acontecidos, estes passos de missão e esta
        /// transformação, com a Elena (e o checkpoint) em <paramref name="spawn"/>. Antes de guardar,
        /// <paramref name="setup"/> pode mexer em mais coisas do mundo (reputação, por exemplo).
        /// Depois de recarregar, chama <paramref name="then"/> (opcional).
        /// </summary>
        public static void StartFrom(IEnumerable<string> events, (string quest, int step)[] quests, int transformation,
            Vector3 spawn, float yaw, Action then = null, Action setup = null)
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (motor == null)
            {
                Debug.LogWarning("Atalho de teste: a Elena não está na cena.");
                return;
            }

            WorldState.Clear();
            foreach (string id in events) WorldState.MarkStoryEvent(id);
            foreach (var (quest, step) in quests) WorldState.SetQuestStep(quest, step);
            WorldState.Transformation = transformation;
            setup?.Invoke();

            // Vida cheia no checkpoint do atalho, mesmo que ela estivesse morta ou ferida.
            var health = motor.GetComponent<Combat.Health>();
            health.Restore(health.Max);
            var point = new GameObject("Atalho_Retorno").transform;
            point.SetPositionAndRotation(spawn, Quaternion.Euler(0f, yaw, 0f));
            motor.GetComponent<PlayerRespawn>().SaveCheckpoint(point);
            Object.Destroy(point.gameObject);

            int oldPlayer = motor.GetInstanceID();
            StopWaiting();
            Checkpoints.Restart(RegionSceneName);

            // Alguns quadros depois de recarregar, para os Start e os eventos refeitos rodarem.
            int readyFrame = -1;
            WaitUntil(() =>
            {
                var current = Object.FindFirstObjectByType<PlayerMotor>();
                if (current == null || current.GetInstanceID() == oldPlayer) return false;
                if (readyFrame < 0) readyFrame = Time.frameCount + 5;
                return Time.frameCount >= readyFrame;
            }, () =>
            {
                ApplyInvincibility();
                then?.Invoke();
            });
        }

        // ---------- Elena invencível nos testes ----------

        const string InvincibleKey = "Nibiru.TestShortcuts.Invencivel";
        const string InvincibleMenu = "Nibiru/Testes/Elena invencível nos atalhos";

        /// <summary>Ligado (o padrão), os atalhos de teste deixam a Elena sem perder vida.</summary>
        public static bool Invincible
        {
            get => EditorPrefs.GetBool(InvincibleKey, true);
            set => EditorPrefs.SetBool(InvincibleKey, value);
        }

        [MenuItem(InvincibleMenu, priority = 0)]
        static void ToggleInvincible()
        {
            Invincible = !Invincible;
            if (EditorApplication.isPlaying) ApplyInvincibility();
            Debug.Log(Invincible ? "Atalhos de teste: Elena invencível." : "Atalhos de teste: Elena leva dano normalmente.");
        }

        [MenuItem(InvincibleMenu, true)]
        static bool ToggleInvincibleCheck()
        {
            Menu.SetChecked(InvincibleMenu, Invincible);
            return true;
        }

        static void ApplyInvincibility()
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            if (motor != null) motor.GetComponent<Combat.Health>().Invincible = Invincible;
        }

        /// <summary>Recomeça a região do zero, sem checkpoint (jogo novo no Acampamento).</summary>
        public static void FreshGame()
        {
            StopWaiting();
            WorldState.Clear();
            Checkpoints.Save(null);
            Checkpoints.Restart(RegionSceneName);
        }

        /// <summary>Espera a condição (a cada quadro do editor) e então faz a ação.</summary>
        public static void WaitUntil(Func<bool> condition, Action then)
        {
            StopWaiting();
            waitCondition = condition;
            waitThen = then;
            EditorApplication.update += Tick;
        }

        /// <summary>Espera alguns segundos do jogo e então faz a ação.</summary>
        public static void After(float seconds, Action then)
        {
            float at = Time.time + seconds;
            WaitUntil(() => Time.time >= at, then);
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                StopWaiting();
                return;
            }
            if (waitCondition == null || !waitCondition()) return;
            Action then = waitThen;
            StopWaiting();
            then();
        }

        static void StopWaiting()
        {
            EditorApplication.update -= Tick;
            waitCondition = null;
            waitThen = null;
        }

        // ---------- Ajudas para os atalhos ----------

        public static void Teleport(Vector3 position, float yaw)
        {
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            var controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            controller.enabled = true;
            Physics.SyncTransforms();
            var cameraController = Object.FindFirstObjectByType<PlayerCameraController>();
            if (cameraController != null) cameraController.AlignTo(motor.transform);
        }

        public static void Raise(string eventName) => StoryEvents.Raise(Load<StoryEvent>("Events/" + eventName));

        public static GameObject Find(string path) => GameObject.Find(path);

        public static T Load<T>(string relativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>($"Assets/_Nibiru/Data/{relativePath}.asset");
    }
}
