using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// Marca cuneiforme escondida: invisível até o Eco acender, por alguns segundos.
    /// Coloque no objeto da marca (com o Renderer dela).
    /// </summary>
    public class EchoHiddenMark : EchoTarget
    {
        [Tooltip("Disparado na primeira vez que o Eco acende esta marca (opcional). A inscrição da Aldeia usa para liberar o \"Ler a inscrição\".")]
        [SerializeField] StoryEvent raiseOnRevealed;

        Renderer[] renderers;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            SetVisible(false);
        }

        protected override void Prepare(Material revealMaterial)
        {
            if (raiseOnRevealed != null && !StoryEvents.HasBeenRaised(raiseOnRevealed)) StoryEvents.Raise(raiseOnRevealed);
        }

        protected override void SetVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (Renderer r in renderers) r.enabled = visible;
        }
    }
}
