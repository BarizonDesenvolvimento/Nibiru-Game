using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Quests
{
    /// <summary>
    /// Reage a um evento da história com o que for configurado no Inspector (ligar soldados,
    /// abrir um portão, mostrar um objeto...). Ao voltar de um checkpoint, se o evento já tinha
    /// acontecido, a reação é refeita para a cena ficar igual.
    /// </summary>
    public class StoryEventListener : MonoBehaviour
    {
        [SerializeField] StoryEvent storyEvent;

        [Tooltip("O que acontece quando o evento ocorre.")]
        [SerializeField] UnityEvent response = new UnityEvent();

        [Tooltip("Ao carregar a cena, refaz a reação se o evento já aconteceu antes.")]
        [SerializeField] bool replayOnLoad = true;

        void OnEnable() => StoryEvents.Raised += OnRaised;

        void OnDisable() => StoryEvents.Raised -= OnRaised;

        void Start()
        {
            if (replayOnLoad && StoryEvents.HasBeenRaised(storyEvent)) response.Invoke();
        }

        void OnRaised(StoryEvent raised)
        {
            if (raised == storyEvent) response.Invoke();
        }
    }
}
