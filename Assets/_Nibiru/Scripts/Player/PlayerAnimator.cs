using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Passa o estado do movimento, da mira e do combate para o Animator do modelo da Elena.
    /// O movimento continua todo no PlayerMotor; o Animator só mostra (sem root motion).
    /// O controlador é montado pelo menu Nibiru > Personagens > Montar Elena animada.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PlayerAnimator : MonoBehaviour
    {
        // Nomes dos parâmetros e das camadas no controlador (Elena.controller).
        public const string SpeedParam = "Velocidade";
        public const string CrouchParam = "Agachar";
        public const string DirectionParam = "Sentido";
        public const string DodgeParam = "Esquivar";
        public const string MeleeParam = "Golpe";
        public const string HurtParam = "Dano";
        public const string DieParam = "Morrer";
        public const string SeatedParam = "Sentada";
        public const string AimLayer = "Mira";
        public const string ReloadLayer = "Recarregar";
        public const string IdleVariationParam = "VariacaoParada";
        public const string IdleVariationLayer = "VariacaoParada";

        [SerializeField] Animator animator;
        [SerializeField] PlayerMotor motor;

        [Tooltip("Opcional: a camada de mira acende enquanto a Elena mira com a arma.")]
        [SerializeField] PlayerWeapons weapons;

        [Tooltip("Opcional: o golpe de faca toca a animação do golpe.")]
        [SerializeField] PlayerMeleeAttack melee;

        [Tooltip("Opcional: dano e morte tocam as animações de reação e de queda.")]
        [SerializeField] Health health;

        [Tooltip("Opcional: a arma na mão (provisória) segue a mão direita do modelo enquanto aparece.")]
        [SerializeField] Transform heldGun;

        [Tooltip("Posição da arma em relação à mão direita, nos eixos do Jogador (frente = z).")]
        [SerializeField] Vector3 heldGunOffset = new(0f, 0.02f, 0.08f);

        [Tooltip("Opcional: a faca (provisória) vai para a mão direita durante o golpe, apontando como o antebraço.")]
        [SerializeField] Transform heldKnife;

        [Tooltip("Suaviza a velocidade passada ao Animator (segundos).")]
        [SerializeField] float speedDamping = 0.08f;

        [Tooltip("Quanto as camadas de mira e de recarregar sobem ou descem por segundo.")]
        [SerializeField] float aimBlendSpeed = 8f;

        [Tooltip("Com a câmera mais perto que isto da cabeça ou do peito (encostada numa parede atrás da Elena), o corpo some e fica só a sombra.")]
        [SerializeField] float hideWhenCameraCloserThan = 0.45f;

        [Header("Variações da parada")]
        [Tooltip("Quantas variações de parada o controlador tem (camada VariacaoParada). 0 = nenhuma.")]
        [SerializeField] int idleVariationCount;

        [Tooltip("Depois de quantos segundos parada, mínimo e máximo, ela troca para uma variação.")]
        [SerializeField] Vector2 idleVariationDelay = new(6f, 12f);

        [Tooltip("Quanto tempo cada variação dura (segundos).")]
        [SerializeField] float idleVariationDuration = 5f;

        [Header("Coice do tiro")]
        [Tooltip("Quanto o tronco e os braços sobem a cada tiro, em graus.")]
        [SerializeField] float recoilAngle = 9f;

        [Tooltip("Tempo para o coice voltar ao zero (segundos).")]
        [SerializeField] float recoilRecovery = 0.18f;

        static readonly int SpeedId = Animator.StringToHash(SpeedParam);
        static readonly int CrouchId = Animator.StringToHash(CrouchParam);
        static readonly int DirectionId = Animator.StringToHash(DirectionParam);
        static readonly int DodgeId = Animator.StringToHash(DodgeParam);
        static readonly int MeleeId = Animator.StringToHash(MeleeParam);
        static readonly int HurtId = Animator.StringToHash(HurtParam);
        static readonly int DieId = Animator.StringToHash(DieParam);

        static readonly int IdleVariationId = Animator.StringToHash(IdleVariationParam);

        int aimLayerIndex = -1, reloadLayerIndex = -1, idleLayerIndex = -1;
        int currentVariation, lastVariation;
        float idleWeight, idleTimer, idleNextDelay, variationTimer;
        bool seated;
        float aimWeight, reloadWeight, recoil;
        bool wasDodging, wasAttacking, hidden;
        Transform spine, chest, neck, head, rightUpperArm, leftUpperArm, rightLowerArm, rightHand;
        Renderer[] bodyRenderers;

        /// <summary>True se o controlador tem a queda de morte (o PlayerDeathHandler então não tomba o corpo).</summary>
        public bool HasDeathAnimation => animator != null && animator.runtimeAnimatorController != null && HasParameter(DieId);

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();
            if (melee == null) melee = GetComponent<PlayerMeleeAttack>();
            if (health == null) health = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;
            aimLayerIndex = animator.GetLayerIndex(AimLayer);
            reloadLayerIndex = animator.GetLayerIndex(ReloadLayer);
            idleLayerIndex = animator.GetLayerIndex(IdleVariationLayer);
            idleNextDelay = Random.Range(idleVariationDelay.x, idleVariationDelay.y);
            bodyRenderers = animator.GetComponentsInChildren<Renderer>(true);
            if (animator.isHuman)
            {
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }

        void OnEnable()
        {
            if (weapons != null) weapons.Fired += OnFired;
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (weapons != null) weapons.Fired -= OnFired;
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        void OnFired() => recoil = 1f;

        void OnDamaged(DamageInfo info)
        {
            if (animator != null && HasParameter(HurtId) && !health.IsDead) animator.SetTrigger(HurtId);
        }

        void OnDied()
        {
            if (!HasDeathAnimation) return;
            animator.SetFloat(SpeedId, 0f);
            animator.SetTrigger(DieId);
        }

        // Roda depois do Update do PlayerMotor (ordem 100), para ler a velocidade já deste quadro,
        // e antes do Animator calcular a pose.
        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (health != null && health.IsDead)
            {
                FadeLayers(false, false);
                return;
            }

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

            bool attacking = melee != null && melee.IsAttacking;
            if (attacking && !wasAttacking && HasParameter(MeleeId))
                animator.SetTrigger(MeleeId);
            wasAttacking = attacking;

            bool aiming = weapons != null ? weapons.IsAiming : motor.Aiming;
            bool reloading = weapons != null && weapons.IsReloading;
            FadeLayers(aiming, reloading);

            bool still = speed < 0.1f && motor.CrouchBlend < 0.05f && !aiming && !reloading && !attacking
                && !motor.IsDodging && !seated;
            UpdateIdleVariation(still);
        }

        // Depois de alguns segundos parada, a camada do tronco troca para uma variação (braços cruzados etc.)
        // e volta sozinha; qualquer ação ou movimento apaga a camada. Nunca repete a mesma em sequência.
        void UpdateIdleVariation(bool still)
        {
            if (idleLayerIndex < 0 || idleVariationCount <= 0) return;
            float dt = Time.deltaTime;
            if (!still)
            {
                idleTimer = 0f;
                if (currentVariation != 0) SetVariation(0);
            }
            else if (currentVariation == 0)
            {
                idleTimer += dt;
                if (idleTimer >= idleNextDelay) SetVariation(NextVariation());
            }
            else
            {
                variationTimer += dt;
                if (variationTimer >= idleVariationDuration) SetVariation(0);
            }
            idleWeight = Mathf.MoveTowards(idleWeight, still ? 1f : 0f, aimBlendSpeed * dt);
            animator.SetLayerWeight(idleLayerIndex, idleWeight);
        }

        int NextVariation()
        {
            if (lastVariation == 0 || idleVariationCount == 1) return Random.Range(1, idleVariationCount + 1);
            return 1 + (lastVariation - 1 + Random.Range(1, idleVariationCount)) % idleVariationCount;
        }

        void SetVariation(int variation)
        {
            if (variation != 0) lastVariation = variation;
            currentVariation = variation;
            variationTimer = 0f;
            idleTimer = 0f;
            idleNextDelay = Random.Range(idleVariationDelay.x, idleVariationDelay.y);
            animator.SetInteger(IdleVariationId, variation);
        }

        /// <summary>Variação de parada em curso (0 = nenhuma). Para testes.</summary>
        public int CurrentIdleVariation => currentVariation;

        void FadeLayers(bool aiming, bool reloading)
        {
            float step = aimBlendSpeed * Time.deltaTime;
            aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1f : 0f, step);
            reloadWeight = Mathf.MoveTowards(reloadWeight, reloading ? 1f : 0f, step);
            if (aimLayerIndex >= 0) animator.SetLayerWeight(aimLayerIndex, aimWeight);
            if (reloadLayerIndex >= 0) animator.SetLayerWeight(reloadLayerIndex, reloadWeight);
        }

        // Depois da animação: endireita o tronco ao mirar (agachada, a pose dobra o corpo para a
        // frente e a mira apontaria para o chão), soma o coice do tiro e leva arma e faca para a mão.
        void LateUpdate()
        {
            Vector3 right = transform.right;
            if (aimWeight > 0f && spine != null && neck != null)
            {
                Vector3 torsoUp = Vector3.ProjectOnPlane(neck.position - spine.position, right);
                float lean = Vector3.SignedAngle(torsoUp, Vector3.up, right);
                spine.rotation = Quaternion.AngleAxis(lean * aimWeight, right) * spine.rotation;
            }

            // Coice: o peito e os braços sobem de uma vez e voltam aos poucos.
            if (recoil > 0f)
            {
                float angle = -recoilAngle * recoil * recoil;
                Quaternion kick = Quaternion.AngleAxis(angle, right);
                Quaternion half = Quaternion.AngleAxis(angle * 0.5f, right);
                if (chest != null) chest.rotation = half * chest.rotation;
                if (rightUpperArm != null) rightUpperArm.rotation = kick * rightUpperArm.rotation;
                if (leftUpperArm != null) leftUpperArm.rotation = kick * leftUpperArm.rotation;
                recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime / Mathf.Max(0.01f, recoilRecovery));
            }

            if (heldGun != null && rightHand != null && heldGun.gameObject.activeInHierarchy)
                heldGun.SetPositionAndRotation(rightHand.position + transform.rotation * heldGunOffset, transform.rotation);

            if (heldKnife != null && rightHand != null && rightLowerArm != null && heldKnife.gameObject.activeInHierarchy)
            {
                Vector3 along = rightHand.position - rightLowerArm.position;
                heldKnife.SetPositionAndRotation(rightHand.position, Quaternion.LookRotation(along, transform.up));
            }

            HideIfCameraTooClose();
        }

        // A câmera encostada numa parede atrás da Elena fica dentro da cabeça: aí só a sombra aparece.
        void HideIfCameraTooClose()
        {
            Camera view = Camera.main;
            if (view == null || head == null || chest == null || bodyRenderers == null) return;
            Vector3 eye = view.transform.position;
            bool tooClose = Vector3.Distance(eye, head.position) < hideWhenCameraCloserThan
                || Vector3.Distance(eye, chest.position) < hideWhenCameraCloserThan;
            if (tooClose == hidden) return;
            hidden = tooClose;
            var mode = hidden ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (Renderer r in bodyRenderers)
                if (r != null) r.shadowCastingMode = mode;
        }

        /// <summary>Senta no chão (a viagem na caçamba) ou levanta. Precisa do estado Sentada no controlador.</summary>
        public void SetSeated(bool seated)
        {
            this.seated = seated;
            if (animator != null && HasParameter(Animator.StringToHash(SeatedParam))) animator.SetBool(SeatedParam, seated);
        }

        bool HasParameter(int id)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == id) return true;
            return false;
        }
    }
}
