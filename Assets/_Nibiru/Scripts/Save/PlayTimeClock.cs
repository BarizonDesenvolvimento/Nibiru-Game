using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>Faz o <see cref="PlayTime"/> andar a cada quadro. Criado sozinho ao ligar o jogo; não vai em cena.</summary>
    public class PlayTimeClock : MonoBehaviour
    {
        void Update() => PlayTime.Tick(Time.unscaledDeltaTime);
    }
}
