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
    /// de longe e só leva dano cheio quando atordoado. Um estrondo que não é da Elena (um
    /// holofote caindo) o distrai por uns segundos. O chefe muda os ajustes de cima a cada fase
    /// (FirstServantFight), sem mexer no arquivo de dados.
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
            Fugindo,
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
        // A investida só sai no mesmo piso: de baixo de um andaime, ou de cima de uma rampa, não.
        const float LungeMaxRise = 0.75f;
        // Preparação das investidas seguidas, em fração da primeira.
        const float ChainWindup = 0.5f;
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
        int lungesLeft;
        float distractedUntil = -999f;
        bool hitDone;
        bool dashing;
        float dashEnd;
        Vector3 lungeDirection;
        Vector3 bodyScale = Vector3.one;
        Quaternion bodyRotation = Quaternion.identity;

        public State CurrentState { get; private set; }

        // Ajustes em tempo de jogo (o chefe muda a cada fase), sem mexer no arquivo de dados.
        /// <summary>Multiplica a preparação do golpe e da investida (menos que 1 = mais rápido).</summary>
        public float WindupScale { get; set; } = 1f;
        /// <summary>Multiplica o tempo parado depois do golpe e da investida.</summary>
        public float RecoveryScale { get; set; } = 1f;
        /// <summary>Multiplica as velocidades de andar e caçar.</summary>
        public float SpeedScale { get; set; } = 1f;
        /// <summary>Quantas investidas seguidas ele faz de uma vez (1 = uma só).</summary>
        public int LungeChain { get; set; } = 1;
        /// <summary>O mesmo "Max Climb" do Inspector: 0 = sobe rampas e andaimes.</summary>
        public float MaxClimb { get => maxClimb; set => maxClimb = value; }

        /// <summary>Ouvindo um estrondo: vai até lá e não ouve os passos da Elena.</summary>
        public bool IsDistracted => Time.time < distractedUntil;

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

        // Se uma cena já o mandou caçar (ou fugir) antes do primeiro quadro, não volta a vagar.
        void Start()
        {
            if (CurrentState == State.Vagando) Enter(State.Vagando);
        }

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
                case State.Fugindo: UpdateFlee(); break;
            }
        }

        // ---------- Estados ----------

        void Enter(State state)
        {
            CurrentState = state;
            bool moving = state == State.Vagando || state == State.Investigando || state == State.Cacando || state == State.Fugindo;
            if (agent.isOnNavMesh) agent.isStopped = !moving;
            agent.speed = SpeedScale * (state == State.Cacando || state == State.Fugindo ? settings.huntSpeed
                : state == State.Investigando ? settings.investigateSpeed
                : settings.wanderSpeed);
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
            if (CurrentState != State.Investigando) return;
            if (IsDistracted)
            {
                // Distraído: vai até o estrondo e fica olhando para lá (de costas para quem estiver longe).
                if (Arrived()) Face(targetPoint - transform.position);
                else FaceMovement();
                return;
            }
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
                stateEnd = Time.time + settings.attackWindup * WindupScale;
                hitDone = false;
                return true;
            }

            if (CanLungeAt(distance) && Time.time >= nextLungeTime)
            {
                lungesLeft = Mathf.Max(1, LungeChain) - 1;
                StartLunge(settings.lungeWindup * WindupScale);
                return true;
            }
            return false;
        }

        bool CanLungeAt(float distance) =>
            settings.canLunge && PlayerAlive
            && distance >= settings.lungeRange.x && distance <= settings.lungeRange.y
            && Mathf.Abs(player.position.y - transform.position.y) <= LungeMaxRise
            && !NavMesh.Raycast(transform.position, player.position, out _, NavMesh.AllAreas);

        void StartLunge(float windup)
        {
            Enter(State.Investida);
            stateEnd = Time.time + windup;
            dashing = false;
            hitDone = false;
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
            stateEnd = Time.time + settings.attackRecovery * RecoveryScale;
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
            // Investidas seguidas (o chefe nas fases finais): outra logo em seguida, com preparação curta.
            if (lungesLeft > 0 && !IsDistracted && CanLungeAt(PlayerDistance()))
            {
                lungesLeft--;
                StartLunge(settings.lungeWindup * ChainWindup * WindupScale);
                return;
            }
            lungesLeft = 0;
            nextLungeTime = Time.time + settings.lungeCooldown;
            Enter(State.Recuperando);
            stateEnd = Time.time + settings.lungeRecovery * RecoveryScale;
        }

        void UpdateRecover()
        {
            if (Time.time < stateEnd) return;
            Enter(IsDistracted ? State.Investigando : HeardRecently(settings.forgetTime) ? State.Cacando : State.Vagando);
        }

        void UpdateStunned()
        {
            if (Time.time < stateEnd) return;
            // Acorda sabendo onde a Elena está.
            if (PlayerAlive) Hear(player.position);
            Enter(State.Cacando);
        }

        // Corre até o ponto de fuga e some (não volta).
        void UpdateFlee()
        {
            FaceMovement();
            if (Arrived()) gameObject.SetActive(false);
        }

        // ---------- Sentidos ----------

        void OnNoise(NoiseEvent noise)
        {
            if (CurrentState == State.Morto || CurrentState == State.Atordoado || CurrentState == State.Fugindo) return;
            if (Vector3.Distance(transform.position, noise.position) > noise.radius * settings.hearingMultiplier) return;

            bool fromPlayer = player != null && noise.source == player.gameObject;
            if (fromPlayer && IsDistracted) return;
            if (!fromPlayer && settings.distractionNoise > 0f && noise.radius >= settings.distractionNoise)
            {
                // No meio de um golpe ou de uma investida, vai até o estrondo assim que terminar (UpdateRecover).
                Hear(noise.position);
                distractedUntil = Time.time + settings.distractionTime;
                if (CanChangeCourse()) Enter(State.Investigando);
                return;
            }
            Hear(noise.position);
            if (!CanChangeCourse()) return;
            Enter(fromPlayer ? State.Cacando : (CurrentState == State.Cacando ? State.Cacando : State.Investigando));
        }

        // Encostar nele (mesmo parada e em silêncio) denuncia a Elena.
        void SenseTouch()
        {
            if (!PlayerAlive || PlayerDistance() > settings.touchRange) return;
            distractedUntil = -999f;
            Hear(player.position);
            if (CurrentState != State.Cacando) Enter(State.Cacando);
        }

        void Hear(Vector3 point)
        {
            targetPoint = Constrain(point);
            lastHeardTime = Time.time;
        }

        // Com limite de subida, um som no alto (andaime, rampa) vira um ponto no chão logo abaixo. Debaixo
        // de uma plataforma maciça, o ponto da malha mais perto seria o topo dela: procura o chão em volta,
        // de perto para longe, só na altura em que ele anda.
        Vector3 Constrain(Vector3 point)
        {
            if (maxClimb <= 0f || point.y <= home.y + maxClimb) return point;
            point.y = home.y;
            // O chão em que ele pisa (a raiz fica acima dos pés, pelo "Base Offset" do agente).
            float ground = home.y - agent.baseOffset;
            for (float ring = 0f; ring <= 7.5f; ring += 1.5f)
            {
                int directions = ring == 0f ? 1 : 8;
                for (int i = 0; i < directions; i++)
                {
                    Vector3 probe = point + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * ring;
                    if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.2f, NavMesh.AllAreas) && hit.position.y <= ground + 0.5f)
                        return hit.position;
                }
            }
            return transform.position;
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
            distractedUntil = -999f;
            dashing = false;
            Enter(State.Vagando);
        }

        /// <summary>Vai direto atrás da Elena, sabendo onde ela está (ao acordar, ao fim de um urro).</summary>
        public void HuntPlayer()
        {
            if (CurrentState == State.Morto || CurrentState == State.Fugindo || !PlayerAlive) return;
            distractedUntil = -999f;
            Hear(player.position);
            Enter(State.Cacando);
        }

        /// <summary>
        /// Foge correndo até o ponto e some, sem ouvir mais nada (o Lullu, quando o Primeiro Servo acorda).
        /// </summary>
        public void FleeTo(Transform point)
        {
            if (CurrentState == State.Morto || point == null || !gameObject.activeInHierarchy) return;
            enabled = true;
            dashing = false;
            distractedUntil = -999f;
            targetPoint = point.position;
            Enter(State.Fugindo);
        }

        bool CanChangeCourse() =>
            CurrentState == State.Vagando || CurrentState == State.Investigando || CurrentState == State.Cacando;

        public void OnEcho(Vector3 origin)
        {
            if (CurrentState == State.Morto || CurrentState == State.Fugindo || !enabled) return;
            distractedUntil = -999f;
            dashing = false;
            Enter(State.Atordoado);
            stateEnd = Time.time + settings.stunDuration;
        }

        public float DamageMultiplier(DamageInfo info) =>
            CurrentState == State.Atordoado ? settings.stunnedDamageMultiplier : settings.damageMultiplier;

        void OnDamaged(DamageInfo info)
        {
            if (health.IsDead || player == null || info.source != player.gameObject) return;
            distractedUntil = -999f;
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
            // Bem acima ou abaixo dele (num andaime), a Elena está fora do alcance, mesmo bem em cima:
            // quem sobe rampas tem de subir até ela, e não golpear através da plataforma.
            if (Mathf.Abs(player.position.y - transform.position.y) > ReachHeight) return float.MaxValue;
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
