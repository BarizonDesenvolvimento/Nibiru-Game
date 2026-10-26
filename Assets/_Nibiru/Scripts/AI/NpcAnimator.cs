using Nibiru.Dialogue;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.AI
{
    /// <summary>
    /// Anima um personagem que não é a Elena (o Samir, por exemplo): andar e correr pela velocidade
    /// do NavMeshAgent, agachar, golpe de faca e gestos de conversa nas falas dele. Quem anda é o
    /// NavMeshAgent (WalkToPoint); o Animator só mostra, sem root motion.
    /// Durante os diálogos o jogo pausa, mas o personagem continua respirando e gesticulando.
    /// O controlador é montado pelo menu do personagem (Nibiru > Personagens > Montar Samir animado).
    /// </summary>
    public class NpcAnimator : MonoBehaviour
    {
        // Nomes dos parâmetros e dos estados no controlador.
        public const string SpeedParam = "Velocidade";
        public const string CrouchParam = "Agachar";
        public const string StrikeParam = "Golpe";
        public const string DrivingParam = "Dirigindo";
        public const string LocomotionState = "Locomocao";
        public const string DrivingState = "Dirigindo";
        public const string TalkStatePrefix = "Conversa_";

        [SerializeField] Animator animator;

        [Tooltip("De onde vem a velocidade. Vazio = o NavMeshAgent do personagem (procurado nos pais).")]
        [SerializeField] NavMeshAgent agent;

        [Tooltip("Nome de quem fala nos diálogos (o campo \"Speaker\" das falas). Nas falas dele, toca um gesto de conversa.")]
        [SerializeField] string speakerName = "Samir";

        [Tooltip("Quantos gestos de conversa o controlador tem (estados Conversa_1, Conversa_2...).")]
        [SerializeField, Min(1)] int talkVariations = 4;

        [Tooltip("Opcional: a faca, que só aparece na mão direita durante o golpe.")]
        [SerializeField] Transform knife;

        [Tooltip("Quanto tempo a faca fica na mão depois do golpe (segundos).")]
        [SerializeField, Min(0f)] float knifeShownFor = 0.8f;

        [Tooltip("Velocidade do NavMeshAgent andando agachado (m/s).")]
        [SerializeField, Min(0.1f)] float crouchSpeed = 1.4f;

        [Tooltip("Sentado ao volante (o Samir na caminhonete): liga o estado Dirigindo, em vez de andar.")]
        [SerializeField] bool driving;

        [Tooltip("Suaviza a velocidade passada ao Animator (segundos).")]
        [SerializeField] float speedDamping = 0.1f;

        [Tooltip("Tempo da troca entre andar e gesticular (segundos).")]
        [SerializeField] float talkBlend = 0.35f;

        static readonly int SpeedId = Animator.StringToHash(SpeedParam);
        static readonly int CrouchId = Animator.StringToHash(CrouchParam);
        static readonly int StrikeId = Animator.StringToHash(StrikeParam);

        DialogueRunner runner;
        Transform rightHand, rightLowerArm;
        float crouch, knifeTimer, standingSpeed = -1f;
        bool talking;
        int lastTalk;

        public bool IsCrouched => crouch > 0.5f;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
            animator.applyRootMotion = false;
            if (animator.isHuman)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            }
            if (knife != null) knife.gameObject.SetActive(false);
            if (driving) animator.SetBool(DrivingParam, true);
        }

        /// <summary>Senta ao volante (ou levanta). Precisa do estado Dirigindo no controlador.</summary>
        public void SetDriving(bool value) => animator.SetBool(DrivingParam, value);

        void OnDisable()
        {
            Listen(null);
            talking = false;
            knifeTimer = 0f;
            if (knife != null) knife.gameObject.SetActive(false);
        }

        /// <summary>Golpe de faca com a mão direita (por exemplo, o Samir derrubando o soldado).</summary>
        public void Strike()
        {
            animator.SetTrigger(StrikeId);
            knifeTimer = knifeShownFor;
            if (knife != null) knife.gameObject.SetActive(true);
        }

        /// <summary>Passa a andar agachado, mais devagar.</summary>
        public void Sneak()
        {
            crouch = 1f;
            if (agent == null) return;
            if (standingSpeed < 0f) standingSpeed = agent.speed;
            agent.speed = crouchSpeed;
        }

        /// <summary>Volta a andar em pé, na velocidade de antes.</summary>
        public void StandUp()
        {
            crouch = 0f;
            if (agent != null && standingSpeed > 0f) agent.speed = standingSpeed;
            standingSpeed = -1f;
        }

        void Update()
        {
            if (runner == null) Listen(FindAnyObjectByType<DialogueRunner>());

            // Na conversa o tempo do jogo para: ele fica parado, mas a animação segue no tempo real.
            bool inDialogue = runner != null && runner.IsRunning;
            animator.updateMode = inDialogue ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            float dt = inDialogue ? Time.unscaledDeltaTime : Time.deltaTime;

            float speed = !inDialogue && agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
            animator.SetFloat(SpeedId, speed, speedDamping, dt);
            animator.SetFloat(CrouchId, crouch, 0.2f, dt);

            if (knifeTimer > 0f)
            {
                knifeTimer -= Time.deltaTime;
                if (knifeTimer <= 0f && knife != null) knife.gameObject.SetActive(false);
            }
        }

        // A faca na mão direita, apontando como o antebraço.
        void LateUpdate()
        {
            if (knife == null || !knife.gameObject.activeSelf || rightHand == null || rightLowerArm == null) return;
            Vector3 along = (rightHand.position - rightLowerArm.position).normalized;
            knife.SetPositionAndRotation(rightHand.position + along * 0.07f, Quaternion.LookRotation(along, transform.up));
        }

        void Listen(DialogueRunner next)
        {
            if (runner != null)
            {
                runner.NodeShown -= OnNodeShown;
                runner.Ended -= OnDialogueEnded;
            }
            runner = next;
            if (runner == null) return;
            runner.NodeShown += OnNodeShown;
            runner.Ended += OnDialogueEnded;
        }

        // Cada fala dele ganha um gesto diferente do anterior; nas falas dos outros, ele escuta parado.
        void OnNodeShown(DialogueNode node, System.Collections.Generic.IReadOnlyList<DialogueChoice> choices)
        {
            if (!isActiveAndEnabled) return;
            bool mine = node != null && string.Equals(node.speaker?.Trim(), speakerName, System.StringComparison.OrdinalIgnoreCase);
            if (mine)
            {
                int next = Random.Range(1, talkVariations + 1);
                if (talkVariations > 1 && next == lastTalk) next = next % talkVariations + 1;
                lastTalk = next;
                animator.CrossFadeInFixedTime(TalkStatePrefix + next, talkBlend);
            }
            else if (talking)
                animator.CrossFadeInFixedTime(LocomotionState, talkBlend);
            talking = mine;
        }

        void OnDialogueEnded()
        {
            if (talking && isActiveAndEnabled) animator.CrossFadeInFixedTime(LocomotionState, talkBlend);
            talking = false;
        }
    }
}
