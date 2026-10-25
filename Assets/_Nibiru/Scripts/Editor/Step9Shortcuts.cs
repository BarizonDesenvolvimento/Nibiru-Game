using System;
using System.Linq;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 9 da Aldeia (o plano de fuga) sem jogar desde o
    /// Acampamento. Cada item liga o Play e põe a Elena no momento escolhido, com o mundo como se
    /// ela tivesse lido a tabuleta e respondido ao Samir. Menu: Nibiru > Testes > Passo 9.
    /// </summary>
    [InitializeOnLoad]
    static class Step9Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 9/";

        // Depois do passo 8: a tabuleta decifrada (o poder e a marca) e a conversa com o Samir.
        static readonly string[] Step8Events =
        {
            "TocouATabuleta", "EntrouNaMemoriaDeZiusudra", "AParedeDeJuncoFalou", "OuviuAParedeDeJunco",
            "DecifrouTabuletaDoJunco", "Tabuleta:TabuletaDoJunco", "Poder:EcoDoAbzu", "ConversouSobreATabuleta",
        };

        // Passos da missão Sobrevivente: "Siga o Samir até a beira da aldeia" e "Passe pela patrulha...".
        const int FollowSamirStep = 15;
        internal const int CrossRoadStep = 16;
        const int TransformationAfterTablet = 5;

        // Um pouco ao sul da beira da aldeia, olhando para o norte: ao andar, a Elena entra na cena do cerco.
        static readonly Vector3 BeforeLookoutSpot = new Vector3(-46f, 0f, -1f);
        // Onde o ponto de retorno depois do plano devolve a Elena, virada para a estrada.
        static readonly Vector3 AfterPlanSpot = new Vector3(-47.5f, 0f, 6.5f);
        // Na ponta sul do muro da base, a poucos passos da entrada.
        static readonly Vector3 WallCornerSpot = new Vector3(-24f, 0f, -9f);

        const string SamirPath = "Conteudo_Aldeia/Encontro_Samir/Cena/Samir";
        const string LookoutPath = "Conteudo_Aldeia/Plano_De_Fuga/Samir_Beira";

        static Step9Shortcuts()
        {
            Register("passo9.inicio", () =>
            {
                Start(revealed: true, extra: null, FollowSamirStep, Step8Shortcuts.NicheSpot, 0f, null);
                Debug.Log("Passo 9: logo depois da conversa da tabuleta. Siga o Samir até a beira norte da aldeia.");
            });
            Register("passo9.beira_guardiao", () => BeforeLookout(revealed: true));
            Register("passo9.beira_sem_revelar", () => BeforeLookout(revealed: false));
            Register("passo9.patrulha", () =>
            {
                Start(revealed: true, extra: new[] { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga" }, CrossRoadStep, AfterPlanSpot, 90f, null);
                Debug.Log("Passo 9: depois do plano. Passe pela patrulha rente ao muro e entre na base pela ponta sul.");
            });
            Register("passo9.muro", () =>
            {
                Start(revealed: true, extra: new[] { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga" }, CrossRoadStep, WallCornerSpot, 90f, null);
                Debug.Log("Passo 9: na ponta sul do muro. Ande para o leste para entrar na base.");
            });
        }

        [MenuItem(MenuRoot + "1. Logo depois da conversa da tabuleta")]
        static void Begin() => Go("passo9.inicio");

        [MenuItem(MenuRoot + "2. Na beira da aldeia, antes do cerco (o Samir revelou que é Guardião)")]
        static void LookoutRevealed() => Go("passo9.beira_guardiao");

        [MenuItem(MenuRoot + "3. Na beira da aldeia, antes do cerco (o Samir não revelou)")]
        static void LookoutNotRevealed() => Go("passo9.beira_sem_revelar");

        [MenuItem(MenuRoot + "4. Depois do plano: passar pela patrulha")]
        static void Patrol() => Go("passo9.patrulha");

        [MenuItem(MenuRoot + "5. Na ponta sul do muro, entrando na base")]
        static void WallCorner() => Go("passo9.muro");

        // A Elena um pouco antes da cena do cerco, e o Samir já esperando na beira. O Samir só é
        // posto lá depois de um segundo, quando a cena já o levou para dentro da casa e mandou andar.
        static void BeforeLookout(bool revealed)
        {
            Start(revealed, extra: null, FollowSamirStep, BeforeLookoutSpot, 0f, () => After(1f, () =>
            {
                var walker = Find(SamirPath).GetComponent<WalkToPoint>();
                walker.SnapTo(Find(LookoutPath).transform);
            }));
            Debug.Log("Passo 9: perto da beira da aldeia. Ande para o norte até o Samir para ver o cerco.");
        }

        // Mundo até o fim do passo 8. Se o Samir não revelou que é Guardião, a Elena contou a verdade (Guardiões +5).
        internal static void Start(bool revealed, string[] extra, int questStep, Vector3 spawn, float yaw, Action then)
        {
            var events = Step8Shortcuts.EventsUntilStep7.Concat(Step8Events)
                .Append(revealed ? "SamirRevelouGuardiao" : "ContouAVerdadeAoSamir")
                .Concat(extra ?? new string[0]).ToArray();
            StartFrom(events, new[] { ("Sobrevivente", questStep), ("RemedioParaYusuf", 0), ("OQueOTomViu", 0) },
                TransformationAfterTablet, spawn, yaw, then,
                setup: () => { if (!revealed) Save.WorldState.SetReputation("Guardioes", 5); });
        }
    }
}
