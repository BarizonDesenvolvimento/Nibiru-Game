using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// As variações de parada da Elena vêm da Universal Animation Library (Quaternius, CC0), que traz os
    /// clipes dentro de um FBX grande com manequim. Para não pôr 24 MB por pacote no projeto, o FBX entra
    /// numa pasta temporária, os clipes escolhidos são copiados como .anim (humanoides) para
    /// Art/Animacoes/Elena e a pasta temporária é apagada. Para trocar uma variação, mude a tabela e rode o menu.
    /// </summary>
    static class UalIdleImport
    {
        const string Ual1 = "ArteFonte/Animacoes/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
        const string Ual2 = "ArteFonte/Animacoes/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";
        const string TempFolder = "Assets/_Nibiru/Art/Animacoes/_UAL_tmp";
        const string Destination = "Assets/_Nibiru/Art/Animacoes/Elena/";

        /// <summary>arquivo-fonte, clipe dentro do FBX, nome do .anim em Assets.</summary>
        public static readonly (string source, string clip, string name)[] Variations =
        {
            // Só os braços cruzados entrou: o "Idle_Loop" da UAL1 é uma postura de luta, de pernas abertas.
            (Ual2, "Armature|Idle_FoldArms_Loop", "Elena_Parada_Var_BracosCruzados"),
        };

        [MenuItem("Nibiru/Animações/Listar clipes de parada da UAL (Console)")]
        static void List()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Saia do Play antes."); return; }
            foreach (string source in new[] { Ual1, Ual2 })
            {
                string path = Import(source);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview"));
                Debug.Log($"{Path.GetFileName(source)}: " + string.Join(", ", clips.Select(c => $"{c.name} ({c.length:F1}s)")));
            }
            Cleanup();
        }

        [MenuItem("Nibiru/Animações/Importar variações de parada da Elena (UAL)")]
        static void ImportVariations()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Saia do Play antes."); return; }
            foreach (var group in Variations.GroupBy(v => v.source))
            {
                string path = Import(group.Key);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToList();
                foreach (var (_, clipName, name) in group)
                {
                    var found = clips.FirstOrDefault(c => c.name == clipName);
                    if (found == null) { Debug.LogError($"Clipe {clipName} não achado em {group.Key}"); continue; }
                    string target = Destination + name + ".anim";
                    var copy = new AnimationClip();
                    EditorUtility.CopySerialized(found, copy);
                    copy.name = name;
                    var settings = AnimationUtility.GetAnimationClipSettings(copy);
                    settings.loopTime = true;
                    settings.keepOriginalPositionY = false;
                    AnimationUtility.SetAnimationClipSettings(copy, settings);
                    AssetDatabase.DeleteAsset(target);
                    AssetDatabase.CreateAsset(copy, target);
                    Debug.Log($"Variação de parada: {name} ({copy.length:F1} s, de {clipName})");
                }
            }
            Cleanup();
            AssetDatabase.SaveAssets();
        }

        static string Import(string source)
        {
            Directory.CreateDirectory(TempFolder);
            string to = TempFolder + "/" + Path.GetFileName(source);
            if (!File.Exists(to)) File.Copy(source, to);
            AssetDatabase.ImportAsset(to);
            var importer = (ModelImporter)AssetImporter.GetAtPath(to);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
            return to;
        }

        static void Cleanup()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.Refresh();
        }
    }
}
