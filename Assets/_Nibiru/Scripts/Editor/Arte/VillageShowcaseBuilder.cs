using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using M = Nibiru.EditorTools.VillageShowcaseMaterials;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Monta a área-vitrine da Aldeia (casa do altar e frente do pátio) no grupo `Arte_AldeiaVitrine` da cena aberta.
    /// Os FBX vêm do kit do Blender (`ArteFonte/Ambientes/SitioDeUr/Aldeia/Blender/kit_casas_barro.py`), já em
    /// coordenadas do mundo: cada um entra na origem, e cada peça recebe o material pelo sufixo do nome ("__Reboco").
    /// A arte não tem colisor: as caixas do blockout que ela substitui só têm o desenho desligado (colisores, malha
    /// de navegação, gatilhos e personagens ficam como estão). Rode depois do Blockout e do menu da Aldeia, que
    /// refazem esses grupos. Pode rodar de novo: apaga o grupo e refaz.
    /// Menus: Nibiru > Arte > Vitrine da Aldeia > 2. Montar vitrine na cena aberta / Desfazer (volta o blockout).
    /// </summary>
    static class VillageShowcaseBuilder
    {
        public const string RootName = "Arte_AldeiaVitrine";
        const string ModelFolder = M.Root + "Modelos/";

        static readonly string[] Models = { "Kit_CasaAltar", "Kit_Casa6", "Kit_CasasFundo", "Kit_ChaoAldeia" };

        // Caixas do blockout cujo desenho a arte substitui (o colisor fica). Casa_1 a 5 são as do fundo dos
        // enquadramentos; Casa_6 é a vizinha a leste da casa do altar.
        static readonly string[] Replaced =
        {
            "CasaAltar_Fundo", "CasaAltar_Oeste", "CasaAltar_Leste", "CasaAltar_Teto",
            "Casa_1", "Porta_1", "Casa_2", "Porta_2", "Casa_3", "Porta_3", "Casa_4", "Porta_4",
            "Casa_5", "Porta_5", "Casa_6", "Porta_6",
        };

        // Peças que só trocam de material: a esteira do Yusuf e as palmeiras do pátio, que seguem em blockout,
        // mas com cores discretas (palmeira de verdade pede um pacote).
        static readonly (string name, string material)[] Reskinned =
        {
            ("Esteira_Yusuf", M.Mat),
            ("Palmeira_1_Tronco", M.PalmTrunk), ("Palmeira_1_Copa", M.PalmCrown),
            ("Palmeira_2_Tronco", M.PalmTrunk), ("Palmeira_2_Copa", M.PalmCrown),
            ("Palmeira_3_Tronco", M.PalmTrunk), ("Palmeira_3_Copa", M.PalmCrown),
            ("Palmeira_4_Tronco", M.PalmTrunk), ("Palmeira_4_Copa", M.PalmCrown),
        };

        // Os discos "Marca_" do blockout dentro deste retângulo (x mín, z mín, x máx, z máx) somem.
        static readonly Rect Area = Rect.MinMaxRect(-58f, -34f, -36f, -12f);

        static readonly Dictionary<string, string> Suffix = new Dictionary<string, string>
        {
            { "Reboco", M.Plaster }, { "RebocoClaro", M.PlasterLight }, { "Madeira", M.Wood }, { "Metal", M.Metal },
            { "Esteira", M.Mat }, { "Piso", M.Floor }, { "Tijolo", M.Brick }, { "Chao", M.Ground },
        };

        [MenuItem("Nibiru/Arte/Vitrine da Aldeia/2. Montar vitrine na cena aberta")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Vitrine da Aldeia: saia do Play antes de montar.");
                return;
            }
            if (M.Get(M.Plaster) == null)
                M.Build();

            var old = GameObject.Find(RootName);
            if (old != null)
                Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            foreach (var model in Models)
                Place(model, root.transform);

            int hidden = SetBlockoutVisible(false);
            foreach (var (name, material) in Reskinned)
            {
                var r = Find(name)?.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = M.Get(material);
                else
                    Debug.LogWarning($"Vitrine da Aldeia: não achei \"{name}\" na cena (rode o menu da Aldeia antes).");
            }
            VillageShowcaseLighting.Build(root.transform);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Vitrine da Aldeia: montada ({Models.Length} modelos, {hidden} caixas do blockout escondidas). Salve a cena.");
        }

        [MenuItem("Nibiru/Arte/Vitrine da Aldeia/Desfazer (volta o blockout)")]
        static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null)
                Object.DestroyImmediate(old);
            SetBlockoutVisible(true);
            Debug.Log("Vitrine da Aldeia: desfeita. Para a esteira do Yusuf e as palmeiras voltarem ao material do blockout, rode o Blockout e o menu da Aldeia.");
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static void Place(string model, Transform root)
        {
            string path = ModelFolder + model + ".fbx";
            ConfigureModel(path);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError($"Vitrine da Aldeia: falta {path} (rode o kit do Blender).");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, root);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = model;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                // Um FBX com uma peça só vira a malha na raiz, com o nome do arquivo: aí o sufixo vem do nome da malha.
                string piece = r.name.Contains("__") ? r.name : r.GetComponent<MeshFilter>().sharedMesh.name;
                string suffix = piece.Contains("__") ? piece.Substring(piece.LastIndexOf("__") + 2) : "";
                if (!Suffix.TryGetValue(suffix, out var mat))
                {
                    Debug.LogWarning($"Vitrine da Aldeia: peça sem material conhecido: {r.name}");
                    continue;
                }
                r.sharedMaterial = M.Get(mat);
                r.shadowCastingMode = suffix == "Tijolo" ? ShadowCastingMode.Off : ShadowCastingMode.On;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
            }
        }

        static void ConfigureModel(string path)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null)
                return;
            bool changed = mi.materialImportMode != ModelImporterMaterialImportMode.None || !mi.bakeAxisConversion
                || mi.importNormals != ModelImporterNormals.Calculate || mi.importAnimation || mi.importCameras || mi.importLights;
            if (!changed)
                return;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.bakeAxisConversion = true;
            mi.importNormals = ModelImporterNormals.Calculate;
            mi.normalSmoothingAngle = 40f;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.SaveAndReimport();
        }

        /// <summary>Liga ou desliga o desenho das caixas substituídas e dos discos "Marca_" da área. Devolve quantos mudou.</summary>
        static int SetBlockoutVisible(bool visible)
        {
            int count = 0;
            foreach (var name in Replaced)
            {
                var r = Find(name)?.GetComponent<Renderer>();
                if (r == null)
                {
                    Debug.LogWarning($"Vitrine da Aldeia: não achei a caixa \"{name}\" do blockout.");
                    continue;
                }
                r.enabled = visible;
                count++;
            }
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(r => r.name.StartsWith("Marca_")))
            {
                var p = r.transform.position;
                if (Area.Contains(new Vector2(p.x, p.z)))
                {
                    r.enabled = visible;
                    count++;
                }
            }
            return count;
        }

        static GameObject Find(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && t.gameObject.scene.IsValid())
                    return t.gameObject;
            return null;
        }
    }
}
