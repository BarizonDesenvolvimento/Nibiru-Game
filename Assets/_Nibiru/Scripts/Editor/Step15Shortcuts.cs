using System.Linq;
using Nibiru.Inventory;
using Nibiru.Save;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 15 (a fuga na caminhonete até o fim da demo). Cada item
    /// liga o Play e põe a Elena no momento escolhido, com o mundo como se ela tivesse vencido o Primeiro
    /// Servo, atravessado a passagem e feito a escolha do wadi, e com pistola, balas e faca.
    /// Menu: Nibiru > Testes > Passo 15.
    /// </summary>
    [InitializeOnLoad]
    static class Step15Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 15/";

        // Passos da missão Sobrevivente: 32 = levar a tabuleta ao nicho; 33 = entrar na caminhonete; 34 = fugir.
        internal const int NicheStep = 32;
        internal const int BoardStep = 33;
        internal const int RideStep = 34;

        internal static readonly string[] AfterRadio = Step14Shortcuts.AtSoldier.Concat(new[] { "RadioDoKastComecou", "EscolheuONoWadi" }).ToArray();
        internal static readonly string[] Entregou = AfterRadio.Concat(new[] { "EntregouATabuleta", "TabuletaResolvida" }).ToArray();
        internal static readonly string[] Ficou = AfterRadio.Concat(new[] { "FicouComATabuleta", "TabuletaResolvida" }).ToArray();
        internal static readonly string[] Devolveu = AfterRadio.Concat(new[] { "DevolveuATabuleta" }).ToArray();

        // Junto do soldado ferido, de frente para a caminhonete; e diante do nicho do túnel, olhando para ele.
        internal static readonly Vector3 NearTruck = new Vector3(60f, 0f, 170.2f);
        internal static readonly Vector3 AtNiche = new Vector3(31.8f, 0f, 102.6f);

        internal enum Pick { Entregou, Devolveu, Ficou }

        internal static string[] EventsOf(Pick pick) => pick == Pick.Entregou ? Entregou : pick == Pick.Devolveu ? Devolveu : Ficou;

        internal static int StepOf(Pick pick) => pick == Pick.Devolveu ? NicheStep : BoardStep;

        static readonly string[] Names = { "entregou a tabuleta à Fundação", "devolve a tabuleta ao Poço", "ficou com a tabuleta" };

        static Step15Shortcuts()
        {
            foreach (Pick pick in new[] { Pick.Entregou, Pick.Devolveu, Pick.Ficou })
            {
                Pick p = pick;
                string name = p.ToString().ToLowerInvariant();
                Register($"passo15.inicio_{name}", () => Begin(p, NearTruck, 0f,
                    p == Pick.Devolveu
                        ? "Passo 15: o rádio acabou e a Elena decidiu devolver a tabuleta. Volte pelo túnel até o nicho dos Guardiões (parede oeste, depois da lamparina 2), enterre a tabuleta e então volte à caminhonete."
                        : $"Passo 15: a Elena {Names[(int)p]}. Vá até a traseira da caminhonete e use \"Entrar na caminhonete\"."));
                Register($"passo15.meio_{name}", () => Begin(p, NearTruck, 0f,
                    $"Passo 15: meio da viagem ({Names[(int)p]}). A caminhonete já está na estrada, a conversa vale para esta escolha. O pause (Esc) congela o trilho.",
                    thenAlso: () => After(0.5f, () => Ride()?.BeginAt(0.5f)), buried: true));
            }
            Register("passo15.nicho", () => Begin(Pick.Devolveu, AtNiche, 270f,
                "Passo 15: diante do nicho dos Guardiões, no túnel. O Samir espera ao lado; use \"Enterrar a tabuleta\". Depois, volte à caminhonete (sul do túnel, rampa acima, wadi)."));
            Register("passo15.chegada", () => Begin(Pick.Ficou, NearTruck, 0f,
                "Passo 15: fim da viagem. A caminhonete está no fim do trilho; depois da última fala vêm o diário e a tela de fim da demo.",
                thenAlso: () => After(0.5f, () => Ride()?.BeginAt(1f)), buried: true));
        }

        // A Elena junto da caminhonete (ou do nicho) com a escolha feita: eventos, passo da missão, reputação e
        // transformação como se a conversa do rádio tivesse acontecido; a tabuleta no inventário.
        internal static void Begin(Pick pick, Vector3 spawn, float yaw, string message, System.Action thenAlso = null, bool buried = false)
        {
            Vector3 feet = spawn;
            if (spawn == AtNiche && Physics.Raycast(new Vector3(spawn.x, -9f, spawn.z), Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                feet.y = hit.point.y;
            // Quem devolve e já enterrou a tabuleta (meio da viagem): nicho feito e passo "entre na caminhonete".
            string[] events = buried && pick == Pick.Devolveu ? Devolveu.Concat(new[] { "EnterrouATabuleta", "TabuletaResolvida" }).ToArray() : EventsOf(pick);
            int step = buried ? BoardStep : StepOf(pick);
            Step11Shortcuts.Begin(events, step, feet, yaw, message, thenAlso: () =>
            {
                ApplyChoice(pick, giveTablet: !buried);
                thenAlso?.Invoke();
            });
        }

        // Reputação e transformação da escolha (o que a conversa do rádio teria feito) e a tabuleta na bolsa.
        internal static void ApplyChoice(Pick pick, bool giveTablet = true)
        {
            WorldState.SetReputation("FundacaoEnlil", pick == Pick.Entregou ? 15 : pick == Pick.Devolveu ? -15 : -5);
            WorldState.SetReputation("Guardioes", pick == Pick.Entregou ? -15 : pick == Pick.Devolveu ? 15 : -5);
            if (pick == Pick.Ficou) WorldState.Transformation += 3;
            var inventory = Object.FindFirstObjectByType<PlayerInventory>();
            var tablet = Load<ItemDefinition>("Items/TabuletaDoJunco");
            if (giveTablet && inventory != null && tablet != null && !inventory.Has(tablet)) inventory.Add(tablet, 1);
            if (!giveTablet && pick == Pick.Devolveu && inventory != null) inventory.Remove(tablet, 1);
        }

        internal static TruckRide Ride() => Object.FindFirstObjectByType<TruckRide>();

        [MenuItem(MenuRoot + "1. Fuga: entregou a tabuleta à Fundação (junto da caminhonete)")]
        static void Start1() => Go("passo15.inicio_entregou");

        [MenuItem(MenuRoot + "2. Fuga: devolve ao Poço (começa no wadi, vai ao nicho do túnel)")]
        static void Start2() => Go("passo15.inicio_devolveu");

        [MenuItem(MenuRoot + "3. Fuga: ficou com a tabuleta (junto da caminhonete)")]
        static void Start3() => Go("passo15.inicio_ficou");

        [MenuItem(MenuRoot + "4. Devolve ao Poço: diante do nicho do túnel")]
        static void Start4() => Go("passo15.nicho");

        [MenuItem(MenuRoot + "5. Meio da viagem: entregou")]
        static void Start5() => Go("passo15.meio_entregou");

        [MenuItem(MenuRoot + "6. Meio da viagem: devolve")]
        static void Start6() => Go("passo15.meio_devolveu");

        [MenuItem(MenuRoot + "7. Meio da viagem: ficou")]
        static void Start7() => Go("passo15.meio_ficou");

        [MenuItem(MenuRoot + "8. Fim da viagem: diário e fim da demo")]
        static void Start8() => Go("passo15.chegada");
    }
}
