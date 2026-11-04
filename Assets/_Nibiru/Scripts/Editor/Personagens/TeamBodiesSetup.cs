using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Nibiru.EditorTools.Content;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Os corpos da equipe no círculo do Acampamento (passo 2): as caixas `Corpo_1` a `Corpo_3` do blockout viram os
    /// modelos deitados e parados. Decisão do dono (3/nov/2026): 4 lugares em volta da fogueira (CampCircle), 3 corpos,
    /// um de cada modelo, com 3 tipos de pose (de costas, de lado e de bruços, do último quadro de mortes do Mixamo), e o
    /// 4º lugar vazio, o do Tom, só com a bolsa da câmera. (Antes eram 7 corpos com 3 modelos: a repetição aparecia de cima.)
    ///
    /// O menu também arruma a cena aberta para o desenho do CampCircle, sem refazer o Blockout nem o Acampamento: apaga
    /// os `Corpo_N` e `Examinar_Corpo_N` que sobram, leva as caixas, as áreas de "Examinar" e a bolsa do Tom para os
    /// lugares da tabela e refaz a malha de navegação da região.
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
    /// cima).
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
            public readonly bool headIn;
            public readonly float yaw, along, across;

            public Body(int index, string model, string shortName, string pose, bool headIn, float yaw, float along, float across)
            {
                this.index = index; this.model = model; this.shortName = shortName; this.pose = pose;
                this.headIn = headIn; this.yaw = yaw; this.along = along; this.across = across;
            }

            public string Placeholder => "Corpo_" + index;
            public string Zone => "Examinar_Corpo_" + index;
            public string PrefabName => $"EquipeMorta_{shortName}_{pose}";
            public string PrefabPath => VillageTeamSetup.PrefabFolder + PrefabName + ".prefab";
            /// <summary>Costas, Brucos ou Lado (sem o número da variante).</summary>
            public string Category => pose.TrimEnd('2', '3');
        }

        const string Veterano = "ArqueologoVeterano", Jovem = "ArqueologaJovem", Trabalhador = "TrabalhadorEscavacao";

        // Nos lugares do CampCircle: o corpo 1 de frente para quem sai da barraca da Elena (o "primeiro rosto": o trabalhador
        // de costas, o único modelo que deita de costas sem a mochila ou o cabelo erguer o corpo), o 2 no fundo, atrás da
        // fogueira, e o 3 à esquerda; o lugar vazio do Tom fica à direita.
        // along: deslocamento para fora do círculo (m); across: de lado (m); yaw: giro extra (graus).
        public static readonly Body[] Bodies =
        {
            // +20° (o máximo combinado): alinhado com o olhar de quem sai da barraca, a barriga escondia o rosto; assim ele aparece de perfil.
            new(1, Trabalhador, "Trabalhador", "Costas", headIn: true, yaw: 20f, along: 0.05f, across: 0f),
            new(2, Veterano, "Veterano", "Lado2", headIn: false, yaw: 12f, along: 0.10f, across: 0f),
            new(3, Jovem, "Jovem", "Brucos", headIn: true, yaw: 15f, along: 0f, across: -0.05f),
        };

        // Limites do colisor pedidos pelo dono (vista de cima).
        public const float MaxOutside = 0.10f, MaxSlack = 0.25f;
        const float MaxTilt = 12f, TiltStep = 0.5f, ContactRadius = 0.25f;

        public static string ClipName(string pose) => "EquipeMorta_" + pose;

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
            AssetDatabase.SaveAssets();

            var report = new System.Text.StringBuilder();
            report.AppendLine(ArrangeCircle());
            foreach (var b in Bodies)
            {
                GameObject placeholder = FindInScene(b.Placeholder, needsBox: true);
                if (placeholder == null)
                {
                    Debug.LogWarning($"Corpos da equipe: não achei {b.Placeholder} na cena (rode o menu do Blockout).");
                    return;
                }
                Place(placeholder, b, report);
            }

            // Caixas e colisores saíram ou mudaram de lugar: a malha da região é sempre refeita.
            report.AppendLine(RebakeNavMesh());
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Corpos da equipe: {Bodies.Length} corpos deitados e o lugar vazio do Tom. Salve a cena.\n" + report);
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

        // ---------- Cena ----------

        /// <summary>
        /// Deixa a cena aberta no desenho do CampCircle: apaga os corpos e as áreas de "Examinar" que sobram (de quando
        /// eram 7) e leva as caixas, as áreas e a bolsa do Tom para os lugares da tabela.
        /// </summary>
        static string ArrangeCircle()
        {
            var removed = new List<string>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                if ((Extra(t.name, "Corpo_") && t.GetComponent<BoxCollider>() != null) || Extra(t.name, "Examinar_Corpo_"))
                {
                    removed.Add(t.name);
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            for (int n = 1; n <= CampCircle.BodyCount; n++)
            {
                Quaternion yaw = Quaternion.Euler(0f, CampCircle.BodyYaw(n), 0f);
                GameObject box = FindInScene($"Corpo_{n}", needsBox: true);
                if (box != null)
                    box.transform.SetPositionAndRotation(new Vector3(CampCircle.BodyPlace(n).x, box.transform.position.y, CampCircle.BodyPlace(n).z), yaw);
                GameObject zone = FindInScene($"Examinar_Corpo_{n}", needsBox: false);
                if (zone != null) zone.transform.SetPositionAndRotation(CampCircle.BodyPlace(n) + Vector3.up * 0.4f, yaw);
            }

            string bagNote = "AVISO: não achei a Bolsa_Camera_Tom (rode o menu do Acampamento).";
            GameObject bag = FindInScene("Bolsa_Camera_Tom", needsBox: false);
            if (bag != null)
            {
                bag.transform.SetPositionAndRotation(CampCircle.BagPosition + Vector3.up * (bag.transform.localScale.y * 0.5f),
                    Quaternion.Euler(0f, CampCircle.BagYaw, 0f));
                bagNote = $"bolsa do Tom no lugar vazio ({bag.transform.position.x:F2}; {bag.transform.position.z:F2})";
            }
            Physics.SyncTransforms();
            return $"Círculo: {CampCircle.BodyCount} lugares com corpo e o do Tom vazio; {bagNote}; " +
                (removed.Count == 0 ? "nada sobrando." : "apagados: " + string.Join(", ", removed.OrderBy(s => s)));
        }

        static bool Extra(string name, string prefix) =>
            name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out int n) && n > CampCircle.BodyCount;

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
            report.AppendLine($"{b.Placeholder}: {b.PrefabName}, chão y = {ground:F2}; colisor {before}" +
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
            return "Malha de navegação refeita. " + MeetingPointsReport();
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
