using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Um acontecimento da história ("PegouPistola", "ViuOResgate", "LiderAbatido").
    /// É só um nome em forma de arquivo (em Data/Events), para não depender de texto digitado:
    /// gatilhos disparam o evento e missões e cenas reagem a ele.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Missões/Evento da História", fileName = "NovoEvento")]
    public class StoryEvent : ScriptableObject
    {
        [Tooltip("Anotação para quem edita: quando este evento acontece.")]
        [TextArea(1, 3)] public string note;

        public string Id => name;
    }
}
