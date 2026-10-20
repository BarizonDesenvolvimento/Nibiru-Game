using Nibiru.Interaction;
using Nibiru.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Nibiru.UI
{
    /// <summary>
    /// Mostra na tela o botão e o verbo do objeto interativo atual, por exemplo "[E]  Pegar".
    /// Fica num objeto que não some (o Canvas); o painel filho é que liga e desliga.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerInputReader input;

        [Tooltip("Painel que aparece e some junto com o aviso.")]
        [SerializeField] GameObject panel;
        [SerializeField] Text label;

        void LateUpdate()
        {
            IInteractable target = interactor.Current;
            bool show = target != null;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (show) label.text = $"[{input.InteractButtonLabel}]  {target.Prompt}";
        }
    }
}
