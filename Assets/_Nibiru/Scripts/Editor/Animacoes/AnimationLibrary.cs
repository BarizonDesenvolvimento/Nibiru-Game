using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Os clipes da biblioteca (ArteFonte/Animacoes, fora do Git) que o jogo usa: de qual arquivo vêm,
    /// com que nome entram em Assets/_Nibiru/Art/Animacoes e como são importados. É a única tabela:
    /// o AssetPostprocessor (MixamoClipPostprocessor) lê daqui, o menu de importação copia daqui e o
    /// teste automático confere daqui. Para trocar um clipe, mude a linha e rode o menu de novo.
    /// </summary>
    static class AnimationLibrary
    {
        public const string Library = "ArteFonte/Animacoes/Mixamo/";
        public const string Destination = "Assets/_Nibiru/Art/Animacoes/";

        /// <summary>
        /// name: nome do clipe e do arquivo em Assets. source: caminho do FBX dentro da biblioteca.
        /// loop: Loop Time. original: giro "Original" (golpes de corpo de lado) em vez de "Body Orientation".
        /// feet: altura pelos pés. moving: anda (a velocidade natural é lida do clipe). keepTravel: o
        /// deslocamento fica na pose. first e last: trecho do arquivo (quadros como no Mixamo, o primeiro é 1;
        /// -1 = inteiro). Em todos o avanço da raiz é descartado: quem anda é o jogo.
        /// </summary>
        public readonly struct Entry
        {
            public readonly string name, group, source;
            public readonly bool loop, original, feet, moving, keepTravel;
            public readonly int first, last;

            public Entry(string name, string group, string source, bool loop,
                bool original = false, bool feet = false, bool moving = false, bool keepTravel = false,
                int first = -1, int last = -1)
            {
                this.name = name; this.group = group; this.source = source; this.loop = loop;
                this.original = original; this.feet = feet; this.moving = moving; this.keepTravel = keepTravel;
                this.first = first; this.last = last;
            }

            public string AssetPath => Destination + group + "/" + name + ".fbx";
        }

        const string Zombie = "Not So Scary Zombie Pack/";
        const string Creature = "Creature Pack/";
        const string Female = "Female Basic Locomotion Pack/";

        public static readonly Entry[] Entries =
        {
            // Lullu: o pacote do zumbi (cambaleante, mais fraco).
            new("Lullu_Parado", "Criatura", Zombie + "zombie idle", true),
            new("Lullu_Andar", "Criatura", Zombie + "walking", true, moving: true),
            new("Lullu_Correr", "Criatura", Zombie + "zombie running", true, moving: true),
            new("Lullu_Ataque", "Criatura", Zombie + "zombie attack", false, feet: true, first: 18, last: 62),
            new("Lullu_Saida_Rachaduras", "Criatura", Zombie + "zombie stand up", false),
            // Primeiro Servo: o pacote do mutante (forte, pesado).
            new("Servo_Parado", "Criatura", Creature + "mutant breathing idle", true),
            new("Servo_Andar", "Criatura", Creature + "mutant walking", true, moving: true),
            new("Servo_Correr", "Criatura", Creature + "mutant run", true, moving: true),
            new("Servo_Ataque", "Criatura", Creature + "mutant swiping", false, feet: true, first: 14, last: 56),
            new("Servo_Investida", "Criatura", Creature + "mutant jump attack", false, first: 1, last: 75),
            new("Servo_Urro", "Criatura", Creature + "mutant roaring", false, first: 10, last: 70),
            // Os dois: o golpe recebido (zumbi, para a camada aditiva) e a morte (o zumbi não tem).
            new("Criatura_Golpe_Recebido", "Criatura", Zombie + "zombie reaction hit", false, first: 8, last: 40),
            new("Criatura_Morte", "Criatura", Creature + "mutant dying", false, keepTravel: true),

            // Elena: locomoção feminina para comparar com a atual (Idle, WalkForward e Fast Run), mais
            // as viradas (sem uso no controlador: o jogo gira o corpo por código) e as interações.
            new("Elena_Parada_Basic", "Elena", Female + "idle", true),
            new("Elena_Andar_Basic", "Elena", Female + "walking", true, moving: true),
            new("Elena_Correr_Basic", "Elena", Female + "running", true, moving: true),
            new("Elena_Virar_Esquerda", "Elena", Female + "left turn", false),
            new("Elena_Virar_Direita", "Elena", Female + "right turn", false),
            new("Elena_Pegar", "Elena", "Avulsas/X Bot@Picking Up Object", false),
            new("Elena_Abrir", "Elena", "Avulsas/X Bot@Open Door Outwards", false),

            // Caminhonete: o Samir dirigindo. O sentado da Elena não vem destes pacotes (veja o catálogo).
            new("Samir_Dirigindo", "Caminhonete", "Avulsas/X Bot@Driving", true),
        };

        public static bool TryGet(string assetPath, out Entry entry)
        {
            foreach (Entry e in Entries)
                if (e.AssetPath == assetPath) { entry = e; return true; }
            entry = default;
            return false;
        }

        public static Entry Get(string name) => Entries.First(e => e.name == name);

        public static AnimationClip Clip(string name)
        {
            if (!Entries.Any(e => e.name == name)) return null;
            return AssetDatabase.LoadAllAssetsAtPath(Get(name).AssetPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
        }

        /// <summary>Velocidade (m/s) em que os pés não escorregam: o avanço médio do clipe.</summary>
        public static float NaturalSpeed(string name) => Clip(name).averageSpeed.magnitude;

        [MenuItem("Nibiru/Animações/Importar clipes da biblioteca")]
        static void ImportAll()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de importar animações.");
                return;
            }
            int copied = 0;
            foreach (Entry e in Entries)
            {
                string from = Library + e.source + ".fbx";
                if (!File.Exists(from)) { Debug.LogError("Animação não encontrada na biblioteca: " + from); continue; }
                string to = e.AssetPath;
                if (File.Exists(to)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                File.Copy(from, to);
                copied++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"Animações: {copied} arquivo(s) copiado(s) da biblioteca; {Entries.Length} clipes no total.");
        }

        [MenuItem("Nibiru/Animações/Reimportar clipes (depois de mudar a tabela)")]
        static void ReimportAll()
        {
            foreach (Entry e in Entries)
                if (File.Exists(e.AssetPath)) AssetDatabase.ImportAsset(e.AssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("Animações reimportadas.");
        }
    }

    /// <summary>
    /// Importa os FBX de Assets/_Nibiru/Art/Animacoes que estão na tabela da AnimationLibrary, sempre do
    /// mesmo jeito: Humanoid com o avatar do X Bot (pose T), um clipe com o nome do arquivo, Loop Time
    /// pela tabela, giro e altura travados na pose, avanço da raiz descartado.
    /// </summary>
    class MixamoClipPostprocessor : AssetPostprocessor
    {
        const string XBotPath = "Assets/_Nibiru/Art/Animacoes/Mixamo/X Bot.fbx";

        void OnPreprocessModel()
        {
            if (!AnimationLibrary.TryGet(assetPath, out _)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(XBotPath).OfType<Avatar>().FirstOrDefault();
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importBlendShapes = false;
            importer.importAnimation = true;
        }

        void OnPreprocessAnimation()
        {
            if (!AnimationLibrary.TryGet(assetPath, out var entry)) return;
            var importer = (ModelImporter)assetImporter;
            var clip = importer.defaultClipAnimations.FirstOrDefault();
            if (clip == null) return;
            clip.name = entry.name;
            // O Unity conta os quadros a partir do primeiro do take; nos cortes, o primeiro é 1.
            float offset = clip.firstFrame - 1f;
            if (entry.first >= 0) clip.firstFrame = entry.first + offset;
            if (entry.last >= 0) clip.lastFrame = entry.last + offset;
            clip.loopTime = entry.loop;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = entry.original;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = !entry.feet;
            clip.heightFromFeet = entry.feet;
            clip.lockRootPositionXZ = entry.keepTravel;
            clip.keepOriginalPositionXZ = entry.keepTravel;
            clip.hasAdditiveReferencePose = false;
            importer.clipAnimations = new[] { clip };
        }
    }
}
