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

        public bool IsAiming => aiming;

        void Start()
        {
            yaw = (alignOnStart != null ? alignOnStart : transform).eulerAngles.y;
            pitch = settings.startPitch;
            SetMode(startingMode as ICameraMode);
            if (lockCursor) LockCursor();
        }

        public void SetMode(ICameraMode newMode)
        {
            mode = newMode;
            if (mode == null)
            {
                Debug.LogError("PlayerCameraController: o modo de câmera não implementa ICameraMode.", this);
                return;
            }
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
            if (lockCursor && Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor();
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
