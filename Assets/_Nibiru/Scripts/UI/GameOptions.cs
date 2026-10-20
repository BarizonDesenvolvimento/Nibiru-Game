using UnityEngine;

namespace Nibiru.UI
{
    /// <summary>
    /// Preferências do jogador, guardadas no computador (não fazem parte do save da partida).
    /// As sensibilidades multiplicam os valores de CameraJogador: 1 = como foi ajustado no projeto.
    /// </summary>
    public static class GameOptions
    {
        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;

        const string MouseKey = "Nibiru.Opcoes.SensibilidadeMouse";
        const string GamepadKey = "Nibiru.Opcoes.SensibilidadeControle";
        const string InvertKey = "Nibiru.Opcoes.InverterY";

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(MouseKey, 1f);
            set => PlayerPrefs.SetFloat(MouseKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
        }

        public static float GamepadSensitivity
        {
            get => PlayerPrefs.GetFloat(GamepadKey, 1f);
            set => PlayerPrefs.SetFloat(GamepadKey, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertKey, 0) == 1;
            set => PlayerPrefs.SetInt(InvertKey, value ? 1 : 0);
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
