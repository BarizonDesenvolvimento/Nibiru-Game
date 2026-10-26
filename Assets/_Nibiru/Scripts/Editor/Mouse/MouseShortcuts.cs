using System.IO;
using System.Reflection;
using Nibiru.EditorTools.Saving;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Mouse
{
    /// <summary>
    /// Só no editor: atalhos para testar o mouse e o cursor no Play (menu Nibiru > Testes > Mouse). O menu com slots de teste
    /// usa uma pasta só de teste (nunca os slots reais) e a apaga ao sair do Play.
    /// </summary>
    [InitializeOnLoad]
    static class MouseShortcuts
    {
        const string Root = "Nibiru/Testes/Mouse/";
        const string TestFolderName = "Teste_MouseAtalho";
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static MouseShortcuts()
        {
            Register("mouse.menu_slots", MenuWithSlots);
            Register("mouse.pausa", PauseWithArchive);
            Register("mouse.cursor", () => InSafePlace(ShowCursorState));
            Register("mouse.alttab", () => InSafePlace(SimulateAltTab));
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredEditMode) return;
                string folder = Path.Combine(Application.persistentDataPath, TestFolderName);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            };
        }

        [MenuItem(Root + "Menu principal com 3 slots de teste (não toca nos slots reais)", priority = 1)]
        static void M1() => Go("mouse.menu_slots");

        [MenuItem(Root + "Pausa aberta com o Arquivo cheio", priority = 2)]
        static void M2() => Go("mouse.pausa");

        [MenuItem(Root + "Mostrar o estado do cursor (Console)", priority = 3)]
        static void M3() => ShowCursorStateMenu();

        [MenuItem(Root + "Simular o alt-tab (a janela recupera o foco)", priority = 4)]
        static void M4() => Go("mouse.alttab");

        // ---------- Ações ----------

        static void MenuWithSlots()
        {
            string folder = Path.Combine(Application.persistentDataPath, TestFolderName);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            SaveSlots.UseFolder(folder);
            SlotShortcuts.FillAll();
            SlotShortcuts.MakeOccupied(1);
            SaveSlots.Detach();
            SceneManager.LoadScene("MenuPrincipal");
            Debug.Log("Mouse, atalho: menu principal com 3 slots de teste (1 ocupado, 2 demo concluída, 3 ilegível), numa pasta só de teste. " +
                "Passe o mouse nos itens (o foco acompanha), clique para ativar, clique direito volta, o botão [Apagar] fica na ponta de cada slot. " +
                "Use as setas: o cursor some; mexa o mouse: ele volta. Ao sair do Play a pasta de teste é apagada.");
        }

        // Começa de um lugar seguro (o mesmo do Arquivo cheio): estes atalhos não movem a Elena, e ela não pode herdar o
        // lugar de outro atalho (no teste geral, a saída do passo 10, perto do atirador).
        static void InSafePlace(System.Action then)
        {
            RemedyShortcuts.Ready = false;
            Go("arquivo.cheio");
            WaitUntil(() => RemedyShortcuts.Ready, then);
        }

        static void PauseWithArchive()
        {
            InSafePlace(() =>
            {
                var pause = Object.FindFirstObjectByType<PauseMenuUI>();
                if (pause == null) return;
                typeof(PauseMenuUI).GetMethod("Open", Any).Invoke(pause, null);
                Debug.Log("Mouse, atalho: pausa aberta com o Arquivo cheio. O cursor está livre e escondido (abriu pelo teclado): mexa o mouse e ele aparece. " +
                    "Passe o mouse nos itens, clique em Arquivo e role com a roda; clique numa entrada para ler (clique fecha); clique direito volta.");
            });
        }

        static void ShowCursorStateMenu()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Mouse: o estado do cursor só existe com o Play ligado.");
                return;
            }
            ShowCursorState();
        }

        static void ShowCursorState() =>
            Debug.Log($"Mouse, cursor: trava {Cursor.lockState}, {(Cursor.visible ? "visível" : "escondido")}. " +
                $"Regra: {(CursorPolicy.AnyFree ? "um menu pediu o cursor livre" : "jogo (cursor preso)")}, ponteiro {(CursorPolicy.PointerShown ? "mostrado" : "escondido")}.");

        static void SimulateAltTab()
        {
            // Como se a Unity tivesse solto o cursor ao perder o foco: a regra deve devolvê-lo ao estado certo.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            CursorPolicy.OnFocusRegained();
            Debug.Log("Mouse, atalho: alt-tab simulado. Depois dele " +
                $"o cursor ficou {Cursor.lockState}, {(Cursor.visible ? "visível" : "escondido")} (no jogo o certo é Locked e escondido; num menu, livre).");
        }
    }
}
