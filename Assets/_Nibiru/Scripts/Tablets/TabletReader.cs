using System.Text;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Tablets
{
    /// <summary>
    /// Fica no jogador. Decifra uma tabuleta: na primeira vez dá o poder, sobe a transformação
    /// e dispara o evento; depois, só mostra o texto de novo para reler.
    /// </summary>
    public class TabletReader : MonoBehaviour
    {
        [Tooltip("Tela de leitura. Vazio = procura sozinho.")]
        [SerializeField] ReadingScreenUI readingScreen;

        [Tooltip("Decifrar pela primeira vez usando o item no inventário. Desmarcado = só num lugar da história (como o nicho da Aldeia); depois de decifrada, a tabuleta pode ser relida pelo inventário.")]
        [SerializeField] bool decipherFromInventory = true;

        [Tooltip("O que aparece ao tentar ler pelo inventário antes da hora.")]
        [SerializeField] string notYetMessage = "A escrita brilha, mas ainda não faz sentido. Não aqui.";

        const string Gold = "#E8C46A";

        void Awake()
        {
            if (readingScreen == null) readingScreen = FindFirstObjectByType<ReadingScreenUI>();
        }

        /// <summary>Leitura pelo inventário: pode ser recusada antes da hora (a mensagem explica).</summary>
        public bool TryReadFromInventory(TabletDefinition tablet, out string message)
        {
            message = null;
            if (!decipherFromInventory && !tablet.IsDeciphered)
            {
                message = notYetMessage;
                return false;
            }
            Read(tablet);
            return true;
        }

        public void Read(TabletDefinition tablet)
        {
            if (tablet == null) return;

            var footer = new StringBuilder();
            if (!tablet.IsDeciphered)
            {
                tablet.MarkDeciphered();
                if (tablet.grantsPower != null)
                {
                    tablet.grantsPower.Unlock();
                    footer.Append("<color=").Append(Gold).Append(">Novo poder: ").Append(tablet.grantsPower.displayName).Append("</color>");
                    if (!string.IsNullOrEmpty(tablet.grantsPower.description))
                        footer.Append("\n<size=24>").Append(tablet.grantsPower.description).Append("</size>");
                }
                if (tablet.transformationGain > 0)
                {
                    Transformation.Add(tablet.transformationGain);
                    footer.Append("\n<size=24>Transformação +").Append(tablet.transformationGain).Append("</size>");
                }
                StoryEvents.Raise(tablet.raiseOnDecipher);
            }

            readingScreen.Show(tablet.displayName, tablet.text, footer.ToString());
        }
    }
}
