using UnityEngine;

namespace Nibiru.Environment
{
    /// <summary>
    /// Liga a tempestade de areia: a intensidade sobe conforme a Elena anda para o norte, do topo da
    /// rampa para dentro do wadi (e desce se ela voltar), pelo z dela. Ajusta a névoa e o vento; os
    /// efeitos na jogabilidade estão em <see cref="Storm"/>. Fica no grupo do wadi, que só liga depois
    /// de a passagem abrir. Ao desligar, devolve a névoa de antes.
    /// </summary>
    public class StormController : MonoBehaviour
    {
        [SerializeField] StormSettings settings;

        [Tooltip("Som do vento, tocado em loop (volume pela intensidade da tempestade).")]
        [SerializeField] AudioSource wind;

        [Tooltip("Força a intensidade (de 0 a 1) em vez de seguir a Elena. Menos que zero = automático. Para testes.")]
        [SerializeField, Range(-1f, 1f)] float forcedIntensity = -1f;

        Transform player;
        float intensity;

        bool savedFog;
        Color savedFogColor;
        float savedFogDensity;
        FogMode savedFogMode;

        public float Intensity => intensity;

        /// <summary>Força a tempestade cheia (ou o que for) até <see cref="ReleaseForce"/>.</summary>
        public void Force(float value) => forcedIntensity = Mathf.Clamp01(value);

        public void ReleaseForce() => forcedIntensity = -1f;

        void OnEnable()
        {
            savedFog = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogDensity = RenderSettings.fogDensity;
            savedFogMode = RenderSettings.fogMode;
            if (wind != null) wind.Play();
            intensity = Target();
            Apply();
        }

        void OnDisable()
        {
            Storm.Set(null, 0f);
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogDensity = savedFogDensity;
            RenderSettings.fogMode = savedFogMode;
        }

        void Update()
        {
            intensity = Mathf.MoveTowards(intensity, Target(), Time.deltaTime / Mathf.Max(0.1f, settings.fadeSeconds));
            Apply();
        }

        float Target()
        {
            if (forcedIntensity >= 0f) return forcedIntensity;
            if (player == null)
            {
                GameObject found = GameObject.FindWithTag("Player");
                if (found == null) return 0f;
                player = found.transform;
            }
            return Mathf.InverseLerp(settings.rampStartZ, settings.rampFullZ, player.position.z);
        }

        void Apply()
        {
            Storm.Set(settings, intensity);
            RenderSettings.fog = intensity > 0.001f || savedFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(savedFogColor, settings.fogColor, intensity);
            RenderSettings.fogDensity = Mathf.Lerp(savedFog ? savedFogDensity : 0f, settings.fogDensity, intensity);
            if (wind != null) wind.volume = settings.windVolume * intensity;
        }
    }
}
