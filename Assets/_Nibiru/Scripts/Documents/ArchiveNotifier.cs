using Nibiru.Localization;
using Nibiru.Quests;
using Nibiru.UI;
using UnityEngine;

namespace Nibiru.Documents
{
    /// <summary>
    /// Avisa uma única vez, na primeira vez que algo entra no Arquivo, que ele existe no menu de pausa
    /// ("Adicionado ao Arquivo (menu de pausa)"). Fica de ouvido nos eventos da história; o evento
    /// <see cref="ArchiveCatalog.noticeEvent"/> guarda no estado do mundo que o aviso já foi dado.
    /// </summary>
    public static class ArchiveNotifier
    {
        public const string NoticeKey = "ui.arquivo.aviso_novo";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            StoryEvents.Raised -= OnRaised;
            StoryEvents.Raised += OnRaised;
        }

        static void OnRaised(StoryEvent raised)
        {
            ArchiveCatalog catalog = ArchiveCatalog.Current;
            if (catalog == null || catalog.noticeEvent == null || raised == null) return;
            if (StoryEvents.HasBeenRaised(catalog.noticeEvent)) return;

            foreach (ArchiveCatalog.Entry entry in catalog.AllEntries())
            {
                if (entry.UnlockEvent != raised) continue;
                Notifications.Post(Loc.Get(NoticeKey, "Adicionado ao Arquivo (menu de pausa)"));
                StoryEvents.Raise(catalog.noticeEvent);
                return;
            }
        }
    }
}
