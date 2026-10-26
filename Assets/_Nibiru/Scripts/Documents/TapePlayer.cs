using System.Collections;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Toca as fitas gravadas: legenda no pé da tela e, quando houver, a voz, com o chiado da fita por
    /// baixo. Não pausa o jogo nem trava o controle: a Elena segue andando. Fica num objeto que não
    /// desliga (não num grupo que some ao sair de uma área), para a fita não ser cortada no meio.
    /// Se a Elena voltar de um ponto de retorno guardado com a fita já pega mas ainda não terminada,
    /// ela continua no começo da linha em que estava (a linha fica em <see cref="WorldState"/>, chave "tape.&lt;id&gt;").
    /// Fita terminada não toca de novo. Outros sistemas que não podem cortar a fita (o rádio do Kast no
    /// passo 14) esperam por <see cref="IsPlaying"/> ou pelo evento de fim da fita.
    /// </summary>
    public class TapePlayer : MonoBehaviour
    {
        [Tooltip("A fita desta cena. Se já foi pega e não terminou (volta de um checkpoint), toca de novo ao carregar.")]
        [SerializeField] TapeDefinition resumeOnLoad;

        [Tooltip("Chiado de fita, tocado enquanto a fita fala (opcional, 2D).")]
        [SerializeField] AudioSource hiss;

        [Tooltip("Onde a voz sai (opcional). Vazio = um som 2D criado sozinho.")]
        [SerializeField] AudioSource voice;

        [Tooltip("Segundos de espera antes da primeira fala.")]
        [SerializeField, Min(0f)] float startDelay = TapeDefinition.StartDelaySeconds;

        SubtitleUI subtitles;
        Coroutine running;

        /// <summary>True enquanto alguma fita está tocando (inclusive com o jogo pausado no meio dela).</summary>
        public bool IsPlaying => running != null;

        public TapeDefinition Current { get; private set; }

        /// <summary>Chave do <see cref="WorldState"/> com a linha em que a fita está (some quando ela termina).</summary>
        public static string ProgressKey(TapeDefinition tape) => "tape." + tape.Id;

        void Start()
        {
            if (resumeOnLoad == null || !resumeOnLoad.IsCollected || resumeOnLoad.IsFinished) return;
            // Volta no começo da linha em que o ponto de retorno foi guardado.
            bool saved = WorldState.TryGetSceneValue(ProgressKey(resumeOnLoad), out int line);
            Play(resumeOnLoad, saved ? line : 0, saved);
        }

        public void Play(TapeDefinition tape) => Play(tape, 0, false);

        void Play(TapeDefinition tape, int firstLine, bool resuming)
        {
            if (tape == null || tape.lines.Length == 0) return;
            if (running != null) StopCoroutine(running);
            Current = tape;
            running = StartCoroutine(Run(tape, Mathf.Clamp(firstLine, 0, tape.lines.Length - 1), resuming));
        }

        IEnumerator Run(TapeDefinition tape, int firstLine, bool resuming)
        {
            if (subtitles == null)
            {
                var go = new GameObject("Legendas");
                go.transform.SetParent(transform, false);
                subtitles = go.AddComponent<SubtitleUI>();
            }
            if (hiss != null) hiss.Play();
            if (tape.voice != null)
            {
                if (voice == null)
                {
                    voice = gameObject.AddComponent<AudioSource>();
                    voice.spatialBlend = 0f;
                    voice.playOnAwake = false;
                }
                voice.clip = tape.voice;
                voice.Play();
            }

            // Tempo do jogo: se um menu ou uma conversa pausar, a fita espera junto.
            if (!resuming) yield return new WaitForSeconds(startDelay);
            for (int i = firstLine; i < tape.lines.Length; i++)
            {
                TapeDefinition.Line line = tape.lines[i];
                WorldState.SetSceneValue(ProgressKey(tape), i);
                subtitles.Show(tape.LocalizedSpeakerLabel, line.LocalizedText);
                yield return new WaitForSeconds(tape.SecondsFor(line));
                subtitles.Hide();
                yield return new WaitForSeconds(tape.pauseBetweenLines);
            }

            if (hiss != null) hiss.Stop();
            Current = null;
            running = null;
            StoryEvents.Raise(tape.raiseOnFinished);
            WorldState.RemoveSceneValue(ProgressKey(tape));
        }

        void OnDisable()
        {
            // Só acontece ao sair da cena: a fita volta do começo no próximo carregamento (veja Start).
            running = null;
            if (subtitles != null) subtitles.Hide();
        }
    }
}
