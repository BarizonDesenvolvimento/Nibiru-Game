using System.Collections.Generic;
using System.Text;
using Nibiru.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra as mensagens de Notifications empilhadas, cada uma por alguns segundos. As comuns ficam no
    /// canto; as importantes (mudança de reputação, por exemplo) aparecem no meio da tela, acima da Elena,
    /// maiores e por mais tempo. Durante uma conversa as mensagens esperam e só aparecem quando ela acaba.
    /// </summary>
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] Text label;

        [Tooltip("Quanto tempo cada mensagem fica na tela, em segundos.")]
        [SerializeField, Min(0.1f)] float duration = 2.5f;

        [Tooltip("Máximo de mensagens visíveis ao mesmo tempo.")]
        [SerializeField, Min(1)] int maxVisible = 4;

        [Header("Avisos importantes (no meio da tela)")]
        [Tooltip("Quanto tempo cada aviso importante fica na tela, em segundos.")]
        [SerializeField, Min(0.1f)] float importantDuration = 9f;

        [Tooltip("Quanto o aviso importante leva para aparecer e para sumir (fade), em segundos.")]
        [SerializeField, Min(0f)] float importantFade = 0.6f;

        [Tooltip("Altura do aviso importante na tela (0 = embaixo, 1 = em cima).")]
        [SerializeField, Range(0.3f, 0.9f)] float importantHeight = 0.62f;

        [Tooltip("Quanto o texto importante é maior que o comum.")]
        [SerializeField, Min(1f)] float importantScale = 1.15f;

        class Lane
        {
            public Text label;
            public float duration;
            public float shownAt;
            public readonly List<Entry> entries = new List<Entry>();
        }

        struct Entry
        {
            public string text;
            public float expiresAt;
        }

        Lane common, important;
        DialogueRunner runner;
        readonly StringBuilder builder = new StringBuilder();

        void OnEnable()
        {
            common = new Lane { label = label, duration = duration };
            important ??= BuildImportantLane();
            Notifications.Posted += ShowCommon;
            Notifications.PostedImportant += ShowImportant;
            label.text = string.Empty;
            important.label.text = string.Empty;
        }

        void OnDisable()
        {
            Notifications.Posted -= ShowCommon;
            Notifications.PostedImportant -= ShowImportant;
        }

        // Texto novo, no meio da tela, centralizado, maior e com contorno. Criado do zero (não clonado do
        // texto comum: o clone levaria este próprio componente junto e se multiplicaria sem parar).
        Lane BuildImportantLane()
        {
            var go = new GameObject(label.name + "_Importante", typeof(RectTransform));
            go.transform.SetParent(label.transform.parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, importantHeight);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 200f);

            var text = go.AddComponent<Text>();
            text.font = label.font;
            text.fontStyle = FontStyle.Normal;
            text.color = new Color(1f, 1f, 1f, 0.92f);
            text.supportRichText = true;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = Mathf.RoundToInt(label.fontSize * importantScale);
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // cada aviso numa linha só, sem quebrar
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            return new Lane { label = text, duration = importantDuration };
        }

        void ShowCommon(string message) => Add(common, message);

        void ShowImportant(string message) => Add(important, message);

        void Add(Lane lane, string message)
        {
            // Tempo sem escala: as mensagens continuam sumindo mesmo com o jogo pausado.
            lane.entries.Add(new Entry { text = message, expiresAt = Time.unscaledTime + lane.duration });
            while (lane.entries.Count > maxVisible) lane.entries.RemoveAt(0);
            if (!DialogueOpen) Rebuild(lane);
        }

        bool DialogueOpen
        {
            get
            {
                if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
                return runner != null && runner.IsRunning;
            }
        }

        void Update()
        {
            // Durante uma conversa as mensagens esperam, escondidas e sem contar tempo: o aviso de reputação de
            // uma escolha não pode sumir enquanto a Elena lê a resposta.
            if (DialogueOpen)
            {
                Hold(common);
                Hold(important);
                return;
            }
            Tick(common);
            Tick(important);
        }

        void Hold(Lane lane)
        {
            if (lane.entries.Count == 0) return;
            for (int i = 0; i < lane.entries.Count; i++)
                lane.entries[i] = new Entry { text = lane.entries[i].text, expiresAt = Time.unscaledTime + lane.duration };
            lane.label.text = string.Empty;
        }

        void Tick(Lane lane)
        {
            bool removed = lane.entries.RemoveAll(e => e.expiresAt <= Time.unscaledTime) > 0;
            if (lane == important) Fade(lane);
            // Mostra de novo o que esperou a conversa acabar.
            if (removed || (lane.entries.Count > 0 && lane.label.text.Length == 0)) Rebuild(lane);
        }

        // Aparece e some com um fade: sobe nos primeiros instantes e desce no fim do aviso mais antigo.
        void Fade(Lane lane)
        {
            if (lane.entries.Count == 0 || importantFade <= 0f) { lane.label.canvasRenderer.SetAlpha(1f); return; }
            float earliestEnd = float.MaxValue;
            foreach (Entry entry in lane.entries) earliestEnd = Mathf.Min(earliestEnd, entry.expiresAt);
            float fadeIn = Mathf.Clamp01((Time.unscaledTime - lane.shownAt) / importantFade);
            float fadeOut = Mathf.Clamp01((earliestEnd - Time.unscaledTime) / importantFade);
            lane.label.canvasRenderer.SetAlpha(Mathf.Min(fadeIn, fadeOut));
        }

        void Rebuild(Lane lane)
        {
            if (lane.label.text.Length == 0)
            {
                lane.shownAt = Time.unscaledTime;
                if (lane == important && importantFade > 0f) lane.label.canvasRenderer.SetAlpha(0f);
            }
            builder.Clear();
            foreach (Entry entry in lane.entries) builder.AppendLine(entry.text);
            lane.label.text = builder.ToString();
        }
    }
}
