using System.Collections;
using Nibiru.Stealth;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Holofote da Fundação num poste, que cai com um tiro ou um golpe de faca. A queda faz um
    /// estrondo (ruído grande, que não é da Elena): o Lullu e o Primeiro Servo vão até lá e ficam
    /// uns segundos distraídos. Caído, o holofote continua aceso no chão. Também pisca quando uma
    /// cena pede (a troca de fase do chefe).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class FallingFloodlight : MonoBehaviour
    {
        [Tooltip("O que tomba (o poste com o holofote), girando em volta da base.")]
        [SerializeField] Transform pivot;

        [Tooltip("Para onde ele cai, em coordenadas do mundo (só a direção no chão conta).")]
        [SerializeField] Vector3 fallDirection = Vector3.forward;

        [SerializeField] Light lamp;

        [Tooltip("Alcance do estrondo da queda, em metros. O Lullu se distrai a partir de 30 m (\"Distração\" nos dados dele).")]
        [SerializeField, Min(0f)] float crashNoise = 40f;

        [SerializeField, Min(0.05f)] float fallTime = 0.55f;

        [Tooltip("Brilho que sobra caído, em fração do original.")]
        [SerializeField, Range(0f, 1f)] float fallenBrightness = 0.6f;

        [SerializeField] AudioSource crashSound;

        Health health;
        float baseIntensity;
        Coroutine flicker;

        public bool IsDown { get; private set; }

        void Awake()
        {
            health = GetComponent<Health>();
            if (lamp != null) baseIntensity = lamp.intensity;
        }

        void OnEnable() => health.Died += Fall;

        void OnDisable() => health.Died -= Fall;

        void Fall()
        {
            if (IsDown) return;
            IsDown = true;
            StartCoroutine(Tip());
        }

        IEnumerator Tip()
        {
            Vector3 flat = Vector3.ProjectOnPlane(fallDirection, Vector3.up).normalized;
            Vector3 axis = Vector3.Cross(Vector3.up, flat);
            Quaternion from = pivot.rotation;
            Quaternion to = Quaternion.AngleAxis(88f, axis) * from;
            for (float t = 0f; t < fallTime; t += Time.deltaTime)
            {
                // Cai acelerando, como um poste de verdade.
                float k = t / fallTime;
                pivot.rotation = Quaternion.Slerp(from, to, k * k);
                yield return null;
            }
            pivot.rotation = to;

            // Caído, não atrapalha a passagem nem segura mais tiros.
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Vector3 at = lamp != null ? lamp.transform.position : pivot.position;
            Noise.Emit(at, crashNoise, gameObject);
            if (crashSound != null) crashSound.Play();
            yield return Blink(0.6f, fallenBrightness);
        }

        /// <summary>Pisca por alguns segundos e volta ao brilho de antes.</summary>
        public void Flicker(float seconds)
        {
            if (lamp == null || !isActiveAndEnabled) return;
            if (flicker != null) StopCoroutine(flicker);
            flicker = StartCoroutine(Blink(seconds, IsDown ? fallenBrightness : 1f));
        }

        IEnumerator Blink(float seconds, float settleAt)
        {
            if (lamp == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                lamp.intensity = baseIntensity * (Random.value < 0.35f ? 0.1f : Random.Range(0.6f, 1.1f));
                yield return null;
            }
            lamp.intensity = baseIntensity * settleAt;
            flicker = null;
        }
    }
}
