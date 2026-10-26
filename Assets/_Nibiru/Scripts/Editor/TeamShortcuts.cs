using Nibiru.AI;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: vitrine dos personagens da Aldeia e da equipe (Umm Karim, Dra. Weiss, Yusuf e a
    /// equipe morta), numa fila no laboratório, com o Samir já aprovado no começo para comparar a escala
    /// e uma régua de 2 m com marcas a cada 25 cm. Menu: Nibiru > Testes > Personagens.
    /// </summary>
    [InitializeOnLoad]
    static class TeamShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Personagens/";
        const string LabScenePath = "Assets/_Nibiru/Scenes/Teste_Personagem.unity";
        static readonly Vector3 ShowcaseSpot = new Vector3(300f, 0f, 300f);

        static TeamShortcuts()
        {
            Register("personagens.vitrine", () => After(0.3f, BuildShowcase));
        }

        [MenuItem(MenuRoot + "1. Vitrine: Aldeia e equipe (ao lado do Samir)")]
        static void Showcase() => Go("personagens.vitrine", LabScenePath);

        static void BuildShowcase()
        {
            ElenaShortcuts.StopEnemies();
            var group = new GameObject("Vitrine_Personagens").transform;
            group.position = ShowcaseSpot;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Chao";
            floor.transform.SetParent(group, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(40f, 0.2f, 30f);

            // Régua: poste de 2 m com uma marca a cada 25 cm, à esquerda do Samir.
            var ruler = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(ruler.GetComponent<Collider>());
            ruler.name = "Regua";
            ruler.transform.SetParent(group, false);
            ruler.transform.localPosition = new Vector3(-13.5f, 1f, 0f);
            ruler.transform.localScale = new Vector3(0.04f, 2f, 0.04f);
            for (int i = 1; i <= 8; i++)
            {
                var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(tick.GetComponent<Collider>());
                tick.name = $"Marca_{i * 25}cm";
                tick.transform.SetParent(group, false);
                tick.transform.localPosition = new Vector3(-13.5f, i * 0.25f, 0f);
                tick.transform.localScale = new Vector3(i % 4 == 0 ? 0.5f : 0.25f, 0.015f, 0.05f);
            }

            var items = new (string label, string prefab, string state, float speed, bool stripNpc)[]
            {
                ("Samir (referência)", SamirSetup.PrefabPath, NpcAnimator.LocomotionState, 0f, true),
                ("Umm Karim parada", VillageTeamSetup.PrefabPath("UmmKarim"), NpcAnimator.LocomotionState, 0f, true),
                ("Umm Karim andando", VillageTeamSetup.PrefabPath("UmmKarim"), NpcAnimator.LocomotionState, 1.2f, true),
                ("Umm Karim conversando", VillageTeamSetup.PrefabPath("UmmKarim"), NpcAnimator.TalkStatePrefix + 1, 0f, true),
                ("Dra. Weiss sentada", VillageTeamSetup.PrefabPath("DraWeiss"), "Sentada", 0f, true),
                ("Yusuf deitado", VillageTeamSetup.PrefabPath("Yusuf"), "Deitado", 0f, true),
                ("Arqueólogo veterano", VillageTeamSetup.PrefabPath("ArqueologoVeterano"), null, 0f, true),
                ("Arqueóloga jovem", VillageTeamSetup.PrefabPath("ArqueologaJovem"), null, 0f, true),
                ("Trabalhador (Hassan)", VillageTeamSetup.PrefabPath("TrabalhadorEscavacao"), null, 0f, true),
            };
            const float spacing = 2.4f;
            for (int i = 0; i < items.Length; i++)
            {
                var (label, path, state, speed, stripNpc) = items[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Vitrine: falta o prefab {path}. Rode Nibiru > Personagens > Montar personagens da Aldeia e da equipe.");
                    continue;
                }
                var copy = Object.Instantiate(prefab, group);
                copy.name = label;
                copy.transform.localPosition = new Vector3(-10f + i * spacing, 0f, 0f);
                copy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var animator = copy.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // Pose fixa pelo controlador, sem o NpcAnimator mexendo na velocidade.
                var npc = copy.GetComponent<NpcAnimator>();
                if (stripNpc && npc != null) Object.Destroy(npc);
                if (state != null && animator.runtimeAnimatorController != null)
                {
                    animator.SetFloat(NpcAnimator.SpeedParam, speed);
                    animator.Play(state, 0, 0f);
                }

                var text = new GameObject("Nome").AddComponent<TextMesh>();
                text.transform.SetParent(copy.transform, false);
                text.transform.localPosition = new Vector3(0f, i % 2 == 0 ? 2.2f : 2.5f, 0f);
                text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                text.text = label;
                text.characterSize = 0.035f;
                text.fontSize = 48;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            Teleport(ShowcaseSpot + new Vector3(0f, 0f, -7f), 0f);
            Debug.Log("Personagens: vitrine da Aldeia e da equipe. Ande ao longo da fila; o Samir (aprovado) está à esquerda, ao lado da régua.");
        }
    }
}
