using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// Inimigo que o Eco mostra através das paredes: uma silhueta brilhante do formato dele.
    /// Coloque no objeto que tem o MeshFilter do corpo.
    /// </summary>
    public class EchoRevealable : EchoTarget
    {
        // Um pouco maior que o corpo, para a borda brilhante aparecer.
        const float SilhouetteScale = 1.04f;

        MeshRenderer silhouette;

        protected override void Prepare(Material revealMaterial)
        {
            if (silhouette != null) return;
            MeshFilter source = GetComponent<MeshFilter>();
            if (source == null || revealMaterial == null) return;

            var go = new GameObject("SilhuetaEco");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * SilhouetteScale;
            go.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            silhouette = go.AddComponent<MeshRenderer>();
            silhouette.sharedMaterial = revealMaterial;
            silhouette.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            silhouette.receiveShadows = false;
        }

        protected override void SetVisible(bool visible)
        {
            if (silhouette != null) silhouette.enabled = visible;
        }
    }
}
