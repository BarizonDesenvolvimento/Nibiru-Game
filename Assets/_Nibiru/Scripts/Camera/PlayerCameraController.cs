using System.Collections.Generic;
using Nibiru.Player;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.Cameras
{
    /// <summary>
    /// Fica na câmera. Lê o "Olhar" (mouse ou analógico direito), guarda a rotação
    /// e entrega para o modo de câmera ativo posicionar a câmera.
    /// </summary>
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField] CameraSettings settings;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Modo usado ao começar. Precisa ser um componente que implementa ICameraMode, como o ThirdPersonCameraMode.")]
        [SerializeField] MonoBehaviour startingMode;

        [Tooltip("Ao começar, a câmera olha para onde este objeto olha (normalmente o Jogador). Vazio = mantém a rotação da câmera na cena.")]
        [SerializeField] Transform alignOnStart;

        [Tooltip("Prende e esconde o cursor do mouse durante o jogo. Clique na tela do jogo para prender de novo depois de apertar Esc.")]
        [SerializeField] bool lockCursor = true;

        ICameraMode mode;
        float yaw;
        float pitch;
        bool aiming;

        // Zoom: a distância que o jogador escolheu (alvo), a suavizada que vai para o modo e a pendência de gravar no disco.
        const float SaveDelay = 1f;
        float zoomTarget;
        float zoomCurrent;
        float zoomVelocity;
        float saveAt = -1f;
        readonly HashSet<object> zoomBlockers = new HashSet<object>();

        /// <summary>
        /// Quanto vale um clique da roda no Input System. Por padrão (o projeto não muda isso) o valor é normalizado: 1 por clique
        /// em qualquer sistema; com "manter a faixa da plataforma" é 120 no Windows.
        /// </summary>
        public static float WheelUnitsPerNotch =>
            InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange ? 120f : 1f;

        public bool IsAiming => aiming;

        public CameraSettings Settings => settings;

        /// <summary>Distância escolhida pelo jogador (alvo do zoom), em metros.</summary>
        public float ZoomTarget => zoomTarget;

        /// <summary>Distância que o modo recebe agora (o alvo, já suavizado), em metros.</summary>
        public float ZoomCurrent => zoomCurrent;

        /// <summary>
        /// True se a roda faz zoom agora: controles da Elena ligados, sem menu aberto, cursor preso, sem mirar e sem
        /// ninguém bloqueando (memórias jogáveis). Na viagem de caminhonete os controles ficam desligados, então não vale.
        /// </summary>
        public bool ZoomAllowed =>
            input != null && input.GameplayEnabled && !aiming && zoomBlockers.Count == 0 && !CursorPolicy.AnyFree
            && (!lockCursor || Cursor.lockState == CursorLockMode.Locked);

        /// <summary>Uma cena com enquadramento próprio (memória jogável) liga e desliga o bloqueio do zoom.</summary>
        public void SetZoomBlocked(object owner, bool blocked)
        {
            if (blocked) zoomBlockers.Add(owner);
            else zoomBlockers.Remove(owner);
        }

        /// <summary>
        /// Escolhe a distância (Opções, roda): limita à faixa, guarda nas preferências e grava no disco 1 s depois do último
        /// ajuste. O modo recebe o valor suavizado nos quadros seguintes.
        /// </summary>
        public void SetZoomTarget(float meters)
        {
            meters = ClampZoom(meters);
            if (Mathf.Approximately(meters, zoomTarget)) return;
            zoomTarget = meters;
            GameOptions.CameraDistance = meters;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        /// <summary>Grava já a distância pendente (ao pausar, ao fechar).</summary>
        public void FlushZoom()
        {
            if (saveAt < 0f) return;
            saveAt = -1f;
            GameOptions.Save();
        }

        float ClampZoom(float meters)
        {
            float min = Mathf.Min(settings.zoomMinDistance, settings.zoomMaxDistance);
            float max = Mathf.Max(settings.zoomMinDistance, settings.zoomMaxDistance);
            return Mathf.Clamp(meters, min, max);
        }

        void Start()
        {
            yaw = (alignOnStart != null ? alignOnStart : transform).eulerAngles.y;
            pitch = settings.startPitch;
            // A preferência guardada, limitada à faixa; sem nenhuma, o padrão do asset.
            zoomTarget = zoomCurrent = ClampZoom(GameOptions.CameraDistanceClamped(
                Mathf.Min(settings.zoomMinDistance, settings.zoomMaxDistance), Mathf.Max(settings.zoomMinDistance, settings.zoomMaxDistance), settings.distance));
            SetMode(startingMode as ICameraMode);
            if (lockCursor)
            {
                CursorPolicy.SetGameActive(true);
                LockCursor();
            }
        }

        void OnDestroy()
        {
            FlushZoom();
            if (lockCursor) CursorPolicy.SetGameActive(false);
        }

        public void SetMode(ICameraMode newMode)
        {
            mode = newMode;
            if (mode == null)
            {
                Debug.LogError("PlayerCameraController: o modo de câmera não implementa ICameraMode.", this);
                return;
            }
            mode.SetZoom(zoomCurrent);
            mode.Enter();
            mode.SetAiming(aiming);
        }

        /// <summary>Liga ou desliga a mira: aproxima a câmera e deixa a sensibilidade mais fina.</summary>
        public void SetAiming(bool value)
        {
            aiming = value;
            mode?.SetAiming(value);
        }

        /// <summary>Vira a câmera para onde o objeto olha, na altura inicial (depois de um teletransporte).</summary>
        public void AlignTo(Transform target)
        {
            yaw = target.eulerAngles.y;
            pitch = settings.startPitch;
        }

        /// <summary>Coice da arma: sobe a mira alguns graus.</summary>
        public void AddRecoil(float degrees) => pitch -= degrees;

        void Update()
        {
            // Com um menu aberto (pausa, leitura) o cursor é do menu: o clique não prende o cursor.
            if (lockCursor && !CursorPolicy.AnyFree && Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                LockCursor();

            if (saveAt >= 0f && Time.unscaledTime >= saveAt) FlushZoom();
        }

        // Roda do mouse: lida direto do mouse. Rolar para a frente aproxima. O D-pad do controle (ação ZoomCamera) faz o mesmo.
        void ReadZoomWheel()
        {
            // D-pad do controle (ME1): cima aproxima, baixo afasta, no passo de um clique da roda. Lido todo quadro, para o
            // "segurar repete" não guardar um toque de quando o zoom estava bloqueado.
            int padStep = input.ZoomPadStep;
            if (!ZoomAllowed) return;
            if (padStep != 0) SetZoomTarget(zoomTarget - padStep * settings.zoomMetersPerNotch);
            if (Mouse.current == null) return;
            float notches = Mouse.current.scroll.ReadValue().y / WheelUnitsPerNotch;
            if (notches == 0f) return;
            float direction = settings.zoomInvertWheel ? 1f : -1f;
            SetZoomTarget(zoomTarget + direction * notches * settings.zoomMetersPerNotch);
        }

        // LateUpdate roda depois do movimento do personagem, então a câmera não treme.
        void LateUpdate()
        {
            if (mode == null) return;

            Vector2 delta = ReadLookDelta();
            yaw += delta.x;
            pitch -= delta.y;
            Vector2 limits = mode.PitchLimits;
            pitch = Mathf.Clamp(pitch, limits.x, limits.y);

            // Zoom: lê a roda, limita à faixa (o Inspector pode mudar no Play), suaviza e entrega ao modo.
            ReadZoomWheel();
            float clamped = ClampZoom(zoomTarget);
            if (!Mathf.Approximately(clamped, zoomTarget)) SetZoomTarget(clamped);
            zoomCurrent = Mathf.SmoothDamp(zoomCurrent, zoomTarget, ref zoomVelocity, settings.zoomSmoothTime, settings.zoomMaxSpeed, Time.deltaTime);
            mode.SetZoom(zoomCurrent);

            mode.UpdateCamera(transform, yaw, pitch, Time.deltaTime);
        }

        // Converte o "Olhar" em graus para este quadro.
        Vector2 ReadLookDelta()
        {
            Vector2 look = input.Look;
            Vector2 degrees;
            if (input.LookFromMouse)
            {
                // Com o cursor solto (depois do Esc), mexer o mouse não gira a câmera.
                if (lockCursor && Cursor.lockState != CursorLockMode.Locked) return Vector2.zero;
                degrees = look * settings.mouseSensitivity * GameOptions.MouseSensitivity;
            }
            else
            {
                degrees = Vector2.Scale(look, settings.gamepadSpeed) * Time.unscaledDeltaTime * GameOptions.GamepadSensitivity;
            }

            // A opção do jogador inverte em cima do que o projeto definiu.
            if (settings.invertY != GameOptions.InvertY) degrees.y = -degrees.y;
            if (aiming) degrees *= settings.aimSensitivityMultiplier;
            return degrees;
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
