using Nibiru.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra a arma equipada e a munição: balas na arma / balas no inventário.
    /// Some quando não há arma equipada.
    /// </summary>
    public class AmmoUI : MonoBehaviour
    {
        [SerializeField] PlayerWeapons weapons;
        [SerializeField] Text label;

        const string Gold = "#E8C46A";
        const string Danger = "#F2584A";

        void LateUpdate()
        {
            FirearmDefinition firearm = weapons.Equipped;
            label.enabled = firearm != null;
            if (firearm == null) return;

            string loaded = weapons.LoadedRounds == 0
                ? $"<color={Danger}>0</color>"
                : weapons.LoadedRounds.ToString();
            string status = weapons.IsReloading ? "   <size=22>RECARREGANDO</size>" : string.Empty;
            label.text = $"<size=22>{firearm.weaponItem.LocalizedName.ToUpperInvariant()}</size>{status}\n" +
                         $"<b>{loaded}</b> <color={Gold}>/ {weapons.ReserveRounds}</color>";
        }
    }
}
