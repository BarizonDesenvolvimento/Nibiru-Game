using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Nibiru.Testes
{
    /// <summary>
    /// Vitrine de um clipe solto: toca o clipe no Animator do modelo (sem controlador), em laço. Os que
    /// não são de laço recomeçam depois de uma pausa. Só para conferir animações no Play.
    /// </summary>
    public class ClipLoopPlayer : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] AnimationClip clip;
        [SerializeField] float pauseBetweenRepeats = 1f;

        PlayableGraph graph;
        AnimationClipPlayable playable;
        float restartAt;

        public AnimationClip Clip => clip;

        void Reset() => animator = GetComponentInChildren<Animator>();

        // No Start (e não no OnEnable): o editor põe o clipe depois de adicionar o componente.
        void Start()
        {
            if (animator == null || clip == null) return;
            animator.runtimeAnimatorController = null;
            graph = PlayableGraph.Create("Vitrine_" + clip.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Animacao", animator);
            playable = AnimationClipPlayable.Create(graph, clip);
            output.SetSourcePlayable(playable);
            graph.Play();
        }

        void Update()
        {
            if (!graph.IsValid() || clip.isLooping) return;
            if (playable.GetTime() >= clip.length)
            {
                if (restartAt <= 0f) restartAt = Time.time + pauseBetweenRepeats;
                if (Time.time >= restartAt)
                {
                    playable.SetTime(0);
                    restartAt = 0f;
                }
            }
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }

    /// <summary>
    /// Vitrine de um personagem com o controlador de verdade: a cada etapa muda um parâmetro (a
    /// velocidade, ou um gatilho como Golpe, Dano, Urro, Morte) e espera. Depois da última etapa
    /// recomeça do zero. Só para conferir os controladores no Play.
    /// </summary>
    public class ControllerShowcase : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            public string label;
            public string parameter;
            public bool trigger;
            public float value;
            public float seconds = 2.5f;
            [Tooltip("Reinicia o Animator antes da etapa (depois da morte).")]
            public bool resetBefore;
        }

        [SerializeField] Animator animator;
        [SerializeField] Step[] steps = new Step[0];
        [SerializeField] string idleState = "Locomocao";

        int index = -1;
        float nextAt;
        public string Label { get; private set; } = "";

        void Reset() => animator = GetComponentInChildren<Animator>();

        void Update()
        {
            if (animator == null || steps.Length == 0 || Time.time < nextAt) return;
            index = (index + 1) % steps.Length;
            var step = steps[index];
            if (step.resetBefore)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.Play(idleState, 0, 0f);
            }
            Label = step.label;
            if (!string.IsNullOrEmpty(step.parameter))
            {
                if (step.trigger) animator.SetTrigger(step.parameter);
                else animator.SetFloat(step.parameter, step.value);
            }
            nextAt = Time.time + step.seconds;
        }
    }

    /// <summary>Escreve na tela o que a vitrine está fazendo (só para conferir animações).</summary>
    public class ShowcaseLabel : MonoBehaviour
    {
        TextMesh text;
        ControllerShowcase showcase;

        void Start()
        {
            showcase = GetComponent<ControllerShowcase>();
            text = new GameObject("Etapa").AddComponent<TextMesh>();
            text.transform.SetParent(transform, false);
            text.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            text.characterSize = 0.03f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.yellow;
        }

        void Update()
        {
            if (text != null) text.text = showcase.Label;
        }
    }
}
