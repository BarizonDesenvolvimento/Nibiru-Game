using UnityEngine;
using UnityEngine.InputSystem;

namespace Nibiru.Player
{
    /// <summary>
    /// Lê o mapa de controles "Jogador" e expõe os valores para os outros sistemas.
    /// Movimento, câmera e combate leem daqui, nunca direto do teclado ou do controle.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Arquivo de controles (.inputactions) com o mapa \"Jogador\".")]
        [SerializeField] InputActionAsset actions;

        InputActionMap map;
        InputAction move;
        InputAction look;
        InputAction run;
        InputAction dodge;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool RunHeld => run.IsPressed();
        public bool DodgePressed => dodge.WasPressedThisFrame();

        void Awake()
        {
            map = actions.FindActionMap("Jogador", throwIfNotFound: true);
            move = map.FindAction("Mover", throwIfNotFound: true);
            look = map.FindAction("Olhar", throwIfNotFound: true);
            run = map.FindAction("Correr", throwIfNotFound: true);
            dodge = map.FindAction("Esquivar", throwIfNotFound: true);
        }

        void OnEnable() => map.Enable();

        void OnDisable() => map.Disable();
    }
}
