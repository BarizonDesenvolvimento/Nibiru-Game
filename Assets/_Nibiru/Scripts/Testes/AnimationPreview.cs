using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.Testes
{
    /// <summary>
    /// Prévia de animações de um personagem: troca o estado do Animator por teclas ou por um menu
    /// na tela. Setas esquerda/direita: anterior/próxima. 1 a 9 e 0: atalhos para as dez primeiras.
    /// Espaço: recomeça a animação atual. Os nomes dos estados vêm do editor (menu de montagem).
    /// </summary>
    public class AnimationPreview : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string[] states = new string[0];
        [SerializeField] float crossFade = 0.2f;

        static readonly Key[] Shortcuts =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        int current;
        Vector2 scroll;

        public int Current => current;
        public string CurrentState => states.Length > 0 ? states[current] : "";

        void Reset() => animator = GetComponent<Animator>();

        void Start()
        {
            if (states.Length > 0)
                Play(current);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || states.Length == 0)
                return;
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                Play((current + 1) % states.Length);
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                Play((current - 1 + states.Length) % states.Length);
            if (keyboard.spaceKey.wasPressedThisFrame)
                Play(current);
            for (int i = 0; i < Shortcuts.Length && i < states.Length; i++)
                if (keyboard[Shortcuts[i]].wasPressedThisFrame)
                    Play(i);
        }

        public void Play(int index)
        {
            current = Mathf.Clamp(index, 0, states.Length - 1);
            animator.CrossFadeInFixedTime(states[current], crossFade, 0, 0f);
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 270, Screen.height - 20), GUI.skin.box);
            GUILayout.Label("Animações (setas, 1–0, espaço recomeça)");
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < states.Length; i++)
            {
                string label = (i == current ? "▶ " : "") + (i < 10 ? $"{(i + 1) % 10}. " : "") + states[i];
                if (GUILayout.Button(label))
                    Play(i);
            }
            GUILayout.EndScrollView();
            GUILayout.Label($"Velocidade: {animator.speed:0.00}");
            animator.speed = GUILayout.HorizontalSlider(animator.speed, 0f, 2f);
            GUILayout.EndArea();
        }
    }
}
