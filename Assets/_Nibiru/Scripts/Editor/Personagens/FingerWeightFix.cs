using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Corrige, na importação, ossos de dedo do AccuRig que vieram sem peso (o dedo não dobra
    /// naquela junta). Vale para todo modelo em Art/Personagens: cada falange sem peso
    /// (CC_Base_L_Mid2, por exemplo) recebe a parte da pele que fica no trecho dela, tirada dos
    /// vizinhos do mesmo dedo (a falange de antes, a de depois e, na primeira, a mão), com uma
    /// transição suave nas juntas. O FBX original não muda; a correção é refeita a cada importação.
    /// </summary>
    class FingerWeightFix : AssetPostprocessor
    {
        const string Folder = "Assets/_Nibiru/Art/Personagens/";
        static readonly Regex FingerBone = new(@"^(CC_Base_[LR]_(?:Thumb|Index|Mid|Ring|Pinky))([123])$");
        // Largura da transição em cada junta, em fração do trecho da falange.
        const float Blend = 0.25f;

        public override uint GetVersion() => 1;

        void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(Folder)) return;
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                Fix(renderer);
        }

        void Fix(SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            Transform[] bones = renderer.bones;
            if (mesh == null || bones == null || bones.Length == 0) return;

            NativeArray<byte> counts = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> flat = mesh.GetAllBoneWeights();
            var weights = new List<List<BoneWeight1>>(mesh.vertexCount);
            int k = 0;
            for (int v = 0; v < mesh.vertexCount; v++)
            {
                var list = new List<BoneWeight1>(counts[v]);
                for (int i = 0; i < counts[v]; i++) list.Add(flat[k++]);
                weights.Add(list);
            }

            var used = new float[bones.Length];
            foreach (var list in weights)
                foreach (var w in list) used[w.boneIndex] += w.weight;

            var index = new Dictionary<string, int>();
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) index[bones[i].name] = i;

            Vector3[] vertices = mesh.vertices;
            Matrix4x4[] bindposes = mesh.bindposes;
            Vector3 Joint(int bone) => bindposes[bone].inverse.MultiplyPoint3x4(Vector3.zero);

            var fixedBones = new List<string>();
            for (int b = 0; b < bones.Length; b++)
            {
                if (bones[b] == null || used[b] > 0.001f) continue;
                Match match = FingerBone.Match(bones[b].name);
                if (!match.Success) continue;
                string finger = match.Groups[1].Value;
                int phalanx = int.Parse(match.Groups[2].Value);

                // A corrente do dedo: falanges 1 a 3; as juntas e a ponta (a 3ª prolongada).
                int[] chain = new int[4];
                bool complete = true;
                for (int p = 1; p <= 3; p++)
                    complete &= index.TryGetValue(finger + p, out chain[p]);
                if (!complete) continue;
                Vector3[] joints = { Joint(chain[1]), Joint(chain[2]), Joint(chain[3]), Vector3.zero };
                joints[3] = joints[2] + (joints[2] - joints[1]) * 0.8f;

                // Quem cede peso: as falanges vizinhas e, para a primeira, a mão (o pai dela).
                var donors = new HashSet<int>();
                if (phalanx > 1) donors.Add(chain[phalanx - 1]);
                if (phalanx < 3) donors.Add(chain[phalanx + 1]);
                Transform hand = bones[chain[1]].parent;
                if (phalanx == 1 && hand != null && index.TryGetValue(hand.name, out int handIndex)) donors.Add(handIndex);

                Vector3 start = joints[phalanx - 1], end = joints[phalanx];
                float length = Vector3.Distance(start, end);
                if (length < 1e-5f) continue;
                Vector3 axis = (end - start) / length;
                float reach = Vector3.Distance(joints[0], joints[3]) * 0.35f;

                int moved = 0;
                for (int v = 0; v < weights.Count; v++)
                {
                    var list = weights[v];
                    float donated = 0f;
                    foreach (var w in list) if (donors.Contains(w.boneIndex)) donated += w.weight;
                    if (donated <= 0f) continue;

                    // Onde o vértice fica ao longo do trecho desta falange (0 = junta de trás, 1 = da frente).
                    Vector3 offset = vertices[v] - start;
                    float along = Vector3.Dot(offset, axis) / length;
                    if ((offset - axis * along * length).magnitude > reach) continue;
                    float share = Mathf.Min(Mathf.InverseLerp(-Blend, Blend, along), 1f - Mathf.InverseLerp(1f - Blend, 1f + Blend, along));
                    share = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(share));
                    if (share <= 0f) continue;

                    for (int i = 0; i < list.Count; i++)
                        if (donors.Contains(list[i].boneIndex))
                            list[i] = new BoneWeight1 { boneIndex = list[i].boneIndex, weight = list[i].weight * (1f - share) };
                    list.Add(new BoneWeight1 { boneIndex = b, weight = donated * share });
                    moved++;
                }
                if (moved > 0) fixedBones.Add($"{bones[b].name} ({moved} vértices)");
            }
            counts.Dispose();
            flat.Dispose();
            if (fixedBones.Count == 0) return;

            // Mesmo formato do Unity: no máximo 4 pesos por vértice, do maior para o menor, somando 1.
            var newCounts = new NativeArray<byte>(weights.Count, Allocator.Temp);
            var all = new List<BoneWeight1>();
            for (int v = 0; v < weights.Count; v++)
            {
                var top = weights[v].Where(w => w.weight > 0f).OrderByDescending(w => w.weight).Take(4).ToList();
                float sum = top.Sum(w => w.weight);
                newCounts[v] = (byte)top.Count;
                foreach (var w in top) all.Add(new BoneWeight1 { boneIndex = w.boneIndex, weight = sum > 0f ? w.weight / sum : 0f });
            }
            var newWeights = new NativeArray<BoneWeight1>(all.ToArray(), Allocator.Temp);
            mesh.SetBoneWeights(newCounts, newWeights);
            newCounts.Dispose();
            newWeights.Dispose();
            Debug.Log($"Pesos de dedo corrigidos em {assetPath}: {string.Join(", ", fixedBones)}.");
        }
    }
}
