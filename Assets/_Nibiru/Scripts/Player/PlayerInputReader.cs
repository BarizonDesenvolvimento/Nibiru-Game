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
        /// <summary>Como o Eco sai no controle (configuração no código, não no menu; veja Docs/Plano-ME1-Dados-Posse-Controles.md).</summary>
        public enum EcoScheme
        {
            /// <summary>Plano A (padrão): LB tocado e solto, se nada mais foi apertado com ele seguro e o toque durou até o limite.</summary>
            SoltarLB,
            /// <summary>Plano B: Y sozinho dispara o Eco na hora; o LB só serve de modificador (sozinho não faz nada).</summary>
            YSozinho,
        }

        [Tooltip("Arquivo de controles (.inputactions) com os mapas \"Jogador\" e \"Interface\".")]
        [SerializeField] InputActionAsset actions;

        [Tooltip("Como o Eco sai no controle. Plano A: ao soltar o LB. Plano B: no Y sozinho (o LB fica só como modificador).")]
        [SerializeField] EcoScheme ecoScheme = EcoScheme.SoltarLB;

        [Tooltip("Plano A: segurar o LB mais que isto (segundos) e soltar sem combinação não dispara o Eco (desistir).")]
        [SerializeField, Min(0.05f)] float ecoTapMaxHold = 0.5f;

        /// <summary>Só para os testes e os atalhos do LB: troca o esquema do Eco neste Play (null = o do prefab). Some ao sair do Play.</summary>
        public static EcoScheme? SchemeOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => SchemeOverride = null;

        /// <summary>O esquema do Eco em uso agora.</summary>
        public EcoScheme CurrentEcoScheme => SchemeOverride ?? ecoScheme;

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
        InputAction power1;
        InputAction power2;
        InputAction power3;
        InputAction powerModifier;
        InputAction power3Combo;
        InputAction zoomPad;
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

            readonly float firstRepeatDelay;
            readonly float repeatInterval;

            public NavRepeater(float firstRepeatDelay = FirstRepeatDelay, float repeatInterval = RepeatInterval)
            {
                this.firstRepeatDelay = firstRepeatDelay;
                this.repeatInterval = repeatInterval;
            }

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
                    nextRepeatTime = now + firstRepeatDelay;
                    return direction;
                }
                if (direction != 0 && now >= nextRepeatTime)
                {
                    nextRepeatTime = now + repeatInterval;
                    return direction;
                }
                return 0;
            }
        }

        readonly NavRepeater verticalNav = new NavRepeater();
        readonly NavRepeater horizontalNav = new NavRepeater();
        // Zoom no D-pad: um passo por toque; segurando, repete a cada 0,15 s.
        readonly NavRepeater zoomRepeater = new NavRepeater(0.15f, 0.15f);

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
        public bool AttackPressed => combatEnabled && attack.WasPressedThisFrame() && !AttackSwallowedByModifier;
        public bool AimHeld => combatEnabled && aim.IsPressed();
        public bool FirePressed => combatEnabled && fire.WasPressedThisFrame();
        public bool ReloadPressed => combatEnabled && reload.WasPressedThisFrame();
        public bool CrouchPressed => crouch.WasPressedThisFrame();

        /// <summary>
        /// A vaga de poder foi pedida neste quadro: teclado e mouse (Q; Z ou Mouse 4; X ou Mouse 5) na hora; no controle,
        /// pela regra do LB (veja EvaluatePowerButtons). Pode ser lido várias vezes no mesmo quadro.
        /// </summary>
        public bool PowerSlotPressed(Nibiru.Powers.PowerSlot slot)
        {
            if (!combatEnabled || !gameplayEnabled) return false;
            EvaluatePowerButtons();
            switch (slot)
            {
                case Nibiru.Powers.PowerSlot.Poder1: return power1.WasPressedThisFrame() || padSlot1;
                case Nibiru.Powers.PowerSlot.Poder2: return power2.WasPressedThisFrame() || padSlot2;
                case Nibiru.Powers.PowerSlot.Poder3: return power3.WasPressedThisFrame() || padSlot3;
                default: return false;
            }
        }

        /// <summary>Há quanto tempo o LB está seguro (0 se solto). Para a dica "RB Sopro · Y Égide" do ME5.</summary>
        public float PowerModifierHeldSeconds
        {
            get
            {
                EvaluatePowerButtons();
                return modifierHeld ? Time.unscaledTime - modifierDownAt : 0f;
            }
        }

        /// <summary>Segundos entre apertar o botão do controle e o poder sair, no último poder pedido pelo controle (o atraso do Eco ao soltar).</summary>
        public float LastPadPowerLatency { get; private set; }

        /// <summary>Passo de zoom do D-pad neste quadro: +1 aproxima (cima), -1 afasta (baixo), 0 nada.</summary>
        public int ZoomPadStep => gameplayEnabled ? zoomRepeater.Step(zoomPad.ReadValue<float>()) : 0;
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
        /// <summary>O nome do botão da vaga de poder no aparelho em uso ("Q", "LB", "LB + RB", "Y"...).</summary>
        public string PowerSlotLabel(Nibiru.Powers.PowerSlot slot)
        {
            if (!UsingGamepad)
            {
                InputAction action = slot == Nibiru.Powers.PowerSlot.Poder1 ? power1 : slot == Nibiru.Powers.PowerSlot.Poder2 ? power2 : power3;
                return FirstLabel(action, KeyboardMouseGroup);
            }
            string modifier = FirstLabel(powerModifier, GamepadGroup);
            switch (slot)
            {
                case Nibiru.Powers.PowerSlot.Poder1: return CurrentEcoScheme == EcoScheme.SoltarLB ? modifier : FirstLabel(power3Combo, GamepadGroup);
                case Nibiru.Powers.PowerSlot.Poder2: return modifier + " + " + FirstLabel(attack, GamepadGroup);
                default: return modifier + " + " + FirstLabel(power3Combo, GamepadGroup);
            }
        }

        // O nome do primeiro botão da ação no grupo (Z, e não "Z | Mouse 4").
        static string FirstLabel(InputAction action, string group)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (binding.groups != null && binding.groups.Contains(group)) return action.GetBindingDisplayString(i);
            }
            return "";
        }
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
            power1 = map.FindAction("Poder1", throwIfNotFound: true);
            power2 = map.FindAction("Poder2", throwIfNotFound: true);
            power3 = map.FindAction("Poder3", throwIfNotFound: true);
            powerModifier = map.FindAction("ModificadorPoderes", throwIfNotFound: true);
            power3Combo = map.FindAction("Poder3Combo", throwIfNotFound: true);
            zoomPad = map.FindAction("ZoomCamera", throwIfNotFound: true);

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
            modifierHeld = false; // um menu abriu ou fechou: o LB que estava seguro não vale mais
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

        // ---------- Regra do LB (controle) ----------

        bool modifierHeld;
        bool modifierCancelled;
        float modifierDownAt;
        int powerEvalFrame = -1;
        bool padSlot1, padSlot2, padSlot3;
        bool attackSwallowed;

        bool AttackSwallowedByModifier
        {
            get
            {
                EvaluatePowerButtons();
                return attackSwallowed;
            }
        }

        /// <summary>
        /// Resolve, uma vez por quadro, o que o controle pediu (a regra aprovada no ME1):
        /// - Plano A: LB tocado e solto dispara o Eco (vaga 1) ao soltar, se nenhum outro botão foi apertado com ele seguro e
        ///   se ele ficou seguro até ecoTapMaxHold (0,5 s); segurar mais e soltar é desistir.
        /// - Com o LB seguro: RB = vaga 2 e Y = vaga 3, na hora; o RB nunca vira faca. Outro botão faz a ação dele e cancela o Eco.
        /// - Plano B: Y sozinho é o Eco, na hora; o LB sozinho não faz nada; as combinações são as mesmas.
        /// Usa tempo sem escala (é tempo de botão, não de jogo).
        /// </summary>
        void EvaluatePowerButtons()
        {
            if (powerEvalFrame == Time.frameCount) return;
            powerEvalFrame = Time.frameCount;
            padSlot1 = padSlot2 = padSlot3 = false;
            attackSwallowed = false;
            if (!gameplayEnabled || powerModifier == null) return;

            float now = Time.unscaledTime;
            bool attackFromPad = attack.activeControl?.device is Gamepad;
            if (powerModifier.WasPressedThisFrame())
            {
                modifierHeld = true;
                modifierCancelled = false;
                modifierDownAt = now;
            }

            if (modifierHeld)
            {
                bool rb = attackFromPad && attack.WasPressedThisFrame();
                bool y = power3Combo.WasPressedThisFrame();
                if (rb) padSlot2 = true;
                if (y) padSlot3 = true;
                if (rb || y || OtherPadButtonPressed(powerModifier.activeControl?.device as Gamepad)) modifierCancelled = true;
                if (rb || y) LastPadPowerLatency = 0f;
                if (attackFromPad && attack.IsPressed()) attackSwallowed = true;

                if (!powerModifier.IsPressed())
                {
                    if (CurrentEcoScheme == EcoScheme.SoltarLB && !modifierCancelled && now - modifierDownAt <= ecoTapMaxHold)
                    {
                        padSlot1 = true;
                        LastPadPowerLatency = now - modifierDownAt;
                    }
                    modifierHeld = false;
                }
            }
            else if (CurrentEcoScheme == EcoScheme.YSozinho && power3Combo.WasPressedThisFrame())
            {
                padSlot1 = true;
                LastPadPowerLatency = 0f;
            }
        }

        // Outro botão do controle apertado neste quadro (fora o LB, o RB e o Y, que a regra trata à parte).
        static bool OtherPadButtonPressed(Gamepad pad)
        {
            if (pad == null) return false;
            return pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
                || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
        }

        void Update()
        {
            EvaluatePowerButtons();
            if (run.WasPressedThisFrame() && run.activeControl?.device is Gamepad)
                runLatched = true;
            if (Move.magnitude < RunReleaseThreshold)
                runLatched = false;
        }
    }
}
