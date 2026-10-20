using UnityEngine;

namespace Nibiru.Cameras
{
    /// <summary>
    /// Câmera em terceira pessoa sobre o ombro. Segue um ponto acima do personagem,
    /// desloca para o lado do ombro e recua. Se houver parede no caminho, aproxima
    /// na hora para não atravessar, e volta devagar quando o caminho libera.
    /// </summary>
    public class ThirdPersonCameraMode : MonoBehaviour, ICameraMode
    {
        [SerializeField] CameraSettings settings;

        [Tooltip("Personagem que a câmera segue.")]
        [SerializeField] Transform target;

        readonly RaycastHit[] hits = new RaycastHit[16];
        float currentDistance;

        public Vector2 PitchLimits => settings.thirdPersonPitchLimits;

        public void Enter() => currentDistance = settings.distance;

        public void UpdateCamera(Transform camera, float yaw, float pitch, float deltaTime)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * settings.pivotHeight;

            // Primeiro vai para o lado do ombro, depois recua. Testar os dois trechos
            // evita que a câmera entre na parede num corredor estreito.
            Vector3 side = rotation * (settings.shoulderOffset >= 0f ? Vector3.right : Vector3.left);
            float shoulder = FreeDistance(pivot, side, Mathf.Abs(settings.shoulderOffset));
            Vector3 shoulderPoint = pivot + side * shoulder;

            Vector3 back = rotation * Vector3.back;
            float allowed = FreeDistance(shoulderPoint, back, settings.distance);
            currentDistance = allowed < currentDistance
                ? allowed
                : Mathf.MoveTowards(currentDistance, allowed, settings.returnSpeed * deltaTime);

            camera.SetPositionAndRotation(shoulderPoint + back * currentDistance, rotation);
        }

        // Até onde dá para ir na direção indicada sem bater em nada (ignorando o próprio personagem).
        float FreeDistance(Vector3 origin, Vector3 direction, float maxDistance)
        {
            int count = Physics.SphereCastNonAlloc(origin, settings.collisionRadius, direction, hits,
                maxDistance, settings.collisionMask, QueryTriggerInteraction.Ignore);

            float closest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(target)) continue;
                closest = Mathf.Min(closest, hits[i].distance);
            }
            return closest;
        }
    }
}
