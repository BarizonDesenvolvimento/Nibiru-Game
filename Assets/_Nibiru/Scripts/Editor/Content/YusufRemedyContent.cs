using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Factions;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// A missão secundária "Remédio para Yusuf": os dois caminhos para os antibióticos (a Dra. Weiss, em troca
    /// de curativos, ou a tenda médica da base), a entrega ao Yusuf, o prazo no fim do wadi, a pergunta do
    /// Samir sobre a Weiss na viagem e os eventos de cada resultado.
    /// Menu: Nibiru > Conteúdo > Povoar Remédio para Yusuf na cena aberta.
    ///
    /// Rode depois dos menus das áreas (Trincheiras, Aldeia, Base, Wadi e Fuga): este menu troca a conversa de
    /// espera da Weiss e a do Yusuf por trocas de item (<see cref="ItemTrade"/>), liga um evento ao pickup dos
    /// antibióticos da tenda médica e acrescenta uma conversa extra à viagem da caminhonete. Se rodar de novo o
    /// menu de uma dessas áreas, rode este depois. Os arquivos de dados só são criados se ainda não existirem.
    /// </summary>
    static class YusufRemedyContent
    {
        const string RootName = "Conteudo_RemedioYusuf";

        class Events
        {
            public StoryEvent comAWeiss, naTenda, ajudou, documentoEntregue, leuDocumento, sobreviveu, morreu, fria, cobriu, denunciou;
            public StoryEvent conheceuSobreviventes, entregou, escolheuNoWadi, saiuDaPassagem;
        }

        class Dialogues
        {
            public DialogueDefinition tradeWithGift, tradeDocumentOnly, weissAfter, delivery, yusufBetter, ummGrateful, ummCold, reminder, roadWeiss;
        }

        static Events ev;
        static Dialogues dlg;
        static ItemDefinition antibiotics, bandage;
        static DocumentDefinition weissDocument;

        [MenuItem("Nibiru/Conteúdo/Povoar Remédio para Yusuf na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar o Remédio para Yusuf.");
                return;
            }

            IdPrefix = "remedio";
            if (!CreateData()) return;

            Transform root = NewRoot(RootName, "Povoar Remédio para Yusuf");

            // Prazo: no fim do wadi, sem a entrega, o Yusuf morre fora da tela.
            var deadline = root.gameObject.AddComponent<DeadlineWatcher>();
            SetField(deadline, "deadline", ev.escolheuNoWadi);
            SetField(deadline, "unlessRaised", ev.entregou);
            SetField(deadline, "onlyIfRaised", ev.conheceuSobreviventes);
            SetField(deadline, "raise", ev.morreu);

            var raiseCold = root.gameObject.AddComponent<StoryEventRaiser>();
            SetField(raiseCold, "storyEvent", ev.fria);
            Listener(root.gameObject, ev.morreu, e => Call(e, raiseCold.Raise));
            SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            BuildDocument(Group(root, "Documento_Da_Weiss"));
            BuildReminder(Group(root, "Lembrete_Do_Yusuf"));

            PatchWeiss();
            PatchYusufAndUmmKarim(root);
            PatchMedicalTentPickup();
            PatchTruckRide();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log("Remédio para Yusuf povoado.");
        }

        // ---------- Dados ----------

        static bool CreateData()
        {
            var quest = Load<QuestDefinition>("Quests/RemedioParaYusuf");
            ev = new Events
            {
                conheceuSobreviventes = Load<StoryEvent>("Events/ConheceuSobreviventes"),
                entregou = Load<StoryEvent>("Events/EntregouAntibioticos"),
                escolheuNoWadi = Load<StoryEvent>("Events/EscolheuONoWadi"),
                saiuDaPassagem = Load<StoryEvent>("Events/SaiuDaPassagem"),
            };
            antibiotics = Load<ItemDefinition>("Items/Antibioticos");
            bandage = Load<ItemDefinition>("Items/Curativo");
            if (quest == null || ev.conheceuSobreviventes == null || ev.entregou == null || ev.escolheuNoWadi == null
                || ev.saiuDaPassagem == null || antibiotics == null || bandage == null)
            {
                Debug.LogError("Remédio para Yusuf: faltam dados das áreas. Rode antes os menus da Aldeia, da Base, do Poço e do Wadi.");
                return false;
            }

            ev.comAWeiss = Event("ConseguiuAntibioticosComAWeiss", "A Dra. Weiss deu os antibióticos à Elena, em troca dos curativos (secundária Remédio para Yusuf).");
            ev.naTenda = Event("ConseguiuAntibioticosNaTenda", "A Elena pegou os antibióticos na tenda médica da base (secundária Remédio para Yusuf).");
            ev.ajudou = Event("AjudouAWeiss", "A Elena trocou curativos com a Dra. Weiss, com ou sem antibióticos como presente. A Weiss pediu silêncio sobre onde se esconde. A pergunta do Samir na viagem depende deste evento.");
            ev.documentoEntregue = Event("WeissEntregouODocumento", "A conversa da troca com a Dra. Weiss terminou: a Elena recebe o documento dela.");
            ev.leuDocumento = Event("LeuODocumentoDaWeiss", "A Elena leu o documento provisório que a Dra. Weiss entregou.");
            ev.sobreviveu = Event("YusufSobreviveu", "O Yusuf recebeu os antibióticos a tempo e sobrevive. Na região 2, avisa a Elena de uma emboscada (ainda não existe).");
            ev.morreu = Event("YusufMorreu", "O prazo (o fim do wadi) venceu sem a entrega dos antibióticos: o Yusuf morre fora da tela e a missão falha.");
            ev.fria = Event("UmmKarimFria", "O Yusuf morreu por falta do remédio: a Umm Karim fica fria com a Elena.");
            ev.cobriu = Event("CobriuAWeiss", "Na viagem, o Samir perguntou e a Elena escondeu dele a Dra. Weiss. Fundação +5, Guardiões -5.");
            ev.denunciou = Event("DenunciouAWeiss", "Na viagem, o Samir perguntou e a Elena contou onde a Dra. Weiss se esconde. Guardiões +5, Fundação -5. O destino dela fica em aberto.");

            weissDocument = Asset<DocumentDefinition>("Documents/Weiss_Documento", d =>
            {
                d.displayName = "Documento da Dra. Weiss (a definir)";
                d.text = "[Texto provisório. O conteúdo deste documento, tirado do briefing da equipe pela Dra. Weiss, ainda será escrito.]";
                d.footer = "Entregue pela Dra. Weiss, nas trincheiras.";
                d.raiseOnRead = ev.leuDocumento;
            });

            dlg = new Dialogues
            {
                tradeWithGift = Dialogue("Remedio_Troca_Weiss", ev.documentoEntregue,
                    N("e1", "Elena", "(Ela estende os curativos.) Achei estes.", "w1"),
                    N("w1", "Dra. Weiss", "Obrigada. Meu Deus, obrigada.", "w2"),
                    N("w2", "Dra. Weiss", "(Ela refaz a bandagem da perna com mãos treinadas, depois tira da mochila uma caixa de antibióticos e uma pasta.)", "w3"),
                    N("w3", "Dra. Weiss", "Os antibióticos são para o seu rapaz. E isto é o que consegui tirar do briefing. Não entendo metade. Talvez a senhora entenda.", "w4"),
                    N("w4", "Dra. Weiss", "Uma coisa, doutora. Se alguém perguntar por mim... não me viu. Preciso de tempo para pensar no que vou fazer.", "e2"),
                    N("e2", "Elena", "Descanse a perna.")),

                tradeDocumentOnly = Dialogue("Remedio_Troca_Weiss_SoDocumento", ev.documentoEntregue,
                    N("e1", "Elena", "(Ela estende os curativos.) Achei estes.", "w1"),
                    N("w1", "Dra. Weiss", "Obrigada. Meu Deus, obrigada.", "w2"),
                    N("w2", "Dra. Weiss", "(Ela refaz a bandagem da perna e tira uma pasta da mochila.) A senhora já tem o remédio de que precisava. Então fique com isto: é o que consegui tirar do briefing.", "w3"),
                    N("w3", "Dra. Weiss", "Uma coisa, doutora. Se alguém perguntar por mim... não me viu. Preciso de tempo para pensar no que vou fazer.", "e2"),
                    N("e2", "Elena", "Descanse a perna.")),

                weissAfter = Dialogue("Trincheiras_DraWeissDepois", null,
                    N("w1", "Dra. Weiss", "Ainda estou aqui. Se alguém perguntar por mim, a senhora não me viu.")),

                delivery = Dialogue("Remedio_Entrega_Yusuf", null,
                    N("e1", "Elena", "(Ela se ajoelha junto da esteira e mostra a caixa à Umm Karim.) Antibióticos. Ele tem que tomar tudo, mesmo quando a febre baixar.", "u1"),
                    N("u1", "Umm Karim", "(Ela pega a caixa com as duas mãos.) Que Deus a guarde, doutora. Esta casa vai lembrar.", "y1"),
                    N("y1", "Yusuf", "(Ele abre os olhos, devagar.) A senhora... voltou.", "e2"),
                    N("e2", "Elena", "Descanse. Eu volto para ver como você está.")),

                yusufBetter = Dialogue("Aldeia_YusufMelhor", null,
                    N("y1", "Yusuf", "A febre baixou, doutora. Mas aquela respiração debaixo das pedras... eu ainda ouço.")),

                ummGrateful = Dialogue("Aldeia_UmmKarimGrata", null,
                    N("u1", "Umm Karim", "Ele dorme sem tremer. Que Deus a guarde, doutora. A casa é sua.")),

                ummCold = Dialogue("Aldeia_UmmKarimFria", null,
                    N("u1", "Umm Karim", "(Ela não levanta o rosto.) A senhora prometeu. Eu esperei. Não tenho mais nada para dizer.")),

                reminder = Dialogue("Remedio_Lembrete_Yusuf", null,
                    N("e1", "Elena", "(A caixa de antibióticos pesa na bolsa.)", "e2"),
                    N("e2", "Elena", "O Yusuf ainda está lá, queimando de febre. Voltar pelo Poço e pela base ainda dá tempo... mas o Kast está atrás de mim, e o Samir me espera.", "e3"),
                    N("e3", "Elena", "Depois que eu seguir pelo wadi, não dá mais.")),

                roadWeiss = BuildRoadWeiss(),
            };

            if (quest.failOn == null) quest.failOn = ev.morreu;
            if (quest.raiseOnComplete == null) quest.raiseOnComplete = ev.sobreviveu;
            EditorUtility.SetDirty(quest);
            return true;
        }

        // A pergunta do Samir na caminhonete, só para quem ajudou a Weiss (com o Yusuf vivo ou morto).
        static DialogueDefinition BuildRoadWeiss()
        {
            var foundation = Load<FactionDefinition>("Factions/FundacaoEnlil");
            var guardians = Load<FactionDefinition>("Factions/Guardioes");
            var cover = new DialogueChoice
            {
                text = "Não vi ninguém. Só os mortos.", next = "cobriu", raise = ev.cobriu,
                reputation = new[] { new ReputationChange { faction = foundation, amount = 5 }, new ReputationChange { faction = guardians, amount = -5 } },
            };
            var tell = new DialogueChoice
            {
                text = "Vi. Uma médica, Hannah Weiss, numa vala junto da tenda médica.", next = "contou", raise = ev.denunciou,
                reputation = new[] { new ReputationChange { faction = guardians, amount = 5 }, new ReputationChange { faction = foundation, amount = -5 } },
            };
            return Dialogue("Fuga_Estrada_Weiss", null,
                N("s1", "Samir", "Antes de Tell Amar, doutora, uma pergunta. Nas trincheiras, antes da Aldeia... a senhora viu alguém? Alguém da Fundação, ferido, escondido?", null, cover, tell),
                N("cobriu", "Samir", "Se a senhora diz que não... então não viu.", "cobriu2"),
                N("cobriu2", "Elena", "(Ela olha a tempestade e pensa nas mãos da médica, refazendo a bandagem.)"),
                N("contou", "Samir", "Uma médica da Fundação. Ela sabe coisas que precisamos saber.", "contou2"),
                N("contou2", "Elena", "(Ele aperta o volante. Ela não pergunta o que ele vai fazer com isso.)"));
        }

        // ---------- Cena ----------

        // O documento da Weiss aparece na tela de leitura quando a conversa da troca termina.
        static void BuildDocument(Transform g)
        {
            var view = g.gameObject.AddComponent<DocumentView>();
            SetField(view, "document", weissDocument);
            var sequence = g.gameObject.AddComponent<TimedSequence>();
            SetSteps(sequence, S("o documento da Weiss", 0.4f, e => Call(e, view.Show)));
            Listener(g.gameObject, ev.documentoEntregue, e => Call(e, sequence.Play));
            SetField(g.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        // Ao sair da passagem (começo do passo 14), a Elena lembra do Yusuf, só se ainda tem os antibióticos.
        static void BuildReminder(Transform g)
        {
            DialogueStarter talk = DialogueTrigger(g, "Dialogo_Lembrete", dlg.reminder);
            var branch = g.gameObject.AddComponent<ItemBranch>();
            SetField(branch, "item", antibiotics);
            Call(GetField<UnityEngine.Events.UnityEvent>(branch, "ifHas"), talk.StartDialogue);
            Listener(g.gameObject, ev.saiuDaPassagem, e => Call(e, branch.Run));
            SetField(g.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        static GameObject Obj(string name) => Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(g => g.name == name && g.scene.IsValid() && g.scene.isLoaded && !EditorUtility.IsPersistent(g));

        // Troca a conversa de espera da Weiss por uma troca de curativos (o mesmo lugar, a mesma tecla).
        static void PatchWeiss()
        {
            GameObject zone = Obj("Falar_DraWeiss_Depois");
            if (zone == null)
            {
                Debug.LogWarning("Remédio para Yusuf: a zona \"Falar_DraWeiss_Depois\" não existe. Rode o menu das Trincheiras.");
                return;
            }
            ItemTrade trade = ReplaceStarter(zone);
            SetField(trade, "tradePrompt", "Dar os curativos à Dra. Weiss");
            SetField(trade, "takeItem", bandage);
            SetField(trade, "giveItem", antibiotics);
            SetField(trade, "skipGiveIfRaised", ev.entregou);
            SetField(trade, "raiseOnTrade", ev.ajudou);
            SetField(trade, "raiseOnGift", ev.comAWeiss);
            SetField(trade, "dialogueWithGift", dlg.tradeWithGift);
            SetField(trade, "dialogueWithoutGift", dlg.tradeDocumentOnly);
            SetField(trade, "waitingDialogue", Load<DialogueDefinition>("Dialogues/Trincheiras_DraWeissEspera"));
            SetField(trade, "talkPrompt", "Falar com a Dra. Weiss");
            SetField(trade, "afterDialogue", dlg.weissAfter);
        }

        // Yusuf recebe os antibióticos; a Umm Karim muda de fala com o resultado.
        static void PatchYusufAndUmmKarim(Transform root)
        {
            GameObject yusufZone = Obj("Falar_Yusuf");
            if (yusufZone == null) Debug.LogWarning("Remédio para Yusuf: a zona \"Falar_Yusuf\" não existe. Rode o menu da Aldeia.");
            else
            {
                ItemTrade trade = ReplaceStarter(yusufZone);
                SetField(trade, "tradePrompt", "Dar os antibióticos ao Yusuf");
                SetField(trade, "takeItem", antibiotics);
                SetField(trade, "raiseOnTrade", ev.entregou);
                SetField(trade, "blockedIfRaised", ev.morreu);
                SetField(trade, "dialogueWithoutGift", dlg.delivery);
                SetField(trade, "waitingDialogue", Load<DialogueDefinition>("Dialogues/Aldeia_YusufEspera"));
                SetField(trade, "talkPrompt", "Falar com Yusuf");
                SetField(trade, "afterDialogue", dlg.yusufBetter);
            }

            GameObject ummZone = Obj("Falar_UmmKarim");
            if (ummZone == null)
            {
                Debug.LogWarning("Remédio para Yusuf: a zona \"Falar_UmmKarim\" não existe. Rode o menu da Aldeia.");
                return;
            }
            var umm = ummZone.GetComponent<DialogueStarter>();
            Listener(root.gameObject, ev.entregou, e => Call(e, umm.SetDialogue, dlg.ummGrateful));
            Listener(root.gameObject, ev.fria, e => Call(e, umm.SetDialogue, dlg.ummCold));
        }

        static ItemTrade ReplaceStarter(GameObject zone)
        {
            var starter = zone.GetComponent<DialogueStarter>();
            if (starter != null) Undo.DestroyObjectImmediate(starter);
            var trade = zone.GetComponent<ItemTrade>();
            if (trade == null) trade = Undo.AddComponent<ItemTrade>(zone);
            return trade;
        }

        // Pegar os antibióticos da tenda médica dispara o evento do caminho "tenda".
        static void PatchMedicalTentPickup()
        {
            ItemPickup pickup = Resources.FindObjectsOfTypeAll<ItemPickup>()
                .FirstOrDefault(p => p.gameObject.scene.IsValid() && p.gameObject.scene.isLoaded && GetItem(p) == antibiotics);
            if (pickup == null)
            {
                Debug.LogWarning("Remédio para Yusuf: não achei os antibióticos da tenda médica. Rode o menu da Base.");
                return;
            }
            SetField(pickup, "raiseOnPickup", ev.naTenda);
        }

        static ItemDefinition GetItem(ItemPickup pickup) =>
            (ItemDefinition)typeof(ItemPickup).GetField("item", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(pickup);

        // A pergunta do Samir entra na viagem, depois da conversa "A estrada", só para quem ajudou a Weiss.
        static void PatchTruckRide()
        {
            TruckRide ride = Resources.FindObjectsOfTypeAll<TruckRide>()
                .FirstOrDefault(r => r.gameObject.scene.IsValid() && r.gameObject.scene.isLoaded);
            if (ride == null)
            {
                Debug.LogWarning("Remédio para Yusuf: a caminhonete não existe. Rode o menu da Fuga.");
                return;
            }
            SetField(ride, "followups", new[] { new TruckRide.Followup { when = ev.ajudou, dialogue = dlg.roadWeiss } });
        }
    }
}
