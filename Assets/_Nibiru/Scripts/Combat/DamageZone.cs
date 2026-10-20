using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Área que machuca quem estiver dentro, em golpes repetidos (brasas, espinhos, gás).
    /// O tamanho da área é o tamanho do objeto (escala), como uma caixa.
    /// </summary>
    public class DamageZone : MonoBehaviour
    {
        [Tooltip("Dano de cada golpe.")]
        [SerializeField, Min(0f)] float damagePerHit = 10f;

        [Tooltip("Tempo entre golpes, em segundos.")]
        [SerializeField, Min(0.05f)] float interval = 1f;

        readonly Collider[] overlaps = new Collider[16];
        readonly HashSet<Health> hitThisPulse = new HashSet<Health>();
        float nextHitTime;

        void Update()
        {
            if (Time.time < nextHitTime) return;
            nextHitTime = Time.time + interval;

            int count = Physics.OverlapBoxNonAlloc(transform.position, transform.lossyScale * 0.5f, overlaps,
                transform.rotation, ~0, QueryTriggerInteraction.Ignore);

            // Um personagem com vários colliders leva só um golpe por vez.
            hitThisPulse.Clear();
            for (int i = 0; i < count; i++)
            {
                Health health = overlaps[i].GetComponentInParent<Health>();
                if (health == null || !hitThisPulse.Add(health)) continue;
                health.TakeDamage(new DamageInfo(damagePerHit, overlaps[i].transform.position, gameObject));
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}
