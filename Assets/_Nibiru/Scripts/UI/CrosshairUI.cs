using Nibiru.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>Mira no centro da tela. Aparece só enquanto a Elena mira e fica vermelha sobre um alvo.</summary>
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] PlayerWeapons weapons;

        [Tooltip("Gráfico da mira (texto ou imagem).")]
        [SerializeField] Graphic crosshair;

        [SerializeField] Color normalColor = new Color(1f, 0.95f, 0.85f, 0.9f);
        [SerializeField] Color onTargetColor = new Color(1f, 0.3f, 0.25f, 1f);

        void LateUpdate()
        {
            crosshair.enabled = weapons.IsAiming;
            if (weapons.IsAiming)
                crosshair.color = weapons.AimOnTarget ? onTargetColor : normalColor;
        }
    }
}
