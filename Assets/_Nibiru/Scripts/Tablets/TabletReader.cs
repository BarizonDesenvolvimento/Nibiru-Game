using System.Text;
using Nibiru.Localization;
using Nibiru.Powers;
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
                    PowerBook.Grant(tablet.grantsPower);
                    footer.Append("<color=").Append(Gold).Append('>')
                        .Append(string.Format(Loc.Get("ui.poder.novo", "Novo poder: {0}"), tablet.grantsPower.LocalizedName)).Append("</color>");
                    string description = tablet.grantsPower.LocalizedDescription;
                    if (!string.IsNullOrEmpty(description))
                        footer.Append("\n<size=24>").Append(description).Append("</size>");
                }
                if (tablet.transformationGain > 0)
                {
                    Transformation.Add(tablet.transformationGain);
                    footer.Append("\n<size=24>")
                        .Append(string.Format(Loc.Get("ui.tabuleta.transformacao", "Transformação +{0}"), tablet.transformationGain)).Append("</size>");
                }
                StoryEvents.Raise(tablet.raiseOnDecipher);
            }

            readingScreen.Show(tablet.LocalizedName, tablet.LocalizedText, footer.ToString());
        }
    }
}
