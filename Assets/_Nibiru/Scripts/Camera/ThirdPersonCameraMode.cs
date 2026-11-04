using UnityEngine;

namespace Nibiru.Cameras
{
    /// <summary>
    /// Câmera em terceira pessoa sobre o ombro. Segue um ponto acima do personagem,
    /// desloca para o lado do ombro e recua. Se houver parede no caminho, aproxima
    /// na hora para não atravessar, e volta devagar quando o caminho libera.
    /// Ao mirar, chega mais perto do ombro e fecha o campo de visão.
    /// </summary>
    public class ThirdPersonCameraMode : MonoBehaviour, ICameraMode
    {
        [SerializeField] CameraSettings settings;

        [Tooltip("Ponto que a câmera segue (o personagem ou um filho dele).")]
        [SerializeField] Transform target;

        [Tooltip("A colisão da câmera ignora este objeto e seus filhos (o personagem). Vazio = o topo da hierarquia do alvo.")]
        [SerializeField] Transform ignoreCollisionsOf;

        readonly RaycastHit[] hits = new RaycastHit[16];
        float currentDistance;
        bool aiming;
        float aimBlend;
        Camera cachedCamera;
        float baseFieldOfView;

        public Vector2 PitchLimits => settings.thirdPersonPitchLimits;

        // Distância escolhida pelo jogador (zoom). Antes de receber a primeira, vale o padrão do asset.
        float zoomDistance = -1f;

        /// <summary>Distância normal (fora da mira) em uso: a do zoom, ou o padrão do asset se ninguém deu zoom.</summary>
        public float ZoomDistance => zoomDistance > 0f ? zoomDistance : settings.distance;

        public void Enter() => currentDistance = ZoomDistance;

        public void SetAiming(bool value) => aiming = value;

        public void SetZoom(float distance) => zoomDistance = distance;

        public void UpdateCamera(Transform camera, float yaw, float pitch, float deltaTime)
        {
            aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1f : 0f, settings.aimBlendSpeed * deltaTime);
            float shoulderOffset = Mathf.Lerp(settings.shoulderOffset, settings.aimShoulderOffset, aimBlend);
            // A distância desejada vem do zoom; a mira a troca pela dela (o zoom é ignorado mirando). As checagens de parede
            // abaixo não sabem do zoom: continuam mandando.
            float distance = Mathf.Lerp(ZoomDistance, settings.aimDistance, aimBlend);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * settings.pivotHeight;

            // Primeiro vai para o lado do ombro, depois recua. Testar os dois trechos
            // evita que a câmera entre na parede num corredor estreito.
            Vector3 side = rotation * (shoulderOffset >= 0f ? Vector3.right : Vector3.left);
            float shoulder = FreeDistance(pivot, side, Mathf.Abs(shoulderOffset));
            Vector3 shoulderPoint = pivot + side * shoulder;

            Vector3 back = rotation * Vector3.back;
            float allowed = FreeDistance(shoulderPoint, back, distance);
            currentDistance = allowed < currentDistance
                ? allowed
                : Mathf.MoveTowards(currentDistance, allowed, settings.returnSpeed * deltaTime);

            // Última conferência, do ponto seguido até a câmera em linha reta: num vão estreito (uma porta
            // na espessura da parede, um túnel) o ombro pode sair da pedra e o recuo, que começa colado
            // nela, passar por dentro do canto. Se a linha bate em algo, a câmera para antes.
            Vector3 desired = shoulderPoint + back * currentDistance;
            Vector3 toCamera = desired - pivot;
            float fullLength = toCamera.magnitude;
            if (fullLength > 0.0001f)
            {
                Vector3 direction = toCamera / fullLength;
                float free = FreeDistance(pivot, direction, fullLength);
                if (free < fullLength) desired = pivot + direction * free;
            }

            camera.SetPositionAndRotation(desired, rotation);
            UpdateFieldOfView(camera);
        }

        void UpdateFieldOfView(Transform camera)
        {
            if (cachedCamera == null)
            {
                cachedCamera = camera.GetComponent<Camera>();
                if (cachedCamera == null) return;
                baseFieldOfView = cachedCamera.fieldOfView;
            }
            cachedCamera.fieldOfView = Mathf.Lerp(baseFieldOfView, settings.aimFieldOfView, aimBlend);
        }

        // Até onde dá para ir na direção indicada sem bater em nada (ignorando o próprio personagem).
        float FreeDistance(Vector3 origin, Vector3 direction, float maxDistance)
        {
            int count = Physics.SphereCastNonAlloc(origin, settings.collisionRadius, direction, hits,
                maxDistance, settings.collisionMask, QueryTriggerInteraction.Ignore);

            float closest = maxDistance;
            for (int i = 0; i < count; i++)
            {
                Transform ignored = ignoreCollisionsOf != null ? ignoreCollisionsOf : target.root;
                if (hits[i].collider.transform.IsChildOf(ignored)) continue;
                closest = Mathf.Min(closest, hits[i].distance);
            }
            return closest;
        }
    }
}
