using Nibiru.Powers;
using UnityEngine;

namespace Nibiru.Testes
{
    /// <summary>
    /// Poder só para testes (arquivos Data/Powers/Teste_*): ocupa uma vaga para os testes automáticos e os atalhos do LB
    /// conferirem as combinações de botão antes de o Sopro (ME3) e a Égide (ME4) terem efeito. Nunca é dado no jogo de verdade.
    /// </summary>
    [CreateAssetMenu(menuName = "Nibiru/Poderes/Poder de teste", fileName = "Teste_Poder")]
    public class TestPowerDefinition : PowerDefinition
    {
    }
}
