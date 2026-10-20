using Nibiru.Combat;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Tela de morte. Aparece quando a vida da Elena chega a zero e, depois de um
    /// instante, deixa o jogador tentar de novo com o botão de confirmar.
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] PlayerDeathHandler deathHandler;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Painel que aparece na morte.")]
        [SerializeField] GameObject panel;

        [Tooltip("Texto com o botão para tentar de novo.")]
        [SerializeField] Text hint;

        [Tooltip("Espera antes de aceitar o botão, para o jogador não pular a tela sem querer.")]
        [SerializeField, Min(0f)] float delayBeforeRetry = 1.5f;

        float shownAt;

        void Awake() => panel.SetActive(false);

        void OnEnable() => health.Died += Show;

        void OnDisable() => health.Died -= Show;

        void Show()
        {
            panel.SetActive(true);
            shownAt = Time.unscaledTime;
            hint.text = string.Empty;
        }

        void Update()
        {
            if (!panel.activeSelf || Time.unscaledTime - shownAt < delayBeforeRetry) return;

            hint.text = $"[{input.ConfirmButtonLabel}]  Tentar de novo";
            if (input.ConfirmPressed) deathHandler.Restart();
        }
    }
}
