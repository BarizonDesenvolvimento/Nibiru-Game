using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Teste automático das animações importadas da biblioteca: todos os clipes da tabela existem, são
    /// Humanoid com o avatar do X Bot, têm Loop Time certo e o tamanho do corte; os controladores das
    /// criaturas, da Elena e do Samir têm os estados e parâmetros previstos. Imprime um resumo no Console.
    /// Menu: Nibiru > Testes > Animações > 4. Teste automático.
    /// </summary>
    static class AnimationTests
    {
        const string Characters = "Assets/_Nibiru/Art/Personagens/";
        static int checks;
        static readonly List<string> failures = new();

        [MenuItem("Nibiru/Testes/Animações/4. Teste automático (resumo no Console)")]
        static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes do teste automático de animações.");
                return;
            }
            checks = 0;
            failures.Clear();
            CheckClips();
            CheckCreature("Lullu", new[] { "Locomocao", "Golpe", "Morte" }, new string[0]);
            CheckCreature("PrimeiroServo", new[] { "Locomocao", "Golpe", "Investida", "Urro", "Morte" }, new[] { "Investida", "Urro" });
            CheckSamir();
            CheckElena();
            Debug.Log(failures.Count == 0
                ? $"Animações: {checks} verificações, todas passaram."
                : $"Animações: {checks} verificações, {failures.Count} falharam:\n- " + string.Join("\n- ", failures));
        }

        static void Check(bool ok, string message)
        {
            checks++;
            if (!ok) failures.Add(message);
        }

        // ---------- Clipes ----------

        static void CheckClips()
        {
            var xbot = AssetDatabase.LoadAllAssetsAtPath("Assets/_Nibiru/Art/Animacoes/Mixamo/X Bot.fbx").OfType<Avatar>().FirstOrDefault();
            foreach (var e in AnimationLibrary.Entries)
            {
                var importer = AssetImporter.GetAtPath(e.AssetPath) as ModelImporter;
                Check(importer != null, $"{e.name}: arquivo não encontrado em {e.AssetPath}");
                if (importer == null) continue;
                Check(importer.animationType == ModelImporterAnimationType.Human, $"{e.name}: não é Humanoid");
                Check(importer.sourceAvatar == xbot && xbot != null, $"{e.name}: o avatar não é o do X Bot");
                var clips = AssetDatabase.LoadAllAssetsAtPath(e.AssetPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
                Check(clips.Length == 1 && clips[0].name == e.name, $"{e.name}: o arquivo deveria ter um clipe com este nome (tem {string.Join(",", clips.Select(c => c.name))})");
                if (clips.Length != 1) continue;
                var clip = clips[0];
                Check(clip.isLooping == e.loop, $"{e.name}: Loop Time é {clip.isLooping}, esperado {e.loop}");
                Check(clip.humanMotion, $"{e.name}: o clipe não é de movimento humano");
                Check(clip.length > 0.1f, $"{e.name}: clipe vazio");
                if (e.first >= 0 && e.last >= 0)
                {
                    float expected = (e.last - e.first + 1) / 30f;
                    Check(Mathf.Abs(clip.length - expected) < 0.1f, $"{e.name}: duração {clip.length:0.00} s, o corte pede {expected:0.00} s");
                }
                var clipSettings = importer.clipAnimations.FirstOrDefault();
                Check(clipSettings != null && clipSettings.lockRootRotation && clipSettings.lockRootHeightY, $"{e.name}: giro e altura da raiz não estão travados");
            }
        }

        // ---------- Controladores ----------

        static AnimatorController Controller(string character) =>
            AssetDatabase.LoadAssetAtPath<AnimatorController>($"{Characters}{character}/{character}.controller");

        static void CheckParams(AnimatorController c, string who, params string[] names)
        {
            foreach (string n in names)
                Check(c.parameters.Any(p => p.name == n), $"{who}: falta o parâmetro {n}");
        }

        static AnimatorState State(AnimatorController c, string layer, string name) =>
            c.layers.FirstOrDefault(l => l.name == layer)?.stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);

        static void CheckCreature(string character, string[] baseStates, string[] extraParams)
        {
            var c = Controller(character);
            Check(c != null, $"{character}: controlador não encontrado");
            if (c == null) return;
            CheckParams(c, character, "Velocidade", "Golpe", "Dano", "Morte");
            CheckParams(c, character, extraParams);
            string prefix = character == "Lullu" ? "Lullu" : "Servo";
            foreach (string name in baseStates)
            {
                var state = State(c, "Base Layer", name);
                Check(state != null && state.motion != null, $"{character}: falta o estado {name}");
            }
            var hurt = State(c, "Reacoes", "Dano");
            Check(hurt != null && hurt.motion != null && hurt.motion.name == "Criatura_Golpe_Recebido", $"{character}: o dano deveria tocar Criatura_Golpe_Recebido");
            Check(State(c, "Base Layer", "Morte")?.motion?.name == "Criatura_Morte", $"{character}: a morte deveria tocar Criatura_Morte");
            Check(State(c, "Base Layer", "Golpe")?.motion?.name == prefix + "_Ataque", $"{character}: o golpe deveria tocar {prefix}_Ataque");
            if (character == "PrimeiroServo")
            {
                Check(State(c, "Base Layer", "Investida")?.motion?.name == "Servo_Investida", "PrimeiroServo: a investida deveria tocar Servo_Investida");
                Check(State(c, "Base Layer", "Urro")?.motion?.name == "Servo_Urro", "PrimeiroServo: o urro deveria tocar Servo_Urro");
            }
            // A locomoção: parado, andar e correr da criatura, ligados à velocidade.
            var locomotion = State(c, "Base Layer", "Locomocao")?.motion as BlendTree;
            Check(locomotion != null && locomotion.blendParameter == "Velocidade", $"{character}: a locomoção não é uma blend tree pela Velocidade");
            if (locomotion != null)
            {
                var names = locomotion.children.Select(ch => ch.motion.name).ToArray();
                foreach (string clip in new[] { "_Parado", "_Andar", "_Correr" })
                    Check(names.Contains(prefix + clip), $"{character}: a blend tree não usa {prefix}{clip}");
            }
            // O prefab usa este controlador e tem o LulluAnimator.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Nibiru/Prefabs/AI/{(character == "Lullu" ? "Lullu" : "PrimeiroServo")}.prefab");
            Check(prefab != null && prefab.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController == c, $"{character}: o prefab não usa o controlador");
            Check(prefab != null && prefab.GetComponent<AI.LulluAnimator>() != null, $"{character}: o prefab não tem o LulluAnimator");
        }

        static void CheckSamir()
        {
            var c = Controller("Samir");
            Check(c != null, "Samir: controlador não encontrado");
            if (c == null) return;
            CheckParams(c, "Samir", "Velocidade", "Agachar", "Golpe", "Dirigindo");
            Check(State(c, "Base Layer", "Dirigindo")?.motion?.name == "Samir_Dirigindo", "Samir: o estado Dirigindo deveria tocar Samir_Dirigindo");
            foreach (string name in new[] { "Locomocao", "Golpe_Faca", "Conversa_1", "Conversa_4" })
                Check(State(c, "Base Layer", name) != null, $"Samir: falta o estado {name}");
        }

        // O controlador da Elena continua o aprovado: só confere que nada se perdeu.
        static void CheckElena()
        {
            var c = Controller("Elena");
            Check(c != null, "Elena: controlador não encontrado");
            if (c == null) return;
            CheckParams(c, "Elena", "Velocidade", "Agachar", "Esquivar", "Golpe", "Dano", "Morrer");
            foreach (string name in new[] { "Locomocao", "Esquiva", "Golpe_Faca", "Morte" })
                Check(State(c, "Base Layer", name) != null, $"Elena: falta o estado {name}");
        }
    }
}
