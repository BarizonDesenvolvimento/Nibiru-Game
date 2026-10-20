using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Move o objeto por uma lista de pontos, virando na direção do movimento e freando perto
    /// do último ponto (como um pouso). Serve para veículos e figurantes roteirizados,
    /// por exemplo o helicóptero da Fundação.
    /// </summary>
    public class MoveAlongPath : MonoBehaviour
    {
        [Tooltip("Pontos do caminho, em ordem. Ao começar, o objeto vai para o primeiro.")]
        [SerializeField] Transform[] points = new Transform[0];

        [Tooltip("Velocidade em metros por segundo.")]
        [SerializeField, Min(0.1f)] float speed = 12f;

        [Tooltip("Graus por segundo para virar na direção do movimento (só no plano). Zero = não vira.")]
        [SerializeField, Min(0f)] float turnSpeed = 90f;

        [Tooltip("Começa a frear quando faltar esta distância para o último ponto. Zero = chega sem frear.")]
        [SerializeField, Min(0f)] float slowDownDistance = 10f;

        [Tooltip("Começa sozinho quando o objeto é ligado.")]
        [SerializeField] bool playOnEnable;

        [Tooltip("O que acontece ao chegar ao último ponto.")]
        [SerializeField] UnityEvent onArrived = new UnityEvent();

        // Velocidade mínima na frenagem, como fração da normal, para não parar antes de chegar.
        const float MinSlowFraction = 0.12f;

        int target = -1;

        public bool IsMoving => target >= 0;

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        public void Play()
        {
            if (points.Length == 0) return;
            transform.position = points[0].position;
            target = points.Length > 1 ? 1 : -1;
            if (target < 0) onArrived.Invoke();
        }

        /// <summary>Põe o objeto direto no fim do caminho, sem disparar a chegada (para voltar de um checkpoint).</summary>
        public void SnapToEnd()
        {
            target = -1;
            if (points.Length == 0) return;
            Transform last = points[points.Length - 1];
            transform.position = last.position;
            if (points.Length > 1 && turnSpeed > 0f)
            {
                Vector3 flat = Vector3.ProjectOnPlane(last.position - points[points.Length - 2].position, Vector3.up);
                if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
            }
        }

        void Update()
        {
            if (target < 0) return;

            Vector3 goal = points[target].position;
            float step = speed * Time.deltaTime;
            bool lastPoint = target == points.Length - 1;
            if (lastPoint && slowDownDistance > 0f)
            {
                float distance = Vector3.Distance(transform.position, goal);
                step *= Mathf.Clamp(distance / slowDownDistance, MinSlowFraction, 1f);
            }

            Vector3 flat = Vector3.ProjectOnPlane(goal - transform.position, Vector3.up);
            if (turnSpeed > 0f && flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);

            transform.position = Vector3.MoveTowards(transform.position, goal, step);
            if (transform.position != goal) return;

            if (lastPoint)
            {
                target = -1;
                onArrived.Invoke();
            }
            else target++;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.8f);
            for (int i = 1; i < points.Length; i++)
                if (points[i - 1] != null && points[i] != null)
                    Gizmos.DrawLine(points[i - 1].position, points[i].position);
        }
    }
}
