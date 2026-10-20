using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Stealth
{
    /// <summary>
    /// Ajuda de teste: desenha no chão um anel do tamanho de cada ruído, para ver até onde
    /// o som chega. Desligue o componente (ou "Mostrar Anéis") para jogar sem eles.
    /// </summary>
    public class NoiseDebugRings : MonoBehaviour
    {
        [SerializeField] bool showRings = true;
        [SerializeField] Material ringMaterial;

        [Tooltip("Quanto tempo cada anel fica visível, em segundos.")]
        [SerializeField, Min(0.1f)] float lifetime = 0.6f;

        [Tooltip("Tempo para o anel abrir até o tamanho total, em segundos.")]
        [SerializeField, Min(0.01f)] float expandTime = 0.2f;

        const int Segments = 40;

        class Ring
        {
            public LineRenderer line;
            public Vector3 center;
            public float radius;
            public float bornAt;
        }

        readonly List<Ring> active = new List<Ring>();
        readonly Stack<LineRenderer> pool = new Stack<LineRenderer>();

        void OnEnable() => Noise.Emitted += OnNoise;

        void OnDisable()
        {
            Noise.Emitted -= OnNoise;
            foreach (Ring ring in active) Release(ring.line);
            active.Clear();
        }

        void OnNoise(NoiseEvent noise)
        {
            if (!showRings) return;
            Vector3 ground = new Vector3(noise.position.x, GroundHeight(noise.position) + 0.05f, noise.position.z);
            active.Add(new Ring { line = Get(), center = ground, radius = noise.radius, bornAt = Time.time });
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Ring ring = active[i];
                float age = Time.time - ring.bornAt;
                if (age >= lifetime)
                {
                    Release(ring.line);
                    active.RemoveAt(i);
                    continue;
                }
                Draw(ring.line, ring.center, ring.radius * Mathf.Clamp01(age / expandTime));
            }
        }

        static float GroundHeight(Vector3 position) =>
            Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : position.y - 1f;

        static void Draw(LineRenderer line, Vector3 center, float radius)
        {
            for (int s = 0; s < Segments; s++)
            {
                float angle = s * Mathf.PI * 2f / Segments;
                line.SetPosition(s, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

        LineRenderer Get()
        {
            if (pool.Count > 0)
            {
                LineRenderer reused = pool.Pop();
                reused.enabled = true;
                return reused;
            }
            var go = new GameObject("AnelDeRuido");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Segments;
            line.widthMultiplier = 0.04f;
            line.sharedMaterial = ringMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void Release(LineRenderer line)
        {
            line.enabled = false;
            pool.Push(line);
        }
    }
}
