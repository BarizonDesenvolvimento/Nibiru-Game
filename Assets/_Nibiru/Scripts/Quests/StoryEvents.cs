using System;
using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Quests
{
    /// <summary>
    /// Dispara eventos da história e lembra quais já aconteceram nesta partida
    /// (o checkpoint guarda isso junto com o resto do mundo).
    /// </summary>
    public static class StoryEvents
    {
        public static event Action<StoryEvent> Raised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Raised = null;

        public static void Raise(StoryEvent storyEvent)
        {
            if (storyEvent == null) return;
            WorldState.MarkStoryEvent(storyEvent.Id);
            Raised?.Invoke(storyEvent);
        }

        public static bool HasBeenRaised(StoryEvent storyEvent) =>
            storyEvent != null && WorldState.HasStoryEvent(storyEvent.Id);
    }
}
