using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Alturas de referência dos personagens (em pé, do chão ao topo da cabeça) e o ajuste da escala
    /// de importação do FBX para chegar a elas. Os modelos do Meshy chegam todos com cerca de 0,95 m.
    /// A tabela também está no Docs/ESTADO.md.
    /// </summary>
    static class CharacterHeights
    {
        public static readonly IReadOnlyDictionary<string, float> Meters = new Dictionary<string, float>
        {
            ["Elena"] = 1.70f,
            ["Samir"] = 1.75f,
            ["Yusuf"] = 1.74f,
            ["UmmKarim"] = 1.58f,
            ["DraWeiss"] = 1.68f,
            ["Soldado"] = 1.82f,
            ["LiderDeEsquadra"] = 1.80f,
            ["AtiradorDeElite"] = 1.78f,
            ["Piloto"] = 1.78f,
            ["ArqueologoVeterano"] = 1.76f,
            ["ArqueologaJovem"] = 1.68f,
            ["TrabalhadorEscavacao"] = 1.72f, // também o Hassan
            ["Lullu"] = 2.10f, // em pé; na pose de caça, curvado, fica bem mais baixo
            ["PrimeiroServo"] = 2.50f,
            ["Ninsar"] = 1.90f,
        };

        /// <summary>
        /// Muda o "Scale Factor" da importação do modelo até ele medir a altura do personagem, na pose
        /// de repouso do FBX. Pode rodar de novo: se já está certo, não reimporta. Devolve a altura final.
        /// </summary>
        public static float FitImportScale(string modelPath, string character)
        {
            float target = Meters[character];
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            float height = Measure(modelPath);
            if (height > 0f && Mathf.Abs(height - target) > 0.002f)
            {
                importer.globalScale *= target / height;
                importer.SaveAndReimport();
                height = Measure(modelPath);
            }
            Debug.Log($"{character}: {height:0.000} m (alvo {target:0.00} m), escala de importação {importer.globalScale:0.000}.");
            return height;
        }

        // Altura da malha na pose em que o FBX foi salvo (a de repouso), com a escala de importação.
        static float Measure(string modelPath)
        {
            var instance = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            try
            {
                float min = float.MaxValue, max = float.MinValue;
                var baked = new Mesh();
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.BakeMesh(baked, true);
                    var t = renderer.transform;
                    var toWorld = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                    foreach (var v in baked.vertices)
                    {
                        float y = toWorld.MultiplyPoint3x4(v).y;
                        min = Mathf.Min(min, y);
                        max = Mathf.Max(max, y);
                    }
                }
                Object.DestroyImmediate(baked);
                return max > min ? max - min : 0f;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
