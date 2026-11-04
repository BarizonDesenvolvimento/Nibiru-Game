using Nibiru.Localization;
using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>A vaga de botão de um poder (veja o mapa de controles em Docs/Plano-ME1-Dados-Posse-Controles.md).</summary>
    public enum PowerSlot
    {
        /// <summary>Teclado Q; controle LB tocado e solto (ou Y sozinho, no plano B).</summary>
        Poder1 = 1,
        /// <summary>Teclado Z ou Mouse 4; controle LB + RB.</summary>
        Poder2 = 2,
        /// <summary>Teclado X ou Mouse 5; controle LB + Y.</summary>
        Poder3 = 3,
    }

    /// <summary>
    /// Um poder Anunnaki (um ME), como arquivo em Data/Powers. A classe base tem o que todo poder tem: código, textos,
    /// vaga de botão, recarga, duração e quando pode ser usado. Os valores próprios de cada poder ficam numa classe
    /// filha (o Eco usa <see cref="EchoPowerDefinition"/>). O efeito fica num <see cref="PowerBehaviour"/> na Elena, e
    /// quem diz se a Elena tem o poder é o <see cref="PowerBook"/>. Não há custo por uso: o único limite é a recarga.
    /// </summary>
    public abstract class PowerDefinition : ScriptableObject
    {
        [Tooltip("Código permanente do poder (poder.<nome>), para o salvamento. Nunca muda, mesmo se o arquivo for renomeado. Dado por Nibiru > Salvamento > Criar registro e códigos.")]
        public string id;

        /// <summary>O código permanente (o campo id, ou o nome do arquivo se ainda estiver vazio).</summary>
        public string Id => string.IsNullOrEmpty(id) ? name : id;

        [Tooltip("Chaves dos textos na tabela de idiomas (power.<nome>.name, .desc e .hint). A dica pode ter {0} para o botão.")]
        [LocKey] public string nameKey;
        [LocKey] public string descriptionKey;
        [LocKey] public string hintKey;

        [Tooltip("Nome mostrado na tela. Depois da extração fica vazio: o texto mora na tabela de idiomas.")]
        public string displayName;

        [Tooltip("Texto curto explicando o que o poder faz. Depois da extração fica vazio.")]
        [TextArea(2, 4)] public string description;

        public string LocalizedName => Loc.Get(nameKey, displayName);

        public string LocalizedDescription => Loc.Get(descriptionKey, description);

        /// <summary>A dica de uso, com o nome do botão no lugar do {0}.</summary>
        public string LocalizedHint(string buttonLabel) => string.Format(Loc.Get(hintKey), buttonLabel);

        [Tooltip("Ícone para o HUD (ME5). Opcional por enquanto.")]
        public Sprite icon;

        [Tooltip("Qual botão dispara o poder.")]
        public PowerSlot slot = PowerSlot.Poder1;

        [Tooltip("Tempo de recarga entre usos, em segundos de jogo (não anda com o jogo pausado, em menus nem no inventário).")]
        [Min(0f)] public float cooldown = 12f;

        [Tooltip("Quanto tempo o poder fica ativo depois de usado, em segundos. 0 = instantâneo.")]
        [Min(0f)] public float duration;

        [Tooltip("Pode ser usado mirando.")]
        public bool usableWhileAiming = true;

        [Tooltip("Pode ser usado agachada.")]
        public bool usableWhileCrouched = true;

        [Tooltip("Pode ser usado durante a recarga da arma.")]
        public bool usableWhileReloading = true;

        /// <summary>O evento antigo que marcava a posse antes do ME1 ("Poder:" + nome do arquivo). Só para converter saves e atalhos antigos.</summary>
        public string LegacyEventId => "Poder:" + name;

        /// <summary>True se a Elena já tem este poder nesta partida.</summary>
        public bool IsUnlocked => PowerBook.Has(this);

        public void Unlock() => PowerBook.Grant(this);
    }
}
