using Nibiru.Interaction;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra na tela o botão e o verbo do objeto interativo atual, por exemplo "[E]  Pegar".
    /// Sem objeto em foco, mostra o golpe pelas costas quando a Elena está atrás de um soldado
    /// distraído, por exemplo "[F]  Golpe pelas costas".
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Faca da Elena, para o aviso do golpe pelas costas. Vazio = procura no jogador.")]
        [SerializeField] Combat.PlayerMeleeAttack melee;

        [Tooltip("Texto do aviso do golpe pelas costas.")]
        [SerializeField] string takedownPrompt = "Golpe pelas costas";

        [Tooltip("Painel que aparece e some junto com o aviso.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text label;

        void Awake()
        {
            if (melee == null && interactor != null) melee = interactor.GetComponent<Combat.PlayerMeleeAttack>();
        }

        void LateUpdate()
        {
            IInteractable target = interactor.Current;
            bool takedown = target == null && melee != null && melee.TakedownTarget != null;
            bool show = target != null || takedown;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (target != null) label.text = $"[{input.InteractButtonLabel}]  {target.Prompt}";
            else if (takedown) label.text = $"[{input.AttackButtonLabel}]  {takedownPrompt}";
        }
    }
}
