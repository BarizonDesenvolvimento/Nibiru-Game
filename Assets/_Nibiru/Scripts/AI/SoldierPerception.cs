using Nibiru.Combat;
using Nibiru.Environment;
using Nibiru.Player;
using Nibiru.Stealth;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Olhos e ouvidos do soldado. Ver a Elena enche a "desconfiança" (de 0 a 1): mais rápido
    /// de perto, mais devagar se ela estiver agachada. Em 1, ele a percebeu de vez.
    /// Sons ouvidos viram um ponto para investigar.
    /// </summary>
    public class SoldierPerception : MonoBehaviour
    {
        [SerializeField] SoldierSettings settings;

        [Tooltip("Camadas que bloqueiam a visão.")]
        [SerializeField] LayerMask visionBlockers = Physics.DefaultRaycastLayers;

        // Sons a partir deste alcance (tiros, por exemplo) deixam o soldado bem mais desconfiado.
        const float LoudNoiseRadius = 15f;

        Transform player;
        PlayerMotor playerMotor;
        Health playerHealth;
        bool hasNoise;
        Vector3 noisePosition;
        float lastSawPlayerTime = float.NegativeInfinity;

        /// <summary>De 0 (tranquilo) a 1 (percebeu a Elena).</summary>
        public float Awareness { get; private set; }
        public bool CanSeePlayer { get; private set; }
        public Vector3 LastKnownPlayerPosition { get; private set; }

        /// <summary>Segundos desde a última vez que ele teve a Elena à vista (infinito se nunca).</summary>
        public float SecondsSinceSawPlayer => Time.time - lastSawPlayerTime;
        public Transform Player => player;
        public bool PlayerAlive => playerHealth != null && !playerHealth.IsDead;

        void Awake()
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found == null) return;
            player = found.transform;
            playerMotor = found.GetComponent<PlayerMotor>();
            playerHealth = found.GetComponent<Health>();
        }

        void OnEnable() => Noise.Emitted += OnNoise;

        void OnDisable() => Noise.Emitted -= OnNoise;

        void Update()
        {
            if (player == null) return;

            float visibility = 0f;
            CanSeePlayer = PlayerAlive && CheckVision(out visibility);
            if (CanSeePlayer)
            {
                lastSawPlayerTime = Time.time;
                LastKnownPlayerPosition = player.position;
                Awareness = Mathf.Min(1f, Awareness + visibility * Time.deltaTime / settings.timeToSpot);
            }
            else
            {
                Awareness = Mathf.Max(0f, Awareness - settings.awarenessDecay * Time.deltaTime);
            }
        }

        /// <summary>Ponto onde a Elena seria vista: o peito, mais baixo se ela estiver agachada.</summary>
        public Vector3 PlayerAimPoint()
        {
            float crouch = playerMotor != null ? playerMotor.CrouchBlend : 0f;
            return player.position + Vector3.up * Mathf.Lerp(0.4f, -0.1f, crouch);
        }

        public Vector3 EyePosition => transform.position + Vector3.up * settings.eyeHeight;

        bool CheckVision(out float visibility)
        {
            visibility = 0f;
            Vector3 eye = EyePosition;
            Vector3 target = PlayerAimPoint();
            Vector3 toPlayer = target - eye;
            float distance = toPlayer.magnitude;
            if (distance > settings.viewDistance * Storm.ViewDistanceMultiplier) return false;

            Vector3 flat = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
            if (flat.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, flat) > settings.viewAngle * 0.5f) return false;

            if (!HasLineOfSight(eye, target)) return false;

            float closeness = distance <= settings.closeRange
                ? 3f
                : Mathf.Lerp(1.2f, 0.4f, Mathf.InverseLerp(settings.closeRange, settings.viewDistance, distance));
            bool crouched = playerMotor != null && playerMotor.IsCrouching;
            visibility = closeness * (crouched ? settings.crouchedVisibility : 1f);
            return true;
        }

        /// <summary>True se nada (fora o próprio soldado e a Elena) está entre os dois pontos.</summary>
        public bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, direction.magnitude, visionBlockers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                Transform t = hit.collider.transform;
                if (t.IsChildOf(transform) || (player != null && t.IsChildOf(player))) continue;
                return false;
            }
            return true;
        }

        void OnNoise(NoiseEvent noise)
        {
            if (noise.source == gameObject || !PlayerAlive) return;
            // Outros soldados atirando também chamam a atenção, mas os passos deles não.
            bool fromPlayer = player != null && noise.source == player.gameObject;
            bool loud = noise.radius >= LoudNoiseRadius;
            if (!fromPlayer && !loud) return;

            // O vento da tempestade cobre passos e golpes, mas não tiros (sons altos).
            float masking = loud ? 1f : Storm.HearingMultiplier;
            float distance = Vector3.Distance(transform.position, noise.position);
            if (distance > noise.radius * settings.hearingMultiplier * masking) return;

            hasNoise = true;
            noisePosition = noise.position;
            Awareness = Mathf.Max(Awareness, loud ? 0.7f : 0.4f);
        }

        /// <summary>Entrega o último som ouvido (uma vez só).</summary>
        public bool ConsumeNoise(out Vector3 position)
        {
            position = noisePosition;
            if (!hasNoise) return false;
            hasNoise = false;
            return true;
        }

        /// <summary>Deixa o soldado em alerta total sabendo onde a Elena está (reforços, dano levado).</summary>
        public void ForceAlert(Vector3 playerPosition)
        {
            Awareness = 1f;
            LastKnownPlayerPosition = playerPosition;
        }

        /// <summary>Ajusta a desconfiança (por exemplo, ao começar a busca).</summary>
        public void SetAwareness(float value) => Awareness = Mathf.Clamp01(value);
    }
}
