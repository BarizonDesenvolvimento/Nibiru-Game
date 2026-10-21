using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Passa o estado do movimento e da mira para o Animator do modelo da Elena.
    /// O movimento continua todo no PlayerMotor; o Animator só mostra (sem root motion).
    /// O controlador é montado pelo menu Nibiru > Personagens > Montar Elena animada.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PlayerAnimator : MonoBehaviour
    {
        // Nomes dos parâmetros e da camada no controlador (Elena.controller).
        public const string SpeedParam = "Velocidade";
        public const string CrouchParam = "Agachar";
        public const string DirectionParam = "Sentido";
        public const string DodgeParam = "Esquivar";
        public const string AimLayer = "Mira";

        [SerializeField] Animator animator;
        [SerializeField] PlayerMotor motor;

        [Tooltip("Opcional: a camada de mira acende enquanto a Elena mira com a arma.")]
        [SerializeField] PlayerWeapons weapons;

        [Tooltip("Opcional: a arma na mão (provisória) segue a mão direita do modelo enquanto aparece.")]
        [SerializeField] Transform heldGun;

        [Tooltip("Posição da arma em relação à mão direita, nos eixos do Jogador (frente = z).")]
        [SerializeField] Vector3 heldGunOffset = new(0f, 0.02f, 0.08f);

        [Tooltip("Suaviza a velocidade passada ao Animator (segundos).")]
        [SerializeField] float speedDamping = 0.08f;

        [Tooltip("Quanto a camada de mira sobe ou desce por segundo.")]
        [SerializeField] float aimBlendSpeed = 8f;

        static readonly int SpeedId = Animator.StringToHash(SpeedParam);
        static readonly int CrouchId = Animator.StringToHash(CrouchParam);
        static readonly int DirectionId = Animator.StringToHash(DirectionParam);
        static readonly int DodgeId = Animator.StringToHash(DodgeParam);

        int aimLayerIndex = -1;
        float aimWeight;
        bool wasDodging;
        Transform spine, neck, rightHand;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            aimLayerIndex = animator.GetLayerIndex(AimLayer);
            if (animator.isHuman)
            {
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }

        // Roda depois do Update do PlayerMotor (ordem 100), para ler a velocidade já deste quadro,
        // e antes do Animator calcular a pose.
        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            Vector3 velocity = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up);
            float speed = velocity.magnitude;

            // Andando de costas (mirando), a animação de andar toca ao contrário.
            float forward = Vector3.Dot(velocity, transform.forward);
            float direction = forward < -0.3f * speed ? -1f : 1f;

            animator.SetFloat(SpeedId, speed, speedDamping, Time.deltaTime);
            animator.SetFloat(CrouchId, motor.CrouchBlend);
            animator.SetFloat(DirectionId, direction);

            // A esquiva para a frente tem animação própria; o passo para trás usa o andar ao contrário.
            if (motor.IsDodging && !wasDodging && forward > 0f)
                animator.SetTrigger(DodgeId);
            wasDodging = motor.IsDodging;

            if (aimLayerIndex >= 0)
            {
                bool aiming = weapons != null ? weapons.IsAiming : motor.Aiming;
                aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1f : 0f, aimBlendSpeed * Time.deltaTime);
                animator.SetLayerWeight(aimLayerIndex, aimWeight);
            }
        }

        // Depois da animação: endireita o tronco ao mirar (agachada, a pose dobra o corpo para a
        // frente e a mira apontaria para o chão) e leva a arma para a mão direita.
        void LateUpdate()
        {
            if (aimWeight > 0f && spine != null && neck != null)
            {
                Vector3 right = transform.right;
                Vector3 torsoUp = Vector3.ProjectOnPlane(neck.position - spine.position, right);
                float lean = Vector3.SignedAngle(torsoUp, Vector3.up, right);
                spine.rotation = Quaternion.AngleAxis(lean * aimWeight, right) * spine.rotation;
            }

            if (heldGun != null && rightHand != null && heldGun.gameObject.activeInHierarchy)
                heldGun.SetPositionAndRotation(rightHand.position + transform.rotation * heldGunOffset, transform.rotation);
        }
    }
}
