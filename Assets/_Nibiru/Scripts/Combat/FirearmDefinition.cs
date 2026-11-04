using Nibiru.Inventory;
using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Uma arma de fogo (pistola, espingarda...). Cada arma é um arquivo destes em Data/Combat,
    /// ligado ao item da arma e ao item de munição que ela usa.
    /// </summary>
    /// <summary>Como a Elena segura a arma: decide a animação de mira e de recarga (a pistola e a espingarda são de duas mãos, com poses diferentes).</summary>
    public enum HoldStyle
    {
        Pistola,
        Longa,
    }

    [CreateAssetMenu(menuName = "Nibiru/Combate/Arma de Fogo", fileName = "NovaArma")]
    public class FirearmDefinition : ScriptableObject
    {
        [Tooltip("Código permanente da arma no arquivo de salvamento (arma.<nome>). Nunca muda, mesmo se o arquivo for renomeado. Preenchido por Nibiru > Salvamento > Criar registro e códigos.")]
        public string id;

        /// <summary>O código permanente usado pelo salvamento em disco.</summary>
        public string Id => id;

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

        [Header("Troca de arma (B6)")]
        [Tooltip("Lugar da arma na troca: 1 = tecla 1 / primeira, 2 = tecla 2 / segunda.")]
        [Min(1)] public int slot = 1;

        [Tooltip("Tempo da troca para esta arma, em segundos. Sem atirar nem recarregar durante ela.")]
        [Min(0f)] public float swapTime = 0.4f;

        [Header("Distância (o dano cai com o alcance)")]
        [Tooltip("Até quantos metros o dano é cheio. 0 = sem queda de dano (pistola).")]
        [Min(0f)] public float falloffStart = 0f;

        [Tooltip("Fração do dano no alcance máximo (0,35 = 35%). Só vale com a queda ligada.")]
        [Range(0f, 1f)] public float minDamageFactor = 1f;

        [Header("Recarga cartucho a cartucho")]
        [Tooltip("Ligado: recarrega um cartucho por vez e dá para interromper sem perder o que já entrou. Desligado: o pente todo no fim do tempo de recarga.")]
        public bool reloadPerRound = false;

        [Tooltip("Tempo de abrir a arma antes do primeiro cartucho (segundos).")]
        [Min(0f)] public float reloadOpenTime = 0.5f;

        [Tooltip("Tempo de cada cartucho (segundos).")]
        [Min(0.05f)] public float reloadRoundTime = 0.8f;

        [Header("Empurrão no alvo")]
        [Tooltip("Até quantos metros o tiro faz o alvo cambalear. 0 = nunca.")]
        [Min(0f)] public float staggerRange = 0f;

        [Tooltip("Quanto tempo o alvo fica cambaleando (segundos).")]
        [Min(0f)] public float staggerTime = 0.8f;

        [Tooltip("Até quantos metros o tiro pode derrubar. 0 = nunca derruba.")]
        [Min(0f)] public float knockdownRange = 0f;

        [Tooltip("Quantos projéteis precisam acertar o mesmo alvo para derrubar.")]
        [Min(1)] public int knockdownPellets = 6;

        [Tooltip("Quanto tempo o alvo derrubado leva para levantar (segundos).")]
        [Min(0f)] public float knockdownTime = 1.5f;

        [Header("Empunhadura (B6)")]
        [Tooltip("Pistola: pose de mira e recarga da pistola. Longa: pose de arma de duas mãos (espingarda).")]
        public HoldStyle holdStyle = HoldStyle.Pistola;

        [Header("Som")]
        public AudioClip fireSound;

        /// <summary>Fator do dano (1 = cheio) para um acerto a esta distância.</summary>
        public float DamageFactor(float distance)
        {
            if (falloffStart <= 0f || distance <= falloffStart || range <= falloffStart) return 1f;
            return Mathf.Lerp(1f, minDamageFactor, Mathf.Clamp01((distance - falloffStart) / (range - falloffStart)));
        }
    }
}
