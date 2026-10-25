using System.Collections.Generic;
using Nibiru.Combat;
using Nibiru.Stealth;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.AI
{
    /// <summary>
    /// Soldado da Fundação. Patrulha entre pontos (ou fica de guarda), desconfia e investiga
    /// sons e vultos, entra em combate quando percebe a Elena, procura por ela quando a perde
    /// de vista e volta à patrulha. O líder chama reforços pelo rádio se o combate durar.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(SoldierPerception))]
    public class SoldierAI : MonoBehaviour
    {
        public enum State
        {
            Patrulha,
            Desconfiado,
            Combate,
            Busca,
            Morto,
        }

        [SerializeField] SoldierSettings settings;

        [Tooltip("Pontos da patrulha, em ordem. Vazio = fica parado de guarda onde começou.")]
        [SerializeField] Transform[] waypoints;

        [Tooltip("Líder: chama reforços pelo rádio depois de alguns segundos de combate.")]
        [SerializeField] bool isLeader;

        [Header("Visual do tiro")]
        [SerializeField] Transform muzzle;
        [SerializeField] LineRenderer tracer;

        // Desconfiança a partir da qual um vulto já faz o soldado ir olhar.
        const float SuspicionThreshold = 0.3f;
        const float LookAroundTime = 3f;
        const float SearchPointRadius = 6f;
        const float TracerTime = 0.06f;

        static readonly List<SoldierAI> all = new List<SoldierAI>();

        NavMeshAgent agent;
        Health health;
        SoldierPerception perception;
        Health playerHealth;

        int waypointIndex;
        float waitUntil = -1f;
        Vector3 guardPosition;
        Quaternion guardRotation;
        Vector3 investigatePoint;
        float lookAroundUntil = -1f;
        float stateEndTime;
        float lastSeenTime;
        float nextShotTime;
        float reinforcementAt;
        bool reinforcementsCalled;
        float tracerHideTime;
        int shotsLeft;
        float reloadEndTime = -1f;

        /// <summary>Soldados com a IA ligada agora (para avisos de fora, como o helicóptero).</summary>
        public static IReadOnlyList<SoldierAI> All => all;

        public State CurrentState { get; private set; }
        public bool IsLeader => isLeader;
        public float Awareness => perception.Awareness;

        /// <summary>Em combate, vendo a Elena e sem recarregar: o fuzil fica apontado para ela.</summary>
        public bool IsAiming => enabled && CurrentState == State.Combate && perception.CanSeePlayer && !IsReloading;
        public bool IsReloading => Time.time < reloadEndTime;
        public float ReloadTime => settings.reloadTime;

        /// <summary>A cada tiro (para a animação do disparo).</summary>
        public event System.Action Fired;
        /// <summary>Quando o carregador acaba e ele começa a recarregar.</summary>
        public event System.Action ReloadStarted;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            perception = GetComponent<SoldierPerception>();
            agent.updateRotation = false;
            guardPosition = transform.position;
            guardRotation = transform.rotation;
            if (tracer != null) tracer.enabled = false;
            // O primeiro carregador começa pela metade ou mais, para os soldados não recarregarem juntos.
            shotsLeft = Random.Range((settings.magazineSize + 1) / 2, settings.magazineSize + 1);
        }

        void OnEnable()
        {
            all.Add(this);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            all.Remove(this);
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Start()
        {
            if (perception.Player != null) playerHealth = perception.Player.GetComponent<Health>();
            EnterPatrol();
        }

        void Update()
        {
            if (CurrentState == State.Morto) return;
            if (tracer != null && tracer.enabled && Time.time >= tracerHideTime) tracer.enabled = false;

            if (CurrentState != State.Combate && perception.PlayerAlive && perception.Awareness >= 1f)
                EnterCombat();

            switch (CurrentState)
            {
                case State.Patrulha: UpdatePatrol(); break;
                case State.Desconfiado: UpdateSuspicious(); break;
                case State.Combate: UpdateCombat(); break;
                case State.Busca: UpdateSearch(); break;
            }

            if (CurrentState != State.Combate || !perception.CanSeePlayer) FaceMovement();
        }

        // ---------- Estados ----------

        void EnterPatrol()
        {
            CurrentState = State.Patrulha;
            agent.isStopped = false;
            agent.speed = settings.patrolSpeed;
            waitUntil = -1f;
            agent.SetDestination(HasWaypoints ? waypoints[waypointIndex].position : guardPosition);
        }

        bool HasWaypoints => waypoints != null && waypoints.Length > 0;

        void UpdatePatrol()
        {
            if (perception.ConsumeNoise(out Vector3 noise))
            {
                EnterSuspicious(noise);
                return;
            }
            if (perception.CanSeePlayer && perception.Awareness >= SuspicionThreshold)
            {
                EnterSuspicious(perception.LastKnownPlayerPosition);
                return;
            }
            if (!Arrived()) return;

            if (!HasWaypoints)
            {
                // De guarda: volta a olhar para onde olhava.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, guardRotation, settings.turnSpeed * Time.deltaTime);
                return;
            }

            if (waitUntil < 0f) waitUntil = Time.time + settings.waitAtWaypoint;
            if (Time.time < waitUntil) return;

            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waitUntil = -1f;
            agent.SetDestination(waypoints[waypointIndex].position);
        }

        void EnterSuspicious(Vector3 point)
        {
            CurrentState = State.Desconfiado;
            agent.isStopped = false;
            agent.speed = settings.patrolSpeed;
            investigatePoint = point;
            lookAroundUntil = -1f;
            agent.SetDestination(point);
        }

        void UpdateSuspicious()
        {
            if (perception.ConsumeNoise(out Vector3 noise)) EnterSuspicious(noise);
            else if (perception.CanSeePlayer && perception.LastKnownPlayerPosition != investigatePoint)
            {
                investigatePoint = perception.LastKnownPlayerPosition;
                agent.SetDestination(investigatePoint);
            }

            if (!Arrived()) return;

            // Chegou: olha em volta por alguns segundos.
            if (lookAroundUntil < 0f) lookAroundUntil = Time.time + LookAroundTime;
            transform.Rotate(0f, settings.turnSpeed * 0.25f * Time.deltaTime, 0f);
            if (Time.time >= lookAroundUntil && perception.Awareness < SuspicionThreshold) EnterPatrol();
        }

        void EnterCombat()
        {
            CurrentState = State.Combate;
            agent.isStopped = false;
            agent.speed = settings.chaseSpeed;
            lastSeenTime = Time.time;
            nextShotTime = Time.time + settings.fireInterval * 0.5f;
            if (isLeader && !reinforcementsCalled) reinforcementAt = Time.time + settings.reinforcementDelay;
        }

        void UpdateCombat()
        {
            if (!perception.PlayerAlive)
            {
                EnterPatrol();
                return;
            }

            if (isLeader && !reinforcementsCalled && Time.time >= reinforcementAt) CallReinforcements();

            if (!perception.CanSeePlayer)
            {
                agent.isStopped = false;
                agent.SetDestination(perception.LastKnownPlayerPosition);
                if (Time.time - lastSeenTime > settings.loseSightTime) EnterSearch();
                return;
            }

            lastSeenTime = Time.time;
            Vector3 playerPosition = perception.Player.position;
            Vector3 target = perception.PlayerAimPoint();
            float distance = Vector3.Distance(Flat(transform.position), Flat(playerPosition));
            Face(playerPosition - transform.position);

            Vector3 muzzlePosition = muzzle != null ? muzzle.position : perception.EyePosition;
            bool clearShot = perception.HasLineOfSight(muzzlePosition, target);

            // Longe ou sem linha de tiro: aproxima. Perto o bastante: para e atira.
            bool approach = distance > settings.preferredDistance || !clearShot;
            agent.isStopped = !approach;
            if (approach) agent.SetDestination(playerPosition);

            if (clearShot && distance <= settings.attackRange && Time.time >= nextShotTime && !IsReloading)
                Shoot(target, distance);
        }

        void EnterSearch()
        {
            CurrentState = State.Busca;
            agent.isStopped = false;
            agent.speed = settings.chaseSpeed * 0.6f;
            stateEndTime = Time.time + settings.searchTime;
            investigatePoint = perception.LastKnownPlayerPosition;
            agent.SetDestination(investigatePoint);
            perception.SetAwareness(0.6f);
        }

        void UpdateSearch()
        {
            if (perception.ConsumeNoise(out Vector3 noise))
            {
                investigatePoint = noise;
                agent.SetDestination(noise);
            }

            if (Time.time >= stateEndTime)
            {
                perception.SetAwareness(0.2f);
                EnterPatrol();
                return;
            }

            // Chegou ao ponto: escolhe outro ponto por perto para olhar.
            if (Arrived() && RandomPointNear(investigatePoint, SearchPointRadius, out Vector3 next))
                agent.SetDestination(next);
        }

        // ---------- Combate ----------

        void Shoot(Vector3 target, float distance)
        {
            nextShotTime = Time.time + settings.fireInterval * Random.Range(0.85f, 1.15f);

            // Mais longe acerta menos; a esquiva da Elena ignora o tiro pela invulnerabilidade.
            float falloff = Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(settings.preferredDistance, settings.attackRange, distance));
            bool hit = Random.value < settings.accuracy * falloff;
            Vector3 end = hit ? target : target + Random.insideUnitSphere * 1.2f;

            if (hit && playerHealth != null)
                playerHealth.TakeDamage(new DamageInfo(settings.damage, target, gameObject));

            ShowTracer(end);
            Noise.Emit(transform.position, settings.shotNoiseRadius, gameObject);
            Fired?.Invoke();

            // Carregador vazio: recarrega antes do próximo tiro (uma brecha para a Elena).
            if (--shotsLeft > 0) return;
            shotsLeft = settings.magazineSize;
            reloadEndTime = Time.time + settings.reloadTime;
            nextShotTime = Mathf.Max(nextShotTime, reloadEndTime + 0.2f);
            ReloadStarted?.Invoke();
        }

        void CallReinforcements()
        {
            reinforcementsCalled = true;
            Notifications.Post("Rádio: \"Contato! Reforços para minha posição!\"");
            foreach (SoldierAI other in all)
            {
                if (other == this || other.CurrentState == State.Morto) continue;
                if (Vector3.Distance(other.transform.position, transform.position) > settings.reinforcementRadius) continue;
                other.Alert(perception.LastKnownPlayerPosition);
            }
        }

        /// <summary>Coloca o soldado em combate sabendo onde a Elena está (chamado de reforço).</summary>
        public void Alert(Vector3 playerPosition)
        {
            if (CurrentState == State.Morto) return;
            perception.ForceAlert(playerPosition);
            if (CurrentState != State.Combate) EnterCombat();
        }

        void OnDamaged(DamageInfo info)
        {
            if (CurrentState == State.Morto || health.IsDead) return;
            // Atacado pela Elena: sabe de onde veio e entra em combate.
            // Dano do ambiente (fogo, por exemplo) não denuncia ninguém.
            if (perception.Player != null && info.source == perception.Player.gameObject)
                Alert(info.source.transform.position);
        }

        void OnDied()
        {
            CurrentState = State.Morto;
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            perception.enabled = false;
            if (tracer != null) tracer.enabled = false;

            // Com modelo animado, quem cai é a animação de morte; o colisor em pé sai do caminho.
            if (GetComponentInChildren<SoldierAnimator>() != null)
            {
                foreach (Collider body in GetComponents<Collider>()) body.enabled = false;
                return;
            }
            // Sem modelo (a cápsula antiga): só tomba.
            transform.SetPositionAndRotation(
                transform.position + Vector3.down * 0.6f,
                transform.rotation * Quaternion.Euler(-85f, 0f, 0f));
        }

        // ---------- Ajudas ----------

        bool Arrived() =>
            !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f;

        void FaceMovement()
        {
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.05f) Face(velocity);
        }

        void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), settings.turnSpeed * Time.deltaTime);
        }

        void ShowTracer(Vector3 end)
        {
            if (tracer == null || muzzle == null) return;
            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerHideTime = Time.time + TracerTime;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static bool RandomPointNear(Vector3 center, float radius, out Vector3 point)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            bool found = NavMesh.SamplePosition(center + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, radius, NavMesh.AllAreas);
            point = found ? hit.position : center;
            return found;
        }

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Vector3 eye = transform.position + Vector3.up * settings.eyeHeight;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Quaternion left = Quaternion.Euler(0f, -settings.viewAngle * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, settings.viewAngle * 0.5f, 0f);
            Gizmos.DrawLine(eye, eye + left * transform.forward * settings.viewDistance);
            Gizmos.DrawLine(eye, eye + right * transform.forward * settings.viewDistance);
        }
    }
}
