using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>Mostra as mensagens de Notifications empilhadas, cada uma por alguns segundos.</summary>
    public class NotificationUI : MonoBehaviour
    {
        [SerializeField] Text label;

        [Tooltip("Quanto tempo cada mensagem fica na tela, em segundos.")]
        [SerializeField, Min(0.1f)] float duration = 2.5f;

        [Tooltip("Máximo de mensagens visíveis ao mesmo tempo.")]
        [SerializeField, Min(1)] int maxVisible = 4;

        struct Entry
        {
            public string text;
            public float expiresAt;
        }

        readonly List<Entry> entries = new List<Entry>();
        readonly StringBuilder builder = new StringBuilder();

        void OnEnable()
        {
            Notifications.Posted += Show;
            label.text = string.Empty;
        }

        void OnDisable() => Notifications.Posted -= Show;

        void Show(string message)
        {
            // Tempo sem escala: as mensagens continuam sumindo mesmo com o jogo pausado.
            entries.Add(new Entry { text = message, expiresAt = Time.unscaledTime + duration });
            while (entries.Count > maxVisible) entries.RemoveAt(0);
            Rebuild();
        }

        void Update()
        {
            if (entries.RemoveAll(e => e.expiresAt <= Time.unscaledTime) > 0)
                Rebuild();
        }

        void Rebuild()
        {
            builder.Clear();
            foreach (Entry entry in entries) builder.AppendLine(entry.text);
            label.text = builder.ToString();
        }
    }
}
