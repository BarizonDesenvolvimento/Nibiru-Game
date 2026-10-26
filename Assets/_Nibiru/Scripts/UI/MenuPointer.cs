using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.UI
{
    /// <summary>
    /// O mouse como os menus o veem: posição, se mexeu neste quadro, clique esquerdo, clique direito e roda. Os menus leem
    /// por aqui (e não direto de <c>Mouse.current</c>) para os testes poderem "mexer o mouse" sem dispositivo.
    /// </summary>
    public static class MenuPointer
    {
        public struct State
        {
            /// <summary>Posição na tela, em pixels (origem embaixo à esquerda).</summary>
            public Vector2 position;
            /// <summary>True se o mouse se mexeu neste quadro.</summary>
            public bool moved;
            public bool leftClick;
            public bool rightClick;
            /// <summary>Roda deste quadro: positivo é para cima.</summary>
            public float wheel;

            public bool Any => moved || leftClick || rightClick || wheel != 0f;
        }

        /// <summary>Só para os testes: no lugar do mouse de verdade (null volta ao normal).</summary>
        public static Func<State> Source;

        /// <summary>Lê o mouse deste quadro. Mexer, clicar ou girar a roda mostra o cursor; veja <see cref="CursorPolicy"/>.</summary>
        public static State Read()
        {
            State state = default;
            if (Source != null) state = Source();
            else
            {
                Mouse mouse = Mouse.current;
                if (mouse != null)
                {
                    state.position = mouse.position.ReadValue();
                    state.moved = mouse.delta.ReadValue().sqrMagnitude > 0.01f;
                    state.leftClick = mouse.leftButton.wasPressedThisFrame;
                    state.rightClick = mouse.rightButton.wasPressedThisFrame;
                    state.wheel = mouse.scroll.ReadValue().y;
                }
            }
            if (state.Any) CursorPolicy.ShowPointer(true);
            return state;
        }

        /// <summary>O teclado ou o controle foram usados num menu: o cursor some até o mouse se mexer de novo.</summary>
        public static void NoteKeyboardOrPad() => CursorPolicy.ShowPointer(false);
    }
}
