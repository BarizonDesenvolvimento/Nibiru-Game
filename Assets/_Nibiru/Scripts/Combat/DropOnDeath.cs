using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Ao morrer, larga no chão, ao lado do corpo, objetos que estavam desligados na cena
    /// (itens para pegar, como o rádio do líder de esquadrão).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DropOnDeath : MonoBehaviour
    {
        [Tooltip("Objetos desligados na cena que aparecem ao lado do corpo.")]
        [SerializeField] GameObject[] drops = new GameObject[0];

        [Tooltip("Distância entre um objeto largado e o próximo (metros).")]
        [SerializeField, Min(0f)] float spacing = 0.6f;

        [Tooltip("Altura acima do chão onde o objeto fica.")]
        [SerializeField, Min(0f)] float heightAboveGround = 0.08f;

        Health health;

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            Vector3 side = transform.right;
            side.y = 0f;
            side = side.sqrMagnitude > 0.001f ? side.normalized : Vector3.right;

            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i] == null) continue;
                Vector3 spot = transform.position + side * (spacing * (i + 1));
                drops[i].transform.position = GroundBelow(spot) + Vector3.up * heightAboveGround;
                drops[i].SetActive(true);
            }
        }

        // Primeiro chão embaixo do ponto, ignorando o próprio corpo.
        Vector3 GroundBelow(Vector3 point)
        {
            Vector3 start = point + Vector3.up * 1.5f;
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 ground = point;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= best) continue;
                best = hit.distance;
                ground = hit.point;
            }
            return ground;
        }
    }
}
