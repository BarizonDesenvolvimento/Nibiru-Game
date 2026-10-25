using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Ferramentas comuns para montar os personagens que vêm do Meshy + AccuRig com animações do
    /// Mixamo: texturas e material URP, importação Humanoid na altura certa e cortes de animação.
    /// Usado pelo Samir e pelos próximos personagens (a Elena tem as mesmas regras em ElenaSetup).
    /// </summary>
    static class CharacterSetupKit
    {
        public const string Mixamo = "Assets/_Nibiru/Art/Animacoes/Mixamo/";
        // Boneco do Mixamo em pose T: o esqueleto de referência de todas as animações do Mixamo.
        const string MixamoReferencePath = Mixamo + "X Bot.fbx";

        /// <summary>
        /// Um trecho de animação. Quadros contados como no Mixamo e no Blender (o primeiro é 1); -1 = clipe inteiro.
        /// feet: a altura vem dos pés. mirror: espelha esquerda e direita. original: o giro segue o
        /// arquivo ("Based Upon: Original"), e não a direção do corpo.
        /// </summary>
        public readonly struct Cut
        {
            public readonly string name, file;
            public readonly int first, last;
            public readonly bool loop, feet, mirror, original;

            public Cut(string name, string file, int first, int last, bool loop, bool feet = false, bool mirror = false, bool original = false)
            {
                this.name = name; this.file = Mixamo + file + ".fbx";
                this.first = first; this.last = last; this.loop = loop;
                this.feet = feet; this.mirror = mirror; this.original = original;
            }
        }

        // ---------- Texturas e material ----------

        public static void SetTexture(string path, TextureImporterType type, bool srgb)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType == type && importer.sRGBTexture == srgb)
                return;
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// O Lit do URP lê metal no canal R e lisura no A, num mapa só: A = 1 - rugosidade.
        /// Gerado só se ainda não existir; para refazer, apague o arquivo.
        /// </summary>
        public static void BuildMetalSmooth(string metalPath, string roughPath, string outputPath)
        {
            if (File.Exists(outputPath)) return;
            var metal = LoadRaw(metalPath);
            var rough = LoadRaw(roughPath);
            if (rough.width != metal.width || rough.height != metal.height)
                rough = Resize(rough, metal.width, metal.height);
            var m = metal.GetPixels32();
            var r = rough.GetPixels32();
            var result = new Color32[m.Length];
            for (int i = 0; i < m.Length; i++)
                result[i] = new Color32(m[i].r, m[i].r, m[i].r, (byte)(255 - r[i].r));
            var output = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            output.SetPixels32(result);
            File.WriteAllBytes(outputPath, output.EncodeToPNG());
            Object.DestroyImmediate(metal);
            Object.DestroyImmediate(rough);
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(outputPath);
        }

        // Lê o PNG direto do disco, sem conversão de cor e sem mexer na importação.
        static Texture2D LoadRaw(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        static Texture2D Resize(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(source);
            return result;
        }

        /// <summary>
        /// Material URP Lit com cor, normal e metal/lisura, de dois lados (o AccuRig marca assim:
        /// mangas e gola mostram o avesso). Criado se não existir; senão, só atualizado.
        /// </summary>
        public static Material LitMaterial(string materialPath, string baseColor, string normal, string metalSmooth)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(materialPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metalSmooth));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- Importação ----------

        /// <summary>
        /// Modelo do AccuRig como Humanoid (com dedos), na altura da tabela de CharacterHeights, com o
        /// material no lugar dos que vêm no FBX. Devolve o avatar.
        /// </summary>
        public static Avatar ConfigureModel(string modelPath, string character, Material material)
        {
            // A escala vem antes do avatar: o Humanoid guarda o tamanho dos ossos e é refeito do zero
            // a cada execução (senão ele fica com as medidas de antes do ajuste).
            CharacterHeights.FitImportScale(modelPath, character);
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.humanDescription = new HumanDescription();
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            foreach (var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(modelPath).GetComponentsInChildren<Renderer>())
                foreach (var original in renderer.sharedMaterials.Where(m => m != null && m != material))
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), original.name), material);
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            int bones = avatar != null ? importer.humanDescription.human.Length : 0;
            Debug.Log($"Avatar de {character}: {(avatar != null && avatar.isValid && avatar.isHuman ? "Humanoid válido" : "PROBLEMA no Humanoid")}, {bones} ossos mapeados.");
            return avatar;
        }

        /// <summary>
        /// Configura os cortes num arquivo do Mixamo (Humanoid, com o avatar do X Bot em pose T).
        /// Só troca os cortes com estes nomes: os de outros personagens no mesmo arquivo ficam.
        /// </summary>
        public static void ConfigureClips(string path, IEnumerable<Cut> cuts)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = MixamoAvatar();
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;

            var source = importer.defaultClipAnimations[0];
            // O Unity conta os quadros a partir do primeiro do take; nos cortes, o primeiro é 1.
            float offset = source.firstFrame - 1f;
            var mine = cuts.Where(c => c.file == path).ToArray();
            var clips = importer.clipAnimations.Where(c => mine.All(cut => cut.name != c.name)).ToList();
            foreach (var cut in mine)
            {
                var clip = new ModelImporterClipAnimation
                {
                    name = cut.name,
                    takeName = source.takeName,
                    firstFrame = cut.first >= 0 ? cut.first + offset : source.firstFrame,
                    lastFrame = cut.last >= 0 ? cut.last + offset : source.lastFrame,
                };
                // O avanço sai para a raiz (e é descartado: quem anda é o jogo); altura e giro ficam na pose.
                clip.loopTime = cut.loop;
                clip.mirror = cut.mirror;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = cut.original;
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = !cut.feet;
                clip.heightFromFeet = cut.feet;
                clips.Add(clip);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
        }

        // As animações do Mixamo baixadas sem pele não têm pose T: o avatar feito delas mesmas parte do
        // primeiro quadro e entorta a animação inteira. Todas usam o avatar do X Bot.
        static Avatar MixamoAvatar()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(MixamoReferencePath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null && avatar.isHuman) return avatar;
            var importer = (ModelImporter)AssetImporter.GetAtPath(MixamoReferencePath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(MixamoReferencePath).OfType<Avatar>().First();
        }

        /// <summary>Um corte já importado, pelo arquivo e pelo nome (null se não existir).</summary>
        public static AnimationClip Clip(string file, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(Mixamo + file + ".fbx").OfType<AnimationClip>().FirstOrDefault(c => c.name == name);

        /// <summary>Velocidade (m/s) em que os pés não escorregam: o avanço médio do clipe.</summary>
        public static float NaturalSpeed(AnimationClip clip) => clip.averageSpeed.magnitude;

        /// <summary>
        /// Cada animação entra na velocidade em que deve tocar. Abaixo da velocidade natural, ela
        /// desacelera junto; acima, acelera só em parte (raiz quadrada): passos rápidos demais
        /// parecem mais errados que um pouco de escorregão.
        /// </summary>
        public static void Speeds(BlendTree tree, string parameter, params (AnimationClip clip, float natural, float speed)[] entries)
        {
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = parameter;
            tree.useAutomaticThresholds = false;
            foreach (var (clip, _, speed) in entries)
                tree.AddChild(clip, speed);
            var children = tree.children;
            for (int i = 0; i < children.Length; i++)
            {
                var (_, natural, speed) = entries[i];
                float ratio = natural > 0f && speed > 0f ? speed / natural : 1f;
                children[i].timeScale = ratio > 1f ? Mathf.Clamp(Mathf.Sqrt(ratio), 1f, 2.5f) : ratio;
            }
            tree.children = children;
        }
    }
}
