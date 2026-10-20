using UnityEngine;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra um aviso na tela a partir de um evento do Inspector (uma sequência, uma área,
    /// um StoryEventListener). O texto vai no próprio evento.
    /// </summary>
    public class NotificationPoster : MonoBehaviour
    {
        public void Post(string message) => Notifications.Post(message);
    }
}
