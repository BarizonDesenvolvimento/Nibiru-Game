using System.Linq;
using Nibiru.Save;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o final de "O que o Tom viu" (a revelação dos filmes com Abu Faris e o item
    /// "Fotos do Tom"). Cada item liga o Play e põe a Elena num momento, com o mundo como se ela tivesse jogado até ali.
    /// Menu: Nibiru > Testes > Secundárias > O que o Tom viu.
    /// </summary>
    [InitializeOnLoad]
    static class TomShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Secundárias/O que o Tom viu/";

        // Diante da Umm Karim (olhando para ela, ao sul) e diante de Abu Faris (dentro da abertura da casinha, olhando para oeste).
        internal static readonly Vector3 UmmFront = new Vector3(-47.4f, 0f, -29.4f);
        internal const float UmmYaw = 202f;
        internal static readonly Vector3 AbuFront = new Vector3(-54.7f, 0f, -28.2f);
        internal const float AbuYaw = 270f;

        static readonly string[] Started = { "AchouBolsaDoTom" };
        static readonly string[] Led = { "AchouBolsaDoTom", "UmmKarimLevouAoQuartoEscuro" };
        static readonly string[] Revealed = { "AchouBolsaDoTom", "UmmKarimLevouAoQuartoEscuro", "RevelouFilmesDoTom" };

        static TomShortcuts()
        {
            Register("tom.zero", () => World(Started, UmmFront, UmmYaw, () => RemedyShortcuts.SetBag(0, 0),
                "O que o Tom viu, do zero: a Elena achou a bolsa do Tom e ainda não tem nenhum rolo. A missão pede os três rolos de filme."));
            Register("tom.dois_rolos", () => World(Started, UmmFront, UmmYaw, () => Rolls(2),
                "Diante da Umm Karim com 2 rolos: ela não leva a Elena a ninguém ainda (precisa dos três)."));
            Register("tom.tres_rolos", () => World(Started, UmmFront, UmmYaw, () => Rolls(3),
                "Diante da Umm Karim com os 3 rolos: fale com ela. Ela leva a Elena ao quarto escuro de Abu Faris, ao lado oeste da casa do altar."));
            Register("tom.quarto_escuro", () => World(Led, AbuFront, AbuYaw, () => Rolls(3),
                "No quarto escuro, diante de Abu Faris, com os 3 rolos: use \"Entregar os rolos de filme a Abu Faris\"."));
            Register("tom.fotos", () => World(Revealed, UmmFront, UmmYaw, () =>
            {
                RemedyShortcuts.SetBag(0, 0);
                RemedyShortcuts.Give("FotosDoTom", 1);
                WorldState.SetQuestStep("OQueOTomViu", 2);
            }, "Depois de revelar: as Fotos do Tom já estão na bolsa. Abra o inventário (Tab) e use o item para ver as seis fotos."));
        }


        // O mundo de quem já leu a tabuleta e conversou com o Samir (fim do passo 8), com estes eventos a mais. A casa do altar
        // fica apertada antes disso: o aviso "Ler a tabuleta" disputa com o da Umm Karim.
        static void World(string[] extra, Vector3 spawn, float yaw, System.Action then, string message)
        {
            RemedyShortcuts.Ready = false;
            Step9Shortcuts.Start(revealed: true, extra: extra, questStep: 15, spawn, yaw, () =>
            {
                then?.Invoke();
                RemedyShortcuts.Ready = true;
            });
            Debug.Log(message);
        }
        static void Rolls(int count)
        {
            RemedyShortcuts.SetBag(0, 0);
            RemedyShortcuts.Give("RoloDeFilmeDoTom", count);
        }

        [MenuItem(MenuRoot + "1. Do zero: bolsa do Tom achada, sem rolos")]
        static void Zero() => RemedyShortcuts.Launch("tom.zero");

        [MenuItem(MenuRoot + "2. Diante da Umm Karim com 2 rolos")]
        static void TwoRolls() => RemedyShortcuts.Launch("tom.dois_rolos");

        [MenuItem(MenuRoot + "3. Diante da Umm Karim com os 3 rolos (ela leva ao quarto escuro)")]
        static void ThreeRolls() => RemedyShortcuts.Launch("tom.tres_rolos");

        [MenuItem(MenuRoot + "4. No quarto escuro, diante de Abu Faris, com os 3 rolos")]
        static void Darkroom() => RemedyShortcuts.Launch("tom.quarto_escuro");

        [MenuItem(MenuRoot + "5. Depois de revelar: com as Fotos do Tom na bolsa")]
        static void Photos() => RemedyShortcuts.Launch("tom.fotos");
    }
}
