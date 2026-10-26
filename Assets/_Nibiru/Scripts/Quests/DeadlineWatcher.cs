using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Prazo genérico de missão: quando o evento do prazo acontece e o evento de cumprimento ainda
    /// não, dispara o evento de perda. Serve para "se a Elena não entregar o remédio até o fim do
    /// wadi, o Yusuf morre". Também confere ao carregar a cena, para um jogo guardado de depois do
    /// prazo ficar certo. Dispara no máximo uma vez.
    /// </summary>
    public class DeadlineWatcher : MonoBehaviour
    {
        [Tooltip("O prazo chega quando este evento acontece.")]
        [SerializeField] StoryEvent deadline;

        [Tooltip("Se este evento já aconteceu, o prazo foi cumprido e nada acontece.")]
        [SerializeField] StoryEvent unlessRaised;

        [Tooltip("O prazo só vale se este evento já aconteceu (por exemplo, a missão ter começado). Opcional.")]
        [SerializeField] StoryEvent onlyIfRaised;

        [Tooltip("Evento disparado quando o prazo vence sem o cumprimento.")]
        [SerializeField] StoryEvent raise;

        void OnEnable() => StoryEvents.Raised += OnRaised;

        void OnDisable() => StoryEvents.Raised -= OnRaised;

        void Start()
        {
            if (StoryEvents.HasBeenRaised(deadline)) Check();
        }

        void OnRaised(StoryEvent raised)
        {
            if (raised == deadline) Check();
        }

        void Check()
        {
            if (raise == null || StoryEvents.HasBeenRaised(raise)) return;
            if (unlessRaised != null && StoryEvents.HasBeenRaised(unlessRaised)) return;
            if (onlyIfRaised != null && !StoryEvents.HasBeenRaised(onlyIfRaised)) return;
            StoryEvents.Raise(raise);
        }
    }
}
