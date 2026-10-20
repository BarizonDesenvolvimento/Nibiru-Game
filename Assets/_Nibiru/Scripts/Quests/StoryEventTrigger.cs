using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Área que dispara um evento da história quando a Elena entra nela (uma vez por partida).
    /// Precisa de um Collider marcado como Is Trigger.
    /// </summary>
    public class StoryEventTrigger : MonoBehaviour
    {
        [SerializeField] StoryEvent storyEvent;

        void OnTriggerEnter(Collider other)
        {
            // Procura o personagem (e não a etiqueta da raiz): no prefab Elena, a raiz é só o grupo.
            if (StoryEvents.HasBeenRaised(storyEvent) || other.GetComponentInParent<Player.PlayerMotor>() == null) return;
            StoryEvents.Raise(storyEvent);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.6f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (TryGetComponent(out BoxCollider box)) Gizmos.DrawCube(box.center, box.size);
        }
    }
}
