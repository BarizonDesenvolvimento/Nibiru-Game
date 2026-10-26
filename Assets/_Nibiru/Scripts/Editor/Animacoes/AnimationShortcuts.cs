using System.Collections.Generic;
using Nibiru.AI;
using Nibiru.Testes;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: vitrines para conferir no Play as animações importadas da biblioteca. Cada item
    /// abre o laboratório num chão vazio, com uma cópia do personagem por animação e o nome em cima.
    /// As criaturas usam os controladores de verdade (parado, andar, correr, golpe, dano, morte...);
    /// a Elena mostra o controlador atual (aprovado) ao lado dos clipes novos, para comparar.
    /// Menu: Nibiru > Testes > Animações.
    /// </summary>
    [InitializeOnLoad]
    static class AnimationShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Animações/";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        const string CharactersRoot = "Assets/_Nibiru/Art/Personagens/";
        static readonly Vector3 ShowcaseSpot = new Vector3(300f, 0f, 300f);

        static AnimationShortcuts()
        {
            Register("animacoes.criaturas", () => After(0.3f, BuildCreatures));
            Register("animacoes.elena", () => After(0.3f, BuildElena));
        }

        [MenuItem(MenuRoot + "1. Criaturas: Lullu e Primeiro Servo (controladores de verdade)")]
        static void Creatures() => Go("animacoes.criaturas", LabScenePath);

        [MenuItem(MenuRoot + "2. Elena: controlador atual ao lado dos clipes novos")]
        static void Elena() => Go("animacoes.elena", LabScenePath);

        [MenuItem(MenuRoot + "3. Caminhonete: meio da viagem (Samir dirigindo)")]
        static void Truck() => Go("passo15.meio_entregou");

        // ---------- Montagem ----------

        static Transform NewStage(string name)
        {
            ElenaShortcuts.StopEnemies();
            var group = new GameObject(name).transform;
            group.position = ShowcaseSpot;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Chao";
            floor.transform.SetParent(group, false);
            floor.transform.localPosition = new Vector3(0f, -0.2f, -15f);
            floor.transform.localScale = new Vector3(60f, 0.4f, 60f);
            return group;
        }

        static GameObject Model(string path, Transform group, string label, float x, float height = 3.4f)
        {
            var copy = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), group);
            copy.name = label;
            copy.transform.localPosition = new Vector3(x, 0f, 0f);
            copy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var animator = copy.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            var text = new GameObject("Nome").AddComponent<TextMesh>();
            text.transform.SetParent(copy.transform, false);
            text.transform.localPosition = new Vector3(0f, height, 0f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            text.text = label;
            text.characterSize = 0.03f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return copy;
        }

        static void AddSteps(GameObject target, string controllerPath, params ControllerShowcase.Step[] steps)
        {
            var animator = target.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            var showcase = target.AddComponent<ControllerShowcase>();
            var serialized = new SerializedObject(showcase);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            var list = serialized.FindProperty("steps");
            list.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("label").stringValue = steps[i].label;
                item.FindPropertyRelative("parameter").stringValue = steps[i].parameter;
                item.FindPropertyRelative("trigger").boolValue = steps[i].trigger;
                item.FindPropertyRelative("value").floatValue = steps[i].value;
                item.FindPropertyRelative("seconds").floatValue = steps[i].seconds;
                item.FindPropertyRelative("resetBefore").boolValue = steps[i].resetBefore;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            target.AddComponent<ShowcaseLabel>();
        }

        static ControllerShowcase.Step Speed(string label, float speed, float seconds = 3f, bool reset = false) =>
            new() { label = label, parameter = "Velocidade", value = speed, seconds = seconds, resetBefore = reset };

        static ControllerShowcase.Step Trigger(string label, string parameter, float seconds = 2.5f, bool reset = false) =>
            new() { label = label, parameter = parameter, trigger = true, seconds = seconds, resetBefore = reset };

        static void AddClip(GameObject target, AnimationClip clip)
        {
            var player = target.AddComponent<ClipLoopPlayer>();
            var serialized = new SerializedObject(player);
            serialized.FindProperty("animator").objectReferenceValue = target.GetComponent<Animator>();
            serialized.FindProperty("clip").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildCreatures()
        {
            var group = NewStage("Vitrine_Criaturas");
            // Velocidades da IA de cada um (dados em Data/AI).
            var lullu = AssetDatabase.LoadAssetAtPath<LulluSettings>("Assets/_Nibiru/Data/AI/Lullu.asset");
            var servo = AssetDatabase.LoadAssetAtPath<LulluSettings>("Assets/_Nibiru/Data/AI/PrimeiroServo.asset");

            var a = Model(CharactersRoot + "Lullu/Lullu.fbx", group, "Lullu (controlador)", -4f);
            AddSteps(a, CharactersRoot + "Lullu/Lullu.controller",
                Speed("Parado", 0f), Speed("Vagando", lullu.wanderSpeed), Speed("Investigando", lullu.investigateSpeed),
                Speed("Caçando", lullu.huntSpeed), Speed("Parado", 0f, 1f),
                Trigger("Golpe", "Golpe"), Trigger("Dano", "Dano", 2f), Trigger("Morte", "Morte", 4f),
                Speed("Recomeça", 0f, 0.5f, reset: true));

            var b = Model(CharactersRoot + "PrimeiroServo/PrimeiroServo.fbx", group, "Primeiro Servo (controlador)", 4f);
            AddSteps(b, CharactersRoot + "PrimeiroServo/PrimeiroServo.controller",
                Speed("Parado", 0f), Speed("Vagando", servo.wanderSpeed), Speed("Investigando", servo.investigateSpeed),
                Speed("Caçando", servo.huntSpeed), Speed("Parado", 0f, 1f),
                Trigger("Golpe", "Golpe"), Trigger("Investida", "Investida", 3f), Trigger("Urro", "Urro", 2.5f),
                Trigger("Dano", "Dano", 2f), Trigger("Morte", "Morte", 4f),
                Speed("Recomeça", 0f, 0.5f, reset: true));

            // Saída das rachaduras: só importada (nenhum evento do jogo a usa).
            var c = Model(CharactersRoot + "Lullu/Lullu.fbx", group, "Saída das rachaduras (clipe)", -10f);
            AddClip(c, AnimationLibrary.Clip("Lullu_Saida_Rachaduras"));

            Teleport(ShowcaseSpot + new Vector3(0f, 0f, -13f), 0f);
            Debug.Log("Animações: vitrine das criaturas. Lullu e Primeiro Servo repetem uma sequência (parado, andar, correr, golpe, dano, morte); o Servo também faz a investida e o urro.");
        }

        static void BuildElena()
        {
            var group = NewStage("Vitrine_Elena");
            string model = CharactersRoot + "Elena/Elena.fbx";
            var current = Model(model, group, "ATUAL (controlador)", -9f, 2.3f);
            AddSteps(current, CharactersRoot + "Elena/Elena.controller",
                Speed("Atual: parada", 0f, 4f), Speed("Atual: andar", 1.7f, 4f), Speed("Atual: correr", 5.4f, 4f));

            var entries = new (string clip, string label)[]
            {
                ("Elena_Parada_Basic", "NOVA: parada"), ("Elena_Andar_Basic", "NOVA: andar"), ("Elena_Correr_Basic", "NOVA: correr"),
                ("Elena_Virar_Esquerda", "Virar esquerda"), ("Elena_Virar_Direita", "Virar direita"),
                ("Elena_Pegar", "Pegar objeto"), ("Elena_Abrir", "Abrir porta"),
            };
            for (int i = 0; i < entries.Length; i++)
            {
                var copy = Model(model, group, entries[i].label, -6f + i * 3f, 2.3f);
                copy.GetComponent<Animator>().runtimeAnimatorController = null;
                AddClip(copy, AnimationLibrary.Clip(entries[i].clip));
            }
            Teleport(ShowcaseSpot + new Vector3(0f, 0f, -8f), 0f);
            Debug.Log("Animações: vitrine da Elena. À esquerda, o controlador atual (aprovado); depois, os clipes novos. Andar e correr ficam no lugar.");
        }
    }
}
