using Nibiru.Player;
using UnityEngine;

namespace Nibiru.Interaction
{
    /// <summary>
    /// Fica no jogador. A cada quadro escolhe o objeto interativo mais adequado
    /// (perto, na frente e sem nada no caminho) e, ao apertar Interagir, usa esse objeto.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;

        [Tooltip("Distância máxima do centro do personagem até o objeto, em metros.")]
        [SerializeField, Min(0f)] float reach = 1.6f;

        [Tooltip("Ângulo máximo entre a frente do personagem e o objeto, em graus.")]
        [SerializeField, Range(0f, 180f)] float maxAngle = 80f;

        [Tooltip("Altura dos olhos acima do centro do personagem, para checar se algo bloqueia a visão.")]
        [SerializeField] float eyeHeight = 0.6f;

        [Tooltip("Camadas que bloqueiam a interação (paredes, por exemplo).")]
        [SerializeField] LayerMask blockingMask = Physics.DefaultRaycastLayers;

        readonly Collider[] overlaps = new Collider[32];

        /// <summary>O objeto que será usado se o jogador apertar Interagir agora, ou null.</summary>
        public IInteractable Current { get; private set; }

        /// <summary>Esquece o objeto em foco (o aviso na tela some), por exemplo ao desligar este componente.</summary>
        public void Clear() => Current = null;

        void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        void Update()
        {
            // Esquivando ou com um menu aberto, não há objeto em foco (e o aviso some).
            bool busy = !input.GameplayEnabled || (motor != null && motor.IsDodging);
            Current = busy ? null : FindBest();

            if (Current != null && input.InteractPressed)
                Current.Interact(this);
        }

        // Entre os objetos ao alcance, prefere o mais perto e mais à frente.
        IInteractable FindBest()
        {
            Vector3 center = transform.position;
            Vector3 eye = center + Vector3.up * eyeHeight;
            int count = Physics.OverlapSphereNonAlloc(center, reach, overlaps, ~0, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider collider = overlaps[i];
                IInteractable candidate = collider.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;

                Vector3 point = collider.bounds.ClosestPoint(eye);
                float distance = Vector3.Distance(center, point);
                if (distance > reach) continue;

                Vector3 flat = Vector3.ProjectOnPlane(point - center, Vector3.up);
                float angle = flat.sqrMagnitude < 0.0001f ? 0f : Vector3.Angle(transform.forward, flat);
                if (angle > maxAngle) continue;

                if (IsBlocked(eye, point, candidate)) continue;

                float score = distance / reach + angle / maxAngle;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        bool IsBlocked(Vector3 eye, Vector3 point, IInteractable candidate)
        {
            if (!Physics.Linecast(eye, point, out RaycastHit hit, blockingMask, QueryTriggerInteraction.Ignore))
                return false;
            return hit.collider.GetComponentInParent<IInteractable>() != candidate;
        }
    }
}
