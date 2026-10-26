using System;
using System.Linq;
using Nibiru.Inventory;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using static Nibiru.EditorTools.TestShortcuts;

namespace Nibiru.EditorTools
{
    /// <summary>
    /// Só no editor: atalhos para testar a secundária "Remédio para Yusuf" sem jogar desde o Acampamento. Cada item
    /// liga o Play e põe a Elena num momento da missão, com o mundo como se ela tivesse jogado até ali.
    /// Menu: Nibiru > Testes > Secundárias > Remédio para Yusuf.
    /// </summary>
    [InitializeOnLoad]
    static class RemedyShortcuts
    {
        const string MenuRoot = "Nibiru/Testes/Secundárias/Remédio para Yusuf/";

        // Passo da missão Sobrevivente em que a Elena acabou de descansar no nicho (passo 7 concluído).
        const int ReadTabletStep = 12;

        // Diante da Dra. Weiss (olhando para oeste, a leste da mochila dela), do Yusuf (olhando para oeste) e da mesa da tenda médica (para o norte).
        // A Weiss fica atrás de uma tenda sólida: a Elena fala com ela por leste, ao lado da mochila.
        internal static readonly Vector3 WeissFront = new Vector3(-18.4f, 0f, -51.2f);
        internal const float WeissYaw = 270f;
        internal static readonly Vector3 YusufFront = Step8Shortcuts.NicheSpot;
        internal static readonly Vector3 TentFront = new Vector3(-10f, 0f, 17.8f);
        internal static readonly Vector3 MuleFront = Step15Shortcuts.NearTruck;

        static readonly string[] MetWeiss = { "ConheceuDraWeiss" };
        static readonly string[] BaseEntered =
        {
            "ChegouABeiraDaAldeia", "OuviuOPlanoDeFuga", "EntrouNaBaseDaFundacao",
        };

        /// <summary>Os eventos do caminho da Dra. Weiss até a entrega (Yusuf vivo).</summary>
        internal static readonly string[] WeissRoute =
        {
            "ConheceuDraWeiss", "AjudouAWeiss", "ConseguiuAntibioticosComAWeiss", "WeissEntregouODocumento", "LeuODocumentoDaWeiss",
        };

        internal static readonly string[] Delivered = { "EntregouAntibioticos", "YusufSobreviveu" };

        /// <summary>Os eventos do caminho da tenda médica, depois de ajudar a Weiss só com o documento.</summary>
        internal static readonly string[] TentRoute =
        {
            "ConheceuDraWeiss", "ConseguiuAntibioticosNaTenda", "AjudouAWeiss", "WeissEntregouODocumento", "LeuODocumentoDaWeiss",
        };

        /// <summary>Vira true quando o último atalho acabou de arrumar o mundo (para o teste automático esperar).</summary>
        internal static bool Ready;

        /// <summary>Nos atalhos da viagem com escolha forçada, escolhe sozinho. O teste automático desliga isto e escolhe ele mesmo.</summary>
        internal static bool AutoChoose = true;

