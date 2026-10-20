using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.Player
{
    /// <summary>
    /// Lê o mapa de controles "Jogador" e expõe os valores para os outros sistemas.
    /// Movimento, câmera e combate leem daqui, nunca direto do teclado ou do controle.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Arquivo de controles (.inputactions) com o mapa \"Jogador\".")]
        [SerializeField] InputActionAsset actions;

        InputActionMap map;
        InputAction move;
        InputAction look;
        InputAction run;
        InputAction dodge;
        InputAction interact;

        const string KeyboardMouseGroup = "TecladoMouse";
        const string GamepadGroup = "Controle";

        // No controle, segurar o analógico apertado cansa: um clique liga a corrida
        // e ela continua até o direcional voltar ao centro.
        const float RunReleaseThreshold = 0.1f;
        bool runLatched;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        // Mouse dá deslocamento em pixels por quadro; analógico dá inclinação de -1 a 1.
        public bool LookFromMouse => look.activeControl?.device is Pointer;
        public bool RunActive => run.IsPressed() || runLatched;
        public bool DodgePressed => dodge.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();

        /// <summary>True se o último comando veio do controle; false se veio do teclado ou mouse.</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>Nome do botão de interagir no aparelho em uso, por exemplo "E" ou "A".</summary>
        public string InteractButtonLabel =>
            interact.GetBindingDisplayString(InputBinding.MaskByGroup(UsingGamepad ? GamepadGroup : KeyboardMouseGroup));

        void Awake()
        {
            map = actions.FindActionMap("Jogador", throwIfNotFound: true);
            move = map.FindAction("Mover", throwIfNotFound: true);
            look = map.FindAction("Olhar", throwIfNotFound: true);
            run = map.FindAction("Correr", throwIfNotFound: true);
            dodge = map.FindAction("Esquivar", throwIfNotFound: true);
            interact = map.FindAction("Interagir", throwIfNotFound: true);
        }

        void OnEnable()
        {
            map.actionTriggered += OnActionTriggered;
            map.Enable();
        }

        void OnDisable()
        {
            map.actionTriggered -= OnActionTriggered;
            map.Disable();
            runLatched = false;
        }

        void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (context.control != null)
                UsingGamepad = context.control.device is Gamepad;
        }

        void Update()
        {
            if (run.WasPressedThisFrame() && run.activeControl?.device is Gamepad)
                runLatched = true;
            if (Move.magnitude < RunReleaseThreshold)
                runLatched = false;
        }
    }
}
