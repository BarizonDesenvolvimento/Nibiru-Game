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

        /// <summary>Chave da distância da câmera (zoom), em metros. Preferência do jogador: não entra no save de slot.</summary>
        public const string CameraDistanceKey = "Nibiru.Opcoes.DistanciaCamera";

        /// <summary>
        /// Distância da câmera escolhida, em metros, como está guardada. Quem usa limita à faixa do CameraSettings
        /// (<see cref="CameraDistanceClamped"/>); valor ausente ou inválido (NaN, infinito) volta ao padrão.
        /// </summary>
        public static float CameraDistance
        {
            get => PlayerPrefs.GetFloat(CameraDistanceKey, float.NaN);
            set => PlayerPrefs.SetFloat(CameraDistanceKey, value);
        }

        /// <summary>A distância guardada limitada a [min, max]; sem valor válido guardado, o padrão (também limitado).</summary>
        public static float CameraDistanceClamped(float min, float max, float fallback)
        {
            float stored = CameraDistance;
            if (float.IsNaN(stored) || float.IsInfinity(stored)) stored = fallback;
            return Mathf.Clamp(stored, min, max);
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
