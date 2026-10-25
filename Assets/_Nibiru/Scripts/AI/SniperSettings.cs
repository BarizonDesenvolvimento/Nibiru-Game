using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// Valores do atirador de elite (Data/AI/AtiradorElite). Vale tudo o que vale para um soldado
    /// (visão, dano, intervalo dos tiros...) e mais o que é só dele: o tempo de aviso antes do
    /// primeiro tiro e a varredura do laser. Mudanças feitas com o Play ligado continuam valendo
    /// depois, porque ficam no arquivo.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/IA/Atirador de elite", fileName = "AtiradorElite")]
    public class SniperSettings : SoldierSettings
    {
        [Header("Atirador de elite")]
        [Tooltip("Segundos com o laser travado na Elena antes do primeiro tiro (tempo de correr para a cobertura). Vale de novo cada vez que ele a reencontra.")]
        [Min(0f)] public float warningTime = 1.5f;

        [Tooltip("Abertura total da varredura do laser enquanto vigia, em graus (metade para cada lado).")]
        [Range(0f, 180f)] public float sweepAngle = 80f;

        [Tooltip("Velocidade da varredura, em graus por segundo.")]
        [Min(0f)] public float sweepSpeed = 12f;

        [Tooltip("Quanto o laser aponta para baixo enquanto vigia, em graus (para cair no chão do pátio).")]
        [Range(0f, 80f)] public float sweepPitch = 28f;

        [Tooltip("Ao ver a Elena, soldados até esta distância dela são avisados pelo rádio e vão até lá, em metros.")]
        [Min(0f)] public float alertRadius = 60f;

        [Tooltip("Aviso na tela quando ele vê a Elena.")]
        public string radioMessage = "Rádio: \"Aqui é o Corvo, no alto do zigurate. Alvo no pátio!\"";
    }
}
