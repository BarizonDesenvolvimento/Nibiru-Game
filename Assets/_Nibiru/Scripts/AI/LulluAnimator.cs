using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Anima o modelo do Lullu sem mexer na IA dele (LulluAI): lê o estado dela e a vida e passa ao
    /// Animator a velocidade (pelo deslocamento), o golpe, o dano e a morte. Como o modelo foi feito
    /// em pé, e o Lullu é uma criatura encurvada, o tronco, o pescoço e a cabeça são dobrados para a
    /// frente por cima da animação (ossos do Humanoid), um pouco menos quando ele corre.
    /// O controlador é montado pelo menu Nibiru > Personagens > Montar Lullu animado.
    /// </summary>
    public class LulluAnimator : MonoBehaviour
    {
        public const string SpeedParam = "Velocidade";
        public const string StrikeParam = "Golpe";
        public const string HurtParam = "Dano";
        public const string DeathParam = "Morte";

        [SerializeField] Animator animator;
        [SerializeField] LulluAI brain;
        [SerializeField] Health health;

        [Tooltip("O corpo visual (o mesmo \"Body\" da LulluAI). Morto, ele fica de pé no lugar em que morreu, " +
                 "porque a IA tomba a raiz e a animação de morte já deita o modelo.")]
        [SerializeField] Transform visual;

        [Header("Postura encurvada (graus para a frente)")]
        [SerializeField] float spineBend = 22f;
        [SerializeField] float chestBend = 22f;
        [Tooltip("Negativo levanta o pescoço, para ele olhar para a frente em vez de para o chão.")]
        [SerializeField] float neckBend = -18f;
        [SerializeField] float headBend = -12f;

        [Tooltip("Quanto da curva sobra andando ou correndo (0 a 1): a corrida já vem inclinada.")]
        [SerializeField, Range(0f, 1f)] float movingWeight = 0.7f;

        [Tooltip("Velocidade a partir da qual vale só o \"movingWeight\" (m/s).")]
        [SerializeField] float fullyMovingSpeed = 1.2f;

        static readonly int SpeedId = Animator.StringToHash(SpeedParam);
        static readonly int StrikeId = Animator.StringToHash(StrikeParam);
        static readonly int HurtId = Animator.StringToHash(HurtParam);
        static readonly int DeathId = Animator.StringToHash(DeathParam);

        Transform spine, chest, neck, head, leftFoot, rightFoot, model;
        Vector3 modelBase;
        float restAnkleHeight;
        Vector3 lastPosition;
        float speed;
        bool dead;
        Vector3 aliveVisualPosition;
        Quaternion aliveVisualRotation;
        LulluAI.State lastState;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (brain == null) brain = GetComponent<LulluAI>();
            if (health == null) health = GetComponent<Health>();
            animator.applyRootMotion = false;
            if (animator.isHuman)
            {
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
            // Altura dos tornozelos em repouso, ainda na pose do modelo (antes da primeira animação).
            model = animator.transform;
            modelBase = model.localPosition;
            if (leftFoot != null && rightFoot != null && visual != null)
                restAnkleHeight = (Mathf.Min(leftFoot.position.y, rightFoot.position.y) - visual.position.y) / Mathf.Max(0.01f, visual.lossyScale.y);
            lastPosition = transform.position;
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (dead) return;

            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                Vector3 moved = transform.position - lastPosition;
                moved.y = 0f;
                speed = Mathf.Lerp(speed, moved.magnitude / dt, 1f - Mathf.Exp(-12f * dt));
            }
            lastPosition = transform.position;
            animator.SetFloat(SpeedId, speed);

            // Golpe e investida: a preparação é a animação do golpe.
            var state = brain.CurrentState;
            if (state != lastState && (state == LulluAI.State.Golpe || state == LulluAI.State.Investida))
                animator.SetTrigger(StrikeId);
            lastState = state;
        }

        void OnDamaged(DamageInfo info)
        {
            if (!dead && !health.IsDead) animator.SetTrigger(HurtId);
        }

        void OnDied()
        {
            if (dead) return;
            dead = true;
            animator.SetTrigger(DeathId);
        }

        /// <summary>
        /// Já no chão, no fim da animação de morte (ao voltar de um checkpoint depois da luta). Chame antes
        /// de matar a vida: a IA tomba a raiz ao morrer, e o modelo fica onde estava em pé.
        /// </summary>
        public void LieDead()
        {
            dead = true;
            if (visual != null) visual.GetPositionAndRotation(out aliveVisualPosition, out aliveVisualRotation);
            animator.Play(DeathParam, 0, 1f);
        }

        void LateUpdate()
        {
            if (dead)
            {
                // A IA tomba a raiz ao morrer; o modelo fica onde estava e a animação o deita.
                if (visual != null) visual.SetPositionAndRotation(aliveVisualPosition, aliveVisualRotation);
                return;
            }
            if (visual != null) visual.GetPositionAndRotation(out aliveVisualPosition, out aliveVisualRotation);

            KeepFeetOnGround();

            // Curva inteira parado; andando e correndo, um pouco menos (a corrida já vem inclinada).
            float weight = Mathf.Lerp(1f, movingWeight, Mathf.Clamp01(speed / Mathf.Max(0.1f, fullyMovingSpeed)));
            Vector3 right = animator.transform.right;
            Bend(spine, spineBend * weight, right);
            Bend(chest, chestBend * weight, right);
            Bend(neck, neckBend * weight, right);
            Bend(head, headBend * weight, right);
        }

        // Algumas animações (a estocada) baixam o corpo e a sola entra no chão. Sem mexer na animação,
        // o modelo sobe o que faltar para o pé mais baixo ficar na altura de repouso dele.
        void KeepFeetOnGround()
        {
            if (model == null || leftFoot == null || rightFoot == null || visual == null) return;
            model.localPosition = modelBase;
            float scale = Mathf.Max(0.01f, visual.lossyScale.y);
            float lowest = (Mathf.Min(leftFoot.position.y, rightFoot.position.y) - visual.position.y) / scale;
            float lift = Mathf.Clamp(restAnkleHeight - lowest, 0f, 0.25f);
            if (lift > 0f) model.localPosition = modelBase + Vector3.up * lift;
        }

        // Gira o osso para a frente em torno do eixo "direita" do corpo. Os filhos acompanham,
        // por isso a ordem vai do quadril para a cabeça.
        static void Bend(Transform bone, float degrees, Vector3 right)
        {
            if (bone != null) bone.rotation = Quaternion.AngleAxis(degrees, right) * bone.rotation;
        }
    }
}
