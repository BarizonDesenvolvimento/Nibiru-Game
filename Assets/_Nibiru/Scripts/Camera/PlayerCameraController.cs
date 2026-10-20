using Nibiru.Player;
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

        [Tooltip("Prende e esconde o cursor do mouse durante o jogo. Clique na tela do jogo para prender de novo depois de apertar Esc.")]
        [SerializeField] bool lockCursor = true;

        ICameraMode mode;
        float yaw;
        float pitch;

        void Start()
        {
            yaw = transform.eulerAngles.y;
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
        }

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
                degrees = look * settings.mouseSensitivity;
            }
            else
            {
                degrees = Vector2.Scale(look, settings.gamepadSpeed) * Time.deltaTime;
            }

            if (settings.invertY) degrees.y = -degrees.y;
            return degrees;
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
