using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Objetos e ambientes do Tripo e do Meshy fora da vitrine da Aldeia: armas, itens, rádio, gravador, tabuleta,
    /// helicóptero e o Selo de Kur. Os FBX vêm do script do Blender `ArteFonte/Objetos/objetos_jogo.py`, já na
    /// escala real, com a frente para +z e o pivô no pé (as armas, na ponta do cano ou da lâmina).
    /// A arte é só desenho: entra como filho "Arte" do placeholder, que só tem o desenho desligado (colisor,
    /// scripts, gatilhos e posição ficam). Nenhum sistema novo; itens sem placeholder ficam só como prefab.
    /// Menus (Nibiru > Arte > Objetos e ambientes): 1. Materiais e prefabs; 2. Armas nos personagens (prefabs);
    /// 3. Trocar na cena aberta; Desfazer na cena aberta. Rode o 3 de novo depois dos menus de conteúdo, que
    /// refazem os placeholders, e o 2 depois dos menus da Elena, do Samir e dos soldados.
    /// </summary>
    static class PropArtSetup
    {
        const string ArtRoot = "Assets/_Nibiru/Art/";
        const string PrefabRoot = "Assets/_Nibiru/Prefabs/";
        const string ArtChild = "Arte";
        const string RotorArtChild = "Arte_Rotor";

        // Nome, pasta (Objetos ou Ambientes), metal e lisura (quando não há máscara do Meshy).
        static readonly (string name, string kind, float metallic, float smoothness)[] Models =
        {
            ("Fuzil", "Objetos", 0.3f, 0.45f), ("Pistola", "Objetos", 0.3f, 0.45f), ("Faca", "Objetos", 0.3f, 0.45f),
            ("Espingarda", "Objetos", 0.2f, 0.4f), ("TabuletaDeJunco", "Objetos", 0f, 0.15f), ("Radio", "Objetos", 0.1f, 0.3f),
            ("Antibioticos", "Objetos", 0f, 0.4f), ("CaixaMunicao", "Objetos", 0f, 0.3f), ("Curativo", "Objetos", 0f, 0.3f),
            ("RoloFilme", "Objetos", 0f, 0.4f), ("Gravador", "Objetos", 0.1f, 0.35f),
            ("Helicoptero", "Ambientes", 0.2f, 0.45f), ("SeloDeKur", "Ambientes", 0f, 0.25f),
        };

        enum Depth { Center, Front }

        // Placeholders simples (o desenho é do próprio objeto): nome (ou "pai/nome"), modelo, giro local (múltiplo de 90 graus, para
        // não entortar com a escala do placeholder) e como alinhar em z. Em x fica centrado, em y pelo pé.
        static readonly (string name, string model, Vector3 euler, Depth depth)[] Simple =
        {
            ("Municao_Poco_Descida", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Municao_Aldeia_Mesquita", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Municao_Passagem", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Municao_Base_Conteiner", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Municao_Lider", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Municao_ChefeSeguranca", "CaixaMunicao", Vector3.zero, Depth.Center),
            ("Curativo_1", "Curativo", Vector3.zero, Depth.Center),
            ("Curativo_2", "Curativo", Vector3.zero, Depth.Center),
            ("Curativo_Boca_Antecamara", "Curativo", Vector3.zero, Depth.Center),
            ("Curativo_Plataforma_Leste", "Curativo", Vector3.zero, Depth.Center),
            ("Rolo_De_Filme_1", "RoloFilme", Vector3.zero, Depth.Center),
            ("Rolo_De_Filme_2", "RoloFilme", Vector3.zero, Depth.Center),
            ("Rolo_De_Filme_3", "RoloFilme", Vector3.zero, Depth.Center),
            ("Antibioticos", "Antibioticos", Vector3.zero, Depth.Center),
            ("Pistola_Makarov", "Pistola", new Vector3(0f, 0f, 90f), Depth.Center),
            ("Faca_De_Campo", "Faca", new Vector3(0f, 0f, 90f), Depth.Center),
            ("Aparelho_Radio", "Radio", Vector3.zero, Depth.Center),
            ("Radio_Mesa", "Radio", Vector3.zero, Depth.Center),
            ("Radio_De_Campanha", "Radio", Vector3.zero, Depth.Center),
            ("Radio_Caido/Aparelho", "Radio", Vector3.zero, Depth.Center),
            ("Tabuleta_Enterrada", "TabuletaDeJunco", new Vector3(-90f, 0f, 0f), Depth.Center),
            ("Porta_Selo_de_Kur", "SeloDeKur", new Vector3(0f, 180f, 0f), Depth.Front),
        };

        // Peças do blockout que somem junto com um placeholder simples (o colisor fica).
        static readonly (string owner, string piece)[] AlsoHidden = { ("Porta_Selo_de_Kur", "Selo_Ouro") };

        static readonly string[] Helicopters = { "Helicoptero", "Helicoptero_Sobrevoo", "Helicoptero_Base", "Helicoptero_Passagem" };

        // O nariz do modelo é mais comprido que o do blockout: o piloto (ContentKit, z = 1,68) vai até o banco da
        // cabine do modelo. O Desfazer volta o lugar do blockout.
        const float PilotZ = 2.68f;
        const float BlockoutPilotZ = 1.68f;

        // ---------- 1. materiais e prefabs ----------

        [MenuItem("Nibiru/Arte/Objetos e ambientes/1. Materiais e prefabs")]
        public static void BuildPrefabs()
        {
            int made = 0;
            foreach (var (name, kind, metallic, smoothness) in Models)
            {
                string folder = $"{ArtRoot}{kind}/{name}/";
                string fbx = folder + name + ".fbx";
                if (!File.Exists(fbx))
                {
                    Debug.LogError($"Objetos e ambientes: falta {fbx} (rode o objetos_jogo.py no Blender).");
                    continue;
                }
                ConfigureModel(fbx);
                var material = BuildMaterial(folder, name, metallic, smoothness);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = name;
                foreach (var r in instance.GetComponentsInChildren<MeshRenderer>())
                    r.sharedMaterial = material;
                string prefabFolder = PrefabRoot + kind;
                if (!AssetDatabase.IsValidFolder(prefabFolder))
                    AssetDatabase.CreateFolder(PrefabRoot.TrimEnd('/'), kind);
                PrefabUtility.SaveAsPrefabAsset(instance, $"{prefabFolder}/{name}.prefab");
                Object.DestroyImmediate(instance);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Objetos e ambientes: {made} prefabs em Prefabs/Objetos e Prefabs/Ambientes.");
        }

        static void ConfigureModel(string path)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            if (mi.materialImportMode == ModelImporterMaterialImportMode.None && mi.bakeAxisConversion && !mi.importAnimation
                && mi.importNormals == ModelImporterNormals.Import && !mi.importCameras && !mi.importLights)
                return;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.bakeAxisConversion = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.SaveAndReimport();
        }

        static Material BuildMaterial(string folder, string name, float metallic, float smoothness)
        {
            string path = folder + "M_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.shaderKeywords = new string[0];
            m.enableInstancing = true;
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", Texture(folder + name + "_cor.jpg", TextureImporterType.Default, true));
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            var normal = Texture(folder + name + "_normal.png", TextureImporterType.NormalMap, false);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            // Máscara do Meshy (R metal, A lisura): a lisura do material multiplica a da máscara.
            var mask = Texture(folder + name + "_mascara.png", TextureImporterType.Default, false);
            if (mask != null)
            {
                m.SetTexture("_MetallicGlossMap", mask);
                m.SetFloat("_SmoothnessTextureChannel", 0f);
                m.SetFloat("_Smoothness", 1f);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D Texture(string path, TextureImporterType type, bool srgb)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
                return null;
            if (ti.textureType != type || ti.sRGBTexture != srgb || ti.anisoLevel != 4)
            {
                ti.textureType = type;
                ti.sRGBTexture = srgb;
                ti.anisoLevel = 4;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static GameObject Prefab(string name)
        {
            var info = Models.First(x => x.name == name);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}{info.kind}/{name}.prefab");
            if (prefab == null)
                Debug.LogError($"Objetos e ambientes: falta o prefab {name} (rode o menu 1).");
            return prefab;
        }

        // ---------- 2. armas nos personagens ----------

        [MenuItem("Nibiru/Arte/Objetos e ambientes/2. Armas nos personagens (prefabs)")]
        public static void BuildWeapons()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Objetos e ambientes: saia do Play antes.");
                return;
            }
            var log = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot + "Personagens", PrefabRoot + "Player" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var t in root.GetComponentsInChildren<Transform>(true).ToArray())
                {
                    // Só o que é deste prefab (o fuzil dos prefabs da IA vem de dentro do prefab do personagem).
                    if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                        continue;
                    string model = t.name switch
                    {
                        "Fuzil" => "Fuzil",
                        "ArmaNaMao" => "Pistola",
                        "Faca" or "PivoFaca" => "Faca",
                        _ => null,
                    };
                    if (model == null)
                        continue;
                    // A ponta do modelo vai no "Cano" do blockout, ou na ponta da "Lamina".
                    var muzzle = t.Find("Cano");
                    var blade = t.Find("Lamina");
                    Vector3 tip;
                    if (muzzle != null)
                        tip = muzzle.localPosition;
                    else if (blade != null)
                        tip = blade.localPosition + blade.localRotation * new Vector3(0f, 0f, blade.localScale.z / 2f);
                    else
                        continue;
                    var art = Attach(t, model);
                    if (art == null)
                        continue;
                    art.transform.localPosition = tip;
                    HideBlockout(t);
                    changed = true;
                    log.Add($"{Path.GetFileNameWithoutExtension(path)}/{t.name}");
                }
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"Objetos e ambientes: armas trocadas em {log.Count} lugares: {string.Join(", ", log)}.");
        }

        // ---------- 3. cena ----------

        [MenuItem("Nibiru/Arte/Objetos e ambientes/3. Trocar na cena aberta")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Objetos e ambientes: saia do Play antes.");
                return;
            }
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene.IsValid()).ToArray();
            int count = 0;
            var missing = new List<string>();
            foreach (var (name, model, euler, depth) in Simple)
            {
                string own = name.Contains('/') ? name.Substring(name.IndexOf('/') + 1) : name;
                string parent = name.Contains('/') ? name.Substring(0, name.IndexOf('/')) : null;
                var found = all.Where(t => t != null && t.name == own && t.GetComponent<MeshFilter>() != null
                                           && (parent == null || (t.parent != null && t.parent.name == parent))).ToArray();
                if (found.Length == 0)
                    missing.Add(name);
                foreach (var t in found)
                {
                    var art = Attach(t, model);
                    if (art == null)
                        continue;
                    art.transform.localRotation = Quaternion.Euler(euler);
                    art.transform.localScale = Unscaled(t, art.transform.localRotation);
                    Align(t, art, t.GetComponent<MeshFilter>().sharedMesh.bounds, depth);
                    t.GetComponent<MeshRenderer>().enabled = false;
                    foreach (var (owner, piece) in AlsoHidden.Where(x => x.owner == name))
                    {
                        var r = all.FirstOrDefault(x => x != null && x.name == piece)?.GetComponent<MeshRenderer>();
                        if (r != null) r.enabled = false;
                    }
                    count++;
                }
            }

            // A tabuleta do nicho é um grupo (pedra e linhas de ouro): a arte fica no centro da pedra.
            foreach (var group in all.Where(t => t != null && t.name == "Tabuleta_NoNicho"))
            {
                var stone = group.Find("Pedra");
                if (stone == null)
                    continue;
                var art = Attach(group, "TabuletaDeJunco");
                if (art == null)
                    continue;
                Align(group, art, LocalBounds(group, stone.GetComponent<MeshFilter>()), Depth.Center);
                HideBlockout(group);
                count++;
            }

            foreach (var heli in all.Where(t => t != null && Helicopters.Contains(t.name) && t.Find("Rotor") != null))
            {
                PlaceHelicopter(heli);
                count++;
            }

            if (missing.Count > 0)
                Debug.LogWarning($"Objetos e ambientes: não achei na cena: {string.Join(", ", missing)}.");
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Objetos e ambientes: {count} placeholders com arte. Salve a cena.");
        }

        // O corpo fica com o eixo do rotor no eixo do Rotor do blockout; as pás vão para dentro do Rotor (o Spinner
        // dele gira as pás). O piloto e os colisores do blockout ficam.
        static void PlaceHelicopter(Transform heli)
        {
            var rotor = heli.Find("Rotor");
            var art = Attach(heli, "Helicoptero");
            if (art == null)
                return;
            PrefabUtility.UnpackPrefabInstance(art, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            art.transform.localPosition = new Vector3(rotor.localPosition.x, 0f, rotor.localPosition.z);
            var blades = art.transform.Cast<Transform>().FirstOrDefault(c => c.name.EndsWith("_RotorPrincipal"));
            var old = rotor.Find(RotorArtChild);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            if (blades != null)
            {
                blades.SetParent(rotor, true);
                blades.name = RotorArtChild;
            }
            SetPilotZ(heli, PilotZ);
            HideBlockout(heli);
        }

        [MenuItem("Nibiru/Arte/Objetos e ambientes/Desfazer na cena aberta (volta o blockout)")]
        static void Restore()
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene.IsValid()).ToArray();
            int count = 0;
            foreach (var t in all.Where(t => t != null && (t.name == ArtChild || t.name == RotorArtChild)).ToArray())
            {
                var owner = t.parent;
                Object.DestroyImmediate(t.gameObject);
                if (owner == null)
                    continue;
                if (owner.name == "Rotor")
                    continue;
                if (Helicopters.Contains(owner.name))
                    SetPilotZ(owner, BlockoutPilotZ);
                foreach (var r in owner.GetComponentsInChildren<MeshRenderer>(true))
                    r.enabled = true;
                foreach (var (o, piece) in AlsoHidden.Where(x => x.owner == owner.name))
                {
                    var r = all.FirstOrDefault(x => x != null && x.name == piece)?.GetComponent<MeshRenderer>();
                    if (r != null) r.enabled = true;
                }
                count++;
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"Objetos e ambientes: {count} placeholders de volta ao blockout. Salve a cena.");
        }

        // ---------- utilidades ----------

        static void SetPilotZ(Transform heli, float z)
        {
            var pilot = heli.Find("Piloto");
            if (pilot != null)
                pilot.localPosition = new Vector3(pilot.localPosition.x, pilot.localPosition.y, z);
        }

        static GameObject Attach(Transform parent, string model)
        {
            var prefab = Prefab(model);
            if (prefab == null)
                return null;
            var old = parent.Find(ArtChild);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
            art.name = ArtChild;
            art.transform.SetParent(parent, false);
            art.transform.localPosition = Vector3.zero;
            art.transform.localRotation = Quaternion.identity;
            art.transform.localScale = Unscaled(parent, Quaternion.identity);
            return art;
        }

        /// <summary>Escala local que anula a do pai, para a arte ficar no tamanho real (giro em múltiplos de 90 graus).</summary>
        static Vector3 Unscaled(Transform parent, Quaternion localRotation)
        {
            var s = parent.lossyScale;
            var result = Vector3.one;
            for (int i = 0; i < 3; i++)
            {
                var axis = Vector3.zero;
                axis[i] = 1f;
                var v = localRotation * axis;
                int j = Mathf.Abs(v.x) > 0.5f ? 0 : Mathf.Abs(v.y) > 0.5f ? 1 : 2;
                result[i] = 1f / s[j];
            }
            return result;
        }

        /// <summary>Encosta a arte no placeholder: centro em x, pé em y, centro ou frente (-z) em z.</summary>
        static void Align(Transform parent, GameObject art, Bounds reference, Depth depth)
        {
            var b = new Bounds();
            bool first = true;
            foreach (var mf in art.GetComponentsInChildren<MeshFilter>())
            {
                var lb = LocalBounds(parent, mf);
                if (first) { b = lb; first = false; }
                else b.Encapsulate(lb);
            }
            var offset = new Vector3(
                reference.center.x - b.center.x,
                reference.min.y - b.min.y,
                depth == Depth.Front ? reference.min.z - b.min.z : reference.center.z - b.center.z);
            art.transform.localPosition += offset;
        }

        static Bounds LocalBounds(Transform parent, MeshFilter mf)
        {
            var mb = mf.sharedMesh.bounds;
            var b = new Bounds();
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = parent.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (i == 0) b = new Bounds(p, Vector3.zero);
                else b.Encapsulate(p);
            }
            return b;
        }

        /// <summary>Desliga o desenho do blockout dentro do grupo (fora a arte e o piloto); colisores ficam.</summary>
        static void HideBlockout(Transform group)
        {
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentsInParent<Transform>(true).Any(p => p.name == ArtChild || p.name == RotorArtChild || p.name == "Piloto"))
                    continue;
                r.enabled = false;
            }
        }
    }
}
