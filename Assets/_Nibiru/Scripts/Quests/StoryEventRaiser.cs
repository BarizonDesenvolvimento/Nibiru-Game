using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Dispara um evento da história a partir de outras coisas: chamado por um evento do
    /// Inspector (Raise) ou, se "Ao Morrer" estiver marcado, quando a vida deste objeto acaba
    /// (um chefe ou um soldado importante, por exemplo).
    /// </summary>
    public class StoryEventRaiser : MonoBehaviour
    {
        [SerializeField] StoryEvent storyEvent;

        [Tooltip("Dispara o evento quando o Health deste objeto chega a zero.")]
        [SerializeField] bool raiseOnDeath;

        Health health;

        void Awake()
        {
            if (raiseOnDeath) health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (health != null) health.Died += Raise;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= Raise;
        }

        public void Raise() => StoryEvents.Raise(storyEvent);
    }
}
