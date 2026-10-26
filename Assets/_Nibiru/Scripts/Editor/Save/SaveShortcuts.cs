using System.Text;
using Nibiru.Player;
using Nibiru.Save;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: atalhos do salvamento em disco (menu Nibiru > Testes > Salvamento). Gravar marca um ponto de retorno
    /// onde a Elena está e grava no slot; carregar, apagar, resumo dos slots e "fechar e continuar" (grava, sai do Play,
    /// liga o Play no menu principal e escolhe "Continuar"). Os arquivos ficam na pasta de persistentDataPath.
    /// </summary>
    [InitializeOnLoad]
    static class SaveShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Salvamento/";
        const string MainMenuScenePath = "Assets/_Nibiru/Scenes/MenuPrincipal.unity";
        const string CloseAndContinueKey = "Nibiru.Salvamento.FecharEContinuar";

        static SaveShortcuts()
        {
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++)
            {
                int s = slot;
                Register($"salvamento.carregar_{s}", () => LoadSlot(s));
            }
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // ---------- Gravar ----------

        [MenuItem(MenuRoot + "Gravar aqui no slot 1", priority = 1)] static void Write1() => WriteHere(1);
        [MenuItem(MenuRoot + "Gravar aqui no slot 2", priority = 2)] static void Write2() => WriteHere(2);
        [MenuItem(MenuRoot + "Gravar aqui no slot 3", priority = 3)] static void Write3() => WriteHere(3);

        /// <summary>Marca um ponto de retorno onde a Elena está e grava no slot (que vira o slot da partida).</summary>
        static void WriteHere(int slot)
        {
            var respawn = EditorApplication.isPlaying ? Object.FindFirstObjectByType<PlayerRespawn>() : null;
            if (respawn == null)
            {
                Debug.LogWarning("Salvamento: ligue o Play na região (por um atalho de passo, por exemplo) antes de gravar.");
                return;
            }
            SaveSlots.Attach(slot);
            respawn.SaveCheckpoint(respawn.transform);
            Debug.Log($"Salvamento: gravado no slot {slot}, com a Elena em {respawn.transform.position}.\n{Describe(slot)}");
        }

        // ---------- Carregar ----------

        [MenuItem(MenuRoot + "Carregar o slot 1", priority = 21)] static void Load1() => Go("salvamento.carregar_1");
        [MenuItem(MenuRoot + "Carregar o slot 2", priority = 22)] static void Load2() => Go("salvamento.carregar_2");
        [MenuItem(MenuRoot + "Carregar o slot 3", priority = 23)] static void Load3() => Go("salvamento.carregar_3");

        static void LoadSlot(int slot)
        {
            if (SaveSlots.Load(slot)) Debug.Log($"Salvamento: carregando o slot {slot}.\n{Describe(slot)}");
        }

        // ---------- Apagar ----------

        [MenuItem(MenuRoot + "Apagar o slot 1", priority = 41)] static void Delete1() => Delete(1);
        [MenuItem(MenuRoot + "Apagar o slot 2", priority = 42)] static void Delete2() => Delete(2);
        [MenuItem(MenuRoot + "Apagar o slot 3", priority = 43)] static void Delete3() => Delete(3);

        static void Delete(int slot)
        {
            if (!EditorUtility.DisplayDialog("Apagar slot", $"Apagar o slot {slot} ({SaveSlots.PathOf(slot)})?", "Apagar", "Cancelar")) return;
            SaveSlots.Delete(slot);
            Debug.Log($"Salvamento: slot {slot} apagado.");
        }

        // ---------- Resumo ----------

        [MenuItem(MenuRoot + "Resumo de todos os slots (Console)", priority = 61)]
        static void Summary()
        {
            var text = new StringBuilder($"Salvamento: slots em {SaveSlots.Folder}\n");
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++) text.AppendLine(Describe(slot));
            int latest = SaveSlots.LatestSlot();
            text.AppendLine(latest == 0 ? "Nenhum slot usado por último." : $"Usado por último: slot {latest} (\"Continuar\" {(SaveSlots.CanContinue ? "abre este" : "não aparece")}).");
            if (EditorApplication.isPlaying) text.AppendLine($"Slot da partida agora: {(SaveSlots.CurrentSlot == 0 ? "nenhum (nada é gravado)" : SaveSlots.CurrentSlot.ToString())}; tempo jogado {PlayTime.Seconds:0} s.");
            Debug.Log(text.ToString());
        }

        static string Describe(int slot)
        {
            SaveSlots.SlotInfo info = SaveSlots.Info(slot);
            if (info == null) return $"Slot {slot}: vazio.";
            int seconds = Mathf.RoundToInt(info.playSeconds);
            return $"Slot {slot}: {info.AreaName}, gravado em {info.savedAt:dd/MM/yyyy HH:mm:ss}, tempo jogado {seconds / 3600}h{seconds / 60 % 60:00}m{seconds % 60:00}s" +
                (info.demoCompleted ? ", DEMO CONCLUÍDA (não carrega)" : "") + ".";
        }

        [MenuItem(MenuRoot + "Abrir a pasta dos slots", priority = 62)]
        static void OpenFolder() => EditorUtility.RevealInFinder(SaveSlots.PathOf(1));

        // ---------- Fechar e continuar ----------

        [MenuItem(MenuRoot + "Fechar e continuar (grava, sai do Play, entra e continua)", priority = 81)]
        static void CloseAndContinue()
        {
            if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<PlayerRespawn>() == null)
            {
                Debug.LogWarning("Salvamento: ligue o Play na região antes de \"fechar e continuar\".");
                return;
            }
            WriteHere(SaveSlots.CurrentSlot != 0 ? SaveSlots.CurrentSlot : 1);
            SessionState.SetInt(CloseAndContinueKey, 1);
            EditorApplication.isPlaying = false;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            int stage = SessionState.GetInt(CloseAndContinueKey, 0);
            if (stage == 1 && change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetInt(CloseAndContinueKey, 2);
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.OpenScene(MainMenuScenePath);
                    EditorApplication.isPlaying = true;
                };
            }
            else if (stage == 2 && change == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.EraseInt(CloseAndContinueKey);
                // Alguns quadros, para o menu se montar (Start) antes de escolher.
                int ready = Time.frameCount + 3;
                WaitUntil(() => Time.frameCount >= ready, () =>
                {
                    var menu = Object.FindFirstObjectByType<MainMenuUI>();
                    if (menu == null || !menu.ShowsContinue)
                    {
                        Debug.LogWarning("Salvamento: o menu principal não mostrou \"Continuar\".");
                        return;
                    }
                    Debug.Log($"Salvamento: Play ligado de novo, no menu principal; escolhendo \"Continuar\" (slot {SaveSlots.LatestSlot()}).");
                    menu.Continue();
                });
            }
        }
    }
}
