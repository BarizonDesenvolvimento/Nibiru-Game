using System;
using Nibiru.AI;
using Nibiru.Combat;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 12 (a luta com o Primeiro Servo, na antecâmara do
    /// Poço). Cada item liga o Play e põe a Elena no momento escolhido, com o mundo como se ela tivesse
    /// descido o Poço, passado pelo Lullu e visto o flash, e com pistola, balas e faca.
    /// Menu: Nibiru > Testes > Passo 12.
    /// </summary>
    [InitializeOnLoad]
    static class Step12Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 12/";

        // Passos da missão Sobrevivente (24 = o flash; 25 = sobreviver ao Servo; 26 = a parede do barco).
        const int PassLulluStep = 23;
        internal const int FightStep = 25;
        internal const int WallStep = 26;

        internal static readonly string[] BeforeFlash = { "ChegouABocaDoPoco", "DesceuOPoco" };
        internal static readonly string[] Woke =
        {
            "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu", "ChegouPertoDoSelo", "FlashDoPoco", "PrimeiroServoAcordou",
        };
        internal static readonly string[] Won =
        {
            "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu", "ChegouPertoDoSelo", "FlashDoPoco", "PrimeiroServoAcordou",
            "VenceuOPrimeiroServo",
        };

        // Onde cada atalho põe a Elena (os pés no chão): o retorno da luta e o de depois dela.
        internal static readonly Vector3 FightSpot = new Vector3(45f, -14f, 83f);
        internal static readonly Vector3 AfterSpot = new Vector3(42f, -14f, 93f);

        static Step12Shortcuts()
        {
            Register("passo12.flash", () => Step11Shortcuts.Begin(BeforeFlash, PassLulluStep, Step11Shortcuts.NearFlashSpot, 0f,
                "Passo 12: um passo antes do flash, com o Lullu vivo. Ande para o norte: a Voz fala, o Servo sai do Selo urrando e o Lullu foge.",
                raiseLive: new[] { "PassouPeloLullu" }));
            Register("passo12.luta", () => Fight(1, 1f,
                "Passo 12: no ponto de retorno da luta, na boca da antecâmara. O Servo vagueia junto ao Selo: faça barulho para ele vir."));
            Register("passo12.fase2", () => Fight(1, 0.59f,
                "Passo 12: o Servo passa para a fase 2 logo ao carregar (olhe para ele): urro, lascas da máscara, holofotes piscando. Depois ele sobe as rampas."));
            Register("passo12.fase3", () => Fight(2, 0.24f,
                "Passo 12: o Servo passa para a fase 3 logo ao carregar: avisa menos antes de investir, mas cansa mais depois."));
            Register("passo12.final", () => Fight(3, 0.06f,
                "Passo 12: o Servo na fase 3, quase sem vida. Use o Eco e ataque: ele tomba diante do Selo."));
            Register("passo12.vitoria", () => Step11Shortcuts.Begin(Won, WallStep, AfterSpot, 270f,
                "Passo 12: depois da vitória, o Servo caído diante do Selo. Ande até a parede do barco de junco, a oeste."));
        }

        [MenuItem(MenuRoot + "1. Do flash ao Servo acordando (o Lullu foge)")]
        static void Flash() => Go("passo12.flash");

        [MenuItem(MenuRoot + "2. Ponto de retorno da luta (fase 1)")]
        static void FightStart() => Go("passo12.luta");

        [MenuItem(MenuRoot + "3. Troca para a fase 2")]
        static void Phase2() => Go("passo12.fase2");

        [MenuItem(MenuRoot + "4. Troca para a fase 3")]
        static void Phase3() => Go("passo12.fase3");

        [MenuItem(MenuRoot + "5. Fim da luta: o Servo quase sem vida")]
        static void Final() => Go("passo12.final");

        [MenuItem(MenuRoot + "6. Depois da vitória: rumo à parede do barco")]
        static void Victory() => Go("passo12.vitoria");

        [MenuItem(MenuRoot + "8. Ferir o Servo (10% da vida)")]
        static void Hurt()
        {
            var health = Servant() != null ? Servant().GetComponent<Health>() : null;
            if (!EditorApplication.isPlaying || health == null)
            {
                Debug.LogWarning("Ligue o Play com o Servo acordado (atalhos 2 a 5).");
                return;
            }
            health.Restore(Mathf.Max(1f, health.Current - health.Max * 0.1f));
            Debug.Log($"Passo 12: o Servo ficou com {health.Fraction:P0} da vida.");
        }

        // Luta já começada, ao voltar do checkpoint da boca da antecâmara: nesta fase e com esta vida.
        static void Fight(int phase, float healthFraction, string message)
        {
            Action setup = () =>
            {
                var fight = Servant();
                if (fight == null) return;
                if (phase > 1) fight.StartAtPhase(phase);
                var health = fight.GetComponent<Health>();
                health.Restore(health.Max * healthFraction);
            };
            Step11Shortcuts.Begin(Woke, FightStep, FightSpot, 0f, message, thenAlso: setup);
        }

        internal static FirstServantFight Servant() => Object.FindFirstObjectByType<FirstServantFight>();
    }
}
