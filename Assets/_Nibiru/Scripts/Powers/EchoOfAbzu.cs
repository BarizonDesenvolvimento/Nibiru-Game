using System.Collections.Generic;
using Nibiru.Combat;
using Nibiru.Player;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// O primeiro poder da Elena. Apertando Poder, ela solta um pulso silencioso que, por alguns
    /// segundos, mostra inimigos através das paredes e acende marcas cuneiformes escondidas.
    /// Coisas que reagem ao Eco (o Lullu) recebem o aviso. Só funciona depois de decifrar a
    /// tabuleta que dá o poder.
    /// </summary>
    public class EchoOfAbzu : MonoBehaviour
    {
        [SerializeField] EchoPowerDefinition power;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Health ownHealth;

        const int RingSegments = 48;
        const float RingHoldTime = 0.25f;

        readonly Collider[] overlaps = new Collider[64];
        readonly List<EchoTarget> targets = new List<EchoTarget>();
        float readyAt;
        LineRenderer ring;
        Vector3 ringCenter;
        float ringStart = -1f;

        public EchoPowerDefinition Power => power;
        public bool IsUnlocked => power != null && power.IsUnlocked;
        public float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (ownHealth == null) ownHealth = GetComponent<Health>();
            CreateRing();
        }

        void Update()
        {
            if (ringStart >= 0f) UpdateRing();

            if (!IsUnlocked || !input.PowerPressed || CooldownLeft > 0f) return;
            if (ownHealth != null && ownHealth.IsDead) return;
            Pulse();
        }

        void Pulse()
        {
            readyAt = Time.time + power.cooldown;
            Vector3 origin = transform.position;

            // Copia a lista: revelar pode criar objetos e mexer nela.
            targets.Clear();
            targets.AddRange(EchoTarget.All);
            foreach (EchoTarget target in targets)
                if (target != null && Vector3.Distance(origin, target.transform.position) <= power.radius)
                    target.Reveal(power.revealDuration, power.revealMaterial);

            int count = Physics.OverlapSphereNonAlloc(origin, power.radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlaps[i].transform.IsChildOf(transform)) continue;
                foreach (IEchoReactive reactive in overlaps[i].GetComponentsInParent<IEchoReactive>())
                    reactive.OnEcho(origin);
            }

            ringCenter = origin + Vector3.down * 0.9f;
            ringStart = Time.time;
            ring.enabled = true;
        }

        void CreateRing()
        {
            var go = new GameObject("AnelEco");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.widthMultiplier = 0.12f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            if (power != null) ring.sharedMaterial = power.ringMaterial;
            ring.enabled = false;
        }

        void UpdateRing()
        {
            float age = Time.time - ringStart;
            if (age > power.expandTime + RingHoldTime)
            {
                ring.enabled = false;
                ringStart = -1f;
                return;
            }
            float radius = power.radius * Mathf.Clamp01(age / power.expandTime);
            for (int s = 0; s < RingSegments; s++)
            {
                float angle = s * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(s, ringCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
    }
}
