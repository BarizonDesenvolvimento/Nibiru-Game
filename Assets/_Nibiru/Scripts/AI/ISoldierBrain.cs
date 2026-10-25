using System;
using UnityEngine;

namespace Nibiru.AI
{
    /// <summary>
    /// O que o animador e o símbolo sobre a cabeça precisam saber de um soldado armado,
    /// seja o de patrulha (<see cref="SoldierAI"/>) ou o atirador de elite (<see cref="SniperAI"/>).
    /// </summary>
    public interface ISoldierBrain
    {
        /// <summary>A IA está ligada (figurantes de cena têm a IA desligada).</summary>
        bool IsActive { get; }

        SoldierAI.State CurrentState { get; }
        float Awareness { get; }

        /// <summary>Fuzil apontado (levantado) agora.</summary>
        bool IsAiming { get; }
        float ReloadTime { get; }

        /// <summary>A cada tiro (para a animação do disparo).</summary>
        event Action Fired;
        /// <summary>Quando o carregador acaba e ele começa a recarregar.</summary>
        event Action ReloadStarted;

        Transform transform { get; }
    }
}
