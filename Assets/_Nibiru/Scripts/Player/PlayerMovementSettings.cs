using UnityEngine;

namespace Nibiru.Player
{
    /// <summary>
    /// Valores de movimento do jogador. Fica num arquivo separado para poder
    /// ajustar durante o Play sem perder as mudanças ao sair.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Jogador/Configuração de Movimento", fileName = "MovimentoJogador")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("Andar")]
        [Tooltip("Velocidade ao andar, em metros por segundo.")]
        [Min(0f)] public float walkSpeed = 3f;

        [Tooltip("Quão rápido ganha e perde velocidade (m/s²). Maior = mais responsivo.")]
        [Min(0f)] public float acceleration = 20f;

        [Tooltip("Velocidade com que o corpo vira para a direção do movimento, em graus por segundo.")]
        [Min(0f)] public float turnSpeed = 720f;

        [Header("Correr")]
        [Tooltip("Velocidade ao correr, em metros por segundo.")]
        [Min(0f)] public float runSpeed = 6f;

        [Header("Gravidade")]
        [Tooltip("Aceleração da gravidade (m/s²). Negativo puxa para baixo.")]
        public float gravity = -20f;
    }
}