        static RemedyShortcuts()
        {
            Register("remedio.zero", () => Village(null, YusufFront, 270f, () => SetBag(0, 0),
                "Remédio para Yusuf, do zero: a Elena acabou de conhecer os sobreviventes e descansar no nicho, de frente para o Yusuf. A missão está no passo \"Consiga antibióticos\"."));
            Register("remedio.passo7_com_antibioticos", () => Village(null, YusufFront, 270f, () => SetBag(0, 1),
                "Passo 7 com os antibióticos já na bolsa. O objetivo avança para \"Leve os antibióticos ao Yusuf\": use \"Dar os antibióticos ao Yusuf\"."));
            Register("remedio.weiss_curativo", () => Village(MetWeiss, WeissFront, RemedyShortcuts.WeissYaw, () => SetBag(1, 0),
                "Diante da Dra. Weiss, que já foi conhecida, com 1 curativo. Use \"Dar os curativos à Dra. Weiss\": ela dá os antibióticos e o documento."));
            Register("remedio.weiss_com_antibioticos", () => Village(new[] { "ConheceuDraWeiss", "ConseguiuAntibioticosNaTenda" }, WeissFront, RemedyShortcuts.WeissYaw,
                () => SetBag(1, 1),
                "Diante da Dra. Weiss, com 1 curativo e os antibióticos da tenda já na bolsa. A troca dá só o documento."));
            Register("remedio.tenda", () => Base(TentFront, 0f, () => SetBag(0, 0),
                "Dentro da tenda médica da base, diante da mesa com os antibióticos. Pegue com o botão de interagir: o evento do caminho da tenda é disparado."));
            Register("remedio.lembrete", () => Step11Shortcuts.Begin(Step13Shortcuts.TapeFinished, Step13Shortcuts.FollowStep, Step13Shortcuts.TopSpot, 0f,
                "Pé do topo da rampa, com os antibióticos não entregues na bolsa. Ande para o norte: a Elena lembra do Yusuf ao sair da passagem.",
                thenAlso: () => { SetBag(0, 1); Ready = true; }));
            Register("remedio.lembrete_sem", () => Step11Shortcuts.Begin(Step13Shortcuts.TapeFinished, Step13Shortcuts.FollowStep, Step13Shortcuts.TopSpot, 0f,
                "Pé do topo da rampa, SEM antibióticos. Ande para o norte: a Elena não lembra de nada.",
                thenAlso: () => { SetBag(0, 0); Ready = true; }));
            Register("remedio.prazo", () => Step11Shortcuts.Begin(Step14Shortcuts.AtSoldier.Append("RadioDoKastComecou").ToArray(), Step14Shortcuts.RadioStep,
                Step14Shortcuts.NearSoldier, 0f,
                "Wadi, junto do soldado, com os antibióticos NÃO entregues. Decida o que fazer com a tabuleta: no fim do wadi o Yusuf morre fora da tela e a missão falha.",
                thenAlso: () => { SetBag(0, 1); Ready = true; }));

            Register("remedio.viagem_vivo_cobrir", () => Ride(WeissRoute.Concat(Delivered), 0, "o Yusuf vive, a Elena cobre a Weiss"));
            Register("remedio.viagem_vivo_contar", () => Ride(WeissRoute.Concat(Delivered), 1, "o Yusuf vive, a Elena conta sobre a Weiss"));
            Register("remedio.viagem_morto_cobrir", () => Ride(TentRoute, 0, "o Yusuf morreu (caminho da tenda), a Elena cobre a Weiss"));
            Register("remedio.viagem_morto_contar", () => Ride(TentRoute, 1, "o Yusuf morreu (caminho da tenda), a Elena conta sobre a Weiss"));
            Register("remedio.viagem_manual", () => Ride(WeissRoute.Concat(Delivered), -1, "escolha manual (o Yusuf vive)"));
            Register("remedio.viagem_sem_weiss", () => Ride(new string[0], -2, "sem ajudar a Weiss: a pergunta não aparece"));
        }

        // ---------- Montagem ----------

        // Aldeia ou Trincheiras, no fim do passo 7: o mundo de quem acabou de descansar no nicho.
        internal static void Village(string[] extra, Vector3 spawn, float yaw, Action then, string message)
        {
            Ready = false;
            string[] events = Step8Shortcuts.EventsUntilStep7.Concat(extra ?? new string[0]).ToArray();
            StartFrom(events, new[] { ("Sobrevivente", ReadTabletStep), ("RemedioParaYusuf", 0), ("OQueOTomViu", 0) },
                0, spawn, yaw, () => { then?.Invoke(); Ready = true; });
            Debug.Log(message);
        }

        // A base do zigurate, com a Elena dentro e o plano de fuga já feito.
        internal static void Base(Vector3 spawn, float yaw, Action then, string message)
        {
            Ready = false;
            Step9Shortcuts.Start(revealed: true, extra: BaseEntered, 17, spawn, yaw, () => { then?.Invoke(); Ready = true; });
            Debug.Log(message);
        }

        // A viagem na caminhonete de quem ficou com a tabuleta, com estes eventos da secundária. pick: 0 = cobrir,
        // 1 = contar, -1 = escolha manual, -2 = sem a pergunta (o teste só espera o fim).
        internal static void Ride(System.Collections.Generic.IEnumerable<string> remedyEvents, int pick, string what)
        {
            Ready = false;
            string[] events = Step15Shortcuts.Ficou.Concat(remedyEvents).ToArray();
            Step11Shortcuts.Begin(events, Step15Shortcuts.BoardStep, MuleFront, 0f,
                $"Viagem na caminhonete: {what}. A conversa \"A estrada\" vem primeiro e, depois dela, a pergunta do Samir (se a Elena ajudou a Weiss).",
                thenAlso: () =>
                {
                    SetBag(0, 0);
                    Step15Shortcuts.ApplyChoice(Step15Shortcuts.Pick.Ficou);
                    After(0.5f, () =>
                    {
                        Step15Shortcuts.Ride()?.BeginAt(0.5f);
                        if (pick >= 0 && AutoChoose) Step14Shortcuts.SkipToChoice(pick);
                        Ready = true;
                    });
                });
        }

