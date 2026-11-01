using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>Uma escrita tentou sair da pasta da execução automática. Nunca é engolida pelo salvamento.</summary>
    public sealed class SlotWriteBlockedException : IOException
    {
        public SlotWriteBlockedException(string message) : base(message) { }
    }

    /// <summary>
    /// A camada mais baixa de escrita do salvamento: é o único lugar que conhece a pasta real e que grava ou apaga arquivos
    /// de save. No jogo publicado, a pasta é sempre a real. No editor, durante uma execução automática
    /// (<see cref="AutomatedRun"/>), a raiz é decidida aqui, numa pasta temporária única da execução, e qualquer escrita
    /// fora dela lança <see cref="SlotWriteBlockedException"/>: é uma lista do que pode, não do que não pode. Por isso
    /// outra forma de escrever o caminho real (com "..", maiúsculas, nome curto) também é barrada. Não existe como desligar.
    /// </summary>
    public static class SaveStorage
    {
        static string folderOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => folderOverride = null;

        /// <summary>A pasta dos slots de quem joga. Só para ler: para gravar, use <see cref="Folder"/>.</summary>
        public static string RealFolder => Application.persistentDataPath;

        /// <summary>A pasta onde os slots são lidos e gravados agora.</summary>
        public static string Folder
        {
            get
            {
#if UNITY_EDITOR
                if (AutomatedRun.Active)
                {
                    string root = AutomatedRun.Root;
                    return folderOverride != null && IsInside(folderOverride, root) ? folderOverride : root;
                }
#endif
                return folderOverride ?? RealFolder;
            }
        }

        /// <summary>Só para os testes: grava e lê noutra pasta (null volta ao normal; numa execução automática, à raiz dela).</summary>
        public static void UseFolder(string path) => folderOverride = path == null ? null : Checked(path);

        // ---------- Escrita ----------

        public static void WriteAllText(string path, string text) => File.WriteAllText(Checked(path), text);

        public static void Replace(string source, string destination) => File.Replace(Checked(source), Checked(destination), null);

        public static void Move(string source, string destination) => File.Move(Checked(source), Checked(destination));

        public static void Delete(string path) => File.Delete(Checked(path));

        public static void CreateDirectory(string path) => Directory.CreateDirectory(Checked(path));

        // Numa execução automática, só passa o que está dentro da pasta dela.
        static string Checked(string path)
        {
#if UNITY_EDITOR
            if (AutomatedRun.Active && !IsInside(path, AutomatedRun.Root))
                throw new SlotWriteBlockedException($"Salvamento, proteção: escrita barrada em \"{path}\": a execução automática \"{AutomatedRun.Owner}\" só pode gravar em \"{AutomatedRun.Root}\".");
#endif
            return path;
        }

        static bool IsInside(string path, string folder)
        {
            string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string parent = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(full, parent, StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        // ---------- Conferência ----------

        /// <summary>Uma "impressão digital" dos arquivos de save da pasta real (nome e conteúdo), para comparar antes e depois de um teste.</summary>
        public static string RealFingerprint()
        {
            if (!Directory.Exists(RealFolder)) return "";
            var text = new StringBuilder();
            using SHA256 sha = SHA256.Create();
            foreach (string path in Directory.GetFiles(RealFolder, "save_*").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                text.Append(Path.GetFileName(path)).Append(':').Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)))).Append(';');
            return text.ToString();
        }

#if UNITY_EDITOR
        // ---------- Pastas de teste (só no editor) ----------

        /// <summary>Base de todas as pastas de teste, fora da pasta real.</summary>
        public static string TestBase => Path.Combine(Path.GetTempPath(), "Nibiru_Testes");

        /// <summary>Uma pasta de teste: dentro da execução automática, ou na base dos testes se for um atalho manual.</summary>
        public static string TestFolder(string name) => Path.Combine(AutomatedRun.Active ? AutomatedRun.Root : Path.Combine(TestBase, "manual"), name);

        /// <summary>Apaga uma pasta de teste. Fora da base dos testes, sempre lança exceção.</summary>
        public static void DeleteTestFolder(string path)
        {
            if (!IsInside(path, TestBase)) throw new SlotWriteBlockedException($"Salvamento, proteção: \"{path}\" não é uma pasta de teste.");
            Checked(path);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
#endif
    }
}
