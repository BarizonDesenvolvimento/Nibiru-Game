using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace Nibiru.EditorTools.Content
{
    // Dentro do namespace, para as ferramentas do kit valerem antes de nomes como Nibiru.Dialogue.
    using static ContentKit;

    /// <summary>
    /// Povoa as Trincheiras (área 2 do Sítio de Ur) com o passo 5 da missão "Sobrevivente":
    /// a ordem "a doutora, viva; o resto, não" pelo rádio dos soldados, a primeira furtividade,
    /// a pistola no corpo do chefe de segurança, o helicóptero passando baixo, a Dra. Weiss
    /// escondida (só a primeira conversa), o 2º rolo de filme do Tom e o rádio de campanha.
    ///
    /// Os arquivos de dados só são criados se ainda não existirem, e os passos novos da missão
    /// só entram se ainda não estiverem lá. Os objetos da cena ficam em "Conteudo_Trincheiras"
    /// e são reconstruídos a cada execução. Rode depois do Acampamento (ele monta as missões).
    /// Menu: Nibiru > Conteúdo > Povoar Trincheiras na cena aberta.
    /// </summary>
    static class TrenchContent
    {
        const string RootName = "Conteudo_Trincheiras";

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent viuExecucao, chegouAsTrincheiras, ouviuOrdemNoRadio, pegouPistola, helicopteroPassou,
                conheceuDraWeiss, chegouAAldeia;
        }

        class Dialogues
        {
            public DialogueDefinition ordemRadio, pistola, draWeiss, draWeissEspera;
        }

        static Events ev;
        static Dialogues dlg;
        static ItemDefinition pistol, ammo, bandage, filmRoll, fieldRadio;

        [MenuItem("Nibiru/Conteúdo/Povoar Trincheiras na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar as Trincheiras.");
                return;
            }

            IdPrefix = "trincheiras";
            CreateData();

            Transform root = NewRoot(RootName, "Povoar Trincheiras");
            BuildEntry(Group(root, "Entrada"));
            BuildCover(Group(root, "Coberturas"));
            BuildSecurityChief(Group(root, "Chefe_Seguranca"));
            BuildSoldiers(Group(root, "Soldados"));
            BuildHelicopterPass(Group(root, "Sobrevoo"));
            BuildWeiss(Group(root, "Dra_Weiss"));
            BuildFinds(Group(root, "Achados"));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Trincheiras povoadas.");
        }

        static void CreateData()
        {
            ev = new Events
            {
                viuExecucao = Event("ViuExecucao", "Os soldados executaram o trabalhador ferido (vista do mirante ou ouvida de longe). Os soldados começam a procurar a Elena."),
                chegouAsTrincheiras = Event("ChegouAsTrincheiras", "A Elena desceu da elevação e chegou às trincheiras."),
                ouviuOrdemNoRadio = Event("OuviuOrdemNoRadio", "Nas trincheiras, a Elena ouviu pelo rádio dos soldados: \"a doutora, viva; o resto, não\"."),
                pegouPistola = Event("PegouPistola", "A Elena pegou a pistola no corpo do chefe de segurança (Trincheiras)."),
                helicopteroPassou = Event("HelicopteroPassou", "O helicóptero da Fundação terminou de passar baixo sobre as trincheiras."),
                conheceuDraWeiss = Event("ConheceuDraWeiss", "A Elena falou com a Dra. Hannah Weiss, ferida e escondida nas Trincheiras. Ela pediu curativos e disse ter antibióticos."),
                chegouAAldeia = Event("ChegouAAldeia", "A Elena chegou à Aldeia dos escavadores (o gatilho entra com o conteúdo da Aldeia)."),
            };

            pistol = Load<ItemDefinition>("Items/PistolaMakarov");
            ammo = Load<ItemDefinition>("Items/Municao9mm");
            bandage = Load<ItemDefinition>("Items/Curativo");
            filmRoll = Load<ItemDefinition>("Items/RoloDeFilmeDoTom");

            fieldRadio = Asset<ItemDefinition>("Items/RadioDeCampanha", item =>
            {
                item.category = ItemCategory.Chave;
            });

            dlg = new Dialogues
            {
                ordemRadio = Dialogue("Trincheiras_OrdemRadio", ev.ouviuOrdemNoRadio,
                    N("r1", "Rádio dos soldados", "r2"),
                    N("r2", "Rádio dos soldados", "r3"),
                    N("r3", "Soldado (rádio)", "e1"),
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "e3"),
                    N("e3", "Elena")),

                pistola = Dialogue("Trincheiras_Pistola", null,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "e3"),
                    N("e3", "Elena")),

                draWeiss = Dialogue("Trincheiras_DraWeiss", ev.conheceuDraWeiss,
                    N("w1", "Mulher ferida", "e1"),
                    N("e1", "Elena", "w2"),
                    N("w2", "Dra. Weiss", "w3"),
                    N("w3", "Dra. Weiss", null,
                        C("c1", "contou", null),
                        C("c2", "calou", null)),
                    N("contou", "Dra. Weiss", "w4"),
                    N("calou", "Dra. Weiss", "w4"),
                    N("w4", "Dra. Weiss", "e2"),
                    N("e2", "Elena")),

                draWeissEspera = Dialogue("Trincheiras_DraWeissEspera", null,
                    N("w1", "Dra. Weiss")),
            };

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("s8", item: pistol),
                    Step("s9", ev.chegouAAldeia));

            AssetDatabase.SaveAssets();
        }

        // ---------- Entrada: a ordem pelo rádio ----------

        static void BuildEntry(Transform g)
        {
            var poster = g.gameObject.AddComponent<NotificationPoster>();
            var radio = DialogueTrigger(g, "Dialogo_OrdemRadio", dlg.ordemRadio);

            // Logo depois de entrar, o rádio de um soldado por perto; depois, a dica de agachar.
            var entry = g.gameObject.AddComponent<TimedSequence>();
            SetField(entry, "skipIfRaised", ev.ouviuOrdemNoRadio);
            SetSteps(entry,
                S("rádio dos soldados", 1.5f, e => Call(e, radio.StartDialogue)),
                S("dica de agachar", 0.5f, e => Notify(e, poster, "Agachada (C ou Ctrl), a Elena faz menos barulho e some atrás dos taludes")));
            Listener(g.gameObject, ev.chegouAsTrincheiras, e => Call(e, entry.Play));

            Checkpoint(g, "Checkpoint_Entrada", new Vector3(0f, 1f, -77f), new Vector3(16f, 2f, 3f), new Vector3(0f, 0f, -77.5f), 0f);
        }

        // ---------- Coberturas: lonas de sombra sobre as valas ----------

        // Lonas esticadas em postes, como nas escavações de verdade: o helicóptero não vê embaixo delas.
        static void BuildCover(Transform g)
        {
            Tarp(g, "Lona_1", new Vector3(-12f, 0f, -70.5f), new Vector2(4.5f, 4f), "BO_LonaVerde");
            Tarp(g, "Lona_2", new Vector3(-8f, 0f, -60f), new Vector2(4f, 4f), "BO_LonaLaranja");
            Tarp(g, "Lona_3", new Vector3(10f, 0f, -51f), new Vector2(4.5f, 4f), "BO_LonaVerde");
            Tarp(g, "Lona_4", new Vector3(-20f, 0f, -61f), new Vector2(3.5f, 3.5f), "BO_LonaLaranja");
        }

        static void Tarp(Transform parent, string name, Vector3 ground, Vector2 size, string material)
        {
            const float height = 2.3f;
            Transform t = Group(parent, name);
            Prim(PrimitiveType.Cube, t, "Lona", ground + Vector3.up * height, new Vector3(size.x, 0.05f, size.y), material);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -0.5f : 0.5f) * (size.x - 0.2f);
                float z = (i < 2 ? -0.5f : 0.5f) * (size.y - 0.2f);
                Prim(PrimitiveType.Cylinder, t, $"Poste_{i + 1}", ground + new Vector3(x, height / 2f, z), new Vector3(0.06f, height / 2f, 0.06f), "BO_Madeira", collider: ColliderMode.None);
            }
        }

        // ---------- Chefe de segurança: pistola e munição ----------

        static void BuildSecurityChief(Transform g)
        {
            // No disco dourado do blockout (Marca_Corpo_ChefeSeguranca_Pistola).
            var body = new Vector3(8f, 0f, -60f);
            Body(g, "Corpo_ChefeSeguranca", body, 70f, "BO_VerdeOliva");

            GameObject gun = Prim(PrimitiveType.Cube, g, "Pistola_Makarov", body + new Vector3(0.7f, 0.06f, 0.5f), new Vector3(0.05f, 0.12f, 0.2f), "BO_Metal", 35f);
            Pickup(gun, pistol, 1, ev.pegouPistola);

            GameObject rounds = Prim(PrimitiveType.Cube, g, "Municao_ChefeSeguranca", body + new Vector3(-0.6f, 0.05f, 0.6f), new Vector3(0.16f, 0.1f, 0.1f), "BO_VerdeOliva", -20f);
            Pickup(rounds, ammo, 8, null);

            // A fala da Elena ao pegar a arma (só ao vivo, não ao voltar de um checkpoint).
            var reaction = DialogueTrigger(g, "Dialogo_Pistola", dlg.pistola);
            Listener(g.gameObject, ev.pegouPistola, e => Call(e, reaction.StartDialogue));
            SetField(g.GetComponent<StoryEventListener>(), "replayOnLoad", false);

            Checkpoint(g, "Checkpoint_Pistola", body + Vector3.up, new Vector3(6f, 2f, 5f), body + new Vector3(-1.5f, 0f, -1f), 0f);
        }

        // ---------- Soldados ----------

        // Taludes do blockout: filas em z -75, -65, -55, -47 (aberturas em x -5, 15, -15, 5)
        // e paredes em x -20, 0 e 20 entre as filas. As patrulhas usam os corredores livres.
        static void BuildSoldiers(Transform g)
        {
            Transform soldiers = Group(g, "Grupo");
            Transform routeA = Group(g, "Rota_Soldado_A");
            Transform routeB = Group(g, "Rota_Soldado_B");
            Transform[] patrolA =
            {
                Point(routeA, "P1_Oeste", new Vector3(-22f, 0f, -67f)),
                Point(routeA, "P2_Leste", new Vector3(22f, 0f, -67f)),
            };
            Transform[] patrolB =
            {
                Point(routeB, "P1_Leste", new Vector3(20f, 0f, -51f)),
                Point(routeB, "P2_Meio", new Vector3(-10f, 0f, -51f)),
                Point(routeB, "P3_Vala", new Vector3(-15f, 0f, -59f)),
            };

            Soldier(soldiers, "Soldado_Trincheira_A", new Vector3(-18f, 0f, -67f), new Vector3(22f, 0f, -67f), patrolA, leader: false);
            Soldier(soldiers, "Soldado_Trincheira_B", new Vector3(16f, 0f, -51f), new Vector3(-10f, 0f, -51f), patrolB, leader: false);
            GameObject leader = Soldier(soldiers, "Lider_Trincheiras", new Vector3(21f, 0f, -50f), new Vector3(0f, 0f, -56f), null, leader: true);

            // O líder carrega o rádio de campanha e um pouco de munição: caem com ele.
            GameObject radio = Prim(PrimitiveType.Cube, g, "Radio_De_Campanha", leader.transform.position, new Vector3(0.2f, 0.28f, 0.1f), "BO_VerdeOliva");
            Pickup(radio, fieldRadio, 1, null);
            radio.SetActive(false);
            GameObject leaderAmmo = Prim(PrimitiveType.Cube, g, "Municao_Lider", leader.transform.position, new Vector3(0.16f, 0.1f, 0.1f), "BO_VerdeOliva");
            Pickup(leaderAmmo, ammo, 4, null);
            leaderAmmo.SetActive(false);
            SetField(leader.AddComponent<DropOnDeath>(), "drops", new[] { radio, leaderAmmo });
            PrefabUtility.RecordPrefabInstancePropertyModifications(leader);

            // Os soldados chegam no helicóptero: aparecem e começam a procurar depois da execução.
            soldiers.gameObject.SetActive(false);
            Listener(g.gameObject, ev.viuExecucao, e =>
            {
                SetActive(e, soldiers.gameObject, true);
                WakeSoldiers(e, soldiers);
            });
        }

        // ---------- Helicóptero passando baixo ----------

        static void BuildHelicopterPass(Transform g)
        {
            // Entra pelo leste, cruza as trincheiras a 12 m de altura e some para oeste.
            Transform path = Group(g, "Rota_Sobrevoo");
            Transform[] points =
            {
                Point(path, "P1", new Vector3(140f, 40f, -20f)),
                Point(path, "P2", new Vector3(45f, 14f, -60f)),
                Point(path, "P3", new Vector3(0f, 12f, -62f)),
                Point(path, "P4", new Vector3(-45f, 14f, -58f)),
                Point(path, "P5", new Vector3(-170f, 45f, -10f)),
            };

            GameObject heli = Helicopter(g, "Helicoptero_Sobrevoo", points[0].position, -110f);
            var move = heli.AddComponent<MoveAlongPath>();
            SetField(move, "points", points);
            SetField(move, "speed", 13f);
            SetField(move, "turnSpeed", 35f);
            SetField(move, "slowDownDistance", 0f);
            heli.AddComponent<HelicopterSpotter>();
            heli.SetActive(false);

            var poster = g.gameObject.AddComponent<NotificationPoster>();
            var passed = g.gameObject.AddComponent<StoryEventRaiser>();
            SetField(passed, "storyEvent", ev.helicopteroPassou);
            UnityEvent arrived = GetField<UnityEvent>(move, "onArrived");
            SetActive(arrived, heli, false);
            Call(arrived, passed.Raise);

            // Vem depois da pistola ou, se a Elena demorar, uns 35 s depois do rádio.
            var pass = g.gameObject.AddComponent<TimedSequence>();
            SetField(pass, "skipIfRaised", ev.helicopteroPassou);
            SetSteps(pass,
                S("espera a pistola ou o tempo", 35f, null, ev.pegouPistola),
                S("helicóptero vem baixo", 3f, e =>
                {
                    SetActive(e, heli, true);
                    Call(e, move.Play);
                    Notify(e, poster, "Helicóptero vindo baixo: esconda-se sob as lonas ou as passarelas");
                }));
            Listener(g.gameObject, ev.ouviuOrdemNoRadio, e => Call(e, pass.Play));
        }

        // ---------- Dra. Weiss ----------

        static void BuildWeiss(Transform g)
        {
            // No disco dourado do blockout, encostada na tenda médica, sentada e escondida.
            var spot = new Vector3(-20f, 0f, -51.3f);
            Prim(PrimitiveType.Capsule, g, "Dra_Weiss", spot + Vector3.up * 0.6f, new Vector3(0.5f, 0.6f, 0.5f), "BO_BrancoFundacao", 180f);
            Prim(PrimitiveType.Cube, g, "Mochila_Medica", spot + new Vector3(0.7f, 0.2f, 0.2f), new Vector3(0.4f, 0.4f, 0.25f), "BO_TurquesaFundacao", 15f);

            GameObject talk = Zone(g, "Falar_DraWeiss", spot + Vector3.up * 0.8f, new Vector3(1.4f, 1.6f, 1.4f));
            DialogueStarterOn(talk, dlg.draWeiss, "Falar com a mulher ferida", interactable: true);

            // Depois da primeira conversa, ela lembra o pedido sempre que a Elena volta.
            GameObject waiting = Zone(g, "Falar_DraWeiss_Depois", spot + Vector3.up * 0.8f, new Vector3(1.4f, 1.6f, 1.4f));
            SetField(DialogueStarterOn(waiting, dlg.draWeissEspera, "Falar com a Dra. Weiss", interactable: true), "onlyOnce", false);
            waiting.SetActive(false);
            Listener(g.gameObject, ev.conheceuDraWeiss, e =>
            {
                SetActive(e, talk, false);
                SetActive(e, waiting, true);
            });
        }

        // ---------- Achados: rolo de filme e curativos ----------

        static void BuildFinds(Transform g)
        {
            // 2º rolo do Tom: no canto nordeste, ao pé do talude, perto do líder.
            GameObject roll = Prim(PrimitiveType.Cylinder, g, "Rolo_De_Filme_2", new Vector3(24f, 0.06f, -53.6f), new Vector3(0.12f, 0.06f, 0.12f), "BO_Metal");
            Pickup(roll, filmRoll, 1, null);

            // Curativos: um num caixote no canto sudoeste, outro ao pé da escada da passarela leste.
            Prim(PrimitiveType.Cube, g, "Caixote_Curativo", new Vector3(-23f, 0.3f, -72.5f), Vector3.one * 0.6f, "BO_Madeira", 10f);
            GameObject bandage1 = Prim(PrimitiveType.Cube, g, "Curativo_1", new Vector3(-23f, 0.66f, -72.5f), new Vector3(0.18f, 0.08f, 0.12f), "BO_BrancoFundacao", 25f);
            Pickup(bandage1, bandage, 1, null);
            GameObject bandage2 = Prim(PrimitiveType.Cube, g, "Curativo_2", new Vector3(13.6f, 0.05f, -62.6f), new Vector3(0.18f, 0.08f, 0.12f), "BO_BrancoFundacao", -30f);
            Pickup(bandage2, bandage, 1, null);
        }
    }
}
