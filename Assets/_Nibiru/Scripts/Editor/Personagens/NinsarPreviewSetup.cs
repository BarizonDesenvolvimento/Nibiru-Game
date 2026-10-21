using System.IO;
using System.Linq;
using Nibiru.Testes;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Prévia da Ninsar: configura a importação (Humanoid), cria os materiais URP, um controlador com
    /// as animações do Meshy (traduzidas pelo Humanoid), o prefab e a cena Teste_Ninsar.
    /// Os arquivos vêm do Blender (ArteFonte/Ninsar/3_Blender/Scripts/exportar_previa_unity.py).
    /// Pode rodar de novo: refaz controlador, prefab e cena; materiais existentes só são atualizados.
    /// Menu: Nibiru > Personagens > Montar prévia da Ninsar.
    /// </summary>
    static class NinsarPreviewSetup
    {
        const string Root = "Assets/_Nibiru/Art/Personagens/Ninsar";
        const string ModelPath = Root + "/Ninsar_Previa.fbx";
        const string AnimationsPath = Root + "/Ninsar_Animacoes.fbx";
        const string ControllerPath = Root + "/Ninsar_Previa.controller";
        const string PrefabPath = "Assets/_Nibiru/Prefabs/Personagens/Ninsar_Previa.prefab";
        const string ScenePath = "Assets/_Nibiru/Scenes/Teste_Ninsar.unity";
        static readonly string[] Parts = { "Corpo", "Saia", "Lanca" };
        static readonly string[] LoopWords = { "walk", "run", "idle", "sprint", "crawl" };
        const string DefaultState = "Idle_12";

        [MenuItem("Nibiru/Personagens/Montar prévia da Ninsar")]
        static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssetDatabase.Refresh();
            ConfigureTextures();
            ConfigureModel();
            ConfigureAnimations();
            var controller = BuildController(out string[] states);
            var prefab = BuildPrefab(controller, states);
            BuildScene(prefab);
            Debug.Log($"Prévia da Ninsar pronta: {states.Length} animações. Abra {ScenePath} e aperte Play.");
        }

        static void ConfigureTextures()
        {
            foreach (string part in Parts)
            {
                SetTexture($"{Root}/Texturas/{part}_Normal.png", TextureImporterType.NormalMap, false);
                SetTexture($"{Root}/Texturas/{part}_MetalSmooth.png", TextureImporterType.Default, false);
                SetTexture($"{Root}/Texturas/{part}_BaseColor.png", TextureImporterType.Default, true);
            }
        }

        static void SetTexture(string path, TextureImporterType type, bool srgb)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType == type && importer.sRGBTexture == srgb)
                return;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.SaveAndReimport();
        }

        static Material BuildMaterial(string part)
        {
            Directory.CreateDirectory($"{Root}/Materiais");
            string path = $"{Root}/Materiais/Ninsar_{part}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Texturas/{part}_BaseColor.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Texturas/{part}_Normal.png"));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Texturas/{part}_MetalSmooth.png"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ConfigureModel()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.bakeAxisConversion = true;
            foreach (string part in Parts)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Ninsar_" + part), BuildMaterial(part));
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"Avatar da Ninsar: {(avatar != null && avatar.isValid && avatar.isHuman ? "Humanoid válido" : "PROBLEMA no Humanoid")}");
        }

        static void ConfigureAnimations()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AnimationsPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;
            importer.bakeAxisConversion = true;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = clip.takeName.Replace("Armature|", "");
                string lower = clip.name.ToLowerInvariant();
                clip.loopTime = LoopWords.Any(lower.Contains);
                // Prévia parada no lugar: o deslocamento do clipe fica "assado" na pose
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = false;   // segue o corpo: olha para +Z, como o objeto
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static AnimatorController BuildController(out string[] states)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var clips = AssetDatabase.LoadAllAssetsAtPath(AnimationsPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).OrderBy(c => c.name).ToArray();
            foreach (var clip in clips)
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name == DefaultState)
                    machine.defaultState = state;
            }
            states = clips.Select(c => c.name).ToArray();
            int idle = System.Array.IndexOf(states, DefaultState);
            if (idle > 0)
                states = new[] { DefaultState }.Concat(states.Where(s => s != DefaultState)).ToArray();
            return controller;
        }

        static GameObject BuildPrefab(AnimatorController controller, string[] states)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Ninsar_Previa";
            var animator = instance.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var preview = instance.AddComponent<AnimationPreview>();
            var serialized = new SerializedObject(preview);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            var list = serialized.FindProperty("states");
            list.arraySize = states.Length;
            for (int i = 0; i < states.Length; i++)
                list.GetArrayElementAtIndex(i).stringValue = states[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static void BuildScene(GameObject prefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Chao";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            var ninsar = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            ninsar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var camera = Camera.main;
            camera.gameObject.AddComponent<OrbitPreviewCamera>().Target = ninsar.transform;
            var light = Object.FindFirstObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneManager.SetActiveScene(scene);
        }
    }
}
