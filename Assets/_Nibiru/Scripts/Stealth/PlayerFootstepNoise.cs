using Nibiru.Player;
using UnityEngine;

namespace Nibiru.Stealth
{
    /// <summary>
    /// Faz barulho a cada passo da Elena. Correr é alto, andar é médio e
    /// agachada quase não se ouve. Parada, não faz barulho.
    /// </summary>
    public class PlayerFootstepNoise : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerMovementSettings settings;

        // Abaixo desta velocidade ela está praticamente parada.
        const float MinMovingSpeed = 0.3f;

        float stepTimer;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        void Update()
        {
            Vector3 velocity = motor.Velocity;
            velocity.y = 0f;
            if (!motor.IsGrounded || velocity.magnitude < MinMovingSpeed)
            {
                // Ao começar a andar, o primeiro passo sai logo.
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;

            bool loud = motor.IsRunning || motor.IsDodging;
            float radius = loud ? settings.runNoiseRadius
                : motor.IsCrouching ? settings.crouchNoiseRadius
                : settings.walkNoiseRadius;
            stepTimer = loud ? settings.stepIntervals.y
                : motor.IsCrouching ? settings.stepIntervals.z
                : settings.stepIntervals.x;

            Noise.Emit(transform.position, radius, gameObject);
        }
    }
}
