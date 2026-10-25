using System;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Valores da luta com o Primeiro Servo (o chefe da região 1): vida, quando muda de fase e o que
    /// muda em cada uma. Os golpes, a investida, os tempos e os sentidos dele ficam no arquivo de
    /// Lullu dele (Data/AI/PrimeiroServo); aqui ficam só os multiplicadores de cada fase.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/IA/Luta do Primeiro Servo", fileName = "PrimeiroServo_Luta")]
    public class FirstServantSettings : ScriptableObject
    {
        [Serializable]
        public class Phase
        {
            [Tooltip("Multiplica a preparação do golpe e da investida (menos que 1 = avisa menos).")]
            [Min(0.1f)] public float windupScale = 1f;

            [Tooltip("Multiplica o tempo parado depois de golpear ou investir (mais que 1 = cansa, a hora de atacar).")]
            [Min(0.1f)] public float recoveryScale = 1f;

            [Tooltip("Multiplica as velocidades de andar e caçar.")]
            [Min(0.1f)] public float speedScale = 1f;

            [Tooltip("Investidas seguidas de uma vez.")]
            [Min(1)] public int lungesInARow = 1;

            [Tooltip("Sobe as rampas e as plataformas atrás da Elena.")]
            public bool climbs;
        }

        [Header("Vida")]
        [Tooltip("Vida do Primeiro Servo. Tiro e faca tiram só uma parte (\"Armadura\" no arquivo PrimeiroServo); " +
                 "atordoado pelo Eco, tiram mais.")]
        [Min(1f)] public float maxHealth = 750f;

        [Header("Fases (fração da vida em que cada uma começa)")]
        [Range(0f, 1f)] public float phase2At = 0.6f;
        [Range(0f, 1f)] public float phase3At = 0.25f;

        [Tooltip("Fase 1: caça pelo som, presa ao chão.")]
        public Phase phase1 = new Phase();

        [Tooltip("Fase 2: a máscara racha; sobe rampas e faz duas investidas seguidas.")]
        public Phase phase2 = new Phase { lungesInARow = 2, climbs = true, speedScale = 1.05f };

        [Tooltip("Fase 3: furioso; avisa menos, mas cansa mais depois de cada investida.")]
        public Phase phase3 = new Phase { windupScale = 0.7f, recoveryScale = 1.6f, speedScale = 1.1f, lungesInARow = 2, climbs = true };

        [Header("Troca de fase e despertar")]
        [Tooltip("Segundos parado urrando ao acordar e a cada troca de fase.")]
        [Min(0f)] public float roarDuration = 1.8f;

        [Tooltip("Quanto ele sobe quando ainda não sobe rampas, em metros (como o Lullu: só degraus).")]
        [Min(0.1f)] public float groundClimb = 2f;

        [Header("Dicas na tela")]
        [TextArea] public string wakeHint = "Dica: a investida dele se atravessa com a esquiva. O Eco do Abzu o atordoa e abre a guarda";
        [TextArea] public string phase2Hint = "Dica: atire num holofote. O estrondo da queda atrai o Servo para longe de você";
    }
}
