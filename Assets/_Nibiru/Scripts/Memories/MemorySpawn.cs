using UnityEngine;

namespace Nibiru.Memories
{
    /// <summary>Onde o jogador começa dentro da cena de uma memória, e para onde olha.</summary>
    public class MemorySpawn : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward);
        }
    }
}
