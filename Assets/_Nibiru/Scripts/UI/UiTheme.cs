using UnityEngine;

namespace Nibiru.UI
{
    /// <summary>
    /// Tema da interface: a fonte de todos os textos do jogo. Fica em Resources/UiTheme. Com o campo vazio, o jogo usa a
    /// fonte embutida da Unity (que já tem os acentos do português e do inglês). Para trocar a fonte do jogo inteiro,
    /// arraste uma fonte para o campo.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Interface/Tema", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        [Tooltip("Fonte da interface. Vazio = fonte embutida da Unity.")]
        public Font font;
    }
}
