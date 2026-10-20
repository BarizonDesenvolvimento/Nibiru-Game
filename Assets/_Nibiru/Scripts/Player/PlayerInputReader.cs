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
        [Tooltip("Arquivo de controles (.inputactions) com os mapas \"Jogador\" e \"Interface\".")]
        [SerializeField] InputActionAsset actions;

        // "Jogador" controla a Elena e desliga quando um menu abre; "Interface" fica sempre ligado.
        InputActionMap map;
        InputActionMap uiMap;
        InputAction move;
        InputAction look;
        InputAction run;
        InputAction dodge;
        InputAction interact;
        InputAction toggleInventory;
        bool gameplayEnabled = true;

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
        public bool InventoryTogglePressed => toggleInventory.WasPressedThisFrame();

        /// <summary>True se o último comando veio do controle; false se veio do teclado ou mouse.</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>Nome do botão de interagir no aparelho em uso, por exemplo "E" ou "A".</summary>
        public string InteractButtonLabel => ButtonLabel(interact);

        /// <summary>Nome do botão do inventário no aparelho em uso, por exemplo "Tab | I" ou "Select".</summary>
        public string InventoryButtonLabel => ButtonLabel(toggleInventory);

        string ButtonLabel(InputAction action) =>
            action.GetBindingDisplayString(InputBinding.MaskByGroup(UsingGamepad ? GamepadGroup : KeyboardMouseGroup));

        void Awake()
        {
            map = actions.FindActionMap("Jogador", throwIfNotFound: true);
            move = map.FindAction("Mover", throwIfNotFound: true);
            look = map.FindAction("Olhar", throwIfNotFound: true);
            run = map.FindAction("Correr", throwIfNotFound: true);
            dodge = map.FindAction("Esquivar", throwIfNotFound: true);
            interact = map.FindAction("Interagir", throwIfNotFound: true);

            uiMap = actions.FindActionMap("Interface", throwIfNotFound: true);
            toggleInventory = uiMap.FindAction("Inventario", throwIfNotFound: true);
        }

        void OnEnable()
        {
            map.actionTriggered += OnActionTriggered;
            uiMap.actionTriggered += OnActionTriggered;
            if (gameplayEnabled) map.Enable();
            uiMap.Enable();
        }

        void OnDisable()
        {
            map.actionTriggered -= OnActionTriggered;
            uiMap.actionTriggered -= OnActionTriggered;
            map.Disable();
            uiMap.Disable();
            runLatched = false;
        }

        /// <summary>False enquanto um menu está aberto e os controles da Elena estão desligados.</summary>
        public bool GameplayEnabled => gameplayEnabled;

        /// <summary>Liga ou desliga os controles da Elena (andar, olhar, interagir...). Menus usam isso.</summary>
        public void SetGameplayEnabled(bool enabled)
        {
            gameplayEnabled = enabled;
            runLatched = false;
            if (!isActiveAndEnabled) return;
            if (enabled) map.Enable();
            else map.Disable();
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
