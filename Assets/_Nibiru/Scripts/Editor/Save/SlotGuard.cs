using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: protege os slots reais (os de persistentDataPath) dos atalhos e situações de teste. Toda ação de teste
    /// que grava, regrava ou apaga um slot passa por <see cref="Allow"/>: slot real vazio segue sem perguntar; slot real
    /// que não está vazio abre uma janela com o número, o estado, a área e a data, e cancelar não deixa nada ser gravado.
    /// Numa execução automática (<see cref="Automated"/>: teste do salvamento e teste geral dos atalhos) nenhuma ação mexe
    /// em slot real e nenhuma janela abre, para não travar o teste. Pastas de teste (<see cref="SaveSlots.UseFolder"/>)
    /// não são slots reais e não pedem nada.
    /// O flag <see cref="Automated"/> fica no SessionState (sobrevive à recarga de código ao ligar o Play) e tem prazo:
    /// quem liga renova o prazo a cada quadro, então um teste que quebra no meio não deixa o flag ligado.
    /// </summary>
    [InitializeOnLoad]
    public static class SlotGuard
    {
        const string AutomatedKey = "Nibiru.SlotGuard.Automated";
        const string ExpiresKey = "Nibiru.SlotGuard.Expires";

        /// <summary>Segundos que o flag vale sem ser renovado.</summary>
        public const double DefaultLease = 120.0;

        /// <summary>Só para os testes: troca a janela de confirmação (devolve true para continuar).</summary>
        internal static Func<string, string, bool> Ask = (title, message) => EditorUtility.DisplayDialog(title, message, "Continuar", "Cancelar");

        /// <summary>Só para os testes: trata a pasta atual como se fosse a dos slots reais (true) ou como pasta de teste (false).</summary>
        internal static bool? ForceReal;

        /// <summary>True se a pasta atual é a dos slots reais.</summary>
        public static bool IsReal => ForceReal ?? (SaveSlots.Folder == Application.persistentDataPath);

        /// <summary>True durante uma execução automática (e dentro do prazo).</summary>
        public static bool Automated
        {
            get
            {
                if (!SessionState.GetBool(AutomatedKey, false)) return false;
                if (EditorApplication.timeSinceStartup <= double.Parse(SessionState.GetString(ExpiresKey, "0"), System.Globalization.CultureInfo.InvariantCulture)) return true;
                SessionState.SetBool(AutomatedKey, false);
                return false;
            }
        }

        // ---------- Execução automática ----------

        /// <summary>Liga o flag por <paramref name="lease"/> segundos (renove com <see cref="Renew"/>).</summary>
        public static void Begin(double lease = DefaultLease)
        {
            SessionState.SetBool(AutomatedKey, true);
            SessionState.SetString(ExpiresKey, (EditorApplication.timeSinceStartup + lease).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Renova o prazo, se o flag estiver ligado.</summary>
        public static void Renew()
        {
            if (Automated) Begin();
        }

        public static void End() => SessionState.SetBool(AutomatedKey, false);

        /// <summary>Roda a ação com o flag no valor pedido e sempre o devolve ao anterior, mesmo que ela lance exceção.</summary>
        public static void Run(bool automated, Action action)
        {
            bool before = SessionState.GetBool(AutomatedKey, false);
            string expires = SessionState.GetString(ExpiresKey, "0");
            try
            {
                if (automated) Begin();
                else End();
                action();
            }
            finally
            {
                SessionState.SetBool(AutomatedKey, before);
                SessionState.SetString(ExpiresKey, expires);
            }
        }

        // ---------- Proteção ----------

        /// <summary>
        /// Diz se a ação de teste pode mexer nos slots. Pasta de teste: sempre. Slot real: nunca em execução automática;
        /// no uso normal, só depois de o jogador confirmar quando algum dos slots não está vazio.
        /// </summary>
        public static bool Allow(string action, params int[] slots)
        {
            if (!IsReal) return true;
            if (Automated)
            {
                Debug.LogWarning($"Salvamento: \"{action}\" não foi feita: a execução automática nunca mexe nos slots reais.");
                return false;
            }
            List<string> filled = slots.Distinct().OrderBy(s => s).Select(Describe).Where(text => text != null).ToList();
            if (filled.Count == 0) return true;

            string plural = filled.Count == 1 ? "este slot não está vazio" : "estes slots não estão vazios";
            string message = $"Ação de teste: {action}.\nO conteúdo atual será perdido ({plural}):\n\n{string.Join("\n", filled)}\n\n{SaveSlots.Folder}";
            if (Ask("Slot real em uso", message)) return true;
            Debug.Log($"Salvamento: \"{action}\" cancelada; nada foi gravado.");
            return false;
        }

        // "Slot 2: ocupado, Aldeia, gravado em 26/10/2026 14:03", ou null se o slot está vazio.
        static string Describe(int slot)
        {
            SaveSlots.SlotInfo info = SaveSlots.Inspect(slot);
            switch (info.state)
            {
                case SaveSlots.SlotState.Empty: return null;
                case SaveSlots.SlotState.Unreadable: return $"Slot {slot}: ilegível (arquivo com dados que o jogo não consegue ler)";
                default:
                    string state = info.state == SaveSlots.SlotState.DemoCompleted ? "demo concluída" : "ocupado";
                    return $"Slot {slot}: {state}, {info.AreaName}, gravado em {info.savedAt:dd/MM/yyyy HH:mm}";
            }
        }

        // ---------- Conferência de que os slots reais não mudaram ----------

        /// <summary>Uma "impressão digital" dos 3 arquivos reais (existência e conteúdo), para comparar antes e depois de um teste.</summary>
        public static string RealFingerprint()
        {
            var text = new StringBuilder();
            using SHA256 sha = SHA256.Create();
            for (int slot = 1; slot <= SaveSlots.SlotCount; slot++)
            {
                string path = Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
                text.Append(slot).Append(':');
                if (File.Exists(path)) text.Append(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))));
                else text.Append("-");
                text.Append(';');
            }
            return text.ToString();
        }
    }
}
