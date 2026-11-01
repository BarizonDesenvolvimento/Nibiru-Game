using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: prova a proteção dos slots reais: a trava recusa a 2ª execução, o fim de A não desprotege B, escrita
    /// forçada fora da pasta da execução lança exceção, os slots reais não mudam e todo ponto de escrita passa pela camada
    /// de baixo. Roda sem Play, dentro do teste do salvamento, do teste geral e pelo menu. As escritas forçadas usam nomes
    /// que não são de slot: se a proteção falhar, o teste acusa sem estragar nada.
    /// </summary>
    public static class SlotGuardTests
    {
        const string RawWriters = @"\bFile\.(WriteAll\w*|AppendAll\w*|Delete|Move|Copy|Replace|Create\w*|Open\b|OpenWrite)\s*\(|\bDirectory\.(Delete|Move|CreateDirectory)\s*\(|\bnew\s+(FileStream|StreamWriter)\b";
        static readonly string[] Exempt = { "SaveStorage.cs", "AutomatedRun.cs", "SlotGuardTests.cs" };

        [MenuItem("Nibiru/Testes/Salvamento/Teste da proteção dos slots (sem Play, resumo no Console)", priority = 102)]
        static void Menu()
        {
            if (!SlotGuard.TryBeginRun("salvamento.protecao")) return;
            string token = AutomatedRun.CurrentToken;
            (bool ok, string report) = Run();
            AutomatedRun.End(token);
            Debug.Log($"Proteção dos slots: {(ok ? "tudo OK" : "FALHOU")}\n{report}");
        }

        /// <summary>Precisa de uma execução automática em andamento: é ela que faz o papel de B.</summary>
        public static (bool ok, string report) Run()
        {
            var report = new StringBuilder();
            bool ok = true;
            void Check(bool condition, string what)
            {
                report.AppendLine($"{(condition ? "OK" : "FALHOU")}  {what}");
                ok &= condition;
            }

            if (!AutomatedRun.Active)
            {
                Check(false, "o teste precisa de uma execução automática em andamento");
                return (ok, report.ToString());
            }

            string fingerprint = SaveStorage.RealFingerprint();
            string savedFolder = SaveSlots.Folder;
            string lab = SaveStorage.TestFolder("Teste_Protecao");
            try
            {
                Directory.CreateDirectory(lab);
                Isolation(Check);
                SecondAcquire(lab, Check);
                DeadProcess(lab, Check);
                TearDown(lab, Check);
                ForcedWrites(Check);
                WritePoints(Check);
                Check(SaveStorage.RealFingerprint() == fingerprint && fingerprint == AutomatedRun.StartFingerprint, "o hash dos slots reais é igual ao do começo da execução");
            }
            catch (Exception e)
            {
                Check(false, $"exceção {e.GetType().Name}: {e.Message}");
            }
            finally
            {
                SaveSlots.UseFolder(savedFolder);
                SaveStorage.DeleteTestFolder(lab);
            }
            return (ok, report.ToString());
        }

        static void Isolation(Action<bool, string> check)
        {
            string real = Path.GetFullPath(SaveStorage.RealFolder);
            check(!Path.GetFullPath(AutomatedRun.Root).StartsWith(real, StringComparison.OrdinalIgnoreCase) && AutomatedRun.Root.Contains(AutomatedRun.CurrentToken),
                "a raiz é uma pasta temporária única da execução, fora da pasta real");
            check(!Path.GetFullPath(AutomatedRun.LockPath).StartsWith(real, StringComparison.OrdinalIgnoreCase), "a trava fica fora da pasta dos slots");
        }

        static void SecondAcquire(string lab, Action<bool, string> check)
        {
            string token = AutomatedRun.CurrentToken;
            bool second = AutomatedRun.TryBegin("segunda execução", out string refusal);
            check(!second && refusal != null && refusal.Contains(AutomatedRun.Owner), "a 2ª aquisição da trava é recusada, com mensagem que diz quem está rodando");
            check(AutomatedRun.CurrentToken == token, "a recusa não muda a execução em andamento");

            string path = Path.Combine(lab, "dupla.lock");
            CrossProcessLock first = CrossProcessLock.TryAcquire(path, "A", "A", lab, false, out _, out _, out _);
            CrossProcessLock other = CrossProcessLock.TryAcquire(path, "B", "B", lab, false, out string why, out _, out _);
            check(first != null && other == null && why != null, "FileShare.None: com a trava aberta, a 2ª abertura do arquivo é recusada");
            first?.Release("A");
            other?.Release("B");
        }

        // Um processo de verdade como dono: vivo recusa; morto libera com aviso.
        static void DeadProcess(string lab, Action<bool, string> check)
        {
            string path = Path.Combine(lab, "morto.lock");
            using Process process = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 60 127.0.0.1 > nul") { CreateNoWindow = true, UseShellExecute = false });
            try
            {
                File.WriteAllText(path, $"pid={process.Id}\ninicio={process.StartTime.ToUniversalTime().Ticks}\ntoken=X\ndono=outro editor\nraiz={lab}\n");
                CrossProcessLock alive = CrossProcessLock.TryAcquire(path, "A", "A", lab, false, out string refusal, out _, out _);
                check(alive == null && refusal != null && refusal.Contains("outro editor"), "trava de processo vivo é recusada");
                alive?.Release("A");
            }
            finally
            {
                if (!process.HasExited) process.Kill();
                process.WaitForExit(5000);
            }
            CrossProcessLock freed = CrossProcessLock.TryAcquire(path, "A", "A", lab, false, out _, out string warning, out string stale);
            check(freed != null && warning != null && warning.Contains("morreu") && stale == lab, "trava de processo morto é liberada com aviso");
            freed?.Release("A");
        }

        static void TearDown(string lab, Action<bool, string> check)
        {
            // Trava: A solta, B pega, e o fim atrasado de A não solta a de B.
            string path = Path.Combine(lab, "ab.lock");
            CrossProcessLock a = CrossProcessLock.TryAcquire(path, "A", "A", lab, false, out _, out _, out _);
            a.Release("A");
            CrossProcessLock b = CrossProcessLock.TryAcquire(path, "B", "B", lab, false, out _, out _, out _);
            check(!a.Release("A") && !b.Release("A"), "o fim atrasado de A não solta a trava de B");
            CrossProcessLock c = CrossProcessLock.TryAcquire(path, "C", "C", lab, false, out _, out _, out _);
            check(c == null, "depois do fim atrasado de A, a trava de B continua valendo");
            b.Release("B");
            c?.Release("C");

            // Execução: esta é B, e tudo o que derrubou a proteção em 26/out roda em cima dela.
            string token = AutomatedRun.CurrentToken;
            check(!AutomatedRun.End("token-de-A") && !AutomatedRun.End("") && !AutomatedRun.End(null) && AutomatedRun.Active && AutomatedRun.CurrentToken == token,
                "End com o token de A não termina a execução de B");
            SaveSlots.UseFolder(null);
            check(InRoot(SaveSlots.Folder), "UseFolder(null) de outro teste cai na raiz da execução, não na pasta real");
            (bool shortcuts, string report) = SlotShortcuts.RunTest();
            check(shortcuts, "os atalhos de slot, que terminam com UseFolder(null), rodam na pasta da execução " + report);
            check(AutomatedRun.Active && InRoot(SaveSlots.Folder), "depois deles, a pasta continua dentro da execução");
            SlotGuard.Simulated = (false, false);
            check(AutomatedRun.Active && InRoot(SaveSlots.Folder), "o Simulated do SlotGuard não muda a pasta nem a proteção");
            SlotGuard.Simulated = null;
        }

        static void ForcedWrites(Action<bool, string> check)
        {
            string real = SaveStorage.RealFolder;
            string file = Path.Combine(real, "__guard_nao_deve_existir.txt");
            string other = Path.Combine(real, "__guard_nao_deve_existir_2.txt");
            string outside = Path.Combine(SaveStorage.TestBase, "fora_da_execucao", "x.txt");
            string inside = Path.Combine(SaveSlots.Folder, "dentro.txt");
            string relative = Path.Combine(real, "..", Path.GetFileName(real), "__guard_nao_deve_existir.txt");

            (string what, Action action)[] attempts =
            {
                ("gravar na pasta real", () => SaveStorage.WriteAllText(file, "x")),
                ("gravar na pasta real com \"..\" no caminho", () => SaveStorage.WriteAllText(relative, "x")),
                ("gravar na pasta real em maiúsculas", () => SaveStorage.WriteAllText(file.ToUpperInvariant(), "x")),
                ("apagar na pasta real", () => SaveStorage.Delete(file)),
                ("mover para a pasta real", () => SaveStorage.Move(inside, file)),
                ("trocar um arquivo da pasta real", () => SaveStorage.Replace(inside, file)),
                ("mover de dentro da pasta real", () => SaveStorage.Move(file, inside)),
                ("criar pasta na real", () => SaveStorage.CreateDirectory(Path.Combine(real, "__guard"))),
                ("apagar a pasta real como pasta de teste", () => SaveStorage.DeleteTestFolder(real)),
                ("apontar UseFolder para a pasta real", () => SaveSlots.UseFolder(real)),
                ("gravar em outra pasta temporária", () => SaveStorage.WriteAllText(outside, "x")),
            };
            foreach ((string what, Action action) in attempts)
            {
                bool blocked = false;
                try
                {
                    action();
                }
                catch (SlotWriteBlockedException)
                {
                    blocked = true;
                }
                check(blocked, $"escrita forçada barrada: {what}");
            }
            check(!File.Exists(file) && !File.Exists(outside) && !Directory.Exists(Path.Combine(real, "__guard")), "nenhuma escrita forçada chegou ao disco");

            // O caminho normal do jogo vai para a pasta da execução e não toca nos reais.
            if (InRoot(SaveSlots.Folder))
            {
                SaveSlots.WriteTestSlot(1, "aldeia", DateTime.Now, 1f, false, "Teste");
                check(File.Exists(Path.Combine(SaveSlots.Folder, "save_1.json")), "a gravação normal do slot vai para a pasta da execução");
                SaveSlots.Delete(1);
            }
            else check(false, "a pasta deveria estar dentro da execução antes da gravação normal");
        }

        static void WritePoints(Action<bool, string> check)
        {
            string scripts = Path.Combine(Application.dataPath, "_Nibiru", "Scripts");
            var found = new StringBuilder();
            int files = 0;
            foreach (string path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (Exempt.Contains(Path.GetFileName(path))) continue;
                files++;
                string[] lines = File.ReadAllLines(path).Select(line => Regex.Replace(line, @"(^\s*///.*$)|(^\s*//.*$)", "")).ToArray();
                bool usesSaves = lines.Any(line => line.Contains("SaveSlots") || line.Contains("SaveStorage"));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("persistentDataPath"))
                        found.AppendLine($"    {Path.GetFileName(path)}:{i + 1} usa persistentDataPath direto");
                    if (usesSaves && Regex.IsMatch(lines[i], RawWriters) && !lines[i].Contains("KnownListPath") && !lines[i].Contains("RegistryPath")) // arquivos do projeto, não saves
                        found.AppendLine($"    {Path.GetFileName(path)}:{i + 1} grava em disco sem passar por SaveStorage");
                }
            }
            check(files > 100 && found.Length == 0, $"todos os pontos de escrita passam pelo guard ({files} scripts conferidos)" + (found.Length > 0 ? "\n" + found : ""));
        }

        static bool InRoot(string folder) => Path.GetFullPath(folder).StartsWith(Path.GetFullPath(AutomatedRun.Root), StringComparison.OrdinalIgnoreCase);
    }
}
