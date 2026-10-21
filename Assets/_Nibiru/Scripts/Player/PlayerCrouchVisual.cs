using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Mostra o agachar: desce o que fica perto da cabeça (arma, faca) e o ponto que a câmera
    /// segue. Com o modelo animado da Elena, o corpo agacha pela animação e "Corpo" fica vazio;
    /// com um corpo de cápsula, ele é achatado mantendo os pés no chão.
    /// </summary>
    public class PlayerCrouchVisual : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        [Tooltip("Corpo visual (filho do Jogador), com a base nos pés.")]
        [SerializeField] Transform body;

        [Tooltip("Ponto que a câmera segue. Desce junto ao agachar.")]
        [SerializeField] Transform cameraTarget;

        [Tooltip("Outros objetos que descem junto (arma na mão, faca).")]
        [SerializeField] Transform[] lowerWithCrouch;

        [Tooltip("Altura do corpo agachado, como fração da altura em pé.")]
        [SerializeField, Range(0.3f, 1f)] float crouchedScale = 0.6f;

        [Tooltip("Quanto a câmera desce agachada, em metros.")]
        [SerializeField] float cameraDrop = 0.6f;

        [Tooltip("Sem Corpo (modelo animado): quanto arma e faca descem agachada, em metros.")]
        [SerializeField] float animatedLowerDrop = 0.3f;

        // O corpo é uma cápsula de 2 m com o centro na origem do Jogador.
        const float BodyHalfHeight = 1f;

        Vector3[] lowerStartPositions;
        Vector3 cameraStart;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            lowerStartPositions = new Vector3[lowerWithCrouch.Length];
            for (int i = 0; i < lowerWithCrouch.Length; i++) lowerStartPositions[i] = lowerWithCrouch[i].localPosition;
            if (cameraTarget != null) cameraStart = cameraTarget.localPosition;
        }

        void LateUpdate()
        {
            float blend = motor.CrouchBlend;
            float scaleY = Mathf.Lerp(1f, crouchedScale, blend);

            if (body != null)
            {
                body.localScale = new Vector3(1f, scaleY, 1f);
                body.localPosition = new Vector3(0f, -BodyHalfHeight * (1f - scaleY), 0f);
            }

            // Na cápsula, o topo do corpo desce o dobro do centro; no modelo, quem agacha é a animação.
            float topDrop = body != null ? 2f * BodyHalfHeight * (1f - scaleY) : animatedLowerDrop * blend;
            for (int i = 0; i < lowerWithCrouch.Length; i++)
                lowerWithCrouch[i].localPosition = lowerStartPositions[i] + Vector3.down * topDrop;

            if (cameraTarget != null)
                cameraTarget.localPosition = cameraStart + Vector3.down * (cameraDrop * blend);
        }
    }
}
