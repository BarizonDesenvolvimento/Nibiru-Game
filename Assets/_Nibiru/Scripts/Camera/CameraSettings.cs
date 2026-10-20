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

        [Header("Colisão")]
        [Tooltip("Raio da esfera usada para a câmera não atravessar paredes, em metros.")]
        [Min(0.01f)] public float collisionRadius = 0.2f;

        [Tooltip("Camadas que empurram a câmera para perto do personagem.")]
        public LayerMask collisionMask = Physics.DefaultRaycastLayers;

        [Tooltip("Velocidade com que a câmera volta para a distância normal depois de sair de perto de uma parede, em m/s.")]
        [Min(0f)] public float returnSpeed = 4f;
    }
}
