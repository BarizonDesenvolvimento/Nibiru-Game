using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// Inimigo que o Eco mostra através das paredes: uma silhueta brilhante do formato dele.
    /// Coloque no objeto que tem o MeshFilter do corpo ou, num personagem animado, na raiz dele
    /// (a silhueta acompanha o corpo animado, achado nos filhos).
    /// </summary>
    public class EchoRevealable : EchoTarget
    {
        // Um pouco maior que o corpo, para a borda brilhante aparecer.
        const float SilhouetteScale = 1.04f;

        Renderer silhouette;

        protected override void Prepare(Material revealMaterial)
        {
            if (silhouette != null || revealMaterial == null) return;
            MeshFilter source = GetComponent<MeshFilter>();
            if (source == null)
            {
                PrepareSkinned(revealMaterial);
                return;
            }

            var go = new GameObject("SilhuetaEco");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * SilhouetteScale;
            go.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            silhouette = go.AddComponent<MeshRenderer>();
            silhouette.sharedMaterial = revealMaterial;
            silhouette.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            silhouette.receiveShadows = false;
        }

        // Corpo animado: uma cópia da malha presa aos mesmos ossos (sem aumentar: a pele segue os ossos).
        void PrepareSkinned(Material revealMaterial)
        {
            SkinnedMeshRenderer source = GetComponentInChildren<SkinnedMeshRenderer>();
            if (source == null) return;

            var go = new GameObject("SilhuetaEco");
            go.transform.SetParent(source.transform.parent, false);
            go.transform.SetLocalPositionAndRotation(source.transform.localPosition, source.transform.localRotation);
            go.transform.localScale = source.transform.localScale;
            var skinned = go.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = source.sharedMesh;
            skinned.bones = source.bones;
            skinned.rootBone = source.rootBone;
            skinned.localBounds = source.localBounds;
            var materials = new Material[source.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = revealMaterial;
            skinned.sharedMaterials = materials;
            skinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skinned.receiveShadows = false;
            silhouette = skinned;
        }

        protected override void SetVisible(bool visible)
        {
            if (silhouette != null) silhouette.enabled = visible;
        }
    }
}
