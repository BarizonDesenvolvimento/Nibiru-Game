using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Mede a pose dos braços dos clipes principais no modelo de origem (X Bot) e na Elena: ângulo do braço,
    /// dobra do cotovelo e quanto a mão entra na coxa (distância com sinal: negativo = dentro).
    /// Só leitura; imprime um resumo no Console.
    /// </summary>
    static class PoseDiagnostic
    {
        const string XBot = "Assets/_Nibiru/Art/Animacoes/Mixamo/X Bot.fbx";
        const string ElenaModel = "Assets/_Nibiru/Art/Personagens/ElenaNova/ElenaNova.fbx";
        static readonly string[] Clips = { "Elena_Parada_Basic", "Elena_Andar_Basic", "Elena_Correr_Basic" };

        [MenuItem("Nibiru/Testes/Diagnóstico da pose parada")]
        static void Run() => Debug.Log(Report());

        /// <summary>Tira fotos da Elena parada (frente, lado e 3/4) com os limites de agora e salva num PNG.</summary>
        public static void Capture(string pngPath, string clipName = "Elena_Parada_Basic", float time = 0.5f)
        {
            var xbot = Spawn(XBot, new Vector3(-5, 0, 0));
            var elena = Spawn(ElenaModel, Vector3.zero);
            var clip = clipName.StartsWith("Elena_Parada_Var")
                ? AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Nibiru/Art/Animacoes/Elena/" + clipName + ".anim")
                : AnimationLibrary.Clip(clipName);
            clip.SampleAnimation(xbot, clip.length * time);
            var pose = new HumanPose();
            new HumanPoseHandler(xbot.GetComponent<Animator>().avatar, xbot.transform).GetHumanPose(ref pose);
            new HumanPoseHandler(elena.GetComponent<Animator>().avatar, elena.transform).SetHumanPose(ref pose);
            Object.DestroyImmediate(xbot);
            var light = new GameObject("luz").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(35, 150, 0);
            var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.65f);
            cam.fieldOfView = 25; cam.cullingMask = 1 << 30; SetLayer(elena, 30); light.cullingMask = 1 << 30;
            var rt = new RenderTexture(480, 760, 24);
            cam.targetTexture = rt;
            var atlas = new Texture2D(480 * 3, 760, TextureFormat.RGB24, false);
            Vector3 center = new Vector3(0, 0.95f, 0);
            Vector3[] dirs = { new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0.7f, 0, 0.7f) };
            for (int i = 0; i < 3; i++)
            {
                camGo.transform.position = center + dirs[i] * 5.2f;
                camGo.transform.LookAt(center);
                cam.Render();
                RenderTexture.active = rt;
                atlas.ReadPixels(new Rect(0, 0, 480, 760), 480 * i, 0);
            }
            RenderTexture.active = null;
            atlas.Apply();
            System.IO.File.WriteAllBytes(pngPath, atlas.EncodeToPNG());
            Object.DestroyImmediate(rt); Object.DestroyImmediate(atlas);
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(light.gameObject);
            Object.DestroyImmediate(elena);
        }

        [MenuItem("Nibiru/Testes/Fotos comparativas da pose parada (PNG na pasta Temp; reimporta a Elena)")]
        static void CaptureMenu()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ElenaModel);
            int n = 0;
            foreach (var (l, r, f) in new[] { (-47f, -43f, 80f), (-52f, -48f, 80f), (-60f, -60f, 80f) })
            {
                ElenaSetup.OpenArms(importer, l, r, f);
                string path = System.IO.Path.GetFullPath($"Temp/pose_parada_{n++}.png");
                Capture(path);
                Debug.Log($"Foto {n - 1} (braço {l}/{r}, antebraço {f}) salva em {path}");
            }
        }

        [MenuItem("Nibiru/Testes/Fotos das variações de parada (PNG na pasta Temp)")]
        static void CaptureVariations()
        {
            foreach (var v in UalIdleImport.Variations)
                for (int i = 0; i < 2; i++)
                    Capture(System.IO.Path.GetFullPath($"Temp/{v.name}_{i}.png"), v.name, i == 0 ? 0.1f : 0.6f);
            foreach (int i in new[] { 0 })
                Capture(System.IO.Path.GetFullPath("Temp/Elena_Parada_Basic_0.png"), "Elena_Parada_Basic", 0.5f);
            Debug.Log("Fotos das variações salvas em Temp/");
        }

        [MenuItem("Nibiru/Testes/Curva do cotovelo (X Bot e Elena)")]
        static void ElbowCurve()
        {
            var sb = new StringBuilder("Curva do cotovelo esquerdo pelo músculo do antebraço (Stretch):\n");
            foreach (var (label, path, at) in new[] { ("X Bot", XBot, new Vector3(-2, 0, 0)), ("Elena", ElenaModel, Vector3.zero) })
            {
                var go = Spawn(path, at);
                var a = go.GetComponent<Animator>();
                var handler = new HumanPoseHandler(a.avatar, go.transform);
                var up = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                var low = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                var hand = a.GetBoneTransform(HumanBodyBones.LeftHand);
                float Bend() => Vector3.Angle((low.position - up.position), (hand.position - low.position));
                sb.Append($"{label} (pose do arquivo): {Bend():F0}°; ");
                int m = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftLowerArm, 2);
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                foreach (float v in new[] { -1f, -0.5f, 0f, 0.5f, 0.82f, 1f })
                {
                    pose.muscles[m] = v;
                    handler.SetHumanPose(ref pose);
                    sb.Append($"músculo {v:F2} → {Bend():F0}°; ");
                }
                sb.AppendLine();
                Object.DestroyImmediate(go);
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Nibiru/Testes/Varredura de limites do braço (reimporta a Elena)")]
        static void Sweep()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ElenaModel);
            var sb = new StringBuilder();
            foreach (var (l, r, f) in new[] { (-47f, -43f, 80f), (-60f, -60f, 80f), (-40f, -36f, 80f), (-30f, -30f, 80f) })
            {
                ElenaSetup.OpenArms(importer, l, r, f);
                sb.AppendLine($"=== braço esq {l}, dir {r}, antebraço máx {f} ===");
                sb.Append(Report());
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Texto com a medição de agora (X Bot e Elena) para parada, andar, correr e mira.</summary>
        public static string Report()
        {
            var sb = new StringBuilder();
            var xbot = Spawn(XBot, new Vector3(-2, 0, 0));
            var elena = Spawn(ElenaModel, Vector3.zero);
            var xHandler = new HumanPoseHandler(xbot.GetComponent<Animator>().avatar, xbot.transform);
            var eHandler = new HumanPoseHandler(elena.GetComponent<Animator>().avatar, elena.transform);
            foreach (string name in Clips.Concat(new[] { "Mira" }))
            {
                var clip = name == "Mira"
                    ? AssetDatabase.LoadAllAssetsAtPath("Assets/_Nibiru/Art/Animacoes/Mixamo/Pistol Aim.fbx").OfType<AnimationClip>().FirstOrDefault(c => c.name == "Mira")
                    : AnimationLibrary.Clip(name);
                if (clip == null) { sb.AppendLine("Clipe não achado: " + name); continue; }
                int samples = name == "Elena_Parada_Basic" || name == "Mira" ? 1 : 8;
                float worstGap = float.MaxValue, worstElbow = 0, worstArm = 0, minElbow = 999, minArm = 999;
                string muscles = "";
                for (int i = 0; i < samples; i++)
                {
                    float t = samples == 1 ? clip.length * 0.5f : clip.length * i / samples;
                    clip.SampleAnimation(xbot, t);
                    var pose = new HumanPose();
                    xHandler.GetHumanPose(ref pose);
                    eHandler.SetHumanPose(ref pose);
                    if (i == 0) muscles = Muscles(pose);
                    var (gap, elbow, arm) = Measure(elena);
                    worstGap = Mathf.Min(worstGap, gap);
                    worstElbow = Mathf.Max(worstElbow, elbow); minElbow = Mathf.Min(minElbow, elbow);
                    worstArm = Mathf.Max(worstArm, arm); minArm = Mathf.Min(minArm, arm);
                }
                clip.SampleAnimation(xbot, clip.length * 0.5f);
                var (_, xe, xa) = Measure(xbot);
                sb.AppendLine($"[{name}] Elena: cotovelo {minElbow:F0}–{worstElbow:F0}°, braço {minArm:F0}–{worstArm:F0}° da vertical, mão×coxa pior folga {worstGap * 100:F1} cm. X Bot (referência): cotovelo {xe:F0}°, braço {xa:F0}°. Músculos: {muscles}");
            }
            sb.AppendLine("Limites da Elena: " + Limits());
            Object.DestroyImmediate(xbot);
            Object.DestroyImmediate(elena);
            return sb.ToString();
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static GameObject Spawn(string path, Vector3 at)
        {
            var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.transform.position = at;
            return go;
        }

        static string Muscles(HumanPose pose)
        {
            string s = "";
            foreach (var (label, bone, axis) in new[]
            {
                ("EsqBraço D-U", HumanBodyBones.LeftUpperArm, 2), ("DirBraço D-U", HumanBodyBones.RightUpperArm, 2),
                ("EsqAntebr", HumanBodyBones.LeftLowerArm, 2), ("DirAntebr", HumanBodyBones.RightLowerArm, 2),
            })
                s += $"{label}={pose.muscles[HumanTrait.MuscleFromBone((int)bone, axis)]:F2} ";
            return s;
        }

        static string Limits()
        {
            string s = "";
            var importer = (ModelImporter)AssetImporter.GetAtPath(ElenaModel);
            foreach (var h in importer.humanDescription.human)
                if (h.humanName is "LeftUpperArm" or "RightUpperArm" or "LeftLowerArm" or "RightLowerArm")
                    s += $"{h.humanName} z=[{h.limit.min.z:F0},{h.limit.max.z:F0}] ";
            return s;
        }

        // Devolve a pior folga da mão na coxa (m, negativo = dentro), o maior cotovelo e o maior ângulo do braço.
        static (float gap, float elbow, float arm) Measure(GameObject go)
        {
            var a = go.GetComponent<Animator>();
            float elbowMax = 0, armMax = 0;
            foreach (bool left in new[] { true, false })
            {
                var up = a.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                var low = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                var hand = a.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                Vector3 arm = (low.position - up.position).normalized;
                Vector3 fore = (hand.position - low.position).normalized;
                armMax = Mathf.Max(armMax, Vector3.Angle(arm, Vector3.down));
                elbowMax = Mathf.Max(elbowMax, Vector3.Angle(arm, fore));
            }
            return (HandGap(go, a), elbowMax, armMax);
        }

        static float HandGap(GameObject go, Animator a)
        {
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh.vertexCount == 0) return float.NaN;
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            var verts = baked.vertices; var normals = baked.normals;
            var weights = smr.sharedMesh.boneWeights;
            var role = new Dictionary<Transform, (bool hand, bool leg)>();
            foreach (HumanBodyBones b in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (b == HumanBodyBones.LastBone) continue;
                var t = a.GetBoneTransform(b);
                if (t == null) continue;
                string n = b.ToString();
                bool hand = n.Contains("Hand") || n.Contains("Thumb") || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little");
                bool leg = n.Contains("UpperLeg") || n == "Hips";
                role[t] = (hand, leg);
            }
            var hands = new List<Vector3>(); var legs = new List<(Vector3 p, Vector3 n)>();
            for (int i = 0; i < verts.Length; i++)
            {
                float handW = 0, legW = 0;
                void Add(int bone, float w)
                {
                    if (w <= 0 || bone >= smr.bones.Length || smr.bones[bone] == null) return;
                    if (role.TryGetValue(smr.bones[bone], out var r)) { if (r.hand) handW += w; if (r.leg) legW += w; }
                }
                var bw = weights[i];
                Add(bw.boneIndex0, bw.weight0); Add(bw.boneIndex1, bw.weight1); Add(bw.boneIndex2, bw.weight2); Add(bw.boneIndex3, bw.weight3);
                var p = smr.transform.TransformPoint(verts[i]);
                if (handW > 0.5f) hands.Add(p);
                else if (legW > 0.5f) legs.Add((p, smr.transform.TransformDirection(normals[i]).normalized));
            }
            Object.DestroyImmediate(baked);
            float worst = float.MaxValue;
            foreach (var h in hands)
            {
                float best = float.MaxValue; float signed = 0;
                foreach (var l in legs)
                {
                    float d = (h - l.p).sqrMagnitude;
                    if (d < best) { best = d; signed = Vector3.Dot(h - l.p, l.n) >= 0 ? Mathf.Sqrt(d) : -Mathf.Sqrt(d); }
                }
                worst = Mathf.Min(worst, signed);
            }
            return worst;
        }
    }
}
