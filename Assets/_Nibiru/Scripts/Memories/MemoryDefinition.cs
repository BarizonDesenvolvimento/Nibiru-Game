using Nibiru.Localization;
using Nibiru.Quests;
using UnityEngine;

namespace Nibiru.Memories
{
    /// <summary>
    /// Uma memória jogável, como arquivo em Data/Memories: a cena do passado, quem a Elena é
    /// dentro dela e o evento que a encerra. Ao decifrar certas tabuletas, o jogador vive
    /// alguns minutos no passado (Ziusudra, um soldado híbrido, Gilgamesh...).
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Memórias/Memória jogável", fileName = "NovaMemoria")]
    public class MemoryDefinition : ScriptableObject
    {
        [Tooltip("Nome da cena do passado (precisa estar na lista de cenas do Build).")]
        public string sceneName;

        [Tooltip("Chaves dos textos na tabela de idiomas (memory.<nome>.character e .place).")]
        [LocKey] public string characterNameKey;
        [LocKey] public string placeTitleKey;

        [Tooltip("Quem a Elena é na memória, mostrado na abertura (\"Você é Ziusudra\"). Depois da extração fica vazio.")]
        public string characterName;

        [Tooltip("Lugar e época, mostrados na abertura. Depois da extração fica vazio.")]
        public string placeTitle;

        public string LocalizedCharacterName => Loc.Get(characterNameKey, characterName);

        public string LocalizedPlaceTitle => Loc.Get(placeTitleKey, placeTitle);

        [Tooltip("Evento disparado ao entrar na memória (opcional).")]
        public StoryEvent raiseOnEnter;

        [Tooltip("A memória termina quando este evento acontece.")]
        public StoryEvent endOn;

        [Tooltip("Segundos entre o evento final e o começo da volta.")]
        [Min(0f)] public float endDelay = 1.5f;

        [Tooltip("Cor da tela na passagem entre o presente e o passado.")]
        public Color fadeColor = new Color(0.72f, 0.82f, 1f);

        [Tooltip("Tempo de cada escurecer ou clarear da passagem, em segundos.")]
        [Min(0.1f)] public float fadeTime = 1.2f;
    }
}
