using Nibiru.Quests;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Escolhe entre duas reações conforme um evento da história já ter acontecido ou não.
    /// Por exemplo: se a Elena estava no mirante, ela comenta o que viu; se não, o que ouviu.
    /// </summary>
    public class StoryEventBranch : MonoBehaviour
    {
        [SerializeField] StoryEvent storyEvent;

        [Tooltip("O que acontece se o evento já aconteceu.")]
        [SerializeField] UnityEvent ifRaised = new UnityEvent();

        [Tooltip("O que acontece se o evento ainda não aconteceu.")]
        [SerializeField] UnityEvent ifNotRaised = new UnityEvent();

        public void Run()
        {
            if (StoryEvents.HasBeenRaised(storyEvent)) ifRaised.Invoke();
            else ifNotRaised.Invoke();
        }
    }
}
