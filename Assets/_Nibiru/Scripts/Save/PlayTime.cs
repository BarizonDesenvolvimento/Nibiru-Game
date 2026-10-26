using System.Collections.Generic;
using UnityEngine;

namespace Nibiru.Save
{
    /// <summary>
    /// Tempo jogado da partida, em segundos de relógio (conversas e cenas contam). Para enquanto alguém o segura:
    /// o menu de pausa (e o que abre por ele, como o Arquivo), o menu principal e a tela de fim da demo.
    /// O salvamento grava e devolve este valor.
    /// </summary>
    public static class PlayTime
    {
        static readonly HashSet<Object> holders = new HashSet<Object>();

        public static float Seconds { get; private set; }

        /// <summary>False enquanto um menu segura o relógio.</summary>
        public static bool Running
        {
            get
            {
                // Quem foi destruído (a pausa, ao recarregar a cena) solta o relógio sozinho.
                holders.RemoveWhere(h => h == null);
                return holders.Count == 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Seconds = 0f;
            holders.Clear();
            var clock = new GameObject("Relogio_Tempo_Jogado") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(clock);
            clock.AddComponent<PlayTimeClock>();
        }

        public static void Set(float seconds) => Seconds = Mathf.Max(0f, seconds);

        /// <summary>Para o relógio enquanto <paramref name="owner"/> estiver aberto.</summary>
        public static void Hold(Object owner) => holders.Add(owner);

        public static void Release(Object owner) => holders.Remove(owner);

        /// <summary>
        /// Teto de cada quadro, em segundos. Um quadro longo (o carregamento de uma cena trava o jogo por alguns segundos)
        /// conta no máximo isto, então carregar não entra no tempo jogado.
        /// </summary>
        public const float MaxStep = 0.25f;

        internal static void Tick(float deltaTime)
        {
            if (Running) Seconds += Mathf.Min(deltaTime, MaxStep);
        }
    }
}
