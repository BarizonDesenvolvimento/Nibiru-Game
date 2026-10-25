using System.Linq;
using Nibiru.Dialogue;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar o passo 14 (o wadi: tempestade, o rádio do Kast e a primeira
    /// escolha de facção). Cada item liga o Play e põe a Elena no momento escolhido, com o mundo como se
    /// ela tivesse descido o Poço, vencido o Primeiro Servo e atravessado a passagem dos Guardiões, e
    /// com pistola, balas e faca. Menu: Nibiru > Testes > Passo 14.
    /// </summary>
    [InitializeOnLoad]
    static class Step14Shortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Passo 14/";

        // Passos da missão Sobrevivente: 29 = seguir o wadi até o soldado; 30 = ouvir o rádio; 31 = decidir.
        internal const int FollowStep = 29;
        internal const int RadioStep = 30;

        // Fita pega e ainda tocando (pega no túnel e ouvida só em parte), fita já ouvida até o fim, fita nunca pega.
        internal static readonly string[] TapePlaying = Step13Shortcuts.TapeTaken.Append("SaiuDaPassagem").ToArray();
        internal static readonly string[] TapeDone = Step13Shortcuts.TapeFinished.Append("SaiuDaPassagem").ToArray();
        internal static readonly string[] AtSoldier = TapeDone.Append("ChegouAoSoldadoFerido").ToArray();
        internal static readonly string[] NoTape = Step13Shortcuts.Opened.Append("SaiuDaPassagem").ToArray();

        // Onde cada atalho põe a Elena (pés no chão) e para onde ela olha (0 = norte, 90 = leste).
        internal static readonly Vector3 RampTop = new Vector3(32f, 0f, 141.5f);
        internal static readonly Vector3 BeforeArrival = new Vector3(60f, 0f, 158.5f);
        internal static readonly Vector3 NearSoldier = new Vector3(60f, 0f, 164f);

        // Nomes dos atalhos que forçam cada uma das três opções (índice na conversa do rádio).
        internal static readonly string[] ChoiceIds = { "passo14.entregar", "passo14.devolver", "passo14.ficar" };

        static Step14Shortcuts()
        {
            Register("passo14.inicio", () => Step11Shortcuts.Begin(TapeDone, FollowStep, RampTop, 0f,
                "Passo 14: no topo da rampa, na entrada do wadi, com a fita de Irena já ouvida. Siga para o norte: a tempestade vai subindo, há um líder de esquadra patrulhando o trecho da curva, e o chiado do rádio se ouve de longe."));
            Register("passo14.radio_fita_tocando", () => Step11Shortcuts.Begin(TapePlaying, FollowStep, BeforeArrival, 0f,
                "Passo 14: a fita de Irena está tocando. Ande para o norte, até a entrada do trecho final: o rádio do Kast espera a fita terminar e começa sozinho, sem voltar ao gatilho."));
            Register("passo14.radio_sem_fita", () => Step11Shortcuts.Begin(NoTape, FollowStep, BeforeArrival, 0f,
                "Passo 14: a fita de Irena nunca foi pega. Ande para o norte, até a entrada do trecho final: o rádio do Kast começa na hora."));
            Register("passo14.escolha", () => Step11Shortcuts.Begin(AtSoldier, RadioStep, NearSoldier, 0f,
                "Passo 14: junto do soldado ferido. O rádio começa sozinho e as falas avançam sozinhas até as três opções da escolha.",
                thenAlso: () => SkipToChoice(-1)));
            for (int i = 0; i < ChoiceIds.Length; i++)
            {
                int pick = i;
                Register(ChoiceIds[i], () => Step11Shortcuts.Begin(AtSoldier, RadioStep, NearSoldier, 0f,
                    $"Passo 14: o rádio fala sozinho e a opção \"{new[] { "entregar a tabuleta à Fundação", "devolver ao Poço com o Samir", "ficar com a tabuleta" }[pick]}\" é escolhida no lugar do jogador.",
                    thenAlso: () => SkipToChoice(pick)));
            }
        }

        // Avança as falas do rádio como se o jogador apertasse Confirmar. Ao chegar às opções, escolhe a
        // de número "pick" (0 a 2) ou, com -1, deixa as opções na tela para o jogador.
        internal static void SkipToChoice(int pick)
        {
            double giveUp = EditorApplication.timeSinceStartup + 90.0;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > giveUp)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                var runner = Object.FindFirstObjectByType<DialogueRunner>();
                if (runner == null || !runner.IsRunning) return;
                if (!ChoiceOnScreen(runner)) { runner.Continue(); return; }
                EditorApplication.update -= tick;
                if (pick >= 0) runner.Choose(pick);
            };
            EditorApplication.update += tick;
        }

        // True se a fala mostrada tem opções (a conversa do rádio tem uma só fala com as três).
        internal static bool ChoiceOnScreen(DialogueRunner runner)
        {
            var visible = (System.Collections.IList)typeof(DialogueRunner)
                .GetField("visibleChoices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(runner);
            return visible.Count > 0;
        }

        [MenuItem(MenuRoot + "1. Início do wadi, no topo da rampa")]
        static void Start1() => Go("passo14.inicio");

        [MenuItem(MenuRoot + "2. Gatilho do rádio, com a fita de Irena tocando")]
        static void Start2() => Go("passo14.radio_fita_tocando");

        [MenuItem(MenuRoot + "3. Gatilho do rádio, sem a fita (nunca pega)")]
        static void Start3() => Go("passo14.radio_sem_fita");

        [MenuItem(MenuRoot + "4. Direto à escolha de facção (as falas avançam sozinhas)")]
        static void Start4() => Go("passo14.escolha");

        [MenuItem(MenuRoot + "5. Forçar: entregar a tabuleta à Fundação")]
        static void Start5() => Go(ChoiceIds[0]);

        [MenuItem(MenuRoot + "6. Forçar: devolver ao Poço com o Samir")]
        static void Start6() => Go(ChoiceIds[1]);

        [MenuItem(MenuRoot + "7. Forçar: ficar com a tabuleta")]
        static void Start7() => Go(ChoiceIds[2]);
    }
}
