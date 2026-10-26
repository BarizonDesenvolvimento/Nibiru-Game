using System.Collections;
using System.Collections.Generic;
using Nibiru.Cameras;
using Nibiru.Player;
using Nibiru.Quests;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nibiru.Memories
{
    /// <summary>
    /// Leva a Elena para uma memória jogável e a traz de volta. A cena do passado é carregada
    /// por cima da região (que continua lá, parada), a Elena vira a pessoa da memória (sem
    /// armas, poderes, inventário nem barra de vida) e, quando o evento final acontece, volta
    /// para o mesmo lugar de onde saiu, com o progresso guardado.
    /// Chame Enter por um evento do Inspector (o fim de uma conversa, uma sequência...).
    /// </summary>
    public class PlayableMemory : MonoBehaviour
    {
        [SerializeField] MemoryDefinition memory;

        [Tooltip("Partes do HUD escondidas durante a memória (nomes dos objetos dentro do HUD).")]
        [SerializeField] string[] hiddenHud = { "BarraVida", "Municao", "MedidorRuido", "Agachada", "Mira", "BarraTransformacao", "Poder" };

        [Tooltip("O que acontece depois de voltar ao presente.")]
        [SerializeField] UnityEvent onReturned = new UnityEvent();

        const float TitleHold = 3.5f;
        const float TitleFadeTime = 1f;

        enum State { Present, Entering, InMemory, Leaving }

        State state;
        PlayerMotor player;
        PlayerInputReader input;
        PlayerCameraController cameraController;
        Vector3 returnPosition;
        Quaternion returnRotation;
        Scene memoryScene;
        GameObject avatar;
        readonly List<Light> sunsTurnedOff = new List<Light>();
        readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        readonly List<CanvasGroup> hiddenHudGroups = new List<CanvasGroup>();

        Image fade;
        Text placeLabel;
        Text characterLabel;

        /// <summary>True do começo da passagem para o passado até o fim da volta.</summary>
        public bool IsInMemory => state != State.Present;

        void OnEnable() => StoryEvents.Raised += OnStoryEvent;

        void OnDisable() => StoryEvents.Raised -= OnStoryEvent;

        public void Enter()
        {
            if (state != State.Present || memory == null) return;
            StartCoroutine(EnterRoutine());
        }

        void OnStoryEvent(StoryEvent raised)
        {
            if (state == State.InMemory && raised == memory.endOn) StartCoroutine(LeaveRoutine());
        }

        // A passagem usa tempo sem escala: conversas pausam o jogo, e a tela precisa continuar.
        IEnumerator EnterRoutine()
        {
            state = State.Entering;
            FindPlayer();
            CreateOverlay();
            input.SetGameplayEnabled(false);
            yield return Fade(0f, 1f);

            AsyncOperation load = SceneManager.LoadSceneAsync(memory.sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Memória {memory.name}: a cena \"{memory.sceneName}\" não está na lista de cenas do Build.", this);
                yield return Fade(1f, 0f);
                input.SetGameplayEnabled(true);
                state = State.Present;
                yield break;
            }
            while (!load.isDone) yield return null;

            // A cena da memória fica ativa para valer o céu, a luz ambiente e a névoa dela.
            memoryScene = SceneManager.GetSceneByName(memory.sceneName);
            SceneManager.SetActiveScene(memoryScene);
            SetRegionSuns(false);

            returnPosition = player.transform.position;
            returnRotation = player.transform.rotation;
            MemorySpawn spawn = FindInScene<MemorySpawn>(memoryScene);
            if (spawn != null) Teleport(spawn.transform.position, spawn.transform.rotation);
            WearAvatar(FindInScene<MemoryAvatar>(memoryScene));
            SetHudHidden(true);
            input.SetCombatEnabled(false);
            StoryEvents.Raise(memory.raiseOnEnter);
            state = State.InMemory;

            SetTitle(1f);
            yield return Fade(1f, 0f);
            input.SetGameplayEnabled(true);
            yield return new WaitForSecondsRealtime(TitleHold);
            for (float t = 0f; t < TitleFadeTime; t += Time.unscaledDeltaTime)
            {
                SetTitle(1f - t / TitleFadeTime);
                yield return null;
            }
            SetTitle(0f);
        }

        IEnumerator LeaveRoutine()
        {
            state = State.Leaving;
            yield return new WaitForSecondsRealtime(memory.endDelay);
            input.SetGameplayEnabled(false);
            yield return Fade(0f, 1f);
            SetTitle(0f);

            RemoveAvatar();
            SetHudHidden(false);
            input.SetCombatEnabled(true);
            Teleport(returnPosition, returnRotation);
            SetRegionSuns(true);
            SceneManager.SetActiveScene(gameObject.scene);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(memoryScene);
            while (unload != null && !unload.isDone) yield return null;

            yield return Fade(1f, 0f);
            input.SetGameplayEnabled(true);
            state = State.Present;
            onReturned.Invoke();
        }

        void FindPlayer()
        {
            player = FindFirstObjectByType<PlayerMotor>();
            input = player.GetComponent<PlayerInputReader>();
            cameraController = FindFirstObjectByType<PlayerCameraController>();
        }

        void Teleport(Vector3 position, Quaternion rotation)
        {
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            if (cameraController != null) cameraController.AlignTo(player.transform);
        }

        // Esconde o corpo da Elena e veste a aparência da memória, com a origem nos pés dela.
        void WearAvatar(MemoryAvatar source)
        {
            hiddenRenderers.Clear();
            foreach (Renderer r in player.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                r.enabled = false;
                hiddenRenderers.Add(r);
            }
            if (source == null) return;

            var controller = player.GetComponent<CharacterController>();
            float feet = controller != null ? controller.center.y - controller.height * 0.5f - controller.skinWidth : 0f;
            avatar = source.gameObject;
            avatar.transform.SetParent(player.transform, false);
            avatar.transform.localPosition = new Vector3(0f, feet, 0f);
            avatar.transform.localRotation = Quaternion.identity;
        }

        // A aparência passou para a cena da região junto com o jogador: precisa ser destruída à mão.
        void RemoveAvatar()
        {
            if (avatar != null) Destroy(avatar);
            avatar = null;
            foreach (Renderer r in hiddenRenderers)
                if (r != null) r.enabled = true;
            hiddenRenderers.Clear();
        }

        // Some com a transparência (e não desligando), porque cada parte do HUD liga e desliga a si mesma.
        void SetHudHidden(bool hidden)
        {
            if (!hidden)
            {
                foreach (CanvasGroup group in hiddenHudGroups)
                    if (group != null) group.alpha = 1f;
                hiddenHudGroups.Clear();
                return;
            }

            var hud = FindFirstObjectByType<UI.ObjectiveHUD>();
            if (hud == null) return;
            foreach (string partName in hiddenHud)
            {
                Transform part = hud.transform.Find(partName);
                if (part == null) continue;
                var group = part.GetComponent<CanvasGroup>();
                if (group == null) group = part.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                hiddenHudGroups.Add(group);
            }
        }

        // O sol da região iluminaria a memória também: fica apagado enquanto ela dura.
        void SetRegionSuns(bool on)
        {
            if (on)
            {
                foreach (Light sun in sunsTurnedOff)
                    if (sun != null) sun.enabled = true;
                sunsTurnedOff.Clear();
                return;
            }

            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || !light.enabled || light.gameObject.scene == memoryScene) continue;
                light.enabled = false;
                sunsTurnedOff.Add(light);
            }
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        // ---------- Tela da passagem ----------

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < memory.fadeTime; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / memory.fadeTime));
                yield return null;
            }
            SetFade(to);
        }

        void SetFade(float alpha)
        {
            Color color = memory.fadeColor;
            color.a = alpha;
            fade.color = color;
            fade.enabled = alpha > 0f;
        }

        void SetTitle(float alpha)
        {
            placeLabel.text = memory.LocalizedPlaceTitle;
            characterLabel.text = string.IsNullOrEmpty(memory.LocalizedCharacterName) ? "" : $"Você é {memory.LocalizedCharacterName}";
            placeLabel.color = new Color(1f, 0.93f, 0.8f, alpha);
            characterLabel.color = new Color(1f, 0.93f, 0.8f, alpha * 0.85f);
        }

        void CreateOverlay()
        {
            if (fade != null) return;

            var go = new GameObject("TelaMemoria");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var fadeGo = new GameObject("Passagem", typeof(RectTransform));
            fadeGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)fadeGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            fade = fadeGo.AddComponent<Image>();
            fade.raycastTarget = false;

            placeLabel = Label(go.transform, "Lugar", 56, new Vector2(0f, 120f));
            characterLabel = Label(go.transform, "Personagem", 34, new Vector2(0f, 60f));
            SetFade(0f);
            SetTitle(0f);
        }

        static Text Label(Transform parent, string name, int size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1600f, 80f);
            rect.anchoredPosition = position;
            var text = go.AddComponent<Text>();
            text.font = Nibiru.UI.UiFont.Current;
            text.fontSize = size;
            text.fontStyle = FontStyle.Italic;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
