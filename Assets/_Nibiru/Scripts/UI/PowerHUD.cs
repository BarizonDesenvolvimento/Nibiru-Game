using System.Text;
using Nibiru.Localization;
using Nibiru.Powers;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// HUD provisório dos poderes (texto): uma linha por poder que a Elena tem, com o botão e a recarga. Some até ela ter
    /// algum. O HUD definitivo, com um ícone e um anel de recarga por ME, é do ME5; ele lê o mesmo PlayerPowers.
    /// </summary>
    public class PowerHUD : MonoBehaviour
    {
        [Tooltip("Efeito do Eco na Elena: o HUD acha o PlayerPowers no mesmo objeto.")]
        [SerializeField] EchoOfAbzu echo;
        [SerializeField] Text label;

        const string Lapis = "#8FB4FF";
        const string Muted = "#9AA0AA";

        PlayerPowers powers;
        readonly StringBuilder text = new StringBuilder();

        void Update()
        {
            if (powers == null && echo != null) powers = echo.GetComponent<PlayerPowers>();
            var owned = PowerBook.Owned;
            label.enabled = powers != null && owned.Count > 0;
            if (!label.enabled) return;

            text.Clear();
            foreach (PowerDefinition power in owned)
            {
                if (power.name.StartsWith("Teste_")) continue;
                float left = PowerBook.CooldownLeft(power);
                string state = left > 0f
                    ? $"<color={Muted}>{string.Format(Loc.Get("ui.hud.poder.recarregando", "recarregando {0}s"), Mathf.CeilToInt(left))}</color>"
                    : string.Format(Loc.Get("ui.hud.poder.pronto", "[{0}] pronto"), powers.ButtonLabel(power.slot));
                if (text.Length > 0) text.Append('\n');
                text.Append($"<color={Lapis}>{power.LocalizedName.ToUpperInvariant()}</color>   {state}");
            }
            label.text = text.ToString();
            label.enabled = text.Length > 0;
        }
    }
}
