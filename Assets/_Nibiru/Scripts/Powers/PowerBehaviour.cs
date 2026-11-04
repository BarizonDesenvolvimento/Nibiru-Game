using UnityEngine;

namespace Nibiru.Powers
{
    /// <summary>
    /// O efeito de um poder: um componente na Elena (no mesmo objeto do <see cref="PlayerPowers"/>) que sabe qual arquivo
    /// executa. Quem lê o botão, confere os requisitos e conta a recarga é o PlayerPowers; o efeito só age quando é chamado.
    /// Um ME novo cria uma classe filha desta (o Eco é o <see cref="EchoOfAbzu"/>).
    /// </summary>
    public abstract class PowerBehaviour : MonoBehaviour
    {
        /// <summary>O arquivo do poder que este componente executa.</summary>
        public abstract PowerDefinition Definition { get; }

        /// <summary>Requisito próprio do efeito, além dos comuns (por exemplo, ter um alvo). Padrão: sempre pode.</summary>
        public virtual bool CanActivateNow => true;

        /// <summary>Faz o efeito. A recarga e a duração são cuidadas pelo PlayerPowers.</summary>
        public abstract void Activate();
    }
}
