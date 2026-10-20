using Nibiru.Inventory;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Uma arma de fogo (pistola, espingarda...). Cada arma é um arquivo destes em Data/Combat,
    /// ligado ao item da arma e ao item de munição que ela usa.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Combate/Arma de Fogo", fileName = "NovaArma")]
    public class FirearmDefinition : ScriptableObject
    {
        [Tooltip("Item da arma no inventário.")]
        public ItemDefinition weaponItem;

        [Tooltip("Item de munição que a arma usa.")]
        public ItemDefinition ammoItem;

        [Tooltip("Quantas balas cabem na arma de uma vez.")]
        [Min(1)] public int magazineSize = 8;

        [Header("Tiro")]
        [Tooltip("Dano de cada projétil. A espingarda solta vários por tiro.")]
        [Min(0f)] public float damagePerPellet = 34f;

        [Tooltip("Projéteis por tiro: 1 na pistola, vários na espingarda.")]
        [Min(1)] public int pellets = 1;

        [Tooltip("Abertura dos projéteis em volta da mira, em graus. Maior = menos preciso.")]
        [Min(0f)] public float spread = 0.6f;

        [Tooltip("Alcance máximo, em metros.")]
        [Min(1f)] public float range = 60f;

        [Tooltip("Tempo mínimo entre tiros, em segundos.")]
        [Min(0.05f)] public float fireInterval = 0.22f;

        [Tooltip("Tempo para recarregar, em segundos.")]
        [Min(0.1f)] public float reloadTime = 1.4f;

        [Tooltip("Coice: quantos graus a mira sobe a cada tiro.")]
        [Min(0f)] public float recoil = 1.5f;

        [Tooltip("Até onde o tiro é ouvido, em metros.")]
        [Min(0f)] public float noiseRadius = 25f;
    }
}
