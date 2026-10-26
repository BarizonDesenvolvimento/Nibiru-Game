using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Inventory;
using Nibiru.Localization;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools.Localization
{
    /// <summary>
    /// Só no editor: atalhos para ver a troca de idioma funcionando, sem ter traduzido nada. O "inglês de teste" é uma
    /// tabela só na memória em que alguns textos (uma conversa, um documento, um item e os objetivos) ganham o prefixo
    /// "[EN]"; não grava nada. Também cria o tema da interface (fonte central).
    /// Menu: Nibiru > Testes > Idioma.
    /// </summary>
    [InitializeOnLoad]
    static class LocShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Idioma/";

        const string DialoguePath = "Assets/_Nibiru/Data/Dialogues/Aldeia_YusufEspera.asset";
        const string DocumentPath = "Assets/_Nibiru/Data/Documents/Base_EmailDoKast.asset";
        const string ItemPath = "Assets/_Nibiru/Data/Items/Antibioticos.asset";

        static LocShortcuts()
        {
            Register("idioma.inicio", () =>
            {
                RemedyShortcuts.Village(null, RemedyShortcuts.YusufFront, 270f, () =>
                {
                    AddTestEnglish();
                    Loc.SetLanguage("en", save: false);
                    RemedyShortcuts.Give("Antibioticos", 1);
                }, "Idioma: na Aldeia, de frente para o Yusuf, com o inglês de teste ligado (os textos de teste começam com [EN]). Use os outros itens do menu Idioma.");
            });
        }

        [MenuItem(MenuRoot + "1. Começar na Aldeia com o inglês de teste ligado")]
        static void Start1() => RemedyShortcuts.Launch("idioma.inicio");

        [MenuItem(MenuRoot + "2. Trocar de idioma (pt-BR e inglês de teste)")]
        static void Toggle()
        {
            if (!Playing()) return;
            AddTestEnglish();
            Loc.SetLanguage(Loc.Language == "en" ? "pt-BR" : "en", save: false);
            Debug.Log($"Idioma agora: {Loc.DisplayName(Loc.Language)}.");
        }

        [MenuItem(MenuRoot + "3. Ver uma conversa (o Yusuf)")]
        static void Dialogue_()
        {
            if (!Playing()) return;
            var runner = Object.FindFirstObjectByType<DialogueRunner>();
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
            if (!runner.Play(dialogue)) Debug.LogWarning("Não deu para abrir a conversa agora (outra tela aberta?).");
        }

        [MenuItem(MenuRoot + "4. Ver um documento (o e-mail do Kast)")]
        static void Document_()
        {
            if (!Playing()) return;
            var document = AssetDatabase.LoadAssetAtPath<DocumentDefinition>(DocumentPath);
            var reading = Object.FindFirstObjectByType<ReadingScreenUI>();
            reading.Show(document.LocalizedTitle, document.LocalizedText, document.LocalizedFooter);
        }

        [MenuItem(MenuRoot + "5. Ver um item (os antibióticos: aviso aqui, nome e descrição no inventário)")]
        static void Item_()
        {
            if (!Playing()) return;
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemPath);
            Notifications.Post(item.LocalizedName);
            Debug.Log($"Item: \"{item.LocalizedName}\" / \"{item.LocalizedDescription}\". Abra o inventário (Tab) para ver na tela.");
        }

        [MenuItem(MenuRoot + "6. Voltar ao pt-BR")]
        static void Back()
        {
            if (!Playing()) return;
            Loc.SetLanguage("pt-BR", save: false);
        }

        static bool Playing()
        {
            if (EditorApplication.isPlaying) return true;
            Debug.LogWarning("Ligue o Play primeiro (use o item 1 deste menu).");
            return false;
        }

        // Cada texto de teste = "[EN] " + o texto em pt-BR. Só na memória.
        static void AddTestEnglish()
        {
            var table = LocTables.Read("pt-BR");
            string[] keys = table.Keys.Where(k =>
                k == "spk.yusuf" || k == "dlg.Aldeia_YusufEspera.y1"
                || k.StartsWith("doc.Base_EmailDoKast.") || k.StartsWith("item.Antibioticos.")
                || k.StartsWith("quest.RemedioParaYusuf.") || k == "quest.Sobrevivente.name" || k == "quest.Sobrevivente.s13").ToArray();
            foreach (string key in keys) Loc.SetTemporaryText("en", key, "[EN] " + table[key]);
        }

        [MenuItem("Nibiru/Localização/Criar tema da interface (fonte central)")]
        static void CreateTheme()
        {
            const string path = "Assets/_Nibiru/Resources/UiTheme.asset";
            if (AssetDatabase.LoadAssetAtPath<UiTheme>(path) != null)
            {
                Debug.Log("O tema da interface já existe: " + path);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UiTheme>(path);
                return;
            }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<UiTheme>(), path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UiTheme>(path);
            Debug.Log("Tema da interface criado em " + path + ". Para trocar a fonte do jogo inteiro, arraste uma fonte para o campo Font.");
        }
    }
}
