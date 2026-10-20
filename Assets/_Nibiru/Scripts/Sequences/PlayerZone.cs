using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Área que reage quando a Elena entra nela, quantas vezes for (com um intervalo mínimo).
    /// Para um acontecimento da história que vale uma vez só, use o StoryEventTrigger.
    /// Precisa de um Collider marcado como Is Trigger.
    /// </summary>
    public class PlayerZone : MonoBehaviour
    {
        [Tooltip("O que acontece quando a Elena entra.")]
        [SerializeField] UnityEvent onEnter = new UnityEvent();

        [Tooltip("Segundos mínimos entre duas reações, para não repetir sem parar na beirada.")]
        [SerializeField, Min(0f)] float cooldown = 4f;

        float readyAt;

        void OnTriggerEnter(Collider other)
        {
            if (Time.time < readyAt || other.GetComponentInParent<Player.PlayerMotor>() == null) return;
            readyAt = Time.time + cooldown;
            onEnter.Invoke();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (TryGetComponent(out BoxCollider box)) Gizmos.DrawCube(box.center, box.size);
        }
    }
}
