using Nibiru.Save;
using UnityEngine;

namespace Nibiru.Tablets
{
    /// <summary>
    /// Soma transformação sem avisar: nenhum aviso na tela, nenhuma fala, nenhum evento de mudança
    /// (<see cref="Transformation.Changed"/> não é chamado). Para ganhos escondidos, como ficar com a
    /// tabuleta no wadi: a única pista é o que a Voz diz. A barra de transformação, que lê o valor
    /// sozinha, continua mostrando o total de sempre. Chamado por um evento da história no Inspector.
    /// </summary>
    public class SilentTransformationGain : MonoBehaviour
    {
        [SerializeField, Range(-100, 100)] int amount = 3;

        public void Apply() =>
            WorldState.Transformation = Mathf.Clamp(WorldState.Transformation + amount, 0, Transformation.Max);
    }
}
