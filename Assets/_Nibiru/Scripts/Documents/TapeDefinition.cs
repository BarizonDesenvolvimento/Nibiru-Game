using System;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Uma fita gravada que a Elena encontra no cenário (as fitas de Irena). Cada uma é um arquivo
    /// destes em Data/Tapes. Ao pegar, toca como áudio com legenda, sem travar o controle; não tem
    /// menu. Pegar e terminar de ouvir ficam registrados como eventos da história, para a futura
    /// tela de documentos e fitas (a mesma da foto de 1979) saber o que já foi coletado.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Fita", fileName = "NovaFita")]
    public class TapeDefinition : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [TextArea(1, 4)] public string text;
        }

        [Tooltip("Nome da fita, mostrado ao pegar e acima da legenda.")]
        public string displayName = "Fita";

        [Tooltip("Quem fala e quando, mostrado antes da legenda (por exemplo \"Irena, 1979\").")]
        public string speakerLabel = "";

        [Tooltip("As falas, em ordem. Cada uma fica na tela pelo tempo calculado abaixo.")]
        public Line[] lines = new Line[0];

        [Tooltip("Voz gravada (opcional). Enquanto não há voz, só a legenda e o chiado da fita.")]
        public AudioClip voice;

        [Header("Tempo da legenda (provisório, até haver voz gravada)")]
        [Tooltip("Segundos de legenda por letra.")]
        [Min(0.01f)] public float secondsPerCharacter = 0.06f;

        [Tooltip("Tempo mínimo de cada fala, em segundos.")]
        [Min(0f)] public float minSecondsPerLine = 2.4f;

        [Tooltip("Pausa entre uma fala e a seguinte, em segundos.")]
        [Min(0f)] public float pauseBetweenLines = 0.8f;

        [Header("Registro")]
        [Tooltip("Disparado ao pegar a fita. É o registro de que ela foi coletada (a tela de fitas lê daqui).")]
        public StoryEvent raiseOnCollected;

        [Tooltip("Disparado quando a fita termina de tocar. O que não pode cortar a fita (como o rádio do Kast no wadi) espera por este evento.")]
        public StoryEvent raiseOnFinished;

        public bool IsCollected => StoryEvents.HasBeenRaised(raiseOnCollected);

        public bool IsFinished => StoryEvents.HasBeenRaised(raiseOnFinished);

        public float SecondsFor(Line line) =>
            Mathf.Max(minSecondsPerLine, (line.text?.Length ?? 0) * secondsPerCharacter);

        /// <summary>Duração total da fita, em segundos.</summary>
        public float TotalSeconds
        {
            get
            {
                float total = 0f;
                foreach (Line line in lines) total += SecondsFor(line) + pauseBetweenLines;
                return total;
            }
        }
    }
}
