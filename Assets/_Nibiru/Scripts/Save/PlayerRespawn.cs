using Nibiru.Combat;
using Nibiru.Inventory;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// Fica no jogador. Guarda o estado dela quando um checkpoint é ativado e, ao recomeçar
    /// a cena, devolve a Elena ao checkpoint com a vida e o inventário daquele momento.
    /// </summary>
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] PlayerInventory inventory;
        [SerializeField] PlayerWeapons weapons;

        [Tooltip("Vida mínima ao voltar de um checkpoint, como fração da vida máxima. Evita voltar quase morta.")]
        [SerializeField, Range(0f, 1f)] float minHealthOnRespawn = 0.5f;

        Checkpoints.Data pending;

        // A posição muda no Awake, antes de qualquer Start, para a câmera já nascer atrás dela.
        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();

            if (!Checkpoints.TryGetFor(gameObject.scene.name, out pending)) return;

            CharacterController controller = GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            transform.SetPositionAndRotation(pending.position, pending.rotation);
            if (controller != null) controller.enabled = wasEnabled;
        }

        // Vida e inventário no Start, depois que eles mesmos se prepararam no Awake.
        void Start()
        {
            if (pending == null) return;
            health.Restore(Mathf.Max(pending.health, health.Max * minHealthOnRespawn));
            inventory.RestoreContents(pending.slots, pending.keyItems);
            // Depois do inventário, porque a arma equipada precisa estar nele.
            if (weapons != null && pending.weapons != null) weapons.Restore(pending.weapons);
            pending = null;
        }

        /// <summary>
        /// Guarda o estado atual como ponto de retorno, reaparecendo em spawnPoint, e grava uma cópia no slot da partida
        /// (se houver um). checkpointId é o código do checkpoint (diz a área no resumo do slot).
        /// </summary>
        public void SaveCheckpoint(Transform spawnPoint, string checkpointId = null)
        {
            var (slots, keyItems) = inventory.CopyContents();
            Checkpoints.Save(new Checkpoints.Data
            {
                sceneName = gameObject.scene.name,
                checkpointId = checkpointId,
                position = spawnPoint.position,
                rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f),
                health = health.Current,
                slots = slots,
                keyItems = keyItems,
                weapons = weapons != null ? weapons.Capture() : null,
                world = WorldState.Capture(),
            });
            SaveSlots.WriteCurrent();
        }
    }
}
