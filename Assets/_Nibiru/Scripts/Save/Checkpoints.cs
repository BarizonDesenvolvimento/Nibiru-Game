using System.Collections.Generic;
using Nibiru.Combat;
using Nibiru.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.Save
{
    /// <summary>
    /// Guarda o último checkpoint (onde a Elena estava, vida, inventário e estado do mundo)
    /// e recomeça a partir dele quando ela morre. Só na memória: o save em disco vem depois.
    /// </summary>
    public static class Checkpoints
    {
        public class Data
        {
            public string sceneName;
            public Vector3 position;
            public Quaternion rotation;
            public float health;
            public List<ItemStack> slots;
            public List<ItemStack> keyItems;
            public PlayerWeapons.Snapshot weapons;
            public WorldState.Snapshot world;
        }

        static Data last;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => last = null;

        public static void Save(Data data) => last = data;

        /// <summary>Esquece o ponto de retorno (ao voltar ao menu principal).</summary>
        public static void Clear() => last = null;

        /// <summary>O último checkpoint, se ele for desta cena.</summary>
        public static bool TryGetFor(string sceneName, out Data data)
        {
            data = last;
            return last != null && last.sceneName == sceneName;
        }

        /// <summary>
        /// Recarrega a cena com o mundo como estava no último checkpoint.
        /// Sem checkpoint, recomeça a cena do zero.
        /// </summary>
        public static void Restart(string sceneName)
        {
            if (TryGetFor(sceneName, out Data data)) WorldState.Restore(data.world);
            else WorldState.Clear();

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
