using System;
using System.Collections.Generic;
using System.Linq;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Saving
{
    /// <summary>
    /// Só no editor: a pergunta antes de um atalho de teste manual mexer num slot real. Toda ação de teste que grava, regrava
    /// ou apaga um slot passa por <see cref="Allow"/>: slot real vazio segue sem perguntar; slot real que não está vazio abre
    /// uma janela com o número, o estado, a área e a data, e cancelar não deixa nada ser gravado.
    /// Isto é só a cortesia dos atalhos manuais. Quem protege os slots numa execução automática é a camada de escrita
    /// (<see cref="SaveStorage"/> e <see cref="AutomatedRun"/>), que não depende de nada daqui.
    /// </summary>
    public static class SlotGuard
    {
        /// <summary>Só para os testes: troca a janela de confirmação (devolve true para continuar).</summary>
        internal static Func<string, string, bool> Ask = (title, message) => EditorUtility.DisplayDialog(title, message, "Continuar", "Cancelar");

        /// <summary>Só para os testes: muda só a resposta de <see cref="Allow"/>. A escrita continua presa à pasta da execução automática.</summary>
        internal static (bool real, bool automated)? Simulated;

        /// <summary>True se a pasta atual é a dos slots reais.</summary>
        public static bool IsReal => Simulated?.real ?? (SaveSlots.Folder == SaveStorage.RealFolder);

        /// <summary>True durante uma execução automática.</summary>
        public static bool Automated => Simulated?.automated ?? AutomatedRun.Active;

        /// <summary>Começa uma execução automática; recusada, escreve o motivo no Console e devolve false (o teste não roda).</summary>
        public static bool TryBeginRun(string owner)
        {
            if (AutomatedRun.TryBegin(owner, out string refusal)) return true;
            Debug.LogError(refusal);
            return false;
        }

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
    }
}
