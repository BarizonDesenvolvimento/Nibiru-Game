using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.UI
{
    /// <summary>
    /// Quem manda no cursor do mouse. No jogo ele fica preso e escondido; menus (principal, pausa, leitura, fim da demo)
    /// pedem o cursor livre com <see cref="RequestFree"/> e o soltam com <see cref="Release"/>. Livre, o cursor só aparece
    /// quando o jogador usa o mouse (teclado e controle o escondem). Ao recuperar o foco da janela (alt-tab) o estado certo
    /// é reaplicado. Fora de uma cena de jogo ou de menu (laboratórios) a regra não mexe no cursor.
    /// </summary>
    public static class CursorPolicy
    {
        static readonly HashSet<object> owners = new HashSet<object>();
        static bool pointerShown = true;
        static bool gameActive;
        static CursorDriver driver;

        /// <summary>True se algum menu pediu o cursor livre.</summary>
        public static bool AnyFree => owners.Count > 0;

        /// <summary>True se, livre, o cursor está visível (o mouse foi usado por último).</summary>
        public static bool PointerShown => pointerShown;

        /// <summary>O modo de trava que a regra quer agora: livre num menu, preso no jogo.</summary>
        public static CursorLockMode WantedLock => AnyFree ? CursorLockMode.None : CursorLockMode.Locked;

        /// <summary>Se o cursor deve estar visível agora.</summary>
        public static bool WantedVisible => AnyFree && pointerShown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            owners.Clear();
            pointerShown = true;
            gameActive = false;
            driver = null;
        }

        /// <summary>A cena de jogo (com a câmera do jogador) avisa que existe, para a regra prender o cursor ao despausar.</summary>
        public static void SetGameActive(bool active)
        {
            gameActive = active;
            if (active) EnsureDriver(); // ao sair do jogo não cria nada (a cena está fechando)
        }

        /// <summary>Um menu pede o cursor livre. <paramref name="showPointer"/>: já visível (aberto pelo mouse) ou escondido.</summary>
        public static void RequestFree(object owner, bool showPointer)
        {
            bool first = owners.Count == 0;
            owners.Add(owner);
            if (first) pointerShown = showPointer;
            EnsureDriver();
            Apply();
            // O editor da Unity também mexe no cursor ao apertar Esc: reaplica nos quadros seguintes.
            if (driver != null) driver.ReapplyLater();
        }

        public static void Release(object owner)
        {
            if (!owners.Remove(owner)) return;
            Apply();
            if (driver != null) driver.ReapplyLater();
        }

        /// <summary>Esconde ou mostra o cursor livre (usado pelo <see cref="MenuPointer"/>).</summary>
        public static void ShowPointer(bool show)
        {
            // Sempre reaplica: o editor ou o sistema podem ter mostrado o cursor por conta própria.
            pointerShown = show;
            Apply();
        }

        /// <summary>Esquece todos os pedidos (volta ao menu principal com o estado limpo).</summary>
        public static void Clear()
        {
            owners.Clear();
            pointerShown = true;
        }

        /// <summary>Aplica o estado querido ao cursor da Unity (só quando há um menu ou um jogo sob a regra).</summary>
        public static void Apply()
        {
            if (!AnyFree && !gameActive) return;
            if (Cursor.lockState != WantedLock) Cursor.lockState = WantedLock;
            if (Cursor.visible != WantedVisible) Cursor.visible = WantedVisible;
        }

        /// <summary>A janela recuperou o foco (alt-tab): reaplica o estado certo agora e de novo uns quadros depois.</summary>
        public static void OnFocusRegained()
        {
            Apply();
            if (driver != null) driver.ReapplyLater();
        }

        static void EnsureDriver()
        {
            if (driver != null || !Application.isPlaying) return;
            var go = new GameObject("Cursor_Policy") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            driver = go.AddComponent<CursorDriver>();
        }

        // Reaplica o estado quando a janela recupera o foco (alt-tab); a Unity às vezes desfaz o cursor depois do aviso,
        // então reaplica de novo uns quadros depois.
        sealed class CursorDriver : MonoBehaviour
        {
            int reapplyFrames;

            void OnApplicationFocus(bool hasFocus)
            {
                if (hasFocus) OnFocusRegained();
            }

            public void ReapplyLater() => reapplyFrames = 3;

            void Update()
            {
                if (reapplyFrames <= 0) return;
                reapplyFrames--;
                Apply();
            }
        }
    }
}
