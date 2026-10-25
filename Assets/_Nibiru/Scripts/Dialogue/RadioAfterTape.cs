using System.Collections;
using Nibiru.Documents;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Dialogue
{
    /// <summary>
    /// Rádio do Kast no wadi (passo 14): começa a conversa quando a Elena chega (<see cref="Begin"/>),
    /// mas nunca por cima de uma fita de Irena tocando. Se a fita está tocando, espera e começa assim que
    /// ela termina, sem a Elena precisar voltar ao gatilho. Se a fita nunca foi pega, ou já terminou (inclusive
    /// num jogo carregado de depois dela), começa na hora. Nunca espera para sempre: o teto é a duração
    /// da fita (a voz gravada, ou o tempo das legendas enquanto não há voz) mais uma margem, contado em tempo
    /// de jogo (com o jogo pausado, o relógio não anda).
    /// </summary>
    public class RadioAfterTape : MonoBehaviour
    {
        [Tooltip("A conversa do rádio.")]
        [SerializeField] DialogueDefinition dialogue;

        [Tooltip("Conversa usada no lugar da primeira se o evento abaixo já aconteceu (opcional).")]
        [SerializeField] DialogueDefinition alternateDialogue;
        [SerializeField] StoryEvent alternateWhenRaised;

        [Tooltip("A fita que o rádio não pode cortar.")]
        [SerializeField] TapeDefinition tape;
        [Tooltip("Quem toca a fita. Vazio = procura sozinho na cena (o gravador fica na passagem dos Guardiões).")]
        [SerializeField] TapePlayer tapePlayer;

        [Tooltip("Margem de segurança sobre a duração da fita, em segundos de jogo.")]
        [SerializeField, Min(0f)] float safetyMarginSeconds = 5f;

        [Tooltip("Disparado quando o rádio começa a falar. Se já aconteceu (jogo carregado de depois), o rádio não repete.")]
        [SerializeField] StoryEvent raiseOnStart;

        [SerializeField] DialogueRunner runner;

        Coroutine waiting;

        /// <summary>True enquanto o rádio espera a fita terminar.</summary>
        public bool IsWaiting => waiting != null;

        public bool HasStarted => StoryEvents.HasBeenRaised(raiseOnStart);

        /// <summary>Segundos de espera no máximo: duração da fita mais a margem.</summary>
        public float MaxWaitSeconds => (tape != null ? tape.PlaybackSeconds : 0f) + safetyMarginSeconds;

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
        }

        /// <summary>Chamado quando a Elena chega ao soldado. Pode ser chamado de novo sem efeito.</summary>
        public void Begin()
        {
            if (HasStarted || IsWaiting) return;
            if (tapePlayer == null) tapePlayer = FindFirstObjectByType<TapePlayer>(FindObjectsInactive.Include);
            waiting = StartCoroutine(WaitThenStart());
        }

        IEnumerator WaitThenStart()
        {
            float waited = 0f;
            float limit = MaxWaitSeconds;
            while (TapeIsPlaying && waited < limit)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            // Outra conversa em andamento também segura o rádio (a fala dele nunca atropela nada).
            while (runner.IsRunning) yield return null;
            waiting = null;
            StartRadio();
        }

        // A fita só segura o rádio se foi pega, ainda não terminou e está de fato tocando.
        bool TapeIsPlaying =>
            tape != null && tapePlayer != null && tape.IsCollected && !tape.IsFinished && tapePlayer.IsPlaying;

        void StartRadio()
        {
            DialogueDefinition chosen = alternateDialogue != null && StoryEvents.HasBeenRaised(alternateWhenRaised)
                ? alternateDialogue
                : dialogue;
            StoryEvents.Raise(raiseOnStart);
            runner.Play(chosen);
        }

        void OnDisable() => waiting = null;
    }
}
