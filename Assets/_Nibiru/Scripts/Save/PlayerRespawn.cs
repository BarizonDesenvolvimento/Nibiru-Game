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
        // A posição guardada é onde o pé pisa; o Jogador fica no meio do corpo. Pôr o centro no ponto enterrava
        // a cápsula 1 m no chão, e a física a desenterrava para um lado que dependia da ordem dos colisores da cena
        // (no pé da descida do Poço, 1,3 m para o lado).
        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (weapons == null) weapons = GetComponent<PlayerWeapons>();

            if (!Checkpoints.TryGetFor(gameObject.scene.name, out pending)) return;

            CharacterController controller = GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            transform.SetPositionAndRotation(pending.position + Vector3.up * FeetToCenter(controller), pending.rotation);
            if (controller != null) controller.enabled = wasEnabled;
            // O autoSyncTransforms está desligado: sem isto, a física só veria a posição nova no próximo passo.
            Physics.SyncTransforms();
        }

        /// <summary>Do pé ao Jogador: meia cápsula acima do centro dela, mais a folga de contato, para nascer sem tocar no chão.</summary>
        static float FeetToCenter(CharacterController controller) =>
            controller != null ? controller.height * 0.5f - controller.center.y + controller.skinWidth : 0f;

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
        /// Guarda o estado atual como ponto de retorno, reaparecendo com o pé em spawnPoint (um ponto no chão), e grava
        /// uma cópia no slot da partida (se houver um). checkpointId é o código do checkpoint (diz a área no resumo do slot).
        /// </summary>
        public void SaveCheckpoint(Transform spawnPoint, string checkpointId = null) =>
            Save(spawnPoint.position, spawnPoint.eulerAngles.y, checkpointId);

        /// <summary>Como <see cref="SaveCheckpoint"/>, reaparecendo onde a Elena está agora (guarda o ponto onde o pé pisa).</summary>
        public void SaveCheckpointHere(string checkpointId = null) =>
            Save(transform.position - Vector3.up * FeetToCenter(GetComponent<CharacterController>()), transform.eulerAngles.y, checkpointId);

        void Save(Vector3 feet, float yaw, string checkpointId)
        {
            var (slots, keyItems) = inventory.CopyContents();
            Checkpoints.Save(new Checkpoints.Data
            {
                sceneName = gameObject.scene.name,
                checkpointId = checkpointId,
                position = feet,
                rotation = Quaternion.Euler(0f, yaw, 0f),
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
