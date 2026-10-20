using System;
using System.Collections.Generic;
using Nibiru.Player;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Ataque corpo a corpo da Elena. Ao apertar Atacar, ela para, vira para a direção
    /// do direcional (se houver) e golpeia à frente. Na recuperação, a esquiva cancela o golpe.
    /// </summary>
    public class PlayerMeleeAttack : MonoBehaviour
    {
        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] Health ownHealth;

        [Tooltip("Opcional: objeto girado durante o golpe para mostrar a faca. Fica escondido fora do ataque.")]
        [SerializeField] Transform swingVisual;

        [Tooltip("Abertura do arco da faca, em graus.")]
        [SerializeField] float swingArc = 120f;

        readonly Collider[] overlaps = new Collider[16];
        readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        float elapsed = -1f;

        public bool IsAttacking => elapsed >= 0f;

        /// <summary>Avisa quando o golpe acerta alguém (para som e efeitos, mais tarde).</summary>
        public event Action<Health> Hit;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (ownHealth == null) ownHealth = GetComponent<Health>();
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

            if (!IsAttacking && input.AttackPressed && CanStart())
                StartAttack();
        }

        bool CanStart() => !motor.IsDodging && motor.IsGrounded && (ownHealth == null || !ownHealth.IsDead);

        void StartAttack()
        {
            Vector3 direction = motor.DesiredMoveDirection;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction);

            elapsed = 0f;
            hitThisSwing.Clear();
            motor.MovementLocked = true;
            motor.DodgeBlocked = true;
            if (swingVisual != null) swingVisual.gameObject.SetActive(true);
            UpdateVisual();
        }

        void Tick(float dt)
        {
            elapsed += dt;
            float activeEnd = attack.windup + attack.activeTime;

            if (elapsed >= attack.windup && elapsed <= activeEnd)
                HitWhatIsInFront();

            // Na recuperação, a esquiva volta a valer e pode cancelar o golpe.
            if (elapsed > activeEnd) motor.DodgeBlocked = false;

            UpdateVisual();
            if (elapsed >= attack.TotalTime) EndAttack();
        }

        void EndAttack()
        {
            elapsed = -1f;
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
