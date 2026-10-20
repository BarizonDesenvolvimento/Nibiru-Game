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
            internal readonly Dictionary<string, int> reputation;
            internal readonly int transformation;

            internal Snapshot()
            {
                transformation = WorldState.Transformation;
                pickupsRemaining = new Dictionary<string, int>(WorldState.pickupsRemaining);
                activatedCheckpoints = new HashSet<string>(WorldState.activatedCheckpoints);
                storyEvents = new HashSet<string>(WorldState.storyEvents);
                questSteps = new Dictionary<string, int>(WorldState.questSteps);
                reputation = new Dictionary<string, int>(WorldState.reputation);
            }
        }

        static readonly Dictionary<string, int> pickupsRemaining = new Dictionary<string, int>();
        static readonly HashSet<string> activatedCheckpoints = new HashSet<string>();
        static readonly HashSet<string> storyEvents = new HashSet<string>();
        static readonly Dictionary<string, int> questSteps = new Dictionary<string, int>();
        static readonly Dictionary<string, int> reputation = new Dictionary<string, int>();

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
            reputation.Clear();
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

        /// <summary>Reputação com uma facção. False se ela nunca mudou.</summary>
        public static bool TryGetReputation(string factionId, out int value) => reputation.TryGetValue(factionId, out value);

        public static void SetReputation(string factionId, int value) => reputation[factionId] = value;

        public static Snapshot Capture() => new Snapshot();

        public static void Restore(Snapshot snapshot)
        {
            Clear();
            foreach (KeyValuePair<string, int> pair in snapshot.pickupsRemaining) pickupsRemaining[pair.Key] = pair.Value;
            activatedCheckpoints.UnionWith(snapshot.activatedCheckpoints);
            storyEvents.UnionWith(snapshot.storyEvents);
            foreach (KeyValuePair<string, int> pair in snapshot.questSteps) questSteps[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, int> pair in snapshot.reputation) reputation[pair.Key] = pair.Value;
            Transformation = snapshot.transformation;
        }
    }
}
