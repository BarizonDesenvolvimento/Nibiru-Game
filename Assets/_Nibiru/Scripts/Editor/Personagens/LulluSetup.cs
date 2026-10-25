using System.Linq;
using Nibiru.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static Nibiru.EditorTools.CharacterSetupKit;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Monta o Lullu definitivo: copia o FBX do AccuRig e as texturas do Meshy da ArteFonte (se ainda
    /// não estiverem na Unity), importa como Humanoid com dedos na altura da tabela (2,10 m em pé),
    /// cria o material URP, o controlador Lullu.controller e troca a cápsula provisória do prefab
    /// Prefabs/AI/Lullu pelo modelo, sem mexer na LulluAI. O prefab é editado no lugar (mesmo arquivo),
    /// então as cenas que o usam já pegam o modelo.
    /// O modelo foi feito em pé, em pose A; a postura de criatura vem do LulluAnimator, que dobra o tronco
    /// por cima das animações do Mixamo (os cortes da Elena, retargetados pelo Humanoid). O andar
    /// agachado (Sneak Walk) não serviu: nas pernas longas dele o joelho não dobra e o corpo vira uma rampa.
    /// Pode rodar de novo: refaz controlador e modelo do prefab; o material só é atualizado.
    /// Menu: Nibiru > Personagens > Montar Lullu animado.
    /// </summary>
    static class LulluSetup
    {
        const string Character = "Lullu";
        const string Root = "Assets/_Nibiru/Art/Personagens/Lullu";
        const string ModelPath = Root + "/Lullu.fbx";
        const string ControllerPath = Root + "/Lullu.controller";
        const string PrefabPath = "Assets/_Nibiru/Prefabs/AI/Lullu.prefab";
        const string SettingsPath = "Assets/_Nibiru/Data/AI/Lullu.asset";

        // Raiz do Lullu com IA: 1 m acima do chão (baseOffset do NavMeshAgent).
        const float FeetBelowRoot = -1f;

        // Colisor da criatura encurvada (altura do chão ao topo das costas) e a cápsula de antes: 0,35 x 2 m.
        const float ColliderHeight = 1.8f;
        const float ColliderRadius = 0.4f;

        [MenuItem("Nibiru/Personagens/Montar Lullu animado")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de montar o Lullu.");
                return;
            }
            AssetDatabase.Refresh();
            SoldiersSetup.ImportCharacter(Character);
            var controller = BuildController();
            if (controller == null) return;
            AssetDatabase.SaveAssets();
            BuildPrefab(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Lullu animado pronto no prefab " + PrefabPath + ".");
        }

        // ---------- Controlador ----------

        static AnimatorController BuildController()
        {
            // Cortes da Elena (ElenaSetup), retargetados para o Lullu pelo Humanoid.
            var idle = Clip("Idle", "Parada");
            var walk = Clip("WalkForward", "Andar");
            var run = Clip("Fast Run", "Correr");
            var strike = Clip("Stabbing", "Golpe_Faca");
            var hurt = Clip("Hit Reaction", "Dano");
            var death = Clip("Standing React Death Backward", "Morte");
            if (new[] { idle, walk, run, strike, hurt, death }.Any(c => c == null))
            {
                Debug.LogError("Lullu: faltam cortes do Mixamo da Elena. Rode antes Nibiru > Personagens > Montar Elena animada.");
                return null;
            }

            var settings = AssetDatabase.LoadAssetAtPath<LulluSettings>(SettingsPath);
            float wander = settings != null ? settings.wanderSpeed : 1.2f;
            float investigate = settings != null ? settings.investigateSpeed : 2.6f;
            float hunt = settings != null ? settings.huntSpeed : 5.5f;

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(LulluAnimator.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(LulluAnimator.StrikeParam, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(LulluAnimator.HurtParam, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(LulluAnimator.DeathParam, AnimatorControllerParameterType.Trigger);

            // Locomoção pela velocidade: parado respirando, o andar (devagar vagando, mais rápido
            // investigando) e a corrida da caça. O LulluAnimator encurva o tronco por cima de todos.
            var machine = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
            Speeds(tree, LulluAnimator.SpeedParam,
                (idle, 0f, 0f),
                (walk, NaturalSpeed(walk), wander),
                (walk, NaturalSpeed(walk), investigate),
                (run, NaturalSpeed(run), Mathf.Max(hunt, NaturalSpeed(run))));

            // Golpe: entra pelo gatilho e volta sozinho para a locomoção.
            var strikeState = machine.AddState("Golpe", new Vector3(300f, 60f));
            strikeState.motion = strike;
            var enter = machine.AddAnyStateTransition(strikeState);
            enter.AddCondition(AnimatorConditionMode.If, 0f, LulluAnimator.StrikeParam);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = false;
            var leave = strikeState.AddTransition(locomotion);
            leave.hasExitTime = true;
            leave.exitTime = 1f;
            leave.hasFixedDuration = true;
            leave.duration = 0.2f;

            // Morte: cai para trás e fica deitado.
            var deathState = machine.AddState("Morte", new Vector3(300f, 120f));
            deathState.motion = death;
            var die = machine.AddAnyStateTransition(deathState);
            die.AddCondition(AnimatorConditionMode.If, 0f, LulluAnimator.DeathParam);
            die.hasExitTime = false;
            die.duration = 0.1f;
            die.canTransitionToSelf = false;

            // Dano: o tranco por cima de tudo (camada aditiva).
            controller.AddLayer("Reacoes");
            var layers = controller.layers;
            var reactions = layers[1];
            reactions.blendingMode = AnimatorLayerBlendingMode.Additive;
            reactions.defaultWeight = 0.6f;
            controller.layers = layers;
            var reactionMachine = controller.layers[1].stateMachine;
            var empty = reactionMachine.AddState("Vazio", new Vector3(300f, 0f));
            var hurtState = reactionMachine.AddState("Dano", new Vector3(300f, 60f));
            hurtState.motion = hurt;
            reactionMachine.defaultState = empty;
            var hit = reactionMachine.AddAnyStateTransition(hurtState);
            hit.AddCondition(AnimatorConditionMode.If, 0f, LulluAnimator.HurtParam);
            hit.hasExitTime = false;
            hit.duration = 0.05f;
            hit.canTransitionToSelf = true;
            var back = hurtState.AddTransition(empty);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.hasFixedDuration = true;
            back.duration = 0.15f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        // ---------- Prefab ----------

        // Lullu (colisor, agente, vida, LulluAI, LulluAnimator) > Corpo (o "body" da IA, com o Eco) > Modelo.
        static void BuildPrefab(AnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var body = root.transform.Find("Corpo");
                foreach (Transform child in body.Cast<Transform>().ToArray())
                    Object.DestroyImmediate(child.gameObject);
                Object.DestroyImmediate(body.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(body.GetComponent<MeshFilter>());
                // Os pés do modelo no chão: a raiz fica 1 m acima dele.
                body.localPosition = new Vector3(0f, FeetBelowRoot, 0f);
                body.localRotation = Quaternion.identity;
                body.localScale = Vector3.one;

                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
                model.name = "Modelo";
                model.transform.SetParent(body, false);
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var capsule = root.GetComponent<CapsuleCollider>();
                capsule.radius = ColliderRadius;
                capsule.height = ColliderHeight;
                capsule.center = new Vector3(0f, FeetBelowRoot + ColliderHeight * 0.5f, 0f);

                // Componente novo a cada execução: assim valem os valores de postura do script.
                var oldAnimator = root.GetComponent<LulluAnimator>();
                if (oldAnimator != null) Object.DestroyImmediate(oldAnimator);
                var anim = root.AddComponent<LulluAnimator>();
                var serialized = new SerializedObject(anim);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("brain").objectReferenceValue = root.GetComponent<LulluAI>();
                serialized.FindProperty("health").objectReferenceValue = root.GetComponent<Combat.Health>();
                serialized.FindProperty("visual").objectReferenceValue = body;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
