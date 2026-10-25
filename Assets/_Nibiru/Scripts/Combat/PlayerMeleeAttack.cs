using System;
using System.Collections.Generic;
using Nibiru.Inventory;
using Nibiru.Player;
using Nibiru.Stealth;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Ataque corpo a corpo da Elena. Ao apertar Atacar, ela para, vira para a direção
    /// do direcional (se houver) e golpeia à frente. Na recuperação, a esquiva cancela o golpe.
    /// Atrás de um soldado distraído (que não a viu há pouco), o golpe vira o golpe pelas
    /// costas: ela vira para ele e o derruba de uma vez, quase sem barulho.
    /// </summary>
    public class PlayerMeleeAttack : MonoBehaviour
    {
        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] Health ownHealth;

        [Tooltip("Item que a Elena precisa ter para usar a faca (opcional). Vazio = a faca está sempre disponível.")]
        [SerializeField] ItemDefinition requiredItem;

        [Tooltip("Inventário onde procurar o item acima. Vazio = procura no próprio objeto.")]
        [SerializeField] PlayerInventory inventory;

        [Tooltip("Opcional: objeto girado durante o golpe para mostrar a faca. Fica escondido fora do ataque.")]
        [SerializeField] Transform swingVisual;

        [Tooltip("Abertura do arco da faca, em graus.")]
        [SerializeField] float swingArc = 120f;

        [Header("Golpe pelas costas")]
        [Tooltip("Distância máxima até o soldado, medida no chão, em metros.")]
        [SerializeField, Min(0f)] float takedownReach = 1.7f;

        [Tooltip("Abertura das costas do soldado em que o golpe vale, em graus (180 = toda a metade de trás).")]
        [SerializeField, Range(10f, 180f)] float takedownBackArc = 140f;

        [Tooltip("Até onde o golpe pelas costas é ouvido, em metros.")]
        [SerializeField, Min(0f)] float takedownNoiseRadius = 1f;

        readonly Collider[] overlaps = new Collider[16];
        readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        float elapsed = -1f;
        ITakedownTarget takedown;
        bool takedownSwing;

        public bool IsAttacking => elapsed >= 0f;

        /// <summary>O soldado que o golpe derrubaria pelas costas agora (para o aviso na tela), ou null.</summary>
        public ITakedownTarget TakedownTarget { get; private set; }

        /// <summary>Avisa quando o golpe acerta alguém (para som e efeitos, mais tarde).</summary>
        public event Action<Health> Hit;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (ownHealth == null) ownHealth = GetComponent<Health>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (swingVisual != null) swingVisual.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (IsAttacking) EndAttack();
        }

        void Update()
        {
            if (IsAttacking)
            {
                if (motor.IsDodging) EndAttack();
                else Tick(Time.deltaTime);
            }

            bool canStart = !IsAttacking && CanStart();
            TakedownTarget = canStart && input.CombatEnabled ? FindTakedownTarget() : null;
            if (canStart && input.AttackPressed)
                StartAttack(TakedownTarget);
        }

        // Mirando, o clique é tiro, não faca.
        bool CanStart() => HasKnife && !motor.IsDodging && !motor.Aiming && motor.IsGrounded && (ownHealth == null || !ownHealth.IsDead);

        // Sem item pedido, a faca vale sempre (como no laboratório de testes).
        bool HasKnife => requiredItem == null || (inventory != null && inventory.Has(requiredItem));

        // Com um alvo pelas costas, vira para ele em vez de seguir o direcional.
        void StartAttack(ITakedownTarget target)
        {
            takedown = target;
            takedownSwing = target != null;
            Vector3 direction = target != null
                ? Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up)
                : motor.DesiredMoveDirection;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);

            elapsed = 0f;
            hitThisSwing.Clear();
            motor.MovementLocked = true;
            motor.DodgeBlocked = true;
            if (swingVisual != null) swingVisual.gameObject.SetActive(true);
            UpdateVisual();
            Noise.Emit(transform.position, target != null ? takedownNoiseRadius : attack.noiseRadius, gameObject);
        }

        void Tick(float dt)
        {
            elapsed += dt;
            float activeEnd = attack.windup + attack.activeTime;

            if (takedownSwing)
            {
                // Pelas costas: um golpe só, no alvo escolhido.
                if (elapsed >= attack.windup && takedown != null)
                {
                    takedown.TakeDown(gameObject);
                    takedown = null;
                }
            }
            else if (elapsed >= attack.windup && elapsed <= activeEnd)
                HitWhatIsInFront();

            // Na recuperação, a esquiva volta a valer e pode cancelar o golpe.
            if (elapsed > activeEnd) motor.DodgeBlocked = false;

            UpdateVisual();
            if (elapsed >= attack.TotalTime) EndAttack();
        }

        void EndAttack()
        {
            elapsed = -1f;
            takedown = null;
            motor.MovementLocked = false;
            motor.DodgeBlocked = false;
            if (swingVisual != null) swingVisual.gameObject.SetActive(false);
        }

        void HitWhatIsInFront()
        {
            Vector3 center = AttackCenter();
            int count = Physics.OverlapSphereNonAlloc(center, attack.radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Health target = overlaps[i].GetComponentInParent<Health>();
                if (target == null || target == ownHealth || !hitThisSwing.Add(target)) continue;

                Vector3 point = overlaps[i].bounds.ClosestPoint(center);
                if (target.TakeDamage(new DamageInfo(attack.damage, point, gameObject)))
                    Hit?.Invoke(target);
            }
        }

        // O soldado mais perto, distraído, com a Elena atrás dele (dentro do arco das costas).
        ITakedownTarget FindTakedownTarget()
        {
            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, takedownReach + 0.5f, overlaps, ~0, QueryTriggerInteraction.Ignore);
            ITakedownTarget best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = overlaps[i].GetComponentInParent<ITakedownTarget>();
                if (candidate == null || !candidate.CanBeTakenDown) continue;

                Transform target = candidate.transform;
                Vector3 offset = center - target.position;
                if (Mathf.Abs(offset.y) > 2f) continue;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance > takedownReach || distance >= bestDistance) continue;

                Vector3 back = Vector3.ProjectOnPlane(-target.forward, Vector3.up);
                if (Vector3.Angle(back, offset) > takedownBackArc * 0.5f) continue;

                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        // A faca varre da direita para a esquerda durante a preparação e o acerto.
        void UpdateVisual()
        {
            if (swingVisual == null) return;
            float t = Mathf.Clamp01(elapsed / (attack.windup + attack.activeTime));
            float angle = Mathf.Lerp(swingArc * 0.5f, -swingArc * 0.5f, t);
            swingVisual.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        Vector3 AttackCenter() =>
            transform.position + transform.forward * attack.reach + Vector3.up * attack.height;

        void OnDrawGizmosSelected()
        {
            if (attack == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(AttackCenter(), attack.radius);
        }
    }
}
