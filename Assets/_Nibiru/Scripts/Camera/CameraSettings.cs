using UnityEngine;

namespace Nibiru.Cameras
{
    /// <summary>
    /// Valores da câmera do jogador. Fica num arquivo separado para poder
    /// ajustar durante o Play sem perder as mudanças ao sair.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Câmera/Configuração de Câmera", fileName = "CameraJogador")]
    public class CameraSettings : ScriptableObject
    {
        [Header("Sensibilidade")]
        [Tooltip("Graus girados por pixel de movimento do mouse.")]
        [Min(0f)] public float mouseSensitivity = 0.1f;

        [Tooltip("Graus por segundo com o analógico direito todo inclinado (x = lados, y = cima/baixo).")]
        public Vector2 gamepadSpeed = new Vector2(180f, 120f);

        [Tooltip("Inverte o eixo vertical (cima olha para baixo).")]
        public bool invertY;

        [Tooltip("Inclinação vertical inicial, em graus. Positivo olha para baixo.")]
        public float startPitch = 10f;

        [Header("Terceira pessoa")]
        [Tooltip("Altura do ponto que a câmera segue, acima do centro do personagem, em metros.")]
        public float pivotHeight = 0.6f;

        [Tooltip("Deslocamento para o lado do ombro, em metros. Positivo = ombro direito.")]
        public float shoulderOffset = 0.6f;

        [Tooltip("Distância atrás do ombro, em metros.")]
        [Min(0f)] public float distance = 2.5f;

        [Tooltip("Limite de olhar para cima (negativo) e para baixo (positivo), em graus.")]
        public Vector2 thirdPersonPitchLimits = new Vector2(-35f, 60f);

        [Header("Mira")]
        [Tooltip("Distância atrás do ombro enquanto mira, em metros.")]
        [Min(0f)] public float aimDistance = 1.2f;

        [Tooltip("Deslocamento para o lado do ombro enquanto mira, em metros.")]
        public float aimShoulderOffset = 0.75f;

        [Tooltip("Campo de visão enquanto mira, em graus. Menor = mais zoom.")]
        [Range(20f, 90f)] public float aimFieldOfView = 48f;

        [Tooltip("Rapidez da transição para a mira e de volta (1 = um segundo, 6 = um sexto de segundo).")]
        [Min(0.1f)] public float aimBlendSpeed = 6f;

        [Tooltip("Sensibilidade enquanto mira, como fração da normal. Menor = mira mais fina.")]
        [Range(0.1f, 1f)] public float aimSensitivityMultiplier = 0.6f;

        [Header("Colisão")]
        [Tooltip("Raio da esfera usada para a câmera não atravessar paredes, em metros.")]
        [Min(0.01f)] public float collisionRadius = 0.2f;

        [Tooltip("Camadas que empurram a câmera para perto do personagem.")]
        public LayerMask collisionMask = Physics.DefaultRaycastLayers;

        [Tooltip("Velocidade com que a câmera volta para a distância normal depois de sair de perto de uma parede, em m/s.")]
        [Min(0f)] public float returnSpeed = 4f;
    }
}
