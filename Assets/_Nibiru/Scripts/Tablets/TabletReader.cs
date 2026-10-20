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

        const string Gold = "#E8C46A";

        void Awake()
        {
            if (readingScreen == null) readingScreen = FindFirstObjectByType<ReadingScreenUI>();
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
