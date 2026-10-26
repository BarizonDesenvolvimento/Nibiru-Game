using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Mostra um documento na tela de leitura. Chamado por uma interação ou por uma sequência
    /// (por exemplo, o e-mail e a foto na mesa da tenda de comando, um depois do outro).
    /// </summary>
    public class DocumentView : MonoBehaviour
    {
        [SerializeField] DocumentDefinition document;

        [Tooltip("Tela de leitura. Vazio = procura sozinho.")]
        [SerializeField] ReadingScreenUI readingScreen;

        public void Show()
        {
            if (document == null) return;
            if (readingScreen == null) readingScreen = FindFirstObjectByType<ReadingScreenUI>();
            if (readingScreen == null)
            {
                Debug.LogWarning("DocumentView: tela de leitura não encontrada.", this);
                return;
            }
            readingScreen.Show(document.LocalizedTitle, document.LocalizedText, document.LocalizedFooter);
            StoryEvents.Raise(document.raiseOnRead);
        }
    }
}
