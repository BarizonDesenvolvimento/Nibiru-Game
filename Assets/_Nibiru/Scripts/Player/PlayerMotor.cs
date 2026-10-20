using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Move o jogador com um CharacterController. A direção é relativa à câmera:
    /// "frente" é para onde a câmera olha, projetado no chão.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour, IInvulnerability
    {
        [SerializeField] PlayerMovementSettings settings;
        [SerializeField] PlayerInputReader input;

        [Tooltip("O jogador anda para onde esta câmera olha. Vazio = Main Camera.")]
        [SerializeField] Transform cameraTransform;

        // Valor negativo pequeno para manter o CharacterController grudado no chão.
        const float GroundedVerticalSpeed = -2f;

        // Abaixo disso o direcional conta como solto.
        const float InputDeadZoneSqr = 0.0001f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;

        Vector3 dodgeDirection;
        float dodgeElapsed;
        float dodgeCooldownLeft;

        float standHeight;
        float standCenterY;
        readonly Collider[] standOverlaps = new Collider[8];

        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }

        /// <summary>De 0 (em pé) a 1 (agachada), suave na transição. O visual e a câmera usam isto.</summary>
        public float CrouchBlend { get; private set; }
        public bool IsDodging { get; private set; }
        public bool IsInvulnerable => IsDodging && dodgeElapsed < settings.dodgeInvulnerableTime;
        public bool IsGrounded => controller.isGrounded;

        /// <summary>Direção do direcional neste quadro, já relativa à câmera (tamanho de 0 a 1).</summary>
        public Vector3 DesiredMoveDirection { get; private set; }

        /// <summary>Enquanto true, a Elena não anda nem gira (por exemplo, durante um golpe).</summary>
        public bool MovementLocked { get; set; }

        /// <summary>Enquanto true, a esquiva não começa (por exemplo, no início de um golpe).</summary>
        public bool DodgeBlocked { get; set; }

        /// <summary>Enquanto true, a Elena anda devagar, sem correr, sempre virada para onde a câmera olha.</summary>
        public bool Aiming { get; set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            standHeight = controller.height;
            standCenterY = controller.center.y;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 moveDirection = CameraRelative(input.Move);
            DesiredMoveDirection = moveDirection;

            if (input.CrouchPressed && !IsDodging && controller.isGrounded)
            {
                if (!IsCrouching) IsCrouching = true;
                else TryStand();
            }
            UpdateCrouchShape(dt);

            dodgeCooldownLeft -= dt;
            if (input.DodgePressed && CanDodge())
                StartDodge(moveDirection);

            if (IsDodging)
                UpdateDodge(dt);
            else
                UpdateWalkAndRun(MovementLocked ? Vector3.zero : moveDirection, dt);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = GroundedVerticalSpeed;
            else
                verticalVelocity += settings.gravity * dt;

            controller.Move(Velocity * dt);
        }

        void UpdateWalkAndRun(Vector3 moveDirection, float dt)
        {
            bool hasInput = moveDirection.sqrMagnitude > InputDeadZoneSqr;

            // Querer correr agachada faz a Elena levantar (se houver espaço).
            if (IsCrouching && hasInput && input.RunActive && !Aiming) TryStand();

            if (Aiming)
            {
                // Mirando: anda devagar para qualquer lado, sempre de frente para a mira.
                IsRunning = false;
                float aimSpeed = IsCrouching ? Mathf.Min(settings.aimWalkSpeed, settings.crouchSpeed) : settings.aimWalkSpeed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, moveDirection * aimSpeed, settings.acceleration * dt);
                Vector3 aimForward = CameraRelative(Vector2.up);
                if (aimForward.sqrMagnitude > InputDeadZoneSqr)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aimForward), settings.turnSpeed * 2f * dt);
                return;
            }

            // Correndo, vai sempre na velocidade máxima; andando, o analógico controla a velocidade.
            IsRunning = input.RunActive && hasInput && !IsCrouching;
            Vector3 targetVelocity = IsRunning
                ? moveDirection.normalized * settings.runSpeed
                : moveDirection * (IsCrouching ? settings.crouchSpeed : settings.walkSpeed);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, settings.acceleration * dt);

            if (hasInput)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, settings.turnSpeed * dt);
            }
        }

        bool CanDodge() => !IsDodging && !DodgeBlocked && dodgeCooldownLeft <= 0f && controller.isGrounded;

        // Com direcional, esquiva para aquele lado e vira o corpo para lá.
        // Sem direcional, dá um passo para trás mantendo a frente.
        void StartDodge(Vector3 moveDirection)
        {
            // A esquiva levanta a Elena, se houver espaço.
            if (IsCrouching) TryStand();

            if (moveDirection.sqrMagnitude > InputDeadZoneSqr)
            {
                dodgeDirection = moveDirection.normalized;
                transform.rotation = Quaternion.LookRotation(dodgeDirection);
            }
            else
            {
                dodgeDirection = -transform.forward;
            }

            dodgeElapsed = 0f;
            IsDodging = true;
            IsRunning = false;
        }

        void UpdateDodge(float dt)
        {
            float previous = DodgeProgress(dodgeElapsed);
            dodgeElapsed += dt;
            float current = DodgeProgress(dodgeElapsed);

            // Anda exatamente a fatia da distância que corresponde a este quadro,
            // então a distância total não depende do FPS.
            float step = (current - previous) * settings.dodgeDistance;
            horizontalVelocity = dt > 0f ? dodgeDirection * (step / dt) : Vector3.zero;

            if (dodgeElapsed >= settings.dodgeDuration)
            {
                IsDodging = false;
                dodgeCooldownLeft = settings.dodgeCooldown;
            }
        }

        // Fração da distância já percorrida (0 a 1). Começa rápida e desacelera até parar.
        float DodgeProgress(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / settings.dodgeDuration);
            return 1f - (1f - t) * (1f - t);
        }

        // Diminui ou aumenta a colisão mantendo os pés no mesmo lugar.
        void UpdateCrouchShape(float dt)
        {
            CrouchBlend = Mathf.MoveTowards(CrouchBlend, IsCrouching ? 1f : 0f, settings.crouchTransitionSpeed * dt);
            float height = Mathf.Lerp(standHeight, settings.crouchHeight, CrouchBlend);
            controller.height = height;
            controller.center = new Vector3(controller.center.x, standCenterY - (standHeight - height) * 0.5f, controller.center.z);
        }

        // Só levanta se não houver nada em cima (um teto baixo, por exemplo).
        void TryStand()
        {
            if (CanStand()) IsCrouching = false;
        }

        bool CanStand()
        {
            float radius = controller.radius * 0.95f;
            // A esfera de baixo sobe um pouco para não encostar no chão ou numa rampa.
            Vector3 bottom = transform.position + Vector3.up * (standCenterY - standHeight * 0.5f + controller.radius + 0.1f);
            Vector3 top = transform.position + Vector3.up * (standCenterY + standHeight * 0.5f - controller.radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, standOverlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (standOverlaps[i] != controller && !standOverlaps[i].transform.IsChildOf(transform))
                    return false;
            return true;
        }

        // Converte o direcional (x = lados, y = frente) em direção no mundo, relativa à câmera.
        // O tamanho vai de 0 a 1, então o analógico meio inclinado anda mais devagar.
        Vector3 CameraRelative(Vector2 stick)
        {
            stick = Vector2.ClampMagnitude(stick, 1f);
            if (cameraTransform == null) return new Vector3(stick.x, 0f, stick.y);

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            return forward * stick.y + right * stick.x;
        }
    }
}
