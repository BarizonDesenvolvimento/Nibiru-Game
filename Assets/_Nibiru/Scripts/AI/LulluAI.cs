using Nibiru.Combat;
using Nibiru.Powers;
using Nibiru.Stealth;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.AI
{
    /// <summary>
    /// Lullu, servo dos deuses: quase cego, caça pelo som. Vaga sem rumo, vai ver qualquer som
    /// e caça a Elena quando ouve os passos ou tiros dela (ou quando ela encosta nele).
    /// Antes de golpear o corpo encolhe: é a hora de esquivar. O Eco do Abzu o atordoa e
    /// abre a guarda. Com "investida" ligada (o chefe Primeiro Servo), ele também se lança
    /// de longe e só leva dano cheio quando atordoado.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class LulluAI : MonoBehaviour, IEchoReactive, IDamageModifier
    {
        public enum State
        {
            Vagando,
            Investigando,
            Cacando,
            Golpe,
            Investida,
            Recuperando,
            Atordoado,
            Morto,
        }

        [SerializeField] LulluSettings settings;

        [Tooltip("Corpo visual (filho), que encolhe ao preparar golpes e treme atordoado.")]
        [SerializeField] Transform body;

        [Tooltip("Quanto ele sobe acima de onde começou, em metros. Com valor, ele não sobe rampas nem andaimes e " +
                 "não alcança a Elena que está bem acima dele. 0 = sem limite.")]
        [SerializeField] float maxClimb;

        // Janela em que o som ainda "aponta" para a Elena: depois disso ele ataca às cegas só se encostar.
        const float KnownWindow = 2f;
        const float LungeHitRange = 1.1f;
        const float ReachHeight = 2.4f;
        const float TurnSpeed = 540f;
        const float PoseSpeed = 6f;

        NavMeshAgent agent;
        Health health;
        Transform player;
        Health playerHealth;

        Vector3 home;
        Vector3 targetPoint;
        float lastHeardTime = -999f;
        float stateEnd;
        float nextWanderTime;
        float nextLungeTime;
        bool hitDone;
        bool dashing;
        float dashEnd;
        Vector3 lungeDirection;
        Vector3 bodyScale = Vector3.one;
        Quaternion bodyRotation = Quaternion.identity;

        public State CurrentState { get; private set; }

        bool PlayerAlive => playerHealth != null && !playerHealth.IsDead;
        bool HeardRecently(float window) => Time.time - lastHeardTime <= window;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            agent.updateRotation = false;
            home = transform.position;
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null)
            {
                player = found.transform;
                playerHealth = found.GetComponent<Health>();
            }
            if (body != null)
            {
                bodyScale = body.localScale;
                bodyRotation = body.localRotation;
            }
        }

        void OnEnable()
        {
            Noise.Emitted += OnNoise;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            Noise.Emitted -= OnNoise;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Start() => Enter(State.Vagando);

        void Update()
        {
            if (CurrentState == State.Morto) return;
            UpdatePose();

            switch (CurrentState)
            {
                case State.Vagando: UpdateWander(); break;
                case State.Investigando: UpdateInvestigate(); break;
                case State.Cacando: UpdateHunt(); break;
                case State.Golpe: UpdateStrike(); break;
                case State.Investida: UpdateLunge(); break;
                case State.Recuperando: UpdateRecover(); break;
                case State.Atordoado: UpdateStunned(); break;
            }
        }

        // ---------- Estados ----------

        void Enter(State state)
        {
            CurrentState = state;
            bool moving = state == State.Vagando || state == State.Investigando || state == State.Cacando;
            if (agent.isOnNavMesh) agent.isStopped = !moving;
            agent.speed = state == State.Cacando ? settings.huntSpeed
                : state == State.Investigando ? settings.investigateSpeed
                : settings.wanderSpeed;
            if (moving && state != State.Vagando && agent.isOnNavMesh) agent.SetDestination(targetPoint);
        }

        void UpdateWander()
        {
            SenseTouch();
            FaceMovement();
            if (Time.time < nextWanderTime || !Arrived()) return;
            nextWanderTime = Time.time + Random.Range(2f, 4f);
            Vector2 offset = Random.insideUnitCircle * settings.wanderRadius;
            if (NavMesh.SamplePosition(home + new Vector3(offset.x, 0f, offset.y), out NavMeshHit hit, settings.wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        void UpdateInvestigate()
        {
            SenseTouch();
            FaceMovement();
            if (TryAttack()) return;
            if (Arrived() && !HeardRecently(settings.forgetTime)) Enter(State.Vagando);
        }

        void UpdateHunt()
        {
            SenseTouch();
            FaceMovement();
            if (TryAttack()) return;
            if (Vector3.Distance(Flat(agent.destination), Flat(targetPoint)) > 0.5f) agent.SetDestination(targetPoint);
            if (Arrived() && !HeardRecently(settings.forgetTime)) Enter(State.Vagando);
        }

        bool TryAttack()
        {
            if (!PlayerAlive || !HeardRecently(KnownWindow)) return false;
            float distance = PlayerDistance();

            if (distance <= settings.attackRange)
            {
                Enter(State.Golpe);
                stateEnd = Time.time + settings.attackWindup;
                hitDone = false;
                return true;
            }

            if (settings.canLunge && Time.time >= nextLungeTime
                && distance >= settings.lungeRange.x && distance <= settings.lungeRange.y
                && !NavMesh.Raycast(transform.position, player.position, out _, NavMesh.AllAreas))
            {
                Enter(State.Investida);
                stateEnd = Time.time + settings.lungeWindup;
                dashing = false;
                hitDone = false;
                return true;
            }
            return false;
        }

        void UpdateStrike()
        {
            if (PlayerAlive) Face(player.position - transform.position);
            if (Time.time < stateEnd || hitDone) return;

            hitDone = true;
            Vector3 center = transform.position + transform.forward * settings.attackRadius;
            if (PlayerAlive && Vector3.Distance(Flat(center), Flat(player.position)) <= settings.attackRadius + 0.5f)
                playerHealth.TakeDamage(new DamageInfo(settings.attackDamage, center, gameObject));

            Enter(State.Recuperando);
            stateEnd = Time.time + settings.attackRecovery;
        }

        void UpdateLunge()
        {
            if (!dashing)
            {
                // Preparação: encara a Elena; a direção trava quando ele se lança.
                if (PlayerAlive) Face(player.position - transform.position);
                if (Time.time < stateEnd) return;
                dashing = true;
                dashEnd = Time.time + settings.lungeDuration;
                lungeDirection = Flat(player.position - transform.position).normalized;
                return;
            }

            agent.Move(lungeDirection * settings.lungeSpeed * Time.deltaTime);
            if (!hitDone && PlayerAlive && PlayerDistance() <= LungeHitRange)
            {
                hitDone = true;
                playerHealth.TakeDamage(new DamageInfo(settings.lungeDamage, transform.position, gameObject));
            }

            if (Time.time < dashEnd) return;
            dashing = false;
            nextLungeTime = Time.time + settings.lungeCooldown;
            Enter(State.Recuperando);
            stateEnd = Time.time + settings.lungeRecovery;
        }

        void UpdateRecover()
        {
            if (Time.time < stateEnd) return;
            Enter(HeardRecently(settings.forgetTime) ? State.Cacando : State.Vagando);
        }

        void UpdateStunned()
        {
            if (Time.time < stateEnd) return;
            // Acorda sabendo onde a Elena está.
            if (PlayerAlive) Hear(player.position);
            Enter(State.Cacando);
        }

        // ---------- Sentidos ----------

        void OnNoise(NoiseEvent noise)
        {
            if (CurrentState == State.Morto || CurrentState == State.Atordoado) return;
            if (Vector3.Distance(transform.position, noise.position) > noise.radius * settings.hearingMultiplier) return;

            bool fromPlayer = player != null && noise.source == player.gameObject;
            Hear(noise.position);
            if (!CanChangeCourse()) return;
            Enter(fromPlayer ? State.Cacando : (CurrentState == State.Cacando ? State.Cacando : State.Investigando));
        }

        // Encostar nele (mesmo parada e em silêncio) denuncia a Elena.
        void SenseTouch()
        {
            if (!PlayerAlive || PlayerDistance() > settings.touchRange) return;
            Hear(player.position);
            if (CurrentState != State.Cacando) Enter(State.Cacando);
        }

        void Hear(Vector3 point)
        {
            targetPoint = Constrain(point);
            lastHeardTime = Time.time;
        }

        // Com limite de subida, um som no alto (andaime, rampa) vira um ponto no chão logo abaixo.
        Vector3 Constrain(Vector3 point)
        {
            if (maxClimb <= 0f || point.y <= home.y + maxClimb) return point;
            point.y = home.y;
            return NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : point;
        }

        /// <summary>
        /// Congela (ou solta) o Lullu, para uma cena em que nenhum inimigo pode agir. Ao soltar, ele
        /// esquece o que ouvia e volta a vagar.
        /// </summary>
        public void Pause(bool paused)
        {
            if (CurrentState == State.Morto) return;
            if (paused)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                enabled = false;
                return;
            }
            enabled = true;
            lastHeardTime = -999f;
            dashing = false;
            Enter(State.Vagando);
        }

        bool CanChangeCourse() =>
            CurrentState == State.Vagando || CurrentState == State.Investigando || CurrentState == State.Cacando;

        public void OnEcho(Vector3 origin)
        {
            if (CurrentState == State.Morto) return;
            dashing = false;
            Enter(State.Atordoado);
            stateEnd = Time.time + settings.stunDuration;
        }

        public float DamageMultiplier(DamageInfo info) =>
            CurrentState == State.Atordoado ? settings.stunnedDamageMultiplier : settings.damageMultiplier;

        void OnDamaged(DamageInfo info)
        {
            if (health.IsDead || player == null || info.source != player.gameObject) return;
            Hear(player.position);
            if (CanChangeCourse()) Enter(State.Cacando);
        }

        void OnDied()
        {
            CurrentState = State.Morto;
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            // Por enquanto só tomba; com modelo e animações entra a animação de morte.
            transform.SetPositionAndRotation(
                transform.position + Vector3.down * 0.6f,
                transform.rotation * Quaternion.Euler(-85f, 0f, 0f));
        }

        // ---------- Visual e ajudas ----------

        // Encolhe ao preparar, estica ao se lançar e inclina atordoado.
        void UpdatePose()
        {
            if (body == null) return;
            bool windup = (CurrentState == State.Golpe && !hitDone) || (CurrentState == State.Investida && !dashing);
            float scaleY = windup ? 0.8f : dashing ? 1.1f : CurrentState == State.Atordoado ? 0.9f : 1f;
            Quaternion tilt = CurrentState == State.Atordoado ? Quaternion.Euler(25f, 0f, 8f * Mathf.Sin(Time.time * 20f))
                : dashing ? Quaternion.Euler(20f, 0f, 0f)
                : Quaternion.identity;

            Vector3 targetScale = new Vector3(bodyScale.x, bodyScale.y * scaleY, bodyScale.z);
            body.localScale = Vector3.Lerp(body.localScale, targetScale, PoseSpeed * Time.deltaTime);
            body.localRotation = Quaternion.Slerp(body.localRotation, bodyRotation * tilt, PoseSpeed * Time.deltaTime);
        }

        float PlayerDistance()
        {
            // Bem acima dele (num andaime), a Elena está fora do alcance, mesmo bem em cima.
            if (maxClimb > 0f && player.position.y - transform.position.y > ReachHeight) return float.MaxValue;
            return Vector3.Distance(Flat(transform.position), Flat(player.position));
        }

        bool Arrived() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;

        void FaceMovement()
        {
            Vector3 velocity = Flat(agent.velocity);
            if (velocity.sqrMagnitude > 0.05f) Face(velocity);
        }

        void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), TurnSpeed * Time.deltaTime);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
