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
        InputAction attack;
        InputAction aim;
        InputAction fire;
        InputAction reload;
        InputAction crouch;
        InputAction power;
        InputAction toggleInventory;
        InputAction navigate;
        InputAction confirm;
        InputAction back;
        InputAction pause;
        bool gameplayEnabled = true;
        bool combatEnabled = true;

        /// <summary>
        /// Navegação de menu num eixo: um passo ao inclinar, e repete se continuar segurando.
        /// Pode ser lido várias vezes no mesmo quadro sem andar duas vezes.
        /// Usa tempo sem escala porque os menus funcionam com o jogo pausado.
        /// </summary>
        class NavRepeater
        {
            const float Threshold = 0.5f;
            const float FirstRepeatDelay = 0.35f;
            const float RepeatInterval = 0.12f;

            int heldDirection;
            float nextRepeatTime;
            int frame = -1;
            int step;

            public int Step(float axis)
            {
                if (frame == Time.frameCount) return step;
                frame = Time.frameCount;
                step = Compute(axis);
                return step;
            }

            int Compute(float axis)
            {
                int direction = axis > Threshold ? 1 : axis < -Threshold ? -1 : 0;
                float now = Time.unscaledTime;
                if (direction != heldDirection)
                {
                    heldDirection = direction;
                    nextRepeatTime = now + FirstRepeatDelay;
                    return direction;
                }
                if (direction != 0 && now >= nextRepeatTime)
                {
                    nextRepeatTime = now + RepeatInterval;
                    return direction;
                }
                return 0;
            }
        }

        readonly NavRepeater verticalNav = new NavRepeater();
        readonly NavRepeater horizontalNav = new NavRepeater();

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
        public bool DodgePressed => combatEnabled && dodge.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool AttackPressed => combatEnabled && attack.WasPressedThisFrame();
        public bool AimHeld => combatEnabled && aim.IsPressed();
        public bool FirePressed => combatEnabled && fire.WasPressedThisFrame();
        public bool ReloadPressed => combatEnabled && reload.WasPressedThisFrame();
        public bool CrouchPressed => crouch.WasPressedThisFrame();
        public bool PowerPressed => combatEnabled && power.WasPressedThisFrame();
        public bool InventoryTogglePressed => combatEnabled && toggleInventory.WasPressedThisFrame();
        public bool ConfirmPressed => confirm.WasPressedThisFrame();
        public bool BackPressed => back.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();

        /// <summary>Passo vertical do menu neste quadro: +1 desce, -1 sobe, 0 parado.</summary>
        public int VerticalNavStep => -verticalNav.Step(navigate.ReadValue<Vector2>().y);

        /// <summary>Passo horizontal do menu neste quadro: +1 direita, -1 esquerda, 0 parado.</summary>
        public int HorizontalNavStep => horizontalNav.Step(navigate.ReadValue<Vector2>().x);

        /// <summary>
        /// Quadro em que os controles da Elena foram ligados ou desligados pela última vez.
        /// Menus usam isto para o mesmo botão não fechar um menu e abrir outro de uma vez.
        /// </summary>
        public int GameplayToggledFrame { get; private set; } = -10;

        /// <summary>True se o último comando veio do controle; false se veio do teclado ou mouse.</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>Nome do botão de interagir no aparelho em uso, por exemplo "E" ou "A".</summary>
        public string InteractButtonLabel => ButtonLabel(interact);

        /// <summary>Nome do botão do inventário no aparelho em uso, por exemplo "Tab | I" ou "Select".</summary>
        public string InventoryButtonLabel => ButtonLabel(toggleInventory);
        public string AttackButtonLabel => ButtonLabel(attack);
        public string ConfirmButtonLabel => ButtonLabel(confirm);
        public string PowerButtonLabel => ButtonLabel(power);
        public string BackButtonLabel => ButtonLabel(back);

        string ButtonLabel(InputAction action) =>
            action.GetBindingDisplayString(InputBinding.MaskByGroup(UsingGamepad ? GamepadGroup : KeyboardMouseGroup));

        void Awake()
        {
            // Usa uma cópia própria do arquivo de controles. Assim a cena antiga e a nova
            // (ao recomeçar de um checkpoint) não disputam o mesmo estado interno.
            actions = Instantiate(actions);
            map = actions.FindActionMap("Jogador", throwIfNotFound: true);
            move = map.FindAction("Mover", throwIfNotFound: true);
            look = map.FindAction("Olhar", throwIfNotFound: true);
            run = map.FindAction("Correr", throwIfNotFound: true);
            dodge = map.FindAction("Esquivar", throwIfNotFound: true);
            interact = map.FindAction("Interagir", throwIfNotFound: true);
            attack = map.FindAction("Atacar", throwIfNotFound: true);
            aim = map.FindAction("Mirar", throwIfNotFound: true);
            fire = map.FindAction("Atirar", throwIfNotFound: true);
            reload = map.FindAction("Recarregar", throwIfNotFound: true);
            crouch = map.FindAction("Agachar", throwIfNotFound: true);
            power = map.FindAction("Poder", throwIfNotFound: true);

            uiMap = actions.FindActionMap("Interface", throwIfNotFound: true);
            toggleInventory = uiMap.FindAction("Inventario", throwIfNotFound: true);
            navigate = uiMap.FindAction("Navegar", throwIfNotFound: true);
            confirm = uiMap.FindAction("Confirmar", throwIfNotFound: true);
            back = uiMap.FindAction("Voltar", throwIfNotFound: true);
            pause = uiMap.FindAction("Pausar", throwIfNotFound: true);
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

        void OnDestroy()
        {
            if (actions != null) Destroy(actions);
        }

        /// <summary>False enquanto um menu está aberto e os controles da Elena estão desligados.</summary>
        public bool GameplayEnabled => gameplayEnabled;

        /// <summary>Liga ou desliga os controles da Elena (andar, olhar, interagir...). Menus usam isso.</summary>
        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled != gameplayEnabled) GameplayToggledFrame = Time.frameCount;
            gameplayEnabled = enabled;
            runLatched = false;
            if (!isActiveAndEnabled) return;
            if (enabled) map.Enable();
            else
            {
                map.Disable();
                if (lookWhileLocked) look.Enable();
            }
        }

        bool lookWhileLocked;

        /// <summary>
        /// Deixa a câmera girar mesmo com os controles da Elena desligados (nas conversas da viagem de
        /// caminhonete, a Elena só olha em volta). O resto continua desligado.
        /// </summary>
        public void SetLookWhileLocked(bool value)
        {
            if (lookWhileLocked == value) return;
            lookWhileLocked = value;
            if (!isActiveAndEnabled || gameplayEnabled) return;
            if (value) look.Enable();
            else look.Disable();
        }

        /// <summary>
        /// False nas memórias jogáveis: em outro corpo, a Elena não tem esquiva, faca, armas,
        /// poderes nem inventário. Andar, correr, agachar e interagir continuam.
        /// </summary>
        public bool CombatEnabled => combatEnabled;

        public void SetCombatEnabled(bool enabled) => combatEnabled = enabled;

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
