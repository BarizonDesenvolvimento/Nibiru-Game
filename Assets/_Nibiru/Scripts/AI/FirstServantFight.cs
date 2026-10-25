using System.Collections;
using Nibiru.Combat;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.AI
{
    /// <summary>
    /// A luta com o Primeiro Servo, por cima da LulluAI dele: acorda urrando, troca de fase pela vida
    /// (com sinal claro: urro parado, máscara rachando em lascas de ouro, olhos mais acesos, o Selo
    /// clareando e os holofotes piscando), muda os ajustes da IA em cada fase, mostra a barra de vida
    /// no alto da tela e, quando ele cai, enfraquece o azul do Selo e dispara o evento da vitória.
    /// Valores em Data/AI/PrimeiroServo_Luta (vida e fases) e Data/AI/PrimeiroServo (golpes e tempos).
    /// </summary>
    [RequireComponent(typeof(LulluAI), typeof(Health))]
    public class FirstServantFight : MonoBehaviour
    {
        [SerializeField] FirstServantSettings settings;

        [Tooltip("Acorda sozinho ao começar (no laboratório). Na região, quem acorda é a cena depois do flash.")]
        [SerializeField] bool awakeOnStart;

        [Tooltip("Disparado quando ele cai (a vitória).")]
        [SerializeField] StoryEvent raiseOnDefeat;

        [Header("Sinais")]
        [SerializeField] AudioSource roarSound;
        [SerializeField] AudioSource crackSound;
        [Tooltip("Luzes do Selo: clareiam a cada troca de fase e enfraquecem quando ele cai.")]
        [SerializeField] Light[] sealLights = new Light[0];
        [SerializeField] FallingFloodlight[] floodlights = new FallingFloodlight[0];
        [Tooltip("Material das lascas da máscara (ouro).")]
        [SerializeField] Material shardMaterial;
        [SerializeField] Color eyeColor = new Color(0.3f, 0.5f, 1f);

        [Tooltip("Brilho do Selo que sobra depois da vitória, em fração do original.")]
        [SerializeField, Range(0f, 1f)] float defeatedSealBrightness = 0.3f;

        LulluAI brain;
        Health health;
        LulluAnimator anim;
        Transform head;
        Light eyes;
        float[] sealBase;
        GameObject bar;
        RectTransform barFill;
        Coroutine roaring;

        public int Phase { get; private set; }
        public bool IsAwake { get; private set; }
        public bool IsRoaring => roaring != null;
        public bool IsDefeated { get; private set; }

        void Awake()
        {
            brain = GetComponent<LulluAI>();
            health = GetComponent<Health>();
            anim = GetComponent<LulluAnimator>();
            if (settings != null) health.SetMax(settings.maxHealth);
            var animator = GetComponentInChildren<Animator>();
            head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : transform;
            sealBase = new float[sealLights.Length];
            for (int i = 0; i < sealLights.Length; i++) sealBase[i] = sealLights[i] != null ? sealLights[i].intensity : 0f;
            BuildEyes();
        }

        void OnEnable() => health.Died += OnDefeated;

        void OnDisable() => health.Died -= OnDefeated;

        void Start()
        {
            if (awakeOnStart) WakeQuiet();
            else if (!IsAwake && !IsDefeated) brain.Pause(true);
        }

        void Update()
        {
            if (!IsAwake || IsDefeated) return;
            if (bar != null) barFill.anchorMax = new Vector2(health.Fraction, 1f);
            if (roaring != null) return;
            int wanted = health.Fraction <= settings.phase3At ? 3 : health.Fraction <= settings.phase2At ? 2 : 1;
            // Uma fase por vez: cada uma tem o seu urro.
            if (wanted > Phase) roaring = StartCoroutine(ChangePhase(Phase + 1));
        }

        // ---------- Acordar ----------

        /// <summary>Acorda ao vivo: urra parado, o Selo clareia, e então caça a Elena.</summary>
        public void Wake()
        {
            if (IsAwake || IsDefeated) return;
            IsAwake = true;
            ApplyPhase(1);
            ShowBar();
            roaring = StartCoroutine(Roar(settings.wakeHint, shards: 0));
        }

        /// <summary>Já acordado, sem urro (ao voltar de um checkpoint durante a luta): vagueia perto do Selo.</summary>
        public void WakeQuiet()
        {
            if (IsAwake || IsDefeated) return;
            IsAwake = true;
            ApplyPhase(1);
            brain.Pause(false);
            ShowBar();
        }

        /// <summary>Já caído diante do Selo (ao voltar de um checkpoint depois da vitória).</summary>
        public void LieDefeated()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            if (anim != null) anim.LieDead();
            health.TakeDamage(new DamageInfo(health.Max * 100f, transform.position));
            SetSeal(defeatedSealBrightness);
            if (eyes != null) eyes.enabled = false;
        }

        /// <summary>Só para os atalhos de teste: começa nesta fase, sem urro.</summary>
        public void StartAtPhase(int phase)
        {
            if (!IsAwake) WakeQuiet();
            ApplyPhase(Mathf.Clamp(phase, 1, 3));
        }

        // ---------- Fases ----------

        IEnumerator ChangePhase(int phase)
        {
            ApplyPhase(phase);
            // A máscara racha: lascas de ouro caindo do rosto (mais na fase 3) e o som da rachadura.
            if (crackSound != null) crackSound.Play();
            yield return Roar(phase == 2 ? settings.phase2Hint : null, shards: phase == 2 ? 6 : 12);
        }

        IEnumerator Roar(string hint, int shards)
        {
            brain.Pause(true);
            if (roarSound != null) roarSound.Play();
            for (int i = 0; i < shards; i++) Shard();
            foreach (FallingFloodlight light in floodlights)
                if (light != null) light.Flicker(settings.roarDuration);
            if (!string.IsNullOrEmpty(hint)) Notifications.Post(hint);

            for (float t = 0f; t < settings.roarDuration; t += Time.deltaTime)
            {
                // O Selo clareia e volta, no ritmo do urro.
                SetSeal(1f + 1.5f * Mathf.Sin(Mathf.PI * t / settings.roarDuration));
                yield return null;
            }
            SetSeal(1f);
            roaring = null;
            if (IsDefeated) yield break;
            brain.Pause(false);
            brain.HuntPlayer();
        }

        void ApplyPhase(int phase)
        {
            Phase = phase;
            var p = phase == 3 ? settings.phase3 : phase == 2 ? settings.phase2 : settings.phase1;
            brain.WindupScale = p.windupScale;
            brain.RecoveryScale = p.recoveryScale;
            brain.SpeedScale = p.speedScale;
            brain.LungeChain = p.lungesInARow;
            brain.MaxClimb = p.climbs ? 0f : settings.groundClimb;
            // Olhos de lápis-lazúli mais acesos a cada fase.
            if (eyes != null) eyes.intensity = phase == 3 ? 6f : phase == 2 ? 3.5f : 2f;
        }

        // ---------- Vitória ----------

        void OnDefeated()
        {
            bool wasDefeated = IsDefeated;
            IsDefeated = true;
            if (roaring != null) StopCoroutine(roaring);
            roaring = null;
            HideBar();
            if (wasDefeated) return;
            if (eyes != null) eyes.enabled = false;
            StartCoroutine(DimSeal());
            StoryEvents.Raise(raiseOnDefeat);
        }

        IEnumerator DimSeal()
        {
            const float time = 3f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                SetSeal(Mathf.Lerp(1f, defeatedSealBrightness, t / time));
                yield return null;
            }
            SetSeal(defeatedSealBrightness);
        }

        // ---------- Visual ----------

        void SetSeal(float factor)
        {
            for (int i = 0; i < sealLights.Length; i++)
                if (sealLights[i] != null) sealLights[i].intensity = sealBase[i] * factor;
        }

        // Os olhos de lápis-lazúli: um foco azul saindo do rosto para a frente. Curvado, a cabeça dele fica
        // colada no peito; uma luz em volta da cabeça pintaria o corpo todo de azul.
        void BuildEyes()
        {
            var go = new GameObject("Olhos_LapisLazuli");
            go.transform.SetParent(transform, false);
            eyes = go.AddComponent<Light>();
            eyes.type = LightType.Spot;
            eyes.color = eyeColor;
            eyes.range = 5f;
            eyes.spotAngle = 50f;
            eyes.intensity = 1.2f;
            eyes.shadows = LightShadows.None;
        }

        void LateUpdate()
        {
            if (eyes == null || !eyes.enabled) return;
            Vector3 forward = transform.forward;
            eyes.transform.SetPositionAndRotation(head.position + forward * 0.35f, Quaternion.LookRotation(forward + Vector3.down * 0.35f));
        }

        // Uma lasca de ouro da máscara, que cai do rosto e some.
        void Shard()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Lasca_Mascara";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = head.position + Random.insideUnitSphere * 0.15f;
            go.transform.rotation = Random.rotation;
            go.transform.localScale = new Vector3(0.09f, 0.03f, 0.07f);
            if (shardMaterial != null) go.GetComponent<Renderer>().sharedMaterial = shardMaterial;
            var body = go.AddComponent<Rigidbody>();
            body.linearVelocity = transform.forward * Random.Range(0.8f, 2f) + Random.insideUnitSphere * 1.2f + Vector3.up;
            body.angularVelocity = Random.insideUnitSphere * 12f;
            Destroy(go, 4f);
        }

        // Barra de vida no alto da tela, com marcas onde começam as fases 2 e 3.
        void ShowBar()
        {
            if (bar != null) { bar.SetActive(true); return; }
            bar = new GameObject("Barra_Primeiro_Servo");
            bar.transform.SetParent(transform, false);
            var canvas = bar.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = bar.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var back = Rect(bar.transform, "Fundo", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(720f, 16f));
            back.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            barFill = Rect(back, "Vida", Vector2.zero, Vector2.zero, Vector2.zero);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = Vector2.one;
            barFill.offsetMin = new Vector2(2f, 2f);
            barFill.offsetMax = new Vector2(-2f, -2f);
            barFill.pivot = new Vector2(0f, 0.5f);
            barFill.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.38f, 0.9f);
            foreach (float at in new[] { settings.phase2At, settings.phase3At })
            {
                var tick = Rect(back, "Fase", new Vector2(at, 0.5f), Vector2.zero, new Vector2(3f, 22f));
                tick.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.78f, 0.3f);
            }
            var label = Rect(bar.transform, "Nome", new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(720f, 30f));
            var text = label.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.85f, 0.55f);
            text.text = "O Primeiro Servo";
        }

        void HideBar()
        {
            if (bar != null) bar.SetActive(false);
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
