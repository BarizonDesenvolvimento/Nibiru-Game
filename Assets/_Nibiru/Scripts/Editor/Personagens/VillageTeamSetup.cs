using System.IO;
using System.Linq;
using Nibiru.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Nibiru.EditorTools.CharacterSetupKit;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Monta os 6 personagens da Aldeia e da equipe com o kit comum: copia da ArteFonte o FBX do AccuRig
    /// e as texturas do Meshy (reduzidas a 2K em Assets), importa como Humanoid com dedos na altura da
    /// tabela, cria o material URP de dois lados e os prefabs em Prefabs/Personagens:
    /// - UmmKarim: em pé, com controlador (parada e andar pela Velocidade, gesto de conversa) e o NpcAnimator;
    /// - DraWeiss: sentada no chão (a mesma animação da Elena na caçamba);
    /// - Yusuf: deitado (Laying Idle);
    /// - ArqueologoVeterano, ArqueologaJovem e TrabalhadorEscavacao (equipe morta; também o Hassan): só o modelo,
    ///   sem animação e fora das cenas.
    /// Parada, andar e conversa são os cortes do Mixamo da Elena e do Samir (rode antes os menus deles).
    /// As cápsulas das cenas NÃO são trocadas aqui. Pode rodar de novo: refaz controladores e prefabs;
    /// os materiais só são atualizados.
    /// Menu: Nibiru > Personagens > Montar personagens da Aldeia e da equipe.
    /// </summary>
    static class VillageTeamSetup
    {
        public const string ArtRoot = "Assets/_Nibiru/Art/Personagens/";
        public const string PrefabFolder = "Assets/_Nibiru/Prefabs/Personagens/";
        public const int TextureSize = 2048;

        public static readonly string[] Characters =
            { "UmmKarim", "DraWeiss", "Yusuf", "ArqueologoVeterano", "ArqueologaJovem", "TrabalhadorEscavacao" };

        public static string PrefabPath(string character) => PrefabFolder + character + ".prefab";
        public static string ControllerPath(string character) => $"{ArtRoot}{character}/{character}.controller";

        [MenuItem("Nibiru/Personagens/Montar personagens da Aldeia e da equipe")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar os personagens.");
                return;
            }
            AssetDatabase.Refresh();
            foreach (string character in Characters)
                SoldiersSetup.ImportCharacter(character, TextureSize);

            // O clipe do Yusuf deitado vem da biblioteca; os cortes da Elena e do Samir já devem existir.
            ImportLibraryClip("Yusuf_Deitado");
            var idle = Clip("Idle", "Parada");
            var walk = Clip("WalkForward", "Andar");
            var talk = Clip("Talking", NpcAnimator.TalkStatePrefix + 1);
            var sitting = AnimationLibrary.Clip("Elena_Sentada");
            var lying = AnimationLibrary.Clip("Yusuf_Deitado");
            if (new[] { idle, walk, talk, sitting, lying }.Any(c => c == null))
            {
                Debug.LogError("Faltam animações. Rode antes Nibiru > Personagens > Montar Elena animada, Montar Samir animado e Nibiru > Animações > Importar clipes da biblioteca.");
                return;
            }

            Directory.CreateDirectory(PrefabFolder);
            BuildStanding("UmmKarim", "Umm Karim", idle, walk, talk);
            BuildSingleState("DraWeiss", "Sentada", sitting);
            BuildSingleState("Yusuf", "Deitado", lying);
            foreach (string character in new[] { "ArqueologoVeterano", "ArqueologaJovem", "TrabalhadorEscavacao" })
                BuildModelOnly(character);
            AssetDatabase.SaveAssets();
            // Os controladores foram apagados e criados de novo: sem reimportar, a cópia dos prefabs na
            // memória continua apontando para os antigos.
            foreach (string character in Characters)
                AssetDatabase.ImportAsset(PrefabPath(character), ImportAssetOptions.ForceUpdate);
            Debug.Log("Personagens da Aldeia e da equipe prontos em " + PrefabFolder + ". As cápsulas das cenas continuam no lugar; veja Nibiru > Testes > Personagens > Vitrine.");
        }

        static void ImportLibraryClip(string name)
        {
            var entry = AnimationLibrary.Get(name);
            if (!File.Exists(entry.AssetPath))
            {
                string from = AnimationLibrary.Library + entry.source + ".fbx";
                if (!File.Exists(from)) { Debug.LogError("Animação não encontrada na biblioteca: " + from); return; }
                Directory.CreateDirectory(Path.GetDirectoryName(entry.AssetPath));
                File.Copy(from, entry.AssetPath);
            }
            AssetDatabase.ImportAsset(entry.AssetPath, ImportAssetOptions.ForceUpdate);
        }

        // ---------- Controladores ----------

        static AnimatorController NewController(string character)
        {
            AssetDatabase.DeleteAsset(ControllerPath(character));
            return AnimatorController.CreateAnimatorControllerAtPath(ControllerPath(character));
        }

        // Um estado só, tocando o clipe em laço (Weiss sentada, Yusuf deitado).
        static void BuildSingleState(string character, string state, AnimationClip clip)
        {
            var controller = NewController(character);
            controller.layers[0].stateMachine.AddState(state).motion = clip;
            EditorUtility.SetDirty(controller);
            SavePrefab(character, controller, null, clip);
        }

        // Em pé: parada e andar pela Velocidade (como o NavMeshAgent), e um gesto de conversa nas falas dela.
        static void BuildStanding(string character, string speaker, AnimationClip idle, AnimationClip walk, AnimationClip talk)
        {
            var controller = NewController(character);
            controller.AddParameter(NpcAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            // O NpcAnimator também escreve o Agachar a cada quadro: sem o parâmetro, o Unity avisa no Console.
            controller.AddParameter(NpcAnimator.CrouchParam, AnimatorControllerParameterType.Float);
            controller.CreateBlendTreeInController(NpcAnimator.LocomotionState, out BlendTree tree, 0);
            Speeds(tree, NpcAnimator.SpeedParam,
                (idle, 0f, 0f),
                (walk, NaturalSpeed(walk), NaturalSpeed(walk)));
            var talking = controller.layers[0].stateMachine.AddState(NpcAnimator.TalkStatePrefix + 1, new Vector3(300f, 100f));
            talking.motion = talk;
            EditorUtility.SetDirty(controller);
            SavePrefab(character, controller, speaker);
        }

        // ---------- Prefabs ----------

        static void BuildModelOnly(string character) => SavePrefab(character, null, null);

        // Personagem > Modelo (o FBX, com o Animator). speaker != null liga o NpcAnimator na raiz.
        static void SavePrefab(string character, AnimatorController controller, string speaker, AnimationClip groundClip = null)
        {
            var root = new GameObject(character);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtRoot}{character}/{character}.fbx"));
                model.name = "Modelo";
                model.transform.SetParent(root.transform, false);
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                // Sentada ou deitada: o Mixamo guarda o corpo na altura do quadril em pé. O modelo desce até o
                // ponto mais baixo da pose encostar no chão (medido no clipe, na escala do personagem).
                if (groundClip != null)
                {
                    float lowest = CharacterHeights.Extent($"{ArtRoot}{character}/{character}.fbx", groundClip).min;
                    model.transform.localPosition = new Vector3(0f, -lowest, 0f);
                }
                if (speaker != null)
                {
                    var npc = root.AddComponent<NpcAnimator>();
                    var serialized = new SerializedObject(npc);
                    serialized.FindProperty("animator").objectReferenceValue = animator;
                    serialized.FindProperty("speakerName").stringValue = speaker;
                    serialized.FindProperty("talkVariations").intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(character));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
