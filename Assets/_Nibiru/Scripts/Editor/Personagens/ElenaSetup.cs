using System.IO;
using System.Linq;
using Nibiru.Combat;
using Nibiru.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Troca a cápsula pela Elena animada no prefab Prefabs/Player/Elena: configura a importação
    /// dos FBX do Meshy (Humanoid, cada um com o próprio avatar), cria o material URP, o controlador
    /// Elena.controller (ligado ao PlayerMotor pelo PlayerAnimator) e põe o modelo no filho "Jogador".
    /// Pode rodar de novo: refaz controlador, máscara e modelo no prefab; o material só é atualizado.
    /// Menu: Nibiru > Personagens > Montar Elena animada.
    /// </summary>
    static class ElenaSetup
    {
        const string Root = "Assets/_Nibiru/Art/Personagens/Elena";
        const string ModelPath = Root + "/Elena_Personagem.fbx";
        const string AnimationsPath = Root + "/Elena_Animacoes.fbx";
        const string WalkPath = Root + "/Elena_Andando.fbx";
        const string MetalSmoothPath = Root + "/Elena_MetalSmooth.png";
        const string MaterialPath = Root + "/Materiais/Elena.mat";
        const string ControllerPath = Root + "/Elena.controller";
        const string MaskPath = Root + "/Elena_Tronco.mask";
        const string PrefabPath = "Assets/_Nibiru/Prefabs/Player/Elena.prefab";
        const string SettingsPath = "Assets/_Nibiru/Data/Player/MovimentoJogador.asset";
        const string ModelChild = "Modelo";
        const string CapsuleChild = "Corpo";
        const string GunChild = "ArmaNaMao";

        /// <summary>
        /// Um trecho de animação usado pelo controlador. Quadros -1 = clipe inteiro.
        /// naturalSpeed: velocidade (m/s) em que os pés da animação não escorregam; 0 = parada.
        /// feet: a altura vem dos pés (o pé mais baixo fica no chão), em vez da altura original do clipe.
        /// Para trocar uma animação (por exemplo, uma "Idle" nova do Meshy), mude o arquivo e o take aqui.
        /// </summary>
        readonly struct Cut
        {
            public readonly string name, file, take;
            public readonly int first, last;
            public readonly bool loop, feet;
            public readonly float naturalSpeed;

            public Cut(string name, string file, string take, int first, int last, bool loop, float naturalSpeed = 0f, bool feet = false)
            {
                this.name = name; this.file = file; this.take = take;
                this.first = first; this.last = last; this.loop = loop; this.naturalSpeed = naturalSpeed; this.feet = feet;
            }
        }

        // O pacote do Meshy não tem "parada", "agachada parada" nem "mirando": por enquanto são
        // quadros parados tirados de outras animações.
        static readonly Cut Idle = new("Parada", AnimationsPath, "Idle_Turn_Right", 0, 1, true);
        static readonly Cut Walk = new("Andar", WalkPath, "mixamo.com", -1, -1, true, 1.56f);
        static readonly Cut Run = new("Correr", AnimationsPath, "run_fast_8", -1, -1, true, 2.77f);
        // Quadro do andar furtivo com os dois pés apoiados e as mãos perto do corpo.
        static readonly Cut CrouchIdle = new("Agachada_Parada", AnimationsPath, "Sneaky_Walk_inplace", 48, 49, true, feet: true);
        static readonly Cut CrouchWalk = new("Agachada_Andando", AnimationsPath, "Sneaky_Walk_inplace", -1, -1, true, 0.5f, feet: true);
        static readonly Cut Dodge = new("Esquiva", AnimationsPath, "sliding_rool", 0, 10, false);
        // Quadro da mira de fuzil em que os braços apontam reto para a frente (no 0 apontam 48° à esquerda).
        static readonly Cut Aim = new("Mira", AnimationsPath, "Rifle_Aim_Turn_Right", 12, 13, true);
        static readonly Cut[] Cuts = { Idle, Walk, Run, CrouchIdle, CrouchWalk, Dodge, Aim };

        [MenuItem("Nibiru/Personagens/Montar Elena animada")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar a Elena.");
                return;
            }
            AssetDatabase.Refresh();
            ConfigureTextures();
            var material = BuildMaterial();
            ConfigureModel(material);
            ConfigureAnimations(AnimationsPath, true);
            ConfigureAnimations(WalkPath, false);
            var controller = BuildController();
            BuildPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Elena animada pronta no prefab " + PrefabPath + ".");
        }

        // ---------- Texturas e material ----------

        static void ConfigureTextures()
        {
            if (!File.Exists(MetalSmoothPath))
                BuildMetalSmooth();
            SetTexture(Root + "/Elena_BaseColor.png", TextureImporterType.Default, true);
            SetTexture(Root + "/Elena_Normal.png", TextureImporterType.NormalMap, false);
            SetTexture(MetalSmoothPath, TextureImporterType.Default, false);
        }

        // O Lit do URP lê metal no canal R e lisura no A, num mapa só: A = 1 - rugosidade.
        static void BuildMetalSmooth()
        {
            var metal = LoadRaw(Root + "/Elena_Metallic.png");
            var rough = LoadRaw(Root + "/Elena_Roughness.png");
            if (rough.width != metal.width || rough.height != metal.height)
                rough = Resize(rough, metal.width, metal.height);
            var m = metal.GetPixels32();
            var r = rough.GetPixels32();
            var result = new Color32[m.Length];
            for (int i = 0; i < m.Length; i++)
                result[i] = new Color32(m[i].r, m[i].r, m[i].r, (byte)(255 - r[i].r));
            var output = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            output.SetPixels32(result);
            File.WriteAllBytes(MetalSmoothPath, output.EncodeToPNG());
            Object.DestroyImmediate(metal);
            Object.DestroyImmediate(rough);
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(MetalSmoothPath);
        }

        // Lê o PNG direto do disco, sem conversão de cor e sem mexer na importação.
        static Texture2D LoadRaw(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        static Texture2D Resize(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(source);
            return result;
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

        static Material BuildMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Elena_BaseColor.png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Elena_Normal.png"));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MetalSmoothPath));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Importação ----------

        static void ConfigureModel(Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            foreach (var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentsInChildren<Renderer>())
                foreach (var original in renderer.sharedMaterials.Where(m => m != null && m != material))
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"Avatar da Elena: {(avatar != null && avatar.isValid && avatar.isHuman ? "Humanoid válido" : "PROBLEMA no Humanoid")}");
        }

        // keepAll: mantém também os clipes originais do Meshy (para prévia), além dos trechos do controlador.
        static void ConfigureAnimations(string path, bool keepAll)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;

            var defaults = importer.defaultClipAnimations;
            var clips = new System.Collections.Generic.List<ModelImporterClipAnimation>();
            if (keepAll)
                foreach (var clip in defaults)
                {
                    Prepare(clip, clip.takeName.ToLowerInvariant().Contains("walk") || clip.takeName.ToLowerInvariant().Contains("run"));
                    clips.Add(clip);
                }
            foreach (var cut in Cuts.Where(c => c.file == path))
            {
                var source = defaults.First(c => c.takeName == cut.take);
                var clip = new ModelImporterClipAnimation
                {
                    name = cut.name,
                    takeName = source.takeName,
                    firstFrame = cut.first >= 0 ? cut.first : source.firstFrame,
                    lastFrame = cut.last >= 0 ? cut.last : source.lastFrame,
                };
                Prepare(clip, cut.loop);
                if (cut.feet)
                {
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                }
                clips.Add(clip);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
        }

        // O movimento é do PlayerMotor: o avanço da animação sai para a raiz (e é descartado, sem
        // root motion), a altura e o giro ficam na pose. "Based Upon: Body Orientation" é obrigatório
        // nas animações do Meshy, senão a Elena fica virada para trás.
        static void Prepare(ModelImporterClipAnimation clip, bool loop)
        {
            clip.loopTime = loop;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = false;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = true;
            clip.lockRootPositionXZ = false;
            clip.keepOriginalPositionXZ = false;
        }

        static AnimationClip Clip(Cut cut) =>
            AssetDatabase.LoadAllAssetsAtPath(cut.file).OfType<AnimationClip>().First(c => c.name == cut.name);

        // ---------- Controlador ----------

        static AnimatorController BuildController()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(SettingsPath);
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(PlayerAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerAnimator.CrouchParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = PlayerAnimator.DirectionParam,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f,
            });
            controller.AddParameter(PlayerAnimator.DodgeParam, AnimatorControllerParameterType.Trigger);

            // Locomoção: em pé e agachada, misturadas pelo Agachar; cada uma pela velocidade.
            var machine = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController("Locomocao", out BlendTree crouchBlend, 0);
            locomotion.speedParameter = PlayerAnimator.DirectionParam;
            locomotion.speedParameterActive = true;
            crouchBlend.name = "Agachar";
            crouchBlend.blendType = BlendTreeType.Simple1D;
            crouchBlend.blendParameter = PlayerAnimator.CrouchParam;
            crouchBlend.useAutomaticThresholds = false;

            var standing = crouchBlend.CreateBlendTreeChild(0f);
            standing.name = "EmPe";
            Speeds(standing,
                (Idle, 0f),
                (Walk, Walk.naturalSpeed),
                (Walk, settings.walkSpeed),
                (Run, settings.runSpeed));

            var crouched = crouchBlend.CreateBlendTreeChild(1f);
            crouched.name = "Agachada";
            Speeds(crouched,
                (CrouchIdle, 0f),
                (CrouchWalk, settings.crouchSpeed));

            // Esquiva: o trecho toca no tempo exato da esquiva do PlayerMotor.
            var dodgeClip = Clip(Dodge);
            var dodge = machine.AddState("Esquiva", new Vector3(300f, 120f));
            dodge.motion = dodgeClip;
            dodge.speed = dodgeClip.length / settings.dodgeDuration;
            var toDodge = machine.AddAnyStateTransition(dodge);
            toDodge.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.DodgeParam);
            toDodge.hasExitTime = false;
            toDodge.duration = 0.05f;
            toDodge.canTransitionToSelf = false;
            var back = dodge.AddTransition(locomotion);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.hasFixedDuration = true;
            back.duration = 0.2f;

            // Mira: só o tronco, braços e cabeça; o peso é controlado pelo PlayerAnimator.
            controller.AddLayer(PlayerAnimator.AimLayer);
            var layers = controller.layers;
            layers[1].avatarMask = BuildUpperBodyMask();
            layers[1].defaultWeight = 0f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            var aim = controller.layers[1].stateMachine.AddState("Mira");
            aim.motion = Clip(Aim);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Cada animação entra na velocidade em que deve tocar. Quando a velocidade do jogo é maior
        // que a natural da animação, ela acelera só em parte (raiz quadrada): passos rápidos demais
        // parecem mais errados que um pouco de escorregão.
        static void Speeds(BlendTree tree, params (Cut cut, float speed)[] entries)
        {
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = PlayerAnimator.SpeedParam;
            tree.useAutomaticThresholds = false;
            foreach (var (cut, speed) in entries)
                tree.AddChild(Clip(cut), speed);
            var children = tree.children;
            for (int i = 0; i < children.Length; i++)
            {
                var (cut, speed) = entries[i];
                children[i].timeScale = cut.naturalSpeed > 0f && speed > cut.naturalSpeed
                    ? Mathf.Clamp(Mathf.Sqrt(speed / cut.naturalSpeed), 1f, 2.5f)
                    : 1f;
            }
            tree.children = children;
        }

        static AvatarMask BuildUpperBodyMask()
        {
            AssetDatabase.DeleteAsset(MaskPath);
            var mask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                var part = (AvatarMaskBodyPart)i;
                bool upper = part is AvatarMaskBodyPart.Body or AvatarMaskBodyPart.Head
                    or AvatarMaskBodyPart.LeftArm or AvatarMaskBodyPart.RightArm
                    or AvatarMaskBodyPart.LeftFingers or AvatarMaskBodyPart.RightFingers
                    or AvatarMaskBodyPart.LeftHandIK or AvatarMaskBodyPart.RightHandIK;
                mask.SetHumanoidBodyPartActive(part, upper);
            }
            AssetDatabase.CreateAsset(mask, MaskPath);
            return mask;
        }

        // ---------- Prefab ----------

        static void BuildPrefab(AnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var player = root.GetComponentInChildren<PlayerMotor>().transform;

                // Tira a cápsula (o "Corpo" direto do Jogador; o da arma fica) e um modelo anterior.
                foreach (string name in new[] { CapsuleChild, ModelChild })
                {
                    var old = player.Find(name);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }

                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.scene);
                model.name = ModelChild;
                model.transform.SetParent(player, false);
                model.transform.SetSiblingIndex(0);

                // Os pés no fundo do colisor (o CharacterController fica a skinWidth do chão).
                var cc = player.GetComponent<CharacterController>();
                float feet = cc.center.y - cc.height * 0.5f - cc.skinWidth;
                model.transform.localPosition = new Vector3(0f, feet, 0f);
                model.transform.localRotation = Quaternion.identity;

                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var playerAnimator = player.GetComponent<PlayerAnimator>();
                if (playerAnimator == null) playerAnimator = player.gameObject.AddComponent<PlayerAnimator>();
                var serialized = new SerializedObject(playerAnimator);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("motor").objectReferenceValue = player.GetComponent<PlayerMotor>();
                serialized.FindProperty("weapons").objectReferenceValue = player.GetComponent<PlayerWeapons>();
                serialized.FindProperty("heldGun").objectReferenceValue = player.Find(GunChild);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // O agachar agora é da animação: o visual do agachar só desce câmera, arma e faca.
                var crouchVisual = player.GetComponent<PlayerCrouchVisual>();
                if (crouchVisual != null)
                {
                    var crouch = new SerializedObject(crouchVisual);
                    crouch.FindProperty("body").objectReferenceValue = null;
                    crouch.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
