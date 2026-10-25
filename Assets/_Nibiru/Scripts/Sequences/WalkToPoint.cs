using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Faz um personagem andar pela malha de navegação até um ponto, contornando casas e
    /// obstáculos (por exemplo, o Samir levando a Elena até a Aldeia). Pode parar e esperar
    /// quando a Elena fica para trás. Precisa de um NavMeshAgent.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class WalkToPoint : MonoBehaviour
    {
        [Tooltip("Para onde ele anda. Ao chegar (ou ao voltar de um checkpoint), fica virado como este ponto.")]
        [SerializeField] Transform destination;

        [Tooltip("Para e espera quando a Elena fica para trás, mais longe que isto (em metros). Zero = nunca espera.")]
        [SerializeField, Min(0f)] float waitIfPlayerFartherThan = 10f;

        [Tooltip("Corre nesta velocidade (m/s) quando a Elena vai à frente e mais longe que a distância de espera. Zero = nunca corre.")]
        [SerializeField, Min(0f)] float catchUpSpeed = 0f;

        [Tooltip("Depois de chegar, vira devagar na direção da Elena.")]
        [SerializeField] bool facePlayerAfterArriving = true;

        [Tooltip("O que acontece ao chegar.")]
        [SerializeField] UnityEvent onArrived = new UnityEvent();

        const float TurnSpeed = 180f;

        NavMeshAgent agent;
        Player.PlayerMotor player;
        bool walking, arrived, leaving;
        float walkSpeed;

        public bool IsWalking => walking;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            walkSpeed = agent.speed;
        }

        public void Walk()
        {
            if (destination == null || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(destination.position);
            walking = true;
            arrived = false;
        }

        /// <summary>Troca o destino e anda até ele (por exemplo, do pátio para dentro de uma casa).</summary>
        public void WalkTo(Transform newDestination)
        {
            destination = newDestination;
            Walk();
        }

        /// <summary>
        /// Vai embora: anda até o ponto sem esperar a Elena e some ao chegar (por exemplo, o
        /// Samir saindo da Aldeia pelos canais).
        /// </summary>
        public void LeaveTo(Transform newDestination)
        {
            waitIfPlayerFartherThan = 0f;
            leaving = true;
            WalkTo(newDestination);
        }

        /// <summary>Troca o destino e põe o personagem direto nele, sem disparar a chegada.</summary>
        public void SnapTo(Transform newDestination)
        {
            destination = newDestination;
            SnapToDestination();
        }

        /// <summary>Põe o personagem direto no destino, sem disparar a chegada (para voltar de um checkpoint).</summary>
        public void SnapToDestination()
        {
            if (destination == null) return;
            walking = false;
            arrived = true;
            agent.Warp(destination.position);
            transform.rotation = destination.rotation;
        }

        void Update()
        {
            if (player == null) player = FindAnyObjectByType<Player.PlayerMotor>();

            if (walking)
            {
                // Espera só quem ficou para trás: se a Elena estiver longe, mas à frente, ele segue.
                if (player != null && waitIfPlayerFartherThan > 0f)
                {
                    Vector3 goal = destination.position;
                    bool far = Vector3.Distance(transform.position, player.transform.position) > waitIfPlayerFartherThan;
                    bool behind = Vector3.Distance(player.transform.position, goal) > Vector3.Distance(transform.position, goal);
                    agent.isStopped = far && behind;
                    // E corre para alcançá-la quando ela vai longe à frente.
                    if (catchUpSpeed > 0f) agent.speed = far && !behind ? catchUpSpeed : walkSpeed;
                }

                if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f) return;
                walking = false;
                arrived = true;
                onArrived.Invoke();
                if (leaving) gameObject.SetActive(false);
            }
            else if (arrived && facePlayerAfterArriving && player != null)
            {
                Vector3 flat = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
                // No diálogo o jogo pausa: ele vira no tempo real, para não falar de costas para a Elena.
                float dt = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                if (flat.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), TurnSpeed * dt);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (destination == null) return;
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
            Gizmos.DrawLine(transform.position, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.4f);
        }
    }
}
