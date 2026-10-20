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

        [Tooltip("Velocidade andando enquanto mira, em metros por segundo.")]
        [Min(0f)] public float aimWalkSpeed = 1.6f;

        [Header("Correr")]
        [Tooltip("Velocidade ao correr, em metros por segundo.")]
        [Min(0f)] public float runSpeed = 6f;

        [Header("Esquivar")]
        [Tooltip("Distância percorrida na esquiva, em metros.")]
        [Min(0f)] public float dodgeDistance = 3.5f;

        [Tooltip("Duração da esquiva, em segundos. Começa rápida e desacelera até parar.")]
        [Min(0.05f)] public float dodgeDuration = 0.4f;

        [Tooltip("Tempo de espera depois de uma esquiva antes de poder esquivar de novo, em segundos.")]
        [Min(0f)] public float dodgeCooldown = 0.3f;

        [Tooltip("Quanto tempo, a partir do início da esquiva, o jogador não leva dano, em segundos.")]
        [Min(0f)] public float dodgeInvulnerableTime = 0.3f;

        [Header("Gravidade")]
        [Tooltip("Aceleração da gravidade (m/s²). Negativo puxa para baixo.")]
        public float gravity = -20f;
    }
}
