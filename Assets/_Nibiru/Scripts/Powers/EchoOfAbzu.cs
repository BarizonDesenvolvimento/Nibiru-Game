using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// O primeiro poder da Elena (o efeito). Ela solta um pulso silencioso que, por alguns segundos,
    /// mostra inimigos através das paredes e acende marcas cuneiformes escondidas. Coisas que reagem
    /// ao Eco (o Lullu, o Primeiro Servo) recebem o aviso e ficam atordoadas. Quem lê o botão, confere
    /// a posse (tabuleta decifrada) e conta a recarga é o <see cref="PlayerPowers"/>.
    /// </summary>
    public class EchoOfAbzu : PowerBehaviour
    {
        [SerializeField] EchoPowerDefinition power;

        const int RingSegments = 48;
        const float RingHoldTime = 0.25f;

        readonly Collider[] overlaps = new Collider[64];
        readonly List<EchoTarget> targets = new List<EchoTarget>();
        LineRenderer ring;
        Vector3 ringCenter;
        float ringStart = -1f;

        public EchoPowerDefinition Power => power;
        public override PowerDefinition Definition => power;
        public bool IsUnlocked => power != null && power.IsUnlocked;
        public float CooldownLeft => PowerBook.CooldownLeft(power);

        void Awake() => CreateRing();

        void Update()
        {
            if (ringStart >= 0f) UpdateRing();
        }

        public override void Activate() => Pulse();

        void Pulse()
        {
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
