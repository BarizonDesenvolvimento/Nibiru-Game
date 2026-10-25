using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Um documento que a Elena lê no cenário: carta, e-mail impresso, relatório, foto com
    /// legenda. Cada um é um arquivo destes em Data/Documents. Por enquanto aparece na mesma
    /// tela de leitura das tabuletas; o arquivo de documentos com tela própria fica para depois.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Documento", fileName = "NovoDocumento")]
    public class DocumentDefinition : ScriptableObject
    {
        [Tooltip("Título na tela de leitura.")]
        public string displayName;

        [Tooltip("O texto do documento.")]
        [TextArea(6, 20)] public string text;

        [Tooltip("Linha de baixo, menor (de onde veio, um comentário da Elena...). Opcional.")]
        [TextArea(1, 4)] public string footer;

        [Tooltip("Evento disparado ao ler pela primeira vez (opcional).")]
        public StoryEvent raiseOnRead;
    }
}
