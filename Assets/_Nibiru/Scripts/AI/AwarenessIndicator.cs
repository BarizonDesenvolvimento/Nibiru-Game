using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Símbolo acima da cabeça do soldado: "?" amarelo quando desconfia ou procura,
    /// "!" vermelho em combate. Sempre virado para a câmera.
    /// </summary>
    public class AwarenessIndicator : MonoBehaviour
    {
        [SerializeField] SoldierAI soldier;
        [SerializeField] TextMesh label;

        [SerializeField] Color suspiciousColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] Color alertColor = new Color(1f, 0.3f, 0.2f);

        // Na patrulha, o "?" aparece aos poucos conforme ele começa a desconfiar.
        const float ShowAbove = 0.1f;

        Transform cameraTransform;

        void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            string text = string.Empty;
            Color color = suspiciousColor;
            switch (soldier.CurrentState)
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
                    if (soldier.Awareness > ShowAbove)
                    {
                        text = "?";
                        color.a = Mathf.Clamp01(soldier.Awareness * 2f);
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
