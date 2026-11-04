using Nibiru.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.Combat
{
    /// <summary>O tipo de reação a um tiro forte.</summary>
    public enum HitReaction
    {
        Nenhuma,
        /// <summary>Cambaleia: cancela o que fazia por um instante.</summary>
        Cambaleio,
        /// <summary>Cai no chão e levanta depois.</summary>
        Queda,
    }

    /// <summary>
    /// Faz um inimigo cambalear ou cair quando leva um tiro forte (a espingarda). É criado sozinho no inimigo
    /// na primeira vez que precisa (<see cref="Apply"/>). Quem pode o quê vem do tipo do inimigo:
    /// soldados e atiradores cambaleiam e caem; o Lullu (híbrido) só cambaleia; o Primeiro Servo (chefe)
    /// leva o dano normalmente, mas nunca reage. Enquanto reage, a IA do inimigo fica parada (<c>ReactionLock</c>).
    /// A reação é provisória: sem animações próprias, o modelo se inclina e o corpo é empurrado para trás.
    /// </summary>
    public class HitReactor : MonoBehaviour
    {
        const float StaggerLean = 18f;
        const float FallAngle = 80f;
        const float FallTime = 0.25f;
        const float RiseTime = 0.45f;
        const float StaggerPush = 0.5f;
        const float FallPush = 1.2f;

        Health health;
        NavMeshAgent agent;
        SoldierAI soldier;
        SniperAI sniper;
        LulluAI lullu;
        Transform model;
        Quaternion modelRest;

        float startTime;
        float endTime;
        Vector3 pushDirection;
        float pushed;
        float pushTotal;

        /// <summary>Reagindo agora.</summary>
        public bool Busy => Current != HitReaction.Nenhuma;
        public HitReaction Current { get; private set; }

        /// <summary>Quantas reações já aconteceram (para os testes).</summary>
        public int Count { get; private set; }

        /// <summary>O que este inimigo aceita: devolve a reação possível, no máximo a pedida.</summary>
        public static HitReaction Allowed(GameObject enemy, HitReaction wanted)
        {
            if (wanted == HitReaction.Nenhuma) return HitReaction.Nenhuma;
            var lulluAI = enemy.GetComponentInParent<LulluAI>();
            if (lulluAI != null)
            {
                // O Primeiro Servo (a luta fica por cima da LulluAI dele) é chefe: nunca reage.
                if (lulluAI.GetComponent<FirstServantFight>() != null) return HitReaction.Nenhuma;
                return HitReaction.Cambaleio; // híbrido: cambaleia, não cai
            }
            if (enemy.GetComponentInParent<SoldierAI>() != null || enemy.GetComponentInParent<SniperAI>() != null) return wanted;
            return HitReaction.Nenhuma;
        }

        /// <summary>Aplica a reação ao inimigo, se ele aceitar. Devolve o que foi feito de verdade.</summary>
        public static HitReaction Apply(Health target, HitReaction wanted, float duration, Vector3 fromPosition)
        {
            if (target == null || target.IsDead) return HitReaction.Nenhuma;
            HitReaction kind = Allowed(target.gameObject, wanted);
            if (kind == HitReaction.Nenhuma) return HitReaction.Nenhuma;
            var reactor = target.GetComponent<HitReactor>();
            if (reactor == null) reactor = target.gameObject.AddComponent<HitReactor>();
            return reactor.Begin(kind, duration, fromPosition);
        }

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            soldier = GetComponent<SoldierAI>();
            sniper = GetComponent<SniperAI>();
            lullu = GetComponent<LulluAI>();
            Animator animator = GetComponentInChildren<Animator>();
            model = animator != null ? animator.transform : transform;
            modelRest = model.localRotation;
        }

        void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            if (health != null) health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (Busy) Finish(false);
        }

        void OnDied()
        {
            if (Busy) Finish(false);
        }

        HitReaction Begin(HitReaction kind, float duration, Vector3 fromPosition)
        {
            // Uma queda não vira cambaleio por cima, e um cambaleio em curso não é renovado.
            if (Current == HitReaction.Queda) return HitReaction.Nenhuma;
            if (Current == HitReaction.Cambaleio && kind == HitReaction.Cambaleio) return HitReaction.Nenhuma;

            if (Current == HitReaction.Cambaleio) model.localRotation = modelRest;
            Current = kind;
            Count++;
            startTime = Time.time;
            endTime = startTime + Mathf.Max(0.1f, duration);
            pushDirection = transform.position - fromPosition;
            pushDirection.y = 0f;
            pushDirection = pushDirection.sqrMagnitude > 0.001f ? pushDirection.normalized : -transform.forward;
            pushed = 0f;
            pushTotal = kind == HitReaction.Queda ? FallPush : StaggerPush;
            SetLock(true);
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return kind;
        }

        void Update()
        {
            if (!Busy) return;
            if (health != null && health.IsDead)
            {
                Finish(false);
                return;
            }

            float now = Time.time;
            float t = now - startTime;

            // Empurrão para trás, no começo da reação.
            if (pushed < pushTotal && agent != null && agent.enabled)
            {
                float step = Mathf.Min(pushTotal - pushed, pushTotal * Time.deltaTime / 0.25f);
                agent.Move(pushDirection * step);
                pushed += step;
            }

            if (Current == HitReaction.Cambaleio)
            {
                float lean = Mathf.Sin(Mathf.Clamp01(t / (endTime - startTime)) * Mathf.PI) * StaggerLean;
                model.localRotation = modelRest * Quaternion.Euler(-lean, 0f, 0f);
            }
            else
            {
                float total = endTime - startTime;
                float angle;
                if (t < FallTime) angle = FallAngle * (t / FallTime);
                else if (t > total - RiseTime) angle = FallAngle * Mathf.Clamp01((endTime - now) / RiseTime);
                else angle = FallAngle;
                model.localRotation = modelRest * Quaternion.Euler(-angle, 0f, 0f);
            }

            if (now >= endTime) Finish(true);
        }

        void Finish(bool resume)
        {
            Current = HitReaction.Nenhuma;
            // Com a morte, quem cai é a animação de morte: o modelo volta ao normal antes.
            if (model != null) model.localRotation = modelRest;
            SetLock(false);
            if (resume && agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        }

        void SetLock(bool locked)
        {
            if (soldier != null) soldier.ReactionLock = locked;
            if (sniper != null) sniper.ReactionLock = locked;
            if (lullu != null) lullu.ReactionLock = locked;
        }
    }
}
