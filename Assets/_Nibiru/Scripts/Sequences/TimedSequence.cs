using System;
using System.Collections;
using Nibiru.Quests;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.Sequences
{
    /// <summary>
    /// Cena roteirizada simples: uma lista de passos "espere e faça", configurada no Inspector.
    /// Serve para encadear acontecimentos (o helicóptero chega, os soldados descem, um tiro...)
    /// sem escrever código novo. Toca uma vez por partida.
    /// </summary>
    public class TimedSequence : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            [Tooltip("Só para quem edita: o que este passo faz.")]
            public string note;

            [Tooltip("Segundos de espera antes deste passo.")]
            [Min(0f)] public float wait;

            [Tooltip("Se preenchido, o passo não espera o tempo todo: segue assim que este evento acontecer (ou na hora, se já aconteceu).")]
            public StoryEvent orUntil;

            [Tooltip("O que acontece neste passo.")]
            public UnityEvent action = new UnityEvent();
        }

        [Tooltip("Começa sozinha quando a cena carrega.")]
        [SerializeField] bool playOnStart;

        [Tooltip("Não toca se este evento já aconteceu (por exemplo, ao voltar de um checkpoint depois da cena).")]
        [SerializeField] StoryEvent skipIfRaised;

        [SerializeField] Step[] steps = new Step[0];

        bool played;

        public bool IsPlaying { get; private set; }

        void Start()
        {
            if (playOnStart) Play();
        }

        public void Play()
        {
            if (played || StoryEvents.HasBeenRaised(skipIfRaised)) return;
            played = true;
            StartCoroutine(Run());
        }

        // Usa o tempo do jogo: com o jogo pausado, a sequência também para.
        IEnumerator Run()
        {
            IsPlaying = true;
            foreach (Step step in steps)
            {
                float end = Time.time + step.wait;
                while (Time.time < end && !StoryEvents.HasBeenRaised(step.orUntil)) yield return null;
                step.action.Invoke();
            }
            IsPlaying = false;
        }
    }
}
