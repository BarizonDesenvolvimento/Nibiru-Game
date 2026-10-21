using Nibiru.Combat;
using Nibiru.Save;
using UnityEngine;

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

            // Por enquanto a Elena só tomba de bruços (falta a animação de morte). O Jogador fica no
            // meio do corpo, então desce quase até o chão para o corpo deitado não flutuar.
            transform.SetPositionAndRotation(
                transform.position + Vector3.down * 0.85f,
                Quaternion.Euler(90f, transform.eulerAngles.y, 0f));
        }

        /// <summary>Tenta de novo a partir do último checkpoint (ou do começo da cena, se não houver).</summary>
        public void Restart() => Checkpoints.Restart(gameObject.scene.name);
    }
}
