using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o Arquivo (etapa A3: documentos e fitas no menu de pausa). Cada item liga o
    /// Play e põe a Elena no momento escolhido, com o mundo como se ela tivesse jogado até ali.
    /// Menu: Nibiru > Testes > Arquivo (A3).
    /// </summary>
    [InitializeOnLoad]
    static class ArchiveShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Arquivo (A3)/";

        // Passos da missão: 15 = seguir o Samir até a beira da aldeia; 21 = chegar à boca do Poço.
        const int FollowSamirStep = 15;
        const int MouthStep = 21;

        // Tudo que o Arquivo lista, já lido ou coletado (e o aviso da primeira vez já dado).
        internal static readonly string[] EverythingCollected =
        {
            "LeuOEmailDoKast", "ViuAFotoDe1979", "LeuACartaDoSoldado", "LeuAInscricaoDaAldeia", "LeuAInscricaoDoPoco",
            "LeuAInscricaoDaPassagem", "LeuODocumentoDaWeiss", "LeuAsFotosDoTom", "ColetouAFitaDeIrena1", "FitaDeIrena1Terminou", "LeuORegistroDoGuincho", "LeuANotaDoSelo", "AvisouDoArquivo",
        };

        // A carta fica no canto sudeste das trincheiras (23,5; -72,5): a Elena chega do lado de dentro, olhando para ela.
        internal static readonly Vector3 LetterApproach = new Vector3(22.3f, 0f, -71.3f);
        internal const float LetterYaw = 135f;

        // Diante da parede do fundo da casa do altar, onde o Eco acende a inscrição da Aldeia (x = -45,6; z = -31,5).
        internal static readonly Vector3 VillageWallSpot = new Vector3(-45.5f, 0f, -30.3f);

        // Achados da base (etapa A3, parte 2). O registro fica na mochila ao pé SUL do contêiner 5, a Elena de frente para o norte
        // (o contêiner a protege do atirador); a nota fica na ponta leste da mesa da tenda de provas, a Elena ao sul da mesa.
        internal static readonly Vector3 RegisterSpot = new Vector3(22.5f, 0f, 14.3f);
        internal static readonly Vector3 NoteSpot = new Vector3(32.35f, 0f, 22.6f);

        // Mundo de quem entrou na base (fim do passo 9) e ainda não passou pela tenda de comando.
        static readonly string[] BaseEntered = { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga", "EntrouNaBaseDaFundacao" };

        static ArchiveShortcuts()
        {
            Register("arquivo.vazio", () =>
            {
                RemedyShortcuts.Ready = false;
                var old = Object.FindFirstObjectByType<Player.PlayerMotor>();
                int oldId = old != null ? old.GetInstanceID() : 0;
                FreshGame();
                WaitUntil(() =>
                {
                    var current = Object.FindFirstObjectByType<Player.PlayerMotor>();
                    return current != null && current.GetInstanceID() != oldId && Time.timeSinceLevelLoad > 1.5f;
                }, () => RemedyShortcuts.Ready = true);
                Debug.Log("Arquivo: jogo novo, nada coletado. Aperte Esc e escolha \"Arquivo\": deve aparecer \"Nada no arquivo ainda.\"");
            });
            Register("arquivo.cheio", () => { RemedyShortcuts.Ready = false; Step11Shortcuts.Begin(EverythingCollected, MouthStep, new Vector3(52f, 0f, 40f), 0f,
                "Arquivo: tudo já lido e coletado. Aperte Esc, escolha \"Arquivo\" e releia cada entrada (Enter fecha e volta à lista).",
                raiseLive: null, thenAlso: () => RemedyShortcuts.Ready = true); });
            Register("arquivo.carta", () =>
            {
                RemedyShortcuts.Ready = false;
                Step9Shortcuts.Start(revealed: true, extra: null, Step9Shortcuts.CrossRoadStep, LetterApproach, LetterYaw, () => RemedyShortcuts.Ready = true);
            });
            Register("arquivo.registro", () => BaseFind(RegisterSpot, 0f,
                "Arquivo: ao pé do contêiner junto do heliponto, de frente para a mochila. O contêiner esconde a Elena do atirador. \"Ler o registro\"."));
            Register("arquivo.nota", () => BaseFind(NoteSpot, 0f,
                "Arquivo: na tenda de provas, ao sul da mesa, de frente para a nota (o 3º rolo do Tom está na outra ponta). \"Ler a nota\"."));
            Register("arquivo.aldeia_antes", () => VillageWall(false));
            Register("arquivo.aldeia_revelada", () => VillageWall(true));
            // Diante da inscrição do Poço (a mesma do passo 11, atalho 8), com a sentinela perto: a Elena já nasce agachada.
            Register("arquivo.poco", () =>
            {
                RemedyShortcuts.Ready = false;
                Step11Shortcuts.Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu" }, 24, new Vector3(52.6f, -14f, 83.9f), 90f,
                    "Arquivo: diante da inscrição do Poço. Leia e abra o Arquivo na pausa.", crouch: true, thenAlso: () => RemedyShortcuts.Ready = true);
            });
        }

        [MenuItem(MenuRoot + "8. Base: diante do registro do guincho")]
        static void Register_() => Go("arquivo.registro");

        [MenuItem(MenuRoot + "9. Base: diante da nota do selo")]
        static void Note() => Go("arquivo.nota");

        static void BaseFind(Vector3 spot, float yaw, string message)
        {
            RemedyShortcuts.Ready = false;
            Step9Shortcuts.Start(revealed: true, BaseEntered, 17, spot, yaw, () => RemedyShortcuts.Ready = true);
            Debug.Log(message);
        }

        [MenuItem(MenuRoot + "1. Jogo novo: Arquivo vazio")]
        static void Empty() => Go("arquivo.vazio");

        [MenuItem(MenuRoot + "2. Tudo coletado: reler no Arquivo (menu de pausa)")]
        static void Full() => Go("arquivo.cheio");

        [MenuItem(MenuRoot + "3. Trincheiras: diante da carta do soldado")]
        static void Letter()
        {
            Go("arquivo.carta");
            Debug.Log("Arquivo: no canto sudeste das trincheiras, olhando para a mochila. \"Ler a carta\": primeira leitura mostra o aviso \"Adicionado ao Arquivo (menu de pausa)\".");
        }

        [MenuItem(MenuRoot + "4. Aldeia: parede do fundo, antes do Eco (use Q / LB)")]
        static void VillageBefore() => Go("arquivo.aldeia_antes");

        [MenuItem(MenuRoot + "5. Aldeia: inscrição já revelada pelo Eco (Ler a inscrição)")]
        static void VillageRevealed() => Go("arquivo.aldeia_revelada");

        [MenuItem(MenuRoot + "6. Poço: diante da inscrição dos Guardiões")]
        static void Well() => Go("arquivo.poco");

        // Mundo depois da tabuleta (a Elena já tem o Eco); a revelação só vale ("revealedByEcho") depois do Eco.
        static void VillageWall(bool revealedByEcho)
        {
            string[] extra = revealedByEcho ? new[] { "RevelouAInscricaoDaAldeia" } : null;
            RemedyShortcuts.Ready = false;
            Step9Shortcuts.Start(revealed: true, extra, FollowSamirStep, VillageWallSpot, 180f, () => RemedyShortcuts.Ready = true);
            Debug.Log(revealedByEcho
                ? "Arquivo: diante da parede do fundo da casa do altar, com a inscrição já revelada. Use \"Ler a inscrição\"."
                : "Arquivo: diante da parede do fundo da casa do altar. Sem o Eco não há aviso nenhum; aperte Q (LB) e depois \"Ler a inscrição\" aparece.");
        }
    }
}