        internal static void Give(string item, int count)
        {
            var inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            var definition = Load<ItemDefinition>("Items/" + item);
            if (inventory != null && definition != null) inventory.Add(definition, count);
        }

        // Chama o atalho. Com o Play ligado, esvazia antes a bolsa dos itens da missão: o ponto de retorno do atalho
        // guarda a bolsa de quem está jogando, e um antibiótico sobrando avançaria a missão ao recarregar a cena.
        internal static void Launch(string id)
        {
            if (EditorApplication.isPlaying) SetBag(0, 0);
            Go(id);
        }

        // O ponto de retorno do atalho guarda o inventário de quem estava jogando: antes de cada atalho, a bolsa fica só
        // com os curativos e antibióticos que ele pede (o que sobrou de um teste anterior sai).
        internal static void SetBag(int bandages, int antibiotics)
        {
            var inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return;
            foreach (var (item, count) in new[] { ("Curativo", bandages), ("Antibioticos", antibiotics) })
            {
                var definition = Load<ItemDefinition>("Items/" + item);
                inventory.Remove(definition, inventory.CountOf(definition));
                if (count > 0) inventory.Add(definition, count);
            }
        }

        // ---------- Menu ----------

        [MenuItem(MenuRoot + "1. Do zero: casa do altar, diante do Yusuf (sem antibióticos)")]
        static void Zero() => Launch("remedio.zero");

        [MenuItem(MenuRoot + "2. Passo 7 com antibióticos: diante do Yusuf")]
        static void Step7WithAntibiotics() => Launch("remedio.passo7_com_antibioticos");

        [MenuItem(MenuRoot + "3. Trincheiras: diante da Dra. Weiss, com 1 curativo")]
        static void WeissBandage() => Launch("remedio.weiss_curativo");

        [MenuItem(MenuRoot + "4. Trincheiras: diante da Dra. Weiss, já com os antibióticos da tenda")]
        static void WeissWithAntibiotics() => Launch("remedio.weiss_com_antibioticos");

        [MenuItem(MenuRoot + "5. Base: tenda médica, diante dos antibióticos")]
        static void Tent() => Launch("remedio.tenda");

        [MenuItem(MenuRoot + "6. Passo 14, saída da passagem: com antibióticos não entregues (a Elena lembra)")]
        static void Reminder() => Launch("remedio.lembrete");

        [MenuItem(MenuRoot + "7. Passo 14, saída da passagem: sem antibióticos (não lembra)")]
        static void ReminderWithout() => Launch("remedio.lembrete_sem");

        [MenuItem(MenuRoot + "8. Passo 14 sem entrega: junto do soldado, o prazo vence ao decidir")]
        static void Deadline() => Launch("remedio.prazo");

        [MenuItem(MenuRoot + "9. Viagem: Yusuf vivo, a Elena cobre a Weiss")]
        static void RideAliveCover() => Launch("remedio.viagem_vivo_cobrir");

        [MenuItem(MenuRoot + "10. Viagem: Yusuf vivo, a Elena conta sobre a Weiss")]
        static void RideAliveTell() => Launch("remedio.viagem_vivo_contar");

        [MenuItem(MenuRoot + "11. Viagem: Yusuf morto (caminho da tenda), a Elena cobre a Weiss")]
        static void RideDeadCover() => Launch("remedio.viagem_morto_cobrir");

        [MenuItem(MenuRoot + "12. Viagem: Yusuf morto (caminho da tenda), a Elena conta sobre a Weiss")]
        static void RideDeadTell() => Launch("remedio.viagem_morto_contar");

        [MenuItem(MenuRoot + "13. Viagem com a pergunta do Samir, escolha manual")]
        static void RideManual() => Launch("remedio.viagem_manual");

        [MenuItem(MenuRoot + "14. Viagem sem ter ajudado a Weiss (a pergunta não aparece)")]
        static void RideWithoutWeiss() => Launch("remedio.viagem_sem_weiss");
    }
}
