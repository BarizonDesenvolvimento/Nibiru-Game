using Nibiru.Powers;
using UnityEngine;

namespace Nibiru.Testes
{
    /// <summary>
    /// Efeito de teste: só conta quantas vezes o poder de teste disparou. Acrescentado na Elena no Play pelos testes
    /// automáticos e pelos atalhos do LB (nunca fica no prefab).
    /// </summary>
    public class TestPowerBehaviour : PowerBehaviour
    {
        public PowerDefinition definition;
        public int activations;

        public override PowerDefinition Definition => definition;

        public override void Activate() => activations++;
    }
}
