using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>Algo que reage ao Eco do Abzu (o Lullu, por exemplo, fica atordoado).</summary>
    public interface IEchoReactive
    {
        void OnEcho(Vector3 origin);
    }

    /// <summary>
    /// Base das coisas que o Eco revela por alguns segundos. Cada uma se registra numa lista
    /// para o pulso achar rápido as que estão no alcance.
    /// </summary>
    public abstract class EchoTarget : MonoBehaviour
    {
        static readonly List<EchoTarget> all = new List<EchoTarget>();
        public static IReadOnlyList<EchoTarget> All => all;

        float hideAt = -1f;

        protected virtual void OnEnable() => all.Add(this);

        protected virtual void OnDisable()
        {
            all.Remove(this);
            SetVisible(false);
            hideAt = -1f;
        }

        public void Reveal(float duration, Material revealMaterial)
        {
            hideAt = Time.time + duration;
            Prepare(revealMaterial);
            SetVisible(true);
        }

        void Update()
        {
            if (hideAt >= 0f && Time.time >= hideAt)
            {
                hideAt = -1f;
                SetVisible(false);
            }
        }

        protected virtual void Prepare(Material revealMaterial) { }

        protected abstract void SetVisible(bool visible);
    }
}
