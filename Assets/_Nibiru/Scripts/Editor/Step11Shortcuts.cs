using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 11 (descer a Boca do Poço) sem jogar desde o
    /// Acampamento. Cada item liga o Play e põe a Elena no momento escolhido, com o mundo como se
    /// ela tivesse feito o plano de fuga, atravessado a base e saído pelo nordeste, e com pistola,
    /// balas e faca.
    /// Menu: Nibiru > Testes > Passo 11.
    /// </summary>
    [InitializeOnLoad]
    static class Step11Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 11/";

        // Passos da missão Sobrevivente depois do passo 10 (20 = sair da base pelo nordeste).
        const int MouthStep = 21;
        const int DescendStep = 22;
        const int PassLulluStep = 23;
        const int SealStep = 24;

        internal static readonly string[] LeftBase =
        {
            "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga", "EntrouNaBaseDaFundacao",
            "ChegouATendaDeComando", "LeuOEmailDoKast", "ViuAFotoDe1979", "LeuOsDocumentosDoKast",
            "AbriuOPortaoDaAldeia", "SaiuDaBaseRumoAoPoco",
        };

        // Onde cada atalho põe a Elena (os pés no chão) e para onde ela olha.
        static readonly Vector3 CorridorSpot = new Vector3(52f, 0f, 40f);
        static readonly Vector3 MouthSpot = new Vector3(45f, 0f, 64f);
        static readonly Vector3 ScaffoldSpot = new Vector3(41f, -3.5f, 79f);
        static readonly Vector3 FootCheckpointSpot = new Vector3(49f, -10.5f, 71f);
        static readonly Vector3 AfterLulluSpot = new Vector3(45f, -14f, 88f);
        // A área do flash começa em z = 89,9 e a Elena tem 0,5 m de raio: daqui ela fica 60 cm antes dela.
        internal static readonly Vector3 NearFlashSpot = new Vector3(45f, -14f, 88.8f);

        static Step11Shortcuts()
        {
            Register("passo11.corredor", () => Begin(null, MouthStep, CorridorSpot, 0f,
                "Passo 11: no corredor nordeste, saindo da base. Ande para o norte até a boca do Poço."));
            Register("passo11.boca", () => Begin(new[] { "ChegouABocaDoPoco" }, DescendStep, MouthSpot, 0f,
                "Passo 11: na boca do Poço, no alto dos andaimes. Desça pelos lances: lá embaixo se ouve o Lullu."));
            Register("passo11.andaime", () => Begin(new[] { "ChegouABocaDoPoco" }, DescendStep, ScaffoldSpot, 90f,
                "Passo 11: no patamar do primeiro lance. Continue descendo ouvindo o raspar do Lullu lá embaixo.", crouch: false));
            Register("passo11.pe", () => Begin(new[] { "ChegouABocaDoPoco" }, DescendStep, FootCheckpointSpot, 270f,
                "Passo 11: no ponto de retorno do pé da descida (num patamar, em silêncio). O Lullu vagueia no fundo; agachada, passe por ele."));
            Register("passo11.depois_lullu", () => Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu" }, SealStep, AfterLulluSpot, 0f,
                "Passo 11: na antecâmara, depois do Lullu (que não está mais lá). Ande para o norte: a uns 12 m do Selo começa o flash.", crouch: true));
            // "Passou pelo Lullu" acontece ao vivo, depois de recarregar, como quando ela cruza a boca
            // da antecâmara andando: guardado antes, o Lullu sumiria ao carregar a cena.
            Register("passo11.flash_vivo", () => Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco" }, PassLulluStep, NearFlashSpot, 0f,
                "Passo 11: na antecâmara, um passo antes do flash, com o Lullu ainda vivo no fundo. Ande para o norte: ele congela até a Voz falar.",
                raiseLive: new[] { "PassouPeloLullu" }));
            RegisterA2();
        }

        // A inscrição dos Guardiões (parede leste da antecâmara) e a sentinela (o 2º Lullu), depois do Lullu do fundo.
        static readonly Vector3 InscriptionSpot = new Vector3(52.6f, -14f, 83.9f);
        static readonly Vector3 SentinelViewSpot = new Vector3(46f, -14f, 85.5f);

        static void RegisterA2()
        {
            Register("passo11.inscricao", () => Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu" }, SealStep, InscriptionSpot, 90f,
                "Passo 11: diante da inscrição dos Guardiões, na parede leste da antecâmara (a sentinela vigia a uns 4 m, ao sul: ande agachada). Use \"Ler a inscrição\".", crouch: true));
            Register("passo11.sentinela", () => Begin(new[] { "ChegouABocaDoPoco", "DesceuOPoco", "PassouPeloLullu" }, SealStep, SentinelViewSpot, 90f,
                "Passo 11: na antecâmara, a uns 6 m da sentinela (o 2º Lullu, a leste). Ele ouve mais que o do fundo: agachada passa, andando ele vem. O flash começa ao norte (z = 90,5).", crouch: true));
        }

        [MenuItem(MenuRoot + "8. Diante da inscrição dos Guardiões (a sentinela perto)")]
        static void Inscription() => Go("passo11.inscricao");

        [MenuItem(MenuRoot + "9. Antecâmara, a uns 6 m da sentinela (2º Lullu)")]
        static void Sentinel() => Go("passo11.sentinela");

        [MenuItem(MenuRoot + "1. Saindo da base, a caminho do Poço")]
        static void Corridor() => Go("passo11.corredor");

        [MenuItem(MenuRoot + "2. Na boca do Poço")]
        static void Mouth() => Go("passo11.boca");

        [MenuItem(MenuRoot + "3. Descendo os andaimes, ouvindo o Lullu")]
        static void Scaffold() => Go("passo11.andaime");

        [MenuItem(MenuRoot + "4. Ponto de retorno do pé da descida (o Lullu no fundo)")]
        static void Foot() => Go("passo11.pe");

        [MenuItem(MenuRoot + "5. Depois do Lullu: rumo ao flash")]
        static void AfterLullu() => Go("passo11.depois_lullu");

        [MenuItem(MenuRoot + "6. Flash com o Lullu ainda vivo (ele congela)")]
        static void LiveFlash() => Go("passo11.flash_vivo");

        // Mundo de quem acabou de sair da base (passos 1 a 10), mais estes eventos, com as armas.
        // Os de raiseLive acontecem depois de recarregar. Também usado pelos atalhos do passo 12.
        internal static void Begin(string[] extra, int questStep, Vector3 spawn, float yaw, string message, bool crouch = false,
            string[] raiseLive = null, Action thenAlso = null)
        {
            Action then = () =>
            {
                foreach (string e in raiseLive ?? new string[0]) Raise(e);
                ElenaShortcuts.GiveWeapons();
                thenAlso?.Invoke();
                if (crouch) After(0.2f, Crouch);
            };
            Step9Shortcuts.Start(revealed: true, extra: LeftBase.Concat(extra ?? new string[0]).ToArray(), questStep, spawn, yaw, then);
            Debug.Log(message);
        }

        // Agacha a Elena (como se o jogador tivesse apertado Agachar).
        static void Crouch()
        {
            var motor = UnityEngine.Object.FindFirstObjectByType<Player.PlayerMotor>();
            if (motor != null) typeof(Player.PlayerMotor).GetProperty(nameof(Player.PlayerMotor.IsCrouching)).SetValue(motor, true);
        }
    }
}
