using UnityEngine;
using UnityEngine.Rendering;

namespace Nibiru.Environment
{
    /// <summary>
    /// Liga um Volume de pós-processamento só perto de uma área (a vitrine da Aldeia, por exemplo), sem colisor:
    /// o peso vai de 1, com a câmera dentro da caixa, a 0, a "Blend Distance" metros dela. Um Volume local do URP
    /// precisaria de um colisor, que os raios da câmera e da furtividade poderiam encontrar.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    [ExecuteAlways]
    public class AreaVolume : MonoBehaviour
    {
        [Tooltip("Caixa da área em coordenadas do mundo: dentro dela o Volume vale inteiro.")]
        [SerializeField] Bounds area = new Bounds(Vector3.zero, Vector3.one * 10f);

        [Tooltip("Metros, fora da caixa, em que o efeito some aos poucos.")]
        [SerializeField, Min(0.01f)] float blendDistance = 6f;

        Volume volume;

        public void Configure(Bounds box, float blend)
        {
            area = box;
            blendDistance = blend;
        }

        void OnEnable()
        {
            volume = GetComponent<Volume>();
            volume.isGlobal = true;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float distance = Mathf.Sqrt(area.SqrDistance(cam.transform.position));
            volume.weight = 1f - Mathf.Clamp01(distance / blendDistance);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
