using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Valores de um Lullu (servo dos deuses). O comum e o chefe Primeiro Servo usam arquivos
    /// diferentes em Data/AI; o chefe liga a investida e a armadura.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/IA/Lullu", fileName = "Lullu")]
    public class LulluSettings : ScriptableObject
    {
        [Header("Movimento")]
        [Tooltip("Velocidade vagando sem rumo, em m/s.")]
        [Min(0f)] public float wanderSpeed = 1.2f;

        [Tooltip("Distância máxima que ele vagueia em volta de onde começou, em metros.")]
        [Min(0f)] public float wanderRadius = 4f;

        [Tooltip("Velocidade indo ver um som, em m/s.")]
        [Min(0f)] public float investigateSpeed = 2.6f;

        [Tooltip("Velocidade caçando a Elena, em m/s. Correr (6 m/s) é mais rápido, mas faz barulho.")]
        [Min(0f)] public float huntSpeed = 5.5f;

        [Header("Sentidos")]
        [Tooltip("Multiplica o alcance dos sons para ele (é quase cego, mas ouve bem).")]
        [Min(0f)] public float hearingMultiplier = 1.2f;

        [Tooltip("Abaixo desta distância ele sente a Elena mesmo sem som, em metros.")]
        [Min(0f)] public float touchRange = 1.2f;

        [Tooltip("Segundos sem ouvir nada até desistir e voltar a vagar.")]
        [Min(0f)] public float forgetTime = 6f;

        [Header("Ataque")]
        [Tooltip("Distância para começar o golpe, em metros.")]
        [Min(0f)] public float attackRange = 1.9f;

        [Tooltip("Preparação do golpe, em segundos (o corpo encolhe: é o aviso para esquivar).")]
        [Min(0f)] public float attackWindup = 0.5f;

        [Tooltip("Tempo parado depois do golpe, em segundos.")]
        [Min(0f)] public float attackRecovery = 0.8f;

        [Min(0f)] public float attackDamage = 25f;

        [Tooltip("Raio da área do golpe, em metros.")]
        [Min(0.1f)] public float attackRadius = 1f;

        [Header("Eco do Abzu")]
        [Tooltip("Tempo atordoado pelo Eco, em segundos.")]
        [Min(0f)] public float stunDuration = 3f;

        [Header("Armadura")]
        [Tooltip("Multiplicador de dano normal (1 = sem armadura; o chefe usa 0,25).")]
        [Min(0f)] public float damageMultiplier = 1f;

        [Tooltip("Multiplicador de dano enquanto atordoado (a guarda abre).")]
        [Min(0f)] public float stunnedDamageMultiplier = 1.5f;

        [Header("Investida (chefe)")]
        public bool canLunge;

        [Tooltip("Distância mínima e máxima para a investida, em metros.")]
        public Vector2 lungeRange = new Vector2(3.5f, 9f);

        [Tooltip("Preparação da investida, em segundos.")]
        [Min(0f)] public float lungeWindup = 0.6f;

        [Tooltip("Velocidade da investida, em m/s.")]
        [Min(0f)] public float lungeSpeed = 14f;

        [Tooltip("Duração da investida, em segundos.")]
        [Min(0.05f)] public float lungeDuration = 0.4f;

        [Min(0f)] public float lungeDamage = 30f;

        [Tooltip("Tempo entre investidas, em segundos.")]
        [Min(0f)] public float lungeCooldown = 4f;

        [Tooltip("Tempo parado depois da investida, em segundos (a hora de atacar).")]
        [Min(0f)] public float lungeRecovery = 1.2f;
    }
}
