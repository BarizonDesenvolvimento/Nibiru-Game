using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Símbolo acima da cabeça do soldado: "?" amarelo quando desconfia ou procura,
    /// "!" vermelho em combate. Sempre virado para a câmera.
    /// </summary>
    public class AwarenessIndicator : MonoBehaviour
    {
        [Tooltip("A IA do soldado (SoldierAI ou SniperAI). Vazio = procura no soldado acima.")]
        [SerializeField] MonoBehaviour soldier;
        [SerializeField] TextMesh label;

        [SerializeField] Color suspiciousColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] Color alertColor = new Color(1f, 0.3f, 0.2f);

        // Na patrulha, o "?" aparece aos poucos conforme ele começa a desconfiar.
        const float ShowAbove = 0.1f;

        Transform cameraTransform;
        ISoldierBrain brain;

        void Awake()
        {
            brain = soldier as ISoldierBrain;
            if (brain == null) brain = GetComponentInParent<ISoldierBrain>();
        }

        void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            // A IA pode ter sido trocada depois do Awake (o atirador troca a do prefab pela dele).
            if (brain as Object == null) brain = GetComponentInParent<ISoldierBrain>();
            if (brain as Object == null) return;

            string text = string.Empty;
            Color color = suspiciousColor;
            switch (brain.CurrentState)
            {
                case SoldierAI.State.Combate:
                    text = "!";
                    color = alertColor;
                    break;
                case SoldierAI.State.Desconfiado:
                case SoldierAI.State.Busca:
                    text = "?";
                    break;
                case SoldierAI.State.Patrulha:
                    if (brain.Awareness > ShowAbove)
                    {
                        text = "?";
                        color.a = Mathf.Clamp01(brain.Awareness * 2f);
                    }
                    break;
            }

            label.text = text;
            label.color = color;
            if (cameraTransform != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        }
    }
}
