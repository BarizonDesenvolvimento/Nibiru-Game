using Nibiru.Combat;
using UnityEngine;

namespace Nibiru.Stealth
{
    /// <summary>
    /// Objeto que faz barulho ao ser destruído (um holofote derrubado a tiro, uma garrafa).
    /// Serve para distrair inimigos que caçam pelo som. Apaga as luzes indicadas e tomba.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class NoiseOnDeath : MonoBehaviour
    {
        [Tooltip("Até onde o barulho é ouvido, em metros.")]
        [SerializeField, Min(0f)] float noiseRadius = 20f;

        [Tooltip("Luzes que apagam quando o objeto cai.")]
        [SerializeField] Light[] lightsToTurnOff = new Light[0];

        [Tooltip("Ângulo em que o objeto tomba, em graus.")]
        [SerializeField] float fallAngle = 70f;

        Health health;

        void Awake() => health = GetComponent<Health>();

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            Noise.Emit(transform.position, noiseRadius, gameObject);
            foreach (Light l in lightsToTurnOff)
                if (l != null) l.enabled = false;
            transform.rotation *= Quaternion.Euler(fallAngle, 0f, 0f);
        }
    }
}
