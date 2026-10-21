using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.Testes
{
    /// <summary>
    /// Câmera de prévia que gira em volta de um alvo: botão direito do mouse arrastando gira,
    /// rodinha aproxima e afasta, botão do meio arrastando sobe e desce o ponto de mira.
    /// </summary>
    public class OrbitPreviewCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float height = 1.1f;
        [SerializeField] float distance = 3.5f;
        [SerializeField] float yaw = 200f;
        [SerializeField] float pitch = 8f;
        [SerializeField] float rotateSpeed = 0.25f;
        [SerializeField] float zoomSpeed = 0.002f;

        public Transform Target { get => target; set => target = value; }

        void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (mouse.rightButton.isPressed)
                {
                    yaw += delta.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, -20f, 70f);
                }
                if (mouse.middleButton.isPressed)
                    height = Mathf.Clamp(height - delta.y * 0.005f, 0.1f, 2.2f);
                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSpeed, 0.8f, 10f);
            }
            Vector3 focus = (target != null ? target.position : Vector3.zero) + Vector3.up * height;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
