using System.Linq;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para ver o Samir animado nos momentos em que ele aparece (encontro,
    /// caminho até o pátio, casa do altar, conversa da faca, saída pelos canais), falar com ele no
    /// laboratório e uma vitrine com todas as animações lado a lado. Menu: Nibiru > Testes > Samir.
    /// </summary>
    [InitializeOnLoad]
    static class SamirShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Samir/";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        const string SamirPath = "Conteudo_Aldeia/Encontro_Samir/Cena/Samir";
        const string LookoutPath = "Conteudo_Aldeia/Plano_De_Fuga/Samir_Beira";
        const string ExitPath = "Conteudo_Aldeia/Plano_De_Fuga/Samir_Saida";

        // Tudo até a Elena atravessar as trincheiras (antes da Aldeia).
        static readonly string[] EventsUntilTrenches = Step8Shortcuts.EventsUntilStep7
            .TakeWhile(id => id != "AproximouSeDaAldeia").ToArray();

        // Passos da missão Sobrevivente: "Atravesse as trincheiras e siga para a Aldeia",
        // "Siga o Samir até a Aldeia" e "Siga o Samir até a casa".
        const int ReachVillageStep = 8;
        const int FollowToPatioStep = 9;
        const int FollowIntoHouseStep = 10;

        // Na estrada das trincheiras, a poucos passos da Aldeia, olhando para o oeste.
        static readonly Vector3 BeforeVillageSpot = new Vector3(-22f, 0f, -16f);
        // Ao lado da entrada leste, onde a cena do encontro fica montada.
        static readonly Vector3 EastEntranceSpot = new Vector3(-33f, 0f, -17.5f);
        // No pátio, onde o checkpoint do pátio devolve a Elena.
        static readonly Vector3 PatioSpot = new Vector3(-44.5f, 0f, -19.5f);
        // Ao sul da beira da aldeia, fora da área que começa o cerco, olhando para o Samir.
        static readonly Vector3 SouthOfLookoutSpot = new Vector3(-46f, 0f, -4f);

        // Vitrine: um chão vazio longe de tudo, com uma cópia do Samir por animação.
        static readonly Vector3 ShowcaseSpot = new Vector3(300f, 0f, 300f);
        static GameObject strikeShowcase;
        static double nextStrike;

        static SamirShortcuts()
        {
            Register("samir.encontro", () =>
            {
                StartFrom(EventsUntilTrenches, new[] { ("Sobrevivente", ReachVillageStep), ("OQueOTomViu", 0) }, 0, BeforeVillageSpot, 270f);
                Debug.Log("Samir: perto da Aldeia. Ande para o oeste: o soldado encurrala a Elena e o Samir o derruba com a faca.");
            });
            Register("samir.seguir", () =>
            {
                StartFrom(EventsUntilTrenches.Concat(new[] { "AproximouSeDaAldeia", "ChegouAAldeia" }),
                    new[] { ("Sobrevivente", FollowToPatioStep), ("OQueOTomViu", 0) }, 0, EastEntranceSpot, 270f);
                Debug.Log("Samir: depois da conversa do guia. Siga o Samir até o pátio (corra à frente dele para vê-lo correr).");
            });
            Register("samir.casa", () =>
            {
                StartFrom(EventsUntilTrenches.Concat(new[] { "AproximouSeDaAldeia", "ChegouAAldeia", "ChegouAoPatioComSamir" }),
                    new[] { ("Sobrevivente", FollowIntoHouseStep), ("OQueOTomViu", 0) }, 0, PatioSpot, 215f);
                Debug.Log("Samir: no pátio. Siga o Samir para dentro da casa do altar: na conversa, ele gesticula nas falas dele.");
            });
            Register("samir.agachado", () =>
            {
                Step9Shortcuts.Start(revealed: true, extra: new[] { "ChegouABeiraDaAldeia" }, Step9Shortcuts.CrossRoadStep,
                    SouthOfLookoutSpot, 0f, () => After(1f, () =>
                    {
                        var walker = Find(SamirPath).GetComponent<WalkToPoint>();
                        walker.SnapTo(Find(LookoutPath).transform);
                        After(1.5f, () =>
                        {
                            walker.LeaveTo(Find(ExitPath).transform);
                            walker.GetComponentInChildren<NpcAnimator>().Sneak();
                        });
                    }));
                Debug.Log("Samir: na beira da aldeia, ele sai agachado pelos canais do sul e passa ao lado da Elena. Não ande para o norte (lá começa o cerco).");
            });
            Register("samir.laboratorio", () => After(0.3f, () =>
            {
                // Os inimigos do laboratório viriam atrás da Elena: ficam parados nesta partida.
                ElenaShortcuts.StopEnemies();
                var samir = Object.FindObjectsByType<DialogueStarter>(FindObjectsSortMode.None).FirstOrDefault(d => d.name == "Samir");
                if (samir == null)
                {
                    Debug.LogWarning("Samir: não achei o Samir no laboratório.");
                    return;
                }
                Vector3 front = samir.transform.position + samir.transform.forward * 1.8f;
                front.y = samir.transform.position.y - 1f;
                Teleport(front, samir.transform.eulerAngles.y + 180f);
                Debug.Log("Samir: no laboratório, de frente para ele. Aperte E (ou A) em \"Falar com Samir\".");
            }));
            Register("samir.vitrine", () => After(0.3f, BuildShowcase));
        }

        [MenuItem(MenuRoot + "1. Encontro na Aldeia: a facada no soldado")]
        static void Encounter() => Go("samir.encontro");

        [MenuItem(MenuRoot + "2. Seguindo o Samir até o pátio (andar e correr)")]
        static void Follow() => Go("samir.seguir");

        [MenuItem(MenuRoot + "3. Casa do altar: os sobreviventes (gestos de conversa)")]
        static void House() => Go("samir.casa");

        [MenuItem(MenuRoot + "4. Volta da memória: o Samir de mão na faca")]
        static void Knife() => Go("passo8.samir");

        [MenuItem(MenuRoot + "5. Depois do cerco: o Samir saindo agachado")]
        static void Sneak() => Go("samir.agachado");

        [MenuItem(MenuRoot + "6. Laboratório: falar com o Samir")]
        static void Lab() => Go("samir.laboratorio", LabScenePath);

        [MenuItem(MenuRoot + "7. Vitrine: todas as animações lado a lado")]
        static void Showcase() => Go("samir.vitrine", LabScenePath);

        // Uma cópia do Samir por animação, em fila, de frente para a Elena, com o nome em cima.
        // Andar e correr ficam no lugar (quem anda no jogo é o NavMeshAgent). O golpe se repete.
        static void BuildShowcase()
        {
            ElenaShortcuts.StopEnemies();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamirSetup.PrefabPath);
            var group = new GameObject("Vitrine_Samir").transform;
            group.position = ShowcaseSpot;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Chao";
            floor.transform.SetParent(group, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(30f, 0.2f, 30f);
            float crouchSpeed = new SerializedObject(prefab.GetComponent<NpcAnimator>()).FindProperty("crouchSpeed").floatValue;

            var poses = new (string label, string state, float speed, float crouch)[]
            {
                ("Parado", NpcAnimator.LocomotionState, 0f, 0f),
                ("Andar", NpcAnimator.LocomotionState, SamirSetup.WalkSpeed, 0f),
                ("Correr", NpcAnimator.LocomotionState, SamirSetup.RunSpeed + 0.5f, 0f),
                ("Agachado", NpcAnimator.LocomotionState, 0f, 1f),
                ("Agachado andando", NpcAnimator.LocomotionState, crouchSpeed, 1f),
                ("Golpe de faca", NpcAnimator.LocomotionState, 0f, 0f),
                ("Conversa 1", NpcAnimator.TalkStatePrefix + 1, 0f, 0f),
                ("Conversa 2", NpcAnimator.TalkStatePrefix + 2, 0f, 0f),
                ("Conversa 3", NpcAnimator.TalkStatePrefix + 3, 0f, 0f),
                ("Conversa 4", NpcAnimator.TalkStatePrefix + 4, 0f, 0f),
            };
            const float spacing = 1.5f;
            for (int i = 0; i < poses.Length; i++)
            {
                var (label, state, speed, crouch) = poses[i];
                var copy = Object.Instantiate(prefab, group);
                copy.name = label;
                copy.transform.localPosition = new Vector3((i - (poses.Length - 1) * 0.5f) * spacing, 0f, 0f);
                copy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var animator = copy.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // O golpe fica com o NpcAnimator (ele leva a faca para a mão); nas outras, a pose é fixa.
                if (label == "Golpe de faca") strikeShowcase = copy;
                else Object.Destroy(copy.GetComponent<NpcAnimator>());
                animator.SetFloat(NpcAnimator.SpeedParam, speed);
                animator.SetFloat(NpcAnimator.CrouchParam, crouch);
                animator.Play(state, 0, 0f);

                var text = new GameObject("Nome").AddComponent<TextMesh>();
                text.transform.SetParent(copy.transform, false);
                // Alturas alternadas, para os nomes vizinhos não se encostarem.
                text.transform.localPosition = new Vector3(0f, i % 2 == 0 ? 2f : 2.2f, 0f);
                text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                text.text = label;
                text.characterSize = 0.035f;
                text.fontSize = 48;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            Teleport(ShowcaseSpot + new Vector3(0f, 0f, -6.5f), 0f);
            nextStrike = 0;
            EditorApplication.update -= RepeatStrike;
            EditorApplication.update += RepeatStrike;
            Debug.Log("Samir: vitrine com todas as animações. Ande ao longo da fila; a faca do golpe segue a mão direita.");
        }

        // Repete o golpe a cada 1,6 s.
        static void RepeatStrike()
        {
            var npc = EditorApplication.isPlaying && strikeShowcase != null ? strikeShowcase.GetComponent<NpcAnimator>() : null;
            if (npc == null)
            {
                EditorApplication.update -= RepeatStrike;
                return;
            }
            if (EditorApplication.timeSinceStartup < nextStrike) return;
            npc.Strike();
            nextStrike = EditorApplication.timeSinceStartup + 1.6;
        }
    }
}
