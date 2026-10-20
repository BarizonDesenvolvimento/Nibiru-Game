using UnityEngine;

namespace Nibiru.Cameras
{
    /// <summary>
    /// Um jeito de posicionar a câmera (terceira pessoa, primeira pessoa...).
    /// O PlayerCameraController cuida da rotação vinda do mouse/controle e
    /// pede ao modo ativo para colocar a câmera no lugar certo.
    /// Para trocar de modo, basta chamar PlayerCameraController.SetMode.
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>Limites de inclinação vertical (mínimo, máximo), em graus. Positivo olha para baixo.</summary>
        Vector2 PitchLimits { get; }

        /// <summary>Chamado quando o modo passa a ser o ativo.</summary>
        void Enter();

        /// <summary>Liga ou desliga a mira (cada modo decide como aproxima a visão).</summary>
        void SetAiming(bool aiming);

        /// <summary>Posiciona e gira a câmera. yaw e pitch em graus.</summary>
        void UpdateCamera(Transform camera, float yaw, float pitch, float deltaTime);
    }
}
