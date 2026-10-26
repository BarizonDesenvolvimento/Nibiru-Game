using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// O que já mudou no mundo nesta partida: itens pegos (ou pegos em parte), checkpoints
    /// ativados, eventos da história e o passo de cada missão. Sobrevive ao recarregar a cena;
    /// os objetos consultam isto ao nascer.
    /// </summary>
    public static class WorldState
    {
        /// <summary>Cópia congelada do estado, guardada pelo checkpoint.</summary>
        public class Snapshot
        {
            internal readonly Dictionary<string, int> pickupsRemaining;
            internal readonly HashSet<string> activatedCheckpoints;
            internal readonly HashSet<string> storyEvents;
            internal readonly Dictionary<string, int> questSteps;
            internal readonly HashSet<string> failedQuests;
            internal readonly Dictionary<string, int> reputation;
            internal readonly int transformation;
            internal readonly Dictionary<string, int> sceneValues;

            internal Snapshot()
            {
                transformation = WorldState.Transformation;
                sceneValues = new Dictionary<string, int>(WorldState.sceneValues);
                failedQuests = new HashSet<string>(WorldState.failedQuests);
                pickupsRemaining = new Dictionary<string, int>(WorldState.pickupsRemaining);
                activatedCheckpoints = new HashSet<string>(WorldState.activatedCheckpoints);
                storyEvents = new HashSet<string>(WorldState.storyEvents);
                questSteps = new Dictionary<string, int>(WorldState.questSteps);
                reputation = new Dictionary<string, int>(WorldState.reputation);
            }

            // Montado peça por peça (ao ler o arquivo de salvamento).
            internal Snapshot(Dictionary<string, int> pickupsRemaining, HashSet<string> activatedCheckpoints, HashSet<string> storyEvents,
                Dictionary<string, int> questSteps, HashSet<string> failedQuests, Dictionary<string, int> reputation, int transformation,
                Dictionary<string, int> sceneValues = null)
            {
                this.sceneValues = sceneValues ?? new Dictionary<string, int>();
                this.pickupsRemaining = pickupsRemaining;
                this.activatedCheckpoints = activatedCheckpoints;
                this.storyEvents = storyEvents;
                this.questSteps = questSteps;
                this.failedQuests = failedQuests;
                this.reputation = reputation;
                this.transformation = transformation;
            }
        }

        static readonly Dictionary<string, int> pickupsRemaining = new Dictionary<string, int>();
        static readonly HashSet<string> activatedCheckpoints = new HashSet<string>();
        static readonly HashSet<string> storyEvents = new HashSet<string>();
        static readonly Dictionary<string, int> questSteps = new Dictionary<string, int>();
        static readonly HashSet<string> failedQuests = new HashSet<string>();
        static readonly Dictionary<string, int> reputation = new Dictionary<string, int>();
        static readonly Dictionary<string, int> sceneValues = new Dictionary<string, int>();

        /// <summary>
        /// Valor solto que a cena precisa lembrar ao voltar de um checkpoint e que não cabe em nenhum
        /// outro lugar do estado (por exemplo, em que linha uma fita estava ou em qual entrada um encontro
        /// aconteceu). A chave sempre começa com o prefixo do sistema ("tape.", "place.") e é estável como
        /// os demais códigos; cada chave nova entra no ESTADO.md. Não guarde aqui o que já tem lugar próprio.
        /// </summary>
        public static void SetSceneValue(string key, int value) => sceneValues[key] = value;

        public static bool TryGetSceneValue(string key, out int value) => sceneValues.TryGetValue(key, out value);

        public static void RemoveSceneValue(string key) => sceneValues.Remove(key);

        /// <summary>Transformação da Elena, de 0 (humana) a 100 (divina).</summary>
        public static int Transformation { get; set; }

        // Começa limpo a cada Play, mesmo com o recarregamento de domínio desligado no editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            pickupsRemaining.Clear();
            activatedCheckpoints.Clear();
            storyEvents.Clear();
            questSteps.Clear();
            failedQuests.Clear();
            reputation.Clear();
            sceneValues.Clear();
            Transformation = 0;
        }

        /// <summary>Quantos ainda restam num item do chão que já foi mexido. False se nunca foi pego.</summary>
        public static bool TryGetPickupRemaining(string id, out int remaining) =>
            pickupsRemaining.TryGetValue(id, out remaining);

        public static void SetPickupRemaining(string id, int remaining) => pickupsRemaining[id] = remaining;

        public static bool IsCheckpointActivated(string id) => activatedCheckpoints.Contains(id);

        public static void MarkCheckpointActivated(string id) => activatedCheckpoints.Add(id);

        public static bool HasStoryEvent(string id) => storyEvents.Contains(id);

        public static void MarkStoryEvent(string id) => storyEvents.Add(id);

        /// <summary>Passo atual de uma missão. False se ela ainda não começou.</summary>
        public static bool TryGetQuestStep(string questId, out int step) => questSteps.TryGetValue(questId, out step);

        public static void SetQuestStep(string questId, int step) => questSteps[questId] = step;

        /// <summary>True se a missão falhou (um prazo venceu, por exemplo).</summary>
        public static bool IsQuestFailed(string questId) => failedQuests.Contains(questId);

        public static void MarkQuestFailed(string questId) => failedQuests.Add(questId);

        /// <summary>Reputação com uma facção. False se ela nunca mudou.</summary>
        public static bool TryGetReputation(string factionId, out int value) => reputation.TryGetValue(factionId, out value);

        public static void SetReputation(string factionId, int value) => reputation[factionId] = value;

        public static Snapshot Capture() => new Snapshot();

        /// <summary>O estado em texto, uma linha por coisa e em ordem (para os testes compararem).</summary>
        public static string Describe(Snapshot snapshot)
        {
            var lines = new List<string>();
            foreach (string id in snapshot.storyEvents) lines.Add("evento " + id);
            foreach (KeyValuePair<string, int> pair in snapshot.questSteps) lines.Add($"missão {pair.Key} = {pair.Value}");
            foreach (string id in snapshot.failedQuests) lines.Add("missão que falhou " + id);
            foreach (KeyValuePair<string, int> pair in snapshot.reputation) lines.Add($"reputação {pair.Key} = {pair.Value}");
            foreach (KeyValuePair<string, int> pair in snapshot.pickupsRemaining) lines.Add($"item pego {pair.Key} = {pair.Value}");
            foreach (string id in snapshot.activatedCheckpoints) lines.Add("checkpoint " + id);
            foreach (KeyValuePair<string, int> pair in snapshot.sceneValues) lines.Add($"valor da cena {pair.Key} = {pair.Value}");
            lines.Sort(System.StringComparer.Ordinal);
            lines.Add("transformação = " + snapshot.transformation);
            return string.Join("\n", lines);
        }

        public static void Restore(Snapshot snapshot)
        {
            Clear();
            foreach (KeyValuePair<string, int> pair in snapshot.sceneValues) sceneValues[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in snapshot.pickupsRemaining) pickupsRemaining[pair.Key] = pair.Value;
            activatedCheckpoints.UnionWith(snapshot.activatedCheckpoints);
            storyEvents.UnionWith(snapshot.storyEvents);
            foreach (KeyValuePair<string, int> pair in snapshot.questSteps) questSteps[pair.Key] = pair.Value;
            failedQuests.UnionWith(snapshot.failedQuests);
            foreach (KeyValuePair<string, int> pair in snapshot.reputation) reputation[pair.Key] = pair.Value;
            Transformation = snapshot.transformation;
        }
    }
}
