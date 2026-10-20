using System;

namespace Nibiru.UI
{
    /// <summary>
    /// Mensagens curtas para o jogador ("Curativo x2", "Inventário cheio", "Guardiões +15").
    /// Qualquer sistema chama Notifications.Post; o NotificationUI mostra na tela.
    /// </summary>
    public static class Notifications
    {
        public static event Action<string> Posted;

        public static void Post(string message) => Posted?.Invoke(message);
    }
}
