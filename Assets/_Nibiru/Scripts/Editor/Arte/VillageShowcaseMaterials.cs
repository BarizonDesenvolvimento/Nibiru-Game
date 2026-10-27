using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Materiais da área-vitrine da Aldeia (casa do altar e frente do pátio, B1v Parte 3).
    /// Texturas CC0 do Poly Haven em Art/Ambientes/SitioDeUr/Aldeia/Texturas/&lt;id&gt; (cor e normal, como baixadas);
    /// a máscara do URP (R = metal, G = oclusão, A = lisura) é montada a partir do "arm" de 1K que fica na
    /// ArteFonte. Também gera as texturas próprias: tapete kilim, esteira de junco, metal pintado (cinza, tingido
    /// de turquesa no material) e as manchas de reboco descascado (tijolo com recorte irregular).
    /// As malhas da vitrine usam UV em metros (1 unidade de UV = 1 m); a escala de cada textura fica no material.
    /// Pode rodar de novo: refaz as texturas geradas e atualiza os materiais.
    /// Menu: Nibiru > Arte > Vitrine da Aldeia > 1. Criar materiais.
    /// </summary>
    static class VillageShowcaseMaterials
    {
        public const string Root = "Assets/_Nibiru/Art/Ambientes/SitioDeUr/Aldeia/";
        public const string TextureFolder = Root + "Texturas/";
        public const string MaterialFolder = Root + "Materiais/";
        const string SourceFolder = "ArteFonte/Ambientes/SitioDeUr/Aldeia/Texturas/";

        public const string Plaster = "M_Reboco_Ocre";
        public const string PlasterLight = "M_Reboco_Claro";
        public const string Ground = "M_Chao_Patio";
        public const string Brick = "M_Tijolo_Descascado";
        public const string Floor = "M_Piso_Barro";
        public const string Wood = "M_Madeira_Velha";
        public const string Metal = "M_Metal_Turquesa";
        public const string Kilim = "M_Tapete_Kilim";
        public const string Mat = "M_Esteira_Junco";
        public const string Gold = "M_Ouro_Gasto";
        public const string Flame = "M_Chama";
        public const string PalmTrunk = "M_Palmeira_Tronco_Discreta";
        public const string PalmCrown = "M_Palmeira_Copa_Discreta";

        // id do Poly Haven, resolução baixada, metros cobertos por uma repetição da textura.
        static readonly (string id, string res, float meters)[] Scanned =
        {
            ("patterned_clay_plaster", "2k", 1.6f),
            ("dense_sand", "2k", 3f),
            ("slumped_mortar_brick", "1k", 1.5f),
            ("clay_floor_001", "1k", 2f),
            ("rough_wood", "1k", 1f),
            ("blue_metal_plate", "1k", 1f),
        };

        public static Material Get(string name) => AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + name + ".mat");

        [MenuItem("Nibiru/Arte/Vitrine da Aldeia/1. Criar materiais")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Vitrine da Aldeia: saia do Play antes de criar os materiais.");
                return;
            }
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(TextureFolder + "Proprias");

            foreach (var s in Scanned)
            {
                ConfigureColor(Path(s.id, "diff", s.res), s.res == "2k" ? 2048 : 1024);
                ConfigureNormal(Path(s.id, "nor_gl", s.res), s.res == "2k" ? 2048 : 1024);
                BuildMask(s.id);
            }

            string own = TextureFolder + "Proprias/";
            WritePng(own + "Kilim_diff.png", KilimTexture(), true);
            var matHeight = MatHeight();
            WritePng(own + "Esteira_diff.png", MatColor(matHeight), true);
            WritePng(own + "Esteira_nor.png", HeightToNormal(matHeight, 3f), false);
            WritePng(own + "MetalPintado_diff.png", GreyPaint(Path("blue_metal_plate", "diff", "1k")), true);
            WritePng(own + "Descascado_diff.png", PeelTexture(Path("slumped_mortar_brick", "diff", "1k")), true);
            AssetDatabase.Refresh();
            ConfigureColor(own + "Kilim_diff.png", 1024);
            ConfigureColor(own + "Esteira_diff.png", 1024);
            ConfigureNormal(own + "Esteira_nor.png", 1024);
            ConfigureColor(own + "MetalPintado_diff.png", 1024);
            ConfigureColor(own + "Descascado_diff.png", 1024, alpha: true);

            // As cores são a média que cada material deve ter na tela (sRGB): o script calcula o tom que leva a
            // média da textura até ela. Assim o barro fica claro e quente, como pede a direção de arte.
            Scan(Plaster, "patterned_clay_plaster", "2k", 1.6f, Calibrated("patterned_clay_plaster", "2k", new Color(0.80f, 0.66f, 0.48f)));
            Scan(PlasterLight, "patterned_clay_plaster", "2k", 1.6f, Calibrated("patterned_clay_plaster", "2k", new Color(0.86f, 0.78f, 0.64f)));
            Scan(Ground, "dense_sand", "2k", 3f, Calibrated("dense_sand", "2k", new Color(0.80f, 0.69f, 0.52f)));
            Scan(Floor, "clay_floor_001", "1k", 2f, Calibrated("clay_floor_001", "1k", new Color(0.72f, 0.60f, 0.46f)));
            Scan(Wood, "rough_wood", "1k", 1f, Calibrated("rough_wood", "1k", new Color(0.47f, 0.38f, 0.29f)));

            var metal = Scan(Metal, "blue_metal_plate", "1k", 1f, new Color(0.24f, 0.66f, 0.66f));
            metal.SetTexture("_BaseMap", Tex(own + "MetalPintado_diff.png"));

            var brick = Scan(Brick, "slumped_mortar_brick", "1k", 1f, Calibrated("slumped_mortar_brick", "1k", new Color(0.64f, 0.50f, 0.36f)));
            brick.SetTexture("_BaseMap", Tex(own + "Descascado_diff.png"));
            brick.SetTextureScale("_BaseMap", Vector2.one);
            Cutout(brick, 0.5f);

            var kilim = Lit(Kilim, Color.white, 0f, 0.15f);
            kilim.SetTexture("_BaseMap", Tex(own + "Kilim_diff.png"));
            kilim.SetTexture("_BumpMap", Tex(own + "Esteira_nor.png"));
            kilim.SetFloat("_BumpScale", 0.35f);
            kilim.EnableKeyword("_NORMALMAP");

            var mat = Lit(Mat, Color.white, 0f, 0.2f);
            mat.SetTexture("_BaseMap", Tex(own + "Esteira_diff.png"));
            mat.SetTexture("_BumpMap", Tex(own + "Esteira_nor.png"));
            mat.EnableKeyword("_NORMALMAP");

            Lit(Gold, new Color(0.80f, 0.62f, 0.30f), 1f, 0.55f);
            Lit(PalmTrunk, new Color(0.36f, 0.28f, 0.21f), 0f, 0.1f);
            Lit(PalmCrown, new Color(0.30f, 0.33f, 0.20f), 0f, 0.1f);

            var flame = Material(Flame, "Universal Render Pipeline/Unlit");
            flame.SetColor("_BaseColor", new Color(4.0f, 2.1f, 0.7f));

            AssetDatabase.SaveAssets();
            Debug.Log("Vitrine da Aldeia: materiais criados em " + MaterialFolder);
        }

        // ---------- materiais ----------

        static Material Scan(string name, string id, string res, float meters, Color tint)
        {
            var m = Lit(name, tint, 0f, 1f);
            var scale = Vector2.one / meters;
            m.SetTexture("_BaseMap", Tex(Path(id, "diff", res)));
            m.SetTextureScale("_BaseMap", scale);
            m.SetTexture("_BumpMap", Tex(Path(id, "nor_gl", res)));
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            var mask = Tex(TextureFolder + id + "/" + id + "_mascara_1k.png");
            m.SetTexture("_MetallicGlossMap", mask);
            m.SetTexture("_OcclusionMap", mask);
            m.SetFloat("_OcclusionStrength", 1f);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.EnableKeyword("_OCCLUSIONMAP");
            return m;
        }

        /// <summary>Tom do material que leva a cor média da textura (em linear) até a média pedida (sRGB).</summary>
        static Color Calibrated(string id, string res, Color target)
        {
            var px = Load(Path(id, "diff", res)).GetPixels();
            Vector3 mean = Vector3.zero;
            foreach (var c in px)
            {
                var l = c.linear;
                mean += new Vector3(l.r, l.g, l.b);
            }
            mean /= px.Length;
            var t = target.linear;
            return new Color(t.r / mean.x, t.g / mean.y, t.b / mean.z).gamma;
        }

        static Material Lit(string name, Color color, float metallic, float smoothness)
        {
            var m = Material(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        static Material Material(string name, string shader)
        {
            string path = MaterialFolder + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Shader.Find(shader);
            m.shaderKeywords = new string[0];
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Cutout(Material m, float cutoff)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        // ---------- texturas baixadas ----------

        static string Path(string id, string map, string res) => $"{TextureFolder}{id}/{id}_{map}_{res}.jpg";

        static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        static void ConfigureColor(string path, int size, bool alpha = false)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = alpha;
            ti.maxTextureSize = size;
            ti.anisoLevel = 4;
            ti.SaveAndReimport();
        }

        static void ConfigureNormal(string path, int size)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.NormalMap;
            ti.maxTextureSize = size;
            ti.anisoLevel = 4;
            ti.SaveAndReimport();
        }

        /// <summary>arm do Poly Haven (R = oclusão, G = aspereza, B = metal) → máscara do URP (R metal, G oclusão, A lisura).</summary>
        static void BuildMask(string id)
        {
            var arm = Load(SourceFolder + id + "_arm_1k.jpg");
            var px = arm.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                px[i] = new Color32(c.b, c.r, 0, (byte)(255 - c.g));
            }
            var mask = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, true);
            mask.SetPixels32(px);
            string path = TextureFolder + id + "/" + id + "_mascara_1k.png";
            File.WriteAllBytes(path, mask.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.sRGBTexture = false;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.maxTextureSize = 1024;
            ti.SaveAndReimport();
        }

        static Texture2D Load(string path)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(path));
            return t;
        }

        static void WritePng(string path, Texture2D t, bool color)
        {
            File.WriteAllBytes(path, t.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        // ---------- texturas próprias ----------

        const int Size = 1024;

        static float Noise(float x, float y, int octaves)
        {
            float sum = 0, amp = 0.5f, freq = 1f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Mathf.PerlinNoise(x * freq + o * 31.7f, y * freq + o * 17.3f);
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum;
        }

        /// <summary>Kilim de faixas: borda escura, fileiras de losangos em vermelho de garança, índigo e ocre sobre creme.</summary>
        static Texture2D KilimTexture()
        {
            var cream = new Color(0.82f, 0.74f, 0.60f);
            var red = new Color(0.55f, 0.13f, 0.09f);
            var indigo = new Color(0.13f, 0.16f, 0.33f);
            var ochre = new Color(0.78f, 0.55f, 0.20f);
            var dark = new Color(0.16f, 0.10f, 0.08f);
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = x / (float)Size, v = y / (float)Size;
                float edge = Mathf.Min(Mathf.Min(u, 1 - u), Mathf.Min(v, 1 - v));
                Color c;
                if (edge < 0.035f) c = dark;
                else if (edge < 0.06f) c = red;
                else if (edge < 0.075f) c = cream;
                else if (edge < 0.11f) c = (Mathf.FloorToInt((u + v) * 40) % 2 == 0) ? indigo : cream;
                else
                {
                    // Faixas horizontais de losangos.
                    float band = (v - 0.11f) / 0.78f * 5f;
                    int bi = Mathf.FloorToInt(band);
                    float bv = band - bi;
                    float cell = (u - 0.11f) / 0.78f * 6f;
                    float cu = cell - Mathf.Floor(cell);
                    float d = Mathf.Abs(cu - 0.5f) + Mathf.Abs(bv - 0.5f);
                    Color field = bi % 2 == 0 ? red : indigo;
                    if (bv < 0.06f || bv > 0.94f) c = cream;
                    else if (d < 0.18f) c = ochre;
                    else if (d < 0.26f) c = dark;
                    else if (d < 0.40f) c = cream;
                    else c = field;
                }
                // Trama e desgaste.
                float weave = (x % 4 < 2 ^ y % 4 < 2) ? 0.95f : 1.03f;
                float wear = Mathf.Lerp(0.82f, 1.05f, Noise(u * 6, v * 6, 4));
                px[y * Size + x] = c * weave * wear;
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>Altura da esteira: tiras de folha de palmeira trançadas em sarja diagonal.</summary>
        static float[] MatHeight()
        {
            var h = new float[Size * Size];
            const int strip = 32;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int a = (x + y) / strip, b = (x - y + Size * 2) / strip;
                float fa = ((x + y) % strip) / (float)strip, fb = ((x - y + Size * 2) % strip) / (float)strip;
                bool over = ((a + b) / 2) % 2 == 0;
                float f = over ? fa : fb;
                float bump = Mathf.Sin(f * Mathf.PI);
                h[y * Size + x] = Mathf.Clamp01(bump * 0.8f + 0.2f * Noise(x * 0.05f, y * 0.05f, 3));
            }
            return h;
        }

        static Texture2D MatColor(float[] h)
        {
            var light = new Color(0.80f, 0.68f, 0.45f);
            var dark = new Color(0.52f, 0.40f, 0.24f);
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var px = new Color[Size * Size];
            for (int i = 0; i < px.Length; i++)
            {
                float n = Noise((i % Size) * 0.01f, (i / Size) * 0.01f, 3);
                px[i] = Color.Lerp(dark, light, h[i] * 0.7f + n * 0.4f);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D HeightToNormal(float[] h, float strength)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = h[y * Size + (x + 1) % Size] - h[y * Size + (x - 1 + Size) % Size];
                float dy = h[((y + 1) % Size) * Size + x] - h[((y - 1 + Size) % Size) * Size + x];
                var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                px[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>Tinta gasta em cinza claro (a cor turquesa vem do material), a partir da chapa azul do Poly Haven.</summary>
        static Texture2D GreyPaint(string diffPath)
        {
            var src = Load(diffPath);
            var px = src.GetPixels();
            float mean = 0;
            foreach (var c in px) mean += c.grayscale;
            mean /= px.Length;
            for (int i = 0; i < px.Length; i++)
            {
                float g = Mathf.Clamp01(0.85f * px[i].grayscale / mean);
                px[i] = new Color(g, g, g, 1f);
            }
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true);
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        /// <summary>Mancha de reboco caído: tijolo dentro de uma borda irregular (alfa), com a beira escurecida.</summary>
        static Texture2D PeelTexture(string brickPath)
        {
            var brick = Load(brickPath);
            int w = brick.width, hgt = brick.height;
            var px = brick.GetPixels();
            for (int y = 0; y < hgt; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w - 0.5f, v = y / (float)hgt - 0.5f;
                float r = Mathf.Sqrt(u * u * 1.4f + v * v * 2.2f);
                float n = Noise(x * 0.012f + 5f, y * 0.012f + 9f, 5);
                float field = 0.56f - r + (n - 0.5f) * 0.5f;
                float a = Mathf.Clamp01(field * 25f + 0.5f);
                float rim = Mathf.Clamp01(1f - field * 18f);
                var c = px[y * w + x] * Mathf.Lerp(1f, 0.55f, rim * rim);
                c.a = a;
                px[y * w + x] = c;
            }
            var t = new Texture2D(w, hgt, TextureFormat.RGBA32, true);
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
