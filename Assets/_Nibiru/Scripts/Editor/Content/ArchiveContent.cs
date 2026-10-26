using Nibiru.Documents;
using Nibiru.Quests;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.Content.ContentKit;

namespace Nibiru.EditorTools.Content
{
    /// <summary>
    /// Dados do Arquivo (etapa A3): os códigos estáveis dos documentos e das fitas, os três documentos novos
    /// (inscrições da Aldeia e da passagem, carta do soldado das Trincheiras) e o catálogo em
    /// Resources/Arquivo/Catalogo. Idempotente: pode rodar quantas vezes quiser, em qualquer ordem em relação aos
    /// menus das áreas (cada área chama <see cref="EnsureData"/> antes de usar os documentos).
    /// Menu: Nibiru > Arquivo > Criar catálogo e códigos.
    /// </summary>
    static class ArchiveContent
    {
        const string CatalogFolder = "Assets/_Nibiru/Resources/Arquivo";
        const string CatalogPath = CatalogFolder + "/Catalogo.asset";

        // Códigos estáveis: nunca mudam, nunca se repetem, nunca são reaproveitados. O A4 grava por estes.
        public static readonly (string asset, string id)[] DocumentIds =
        {
            ("Base_EmailDoKast", "doc.base_email_kast"),
            ("Base_Foto1979", "doc.base_foto_1979"),
            ("Trincheiras_CartaDoSoldado", "doc.trincheiras_carta_soldado"),
            ("Aldeia_Inscricao", "doc.aldeia_inscricao"),
            ("Poco_Inscricao", "doc.poco_inscricao"),
            ("Passagem_Inscricao", "doc.passagem_inscricao"),
            ("Weiss_Documento", "doc.weiss_documento"),
            ("Fotos_Do_Tom", "doc.fotos_do_tom"),
        };

        public static readonly (string asset, string id)[] TapeIds =
        {
            ("Fita_Irena_1", "tape.irena_1"),
        };

        [MenuItem("Nibiru/Arquivo/Criar catálogo e códigos")]
        public static void EnsureData()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder + "/Documents")) AssetDatabase.CreateFolder(DataFolder, "Documents");

            // Documentos novos da etapa A3 (os textos são provisórios, no pt-BR.json).
            Asset<DocumentDefinition>("Documents/Passagem_Inscricao", d =>
                d.raiseOnRead = Event("LeuAInscricaoDaPassagem", "A Elena leu em voz alta a inscrição dos Guardiões na parede do barco de junco (passo 13). A tabuleta pulsa e a parede abre."));
            Asset<DocumentDefinition>("Documents/Aldeia_Inscricao", d =>
                d.raiseOnRead = Event("LeuAInscricaoDaAldeia", "A Elena leu a inscrição dos Guardiões da Aldeia, que o Eco do Abzu revelou na parede do fundo da casa do altar."));
            Asset<DocumentDefinition>("Documents/Trincheiras_CartaDoSoldado", d =>
                d.raiseOnRead = Event("LeuACartaDoSoldado", "A Elena leu a carta de um soldado da Fundação que estava numa mochila nas Trincheiras (achado opcional)."));

            foreach ((string asset, string id) in DocumentIds)
            {
                var document = Load<DocumentDefinition>("Documents/" + asset);
                if (document == null) { Debug.LogWarning($"Arquivo: documento {asset} não encontrado."); continue; }
                SetId(document, id);
            }
            foreach ((string asset, string id) in TapeIds)
            {
                var tape = Load<TapeDefinition>("Tapes/" + asset);
                if (tape == null) { Debug.LogWarning($"Arquivo: fita {asset} não encontrada."); continue; }
                SetId(tape, id);
            }

            BuildCatalog();
            AssetDatabase.SaveAssets();
        }

        static void SetId(Object asset, string id)
        {
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty("id");
            if (property.stringValue == id) return;
            property.stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        static ArchiveCatalog.Entry Doc(string asset) => new ArchiveCatalog.Entry { document = Load<DocumentDefinition>("Documents/" + asset) };

        static ArchiveCatalog.Entry Tape(string asset) => new ArchiveCatalog.Entry { tape = Load<TapeDefinition>("Tapes/" + asset) };

        static ArchiveCatalog.Group Group(string titleKey, params ArchiveCatalog.Entry[] entries) =>
            new ArchiveCatalog.Group { titleKey = titleKey, entries = entries };

        static void BuildCatalog()
        {
            if (!AssetDatabase.IsValidFolder(CatalogFolder)) AssetDatabase.CreateFolder("Assets/_Nibiru/Resources", "Arquivo");

            var catalog = AssetDatabase.LoadAssetAtPath<ArchiveCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArchiveCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.groups = new[]
            {
                Group("ui.arquivo.grupo_fundacao", Doc("Base_EmailDoKast"), Doc("Base_Foto1979"), Doc("Trincheiras_CartaDoSoldado")),
                Group("ui.arquivo.grupo_guardioes", Doc("Aldeia_Inscricao"), Doc("Poco_Inscricao"), Doc("Passagem_Inscricao")),
                Group("ui.arquivo.grupo_pessoal", Doc("Weiss_Documento"), Doc("Fotos_Do_Tom")),
                Group("ui.arquivo.grupo_fitas", Tape("Fita_Irena_1")),
            };
            catalog.noticeEvent = Event("AvisouDoArquivo", "O aviso \"Adicionado ao Arquivo (menu de pausa)\" já foi dado (a primeira vez que algo entrou no Arquivo).");
            EditorUtility.SetDirty(catalog);
        }
    }
}
