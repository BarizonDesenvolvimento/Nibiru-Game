using System.IO;
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
    /// Monta os 4 soldados da Fundação (Soldado, Líder de esquadra, Atirador de elite e Piloto) com
    /// o kit comum: copia o FBX do AccuRig e as texturas do Meshy da ArteFonte (se ainda não
    /// estiverem em Art/Personagens), importa como Humanoid na altura da tabela, cria o material URP,
    /// o controlador dos soldados (parado, patrulha, corrida, mira, tiro, recarga, dano e morte, com
    /// as animações de fuzil do Mixamo), o do piloto (sentado) e os prefabs:
    /// - Prefabs/Personagens/Soldado, LiderDeEsquadra, AtiradorDeElite: o modelo, o SoldierAnimator
    ///   e um fuzil provisório na mão direita (com o cano, de onde sai o tiro);
    /// - Prefabs/Personagens/Piloto: o modelo sentado, para a cabine dos helicópteros;
    /// - Prefabs/AI/Soldado (o soldado com IA, que já existia: sai a cápsula, entra o Soldado) e as
    ///   variantes Soldado_Lider e Soldado_Atirador (mesma IA, outro modelo).
    /// Parado, recarga, dano e morte usam os cortes da Elena (rode antes o menu dela).
    /// No laboratório (Teste_Personagem), o Líder passa a usar o Soldado_Lider. Nas cenas da região,
    /// rode de novo os menus de conteúdo (Acampamento, Trincheiras e Aldeia, nessa ordem).
    /// Pode rodar de novo: refaz controladores e prefabs; os materiais só são atualizados.
    /// Menu: Nibiru > Personagens > Montar soldados da Fundação.
    /// </summary>
    static class SoldiersSetup
    {
        const string ArtRoot = "Assets/_Nibiru/Art/Personagens/";
        const string ControllerPath = ArtRoot + "Soldado/Soldados.controller";
        const string MaskPath = ArtRoot + "Soldado/Soldado_Tronco.mask";
        const string PilotControllerPath = ArtRoot + "Piloto/Piloto.controller";
        const string SeatedClipPath = ArtRoot + "Piloto/Piloto_Sentado.anim";
        const string VisualFolder = "Assets/_Nibiru/Prefabs/Personagens/";
        public const string SoldierPrefab = "Assets/_Nibiru/Prefabs/AI/Soldado.prefab";
        public const string LeaderPrefab = "Assets/_Nibiru/Prefabs/AI/Soldado_Lider.prefab";
        public const string SniperPrefab = "Assets/_Nibiru/Prefabs/AI/Soldado_Atirador.prefab";
        public const string PilotPrefab = VisualFolder + "Piloto.prefab";
        const string SoldierSettingsPath = "Assets/_Nibiru/Data/AI/SoldadoContencao.asset";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        const string MetalMaterial = "Assets/_Nibiru/Art/Materials/Blockout/BO_Metal.mat";
        const string WoodMaterial = "Assets/_Nibiru/Art/Materials/Blockout/BO_Madeira.mat";

        public const string VisualChild = "Visual";
        const string ModelChild = "Modelo";
        public const string RifleChild = "Fuzil";
        const string MuzzleChild = "Cano";

        // Os quatro, pelo nome da pasta (o mesmo em ArteFonte, Art/Personagens e CharacterHeights).
        public static readonly string[] Characters = { "Soldado", "LiderDeEsquadra", "AtiradorDeElite", "Piloto" };

        // A raiz do soldado com IA fica 1 m acima do chão (o baseOffset do NavMeshAgent).
        const float FeetBelowRoot = -1f;

        // Animações de fuzil do Mixamo (só dos soldados; arquivos copiados da ArteFonte). Todas com o
        // giro original: assim andam reto para a frente. Pela direção do corpo, o corpo fica de frente
        // e o fuzil, atravessado. No giro original o fuzil fica uns 27° à esquerda (postura de lado);
        // na mira, o SoldierAnimator gira o tronco para o cano apontar para o alvo.
        static readonly Cut RifleIdle = new("Fuzil_Parado", "rifle aiming idle", -1, -1, true, original: true);
        static readonly Cut RifleWalk = new("Fuzil_Andar", "Walking", -1, -1, true, original: true);
        static readonly Cut RifleRun = new("Fuzil_Correr", "rifle run", -1, -1, true, original: true);
        static readonly Cut RifleAim = new("Fuzil_Mira", "rifle aiming idle", -1, -1, true, original: true);
        static readonly Cut RifleFire = new("Fuzil_Disparo", "firing rifle", -1, -1, false, original: true);
        // Golpe pelas costas (a facada do Samir): só a queda de frente, sem os passos do começo.
        static readonly Cut DeathForward = new(SoldierAnimator.ForwardDeathState, "walking to dying", 40, 92, false);
        static readonly Cut[] RifleCuts = { RifleIdle, RifleWalk, RifleRun, RifleAim, RifleFire, DeathForward };

        [MenuItem("Nibiru/Personagens/Montar soldados da Fundação")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar os soldados.");
                return;
            }
            AssetDatabase.Refresh();
            foreach (string character in Characters)
                ImportCharacter(character);
            CopyMixamo(RifleCuts.Select(c => c.file).Distinct());
            foreach (string file in RifleCuts.Select(c => c.file).Distinct())
                ConfigureClips(file, RifleCuts);

            var controller = BuildController();
            if (controller == null) return;
            var pilotController = BuildPilotController();
            foreach (string character in Characters.Where(c => c != "Piloto"))
                BuildSoldierVisual(character, controller);
            BuildPilotVisual(pilotController);
            AssetDatabase.SaveAssets();
            // Os controladores foram apagados e criados de novo: sem reimportar, a cópia dos prefabs
            // na memória continua apontando para os antigos (o Animator fica sem controlador).
            foreach (string character in Characters)
                AssetDatabase.ImportAsset(VisualPrefab(character), ImportAssetOptions.ForceUpdate);

            BuildAIPrefab(SoldierPrefab, "Soldado");
            BuildVariant(LeaderPrefab, "LiderDeEsquadra");
            BuildVariant(SniperPrefab, "AtiradorDeElite");
            AssetDatabase.SaveAssets();
            foreach (string prefab in new[] { SoldierPrefab, LeaderPrefab, SniperPrefab })
                AssetDatabase.ImportAsset(prefab, ImportAssetOptions.ForceUpdate);
            ReplaceInLab();
            Debug.Log("Soldados prontos. Para as cenas da região, rode Nibiru > Conteúdo > Povoar Acampamento, Trincheiras e Aldeia (nessa ordem) com a região aberta.");
        }

        public static string VisualPrefab(string character) => VisualFolder + character + ".prefab";

        // ---------- Modelo e material ----------

        // Cópia do FBX do AccuRig e das texturas do Meshy, se ainda não estiverem na Unity.
        // maxTexture: se maior que zero, as cópias em Assets da cor e do normal são reduzidas a esse
        // tamanho (o original continua na ArteFonte).
        public static void ImportCharacter(string character, int maxTexture = 0)
        {
            string root = ArtRoot + character;
            string source = Path.Combine(Application.dataPath, "..", "ArteFonte", character);
            Directory.CreateDirectory(root);
            string model = $"{root}/{character}.fbx";
            if (!File.Exists(model))
            {
                string rig = Directory.GetFiles(source, character + ".Rig.fbx", SearchOption.AllDirectories).FirstOrDefault();
                if (rig == null)
                {
                    Debug.LogError($"{character}: não achei {character}.Rig.fbx em ArteFonte/{character}.");
                    return;
                }
                File.Copy(rig, model);
            }
            foreach (var (suffix, name) in new[] { ("texture", "BaseColor"), ("texture_normal", "Normal"), ("texture_metallic", "Metallic"), ("texture_roughness", "Roughness") })
            {
                string target = $"{root}/{character}_{name}.png";
                if (File.Exists(target)) continue;
                string png = Directory.GetFiles(Path.Combine(source, "1_Meshy"), $"Meshy_AI_*_{suffix}.png").FirstOrDefault();
                if (png == null)
                {
                    Debug.LogError($"{character}: falta a textura {name} (baixe de novo no Meshy para ArteFonte/{character}/1_Meshy).");
                    return;
                }
                File.Copy(png, target);
                if (maxTexture > 0) ShrinkPng(target, maxTexture);
            }
            AssetDatabase.Refresh();

            string metalSmooth = $"{root}/{character}_MetalSmooth.png";
            BuildMetalSmooth($"{root}/{character}_Metallic.png", $"{root}/{character}_Roughness.png", metalSmooth);
            SetTexture($"{root}/{character}_BaseColor.png", TextureImporterType.Default, true);
            SetTexture($"{root}/{character}_Normal.png", TextureImporterType.NormalMap, false);
            SetTexture(metalSmooth, TextureImporterType.Default, false);
            var material = LitMaterial($"{root}/Materiais/{character}.mat", $"{root}/{character}_BaseColor.png", $"{root}/{character}_Normal.png", metalSmooth);
            ConfigureModel(model, character, material);
        }

        static void CopyMixamo(System.Collections.Generic.IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                if (File.Exists(file)) continue;
                string source = Path.Combine(Application.dataPath, "..", "ArteFonte", "Animacoes", "Mixamo", Path.GetFileName(file));
                File.Copy(source, file);
            }
            AssetDatabase.Refresh();
        }

        // ---------- Controlador dos soldados ----------

        static AnimatorController BuildController()
        {
            var idle = Clip("rifle aiming idle", RifleIdle.name);
            var walk = Clip("Walking", RifleWalk.name);
            var run = Clip("rifle run", RifleRun.name);
            var aim = Clip("rifle aiming idle", RifleAim.name);
            var fire = Clip("firing rifle", RifleFire.name);
            var deathForward = Clip("walking to dying", DeathForward.name);
            // Cortes da Elena (ElenaSetup), retargetados pelo Humanoid.
            var reload = Clip("Reloading", "Recarregar");
            var hurt = Clip("Hit Reaction", "Dano");
            var death = Clip("Standing React Death Backward", "Morte");
            if (new[] { idle, walk, run, aim, fire, deathForward, reload, hurt, death }.Any(c => c == null))
            {
                Debug.LogError("Soldados: faltam cortes do Mixamo. Rode antes Nibiru > Personagens > Montar Elena animada.");
                return null;
            }
            var settings = AssetDatabase.LoadAssetAtPath<SoldierSettings>(SoldierSettingsPath);

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SoldierAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            foreach (string trigger in new[] { SoldierAnimator.FireParam, SoldierAnimator.HurtParam, SoldierAnimator.DieParam })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            // Locomoção pela velocidade: parado, patrulha (andar) e perseguição (correr).
            var machine = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController(SoldierAnimator.LocomotionState, out BlendTree tree, 0);
            Speeds(tree, SoldierAnimator.SpeedParam,
                (idle, 0f, 0f),
                (walk, NaturalSpeed(walk), NaturalSpeed(walk)),
                (walk, NaturalSpeed(walk), settings.patrolSpeed),
                (run, NaturalSpeed(run), Mathf.Max(NaturalSpeed(run), settings.chaseSpeed + 0.3f)));

            // Morte: cai e fica no chão.
            var dying = machine.AddState(SoldierAnimator.DeathState, new Vector3(300f, 60f));
            dying.motion = death;
            var toDeath = machine.AddAnyStateTransition(dying);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, SoldierAnimator.DieParam);
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;
            // A queda de frente entra direto pelo SoldierAnimator (DieForward).
            machine.AddState(SoldierAnimator.ForwardDeathState, new Vector3(300f, 120f)).motion = deathForward;

            // Mira (só o tronco): o fuzil apontado; o disparo entra por cima e volta para a mira.
            var mask = BuildUpperBodyMask();
            var aimLayer = AddLayer(controller, SoldierAnimator.AimLayer, mask, AnimatorLayerBlendingMode.Override, 0f);
            var aiming = aimLayer.AddState("Mira");
            aiming.motion = aim;
            var firing = aimLayer.AddState("Disparo", new Vector3(300f, 60f));
            firing.motion = fire;
            firing.speed = 1.6f;
            var toFire = aimLayer.AddAnyStateTransition(firing);
            toFire.AddCondition(AnimatorConditionMode.If, 0f, SoldierAnimator.FireParam);
            toFire.hasExitTime = false;
            toFire.duration = 0.03f;
            toFire.canTransitionToSelf = true;
            var backToAim = firing.AddTransition(aiming);
            backToAim.hasExitTime = true;
            backToAim.exitTime = 0.9f;
            backToAim.duration = 0.1f;

            // Recarregar (só o tronco), no tempo da recarga da IA; o peso vem do SoldierAnimator.
            var reloading = AddLayer(controller, SoldierAnimator.ReloadLayer, mask, AnimatorLayerBlendingMode.Override, 0f)
                .AddState(SoldierAnimator.ReloadState);
            reloading.motion = reload;
            reloading.speed = reload.length / settings.reloadTime;

            // Dano: aditivo, por cima de tudo (como na Elena).
            var reactions = AddLayer(controller, "Reacoes", mask, AnimatorLayerBlendingMode.Additive, 0.6f);
            var calm = reactions.AddState("Nada");
            reactions.defaultState = calm;
            var hit = reactions.AddState("Dano", new Vector3(300f, 60f));
            hit.motion = hurt;
            hit.speed = 1.3f;
            var toHit = reactions.AddAnyStateTransition(hit);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, SoldierAnimator.HurtParam);
            toHit.hasExitTime = false;
            toHit.duration = 0.05f;
            toHit.canTransitionToSelf = true;
            var calmDown = hit.AddTransition(calm);
            calmDown.hasExitTime = true;
            calmDown.exitTime = 0.85f;
            calmDown.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimatorStateMachine AddLayer(AnimatorController controller, string name, AvatarMask mask,
            AnimatorLayerBlendingMode blending, float weight)
        {
            controller.AddLayer(name);
            var layers = controller.layers;
            var layer = layers[layers.Length - 1];
            layer.avatarMask = mask;
            layer.defaultWeight = weight;
            layer.blendingMode = blending;
            controller.layers = layers;
            return layer.stateMachine;
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

        // ---------- Piloto ----------

        // O Mixamo da pasta não tem ninguém sentado: a pose é feita aqui, pelos músculos do Humanoid.
        static readonly (string muscle, float value)[] SeatedPose =
        {
            ("Spine Front-Back", 0.15f),
            ("Chest Front-Back", 0.1f),
            ("Head Nod Down-Up", -0.1f),
            ("Left Upper Leg Front-Back", -0.95f),
            ("Right Upper Leg Front-Back", -0.95f),
            ("Left Upper Leg In-Out", -0.15f),
            ("Right Upper Leg In-Out", -0.15f),
            ("Left Lower Leg Stretch", -0.75f),
            ("Right Lower Leg Stretch", -0.75f),
            ("Left Foot Up-Down", 0.2f),
            ("Right Foot Up-Down", 0.2f),
            ("Left Arm Down-Up", -0.55f),
            ("Right Arm Down-Up", -0.55f),
            ("Left Arm Front-Back", 0.35f),
            ("Right Arm Front-Back", 0.45f),
            ("Left Forearm Stretch", -0.35f),
            ("Right Forearm Stretch", -0.45f),
            ("Left Hand In-Out", 0f),
            ("Right Hand In-Out", 0f),
        };

        static AnimatorController BuildPilotController()
        {
            AssetDatabase.DeleteAsset(SeatedClipPath);
            var clip = new AnimationClip { name = "Piloto_Sentado" };
            void Constant(string property, float value) =>
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), property),
                    AnimationCurve.Constant(0f, 1f, value));
            Constant("RootT.x", 0f);
            Constant("RootT.y", 0.6f);
            Constant("RootT.z", 0f);
            Constant("RootQ.x", 0f);
            Constant("RootQ.y", 0f);
            Constant("RootQ.z", 0f);
            Constant("RootQ.w", 1f);
            foreach (string muscle in HumanTrait.MuscleName)
            {
                var pose = SeatedPose.FirstOrDefault(p => p.muscle == muscle);
                Constant(muscle, pose.muscle != null ? pose.value : 0f);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, SeatedClipPath);

            AssetDatabase.DeleteAsset(PilotControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(PilotControllerPath);
            controller.layers[0].stateMachine.AddState("Sentado").motion = clip;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Piloto > Modelo, com os quadris logo acima da raiz: a raiz vai no assento da cabine.
        static void BuildPilotVisual(AnimatorController controller)
        {
            var root = new GameObject("Piloto");
            try
            {
                var animator = AddModel(root.transform, "Piloto", controller);
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                Pose(animator, "Sentado", 0, 0f);
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Vector3 offset = root.transform.InverseTransformPoint(hips.position);
                animator.transform.localPosition = new Vector3(0f, 0.1f - offset.y, -offset.z);
                ResetPose(animator);
                PrefabUtility.SaveAsPrefabAsset(root, PilotPrefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------- Soldados ----------

        // Soldado > Modelo (o FBX, com o Animator e o fuzil na mão direita), com o SoldierAnimator na raiz.
        static void BuildSoldierVisual(string character, AnimatorController controller)
        {
            var root = new GameObject(character);
            try
            {
                var animator = AddModel(root.transform, character, controller);
                float twist = AddRifle(animator);
                var soldierAnimator = root.AddComponent<SoldierAnimator>();
                var serialized = new SerializedObject(soldierAnimator);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("aimTwist").floatValue = Mathf.Round(twist);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, VisualPrefab(character));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Animator AddModel(Transform root, string character, AnimatorController controller)
        {
            Directory.CreateDirectory(VisualFolder);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtRoot}{character}/{character}.fbx"));
            model.name = ModelChild;
            model.transform.SetParent(root, false);
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return animator;
        }

        // Põe o modelo numa pose do controlador, no editor (para medir e encaixar peças).
        static void Pose(Animator animator, string state, int layer, float time)
        {
            // Sem câmera olhando, o Animator do editor não mexe nos ossos (fica em pose T).
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            if (layer > 0) animator.SetLayerWeight(layer, 1f);
            animator.Play(state, layer, time);
            animator.Update(0f);
        }

        static void ResetPose(Animator animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        // Fuzil provisório em formas simples (coronha, corpo, carregador, cano), preso à mão direita.
        // O encaixe é medido na pose de mira: a empunhadura na palma direita e o cano na direção da
        // mão esquerda (que segura a frente do fuzil). Devolve quanto o tronco precisa girar (graus)
        // para o cano apontar para a frente do soldado.
        static float AddRifle(Animator animator)
        {
            Pose(animator, "Mira", animator.GetLayerIndex(SoldierAnimator.AimLayer), 0.3f);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightMiddle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            Vector3 grip = Vector3.Lerp(rightHand.position, rightMiddle.position, 0.6f);
            Vector3 forward = (Vector3.Lerp(leftHand.position, animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).position, 0.5f) - grip).normalized;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;

            var rifle = new GameObject(RifleChild).transform;
            rifle.SetPositionAndRotation(grip, Quaternion.LookRotation(forward, up));
            rifle.SetParent(rightHand, true);

            const string dark = MetalMaterial;
            Piece(rifle, "Empunhadura", new Vector3(0f, -0.06f, -0.01f), new Vector3(0.03f, 0.1f, 0.04f), dark, -15f);
            Piece(rifle, "Corpo", new Vector3(0f, 0.03f, 0.08f), new Vector3(0.05f, 0.08f, 0.36f), dark);
            Piece(rifle, "Carregador", new Vector3(0f, -0.06f, 0.13f), new Vector3(0.03f, 0.13f, 0.06f), dark, 15f);
            Piece(rifle, "Guarda_Mao", new Vector3(0f, 0.025f, 0.34f), new Vector3(0.045f, 0.06f, 0.2f), WoodMaterial);
            Piece(rifle, "Tubo", new Vector3(0f, 0.035f, 0.52f), new Vector3(0.018f, 0.018f, 0.2f), dark);
            Piece(rifle, "Coronha", new Vector3(0f, 0.0f, -0.2f), new Vector3(0.04f, 0.1f, 0.22f), WoodMaterial);
            var muzzle = new GameObject(MuzzleChild).transform;
            muzzle.SetParent(rifle, false);
            muzzle.localPosition = new Vector3(0f, 0.035f, 0.63f);

            Vector3 flat = animator.transform.InverseTransformDirection(forward);
            ResetPose(animator);
            return -Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        static void Piece(Transform parent, string name, Vector3 position, Vector3 scale, string material, float pitch = 0f)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            piece.transform.localScale = scale;
            var renderer = piece.GetComponent<Renderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(material);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        /// <summary>O cano do fuzil de um soldado (filho do modelo), ou null.</summary>
        public static Transform MuzzleIn(Transform soldier) =>
            soldier.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == MuzzleChild && t.parent != null && t.parent.name == RifleChild);

        // ---------- Prefabs com IA ----------

        // O prefab do soldado com IA que já existia: sai a cápsula (forma, capacete, faixa e arma),
        // entra o modelo como filho "Visual", com os pés no chão. O colisor fica na altura dele.
        static void BuildAIPrefab(string path, string character)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(root.GetComponent<MeshFilter>());
                foreach (string old in new[] { "Capacete", "Faixa", "Arma", MuzzleChild, VisualChild })
                {
                    var child = root.transform.Find(old);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
                float height = CharacterHeights.Meters[character];
                var capsule = root.GetComponent<CapsuleCollider>();
                capsule.radius = 0.35f;
                capsule.height = height;
                capsule.center = new Vector3(0f, FeetBelowRoot + height * 0.5f, 0f);
                SetVisual(root, character);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Variante do Soldado com outro modelo (a IA, a vida e o colisor vêm do Soldado).
        static void BuildVariant(string path, string character)
        {
            if (!File.Exists(path))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefab));
                instance.name = Path.GetFileNameWithoutExtension(path);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // O modelo do Soldado (que vem do prefab base) e o desta variante, de uma montagem anterior.
                foreach (Transform old in root.transform.Cast<Transform>().Where(t => t.name == VisualChild).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                SetVisual(root, character);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SetVisual(GameObject root, string character)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefab(character)), root.transform);
            visual.name = VisualChild;
            visual.transform.SetLocalPositionAndRotation(new Vector3(0f, FeetBelowRoot, 0f), Quaternion.identity);
            visual.transform.SetSiblingIndex(0);
            var ai = new SerializedObject(root.GetComponent<SoldierAI>());
            ai.FindProperty("muzzle").objectReferenceValue = MuzzleIn(visual.transform);
            ai.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Laboratório ----------

        // Os soldados do laboratório já são o prefab Soldado (mudam sozinhos); o Líder vira o Soldado_Lider,
        // mantendo o que foi mudado nele (vida, cor de antes, o aviso ao morrer).
        static void ReplaceInLab()
        {
            var scene = SceneManager.GetSceneByPath(LabScenePath);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen) scene = EditorSceneManager.OpenScene(LabScenePath, OpenSceneMode.Additive);
            var leaderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeaderPrefab);
            foreach (var ai in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SoldierAI>(true)))
            {
                if (!ai.IsLeader || PrefabUtility.GetCorrespondingObjectFromSource(ai.gameObject) == leaderPrefab) continue;
                string name = ai.name;
                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(ai.gameObject, leaderPrefab, InteractionMode.AutomatedAction);
                ai.name = name;
                Debug.Log($"Soldados: {ai.name} do laboratório agora usa o modelo do Líder de esquadra.");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
