using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Move o jogador com um CharacterController. A direção é relativa à câmera:
    /// "frente" é para onde a câmera olha, projetado no chão.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] PlayerMovementSettings settings;
        [SerializeField] PlayerInputReader input;

        [Tooltip("O jogador anda para onde esta câmera olha. Vazio = Main Camera.")]
        [SerializeField] Transform cameraTransform;

        // Valor negativo pequeno para manter o CharacterController grudado no chão.
        const float GroundedVerticalSpeed = -2f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalVelocity;

        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        public bool IsRunning { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 moveDirection = CameraRelative(input.Move);

            // Correndo, vai sempre na velocidade máxima; andando, o analógico controla a velocidade.
            IsRunning = input.RunActive && moveDirection.sqrMagnitude > 0.0001f;
            Vector3 targetVelocity = IsRunning
                ? moveDirection.normalized * settings.runSpeed
                : moveDirection * settings.walkSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, settings.acceleration * dt);

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, settings.turnSpeed * dt);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = GroundedVerticalSpeed;
            else
                verticalVelocity += settings.gravity * dt;

            controller.Move(Velocity * dt);
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
