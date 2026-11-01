#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nibiru.Save
{
    /// <summary>
    /// Só no editor: a execução automática em andamento. Só uma de cada vez, garantido pela trava entre processos: o segundo
    /// teste se recusa a começar. Enquanto ela existir, <see cref="SaveStorage"/> só grava na pasta temporária dela.
    /// Só quem começou a execução a termina, com o código (token) que recebeu; não há flag que outro teste possa desligar.
    /// Se o teste quebrar, a execução termina ao sair do Play; até lá, tudo continua indo para a pasta temporária.
    /// </summary>
    public static class AutomatedRun
    {
        const string TokenKey = "Nibiru.AutomatedRun.Token";
        const string RootKey = "Nibiru.AutomatedRun.Root";
        const string OwnerKey = "Nibiru.AutomatedRun.Owner";
        const string FingerprintKey = "Nibiru.AutomatedRun.Fingerprint";
        const string KeepKey = "Nibiru.AutomatedRun.KeepForNextPlay";

        static CrossProcessLock held;
        static string heldRoot;
        static double nextRetry;

        /// <summary>A trava mora fora da pasta dos slots, num lugar comum a todos os processos desta conta do Windows.</summary>
        public static string LockPath => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Nibiru", "testes_automaticos.lock");

        /// <summary>True durante uma execução automática. Rodar a Unity sem janela (batch) conta sempre como automático.</summary>
        public static bool Active => Application.isBatchMode || held != null || !string.IsNullOrEmpty(CurrentToken);

        public static string CurrentToken => SessionState.GetString(TokenKey, "");

        public static string Owner => SessionState.GetString(OwnerKey, Application.isBatchMode ? "batch" : "");

        /// <summary>A pasta temporária da execução.</summary>
        public static string Root
        {
            get
            {
                string root = SessionState.GetString(RootKey, "");
                if (root.Length > 0) return root;
                return heldRoot ?? Path.Combine(SaveStorage.TestBase, $"batch_{Process.GetCurrentProcess().Id}");
            }
        }

        /// <summary>A impressão digital dos slots reais ao começar a execução.</summary>
        public static string StartFingerprint => SessionState.GetString(FingerprintKey, "");

        /// <summary>Começa uma execução. False (com o motivo) se já há outra, neste ou noutro processo.</summary>
        public static bool TryBegin(string owner, out string refusal)
        {
            string token = Guid.NewGuid().ToString("N");
            string root = Path.Combine(SaveStorage.TestBase, token);
            CrossProcessLock taken = CrossProcessLock.TryAcquire(LockPath, token, owner, root, false, out refusal, out string warning, out string staleRoot);
            if (taken == null)
            {
                string current = Owner;
                string who = current.Length > 0 ? $"\"{current}\" está rodando neste editor" : refusal;
                refusal = $"Salvamento, proteção: \"{owner}\" não começou: {who}. Testes automáticos nunca rodam em paralelo: espere o outro terminar e confira o resumo novo no log. " +
                    "Se nada estiver rodando, saia do Play; se continuar, use Nibiru > Testes > Salvamento > Liberar a trava dos testes.";
                return false;
            }
            if (warning != null) Debug.LogWarning(warning);
            DeleteRoot(staleRoot);

            Directory.CreateDirectory(root);
            held = taken;
            heldRoot = root;
            SessionState.SetString(TokenKey, token);
            SessionState.SetString(RootKey, root);
            SessionState.SetString(OwnerKey, owner);
            SessionState.SetString(FingerprintKey, SaveStorage.RealFingerprint());
            SessionState.EraseBool(KeepKey);
            SaveStorage.UseFolder(null);
            SaveSlots.Detach();
            Debug.Log($"Salvamento, proteção: execução automática \"{owner}\" começou; os slots ficam em {root}.");
            return true;
        }

        /// <summary>Termina a execução. Sem efeito (false) se o código não é o dela: quem terminou antes não desprotege quem veio depois.</summary>
        public static bool End(string token)
        {
            if (string.IsNullOrEmpty(token) || token != CurrentToken) return false;

            string owner = Owner;
            string root = Root;
            bool same = SaveStorage.RealFingerprint() == StartFingerprint;
            SaveSlots.Detach();
            SaveStorage.UseFolder(null);
            held?.Release(token);
            held = null;
            heldRoot = null;
            SessionState.EraseString(TokenKey);
            SessionState.EraseString(RootKey);
            SessionState.EraseString(OwnerKey);
            SessionState.EraseString(FingerprintKey);
            SessionState.EraseBool(KeepKey);
            DeleteRoot(root);

            if (same) Debug.Log($"Salvamento, proteção: execução automática \"{owner}\" terminou; slots reais iguais (hash conferido).");
            else Debug.LogError($"Salvamento, proteção: OS SLOTS REAIS MUDARAM durante a execução automática \"{owner}\"!");
            return true;
        }

        /// <summary>A execução continua depois de sair do Play (o teste geral passa do Play da região para o do laboratório).</summary>
        public static void KeepForNextPlay(string token)
        {
            if (!string.IsNullOrEmpty(token) && token == CurrentToken) SessionState.SetBool(KeepKey, true);
        }

        static void DeleteRoot(string root)
        {
            if (string.IsNullOrEmpty(root) || !root.StartsWith(SaveStorage.TestBase, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Salvamento, proteção: não consegui apagar a pasta temporária {root} ({e.Message}).");
            }
        }

        // ---------- Recarga de código e saída do Play ----------

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            AssemblyReloadEvents.beforeAssemblyReload += Suspend;
            EditorApplication.update += Reattach;
        }

        // A recarga fecha o arquivo; o conteúdo dele fica lá, então a trava continua valendo (PID vivo) até reabrir.
        static void Suspend()
        {
            held?.Suspend();
            held = null;
        }

        static void Reattach()
        {
            string token = CurrentToken;
            if (held != null || token.Length == 0 || EditorApplication.timeSinceStartup < nextRetry) return;
            nextRetry = EditorApplication.timeSinceStartup + 1.0;
            held = CrossProcessLock.TryAcquire(LockPath, token, Owner, Root, true, out _, out _, out _);
            if (held != null) heldRoot = Root;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || CurrentToken.Length == 0) return;
            if (SessionState.GetBool(KeepKey, false)) SessionState.EraseBool(KeepKey);
            else End(CurrentToken);
        }

        [MenuItem("Nibiru/Testes/Salvamento/Liberar a trava dos testes (só se nada estiver rodando)", priority = 103)]
        static void ReleaseMenu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Salvamento, proteção: saia do Play antes (sair do Play já libera a trava).");
                return;
            }
            if (CurrentToken.Length == 0)
            {
                Debug.Log($"Salvamento, proteção: este editor não tem trava. Se outro processo a segura, feche-o. Arquivo: {LockPath}");
                return;
            }
            if (EditorUtility.DisplayDialog("Liberar a trava dos testes", $"\"{Owner}\" ainda consta como rodando. Libere só se nenhum teste estiver rodando.", "Liberar", "Cancelar"))
                End(CurrentToken);
        }
    }

    /// <summary>
    /// A trava em si: um arquivo aberto com FileShare.None enquanto a execução vive, com o processo dono escrito dentro.
    /// Abrir o arquivo decide a corrida entre processos; o conteúdo cobre o intervalo da recarga de código (arquivo fechado)
    /// e o processo que morreu sem soltar: PID morto libera com aviso, PID vivo recusa.
    /// </summary>
    public sealed class CrossProcessLock
    {
        readonly string path;
        readonly string token;
        FileStream stream;

        CrossProcessLock(string path, string token, FileStream stream)
        {
            this.path = path;
            this.token = token;
            this.stream = stream;
        }

        /// <summary>Tenta pegar a trava; null (com o motivo em refusal) se ela tem dono. Com reattach, aceita o próprio código no mesmo processo.</summary>
        public static CrossProcessLock TryAcquire(string path, string token, string owner, string root, bool reattach, out string refusal, out string warning, out string staleRoot)
        {
            refusal = warning = staleRoot = null;
            FileStream file;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                refusal = "outro teste automático está rodando (a trava está aberta por outra execução)";
                return null;
            }

            try
            {
                Dictionary<string, string> info = ReadInfo(file);
                if (info.Count > 0)
                {
                    int.TryParse(Value(info, "pid"), out int pid);
                    long.TryParse(Value(info, "inicio"), out long started);
                    Process self = Process.GetCurrentProcess();
                    bool mine = reattach && pid == self.Id && Value(info, "token") == token;
                    if (!mine)
                    {
                        if (IsAlive(pid, started))
                        {
                            refusal = $"\"{Value(info, "dono")}\" está rodando no processo {pid}";
                            file.Dispose();
                            return null;
                        }
                        warning = $"Salvamento, proteção: a trava de \"{Value(info, "dono")}\" era do processo {pid}, que já morreu; liberada.";
                        staleRoot = Value(info, "raiz");
                    }
                }
                WriteInfo(file, token, owner, root);
                return new CrossProcessLock(path, token, file);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        /// <summary>Solta a trava. Sem efeito (false) com o código errado.</summary>
        public bool Release(string releaseToken)
        {
            if (stream == null || releaseToken != token) return false;
            stream.SetLength(0);
            stream.Dispose();
            stream = null;
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Outro processo abriu no mesmo instante: o arquivo vazio já quer dizer trava livre.
            }
            return true;
        }

        /// <summary>Fecha o arquivo sem apagar o dono (recarga de código).</summary>
        public void Suspend()
        {
            stream?.Dispose();
            stream = null;
        }

        static string Value(Dictionary<string, string> info, string key) => info.TryGetValue(key, out string value) ? value : "";

        static Dictionary<string, string> ReadInfo(FileStream file)
        {
            var info = new Dictionary<string, string>();
            file.Position = 0;
            var bytes = new byte[file.Length];
            int read = file.Read(bytes, 0, bytes.Length);
            foreach (string line in Encoding.UTF8.GetString(bytes, 0, read).Split('\n'))
            {
                int equals = line.IndexOf('=');
                if (equals > 0) info[line.Substring(0, equals)] = line.Substring(equals + 1).Trim();
            }
            return info;
        }

        static void WriteInfo(FileStream file, string token, string owner, string root)
        {
            Process self = Process.GetCurrentProcess();
            string text = $"pid={self.Id}\ninicio={self.StartTime.ToUniversalTime().Ticks}\ntoken={token}\ndono={owner}\nraiz={root}\ndata={DateTime.Now:O}\n";
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            file.SetLength(0);
            file.Position = 0;
            file.Write(bytes, 0, bytes.Length);
            file.Flush(true);
        }

        // O início do processo separa um PID reaproveitado pelo Windows do dono de verdade. Na dúvida, vivo (recusa).
        static bool IsAlive(int pid, long startedTicks)
        {
            if (pid <= 0) return false;
            Process process;
            try
            {
                process = Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                return false;
            }
            try
            {
                if (process.HasExited) return false;
                return startedTicks == 0 || Math.Abs(process.StartTime.ToUniversalTime().Ticks - startedTicks) < TimeSpan.TicksPerSecond;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
#endif
