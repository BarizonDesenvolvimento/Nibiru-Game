using Nibiru.Environment;
using Nibiru.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nibiru.Save
{
    /// <summary>
    /// Volta ao menu principal com o estado limpo, como numa sessão nova: tempo normal, áudio ligado, cursor livre, nenhum
    /// slot de partida, mundo, pontos de retorno, relógio de jogo e tempestade zerados. Chamado pela pausa
    /// ("Sair para o menu") e pelo fim da demo; o menu principal também limpa ao abrir (<see cref="Clean"/>).
    /// </summary>
    public static class SessionReset
    {
        public const string MenuScene = "MenuPrincipal";

        /// <summary>Zera o que a partida deixa na memória (sem trocar de cena).</summary>
        public static void Clean()
        {
            Checkpoints.Clear();
            WorldState.Clear();
            SaveSlots.Detach();
            PlayTime.Set(0f);
            Storm.Clear();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CursorPolicy.Clear();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Limpa tudo e carrega o menu principal.</summary>
        public static void ToMenu(string menuScene = MenuScene)
        {
            Clean();
            SceneManager.LoadScene(menuScene);
        }
    }
}
