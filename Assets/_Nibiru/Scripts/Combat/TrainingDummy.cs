using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Alvo para testar golpes. Pisca ao levar dano, tomba quando a vida acaba
    /// e se levanta de novo depois de alguns segundos, com a vida cheia.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [Tooltip("Renderer que pisca ao levar dano.")]
        [SerializeField] Renderer body;

        [SerializeField] Color hitColor = new Color(1f, 0.25f, 0.2f);

        [Tooltip("Quanto tempo a cor de dano fica, em segundos.")]
        [SerializeField, Min(0.01f)] float flashTime = 0.15f;

        [Tooltip("Tempo caído antes de se levantar, em segundos.")]
        [SerializeField, Min(0f)] float respawnDelay = 3f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Health health;
        MaterialPropertyBlock block;
        Color baseColor;
        Vector3 uprightPosition;
        Quaternion uprightRotation;
        float flashUntil;
        float respawnAt = -1f;

        void Awake()
        {
            health = GetComponent<Health>();
            if (body == null) body = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            baseColor = body.sharedMaterial.GetColor(BaseColorId);
            uprightPosition = transform.position;
            uprightRotation = transform.rotation;
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info) => flashUntil = Time.time + flashTime;

        // Por enquanto só tomba de lado; inimigos de verdade terão animação.
        void OnDied()
        {
            transform.SetPositionAndRotation(
                uprightPosition + Vector3.down * 0.6f,
                uprightRotation * Quaternion.Euler(0f, 0f, 85f));
            respawnAt = Time.time + respawnDelay;
        }

        void Update()
        {
            block.SetColor(BaseColorId, Time.time < flashUntil ? hitColor : baseColor);
            body.SetPropertyBlock(block);

            if (respawnAt >= 0f && Time.time >= respawnAt)
            {
                respawnAt = -1f;
                transform.SetPositionAndRotation(uprightPosition, uprightRotation);
                health.Restore(health.Max);
            }
        }
    }
}
