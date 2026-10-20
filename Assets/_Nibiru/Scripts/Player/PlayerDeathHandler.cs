using Nibiru.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.Player
{
    /// <summary>
    /// O que acontece quando a vida da Elena chega a zero: os controles desligam e ela cai.
    /// A tela de morte chama Restart para tentar de novo.
    /// </summary>
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            input.SetGameplayEnabled(false);
            motor.enabled = false;
            GetComponent<CharacterController>().enabled = false;

            // Por enquanto a cápsula só tomba; com o modelo da Elena entra a animação de morte.
            transform.SetPositionAndRotation(
                transform.position + Vector3.down * 0.5f,
                Quaternion.Euler(90f, transform.eulerAngles.y, 0f));
        }

        /// <summary>Tenta de novo. Até existirem checkpoints, recomeça a cena do início.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.name);
        }
    }
}
