using System.Collections;
using Nibiru.Quests;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Environment
{
    /// <summary>
    /// A passagem dos Guardiões atrás da parede do barco de junco (passo 13). Quando a Elena lê a
    /// inscrição, <see cref="BeginOpening"/> faz a tabuleta pulsar numa luz azul sobre a inscrição e
    /// então a parede afunda no chão; só no fim dispara o evento "abriu". Ao voltar de um ponto de
    /// retorno depois disso, <see cref="SnapOpen"/> a põe direto aberta. As lamparinas dos Guardiões
    /// ficam acesas, com a chama tremendo. Os valores vêm de <see cref="GuardianPassageSettings"/>.
    /// </summary>
    public class GuardianPassage : MonoBehaviour
    {
        [SerializeField] GuardianPassageSettings settings;

        [Tooltip("A parede que afunda (com o barco e as ondas como filhos).")]
        [SerializeField] Transform wall;

        [Tooltip("Luz que pulsa na inscrição quando a tabuleta reage.")]
        [SerializeField] Light pulseLight;

        [Tooltip("As lamparinas acesas da passagem.")]
        [SerializeField] Light[] lamps = new Light[0];

        [Tooltip("Som ambiente do túnel (vento e pingos, em loop).")]
        [SerializeField] AudioSource ambience;

        [Tooltip("Som da pedra correndo, tocado quando a parede começa a descer.")]
        [SerializeField] AudioSource wallSound;

        [Tooltip("Poeira que sobe quando a parede desce (opcional).")]
        [SerializeField] ParticleSystem dust;

        [Tooltip("Disparado quando a parede termina de abrir (completa o passo da missão).")]
        [SerializeField] StoryEvent raiseWhenOpen;

        [Tooltip("Quando a parede começa a descer (ao vivo).")]
        [SerializeField] UnityEvent onOpeningStarted = new UnityEvent();

        Vector3 closedPosition;
        float[] lampSeeds;
        Coroutine opening;

        public bool IsOpening => opening != null;
        public bool IsOpen { get; private set; }
        public Transform Wall => wall;

        void Awake()
        {
            if (wall != null) closedPosition = wall.position;
            lampSeeds = new float[lamps.Length];
            for (int i = 0; i < lampSeeds.Length; i++) lampSeeds[i] = Random.value * 100f;
            if (pulseLight != null) pulseLight.intensity = 0f;
            if (pulseLight != null) pulseLight.enabled = false;
            ApplyLampSettings();
            if (ambience != null && settings != null) ambience.volume = settings.tunnelAmbienceVolume;
        }

        void ApplyLampSettings()
        {
            if (settings == null) return;
            foreach (Light lamp in lamps)
            {
                if (lamp == null) continue;
                lamp.color = settings.lampColor;
                lamp.range = settings.lampRange;
            }
        }

        // A chama treme com ruído suave. Tempo sem escala: continua durante uma conversa.
        void Update()
        {
            if (settings == null) return;
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] == null) continue;
                float noise = (Mathf.PerlinNoise(Time.unscaledTime * 5f, lampSeeds[i]) - 0.5f) * 2f * settings.lampFlicker;
                lamps[i].intensity = settings.lampIntensity * (1f + noise);
            }
        }

        /// <summary>Faz a tabuleta pulsar e depois abre a parede. Chamado quando a Elena termina de ler a inscrição.</summary>
        public void BeginOpening()
        {
            if (IsOpen || opening != null || wall == null) return;
            opening = StartCoroutine(Open());
        }

        IEnumerator Open()
        {
            if (pulseLight != null && settings != null)
            {
                pulseLight.color = settings.tabletPulseColor;
                pulseLight.enabled = true;
                for (int pulse = 0; pulse < settings.tabletPulses; pulse++)
                {
                    for (float t = 0f; t < settings.tabletPulseSeconds; t += Time.unscaledDeltaTime)
                    {
                        // Sobe e desce, como um batimento.
                        float k = Mathf.Sin(t / settings.tabletPulseSeconds * Mathf.PI);
                        pulseLight.intensity = settings.tabletPulseIntensity * k * k;
                        yield return null;
                    }
                }
                pulseLight.intensity = 0f;
                pulseLight.enabled = false;
            }

            onOpeningStarted.Invoke();
            if (wallSound != null) wallSound.Play();
            if (dust != null) dust.Play();

            float seconds = settings != null ? settings.wallOpenSeconds : 4f;
            float depth = settings != null ? settings.wallSinkDepth : 4.4f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                wall.position = closedPosition + Vector3.down * (depth * Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Finish();
            opening = null;
            StoryEvents.Raise(raiseWhenOpen);
        }

        /// <summary>Põe a parede direto aberta, sem pulso nem som (volta de um ponto de retorno).</summary>
        public void SnapOpen()
        {
            if (opening != null)
            {
                StopCoroutine(opening);
                opening = null;
            }
            Finish();
        }

        void Finish()
        {
            IsOpen = true;
            if (pulseLight != null) pulseLight.enabled = false;
            // Aberta, a parede inteira sai de cena (colisão e desenho): nada fica no caminho.
            if (wall != null) wall.gameObject.SetActive(false);
        }
    }
}
