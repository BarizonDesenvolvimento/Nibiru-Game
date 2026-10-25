using Nibiru.Dialogue;
using Nibiru.Environment;
using Nibiru.Factions;
using Nibiru.Quests;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Passo 14 da missão "Sobrevivente": o wadi. Tempestade de areia, o soldado ferido com o rádio, o
    /// Kast na frequência e a primeira escolha de facção ("O que fazer com a tabuleta").
    /// Menu: Nibiru > Conteúdo > Povoar Wadi na cena aberta. Os eventos, as conversas e os passos da
    /// missão são criados aqui (só se ainda não existirem: textos mudados no Inspector ficam).
    /// </summary>
    static class WadiContent
    {
        internal class Events
        {
            public StoryEvent chegouAoSoldado, radioComecou, entregou, devolveu, ficou, escolheu;
        }

        internal static Events ev;
        internal static DialogueDefinition radioKast, radioKastDesconfia;
        internal static StormSettings stormSettings;

        [MenuItem("Nibiru/Conteúdo/Povoar Wadi na cena aberta")]
        static void Populate()
        {
            CreateData();
            Debug.Log("Wadi: dados criados (eventos, conversas e passos da missão).");
        }

        internal static void CreateData()
        {
            ev = new Events
            {
                chegouAoSoldado = Event("ChegouAoSoldadoFerido", "A Elena chegou ao soldado ferido no wadi, junto do rádio caído (passo 14)."),
                radioComecou = Event("RadioDoKastComecou", "O Kast começou a falar pela frequência do soldado ferido no wadi. Só começa com a fita de Irena nº 1 fora do ar."),
                entregou = Event("EntregouATabuleta", "Primeira escolha de facção: a Elena deixou a tabuleta no wadi para a Fundação. Fundação +15, Guardiões -15. O passo 15 e a região 2 leem isto."),
                devolveu = Event("DevolveuATabuleta", "Primeira escolha de facção: a Elena vai devolver a tabuleta ao Poço com o Samir. Guardiões +15, Fundação -15. O passo 15 e a região 2 leem isto."),
                ficou = Event("FicouComATabuleta", "Primeira escolha de facção: a Elena ficou com a tabuleta. Fundação -5, Guardiões -5, transformação +3 escondida. O passo 15 e a região 2 leem isto."),
                escolheu = Event("EscolheuONoWadi", "A Elena terminou a conversa do rádio e a primeira escolha de facção está feita (qualquer das três). Fecha o passo 14."),
            };

            if (!AssetDatabase.IsValidFolder($"{DataFolder}/Tempestade")) AssetDatabase.CreateFolder(DataFolder, "Tempestade");
            stormSettings = Asset<StormSettings>("Tempestade/TempestadeDoWadi", t => { });

            FactionDefinition foundation = Load<FactionDefinition>("Factions/FundacaoEnlil");
            FactionDefinition guardians = Load<FactionDefinition>("Factions/Guardioes");
            StoryEvent kastDistrusted = Load<StoryEvent>("Events/KastDesconfiou");

            radioKast = RadioDialogue("Wadi_RadioKast", "Doutora Varga. Sabia que a senhora atravessaria. Sua mãe também atravessava.", foundation, guardians);
            radioKastDesconfia = RadioDialogue("Wadi_RadioKast_Desconfia", "Doutora Varga. Eu disse que torcíamos pela senhora, e torcíamos. Agora a senhora está aí, e eu preciso ser franco.", foundation, guardians);
            if (kastDistrusted == null) Debug.LogWarning("Wadi: falta o evento KastDesconfiou. Rode o menu do Acampamento.");

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest != null)
                AppendSteps(mainQuest,
                    Step("Siga o wadi até o soldado ferido", ev.chegouAoSoldado),
                    Step("Ouça o rádio do soldado", ev.radioComecou),
                    Step("Decida o que fazer com a tabuleta", ev.escolheu));

            AssetDatabase.SaveAssets();
        }

        // O Kast fala pelo rádio do soldado; o Samir entra na mesma frequência (rádio de campo da caminhonete,
        // provisório) e a Voz sussurra. Ao fim, as três opções. Texto provisório.
        static DialogueDefinition RadioDialogue(string name, string opening, FactionDefinition foundation, FactionDefinition guardians) =>
            Dialogue(name, ev.escolheu,
                N("k1", "Kast (rádio)", opening, "k2"),
                N("k2", "Kast (rádio)", "Os moradores da Aldeia estão cercados. Todos: o rapaz doente, a senhora idosa, quem mais estiver nas casas. Meus homens esperam uma palavra minha.", "k3"),
                N("k3", "Kast (rádio)", "Deixe a tabuleta no chão do wadi, onde está, e siga seu caminho. Meus homens a buscam, soltam todos e saem da Aldeia. Ninguém mais se machuca.", "s1"),
                N("s1", "Samir (rádio)", "Doutora, não ouça. A tabuleta tem de voltar para o Poço, enterrada no nicho. Se ele a levar, não sobra ninguém para impedi-lo.", "v1"),
                N("v1", "Voz", "(baixinho, só para ela) Não abra mão de nada.", "escolha"),
                N("escolha", "Elena", "(O rádio chia. A tabuleta está morna na bolsa. O que fazer com ela?)", null,
                    Choice("Deixar a tabuleta no chão para o Kast.", "entrega", ev.entregou, (foundation, 15), (guardians, -15)),
                    Choice("Levar a tabuleta de volta ao Poço, com o Samir.", "devolve", ev.devolveu, (guardians, 15), (foundation, -15)),
                    Choice("Ficar com a tabuleta e não dizer nada.", "fica", ev.ficou, (foundation, -5), (guardians, -5))),
                N("entrega", "Kast (rádio)", "Sensata, doutora. Meus homens a buscam. Os soldados deixam a Aldeia ao amanhecer.", "entrega2"),
                N("entrega2", "Samir (rádio)", "Você deu uma chave para eles, doutora. Uma chave.", "fim"),
                N("devolve", "Kast (rádio)", "Lamentável.", "devolve2"),
                N("devolve2", "Samir (rádio)", "Venha. Eu espero na caminhonete.", "fim"),
                N("fica", "Voz", "Sábia.", "fim"),
                N("fim", "Elena", "(Ela desliga o rádio. O vento cobre o chiado.)"));

        static DialogueChoice Choice(string text, string next, StoryEvent raise, params (FactionDefinition faction, int amount)[] changes)
        {
            var choice = C(text, next, raise);
            choice.reputation = System.Array.ConvertAll(changes, c => new ReputationChange { faction = c.faction, amount = c.amount });
            return choice;
        }
    }
}
