using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nibiru.Dialogue;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Teste dos 7 corpos da equipe no círculo do Acampamento (TeamBodiesSetup). Não liga o Play e não mexe em
    /// salvamento: só lê a cena aberta (Regiao1_SitioDeUr). Resumo no Console.
    /// Menu: Nibiru > Testes > Personagens > Teste dos corpos da equipe (resumo no Console).
    /// </summary>
    static class TeamBodiesTests
    {
        const float BelowGround = 0.01f;   // nada mais de 1 cm dentro do chão
        const float LowestAbove = 0.02f;   // o ponto mais baixo encosta no chão
        const float ContactGap = 0.05f;    // quadril e peito a até 5 cm do chão
        const float ContactRadius = 0.25f;

        [MenuItem("Nibiru/Testes/Personagens/Teste dos corpos da equipe (resumo no Console)")]
        static void Run()
        {
            var report = new StringBuilder();
            int failed = 0, passed = 0;
            void Result(bool ok, string name, string detail)
            {
                if (ok) passed++; else failed++;
                report.AppendLine($"{(ok ? "OK" : "FALHOU")}  {name}: {detail}");
            }

            if (SceneManager.GetActiveScene().name != "Regiao1_SitioDeUr")
            {
                Debug.LogWarning("Corpos da equipe, teste: abra a cena Regiao1_SitioDeUr antes.");
                return;
            }
            Physics.SyncTransforms();

            var bodies = TeamBodiesSetup.Bodies;
            var found = new Dictionary<int, (GameObject owner, GameObject visual)>();
            foreach (var b in bodies)
            {
                GameObject owner = Find(b.Placeholder, t => t.GetComponent<BoxCollider>() != null);
                Transform visual = owner != null ? owner.transform.Find("Visual") : null;
                string source = visual != null ? AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject)) : "";
                Result(visual != null && source == b.PrefabPath, $"{b.Placeholder} no lugar",
                    visual == null ? "sem o filho Visual (rode Nibiru > Personagens > Corpos da equipe na cena aberta)" : source);
                if (visual != null) found[b.index] = (owner, visual.gameObject);
            }
            if (found.Count < bodies.Length)
            {
                Debug.Log($"Corpos da equipe, teste ({failed} FALHOU):\n" + report);
                return;
            }

            // Distribuição e variedade.
            var counts = bodies.GroupBy(b => b.shortName).ToDictionary(g => g.Key, g => g.Count());
            Result(counts.GetValueOrDefault("Trabalhador") == 3 && counts.GetValueOrDefault("Veterano") == 2 && counts.GetValueOrDefault("Jovem") == 2,
                "distribuição 3/2/2", string.Join(", ", counts.Select(c => $"{c.Key} {c.Value}")));
            int combos = bodies.Select(b => b.PrefabName).Distinct().Count();
            Result(combos == bodies.Length, "nenhuma combinação de modelo e pose se repete", $"{combos} combinações em {bodies.Length} corpos");
            var categories = bodies.Select(b => b.Category).Distinct().ToArray();
            Result(categories.Length >= 3, "pelo menos 3 tipos de pose", string.Join(", ", categories));
            var clash = new List<string>();
            for (int i = 0; i < bodies.Length; i++)
            {
                var a = bodies[i];
                var n = bodies[(i + 1) % bodies.Length];
                if (a.shortName == n.shortName || a.Category == n.Category) clash.Add($"{a.index}-{n.index}");
            }
            Result(clash.Count == 0, "vizinhos com modelo e tipo de pose diferentes", clash.Count == 0 ? "todos" : "repetem: " + string.Join(", ", clash));

            foreach (var b in bodies)
            {
                var (owner, visual) = found[b.index];
                var smr = visual.GetComponentInChildren<SkinnedMeshRenderer>();

                // Parado: nenhum Animator ligado nem controlador.
                var animators = visual.GetComponentsInChildren<Animator>(true);
                Result(animators.All(a => !a.enabled && a.runtimeAnimatorController == null), $"{b.Placeholder} parado",
                    animators.Length == 0 ? "sem Animator" : $"{animators.Length} Animator desligado, sem controlador");

                // Chão: ponto mais baixo encostado, nada enterrado, quadril e peito apoiados.
                float ground = TeamBodiesSetup.GroundY(owner.transform);
                Vector3[] skin = TeamBodiesSetup.Skin(smr);
                float lowest = skin.Min(v => v.y) - ground;
                Result(lowest >= -BelowGround && lowest <= LowestAbove, $"{b.Placeholder} no chão",
                    $"ponto mais baixo {lowest * 100:F1} cm do chão (y = {ground:F2})");
                var animator = visual.GetComponentInChildren<Animator>(true);
                string Gap(HumanBodyBones bone, out float g)
                {
                    Vector3 p = animator.GetBoneTransform(bone).position;
                    float min = float.MaxValue;
                    foreach (var v in skin) if ((v - p).sqrMagnitude < ContactRadius * ContactRadius) min = Mathf.Min(min, v.y);
                    g = min - ground;
                    return $"{g * 100:F1} cm";
                }
                string hips = Gap(HumanBodyBones.Hips, out float gh), chest = Gap(HumanBodyBones.Chest, out float gc);
                Result(gh <= ContactGap && gc <= ContactGap, $"{b.Placeholder} apoiado", $"quadril {hips}, peito {chest}");

                // Colisor cobre a silhueta deitada (vista de cima).
                var box = owner.GetComponent<BoxCollider>();
                var (lo, hi) = TeamBodiesSetup.Silhouette(visual, owner.transform);
                var (outside, slack) = TeamBodiesSetup.Compare(box, lo, hi);
                bool solid = box.enabled && !box.isTrigger && owner.transform.lossyScale == Vector3.one;
                Result(solid && outside <= TeamBodiesSetup.MaxOutside && slack <= TeamBodiesSetup.MaxSlack, $"{b.Placeholder} colisor cobre a silhueta",
                    $"para fora {outside * 100:F0} cm (máx. {TeamBodiesSetup.MaxOutside * 100:F0}), sobra {slack * 100:F0} cm (máx. {TeamBodiesSetup.MaxSlack * 100:F0})" +
                    (solid ? "" : "; colisor desligado, gatilho ou com escala"));

                // Examinar: a área continua lá, com a conversa, e cobre o corpo.
                GameObject zone = Find(b.Zone, t => t.GetComponent<DialogueStarter>() != null);
                bool zoneOk = zone != null && zone.activeInHierarchy && zone.GetComponent<BoxCollider>() is { isTrigger: true };
                bool covers = false;
                if (zoneOk)
                {
                    Vector3 center = owner.transform.TransformPoint(new Vector3((lo.x + hi.x) * 0.5f, 0f, (lo.y + hi.y) * 0.5f));
                    center.y = zone.GetComponent<BoxCollider>().bounds.center.y;
                    covers = zone.GetComponent<BoxCollider>().bounds.Contains(center);
                }
                Result(zoneOk && covers, $"{b.Zone}", zone == null ? "não existe" : !zoneOk ? "sem gatilho ou desligada" : covers ? "gatilho com a conversa, sobre o corpo" : "não cobre o centro do corpo");
            }

            GameObject bag = Find("Bolsa_Camera_Tom", t => t.GetComponent<DialogueStarter>() != null);
            Result(bag != null && bag.activeInHierarchy, "bolsa do Tom (início de \"O que o Tom viu\")", bag != null ? "lá, com a conversa" : "não existe");

            report.AppendLine(TeamBodiesSetup.MeetingPointsReport());
            Debug.Log($"Corpos da equipe, teste ({(failed == 0 ? $"{passed} OK" : $"{passed} OK, {failed} FALHOU")}):\n" + report);
        }

        static GameObject Find(string name, System.Func<Transform, bool> extra)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.gameObject.scene.IsValid() && extra(t)) return t.gameObject;
            return null;
        }
    }
}
