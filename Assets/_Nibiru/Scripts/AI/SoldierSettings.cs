using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Valores de um tipo de soldado (contenção, líder...). Cada tipo é um arquivo destes
    /// em Data/AI, para ajustar visão, audição e combate sem mexer em código.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/IA/Soldado", fileName = "Soldado")]
    public class SoldierSettings : ScriptableObject
    {
        [Header("Movimento")]
        [Tooltip("Velocidade patrulhando e investigando, em m/s.")]
        [Min(0f)] public float patrolSpeed = 1.6f;

        [Tooltip("Velocidade em combate e buscando, em m/s.")]
        [Min(0f)] public float chaseSpeed = 4.2f;

        [Tooltip("Tempo parado em cada ponto da patrulha, em segundos.")]
        [Min(0f)] public float waitAtWaypoint = 1.5f;

        [Tooltip("Velocidade para virar o corpo, em graus por segundo.")]
        [Min(0f)] public float turnSpeed = 360f;

        [Header("Visão")]
        [Tooltip("Até onde enxerga, em metros.")]
        [Min(0f)] public float viewDistance = 18f;

        [Tooltip("Abertura da visão, em graus (o cone à frente dele).")]
        [Range(10f, 360f)] public float viewAngle = 110f;

        [Tooltip("Altura dos olhos acima do centro do soldado, em metros.")]
        public float eyeHeight = 0.7f;

        [Tooltip("Segundos vendo a Elena em média distância até perceber de vez.")]
        [Min(0.05f)] public float timeToSpot = 1.2f;

        [Tooltip("Abaixo desta distância, percebe três vezes mais rápido.")]
        [Min(0f)] public float closeRange = 4f;

        [Tooltip("Agachada, a Elena é percebida mais devagar (1 = igual, 0,5 = metade da velocidade).")]
        [Range(0f, 1f)] public float crouchedVisibility = 0.45f;

        [Tooltip("Quanto da desconfiança some por segundo sem ver nada.")]
        [Min(0f)] public float awarenessDecay = 0.2f;

        [Header("Audição")]
        [Tooltip("Multiplica o alcance dos sons para este soldado (1 = normal, 2 = ouve o dobro).")]
        [Min(0f)] public float hearingMultiplier = 1f;

        [Header("Combate")]
        [Tooltip("Distância máxima para atirar, em metros.")]
        [Min(0f)] public float attackRange = 14f;

        [Tooltip("Distância que ele tenta manter da Elena ao atirar, em metros.")]
        [Min(0f)] public float preferredDistance = 8f;

        [Tooltip("Tempo entre tiros, em segundos.")]
        [Min(0.1f)] public float fireInterval = 1.3f;

        [Tooltip("Dano de cada tiro que acerta. A ordem é pegar a Elena viva, então o dano é baixo.")]
        [Min(0f)] public float damage = 12f;

        [Tooltip("Chance de acertar cada tiro (0 a 1).")]
        [Range(0f, 1f)] public float accuracy = 0.55f;

        [Tooltip("Até onde o tiro dele é ouvido, em metros.")]
        [Min(0f)] public float shotNoiseRadius = 25f;

        [Tooltip("Tiros antes de recarregar.")]
        [Min(1)] public int magazineSize = 8;

        [Tooltip("Tempo da recarga, em segundos (ele não atira enquanto isso).")]
        [Min(0.1f)] public float reloadTime = 2.4f;

        [Tooltip("Segundos sem ver a Elena até passar a procurar por ela.")]
        [Min(0f)] public float loseSightTime = 3f;

        [Tooltip("Segundos procurando antes de voltar à patrulha.")]
        [Min(0f)] public float searchTime = 8f;

        [Header("Líder")]
        [Tooltip("Segundos de combate até o líder chamar reforços pelo rádio. Matar o líder antes impede.")]
        [Min(0f)] public float reinforcementDelay = 3f;

        [Tooltip("Soldados até esta distância do líder atendem o chamado, em metros.")]
        [Min(0f)] public float reinforcementRadius = 30f;
    }
}
