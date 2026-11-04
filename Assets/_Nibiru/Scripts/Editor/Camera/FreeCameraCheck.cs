using System;
using Nibiru.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using InputMouse = UnityEngine.InputSystem.Mouse;

namespace Nibiru.EditorTools.Cameras
{
    /// <summary>
    /// Câmera livre de conferência (só para teste; Docs/Plano-Camera-Livre-Conferencia.md). Liga e desliga em
    /// Nibiru > Testes > Câmera livre (conferência), só no Play: pausa o jogo (timeScale 0) e voa com uma câmera própria
    /// (WASD, E sobe, Q desce, Shift acelera, mouse gira). Não mexe no CameraRig, no PlayerMotor nem nas preferências:
    /// só desliga os controles do jogador enquanto está ligada e devolve tudo ao desligar. Fica na montagem do editor,
    /// então não existe em nenhuma build.
    /// </summary>
    [InitializeOnLoad]
    public static class FreeCameraCheck
    {
        const string MenuPath = "Nibiru/Testes/Câmera livre (conferência)";
        const float Speed = 5f;
        const float FastFactor = 4f;
        const float MouseDegreesPerPixel = 0.1f;

        static GameObject cameraObject;
        static float yaw, pitch;
        static double lastTime;
        static Vector2 pendingMouse;

        // O que estava antes de ligar.
        static float savedTimeScale;
        static CursorLockMode savedLock;
        static bool savedCursorVisible;
        static PlayerInputReader reader;
        static bool savedGameplayEnabled;

        /// <summary>A câmera livre está ligada.</summary>
        public static bool IsOn => cameraObject != null;

        /// <summary>A câmera da conferência (nula se desligada).</summary>
        public static Camera FreeCamera => cameraObject != null ? cameraObject.GetComponent<Camera>() : null;

        static FreeCameraCheck()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.ExitingPlayMode && IsOn) Disable();
            };
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            if (IsOn) Disable();
            else if (!Enable()) Debug.LogWarning("Câmera livre: ligue o Play e espere a Elena aparecer.");
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleCheck()
        {
            Menu.SetChecked(MenuPath, IsOn);
            return true;
        }

        /// <summary>Liga a câmera livre. Devolve false se não está no Play ou não há câmera do jogo. Ligar de novo não faz nada.</summary>
        public static bool Enable()
        {
            if (!EditorApplication.isPlaying) return false;
            if (IsOn) return true;
            Camera main = Camera.main;
            if (main == null) return false;

            savedTimeScale = Time.timeScale;
            savedLock = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            reader = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
            savedGameplayEnabled = reader != null && reader.GameplayEnabled;

            cameraObject = new GameObject("Teste_CameraLivre") { hideFlags = HideFlags.DontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(main);
            camera.depth = main.depth + 100f;
            var mainData = main.GetUniversalAdditionalCameraData();
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = mainData.renderPostProcessing;
            data.antialiasing = mainData.antialiasing;
            camera.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            Vector3 angles = main.transform.rotation.eulerAngles;
            yaw = angles.y;
            pitch = angles.x > 180f ? angles.x - 360f : angles.x;

            // Sem os controles do jogador, o mouse e as teclas não mexem na Elena nem no CameraRig.
            if (reader != null) reader.SetGameplayEnabled(false);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            lastTime = EditorApplication.timeSinceStartup;
            pendingMouse = Vector2.zero;
            InputSystem.onAfterUpdate -= OnInputUpdate;
            InputSystem.onAfterUpdate += OnInputUpdate;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return true;
        }

        /// <summary>Desliga a câmera livre e devolve tudo como estava. Desligar de novo não faz nada.</summary>
        public static void Disable()
        {
            EditorApplication.update -= Tick;
            InputSystem.onAfterUpdate -= OnInputUpdate;
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            cameraObject = null;
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) return;
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedLock;
            Cursor.visible = savedCursorVisible;
            if (reader != null) reader.SetGameplayEnabled(savedGameplayEnabled);
            reader = null;
        }

        /// <summary>Só para o teste automático: teclas tratadas como apertadas (o teclado virtual do Input System perde a tecla entre os quadros).</summary>
        internal static readonly System.Collections.Generic.HashSet<Key> SimulatedKeys = new System.Collections.Generic.HashSet<Key>();

        static bool Pressed(Key key)
        {
            if (SimulatedKeys.Contains(key)) return true;
            // Qualquer teclado conectado vale.
            foreach (InputDevice device in InputSystem.devices)
                if (device is Keyboard keyboard && keyboard[key].isPressed) return true;
            return false;
        }

        static void OnInputUpdate()
        {
            InputMouse mouse = InputMouse.current;
            if (mouse != null && cameraObject != null) pendingMouse += mouse.delta.ReadValue();
        }

        static void Tick()
        {
            if (cameraObject == null || !EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }

            // Se a cena recarregar com a câmera ligada (um atalho de teste, morrer e voltar), a Elena nova nasce com os controles ligados e
            // o tempo pode voltar a andar: a ferramenta segura os dois enquanto estiver ligada.
            if (reader == null) reader = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
            if (reader != null && reader.GameplayEnabled) reader.SetGameplayEnabled(false);
            if (Time.timeScale != 0f) Time.timeScale = 0f;
            if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

            double now = EditorApplication.timeSinceStartup;
            float dt = (float)Math.Min(now - lastTime, 0.1);
            lastTime = now;

            // O deslocamento do mouse é somado a cada atualização da entrada (ver OnInputUpdate) e gasto aqui.
            yaw += pendingMouse.x * MouseDegreesPerPixel;
            pitch = Mathf.Clamp(pitch - pendingMouse.y * MouseDegreesPerPixel, -89f, 89f);
            pendingMouse = Vector2.zero;

            Transform t = cameraObject.transform;
            t.rotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 move = Vector3.zero;
            if (Pressed(Key.W)) move += t.forward;
            if (Pressed(Key.S)) move -= t.forward;
            if (Pressed(Key.D)) move += t.right;
            if (Pressed(Key.A)) move -= t.right;
            if (Pressed(Key.E)) move += Vector3.up;
            if (Pressed(Key.Q)) move -= Vector3.up;
            bool fast = Pressed(Key.LeftShift) || Pressed(Key.RightShift);
            t.position += move * (Speed * (fast ? FastFactor : 1f) * dt);
        }
    }
}
