using Nibiru.Localization;
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
        [Tooltip("Chaves dos textos na tabela de idiomas (doc.<nome>.title, .text, .footer).")]
        public string titleKey;
        public string textKey;
        public string footerKey;

        [Tooltip("Título na tela de leitura. Depois da extração fica vazio: o texto mora na tabela de idiomas.")]
        public string displayName;

        [Tooltip("O texto do documento. Depois da extração fica vazio.")]
        [TextArea(6, 20)] public string text;

        [Tooltip("Linha de baixo, menor (de onde veio, um comentário da Elena...). Opcional. Depois da extração fica vazia.")]
        [TextArea(1, 4)] public string footer;

        public string LocalizedTitle => Loc.Get(titleKey, displayName);

        public string LocalizedText => Loc.Get(textKey, text);

        public string LocalizedFooter => Loc.Get(footerKey, footer);

        [Tooltip("Evento disparado ao ler pela primeira vez (opcional).")]
        public StoryEvent raiseOnRead;
    }
}
