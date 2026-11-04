using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Os 7 corpos da equipe no círculo do Acampamento (passo 2): as caixas `Corpo_1` a `Corpo_7` do blockout viram
    /// os modelos deitados e parados (decisão do dono em 3/nov/2026). 3 modelos para 7 corpos: a repetição some pela
    /// pose (de costas, de bruços e de lado, do último quadro de mortes do Mixamo), pelo giro no círculo e por um tom
    /// "empoeirado" nas segundas cópias.
    ///
    /// Cada combinação de modelo e pose é um prefab (`Prefabs/Personagens/EquipeMorta_<Modelo>_<Pose>`), variante do
    /// prefab do modelo: a pose fica gravada nos ossos e o Animator fica desligado e sem controlador (custo zero de
    /// animação). O prefab já sai com a cabeça para +z, o centro da silhueta na origem, o ponto mais baixo da pele no
    /// chão e uma inclinação de até 12° que encosta quadril e peito no chão (as mortes acabam com o pé mais baixo que o tronco).
    ///
    /// Na cena: a caixa continua dona do lugar e do colisor (a interação fica nas áreas `Examinar_Corpo_N`, à parte, e não
    /// muda). Saem o MeshRenderer e o MeshFilter; a escala da caixa passa para o tamanho do BoxCollider (mesmo volume no
    /// mundo), para o corpo poder girar sem se deformar; o modelo entra como filho "Visual". Se a silhueta deitada passar
    /// mais de 10 cm para fora do colisor, ou o colisor sobrar mais de 25 cm, o colisor é ajustado à silhueta (vista de
    /// cima) e a malha de navegação da região é refeita.
    ///
    /// Menu: Nibiru > Personagens > Corpos da equipe na cena aberta. Rode depois do Blockout, do Acampamento e do
    /// Montar personagens da Aldeia e da equipe (e de Nibiru > Animações > Importar clipes da biblioteca).
    /// </summary>
    public static class TeamBodiesSetup
    {
        public readonly struct Body
        {
            public readonly int index;
            public readonly string model, shortName, pose;
            public readonly bool headIn, dusty;
            public readonly float yaw, along, across;

            public Body(int index, string model, string shortName, string pose, bool headIn, float yaw, float along, float across, bool dusty)
            {
                this.index = index; this.model = model; this.shortName = shortName; this.pose = pose;
                this.headIn = headIn; this.yaw = yaw; this.along = along; this.across = across; this.dusty = dusty;
            }

            public string Placeholder => "Corpo_" + index;
            public string Zone => "Examinar_Corpo_" + index;
            public string PrefabName => $"EquipeMorta_{shortName}_{pose}";
            public string PrefabPath => VillageTeamSetup.PrefabFolder + PrefabName + ".prefab";
            /// <summary>Costas, Brucos ou Lado (sem o número da variante).</summary>
            public string Category => pose.TrimEnd('2', '3');
        }

        const string Veterano = "ArqueologoVeterano", Jovem = "ArqueologaJovem", Trabalhador = "TrabalhadorEscavacao";

        // Em volta da fogueira, na ordem do blockout (o corpo 1 a leste, sentido anti-horário visto de cima). A Elena chega
        // da barraca, a oeste: os corpos 3 e 4 são os primeiros que ela vê. Vizinhos nunca repetem modelo nem tipo de pose;
        // nenhuma combinação de modelo e pose se repete; o tom empoeirado vai nas segundas cópias (4, 5 e 7).
        // along: deslocamento para fora do círculo (m); across: de lado (m); yaw: giro extra (graus).
        public static readonly Body[] Bodies =
        {
            new(1, Veterano, "Veterano", "Lado2", headIn: true, yaw: 12f, along: 0.10f, across: 0f, dusty: false),
            new(2, Trabalhador, "Trabalhador", "Brucos", headIn: false, yaw: -18f, along: -0.10f, across: 0.08f, dusty: false),
            new(3, Jovem, "Jovem", "Lado2", headIn: true, yaw: 7f, along: 0f, across: -0.10f, dusty: false),
            new(4, Trabalhador, "Trabalhador", "Costas", headIn: true, yaw: -10f, along: 0.05f, across: 0f, dusty: true),
            new(5, Veterano, "Veterano", "Brucos2", headIn: false, yaw: 20f, along: 0f, across: 0.10f, dusty: true),
            new(6, Trabalhador, "Trabalhador", "Lado3", headIn: true, yaw: -5f, along: -0.12f, across: 0f, dusty: false),
            new(7, Jovem, "Jovem", "Brucos", headIn: false, yaw: 15f, along: 0.10f, across: -0.05f, dusty: true),
        };

        // Limites do colisor pedidos pelo dono (vista de cima).
        public const float MaxOutside = 0.10f, MaxSlack = 0.25f;
        const float MaxTilt = 12f, TiltStep = 0.5f, ContactRadius = 0.25f;
        static readonly Color DustTint = new(0.88f, 0.85f, 0.80f, 1f);

        public static string ClipName(string pose) => "EquipeMorta_" + pose;
        public static string DustyMaterialPath(string model) => $"{VillageTeamSetup.ArtRoot}{model}/Materiais/{model}_Poeira.mat";

        [MenuItem("Nibiru/Personagens/Corpos da equipe na cena aberta")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes.");
                return;
            }
            foreach (var group in Bodies.GroupBy(b => b.PrefabPath))
            {
                var b = group.First();
                string problem = BuildPrefab(b);
                if (problem != null)
                {
                    Debug.LogError("Corpos da equipe: " + problem);
                    return;
                }
            }
            foreach (string model in Bodies.Where(b => b.dusty).Select(b => b.model).Distinct())
                DustyMaterial(model);
            AssetDatabase.SaveAssets();

            var report = new System.Text.StringBuilder();
            bool collidersChanged = false;
            foreach (var b in Bodies)
            {
                GameObject placeholder = FindInScene(b.Placeholder, needsBox: true);
                if (placeholder == null)
                {
                    Debug.LogWarning($"Corpos da equipe: não achei {b.Placeholder} na cena (rode o menu do Blockout).");
                    return;
                }
                collidersChanged |= Place(placeholder, b, report);
            }

            if (collidersChanged) report.AppendLine(RebakeNavMesh());
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Corpos da equipe: 7 corpos deitados no lugar das caixas. Salve a cena.\n" + report);
        }

        // ---------- Prefab de cada combinação ----------

        static string BuildPrefab(Body b)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(VillageTeamSetup.PrefabPath(b.model));
            var clip = AnimationLibrary.Clip(ClipName(b.pose));
            if (source == null) return $"falta o prefab {VillageTeamSetup.PrefabPath(b.model)} (rode Montar personagens da Aldeia e da equipe).";
            if (clip == null) return $"falta o clipe {ClipName(b.pose)} (rode Nibiru > Animações > Importar clipes da biblioteca).";

            var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Transform model = root.transform.Find("Modelo");
                var animator = model.GetComponent<Animator>();
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
                clip.SampleAnimation(model.gameObject, clip.length);
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;

                // Pele na pose, no espaço do prefab.
                var smr = root.GetComponentInChildren<SkinnedMeshRenderer>();
                Vector3[] verts = Skin(smr);
                Vector3 hips = animator.GetBoneTransform(HumanBodyBones.Hips).position;
                Vector3 chest = animator.GetBoneTransform(HumanBodyBones.Chest).position;
                Vector3 head = animator.GetBoneTransform(HumanBodyBones.Head).position;

                // 1. Cabeça para +z.
                Vector3 toHead = head - hips;
                toHead.y = 0f;
                Quaternion align = Quaternion.Euler(0f, -Vector3.SignedAngle(Vector3.forward, toHead, Vector3.up), 0f);

                // 2. Inclinação (até 12°) que encosta quadril e peito no chão.
                int[] nearHips = Near(verts, hips), nearChest = Near(verts, chest);
                Quaternion best = align;
                float bestScore = float.MaxValue;
                for (float p = -MaxTilt; p <= MaxTilt + 0.001f; p += TiltStep)
                    for (float r = -MaxTilt; r <= MaxTilt + 0.001f; r += TiltStep)
                    {
                        Quaternion q = Quaternion.Euler(p, 0f, r) * align;
                        Vector3 up = Quaternion.Inverse(q) * Vector3.up; // y depois do giro = Dot(v, up)
                        float min = float.MaxValue;
                        foreach (var v in verts) min = Mathf.Min(min, Vector3.Dot(v, up));
                        float gap = Mathf.Max(MinAlong(verts, nearHips, up), MinAlong(verts, nearChest, up)) - min;
                        float score = gap + 0.002f * (Mathf.Abs(p) + Mathf.Abs(r));
                        if (score < bestScore) { bestScore = score; best = q; }
                    }

                // 3. Centro da silhueta na origem e ponto mais baixo no chão.
                Vector3 lo = Vector3.one * float.MaxValue, hi = Vector3.one * float.MinValue;
                foreach (var v in verts)
                {
                    Vector3 w = best * v;
                    lo = Vector3.Min(lo, w);
                    hi = Vector3.Max(hi, w);
                }
                model.localRotation = best;
                model.localPosition = new Vector3(-(lo.x + hi.x) * 0.5f, -lo.y, -(lo.z + hi.z) * 0.5f);

                // 4. Parado: sem controlador e com o Animator desligado (a pose fica nos ossos).
                animator.runtimeAnimatorController = null;
                animator.enabled = false;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

                PrefabUtility.SaveAsPrefabAsset(root, b.PrefabPath);
                return null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static int[] Near(Vector3[] verts, Vector3 bone)
        {
            var list = new List<int>();
            for (int i = 0; i < verts.Length; i++)
                if ((verts[i] - bone).sqrMagnitude < ContactRadius * ContactRadius) list.Add(i);
            return list.ToArray();
        }

        static float MinAlong(Vector3[] verts, int[] indices, Vector3 up)
        {
            float min = float.MaxValue;
            foreach (int i in indices) min = Mathf.Min(min, Vector3.Dot(verts[i], up));
            return min;
        }

        /// <summary>Vértices da pele na pose atual, no espaço do mundo.</summary>
        public static Vector3[] Skin(SkinnedMeshRenderer smr)
        {
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            var toWorld = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            Vector3[] verts = baked.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] = toWorld.MultiplyPoint3x4(verts[i]);
            Object.DestroyImmediate(baked);
            return verts;
        }

        static void DustyMaterial(string model)
        {
            string path = DustyMaterialPath(model);
            var smr = AssetDatabase.LoadAssetAtPath<GameObject>(VillageTeamSetup.PrefabPath(model)).GetComponentInChildren<SkinnedMeshRenderer>();
            Material original = smr.sharedMaterial;
            var dusty = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (dusty == null)
            {
                dusty = new Material(original);
                AssetDatabase.CreateAsset(dusty, path);
            }
            else dusty.CopyPropertiesFromMaterial(original);
            Color baseColor = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
            dusty.SetColor("_BaseColor", baseColor * DustTint);
            EditorUtility.SetDirty(dusty);
        }

        // ---------- Cena ----------

        static GameObject FindInScene(string name, bool needsBox)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.gameObject.scene.IsValid() && (!needsBox || t.GetComponent<BoxCollider>() != null))
                    return t.gameObject;
            return null;
        }

        /// <summary>Troca a caixa pelo corpo; devolve true se o colisor teve de mudar.</summary>
        static bool Place(GameObject placeholder, Body b, System.Text.StringBuilder report)
        {
            Transform t = placeholder.transform;
            var box = placeholder.GetComponent<BoxCollider>();
            Object.DestroyImmediate(placeholder.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(placeholder.GetComponent<MeshFilter>());
            var old = t.Find("Visual");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // A escala da caixa vira o tamanho do colisor (mesmo volume no mundo).
            Vector3 s = t.localScale;
            if (s != Vector3.one)
            {
                box.center = Vector3.Scale(box.center, s);
                box.size = Vector3.Scale(box.size, s);
                t.localScale = Vector3.one;
            }

            float ground = GroundY(t);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(b.PrefabPath);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, placeholder.scene);
            visual.name = "Visual";
            visual.transform.SetParent(t, false);
            visual.transform.localPosition = new Vector3(b.across, ground - t.position.y, b.along);
            visual.transform.localRotation = Quaternion.Euler(0f, (b.headIn ? 180f : 0f) + b.yaw, 0f);
            visual.transform.localScale = Vector3.one;
            if (b.dusty)
            {
                var smr = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                smr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DustyMaterialPath(b.model));
                PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
            }

            // Colisor x silhueta (vista de cima).
            var (lo, hi) = Silhouette(visual, t);
            var fit = Compare(box, lo, hi);
            bool changed = fit.outside > MaxOutside || fit.slack > MaxSlack;
            string before = $"para fora {fit.outside * 100:F0} cm, sobra {fit.slack * 100:F0} cm";
            if (changed)
            {
                box.center = new Vector3((lo.x + hi.x) * 0.5f, box.center.y, (lo.y + hi.y) * 0.5f);
                box.size = new Vector3(hi.x - lo.x, box.size.y, hi.y - lo.y);
            }
            report.AppendLine($"{b.Placeholder}: {b.PrefabName}{(b.dusty ? " (empoeirado)" : "")}, chão y = {ground:F2}; colisor {before}" +
                (changed ? $" -> ajustado a {box.size.x:F2} x {box.size.z:F2} m" : " (mantido)"));
            return changed;
        }

        /// <summary>Retângulo da pele vista de cima, no espaço local do dono do colisor (x, z).</summary>
        public static (Vector2 lo, Vector2 hi) Silhouette(GameObject visual, Transform owner)
        {
            var lo = new Vector2(float.MaxValue, float.MaxValue);
            var hi = new Vector2(float.MinValue, float.MinValue);
            foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach (var w in Skin(smr))
                {
                    Vector3 l = owner.InverseTransformPoint(w);
                    lo = Vector2.Min(lo, new Vector2(l.x, l.z));
                    hi = Vector2.Max(hi, new Vector2(l.x, l.z));
                }
            return (lo, hi);
        }

        /// <summary>Quanto a silhueta passa para fora do colisor e quanto o colisor sobra, no pior lado.</summary>
        public static (float outside, float slack) Compare(BoxCollider box, Vector2 lo, Vector2 hi)
        {
            Vector2 bLo = new Vector2(box.center.x - box.size.x * 0.5f, box.center.z - box.size.z * 0.5f);
            Vector2 bHi = new Vector2(box.center.x + box.size.x * 0.5f, box.center.z + box.size.z * 0.5f);
            float outside = Mathf.Max(Mathf.Max(bLo.x - lo.x, bLo.y - lo.y), Mathf.Max(hi.x - bHi.x, hi.y - bHi.y));
            float slack = Mathf.Max(Mathf.Max(lo.x - bLo.x, lo.y - bLo.y), Mathf.Max(bHi.x - hi.x, bHi.y - hi.y));
            return (Mathf.Max(0f, outside), Mathf.Max(0f, slack));
        }

        /// <summary>Altura do chão debaixo do corpo (ignora o próprio colisor e os gatilhos).</summary>
        public static float GroundY(Transform owner)
        {
            var hits = Physics.RaycastAll(owner.position + Vector3.up * 3f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MinValue;
            foreach (var h in hits)
                if (!h.collider.transform.IsChildOf(owner) && h.point.y > best) best = h.point.y;
            return best == float.MinValue ? owner.position.y : best;
        }

        // ---------- Malha de navegação ----------

        /// <summary>Refaz a malha da região (a mesma superfície do blockout) e confere os 4 pontos de encontro da Aldeia.</summary>
        static string RebakeNavMesh()
        {
            var surface = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).FirstOrDefault();
            if (surface == null) return "AVISO: não achei a NavMeshSurface do blockout; a malha não foi refeita.";
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            string scenePath = surface.gameObject.scene.path;
            string dataPath = scenePath.Substring(0, scenePath.Length - ".unity".Length) + "/NavMesh-Blockout.asset";
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
            EditorUtility.SetDirty(surface);
            return "Malha de navegação refeita (algum colisor mudou). " + MeetingPointsReport();
        }

        /// <summary>Os 4 pontos do encontro com o Samir (Aldeia) continuam sobre a malha?</summary>
        public static string MeetingPointsReport()
        {
            GameObject group = FindInScene("Pontos_Encontro", needsBox: false);
            if (group == null) return "AVISO: não achei os Pontos_Encontro da Aldeia.";
            var lines = new List<string>();
            foreach (Transform p in group.transform)
            {
                bool ok = NavMesh.SamplePosition(p.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas);
                lines.Add($"{p.name} {(ok ? $"OK ({Vector3.Distance(hit.position, p.position):F2} m)" : "FORA DA MALHA")}");
            }
            return $"Pontos de encontro ({lines.Count}): " + string.Join(", ", lines);
        }
    }
}
