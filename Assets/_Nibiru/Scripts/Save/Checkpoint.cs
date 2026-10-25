using Nibiru.Interaction;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// Ponto de retorno. No modo "Ao passar", ativa quando a Elena entra na área
    /// (precisa de um Collider marcado como Is Trigger). No modo "Altar", ativa quando
    /// ela interage (precisa de um Collider normal, como o do pedestal).
    /// </summary>
    [RequireComponent(typeof(PersistentId))]
    public class Checkpoint : MonoBehaviour, IInteractable
    {
        public enum Activation
        {
            AoPassar,
            Altar,
        }

        [SerializeField] Activation activation = Activation.AoPassar;

        [Tooltip("Onde a Elena reaparece e para onde ela olha. Vazio = este objeto.")]
        [SerializeField] Transform spawnPoint;

        [Tooltip("Texto do aviso no modo Altar.")]
        [SerializeField] string altarPrompt = "Tocar o altar";

        [Tooltip("Evento disparado ao ativar (opcional), por exemplo para completar um objetivo.")]
        [SerializeField] Quests.StoryEvent raiseOnActivate;

        PersistentId persistentId;

        public string Prompt => altarPrompt;
        public bool CanInteract => activation == Activation.Altar && isActiveAndEnabled;

        void Awake() => persistentId = GetComponent<PersistentId>();

        public void Interact(PlayerInteractor interactor)
        {
            PlayerRespawn player = interactor.GetComponent<PlayerRespawn>();
            if (player != null) Activate(player);
        }

        // Um checkpoint de passagem só ativa uma vez por partida.
        void OnTriggerEnter(Collider other)
        {
            if (activation != Activation.AoPassar || WorldState.IsCheckpointActivated(persistentId.Id)) return;

            PlayerRespawn player = other.GetComponentInParent<PlayerRespawn>();
            if (player != null) Activate(player);
        }

        /// <summary>Marca este ponto de retorno na hora, sem a Elena passar por ele (chamado por uma cena).</summary>
        public void ActivateNow()
        {
            PlayerRespawn player = FindFirstObjectByType<PlayerRespawn>();
            if (player != null) Activate(player);
        }

        void Activate(PlayerRespawn player)
        {
            // Marca antes de guardar, para o próprio checkpoint já constar como ativado ao voltar.
            WorldState.MarkCheckpointActivated(persistentId.Id);
            player.SaveCheckpoint(spawnPoint != null ? spawnPoint : transform);
            Notifications.Post("Ponto de retorno marcado");
            Quests.StoryEvents.Raise(raiseOnActivate);
        }

        void OnDrawGizmos()
        {
            Transform point = spawnPoint != null ? spawnPoint : transform;
            Gizmos.color = new Color(0.25f, 0.55f, 1f, 0.8f);
            Gizmos.DrawWireSphere(point.position, 0.4f);
            Gizmos.DrawLine(point.position, point.position + point.forward);
        }
    }
}
