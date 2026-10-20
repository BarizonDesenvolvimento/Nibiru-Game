using UnityEngine;

namespace Nibiru.Sequences
{
    /// <summary>Gira o objeto sem parar (hélice, ventilador, antena).</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.up;

        [Tooltip("Graus por segundo.")]
        [SerializeField] float speed = 720f;

        void Update() => transform.Rotate(axis, speed * Time.deltaTime, Space.Self);
    }
}
