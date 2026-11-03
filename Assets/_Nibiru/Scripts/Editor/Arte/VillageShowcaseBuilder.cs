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

        // Objetos do Tripo (`Objetos/`), com o pivô no centro do pé, no lugar das caixas do blockout (medidas iguais
        // às delas). A lamparina não está aqui: ela vai para baixo da chama do nicho (veja PlaceLamp).
        static readonly (string model, Vector3 position, float yaw)[] Objects =
        {
            ("Altar_M1", new Vector3(-47f, 0f, -31f), 0f),
            ("Poco_M9", new Vector3(-55f, 0f, -22f), 0f),
        };

        // Altura do pavio da lamparina do Tripo (impressa pelo script do Blender) e a da chama do blockout
        // (VillageContent.NicheMark), que fica acima dele: a vitrine desce a chama até o pavio. Lamparina de 22 cm.
        const float WickHeight = 0.055f;
        const float BlockoutFlameHeight = 0.09f;
        const float FlameHalfHeight = 0.025f;

        // Caixas do blockout cujo desenho a arte substitui (o colisor fica). Casa_1 a 5 são as do fundo dos
        // enquadramentos; Casa_6 é a vizinha a leste da casa do altar; Nicho_Guardioes é o altar e Poco_Agua, o poço.
        static readonly string[] Replaced =
        {
            "CasaAltar_Fundo", "CasaAltar_Oeste", "CasaAltar_Leste", "CasaAltar_Teto",
            "Casa_1", "Porta_1", "Casa_2", "Porta_2", "Casa_3", "Porta_3", "Casa_4", "Porta_4",
            "Casa_5", "Porta_5", "Casa_6", "Porta_6", "Nicho_Guardioes", "Poco_Agua",
        };

        // Peças da lamparina do blockout em cima do nicho (Marca_Guardioes/Lamparina); a chama fica.
        static readonly string[] LampPieces = { "Pires", "Bico" };

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
            { "Altar", M.Altar }, { "Poco", M.Well }, { "Lamparina", M.Lamp }, { "TijoloPoco", M.WellBrick },
            { "Agua", M.WellWater },
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
            else if (M.Get(M.Altar) == null)
                M.BuildObjects();

            var old = GameObject.Find(RootName);
            if (old != null)
                Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            foreach (var model in Models)
                Place(model, root.transform);
            var objects = new GameObject("Objetos").transform;
            objects.SetParent(root.transform, false);
            foreach (var (model, position, yaw) in Objects)
                Place(model, objects, M.ObjectFolder, false)?.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            PlaceLamp(objects);

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
            Debug.Log($"Vitrine da Aldeia: montada ({Models.Length} modelos, {Objects.Length + 1} objetos, {hidden} caixas do blockout escondidas). Salve a cena.");
        }

        [MenuItem("Nibiru/Arte/Vitrine da Aldeia/Desfazer (volta o blockout)")]
        static void Restore()
        {
            var old = GameObject.Find(RootName);
            if (old != null)
                Object.DestroyImmediate(old);
            SetBlockoutVisible(true);
            var flame = FindLampGroup()?.Find("Chama");
            if (flame != null)
                flame.localPosition = new Vector3(flame.localPosition.x, BlockoutFlameHeight, flame.localPosition.z);
            Debug.Log("Vitrine da Aldeia: desfeita. Para a esteira do Yusuf e as palmeiras voltarem ao material do blockout, rode o Blockout e o menu da Aldeia.");
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // A lamparina vai em cima do nicho, com o pavio embaixo da chama do blockout; a chama desce até o pavio.
        static void PlaceLamp(Transform parent)
        {
            var group = FindLampGroup();
            var flame = group != null ? group.Find("Chama") : null;
            if (flame == null)
            {
                Debug.LogWarning("Vitrine da Aldeia: não achei Marca_Guardioes/Lamparina/Chama (rode o menu da Aldeia antes).");
                return;
            }
            var lamp = Place("Lamparina", parent, M.ObjectFolder, false);
            if (lamp == null)
                return;
            var wick = flame.position;
            lamp.transform.SetPositionAndRotation(new Vector3(wick.x, group.position.y, wick.z), group.rotation);
            flame.localPosition = new Vector3(flame.localPosition.x, WickHeight + FlameHalfHeight, flame.localPosition.z);
        }

        static Transform FindLampGroup() => Find("Marca_Guardioes")?.transform.Find("Lamparina");

        static GameObject Place(string model, Transform root, string folder = ModelFolder, bool calculateNormals = true)
        {
            string path = folder + model + ".fbx";
            ConfigureModel(path, calculateNormals);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError($"Vitrine da Aldeia: falta {path} (rode o kit do Blender ou o objetos_vitrine.py).");
                return null;
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
            return go;
        }

        // As peças do kit recalculam as normais (40 graus); os objetos do Tripo trazem as suas, mais finas.
        static void ConfigureModel(string path, bool calculateNormals)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null)
                return;
            var normals = calculateNormals ? ModelImporterNormals.Calculate : ModelImporterNormals.Import;
            bool changed = mi.materialImportMode != ModelImporterMaterialImportMode.None || !mi.bakeAxisConversion
                || mi.importNormals != normals || mi.importAnimation || mi.importCameras || mi.importLights;
            if (!changed)
                return;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.bakeAxisConversion = true;
            mi.importNormals = normals;
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
            var lamp = FindLampGroup();
            foreach (var name in LampPieces)
            {
                var r = lamp?.Find(name)?.GetComponent<Renderer>();
                if (r == null)
                {
                    Debug.LogWarning($"Vitrine da Aldeia: não achei a peça \"{name}\" da lamparina do blockout.");
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
