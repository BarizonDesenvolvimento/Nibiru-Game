using System;
using System.Collections;
using Nibiru.Cameras;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Interaction;
using Nibiru.Player;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.Sequences
{
    /// <summary>
    /// A fuga na caminhonete do Samir (passo 15): um trilho, sem física de veículo. A Elena entra na
    /// caçamba, a tela escurece, a caminhonete aparece numa estrada provisória e anda por uma lista de
    /// pontos enquanto o Samir conversa ("A estrada", uma versão por escolha do wadi). Dura no mínimo
    /// <see cref="minSeconds"/>; se a conversa for mais longa, a caminhonete chega ao fim do trilho e
    /// espera a última fala. Depois mostra a entrada do diário e, ao fechá-la, dispara o fim da fatia.
    /// A Elena não sai da caminhonete (movimento, interação e combate ficam desligados); a pausa
    /// continua funcionando e congela o trilho. Usa o tempo real, para o trilho andar durante a conversa.
    /// </summary>
    public class TruckRide : MonoBehaviour
    {
        [Serializable]
        public class Variant
        {
            [Tooltip("Esta conversa vale se este evento já aconteceu. A última da lista vale se nenhuma bater.")]
            public StoryEvent when;
            public DialogueDefinition dialogue;
        }

        [Tooltip("Onde a Elena fica durante a viagem (a caçamba).")]
        [SerializeField] Transform elenaSeat;

        [Tooltip("Pontos da estrada, em ordem. A caminhonete aparece no primeiro.")]
        [SerializeField] Transform[] route = new Transform[0];

        [Tooltip("Duração mínima da viagem, em segundos.")]
        [SerializeField, Min(5f)] float minSeconds = 60f;

        [Tooltip("Segundos de estrada antes de a conversa começar.")]
        [SerializeField, Min(0f)] float dialogueDelay = 2f;

        [SerializeField] Variant[] variants = new Variant[0];

        [Serializable]
        public class Followup
        {
            [Tooltip("Esta conversa extra só acontece se este evento já aconteceu.")]
            public StoryEvent when;
            public DialogueDefinition dialogue;
        }

        [Tooltip("Conversas extras, uma depois da outra, depois da conversa da viagem (por exemplo, a pergunta do Samir sobre a Dra. Weiss).")]
        [SerializeField] Followup[] followups = new Followup[0];

        [Tooltip("A estrada e o que a enfeita; liga quando a viagem começa.")]
        [SerializeField] GameObject roadGroup;

        [Tooltip("A entrada do diário mostrada ao chegar.")]
        [SerializeField] DocumentView diary;

        [SerializeField] StoryEvent raiseOnBoard;
        [SerializeField] StoryEvent raiseOnArrive;
        [SerializeField] StoryEvent raiseOnEnd;

        enum State
        {
            Idle,
            Fading,
            Riding,
            Arrived,
            Done,
        }

        State state;
        float elapsed;
        float length;
        float[] cumulative;
        Image fade;
        PauseMenuUI pauseMenu;

        public bool IsBoarded => state != State.Idle;
        public bool IsRiding => state == State.Riding;
        public bool IsDone => state == State.Done;

        /// <summary>De 0 (saída) a 1 (fim do trilho).</summary>
        public float Progress => Mathf.Clamp01(elapsed / minSeconds);

        public float MinSeconds => minSeconds;

        bool Paused => pauseMenu != null && pauseMenu.IsOpen;

        public void Begin() => BeginAt(0f);

        /// <summary>Começa a viagem já adiantada (para os atalhos de teste): 0 = saída, 1 = fim do trilho.</summary>
        public void BeginAt(float fraction)
        {
            if (state != State.Idle || route.Length < 2) return;
            state = State.Fading;
            elapsed = Mathf.Clamp01(fraction) * minSeconds;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var player = FindFirstObjectByType<PlayerMotor>();
            var input = player.GetComponent<PlayerInputReader>();
            pauseMenu = FindFirstObjectByType<PauseMenuUI>();
            var runner = FindFirstObjectByType<DialogueRunner>();
            BuildRoute();

            LockPlayer(player, input);
            StoryEvents.Raise(raiseOnBoard);

            yield return Fade(1f, 0.7f);
            MoveToRoad(player);
            yield return Fade(0f, 0.9f);

            state = State.Riding;
            DialogueDefinition talk = ChooseDialogue();
            bool talkStarted = talk == null;
            var extraTalks = new System.Collections.Generic.Queue<DialogueDefinition>();
            foreach (Followup followup in followups)
                if (followup.dialogue != null && (followup.when == null || StoryEvents.HasBeenRaised(followup.when)))
                    extraTalks.Enqueue(followup.dialogue);
            float sinceStart = 0f;
            float startedLimit = dialogueDelay + 15f;

            while (true)
            {
                // Na conversa a Elena só olha em volta; com a pausa aberta, nem isso.
                input.SetLookWhileLocked(!Paused);
                if (!Paused)
                {
                    float dt = Time.unscaledDeltaTime;
                    elapsed += dt;
                    sinceStart += dt;
                    Place(Progress);
                    if (!talkStarted && sinceStart >= dialogueDelay)
                        talkStarted = runner.Play(talk) || sinceStart > startedLimit;
                    else if (talkStarted && !runner.IsRunning && extraTalks.Count > 0 && runner.Play(extraTalks.Peek()))
                        extraTalks.Dequeue();
                    if (elapsed >= minSeconds && talkStarted && !runner.IsRunning && extraTalks.Count == 0) break;
                }
                yield return null;
            }

            input.SetLookWhileLocked(false);
            state = State.Arrived;
            StoryEvents.Raise(raiseOnArrive);
            yield return WaitUnpaused(1f);

            if (diary != null)
            {
                diary.Show();
                var reading = FindFirstObjectByType<ReadingScreenUI>();
                yield return null;
                yield return null;
                while (reading != null && reading.IsOpen) yield return null;
            }

            state = State.Done;
            StoryEvents.Raise(raiseOnEnd);
        }

        IEnumerator WaitUnpaused(float seconds)
        {
            for (float t = 0f; t < seconds;)
            {
                if (!Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        DialogueDefinition ChooseDialogue()
        {
            foreach (Variant variant in variants)
                if (variant.when != null && StoryEvents.HasBeenRaised(variant.when)) return variant.dialogue;
            return variants.Length > 0 ? variants[variants.Length - 1].dialogue : null;
        }

        // A Elena fica na caçamba sem poder andar, interagir, atirar nem morrer. A pausa continua livre.
        static void LockPlayer(PlayerMotor player, PlayerInputReader input)
        {
            player.enabled = false;
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor != null)
            {
                interactor.enabled = false;
                interactor.Clear();
            }
            input.SetCombatEnabled(false);
            var animator = player.GetComponentInChildren<PlayerAnimator>(true);
            if (animator != null) animator.SetSeated(true);
            var health = player.GetComponent<Health>();
            if (health != null) health.Invincible = true;
        }

        void MoveToRoad(PlayerMotor player)
        {
            // Sem colisão: a câmera não bate na caminhonete nem a Elena é empurrada por ela.
            foreach (Collider c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
            if (roadGroup != null) roadGroup.SetActive(true);
            Place(Progress);

            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetParent(elenaSeat, false);
            // O ponto do banco é o chão da caçamba: a raiz da Elena fica acima dele o quanto o modelo
            // desce até os pés (antes ela ficava 1 m enterrada no chão da caçamba).
            var animator = player.GetComponentInChildren<Animator>();
            float feetBelowRoot = animator != null ? -animator.transform.localPosition.y : 0f;
            player.transform.localPosition = new Vector3(0f, Mathf.Max(0f, feetBelowRoot), 0f);
            player.transform.localRotation = Quaternion.identity;

            var cameraController = FindFirstObjectByType<PlayerCameraController>();
            if (cameraController != null) cameraController.AlignTo(elenaSeat);
        }

        void BuildRoute()
        {
            cumulative = new float[route.Length];
            for (int i = 1; i < route.Length; i++)
                cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1].position, route[i].position);
            length = cumulative[route.Length - 1];
        }

        // Põe a caminhonete a "fraction" do comprimento do trilho, virada na direção da estrada.
        void Place(float fraction)
        {
            float distance = fraction * length;
            int i = 1;
            while (i < route.Length - 1 && cumulative[i] < distance) i++;
            float segment = Mathf.Max(0.001f, cumulative[i] - cumulative[i - 1]);
            float t = Mathf.Clamp01((distance - cumulative[i - 1]) / segment);
            Vector3 a = route[i - 1].position;
            Vector3 b = route[i].position;
            transform.position = Vector3.Lerp(a, b, t);
            Vector3 flat = Vector3.ProjectOnPlane(b - a, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
        }

        // Tela preta por cima de tudo (só enquanto a viagem precisa dela), no tempo real.
        IEnumerator Fade(float target, float seconds)
        {
            if (fade == null) fade = BuildFade();
            float from = fade.color.a;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, target, t / seconds));
                yield return null;
            }
            SetFade(target);
        }

        void SetFade(float alpha)
        {
            fade.color = new Color(0f, 0f, 0f, alpha);
            fade.enabled = alpha > 0f;
        }

        static Image BuildFade()
        {
            var canvasGo = new GameObject("Fade_Da_Viagem");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            var imageGo = new GameObject("Preto");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.enabled = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return image;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
            for (int i = 1; i < route.Length; i++)
                if (route[i - 1] != null && route[i] != null)
                    Gizmos.DrawLine(route[i - 1].position, route[i].position);
        }
    }
}
