using Nibiru.Combat;
using Nibiru.Player;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.AI
{
    /// <summary>
    /// Os olhos do helicóptero da Fundação. Se a Elena ficar à vista embaixo dele por um
    /// instante, ele avisa pelo rádio e os soldados por perto vão até onde ela está.
    /// Passarelas, lonas e a parede de uma trincheira entre os dois escondem; agachada,
    /// ela só é vista bem embaixo dele. Avisa uma vez cada vez que é ligado.
    /// </summary>
    public class HelicopterSpotter : MonoBehaviour
    {
        [Tooltip("Raio, medido no chão, do que o helicóptero enxerga embaixo dele (metros).")]
        [SerializeField, Min(1f)] float viewRadius = 14f;

        [Tooltip("Fração do raio que vale para a Elena agachada.")]
        [SerializeField, Range(0f, 1f)] float crouchedRadiusFactor = 0.5f;

        [Tooltip("Segundos com a Elena à vista até dar o alarme.")]
        [SerializeField, Min(0f)] float timeToSpot = 0.8f;

        [Tooltip("Soldados até esta distância da Elena são avisados.")]
        [SerializeField, Min(1f)] float alertRadius = 45f;

        [Tooltip("Aviso na tela quando o helicóptero avista a Elena.")]
        [SerializeField] string radioMessage = "Rádio: \"Aqui é o Falcão. Alvo à vista, mandando a posição!\"";

        [Tooltip("Chave do aviso na tabela de idiomas (ui.radio...). Vazio = usa o texto acima.")]
        [SerializeField] string radioMessageKey;

        [Tooltip("O que mais acontece quando ele avista a Elena (opcional).")]
        [SerializeField] UnityEvent onSpotted = new UnityEvent();

        PlayerMotor player;
        Health playerHealth;
        float seenTime;
        bool spotted;

        void OnEnable()
        {
            seenTime = 0f;
            spotted = false;
        }

        void Start()
        {
            player = FindAnyObjectByType<PlayerMotor>();
            if (player != null) playerHealth = player.GetComponent<Health>();
        }

        void Update()
        {
            if (spotted || player == null || (playerHealth != null && playerHealth.IsDead)) return;

            if (CanSeePlayer())
            {
                seenTime += Time.deltaTime;
                if (seenTime >= timeToSpot) Spot();
            }
            else seenTime = Mathf.Max(0f, seenTime - Time.deltaTime);
        }

        bool CanSeePlayer()
        {
            Vector3 flat = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
            float radius = player.IsCrouching ? viewRadius * crouchedRadiusFactor : viewRadius;
            if (flat.magnitude > radius) return false;

            // Linha da barriga do helicóptero até o peito da Elena: se bater em outra coisa antes, ela está coberta.
            Vector3 from = transform.position + Vector3.down * 0.5f;
            Vector3 to = player.transform.position + Vector3.up * 1f;
            if (!Physics.Linecast(from, to, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.GetComponentInParent<PlayerMotor>() == player;
        }

        void Spot()
        {
            spotted = true;
            string message = Localization.Loc.Get(radioMessageKey, radioMessage);
            if (!string.IsNullOrEmpty(message)) Notifications.Post(message);

            Vector3 position = player.transform.position;
            foreach (SoldierAI soldier in SoldierAI.All)
                if (Vector3.Distance(soldier.transform.position, position) <= alertRadius)
                    soldier.Alert(position);

            onSpotted.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Vector3 ground = transform.position;
            if (Physics.Raycast(transform.position + Vector3.down * 0.5f, Vector3.down, out RaycastHit hit, 200f)) ground = hit.point;
            const int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
                Gizmos.DrawLine(ground + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * viewRadius,
                                ground + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * viewRadius);
            }
        }
    }
}
