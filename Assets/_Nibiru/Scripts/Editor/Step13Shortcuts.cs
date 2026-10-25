using System.Linq;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 13 (a passagem dos Guardiões atrás da parede do
    /// barco de junco). Cada item liga o Play e põe a Elena no momento escolhido, com o mundo como se
    /// ela tivesse descido o Poço e vencido o Primeiro Servo, e com pistola, balas e faca.
    /// Menu: Nibiru > Testes > Passo 13.
    /// </summary>
    [InitializeOnLoad]
    static class Step13Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 13/";

        // Passos da missão Sobrevivente (27 = ler a inscrição e abrir a passagem; 28 = seguir até o wadi).
        internal const int OpenStep = 27;
        internal const int FollowStep = 28;

        internal static readonly string[] AtWall = Step12Shortcuts.Won.Append("ChegouAParedeDoBarco").ToArray();
        internal static readonly string[] Opened = AtWall.Concat(new[] { "LeuAInscricaoDaPassagem", "AbriuAPassagem" }).ToArray();
        internal static readonly string[] TapeTaken = Opened.Append("ColetouAFitaDeIrena1").ToArray();
        internal static readonly string[] TapeFinished = TapeTaken.Append("FitaDeIrena1Terminou").ToArray();

        // Onde cada atalho põe a Elena (os pés no chão) e para onde ela olha (270 = oeste, 0 = norte).
        internal static readonly Vector3 WallSpot = new Vector3(38.2f, -14f, 96f);
        internal static readonly Vector3 GapSpot = new Vector3(38f, -14f, 96f);
        internal static readonly Vector3 TapeSpot = new Vector3(31.8f, -14f, 105.8f);
        internal static readonly Vector3 RampSpot = new Vector3(32f, -7f, 125f);
        internal static readonly Vector3 TopSpot = new Vector3(32f, -1.4f, 137f);
        internal static readonly Vector3 TunnelSpot = new Vector3(32f, -14f, 97f);

        static Step13Shortcuts()
        {
            Register("passo13.parede", () => Step11Shortcuts.Begin(AtWall, OpenStep, WallSpot, 270f,
                "Passo 13: diante da parede do barco de junco, com o Servo caído. Aperte o botão de interagir: \"Ler a inscrição\". A tabuleta pulsa e a parede afunda."));
            Register("passo13.aberta", () => Step11Shortcuts.Begin(Opened, FollowStep, GapSpot, 270f,
                "Passo 13: a passagem já está aberta (como no ponto de retorno depois da leitura). Entre no túnel, a oeste: lamparinas acesas e a fita de Irena mais adiante."));
            Register("passo13.fita", () => Step11Shortcuts.Begin(Opened, FollowStep, TapeSpot, 300f,
                "Passo 13: no túnel, junto da fita de Irena (degrau de pedra à esquerda). Pegue com o botão de interagir e siga andando: a fita toca sem travar o controle."));
            Register("passo13.rampa", () => Step11Shortcuts.Begin(TapeTaken, FollowStep, RampSpot, 0f,
                "Passo 13: na rampa, com a fita já pega e ainda não ouvida até o fim (como ao voltar do salvamento): ela toca de novo do começo. Suba até o topo."));
            Register("passo13.topo", () => Step11Shortcuts.Begin(TapeFinished, FollowStep, TopSpot, 0f,
                "Passo 13: no pé do topo da rampa. Ande para o norte: o salvamento automático e o fim do passo (a saída para o wadi)."));
            Register("passo13.antecamara", () => Step11Shortcuts.Begin(Opened, FollowStep, TunnelSpot, 90f,
                "Passo 13: no começo do túnel, de frente para a antecâmara. Volte até ela: com a passagem aberta, nada deve travar."));
        }

        [MenuItem(MenuRoot + "1. Diante da parede, pronta para ler a inscrição")]
        static void Wall() => Go("passo13.parede");

        [MenuItem(MenuRoot + "2. Passagem aberta, na entrada do túnel")]
        static void Opened_() => Go("passo13.aberta");

        [MenuItem(MenuRoot + "3. Junto da fita de Irena")]
        static void Tape() => Go("passo13.fita");

        [MenuItem(MenuRoot + "4. Na rampa, com a fita já pega (toca de novo)")]
        static void Ramp() => Go("passo13.rampa");

        [MenuItem(MenuRoot + "5. Pé do topo da rampa: salvamento e saída para o wadi")]
        static void Top() => Go("passo13.topo");

        [MenuItem(MenuRoot + "6. Voltar do túnel à antecâmara")]
        static void Back() => Go("passo13.antecamara");
    }
}
