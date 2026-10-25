using Nibiru.Combat;
using Nibiru.Stealth;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Atirador de elite da Fundação, num posto fixo (no alto do zigurate). Não sai do lugar.
    /// Vigia varrendo o pátio com a mira laser vermelha, que mostra para onde ele olha.
    /// Desconfiado, o laser vai até o vulto ou o som. Ao perceber a Elena, avisa pelo rádio (os
    /// soldados por perto vão até ela), trava o laser nela e só atira depois do tempo de aviso.
    /// Perdendo a Elena de vista, mira onde a viu por último e depois volta a vigiar.
    /// Como os outros soldados, cai de uma vez com a faca pelas costas se estiver distraído.
    /// Os valores ficam no arquivo <see cref="SniperSettings"/>.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(SoldierPerception))]
    public class SniperAI : MonoBehaviour, ISoldierBrain, ITakedownTarget
    {
        [SerializeField] SniperSettings settings;

        [Tooltip("Ponta do cano: de onde saem o laser e o tiro.")]
        [SerializeField] Transform muzzle;

        [Header("Laser e tiro")]
        [Tooltip("Linha da mira laser (do cano até onde ela bate).")]
        [SerializeField] LineRenderer laser;

        [Tooltip("Ponto vermelho onde o laser bate (opcional).")]
        [SerializeField] Transform laserDot;

        [Tooltip("Largura do laser vigiando e travado na Elena.")]
        [SerializeField] Vector2 laserWidth = new Vector2(0.025f, 0.05f);

        [Tooltip("Rastro do tiro (opcional).")]
        [SerializeField] LineRenderer tracer;

        [Header("Som")]
        [SerializeField] AudioSource audioSource;
        [Tooltip("Bipe quando o laser trava na Elena.")]
        [SerializeField] AudioClip warningSound;
        [SerializeField] AudioClip shotSound;

        const float SuspicionThreshold = 0.3f;
        const float LookAroundTime = 3f;
        const float TracerTime = 0.08f;
        const float AimTurnSpeed = 120f;
        const float LaserMaxDistance = 120f;

        Health health;
        SoldierPerception perception;
        Health playerHealth;

        float centerYaw;
        float sweepOffset;
        float sweepDirection = 1f;
        Vector3 aimPoint;
        Vector3 investigatePoint;
        float stateEndTime;
        float lastSeenTime;
        float lockedAt = -1f;
        float nextShotTime;
        float tracerHideTime;
        int shotsLeft;
        float reloadEndTime = -1f;

        public SoldierAI.State CurrentState { get; private set; }
        public float Awareness => perception.Awareness;
        public bool IsActive => enabled;
        public bool IsAiming => enabled && CurrentState != SoldierAI.State.Morto && !IsReloading;
        public bool IsReloading => Time.time < reloadEndTime;
        public float ReloadTime => settings.reloadTime;

        /// <summary>True enquanto o laser está travado na Elena (contando o tempo de aviso ou atirando).</summary>
        public bool IsLockedOn => lockedAt >= 0f;

        public bool CanBeTakenDown => enabled && CurrentState != SoldierAI.State.Morto && CurrentState != SoldierAI.State.Combate
            && perception.SecondsSinceSawPlayer >= settings.takedownLockout;

        public event System.Action Fired;
        public event System.Action ReloadStarted;

        void Awake()
        {
            health = GetComponent<Health>();
            perception = GetComponent<SoldierPerception>();
            centerYaw = transform.eulerAngles.y;
            shotsLeft = settings.magazineSize;
            if (tracer != null) tracer.enabled = false;
            aimPoint = SweepPoint(0f);
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            SetLaser(CurrentState != SoldierAI.State.Morto);
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            SetLaser(false);
        }

        void Start()
        {
            if (perception.Player != null) playerHealth = perception.Player.GetComponent<Health>();
        }

        void Update()
        {
            if (CurrentState == SoldierAI.State.Morto) return;
            if (tracer != null && tracer.enabled && Time.time >= tracerHideTime) tracer.enabled = false;

            if (CurrentState != SoldierAI.State.Combate && perception.PlayerAlive && perception.Awareness >= 1f)
                EnterCombat();

            switch (CurrentState)
            {
                case SoldierAI.State.Patrulha: UpdateWatch(); break;
                case SoldierAI.State.Desconfiado: UpdateSuspicious(); break;
                case SoldierAI.State.Combate: UpdateCombat(); break;
                case SoldierAI.State.Busca: UpdateSearch(); break;
            }

            TurnTowards(aimPoint);
        }

        void LateUpdate()
        {
            if (CurrentState != SoldierAI.State.Morto) DrawLaser();
        }

        // ---------- Estados ----------

        // Vigiando: o laser varre o pátio de um lado para o outro, sempre na mesma velocidade.
        void UpdateWatch()
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

            float half = settings.sweepAngle * 0.5f;
            sweepOffset += sweepDirection * settings.sweepSpeed * Time.deltaTime;
            if (sweepOffset > half) { sweepOffset = half; sweepDirection = -1f; }
            else if (sweepOffset < -half) { sweepOffset = -half; sweepDirection = 1f; }
            aimPoint = SweepPoint(sweepOffset);
        }

        void EnterWatch()
        {
            CurrentState = SoldierAI.State.Patrulha;
            lockedAt = -1f;
            // Recomeça a varredura de onde o laser está, sem pular.
            float yaw = Mathf.DeltaAngle(centerYaw, Quaternion.LookRotation(Flat(aimPoint - transform.position)).eulerAngles.y);
            sweepOffset = Mathf.Clamp(yaw, -settings.sweepAngle * 0.5f, settings.sweepAngle * 0.5f);
        }

        void EnterSuspicious(Vector3 point)
        {
            CurrentState = SoldierAI.State.Desconfiado;
            investigatePoint = point;
            stateEndTime = Time.time + LookAroundTime;
        }

        // Desconfiado: o laser vai até o vulto (ou o som) e fica ali olhando.
        void UpdateSuspicious()
        {
            if (perception.ConsumeNoise(out Vector3 noise)) EnterSuspicious(noise);
            else if (perception.CanSeePlayer)
            {
                investigatePoint = perception.LastKnownPlayerPosition;
                stateEndTime = Time.time + LookAroundTime;
            }

            aimPoint = Vector3.MoveTowards(aimPoint, investigatePoint, settings.sweepSpeed * 0.5f * Time.deltaTime);
            if (Time.time >= stateEndTime && perception.Awareness < SuspicionThreshold) EnterWatch();
        }

        void EnterCombat()
        {
            CurrentState = SoldierAI.State.Combate;
            lastSeenTime = Time.time;
            lockedAt = -1f;
            if (!string.IsNullOrEmpty(settings.radioMessage)) Notifications.Post(settings.radioMessage);

            // O rádio chama os soldados por perto até onde a Elena está.
            Vector3 position = perception.LastKnownPlayerPosition;
            foreach (SoldierAI soldier in SoldierAI.All)
                if (Vector3.Distance(soldier.transform.position, position) <= settings.alertRadius)
                    soldier.Alert(position);
        }

        void UpdateCombat()
        {
            if (!perception.PlayerAlive)
            {
                EnterWatch();
                return;
            }

            if (!perception.CanSeePlayer)
            {
                // Perdeu de vista: o laser fica onde ela estava e o aviso recomeça quando ela aparecer.
                lockedAt = -1f;
                aimPoint = Vector3.MoveTowards(aimPoint, perception.LastKnownPlayerPosition, AimTurnSpeed * Time.deltaTime);
                if (Time.time - lastSeenTime > settings.loseSightTime) EnterSearch();
                return;
            }

            lastSeenTime = Time.time;
            Vector3 target = perception.PlayerAimPoint();
            aimPoint = target;

            if (lockedAt < 0f)
            {
                lockedAt = Time.time;
                nextShotTime = Mathf.Max(nextShotTime, Time.time + settings.warningTime);
                Play(warningSound);
            }

            Vector3 from = muzzle != null ? muzzle.position : perception.EyePosition;
            float distance = Vector3.Distance(from, target);
            if (Time.time >= nextShotTime && !IsReloading && distance <= settings.attackRange && perception.HasLineOfSight(from, target))
                Shoot(target);
        }

        void EnterSearch()
        {
            CurrentState = SoldierAI.State.Busca;
            lockedAt = -1f;
            investigatePoint = perception.LastKnownPlayerPosition;
            stateEndTime = Time.time + settings.searchTime;
            perception.SetAwareness(0.6f);
        }

        // Procurando: o laser passeia em volta de onde ela foi vista por último.
        void UpdateSearch()
        {
            if (perception.ConsumeNoise(out Vector3 noise)) investigatePoint = noise;
            if (Time.time >= stateEndTime)
            {
                perception.SetAwareness(0.2f);
                EnterWatch();
                return;
            }

            float wobble = Mathf.Sin(Time.time * 0.9f) * 4f;
            Vector3 side = Vector3.Cross(Vector3.up, Flat(investigatePoint - transform.position).normalized);
            aimPoint = Vector3.MoveTowards(aimPoint, investigatePoint + side * wobble, settings.sweepSpeed * 0.5f * Time.deltaTime);
        }

        // ---------- Tiro ----------

        void Shoot(Vector3 target)
        {
            nextShotTime = Time.time + settings.fireInterval;
            bool hit = Random.value < settings.accuracy;
            if (hit && playerHealth != null)
                playerHealth.TakeDamage(new DamageInfo(settings.damage, target, gameObject));

            ShowTracer(hit ? target : target + Random.insideUnitSphere * 1.5f);
            Play(shotSound);
            Noise.Emit(transform.position, settings.shotNoiseRadius, gameObject);
            Fired?.Invoke();

            if (--shotsLeft > 0) return;
            shotsLeft = settings.magazineSize;
            reloadEndTime = Time.time + settings.reloadTime;
            nextShotTime = Mathf.Max(nextShotTime, reloadEndTime + 0.2f);
            ReloadStarted?.Invoke();
        }

        void ShowTracer(Vector3 end)
        {
            if (tracer == null) return;
            tracer.SetPosition(0, muzzle != null ? muzzle.position : perception.EyePosition);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerHideTime = Time.time + TracerTime;
        }

        void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        // ---------- Golpe, dano e morte ----------

        public void TakeDown(GameObject attacker) => SoldierAI.TakedownKill(health, attacker);

        void OnDamaged(DamageInfo info)
        {
            if (CurrentState == SoldierAI.State.Morto || health.IsDead) return;
            if (perception.Player != null && info.source == perception.Player.gameObject)
            {
                perception.ForceAlert(info.source.transform.position);
                if (CurrentState != SoldierAI.State.Combate) EnterCombat();
            }
        }

        void OnDied()
        {
            CurrentState = SoldierAI.State.Morto;
            lockedAt = -1f;
            perception.enabled = false;
            SetLaser(false);
            if (tracer != null) tracer.enabled = false;
        }

        // ---------- Mira e laser ----------

        // Ponto no chão do pátio para onde o laser aponta vigiando, com este desvio para o lado.
        Vector3 SweepPoint(float offset)
        {
            Vector3 eye = perception != null ? perception.EyePosition : transform.position;
            Vector3 direction = Quaternion.Euler(settings.sweepPitch, centerYaw + offset, 0f) * Vector3.forward;
            return Physics.Raycast(eye, direction, out RaycastHit hit, LaserMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform)
                ? hit.point
                : eye + direction * LaserMaxDistance * 0.5f;
        }

        // O corpo (e com ele o cone da visão) vira para onde o laser aponta.
        void TurnTowards(Vector3 point)
        {
            Vector3 flat = Flat(point - transform.position);
            if (flat.sqrMagnitude < 0.01f) return;
            float speed = CurrentState == SoldierAI.State.Combate ? AimTurnSpeed : settings.turnSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), speed * Time.deltaTime);
        }

        void DrawLaser()
        {
            if (laser == null) return;
            Vector3 from = muzzle != null ? muzzle.position : perception.EyePosition;
            Vector3 direction = aimPoint - from;
            Vector3 end = from + direction.normalized * LaserMaxDistance;

            RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, LaserMaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                end = hit.point;
            }

            laser.SetPosition(0, from);
            laser.SetPosition(1, end);
            float width = IsLockedOn ? laserWidth.y : laserWidth.x;
            laser.startWidth = width * 0.5f;
            laser.endWidth = width;
            if (laserDot != null)
            {
                laserDot.position = end;
                laserDot.localScale = Vector3.one * width * 4f;
            }
        }

        void SetLaser(bool on)
        {
            if (laser != null) laser.enabled = on;
            if (laserDot != null) laserDot.gameObject.SetActive(on);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void OnDrawGizmosSelected()
        {
            if (settings == null) return;
            Vector3 eye = transform.position + Vector3.up * settings.eyeHeight;
            float yaw = Application.isPlaying ? centerYaw : transform.eulerAngles.y;
            float half = settings.sweepAngle * 0.5f + settings.viewAngle * 0.5f;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawLine(eye, eye + Quaternion.Euler(0f, yaw - half, 0f) * Vector3.forward * settings.viewDistance);
            Gizmos.DrawLine(eye, eye + Quaternion.Euler(0f, yaw + half, 0f) * Vector3.forward * settings.viewDistance);
        }
    }
}
