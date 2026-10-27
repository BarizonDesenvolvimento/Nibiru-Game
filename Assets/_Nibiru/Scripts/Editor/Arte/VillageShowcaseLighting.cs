using System.IO;
using Nibiru.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using M = Nibiru.EditorTools.VillageShowcaseMaterials;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Luz da área-vitrine da Aldeia, montada junto com a arte (menu 2), no grupo `Arte_AldeiaVitrine/Luz`:
    /// - rebote do pátio: um spot quente e sem sombra na frente aberta da casa do altar, no lugar da luz que o chão
    ///   ensolarado do pátio jogaria para dentro (o URP não calcula esse rebote em tempo real);
    /// - Volume de pós-processamento só perto da vitrine (<see cref="AreaVolume"/>, sem colisor): bloom leve, tom
    ///   quente, contraste e saturação, sombras levemente quentes e vinheta suave. O tonemapping não muda;
    /// - sonda de reflexo fixa dentro da casa do altar (o metal e o vidro não refletem o céu lá dentro).
    /// A claraboia é geometria do kit: o sol de verdade entra por ela.
    /// O céu procedural vale para a região inteira e tem menu próprio (3), que também liga o pós-processamento na
    /// câmera do prefab da Elena (sem nenhum Volume por perto, nada muda na imagem das outras áreas).
    /// </summary>
    static class VillageShowcaseLighting
    {
        const string LightFolder = M.Root + "Luz/";
        const string ProfilePath = LightFolder + "VP_AldeiaVitrine.asset";
        const string SkyPath = "Assets/_Nibiru/Art/Ambientes/SitioDeUr/Ceu_Procedural.mat";
        const string ElenaPrefab = "Assets/_Nibiru/Prefabs/Player/Elena.prefab";

        /// <summary>Caixa em que o Volume vale inteiro (pátio da frente e casa do altar).</summary>
        public static readonly Bounds Area = new Bounds(new Vector3(-47f, 4f, -24f), new Vector3(24f, 12f, 24f));

        public static void Build(Transform root)
        {
            Directory.CreateDirectory(LightFolder);
            var group = new GameObject("Luz").transform;
            group.SetParent(root, false);

            var bounce = new GameObject("Luz_Rebote_Patio").AddComponent<Light>();
            bounce.transform.SetParent(group, false);
            bounce.transform.position = new Vector3(-47f, 2.6f, -24.5f);
            bounce.transform.LookAt(new Vector3(-47f, 0.0f, -29.5f));
            bounce.type = LightType.Spot;
            bounce.color = new Color(1.0f, 0.80f, 0.58f);
            bounce.intensity = 3f;
            bounce.range = 10f;
            bounce.spotAngle = 95f;
            bounce.innerSpotAngle = 55f;
            bounce.shadows = LightShadows.None;

            // Preenchimento quente dentro da casa: o ambiente do céu ilumina por igual o que está coberto e deixaria o
            // interior azulado; lá dentro, a luz que chega é a do pátio ensolarado.
            var fill = new GameObject("Luz_Preenchimento_Interior").AddComponent<Light>();
            fill.transform.SetParent(group, false);
            fill.transform.position = new Vector3(-47f, 2.3f, -28.6f);
            fill.type = LightType.Point;
            fill.color = new Color(1.0f, 0.80f, 0.60f);
            fill.intensity = 0.9f;
            fill.range = 5.5f;
            fill.shadows = LightShadows.None;

            var volume = new GameObject("Volume_AldeiaVitrine").AddComponent<Volume>();
            volume.transform.SetParent(group, false);
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = Profile();
            volume.gameObject.AddComponent<AreaVolume>().Configure(Area, 8f);

            var probe = new GameObject("Sonda_CasaAltar").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(group, false);
            probe.transform.position = new Vector3(-47f, 1.75f, -28.75f);
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.size = new Vector3(5.2f, 3.6f, 5.8f);
            probe.resolution = 128;
            probe.importance = 2;
            BakeProbe(probe);
        }

        static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.65f);

            var color = Get<ColorAdjustments>(profile);
            color.postExposure.Override(0.1f);
            color.contrast.Override(10f);
            color.saturation.Override(10f);

            var balance = Get<WhiteBalance>(profile);
            balance.temperature.Override(10f);

            var smh = Get<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(1.07f, 0.99f, 0.88f, 0f));
            smh.highlights.Override(new Vector4(1.0f, 0.99f, 0.96f, 0f));

            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.4f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            component.active = true;
            return component;
        }

        static void BakeProbe(ReflectionProbe probe)
        {
            var scene = probe.gameObject.scene;
            string folder = Path.ChangeExtension(scene.path, null);
            Directory.CreateDirectory(folder);
            string path = folder + "/Sonda_CasaAltar.exr";
            if (Lightmapping.BakeReflectionProbe(probe, path))
            {
                AssetDatabase.ImportAsset(path);
                probe.bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            }
            else
                Debug.LogWarning("Vitrine da Aldeia: a sonda de reflexo não foi gravada.");
        }

        [MenuItem("Nibiru/Arte/Vitrine da Aldeia/3. Céu procedural (região inteira) e pós na câmera da Elena")]
        public static void Sky()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Vitrine da Aldeia: saia do Play antes de trocar o céu.");
                return;
            }
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, SkyPath);
            }
            // Céu azul intenso, sol pequeno e duro, horizonte com poeira cor de areia.
            sky.SetFloat("_SunDisk", 2f);
            sky.SetFloat("_SunSize", 0.035f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.46f, 0.57f, 0.76f));
            sky.SetColor("_GroundColor", new Color(0.74f, 0.62f, 0.46f));
            sky.SetFloat("_Exposure", 1.2f);
            EditorUtility.SetDirty(sky);
            AssetDatabase.SaveAssets();

            RenderSettings.skybox = sky;
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in sun)
                if (l.type == LightType.Directional && l.name == "Directional Light")
                    RenderSettings.sun = l;
            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            var contents = PrefabUtility.LoadPrefabContents(ElenaPrefab);
            try
            {
                foreach (var cam in contents.GetComponentsInChildren<Camera>(true))
                    cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                PrefabUtility.SaveAsPrefabAsset(contents, ElenaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            Debug.Log("Vitrine da Aldeia: céu procedural aplicado à região e pós-processamento ligado na câmera da Elena. Salve a cena.");
        }
    }
}
