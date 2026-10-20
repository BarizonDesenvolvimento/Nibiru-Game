using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// O que já mudou no mundo nesta partida: itens pegos (ou pegos em parte) e checkpoints
    /// ativados. Sobrevive ao recarregar a cena; os objetos consultam isto ao nascer.
    /// </summary>
    public static class WorldState
    {
        /// <summary>Cópia congelada do estado, guardada pelo checkpoint.</summary>
        public class Snapshot
        {
            internal readonly Dictionary<string, int> pickupsRemaining;
            internal readonly HashSet<string> activatedCheckpoints;

            internal Snapshot(Dictionary<string, int> pickups, HashSet<string> checkpoints)
            {
                pickupsRemaining = new Dictionary<string, int>(pickups);
                activatedCheckpoints = new HashSet<string>(checkpoints);
            }
        }

        static readonly Dictionary<string, int> pickupsRemaining = new Dictionary<string, int>();
        static readonly HashSet<string> activatedCheckpoints = new HashSet<string>();

        // Começa limpo a cada Play, mesmo com o recarregamento de domínio desligado no editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            pickupsRemaining.Clear();
            activatedCheckpoints.Clear();
        }

        /// <summary>Quantos ainda restam num item do chão que já foi mexido. False se nunca foi pego.</summary>
        public static bool TryGetPickupRemaining(string id, out int remaining) =>
            pickupsRemaining.TryGetValue(id, out remaining);

        public static void SetPickupRemaining(string id, int remaining) => pickupsRemaining[id] = remaining;

        public static bool IsCheckpointActivated(string id) => activatedCheckpoints.Contains(id);

        public static void MarkCheckpointActivated(string id) => activatedCheckpoints.Add(id);

        public static Snapshot Capture() => new Snapshot(pickupsRemaining, activatedCheckpoints);

        public static void Restore(Snapshot snapshot)
        {
            Clear();
            foreach (KeyValuePair<string, int> pair in snapshot.pickupsRemaining) pickupsRemaining[pair.Key] = pair.Value;
            activatedCheckpoints.UnionWith(snapshot.activatedCheckpoints);
        }
    }
}
