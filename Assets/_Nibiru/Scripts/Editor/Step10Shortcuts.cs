using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 10 (atravessar a base do zigurate) sem jogar
    /// desde o Acampamento. Cada item liga o Play e põe a Elena no momento escolhido, com o mundo
    /// como se ela tivesse feito o plano de fuga e entrado na base, e com pistola, balas e faca.
    /// Menu: Nibiru > Testes > Passo 10.
    /// </summary>
    [InitializeOnLoad]
    static class Step10Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 10/";

        // Passos da missão Sobrevivente depois do passo 9 (16 = passar pela patrulha da estrada).
        const int CrossYardStep = 17;
        const int SearchDeskStep = 18;
        const int OpenGateStep = 19;
        const int LeaveBaseStep = 20;

        static readonly string[] EnteredBase = { "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga", "EntrouNaBaseDaFundacao" };
        static readonly string[] ReadDocuments = { "ChegouATendaDeComando", "LeuOEmailDoKast", "ViuAFotoDe1979", "LeuOsDocumentosDoKast" };

        // Onde cada atalho põe a Elena (e para onde ela olha).
        static readonly Vector3 EntranceSpot = new Vector3(-19.5f, 0f, 3f);
        static readonly Vector3 BehindContainersSpot = new Vector3(4f, 0f, -2.2f);
        static readonly Vector3 AvenueSpot = new Vector3(2f, 0f, 8f);
        static readonly Vector3 HighRouteSpot = new Vector3(6f, 14f, 45f);
        static readonly Vector3 BehindLeaderSpot = new Vector3(21.4f, 0f, 6.7f);
        static readonly Vector3 LastContainerSpot = new Vector3(14f, 0f, -2.5f);
        static readonly Vector3 EastLeaderApproachSpot = new Vector3(42f, 0f, 14f);
        static readonly Vector3 InsideTentSpot = new Vector3(28.5f, 0f, 7f);
        static readonly Vector3 PanelSpot = new Vector3(23.8f, 0f, 9.3f);
        static readonly Vector3 NorthEastSpot = new Vector3(52f, 0f, 26f);

        static Step10Shortcuts()
        {
            Register("passo10.entrada", () => Begin(null, CrossYardStep, EntranceSpot, 45f,
                "na entrada da base. Atravesse o pátio até a tenda de comando, a leste; o laser do atirador varre o chão."));
            Register("passo10.conteineres", () => Begin(null, CrossYardStep, BehindContainersSpot, 0f,
                "agachada atrás dos contêineres do sul. A avenida à frente fica à vista do atirador.", crouch: true));
            Register("passo10.avenida", () => Begin(null, CrossYardStep, AvenueSpot, 20f,
                "no meio da avenida, à vista do atirador: o laser trava, o bipe toca e, depois do aviso, ele atira."));
            Register("passo10.rota_alta", () => Begin(null, CrossYardStep, HighRouteSpot, 100f,
                "no 2º terraço do zigurate, atrás do atirador. Chegue perto agachada e use a faca (Golpe pelas costas).", crouch: true));
            Register("passo10.lider", () => Begin(null, CrossYardStep, BehindLeaderSpot, 214f,
                "atrás do líder de esquadra, na porta da tenda de comando: \"Golpe pelas costas\" aparece na tela.", crouch: true));
            Register("passo10.conteiner_gatilho", () => Begin(null, CrossYardStep, LastContainerSpot, 0f,
                "no corredor atrás do último contêiner do sul. Um helicóptero entra de longe e cruza a avenida: espere coberta e ele passa; corra pela avenida e ele avista."));
            Register("passo10.lider_leste", () => Begin(null, CrossYardStep, EastLeaderApproachSpot, 90f,
                "a leste do heliponto, perto da ronda do 2º líder. Chegue ao nordeste (saída para o Poço) passando pelo lado oeste quando ele olhar para o sul, ou pelas costas dele com a faca.", crouch: true));
            Register("passo10.tenda", () => Begin(new[] { "ChegouATendaDeComando" }, SearchDeskStep, InsideTentSpot, 90f,
                "dentro da tenda de comando, de frente para a mesa do Kast. Examine a mesa."));
            Register("passo10.painel", () => Begin(ReadDocuments, OpenGateStep, PanelSpot, 0f,
                "depois dos documentos, de frente para o painel do portão. Use o painel e olhe o portão do muro oeste pela porta."));
            Register("passo10.saida", () => Begin(ReadDocuments.Append("AbriuOPortaoDaAldeia").ToArray(), LeaveBaseStep, NorthEastSpot, 0f,
                "no nordeste do pátio, com o portão já aberto. Ande para o norte, entre o zigurate e a cerca, para sair da base."));
        }

        [MenuItem(MenuRoot + "1. Entrando na base")]
        static void Entrance() => Go("passo10.entrada");

        [MenuItem(MenuRoot + "2. Atrás dos contêineres, perto do laser")]
        static void Containers() => Go("passo10.conteineres");

        [MenuItem(MenuRoot + "3. Na avenida, à vista do atirador")]
        static void Avenue() => Go("passo10.avenida");

        [MenuItem(MenuRoot + "4. Na rota alta, atrás do atirador")]
        static void HighRoute() => Go("passo10.rota_alta");

        [MenuItem(MenuRoot + "5. Atrás do líder, na porta da tenda (golpe pelas costas)")]
        static void BehindLeader() => Go("passo10.lider");

        [MenuItem(MenuRoot + "6. Na tenda de comando, diante da mesa")]
        static void Tent() => Go("passo10.tenda");

        [MenuItem(MenuRoot + "7. Depois dos documentos: o painel do portão")]
        static void Panel() => Go("passo10.painel");

        [MenuItem(MenuRoot + "8. Portão aberto: saindo para o Poço")]
        static void Leave() => Go("passo10.saida");

        [MenuItem(MenuRoot + "8a. Último contêiner: o helicóptero cruza a avenida")]
        static void LastContainer() => Go("passo10.conteiner_gatilho");

        [MenuItem(MenuRoot + "8b. Perto da ronda do 2º líder, a leste")]
        static void EastLeader() => Go("passo10.lider_leste");

        // Mundo do fim do passo 9 (a Elena já dentro da base), mais estes eventos, com as armas.
        static void Begin(string[] extra, int questStep, Vector3 spawn, float yaw, string message, bool crouch = false)
        {
            Action then = () =>
            {
                ElenaShortcuts.GiveWeapons();
                if (crouch) After(0.2f, Crouch);
            };
            Step9Shortcuts.Start(revealed: true, extra: EnteredBase.Concat(extra ?? new string[0]).ToArray(), questStep, spawn, yaw, then);
            Debug.Log("Passo 10: " + message);
        }

        // Agacha a Elena (como se o jogador tivesse apertado Agachar).
        static void Crouch()
        {
            var motor = UnityEngine.Object.FindFirstObjectByType<Player.PlayerMotor>();
            if (motor != null) typeof(Player.PlayerMotor).GetProperty(nameof(Player.PlayerMotor.IsCrouching)).SetValue(motor, true);
        }
    }
}
