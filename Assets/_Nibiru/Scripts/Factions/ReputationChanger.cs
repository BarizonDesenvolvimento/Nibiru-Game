using UnityEngine;

namespace Nibiru.Factions
{
    /// <summary>
    /// Muda reputações quando chamado por um evento do Inspector (por exemplo, um
    /// StoryEventListener reagindo a uma escolha da história).
    /// </summary>
    public class ReputationChanger : MonoBehaviour
    {
        [SerializeField] ReputationChange[] changes = new ReputationChange[0];

        public void Apply() => Reputation.Apply(changes);
    }
}
