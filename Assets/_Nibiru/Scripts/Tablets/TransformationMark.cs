using UnityEngine;

namespace Nibiru.Tablets
{
    /// <summary>
    /// A marca azul na pele do pulso esquerdo da Elena: um anel fino da cor de Kur, logo acima
    /// da mão. Aparece com a primeira tabuleta (transformação acima de zero) e fica com ela.
    /// Fica no jogador; precisa de um modelo Humanoid nos filhos.
    /// </summary>
    public class TransformationMark : MonoBehaviour
    {
        [Tooltip("Animator do modelo. Vazio = procura nos filhos.")]
        [SerializeField] Animator animator;

        [Tooltip("Material da marca (azul de lápis-lazúli, com luz própria).")]
        [SerializeField] Material material;

        [Tooltip("Diâmetro do anel, em metros (um pouco mais largo que o pulso, para aparecer).")]
        [SerializeField, Min(0.01f)] float diameter = 0.075f;

        [Tooltip("Largura da faixa, em metros.")]
        [SerializeField, Min(0.002f)] float width = 0.018f;

        [Tooltip("Distância da marca até a junta da mão, subindo pelo antebraço, em metros.")]
        [SerializeField, Min(0f)] float distanceFromHand = 0.035f;

        GameObject mark;

        void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return;

            Transform forearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (forearm == null || hand == null) return;

            Vector3 along = (hand.position - forearm.position).normalized;
            mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mark.name = "MarcaDeKur";
            Destroy(mark.GetComponent<Collider>());
            var meshRenderer = mark.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // O cilindro da Unity tem 2 de altura: metade da largura na escala Y.
            mark.transform.localScale = new Vector3(diameter, width * 0.5f, diameter);
            mark.transform.SetPositionAndRotation(hand.position - along * distanceFromHand, Quaternion.FromToRotation(Vector3.up, along));
            mark.transform.SetParent(forearm, true);
            mark.SetActive(Transformation.Value > 0);
        }

        void Update()
        {
            if (mark == null) return;
            bool show = Transformation.Value > 0;
            if (mark.activeSelf != show) mark.SetActive(show);
        }
    }
}
