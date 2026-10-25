using UnityEngine;

namespace Nibiru.Environment
{
    /// <summary>Pisca uma luz em intervalos fixos (o LED de um rádio caído, por exemplo).</summary>
    public class BlinkingLight : MonoBehaviour
    {
        [SerializeField] Light target;

        [SerializeField, Min(0.05f)] float onSeconds = 0.25f;
        [SerializeField, Min(0.05f)] float offSeconds = 1.1f;

        float timer;

        void Update()
        {
            if (target == null) return;
            timer += Time.deltaTime;
            float cycle = onSeconds + offSeconds;
            target.enabled = timer % cycle < onSeconds;
        }
    }
}
