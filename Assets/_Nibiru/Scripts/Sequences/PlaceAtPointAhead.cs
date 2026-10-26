using UnityEngine;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Leva um objeto (por exemplo, o grupo de figurantes de um encontro) para um dos pontos
    /// da lista: o mais perto da Elena entre os que estão à frente dela. Se nenhum estiver à
    /// frente, usa o mais perto. Serve para encontros que podem acontecer em várias entradas
    /// de uma área. O objeto fica virado para a Elena.
    /// </summary>
    public class PlaceAtPointAhead : MonoBehaviour
    {
        [Tooltip("Pontos possíveis, por exemplo um em cada entrada da área.")]
        [SerializeField] Transform[] points = new Transform[0];

        [Tooltip("O que é levado até o ponto. Vazio = este objeto.")]
        [SerializeField] Transform target;

        [Tooltip("Até quantos graus para cada lado da frente da Elena um ponto conta como \"à frente\".")]
        [SerializeField, Range(10f, 180f)] float maxAngle = 90f;

        [Tooltip("Chave estável no estado do mundo (começa com \"place.\"). Com ela, o ponto escolhido e a direção ficam guardados no ponto de retorno e PlaceSaved refaz a escolha. Vazio = não guarda.")]
        [SerializeField] string stateKey = "";

        /// <summary>Volta de um checkpoint: leva o objeto ao ponto que foi escolhido ao vivo. Sem nada guardado, não mexe.</summary>
        public void PlaceSaved()
        {
            if (string.IsNullOrEmpty(stateKey)) return;
            if (!Save.WorldState.TryGetSceneValue(stateKey + ".ponto", out int index) || index < 0 || index >= points.Length || points[index] == null) return;
            Save.WorldState.TryGetSceneValue(stateKey + ".giro", out int yaw);
            (target != null ? target : transform).SetPositionAndRotation(points[index].position, Quaternion.Euler(0f, yaw, 0f));
        }

        public void Place()
        {
            var player = FindAnyObjectByType<Player.PlayerMotor>();
            Transform moved = target != null ? target : transform;
            if (player == null || points.Length == 0) return;

            Vector3 from = player.transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            Transform best = null, bestAny = null;
            float bestDistance = float.MaxValue, bestAnyDistance = float.MaxValue;

            foreach (Transform point in points)
            {
                if (point == null) continue;
                Vector3 flat = Vector3.ProjectOnPlane(point.position - from, Vector3.up);
                float distance = flat.magnitude;
                if (distance < bestAnyDistance)
                {
                    bestAnyDistance = distance;
                    bestAny = point;
                }
                if (distance < bestDistance && Vector3.Angle(forward, flat) <= maxAngle)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            Transform chosen = best != null ? best : bestAny;
            if (chosen == null) return;

            Vector3 toPlayer = Vector3.ProjectOnPlane(from - chosen.position, Vector3.up);
            Quaternion facing = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : chosen.rotation;
            moved.SetPositionAndRotation(chosen.position, facing);

            if (!string.IsNullOrEmpty(stateKey))
            {
                Save.WorldState.SetSceneValue(stateKey + ".ponto", System.Array.IndexOf(points, chosen));
                Save.WorldState.SetSceneValue(stateKey + ".giro", Mathf.RoundToInt(facing.eulerAngles.y) % 360);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f);
            foreach (Transform point in points)
                if (point != null) Gizmos.DrawWireSphere(point.position, 2.5f);
        }
    }
}
