using System.Linq;
using Nibiru.Combat;
using Nibiru.Dialogue;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Anima um soldado da Fundação: parado, andar e correr pela velocidade com que ele se move
    /// (pela malha de navegação ou levado por uma cena), mira e tiro de fuzil, recarga, dano e
    /// morte. Com a IA ligada, tudo vem do SoldierAI e da vida; sem ela (figurantes de cena), as
    /// sequências chamam <see cref="AimAt"/>, <see cref="Fire"/> e <see cref="Die"/>.
    /// Fica no modelo (o filho "Visual" do soldado). O controlador é montado pelo menu
    /// Nibiru > Personagens > Montar soldados da Fundação.
    /// </summary>
    public class SoldierAnimator : MonoBehaviour
    {
        // Nomes dos parâmetros, estados e camadas no controlador.
        public const string SpeedParam = "Velocidade";
        public const string FireParam = "Atirar";
        public const string HurtParam = "Dano";
        public const string DieParam = "Morrer";
        public const string LocomotionState = "Locomocao";
        public const string DeathState = "Morte";
        public const string ForwardDeathState = "Morte_Frente";
        public const string ReloadState = "Recarregar";
        public const string AimLayer = "Mira";
        public const string ReloadLayer = "Recarregar";

        [SerializeField] Animator animator;

        [Tooltip("Suaviza a velocidade passada ao Animator (segundos).")]
        [SerializeField] float speedDamping = 0.15f;

        [Tooltip("Tempo para levantar ou baixar o fuzil (segundos).")]
        [SerializeField] float aimBlend = 0.2f;

        [Tooltip("Quanto o tronco gira na mira (graus, + para a direita): nas animações de fuzil do Mixamo o soldado fica de lado e o cano aponta para a esquerda. Medido pelo menu dos soldados.")]
        [SerializeField] float aimTwist = 27f;

        [Tooltip("Tempo da recarga quando não há IA para dizer (segundos).")]
        [SerializeField] float reloadTime = 2.4f;

        // Acima disso o soldado foi levado de um lugar para outro (cena, checkpoint), não andou.
        const float TeleportSpeed = 12f;

        static readonly int SpeedId = Animator.StringToHash(SpeedParam);
        static readonly int FireId = Animator.StringToHash(FireParam);
        static readonly int HurtId = Animator.StringToHash(HurtParam);
        static readonly int DieId = Animator.StringToHash(DieParam);

        SoldierAI ai;
        Health health;
        Transform body;
        DialogueRunner runner;
        int aimLayer = -1, reloadLayer = -1;
        float aimWeight, reloadUntil = -1f;
        Vector3 lastPosition;
        bool scriptedAim, dead;
        string deathPose = DeathState;
        Transform[] spine;

        public bool IsDead => dead;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            ai = GetComponentInParent<SoldierAI>();
            health = GetComponentInParent<Health>();
            body = ai != null ? ai.transform : transform;
            aimLayer = animator.GetLayerIndex(AimLayer);
            reloadLayer = animator.GetLayerIndex(ReloadLayer);
            if (animator.isHuman)
                spine = new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest }
                    .Select(animator.GetBoneTransform).Where(b => b != null).ToArray();
        }

        void OnEnable()
        {
            lastPosition = body.position;
            if (ai != null)
            {
                ai.Fired += Fire;
                ai.ReloadStarted += Reload;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += Die;
            }
            // Ao ligar de novo (o grupo de soldados aparece), continua morto se já estava.
            if (dead) LieDead();
        }

        void OnDisable()
        {
            if (ai != null)
            {
                ai.Fired -= Fire;
                ai.ReloadStarted -= Reload;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= Die;
            }
        }

        /// <summary>Levanta (ou baixa) o fuzil numa cena, sem a IA. Quando a IA liga, ela manda.</summary>
        public void AimAt(bool aim) => scriptedAim = aim;

        /// <summary>O coice de um disparo (a IA chama sozinha; numa cena, junto com o tiro).</summary>
        public void Fire()
        {
            if (dead) return;
            animator.SetTrigger(FireId);
        }

        /// <summary>Gesto de recarregar o fuzil, no tempo da recarga da IA.</summary>
        public void Reload()
        {
            if (dead) return;
            float duration = ai != null ? ai.ReloadTime : reloadTime;
            reloadUntil = Time.time + duration;
            if (reloadLayer >= 0) animator.Play(ReloadState, reloadLayer, 0f);
        }

        /// <summary>Cai morto para trás (levou tiro ou golpe de frente).</summary>
        public void Die()
        {
            if (dead) return;
            Fall();
            animator.SetTrigger(DieId);
        }

        /// <summary>Cai de frente (golpe pelas costas, como a facada do Samir).</summary>
        public void DieForward()
        {
            if (dead) return;
            Fall();
            deathPose = ForwardDeathState;
            animator.CrossFadeInFixedTime(ForwardDeathState, 0.1f, 0);
        }

        /// <summary>Já no chão, no último quadro da morte (ao voltar de um checkpoint).</summary>
        public void LieDead()
        {
            Fall();
            aimWeight = 0f;
            SetLayerWeights(0f, 0f);
            animator.Play(deathPose, 0, 1f);
            animator.Update(0f);
        }

        /// <summary>Já no chão, caído de frente.</summary>
        public void LieDeadForward()
        {
            deathPose = ForwardDeathState;
            LieDead();
        }

        // Morto, o colisor em pé do soldado sai do caminho (de quem anda e dos tiros).
        void Fall()
        {
            dead = true;
            scriptedAim = false;
            if (ai != null)
                foreach (Collider collider in ai.GetComponents<Collider>()) collider.enabled = false;
        }

        void OnDamaged(DamageInfo info)
        {
            if (!dead && health != null && !health.IsDead) animator.SetTrigger(HurtId);
        }

        void Update()
        {
            if (runner == null) runner = FindAnyObjectByType<DialogueRunner>();

            // Na conversa o tempo do jogo para: ele fica parado, mas continua respirando.
            bool inDialogue = runner != null && runner.IsRunning;
            animator.updateMode = inDialogue ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            float dt = inDialogue ? Time.unscaledDeltaTime : Time.deltaTime;

            // Velocidade pelo deslocamento: vale para a malha de navegação e para as cenas.
            Vector3 position = body.position;
            Vector3 moved = position - lastPosition;
            lastPosition = position;
            moved.y = 0f;
            float speed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;
            if (speed > TeleportSpeed || dead) speed = 0f;
            animator.SetFloat(SpeedId, speed, speedDamping, dt);

            // Com a IA ligada, ela decide a mira; a mira de cena vale só para figurantes.
            if (ai != null && ai.enabled) scriptedAim = false;
            bool reloading = !dead && Time.time < reloadUntil;
            bool aiming = !dead && !reloading && (scriptedAim || (ai != null && ai.IsAiming));
            float step = aimBlend > 0f ? dt / aimBlend : 1f;
            aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1f : 0f, step);
            float reloadWeight = reloadLayer >= 0 ? animator.GetLayerWeight(reloadLayer) : 0f;
            reloadWeight = Mathf.MoveTowards(reloadWeight, reloading ? 1f : 0f, step);
            SetLayerWeights(aimWeight, reloadWeight);
        }

        // Na mira, o tronco gira (dividido entre as vértebras) para o cano apontar para a frente.
        void LateUpdate()
        {
            if (spine == null || spine.Length == 0 || aimWeight <= 0f) return;
            Quaternion turn = Quaternion.AngleAxis(aimTwist * aimWeight / spine.Length, body.up);
            foreach (Transform bone in spine) bone.rotation = turn * bone.rotation;
        }

        void SetLayerWeights(float aim, float reload)
        {
            if (aimLayer >= 0) animator.SetLayerWeight(aimLayer, aim);
            if (reloadLayer >= 0) animator.SetLayerWeight(reloadLayer, reload);
        }
    }
}
