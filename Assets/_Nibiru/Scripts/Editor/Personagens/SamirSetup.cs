using System.Linq;
using Nibiru.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Nibiru.EditorTools.CharacterSetupKit;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Monta o Samir animado: configura o FBX do AccuRig (Humanoid, com dedos, 1,75 m), o material
    /// URP com as texturas PBR do Meshy, o controlador Samir.controller e o prefab
    /// Prefabs/Personagens/Samir (modelo + NpcAnimator + faca, que só aparece no golpe). O conteúdo
    /// da Aldeia põe esse prefab no Samir que anda pela malha de navegação (VillageContent), e este
    /// menu também troca a cápsula do Samir do laboratório (Teste_Personagem).
    /// Parada, andar, correr, agachar e golpe de faca são os mesmos cortes do Mixamo da Elena (rode
    /// antes o menu dela); os gestos de conversa (Talking 1 a 4) são configurados aqui.
    /// Pode rodar de novo: refaz controlador e prefab; o material só é atualizado.
    /// Menu: Nibiru > Personagens > Montar Samir animado.
    /// </summary>
    static class SamirSetup
    {
        const string Root = "Assets/_Nibiru/Art/Personagens/Samir";
        const string ModelPath = Root + "/Samir.fbx";
        const string MetalSmoothPath = Root + "/Samir_MetalSmooth.png";
        const string MaterialPath = Root + "/Materiais/Samir.mat";
        const string ControllerPath = Root + "/Samir.controller";
        public const string PrefabPath = "Assets/_Nibiru/Prefabs/Personagens/Samir.prefab";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        const string MetalMaterial = "Assets/_Nibiru/Art/Materials/Blockout/BO_Metal.mat";
        const string WoodMaterial = "Assets/_Nibiru/Art/Materials/Blockout/BO_Madeira.mat";

        // Velocidade do NavMeshAgent do Samir andando (VillageContent) e correndo para alcançar a Elena.
        public const float WalkSpeed = 3.2f;
        public const float RunSpeed = 5f;

        // Gestos de conversa, um por arquivo; o NpcAnimator escolhe um diferente a cada fala do Samir.
        static readonly Cut[] TalkCuts =
        {
            new(NpcAnimator.TalkStatePrefix + 1, "Talking", -1, -1, true),
            new(NpcAnimator.TalkStatePrefix + 2, "Talking2", -1, -1, true),
            new(NpcAnimator.TalkStatePrefix + 3, "Talking3", -1, -1, true),
            new(NpcAnimator.TalkStatePrefix + 4, "Talking4", -1, -1, true),
        };

        [MenuItem("Nibiru/Personagens/Montar Samir animado")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar o Samir.");
                return;
            }
            AssetDatabase.Refresh();
            BuildMetalSmooth(Root + "/Samir_Metallic.png", Root + "/Samir_Roughness.png", MetalSmoothPath);
            SetTexture(Root + "/Samir_BaseColor.png", TextureImporterType.Default, true);
            SetTexture(Root + "/Samir_Normal.png", TextureImporterType.NormalMap, false);
            SetTexture(MetalSmoothPath, TextureImporterType.Default, false);
            var material = LitMaterial(MaterialPath, Root + "/Samir_BaseColor.png", Root + "/Samir_Normal.png", MetalSmoothPath);
            ConfigureModel(ModelPath, "Samir", material);
            foreach (var cut in TalkCuts)
                ConfigureClips(cut.file, TalkCuts);

            var controller = BuildController(out float crouchSpeed);
            if (controller == null) return;
            BuildPrefab(controller, crouchSpeed);
            AssetDatabase.SaveAssets();
            ReplaceInLab();
            Debug.Log("Samir animado pronto no prefab " + PrefabPath + ". Para a Aldeia, rode Nibiru > Conteúdo > Povoar Aldeia na cena da região.");
        }

        // ---------- Controlador ----------

        static AnimatorController BuildController(out float crouchSpeed)
        {
            crouchSpeed = 0f;
            // Cortes da Elena (ElenaSetup), retargetados para o Samir pelo Humanoid.
            var idle = Clip("Idle", "Parada");
            var walk = Clip("WalkForward", "Andar");
            var run = Clip("Fast Run", "Correr");
            var crouchIdle = Clip("Sneak Walk", "Agachada_Parada");
            var crouchWalk = Clip("Sneak Walk", "Agachada_Andando");
            var slash = Clip("Stabbing", "Golpe_Faca");
            if (new[] { idle, walk, run, crouchIdle, crouchWalk, slash }.Any(c => c == null))
            {
                Debug.LogError("Samir: faltam cortes do Mixamo da Elena. Rode antes Nibiru > Personagens > Montar Elena animada.");
                return null;
            }
            crouchSpeed = NaturalSpeed(crouchWalk);

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(NpcAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(NpcAnimator.CrouchParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(NpcAnimator.StrikeParam, AnimatorControllerParameterType.Trigger);

            // Locomoção: em pé e agachado, misturados pelo Agachar; cada um pela velocidade do NavMeshAgent.
            var machine = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController(NpcAnimator.LocomotionState, out BlendTree crouchBlend, 0);
            crouchBlend.name = "Agachar";
            crouchBlend.blendType = BlendTreeType.Simple1D;
            crouchBlend.blendParameter = NpcAnimator.CrouchParam;
            crouchBlend.useAutomaticThresholds = false;

            var standing = crouchBlend.CreateBlendTreeChild(0f);
            standing.name = "EmPe";
            Speeds(standing, NpcAnimator.SpeedParam,
                (idle, 0f, 0f),
                (walk, NaturalSpeed(walk), NaturalSpeed(walk)),
                (walk, NaturalSpeed(walk), WalkSpeed),
                (run, NaturalSpeed(run), Mathf.Max(NaturalSpeed(run), RunSpeed + 0.5f)));

            var crouched = crouchBlend.CreateBlendTreeChild(1f);
            crouched.name = "Agachado";
            Speeds(crouched, NpcAnimator.SpeedParam,
                (crouchIdle, 0f, 0f),
                (crouchWalk, crouchSpeed, crouchSpeed));

            // Golpe de faca: entra pelo gatilho e volta sozinho para a locomoção.
            var strike = machine.AddState("Golpe_Faca", new Vector3(300f, 60f));
            strike.motion = slash;
            var enter = machine.AddAnyStateTransition(strike);
            enter.AddCondition(AnimatorConditionMode.If, 0f, NpcAnimator.StrikeParam);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = false;
            var leave = strike.AddTransition(locomotion);
            leave.hasExitTime = true;
            leave.exitTime = 1f;
            leave.hasFixedDuration = true;
            leave.duration = 0.25f;

            // Gestos de conversa: o NpcAnimator entra e sai deles (CrossFade), um por fala do Samir.
            for (int i = 0; i < TalkCuts.Length; i++)
            {
                var talk = machine.AddState(TalkCuts[i].name, new Vector3(300f, 140f + i * 60f));
                talk.motion = AssetDatabase.LoadAllAssetsAtPath(TalkCuts[i].file).OfType<AnimationClip>().First(c => c.name == TalkCuts[i].name);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---------- Prefab ----------

        // Samir (NpcAnimator) > Modelo (o FBX, com o Animator) e Faca (só aparece no golpe).
        static void BuildPrefab(AnimatorController controller, float crouchSpeed)
        {
            var root = new GameObject("Samir");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
                model.name = "Modelo";
                model.transform.SetParent(root.transform, false);
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // Faca provisória: cabo de madeira e lâmina curta, a partir do punho.
                var knife = new GameObject("Faca").transform;
                knife.SetParent(root.transform, false);
                Piece(knife, "Cabo", new Vector3(0f, 0f, -0.01f), new Vector3(0.028f, 0.03f, 0.1f), WoodMaterial);
                Piece(knife, "Lamina", new Vector3(0f, 0f, 0.13f), new Vector3(0.008f, 0.03f, 0.18f), MetalMaterial);
                knife.gameObject.SetActive(false);

                var npc = root.AddComponent<NpcAnimator>();
                var serialized = new SerializedObject(npc);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("speakerName").stringValue = "Samir";
                serialized.FindProperty("talkVariations").intValue = TalkCuts.Length;
                serialized.FindProperty("knife").objectReferenceValue = knife;
                serialized.FindProperty("crouchSpeed").floatValue = Mathf.Round(crouchSpeed * 10f) / 10f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void Piece(Transform parent, string name, Vector3 position, Vector3 scale, string material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(material);
        }

        /// <summary>
        /// Põe o prefab do Samir num objeto que já tem o resto (NavMeshAgent, falas...), como filho
        /// "Visual", com os pés em <paramref name="feetHeight"/> (altura local).
        /// </summary>
        public static GameObject AddVisual(Transform samir, float feetHeight)
        {
            var old = samir.Find("Visual");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), samir.gameObject.scene);
            visual.name = "Visual";
            visual.transform.SetParent(samir, false);
            visual.transform.localPosition = new Vector3(0f, feetHeight, 0f);
            visual.transform.localRotation = Quaternion.identity;
            return visual;
        }

        // ---------- Laboratório ----------

        // No laboratório, o Samir é uma cápsula (com colisor e a conversa de teste) com a cabeça
        // como filha. Sai a forma, fica o colisor (na altura dele) e entra o modelo.
        static void ReplaceInLab()
        {
            var scene = SceneManager.GetSceneByPath(LabScenePath);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(LabScenePath, OpenSceneMode.Additive);
            var samir = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "Samir" && t.GetComponent<Dialogue.DialogueStarter>() != null);
            if (samir == null)
            {
                Debug.LogWarning("Samir: não achei o Samir do laboratório (objeto \"Samir\" com DialogueStarter).");
            }
            else
            {
                // A raiz da cápsula fica a 1 m do chão (meio da cápsula de 2 m).
                const float feet = -1f;
                foreach (Transform child in samir.Cast<Transform>().ToArray())
                    if (child.name != "Visual") Object.DestroyImmediate(child.gameObject);
                Object.DestroyImmediate(samir.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(samir.GetComponent<MeshFilter>());
                var capsule = samir.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.radius = 0.3f;
                    capsule.height = 1.75f;
                    capsule.center = new Vector3(0f, feet + 0.875f, 0f);
                }
                AddVisual(samir, feet);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Samir: cápsula do laboratório trocada pelo modelo.");
            }
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
