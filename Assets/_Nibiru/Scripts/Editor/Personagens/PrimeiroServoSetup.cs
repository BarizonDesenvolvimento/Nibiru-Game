using System.Linq;
using Nibiru.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Monta o Primeiro Servo definitivo (o chefe do Poço): copia da ArteFonte o FBX do AccuRig e as
    /// texturas do Meshy (se ainda não estiverem na Unity), importa como Humanoid com dedos na altura
    /// da tabela (2,50 m; os ossos de dedo sem peso são corrigidos pelo FingerWeightFix), cria o
    /// controlador PrimeiroServo.controller (os mesmos cortes do Lullu) e o prefab Prefabs/AI/PrimeiroServo:
    /// na primeira vez, uma cópia do prefab do Lullu; depois, editado no lugar. O prefab ganha a
    /// FirstServantFight (fases, sinais e barra de vida), com os dados Data/AI/PrimeiroServo_Luta.
    /// No laboratório (Teste_Personagem), o Primeiro Servo (o Lullu maior) passa a usar este prefab.
    /// Rode antes o menu do Lullu (os cortes do golpe ficam no arquivo do Mixamo).
    /// Menu: Nibiru > Personagens > Montar Primeiro Servo animado.
    /// </summary>
    static class PrimeiroServoSetup
    {
        const string Character = "PrimeiroServo";
        const string Root = "Assets/_Nibiru/Art/Personagens/PrimeiroServo";
        const string ModelPath = Root + "/PrimeiroServo.fbx";
        const string ControllerPath = Root + "/PrimeiroServo.controller";
        const string LulluPrefabPath = "Assets/_Nibiru/Prefabs/AI/Lullu.prefab";
        public const string PrefabPath = "Assets/_Nibiru/Prefabs/AI/PrimeiroServo.prefab";
        const string SettingsPath = "Assets/_Nibiru/Data/AI/PrimeiroServo.asset";
        const string FightPath = "Assets/_Nibiru/Data/AI/PrimeiroServo_Luta.asset";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";

        // Curvado, as costas ficam a uns 2,2 m do chão; o raio cobre os ombros largos dele.
        const float ColliderHeight = 2.2f;
        const float ColliderRadius = 0.55f;

        [MenuItem("Nibiru/Personagens/Montar Primeiro Servo animado")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar o Primeiro Servo.");
                return;
            }
            AssetDatabase.Refresh();
            Audio.ProvisionalSounds.Generate(overwrite: false);
            SoldiersSetup.ImportCharacter(Character);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) return;
            CharacterSetupKit.ConfigureClips(LulluSetup.Strike.file, new[] { LulluSetup.Strike });
            var controller = LulluSetup.BuildController(ControllerPath, SettingsPath, "Servo");
            if (controller == null) return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                AssetDatabase.CopyAsset(LulluPrefabPath, PrefabPath);
            AssetDatabase.SaveAssets();
            LulluSetup.BuildPrefab(PrefabPath, ModelPath, controller, ColliderRadius, ColliderHeight);
            ConfigurePrefab();
            AssetDatabase.SaveAssets();
            ReplaceInLab();
            Debug.Log("Primeiro Servo animado pronto no prefab " + PrefabPath + ".");
        }

        // Os dados e os componentes que só o chefe tem.
        static void ConfigurePrefab()
        {
            var fightSettings = AssetDatabase.LoadAssetAtPath<FirstServantSettings>(FightPath);
            if (fightSettings == null)
            {
                fightSettings = ScriptableObject.CreateInstance<FirstServantSettings>();
                AssetDatabase.CreateAsset(fightSettings, FightPath);
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var ai = new SerializedObject(root.GetComponent<LulluAI>());
                ai.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LulluSettings>(SettingsPath);
                ai.ApplyModifiedPropertiesWithoutUndo();
                var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
                agent.radius = 0.5f;
                agent.height = 2.4f;

                var fight = root.GetComponent<FirstServantFight>();
                if (fight == null) fight = root.AddComponent<FirstServantFight>();
                var serialized = new SerializedObject(fight);
                serialized.FindProperty("settings").objectReferenceValue = fightSettings;
                serialized.FindProperty("roarSound").objectReferenceValue = Sound(root, "Som_Urro", Audio.ProvisionalSounds.ServantRoar, 1f);
                serialized.FindProperty("crackSound").objectReferenceValue = Sound(root, "Som_Mascara", Audio.ProvisionalSounds.MaskCrack, 0.9f);
                serialized.FindProperty("shardMaterial").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>("Assets/_Nibiru/Art/Materials/Blockout/BO_Ouro.mat");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.name = "PrimeiroServo";
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Fonte de som 3D num filho (refeita a cada execução).
        static AudioSource Sound(GameObject root, string name, string clipPath, float volume)
        {
            Transform old = root.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.playOnAwake = false;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 6f;
            source.maxDistance = 45f;
            return source;
        }

        // ---------- Laboratório ----------

        // O Primeiro Servo do laboratório era o prefab do Lullu em escala 1,25 com os dados do chefe:
        // passa a ser este prefab, em escala 1, acordado desde o começo.
        static void ReplaceInLab()
        {
            var scene = SceneManager.GetSceneByPath(LabScenePath);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(LabScenePath, OpenSceneMode.Additive);
            var servantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var bossData = AssetDatabase.LoadAssetAtPath<LulluSettings>(SettingsPath);
            foreach (var ai in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LulluAI>(true)).ToArray())
            {
                if (new SerializedObject(ai).FindProperty("settings").objectReferenceValue != bossData) continue;
                GameObject go = ai.gameObject;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
                {
                    Debug.LogWarning($"Primeiro Servo: {go.name} do laboratório não é um prefab; ficou como estava.");
                    continue;
                }
                if (PrefabUtility.GetCorrespondingObjectFromSource(go) != servantPrefab)
                {
                    string name = go.name;
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go, servantPrefab, InteractionMode.AutomatedAction);
                    go.name = name;
                }
                go.transform.localScale = Vector3.one;
                var fight = new SerializedObject(go.GetComponent<FirstServantFight>());
                fight.FindProperty("awakeOnStart").boolValue = true;
                fight.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"Primeiro Servo: {go.name} do laboratório agora usa o modelo definitivo.");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
