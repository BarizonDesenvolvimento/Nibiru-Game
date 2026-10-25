using UnityEngine;

namespace Nibiru.Combat
{
    /// <summary>
    /// Inimigo que pode ser derrubado de uma vez pela faca, pelas costas (os soldados da Fundação).
    /// Quem ataca confere se está atrás dele; o alvo diz se está distraído o bastante.
    /// </summary>
    public interface ITakedownTarget
    {
        /// <summary>True se ele não está em combate e não viu a Elena há pouco tempo.</summary>
        bool CanBeTakenDown { get; }

        Transform transform { get; }

        /// <summary>Cai de frente, morto, sem dar alarme.</summary>
        void TakeDown(GameObject attacker);
    }
}
