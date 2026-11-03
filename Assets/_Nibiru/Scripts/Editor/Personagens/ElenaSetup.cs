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
    /// Monta a Elena animada no prefab Prefabs/Player/Elena: configura a importação do FBX do
    /// AccuRig (Humanoid, com dedos, na altura de 1,70 m) e das animações do Mixamo (Humanoid, com o
    /// avatar do X Bot em pose T), cria o material URP, o controlador Elena.controller (ligado ao
    /// PlayerMotor e ao combate pelo PlayerAnimator) e põe o modelo no filho "Jogador".
    /// Pode rodar de novo: refaz controlador, máscara e modelo no prefab; o material só é atualizado.
    /// Menu: Nibiru > Personagens > Montar Elena animada.
    /// </summary>
    static class ElenaSetup
    {
        const string Root = "Assets/_Nibiru/Art/Personagens/Elena";
        // O modelo novo (só textura de cor). A Elena antiga fica na pasta Root, como reserva; o
        // controlador, a máscara e a marca do pulso continuam lá, porque valem para os dois modelos.
        const string ModelRoot = "Assets/_Nibiru/Art/Personagens/ElenaNova";
        const string ModelPath = ModelRoot + "/ElenaNova.fbx";
        const string BaseColorPath = ModelRoot + "/ElenaNova_BaseColor.jpg";
        // Até onde cada braço desce no Avatar, em graus (fotografado em 3/nov/2026). O esquerdo fica no padrão da
        // Unity (-60°): braço junto do corpo, natural. O direito desce um pouco menos (-55°) por causa do coldre, que
        // é parte da malha; a luva só toca a borda dele. Com -40/-36 os braços ficavam afastados, "de manequim".
        const float LeftArmDownLimit = -60f;
        const float RightArmDownLimit = -55f;
        // Máximo do "Forearm Stretch": o padrão da Unity é +80° (faixa de -80 a +80). Um valor menor trava o
        // antebraço dobrado (com -15° o cotovelo nunca passava de 40° de dobra: braço de manequim). Fica no padrão.
        const float ForearmStretchLimit = 80f;
        const string MaterialPath = ModelRoot + "/Materiais/ElenaNova.mat";
        const string Mixamo = "Assets/_Nibiru/Art/Animacoes/Mixamo/";
        // Boneco do Mixamo em pose T: o esqueleto de referência de todas as animações do Mixamo.
        const string MixamoReferencePath = Mixamo + "X Bot.fbx";
        const string ControllerPath = Root + "/Elena.controller";
        const string MaskPath = Root + "/Elena_Tronco.mask";
        const string MarkMaterialPath = Root + "/Materiais/Elena_MarcaKur.mat";
        const string PrefabPath = "Assets/_Nibiru/Prefabs/Player/Elena.prefab";
        const string SettingsPath = "Assets/_Nibiru/Data/Player/MovimentoJogador.asset";
        const string KnifePath = "Assets/_Nibiru/Data/Combat/Faca.asset";
        const string PistolPath = "Assets/_Nibiru/Data/Combat/Pistola.asset";
        const string ModelChild = "Modelo";
        const string CapsuleChild = "Corpo";
        const string GunChild = "ArmaNaMao";
        const string KnifeChild = "PivoFaca";
        const string BladeChild = "Lamina";

        /// <summary>
        /// Um trecho de animação usado pelo controlador. Quadros contados como no Mixamo e no Blender
        /// (o primeiro é 1); -1 = clipe inteiro.
        /// moving: a animação anda; a velocidade em que os pés não escorregam é lida do próprio clipe (já na escala da Elena).
        /// feet: a altura vem dos pés (o pé mais baixo fica no chão), em vez da altura original do clipe.
        /// keepTravel: o deslocamento do corpo fica na pose (a queda da morte vai mesmo para trás).
        /// mirror: espelha esquerda e direita (golpe feito com a outra mão).
        /// original: o giro segue o arquivo ("Based Upon: Original"), e não a direção do corpo.
        /// Para trocar uma animação, mude o arquivo e os quadros aqui e rode o menu de novo.
        /// </summary>
        readonly struct Cut
        {
            public readonly string name, file;
            public readonly int first, last;
            public readonly bool loop, moving, feet, keepTravel, mirror, original;

            public Cut(string name, string file, int first, int last, bool loop,
                bool moving = false, bool feet = false, bool keepTravel = false, bool mirror = false, bool original = false)
            {
                this.name = name; this.file = Mixamo + file + ".fbx";
                this.first = first; this.last = last; this.loop = loop;
                this.moving = moving; this.feet = feet; this.keepTravel = keepTravel; this.mirror = mirror;
                this.original = original;
            }
        }

        // Parada, andar e correr vêm da Female Basic Locomotion (veja UseFemaleLocomotion). O resto não
        // tem versão feminina na biblioteca e é universal.
        // O andar furtivo; a agachada parada é a Elena_Agachada_Parada da biblioteca (veja UseCrouch).
        static readonly Cut CrouchWalk = new("Agachada_Andando", "Sneak Walk", -1, -1, true, moving: true, feet: true);
        // Só o mergulho e a cambalhota, sem o levantar do fim.
        static readonly Cut Dodge = new("Esquiva", "Stand To Roll", 10, 50, false);
        // Quadro da mira de pistola com os dois braços esticados (depois do 90 ela abaixa a arma).
        static readonly Cut Aim = new("Mira", "Pistol Aim", 20, 21, true);
        static readonly Cut Reload = new("Recarregar", "Reloading", 8, 92, false);
        // A facada, do começo do bote até recolher o braço (pico no quadro 29). No Mixamo ela é com a
        // mão esquerda; espelhada, fica na direita, onde está a faca.
        static readonly Cut Slash = new("Golpe_Faca", "Stabbing", 15, 37, false, mirror: true, original: true);
        // Somado por cima do que ela estiver fazendo (camada aditiva): só o tranco do golpe.
        static readonly Cut Hurt = new("Dano", "Hit Reaction", 13, 60, false);
        static readonly Cut Death = new("Morte", "Standing React Death Backward", -1, -1, false, keepTravel: true);
        static readonly Cut[] Cuts = { CrouchWalk, Dodge, Aim, Reload, Slash, Hurt, Death };

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
            var mixamoAvatar = ConfigureMixamoReference();
            foreach (string file in Cuts.Select(c => c.file).Distinct())
                ConfigureAnimations(file, mixamoAvatar);
            var controller = BuildController();
            BuildPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Elena animada pronta no prefab " + PrefabPath + ".");
        }

        // ---------- Texturas e material ----------

        // A textura vem do Meshy com 8192 px. No jogo, 4096: sem normal nem metal, a memória fica
        // menor que a dos 3 mapas de 2048 da Elena antiga, e os detalhes pintados aguentam a câmera perto.
        static void ConfigureTextures()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(BaseColorPath);
            if (importer.maxTextureSize == 4096 && importer.sRGBTexture)
                return;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        // O modelo novo só tem a textura de cor: sem normal e sem mapa de metal e lisura.
        static Material BuildMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.25f);
            // O AccuRig marca o material como de dois lados (mangas e gola mostram o avesso).
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Só o modelo ----------

        // Troca a malha e o Avatar no prefab sem refazer o controlador nem as animações.
        [MenuItem("Nibiru/Personagens/Trocar só o modelo da Elena")]
        static void SwapModel()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de trocar o modelo da Elena.");
                return;
            }
            AssetDatabase.Refresh();
            ConfigureTextures();
            ConfigureModel(BuildMaterial());
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(SettingsPath);
            UseFemaleLocomotion(LocomotionTree(controller, "EmPe"), settings);
            UseCrouch(LocomotionTree(controller, "Agachada"), settings);
            EditorUtility.SetDirty(controller);
            BuildPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Modelo da Elena trocado no prefab " + PrefabPath + ".");
        }

        // ---------- Importação ----------

        static void ConfigureModel(Material material)
        {
            // A escala vem antes do avatar: o Humanoid guarda o tamanho dos ossos e é refeito do zero
            // a cada execução (senão ele fica com as medidas de antes do ajuste).
            CharacterHeights.FitImportScale(ModelPath, "Elena");
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.humanDescription = new HumanDescription();
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            foreach (var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentsInChildren<Renderer>())
                foreach (var original in renderer.sharedMaterials.Where(m => m != null && m != material))
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
            importer.SaveAndReimport();
            OpenArms(importer);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"Avatar da Elena: {(avatar != null && avatar.isValid && avatar.isHuman ? "Humanoid válido" : "PROBLEMA no Humanoid")}");
        }

        // O modelo novo tem braços longos e coxas largas (calça e coldre): com os limites padrão, as
        // mãos entram nas coxas. Encurtar só o "Arm Down-Up" para baixo (padrão -60°) deixa o braço
        // caído um pouco mais aberto; mira, recarga e golpe, de braço erguido, quase não mudam.
        static void OpenArms(ModelImporter importer) => OpenArms(importer, LeftArmDownLimit, RightArmDownLimit, ForearmStretchLimit);

        internal static void OpenArms(ModelImporter importer, float leftDown, float rightDown, float forearmMax)
        {
            var description = importer.humanDescription;
            var human = description.human;
            for (int i = 0; i < human.Length; i++)
            {
                HumanBodyBones bone;
                if (human[i].humanName == nameof(HumanBodyBones.LeftUpperArm)) bone = HumanBodyBones.LeftUpperArm;
                else if (human[i].humanName == nameof(HumanBodyBones.RightUpperArm)) bone = HumanBodyBones.RightUpperArm;
                else if (human[i].humanName == nameof(HumanBodyBones.LeftLowerArm)) bone = HumanBodyBones.LeftLowerArm;
                else if (human[i].humanName == nameof(HumanBodyBones.RightLowerArm)) bone = HumanBodyBones.RightLowerArm;
                else continue;

                Vector3 min = default, max = default;
                for (int axis = 0; axis < 3; axis++)
                {
                    int muscle = HumanTrait.MuscleFromBone((int)bone, axis);
                    if (muscle < 0) continue;
                    min[axis] = HumanTrait.GetMuscleDefaultMin(muscle);
                    max[axis] = HumanTrait.GetMuscleDefaultMax(muscle);
                }
                if (bone == HumanBodyBones.LeftUpperArm) min.z = leftDown;
                else if (bone == HumanBodyBones.RightUpperArm) min.z = rightDown;
                else max.z = forearmMax;
                var limit = human[i].limit;
                limit.useDefaultValues = false;
                limit.min = min;
                limit.max = max;
                limit.center = Vector3.zero;
                human[i].limit = limit;
            }
            // O mapeamento automático deixa de fora a ponta do polegar esquerdo (ossos que vêm sem peso no FBX).
            var mapped = human.ToList();
            AddIfMissing(mapped, "Left Thumb Intermediate", "CC_Base_L_Thumb2");
            AddIfMissing(mapped, "Left Thumb Distal", "CC_Base_L_Thumb3");
            description.human = mapped.ToArray();
            importer.humanDescription = description;
            importer.SaveAndReimport();
        }

        static void AddIfMissing(System.Collections.Generic.List<HumanBone> human, string humanName, string boneName)
        {
            if (human.Any(b => b.humanName == humanName)) return;
            human.Add(new HumanBone { humanName = humanName, boneName = boneName, limit = new HumanLimit { useDefaultValues = true } });
        }

        // As animações do Mixamo baixadas sem pele não têm pose T: o avatar feito delas mesmas parte do
        // primeiro quadro, e o erro vai para a animação inteira (na corrida, o corpo ficava girado 40°).
        // Todas usam o avatar do X Bot, que está em pose T e tem os mesmos nomes de ossos.
        static Avatar ConfigureMixamoReference()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(MixamoReferencePath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(MixamoReferencePath).OfType<Avatar>().First();
            Debug.Log($"Avatar de referência do Mixamo (X Bot): {(avatar.isValid && avatar.isHuman ? "Humanoid válido" : "PROBLEMA no Humanoid")}");
            return avatar;
        }

        static void ConfigureAnimations(string path, Avatar mixamoAvatar)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = mixamoAvatar;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;

            var source = importer.defaultClipAnimations[0];
            // O Unity conta os quadros a partir do primeiro do take; nos cortes, o primeiro é 1.
            float offset = source.firstFrame - 1f;
            var clips = new System.Collections.Generic.List<ModelImporterClipAnimation>();
            foreach (var cut in Cuts.Where(c => c.file == path))
            {
                var clip = new ModelImporterClipAnimation
                {
                    name = cut.name,
                    takeName = source.takeName,
                    firstFrame = cut.first >= 0 ? cut.first + offset : source.firstFrame,
                    lastFrame = cut.last >= 0 ? cut.last + offset : source.lastFrame,
                };
                Prepare(clip, cut);
                clips.Add(clip);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
        }

        // O movimento é do PlayerMotor: o avanço da animação sai para a raiz (e é descartado, sem
        // root motion), a altura e o giro ficam na pose. Na morte, o avanço também fica na pose.
        // "Based Upon: Body Orientation" no giro: o corpo aponta para a frente do Jogador. Algumas
        // animações do Mixamo (Run, Sneak Walk) correm na diagonal, com o corpo girado uns 35°.
        // A facada é a exceção: o corpo fica de lado na estocada, e só o giro original manda o braço
        // para a frente.
        static void Prepare(ModelImporterClipAnimation clip, Cut cut)
        {
            clip.loopTime = cut.loop;
            clip.mirror = cut.mirror;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = cut.original;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = !cut.feet;
            clip.heightFromFeet = cut.feet;
            clip.lockRootPositionXZ = cut.keepTravel;
            clip.keepOriginalPositionXZ = cut.keepTravel;
            // Na camada aditiva (dano), a pose de referência é o primeiro quadro do trecho.
            clip.hasAdditiveReferencePose = false;
        }

        // Velocidade (m/s) em que os pés não escorregam: o avanço médio do clipe, na escala da Elena.
        static float NaturalSpeed(Cut cut) => cut.moving ? Clip(cut).averageSpeed.magnitude : 0f;

        static AnimationClip Clip(Cut cut) =>
            AssetDatabase.LoadAllAssetsAtPath(cut.file).OfType<AnimationClip>().First(c => c.name == cut.name);

        // ---------- Controlador ----------

        const string AnimationLibraryDestination = "Assets/_Nibiru/Art/Animacoes/Elena/";
        static int IdleVariationCount;

        static AnimatorController BuildController()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(SettingsPath);
            var knife = AssetDatabase.LoadAssetAtPath<MeleeAttackDefinition>(KnifePath);
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
            foreach (string trigger in new[] { PlayerAnimator.DodgeParam, PlayerAnimator.MeleeParam, PlayerAnimator.HurtParam, PlayerAnimator.DieParam })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

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
            UseFemaleLocomotion(standing, settings);

            var crouched = crouchBlend.CreateBlendTreeChild(1f);
            crouched.name = "Agachada";
            UseCrouch(crouched, settings);

            // Esquiva e golpe de faca: cada trecho toca no tempo exato da ação no jogo.
            OneShot(machine, locomotion, Dodge, PlayerAnimator.DodgeParam, settings.dodgeDuration, new Vector3(300f, 60f), 0.05f);
            OneShot(machine, locomotion, Slash, PlayerAnimator.MeleeParam, knife.TotalTime, new Vector3(300f, 140f), 0.08f);

            // Morte: cai e fica no chão (sem volta; a tela de morte recomeça a cena).
            var death = machine.AddState("Morte", new Vector3(300f, 220f));
            death.motion = Clip(Death);
            var toDeath = machine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.DieParam);
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;

            // Sentada no chão (a viagem na caçamba): entra e sai pelo parâmetro Sentada, ligado pelo TruckRide.
            var seated = AnimationLibrary.Clip("Elena_Sentada");
            if (seated != null)
            {
                controller.AddParameter(PlayerAnimator.SeatedParam, AnimatorControllerParameterType.Bool);
                var sit = machine.AddState("Sentada", new Vector3(300f, 280f));
                sit.motion = seated;
                var toSit = machine.AddAnyStateTransition(sit);
                toSit.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.SeatedParam);
                toSit.hasExitTime = false;
                toSit.duration = 0.25f;
                toSit.canTransitionToSelf = false;
                var stand = sit.AddTransition(locomotion);
                stand.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerAnimator.SeatedParam);
                stand.hasExitTime = false;
                stand.duration = 0.25f;
            }

            // Variações da parada: só o tronco, braços e cabeça, por cima da parada de baixo (sem arrastar os pés);
            // o PlayerAnimator escolhe a variação (parâmetro) e acende a camada só com a Elena parada.
            var mask = BuildUpperBodyMask();
            var variations = UalIdleImport.Variations
                .Select(v => AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationLibraryDestination + v.name + ".anim"))
                .Where(c => c != null).ToList();
            if (variations.Count > 0)
            {
                controller.AddParameter(PlayerAnimator.IdleVariationParam, AnimatorControllerParameterType.Int);
                var variationMachine = AddLayer(controller, PlayerAnimator.IdleVariationLayer, mask, AnimatorLayerBlendingMode.Override, 0f);
                var none = variationMachine.AddState("Nada");
                variationMachine.defaultState = none;
                for (int i = 0; i < variations.Count; i++)
                {
                    var state = variationMachine.AddState(variations[i].name, new Vector3(300f, 60f + 60f * i));
                    state.motion = variations[i];
                    var enterVariation = none.AddTransition(state);
                    enterVariation.AddCondition(AnimatorConditionMode.Equals, i + 1, PlayerAnimator.IdleVariationParam);
                    enterVariation.hasExitTime = false;
                    enterVariation.duration = 0.5f;
                    var leaveVariation = state.AddTransition(none);
                    leaveVariation.AddCondition(AnimatorConditionMode.Equals, 0f, PlayerAnimator.IdleVariationParam);
                    leaveVariation.hasExitTime = false;
                    leaveVariation.duration = 0.5f;
                }
            }
            IdleVariationCount = variations.Count;

            // Mira e recarregar: só o tronco, braços, dedos e cabeça; o peso vem do PlayerAnimator.
            AddLayer(controller, PlayerAnimator.AimLayer, mask, AnimatorLayerBlendingMode.Override, 0f)
                .AddState("Mira").motion = Clip(Aim);
            var pistol = AssetDatabase.LoadAssetAtPath<FirearmDefinition>(PistolPath);
            var reload = AddLayer(controller, PlayerAnimator.ReloadLayer, mask, AnimatorLayerBlendingMode.Override, 0f).AddState("Recarregar");
            var reloadClip = Clip(Reload);
            reload.motion = reloadClip;
            reload.speed = reloadClip.length / pistol.reloadTime;

            // Dano: aditivo, por cima de tudo; fica num estado vazio até levar um golpe. Com peso 1 o
            // tranco do Mixamo joga a cabeça para trás demais.
            var reactions = AddLayer(controller, "Reacoes", mask, AnimatorLayerBlendingMode.Additive, 0.55f);
            var calm = reactions.AddState("Nada");
            reactions.defaultState = calm;
            var hurt = reactions.AddState("Dano", new Vector3(300f, 60f));
            hurt.motion = Clip(Hurt);
            hurt.speed = 1.3f;
            var toHurt = reactions.AddAnyStateTransition(hurt);
            toHurt.AddCondition(AnimatorConditionMode.If, 0f, PlayerAnimator.HurtParam);
            toHurt.hasExitTime = false;
            toHurt.duration = 0.05f;
            toHurt.canTransitionToSelf = true;
            var calmDown = hurt.AddTransition(calm);
            calmDown.hasExitTime = true;
            calmDown.exitTime = 0.85f;
            calmDown.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // Um estado que entra pelo gatilho (de qualquer estado) e volta sozinho para a locomoção.
        static void OneShot(AnimatorStateMachine machine, AnimatorState back, Cut cut, string trigger,
            float gameDuration, Vector3 position, float blendIn)
        {
            var clip = Clip(cut);
            var state = machine.AddState(cut.name, position);
            state.motion = clip;
            state.speed = clip.length / gameDuration;
            var enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = blendIn;
            enter.canTransitionToSelf = false;
            var leave = state.AddTransition(back);
            leave.hasExitTime = true;
            leave.exitTime = 1f;
            leave.hasFixedDuration = true;
            leave.duration = 0.2f;
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

        // Cada animação entra na velocidade em que deve tocar. Abaixo da velocidade natural, ela
        // desacelera junto; acima, acelera só em parte (raiz quadrada): passos rápidos demais
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
                float natural = NaturalSpeed(cut);
                float ratio = natural > 0f && speed > 0f ? speed / natural : 1f;
                children[i].timeScale = ratio > 1f ? Mathf.Clamp(Mathf.Sqrt(ratio), 1f, 2.5f) : ratio;
            }
            tree.children = children;
        }

        static BlendTree LocomotionTree(AnimatorController controller, string name)
        {
            var locomotion = controller.layers[0].stateMachine.states.First(s => s.state.name == "Locomocao").state;
            return ((BlendTree)locomotion.motion).children.Select(c => c.motion).OfType<BlendTree>().First(t => t.name == name);
        }

        static void UseCrouch(BlendTree tree, PlayerMovementSettings settings)
        {
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = PlayerAnimator.SpeedParam;
            tree.useAutomaticThresholds = false;
            tree.children = new[]
            {
                new ChildMotion { motion = AnimationLibrary.Clip("Elena_Agachada_Parada"), threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = Clip(CrouchWalk), threshold = settings.crouchSpeed, timeScale = settings.crouchSpeed / NaturalSpeed(CrouchWalk) },
            };
            EditorUtility.SetDirty(tree);
        }

        // Parada, andar e correr femininos. O andar segue a regra de Speeds; a corrida natural (3,6 m/s)
        // acelera até a velocidade do jogo (1,67 vez a 6 m/s), a opção em que os pés menos escorregam
        // (comparada de lado com 1,51 vez e com o Fast Run em 1º/nov/2026).
        static void UseFemaleLocomotion(BlendTree tree, PlayerMovementSettings settings)
        {
            var idle = AnimationLibrary.Clip("Elena_Parada_Basic");
            var walk = AnimationLibrary.Clip("Elena_Andar_Basic");
            var run = AnimationLibrary.Clip("Elena_Correr_Basic");
            float walkNatural = walk.averageSpeed.magnitude;
            float runRatio = settings.runSpeed / run.averageSpeed.magnitude;
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = PlayerAnimator.SpeedParam;
            tree.useAutomaticThresholds = false;
            tree.children = new[]
            {
                new ChildMotion { motion = idle, threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = walk, threshold = walkNatural, timeScale = 1f },
                new ChildMotion { motion = walk, threshold = settings.walkSpeed, timeScale = Mathf.Sqrt(settings.walkSpeed / walkNatural) },
                new ChildMotion { motion = run, threshold = settings.runSpeed, timeScale = runRatio },
            };
            EditorUtility.SetDirty(tree);
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

                // Tira a cápsula (o "Corpo" direto do Jogador; o da arma fica) e o modelo anterior,
                // mesmo se o FBX dele já foi apagado (a Unity o mostra como "Modelo (Missing Prefab...)").
                foreach (Transform child in player.Cast<Transform>().ToArray())
                {
                    bool old = child.name == CapsuleChild || child.name == ModelChild
                        || (child.name.StartsWith(ModelChild) && PrefabUtility.IsPrefabAssetMissing(child.gameObject));
                    if (old) Object.DestroyImmediate(child.gameObject);
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

                // A faca provisória vira uma lâmina curta, que sai do punho da mão direita.
                var knife = player.Find(KnifeChild);
                var blade = knife != null ? knife.Find(BladeChild) : null;
                if (blade != null)
                {
                    blade.localPosition = new Vector3(0f, 0f, 0.14f);
                    blade.localScale = new Vector3(0.025f, 0.012f, 0.2f);
                }

                var playerAnimator = player.GetComponent<PlayerAnimator>();
                if (playerAnimator == null) playerAnimator = player.gameObject.AddComponent<PlayerAnimator>();
                var serialized = new SerializedObject(playerAnimator);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("motor").objectReferenceValue = player.GetComponent<PlayerMotor>();
                serialized.FindProperty("weapons").objectReferenceValue = player.GetComponent<PlayerWeapons>();
                serialized.FindProperty("melee").objectReferenceValue = player.GetComponent<PlayerMeleeAttack>();
                serialized.FindProperty("health").objectReferenceValue = player.GetComponent<Health>();
                serialized.FindProperty("heldGun").objectReferenceValue = player.Find(GunChild);
                serialized.FindProperty("heldKnife").objectReferenceValue = knife;
                serialized.FindProperty("idleVariationCount").intValue = IdleVariationCount;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // O agachar é da animação: o visual do agachar só desce câmera, arma e faca.
                var crouchVisual = player.GetComponent<PlayerCrouchVisual>();
                if (crouchVisual != null)
                {
                    var crouch = new SerializedObject(crouchVisual);
                    crouch.FindProperty("body").objectReferenceValue = null;
                    crouch.ApplyModifiedPropertiesWithoutUndo();
                }

                AddTransformationMark(player, animator);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // A marca azul no pulso, que aparece com a primeira tabuleta. O material é criado só se ainda não existir.
        static void AddTransformationMark(Transform player, Animator animator)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MarkMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", new Color(0.3f, 0.55f, 1f));
                AssetDatabase.CreateAsset(material, MarkMaterialPath);
            }

            var mark = player.GetComponent<Tablets.TransformationMark>();
            if (mark == null) mark = player.gameObject.AddComponent<Tablets.TransformationMark>();
            var serialized = new SerializedObject(mark);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("material").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
