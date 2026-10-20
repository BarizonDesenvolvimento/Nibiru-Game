using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// Marca cuneiforme escondida: invisível até o Eco acender, por alguns segundos.
    /// Coloque no objeto da marca (com o Renderer dela).
    /// </summary>
    public class EchoHiddenMark : EchoTarget
    {
        Renderer[] renderers;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            SetVisible(false);
        }

        protected override void SetVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (Renderer r in renderers) r.enabled = visible;
        }
    }
}
