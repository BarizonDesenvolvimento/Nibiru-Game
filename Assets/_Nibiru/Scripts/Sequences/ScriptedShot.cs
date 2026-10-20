using UnityEngine;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Tiro de cena roteirizada: um risco de luz da arma até o alvo e o som do disparo.
    /// Não causa dano; o que acontece com o alvo fica a cargo da sequência.
    /// </summary>
    public class ScriptedShot : MonoBehaviour
    {
        [Tooltip("De onde sai o tiro (o cano da arma).")]
        [SerializeField] Transform origin;

        [Tooltip("Para onde vai o tiro.")]
        [SerializeField] Transform target;

        [Tooltip("Altura acima do alvo onde o tiro acerta.")]
        [SerializeField] float targetHeight = 1f;

        [SerializeField] LineRenderer tracer;

        [Tooltip("Som do disparo. Com som 3D e alcance grande, ouve-se de longe.")]
        [SerializeField] AudioSource sound;

        [SerializeField, Min(0.01f)] float tracerTime = 0.08f;

        float hideAt = -1f;

        void Awake()
        {
            if (tracer != null) tracer.enabled = false;
        }

        public void Fire()
        {
            if (sound != null) sound.Play();
            if (tracer == null || origin == null || target == null) return;

            tracer.positionCount = 2;
            tracer.SetPosition(0, origin.position);
            tracer.SetPosition(1, target.position + Vector3.up * targetHeight);
            tracer.enabled = true;
            hideAt = Time.time + tracerTime;
        }

        void Update()
        {
            if (hideAt >= 0f && Time.time >= hideAt)
            {
                tracer.enabled = false;
                hideAt = -1f;
            }
        }
    }
}
