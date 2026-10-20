using Nibiru.Player;
using Nibiru.Powers;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>Mostra o Eco do Abzu e se ele está pronto ou recarregando. Some até a Elena ganhar o poder.</summary>
    public class PowerHUD : MonoBehaviour
    {
        [SerializeField] EchoOfAbzu echo;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Text label;

        const string Lapis = "#8FB4FF";
        const string Muted = "#9AA0AA";

        void Update()
        {
            label.enabled = echo.IsUnlocked;
            if (!echo.IsUnlocked) return;

            float left = echo.CooldownLeft;
            string state = left > 0f
                ? $"<color={Muted}>recarregando {Mathf.CeilToInt(left)}s</color>"
                : $"[{input.PowerButtonLabel}] pronto";
            label.text = $"<color={Lapis}>{echo.Power.displayName.ToUpperInvariant()}</color>   {state}";
        }
    }
}
