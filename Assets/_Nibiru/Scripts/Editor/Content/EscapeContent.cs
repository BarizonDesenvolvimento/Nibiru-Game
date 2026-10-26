using System.Linq;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Documents;
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
    using static ContentKit;

    /// <summary>
    /// Passo 15 da missão "Sobrevivente": a fuga na caminhonete do Samir, até o fim da fatia.
    /// Menu: Nibiru > Conteúdo > Povoar Fuga na cena aberta. Monta, em "Conteudo_Fuga": a caminhonete
    /// (no lugar da caixa do blockout), o ponto de embarque, o nicho dos Guardiões no túnel (só para quem
    /// devolve a tabuleta), a estrada provisória longe do mapa, a viagem (TruckRide), a entrada do diário
    /// e a tela de fim da demo. Cria também a cena MenuPrincipal (Nibiru > Conteúdo > Criar menu principal).
    /// Rode antes os menus da Boca do Poço e do Wadi (usa os eventos e a missão deles).
    /// </summary>
    static class EscapeContent
    {
        const string RootName = "Conteudo_Fuga";
        const string MenuScenePath = "Assets/_Nibiru/Scenes/MenuPrincipal.unity";

        // A caminhonete do Samir fica onde estava a caixa do blockout, virada para o norte.
        static readonly Vector3 TruckSpot = new Vector3(60f, 0f, 176f);

        // Embarque pela traseira: a Elena chega do sul.
        static readonly Vector3 BoardZoneCenter = new Vector3(60f, 1f, 173.1f);
        static readonly Vector3 BoardZoneSize = new Vector3(3.2f, 2f, 1.6f);
        static readonly Vector3 BoardSaveCenter = new Vector3(60f, 1.5f, 172.2f);
        static readonly Vector3 BoardSaveSize = new Vector3(6f, 3f, 1.4f);
        static readonly Vector3 BoardSaveSpawn = new Vector3(60f, 0f, 171.6f);

        // O nicho no túnel, na parede oeste, depois da lamparina 2. Quem enterra fica a leste dele.
        const float NicheZ = 102.6f;
        static readonly Vector3 NicheStand = new Vector3(31.8f, 0f, NicheZ);
        static readonly Vector3 NicheSamir = new Vector3(32.9f, 0f, NicheZ + 0.9f);

        // A estrada provisória: longe de tudo, indo para o norte (z alto), sem colisão.
        static readonly Vector3[] RoadRoute =
        {
            new Vector3(400f, 0f, 300f), new Vector3(400f, 0f, 700f), new Vector3(440f, 0f, 1000f), new Vector3(440f, 0f, 1550f),
        };

        const float RideSeconds = 60f;

        class Events
        {
            public StoryEvent resolved, buried, boarded, arrived, readDiary, end;
        }

        static Events ev;
        static DialogueDefinition nicheTalk, roadEntregou, roadDevolveu, roadFicou;
        static DocumentDefinition diary;

        [MenuItem("Nibiru/Conteúdo/Povoar Fuga na cena aberta")]
        static void Populate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar a Fuga.");
                return;
            }

            IdPrefix = "fuga";
            EditorTools.Audio.ProvisionalSounds.Generate(overwrite: false);
            if (!CreateData()) return;

            StoryEvent entregou = Load<StoryEvent>("Events/EntregouATabuleta");
            StoryEvent devolveu = Load<StoryEvent>("Events/DevolveuATabuleta");
            StoryEvent ficou = Load<StoryEvent>("Events/FicouComATabuleta");

            // A caixa do blockout dá lugar à caminhonete de verdade (a que anda).
            GameObject box = GameObject.Find("Caminhonete_Samir");
            if (box != null) box.SetActive(false);

            Transform root = NewRoot(RootName, "Povoar Fuga");

            TruckRide ride = BuildTruck(Group(root, "Caminhonete"), out GameObject samirAtTruck);
            BuildRoad(Group(root, "Estrada_Da_Fuga"), ride);

            GameObject boardZone = BuildBoarding(Group(root, "Embarque"), ride, out GameObject boardSaveZone);

            // Lógica sempre ligada (os grupos acima ficam apagados até a hora).
            var raiser = root.gameObject.AddComponent<StoryEventRaiser>();
            SetField(raiser, "storyEvent", ev.resolved);
            BuildNiche(Group(root, "Nicho_Dos_Guardioes"), root, raiser, devolveu, samirAtTruck);
            foreach (StoryEvent e in new[] { entregou, ficou })
            {
                Listener(root.gameObject, e, r => Call(r, raiser.Raise));
                SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
                Listener(root.gameObject, e, r => SetActive(r, samirAtTruck, true));
            }

            Listener(root.gameObject, ev.resolved, r =>
            {
                SetActive(r, boardZone, true);
                SetActive(r, boardSaveZone, true);
            });
            Listener(root.gameObject, ev.boarded, r => SetActive(r, boardZone, false));
            SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            var screen = root.gameObject.AddComponent<DemoEndScreen>();
            Listener(root.gameObject, ev.end, r => Call(r, screen.Show));
            SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Fuga povoada.");
        }

        // ---------- Dados ----------

        static bool CreateData()
        {
            StoryEvent chose = Load<StoryEvent>("Events/EscolheuONoWadi");
            if (chose == null)
            {
                Debug.LogError("Fuga: falta o evento EscolheuONoWadi. Rode o menu do Wadi antes.");
                return false;
            }

            ev = new Events
            {
                resolved = Event("TabuletaResolvida", "O destino da tabuleta está decidido e a Elena pode ir à caminhonete: de imediato para quem entregou ou ficou com ela; só depois de enterrá-la no nicho do túnel para quem a devolveu (passo 15)."),
                buried = Event("EnterrouATabuleta", "A Elena enterrou a tabuleta no nicho dos Guardiões do túnel, com o Samir (escolha 'Devolver ao Poço'). A tabuleta sai do inventário; o poder fica."),
                boarded = Event("EmbarcouNaCaminhonete", "A Elena entrou na caminhonete do Samir: começa a viagem (passo 15)."),
                arrived = Event("FimDaViagem", "A viagem na caminhonete terminou, depois da última fala do Samir (passo 15)."),
                readDiary = Event("LeuODiario", "A Elena leu a primeira entrada do diário, ao fim da fuga."),
                end = Event("FimDaFatia", "Fim da fatia vertical: abre a tela provisória de fim da demo, que volta ao menu principal."),
            };

            nicheTalk = Dialogue("Fuga_Nicho", ev.buried,
                N("n1", "Samir", "Deixa aqui. Os Guardiões levam ao Poço.", "n2"),
                N("n2", "Elena", "(Ela põe a tabuleta no nicho, entre a lamparina e o barco de junco. A pedra esfria na mão dela. O que ficou dentro dela, não.)", "n3"),
                N("n3", "Samir", "Pronto. Eu vou na frente, para ligar o motor. Não demore.", null));

            roadEntregou = Dialogue("Fuga_Estrada_Entregou", null,
                N("e1", "Elena", "(Samir dirige sem olhar para ela. As mãos apertam o volante até os nós dos dedos clarearem. Fora, a tempestade é uma parede laranja.)", "e2"),
                N("e2", "Samir", "Uma chave, doutora. Você entregou uma chave a quem a procura há décadas.", "e3"),
                N("e3", "Elena", "Eles iam machucar a Aldeia. Eu não tinha mais nada para trocar.", "e4"),
                N("e4", "Samir", "Talvez eu tivesse feito o mesmo. Isso não ajuda ninguém agora.", "e5"),
                N("e5", "Samir", "Os Guardiões fizeram um juramento sobre este caminho. Quem guarda, guarda para os que vêm depois. Eu fiz esse juramento.", "e6"),
                N("e6", "Elena", "Para os que vêm depois? Quem?", "e7"),
                N("e7", "Samir", "Olhe para a frente. Tell Amar fica a algumas horas, e a tempestade só piora.", "e8"),
                N("e8", "Elena", "(Ele não diz mais nada. A estrada some na areia, e o motor é o único som.)"));

            roadDevolveu = Dialogue("Fuga_Estrada_Devolveu", null,
                N("d1", "Elena", "(Samir dirige com uma mão só, a outra no joelho. Pela primeira vez desde a Aldeia, os ombros dele estão soltos.)", "d2"),
                N("d2", "Samir", "Obrigado, doutora. Deixar aquilo no nicho não foi pouca coisa. Os Guardiões vão saber.", "d3"),
                N("d3", "Elena", "O Kast disse que era lamentável.", "d4"),
                N("d4", "Samir", "Ele diz isso de tudo que não consegue comprar.", "d5"),
                N("d5", "Samir", "Há um juramento, doutora, mais velho que eu e que o meu pai. Quem guarda o Poço, guarda para os que vêm depois. Eu o fiz ainda menino.", "d6"),
                N("d6", "Elena", "E o que mais esse juramento diz?", "d7"),
                N("d7", "Samir", "O resto fica para Tell Amar. Lá há quem explique melhor do que eu.", "d8"),
                N("d8", "Elena", "(Ele sorri para a estrada. Ela repara que ele não respondeu.)"));

            roadFicou = Dialogue("Fuga_Estrada_Ficou", null,
                N("f1", "Elena", "(Samir dirige em silêncio. Pelo retrovisor, ela o vê olhar para a bolsa dela e desviar o rosto.)", "f2"),
                N("f2", "Samir", "Você ficou com ela.", "f3"),
                N("f3", "Elena", "Eu li aquilo. Eu não ia deixar para ninguém.", "f4"),
                N("f4", "Samir", "Os Guardiões fizeram um juramento sobre este caminho. Quem guarda, guarda para os que vêm depois. Eu o fiz.", "f5"),
                N("f5", "Elena", "E você acha que eu sou um perigo para o que você guarda.", "f6"),
                N("f6", "Samir", "Acho que a tabuleta está quente demais para quem não sabe o que ela quer.", "f7"),
                N("f7", "Elena", "(A tabuleta pulsa na bolsa, uma vez, devagar. Nenhum dos dois comenta. A tempestade cobre a estrada atrás deles.)"));

            diary = Asset<DocumentDefinition>("Documents/Diario_Entrada_1", d =>
            {
                d.displayName = "Diário de Elena Varga: entrada 1";
                d.text = "Iraque, sul do deserto. Tell Amar fica a algumas horas.\n\nEm três dias vi minha equipe morta, uma voz me chamar pelo nome que minha mãe me deu e uma tabuleta acender na minha mão. Um homem que diz ser meu amigo me levou por um túnel que ele jura que ninguém mais conhece.\n\nNão sei em quem confiar. Sei que a Fundação sabia que eu estaria aqui, e que minha mãe esteve neste lugar muito antes de mim. Preciso descobrir por quê.";
                d.footer = "Anotado na caçamba de uma caminhonete, no meio de uma tempestade de areia.";
                d.raiseOnRead = ev.readDiary;
            });

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest != null)
                AppendSteps(mainQuest,
                    Step("Leve a tabuleta ao nicho dos Guardiões, no túnel", ev.resolved),
                    Step("Entre na caminhonete do Samir", ev.boarded),
                    Step("Fuja para Tell Amar", ev.arrived));

            AssetDatabase.SaveAssets();
            return true;
        }

        // ---------- A caminhonete ----------

        // Onde o Samir senta na cabine (local da caminhonete): os pés no chão da cabine, o quadril sobre o banco.
        const float DriverX = -0.5f, DriverY = 0.9f, DriverZ = 0.45f;

        static TruckRide BuildTruck(Transform g, out GameObject samir)
        {
            // g já é filho do grupo da fuga; a própria caminhonete é o objeto que anda.
            var truck = new GameObject("Caminhonete_Do_Samir");
            truck.transform.SetParent(g, false);
            truck.transform.SetPositionAndRotation(TruckSpot, Quaternion.identity);
            Transform truckRoot = truck.transform;

            // Chassi e caçamba (a Elena vai em pé, atrás da cabine).
            Local(PrimitiveType.Cube, truckRoot, "Chassi", new Vector3(0f, 0.75f, 0f), new Vector3(2.2f, 0.3f, 5f), "BO_Metal", ColliderMode.Solid);
            Local(PrimitiveType.Cube, truckRoot, "Capo", new Vector3(0f, 1.2f, 1.9f), new Vector3(2f, 0.7f, 1.3f), "BO_VerdeOliva", ColliderMode.Solid);
            Local(PrimitiveType.Cube, truckRoot, "Painel", new Vector3(0f, 1.45f, 1.15f), new Vector3(2f, 0.4f, 0.1f), "BO_Metal");
            // Fundo da cabine com uma janela traseira (de 1,9 m a 2,75 m), para o Samir aparecer ao volante.
            Local(PrimitiveType.Cube, truckRoot, "Cabine_Fundo", new Vector3(0f, 1.45f, -0.1f), new Vector3(2f, 0.9f, 0.1f), "BO_Metal", ColliderMode.Solid);
            Local(PrimitiveType.Cube, truckRoot, "Cabine_Fundo_Topo", new Vector3(0f, 2.8f, -0.1f), new Vector3(2f, 0.1f, 0.1f), "BO_Metal");
            Local(PrimitiveType.Cube, truckRoot, "Teto", new Vector3(0f, 2.95f, 0.5f), new Vector3(2.3f, 0.1f, 1.5f), "BO_Metal");
            foreach (float x in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -0.05f, 1.15f })
                    Local(PrimitiveType.Cube, truckRoot, "Coluna", new Vector3(x, 1.95f, z), new Vector3(0.1f, 2f, 0.1f), "BO_Metal");
                Local(PrimitiveType.Cube, truckRoot, "Grade_Lateral", new Vector3(x * 1.05f, 1.075f, -1.4f), new Vector3(0.1f, 0.35f, 2.5f), "BO_Madeira", ColliderMode.Solid);
                foreach (float z in new[] { -1.3f, 1.3f })
                {
                    GameObject wheel = Local(PrimitiveType.Cylinder, truckRoot, "Roda", new Vector3(x * 1.1f, 0.45f, z), new Vector3(0.9f, 0.12f, 0.9f), "BO_TijoloBarroEscuro");
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
            Headlight(truckRoot, -0.7f);
            Headlight(truckRoot, 0.7f);

            Transform seatPoint = new GameObject("Lugar_Da_Elena").transform;
            seatPoint.SetParent(truckRoot, false);
            seatPoint.localPosition = new Vector3(0.35f, 0.9f, -1.35f);
            Transform seat = seatPoint;

            // O Samir na cabine, sentado ao volante (animação Dirigindo): só aparece depois da escolha.
            // Banco e volante provisórios, em formas simples, na altura do quadril e das mãos do clipe.
            Local(PrimitiveType.Cube, truckRoot, "Banco", new Vector3(DriverX, 1.1f, DriverZ - 0.15f), new Vector3(0.9f, 0.4f, 0.6f), "BO_Madeira");
            GameObject wheelRim = Local(PrimitiveType.Cylinder, truckRoot, "Volante", new Vector3(DriverX + 0.02f, 1.74f, DriverZ + 0.2f), new Vector3(0.42f, 0.02f, 0.42f), "BO_Metal");
            wheelRim.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            samir = new GameObject("Samir_Na_Caminhonete");
            samir.transform.SetParent(truckRoot, false);
            samir.transform.localPosition = new Vector3(DriverX, DriverY, DriverZ);
            samir.transform.localRotation = Quaternion.identity;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SamirSetup.PrefabPath) != null)
            {
                var visual = SamirSetup.AddVisual(samir.transform, 0f);
                var npc = visual.GetComponent<NpcAnimator>();
                if (npc != null) SetField(npc, "driving", true);
            }
            else Debug.LogWarning("Fuga: prefab do Samir não encontrado. Rode Nibiru > Personagens > Montar Samir animado.");
            samir.SetActive(false);

            var ride = truck.AddComponent<TruckRide>();
            SetField(ride, "elenaSeat", seat);
            SetField(ride, "minSeconds", RideSeconds);
            SetField(ride, "raiseOnBoard", ev.boarded);
            SetField(ride, "raiseOnArrive", ev.arrived);
            SetField(ride, "raiseOnEnd", ev.end);
            SetField(ride, "variants", new[]
            {
                new TruckRide.Variant { when = Load<StoryEvent>("Events/EntregouATabuleta"), dialogue = roadEntregou },
                new TruckRide.Variant { when = Load<StoryEvent>("Events/DevolveuATabuleta"), dialogue = roadDevolveu },
                new TruckRide.Variant { when = Load<StoryEvent>("Events/FicouComATabuleta"), dialogue = roadFicou },
            });

            var view = truck.AddComponent<DocumentView>();
            SetField(view, "document", diary);
            SetField(ride, "diary", view);
            return ride;
        }

        static void Headlight(Transform truck, float x)
        {
            var go = new GameObject("Farol");
            go.transform.SetParent(truck, false);
            go.transform.localPosition = new Vector3(x, 1.2f, 2.6f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 60f;
            light.range = 26f;
            light.intensity = 5f;
            light.color = new Color(1f, 0.93f, 0.75f);
            light.shadows = LightShadows.None;
        }

        // ---------- A estrada provisória ----------

        static void BuildRoad(Transform g, TruckRide ride)
        {
            Vector3 center = new Vector3(420f, -0.1f, 925f);
            Prim(PrimitiveType.Cube, g, "Chao", center, new Vector3(110f, 0.2f, 1400f), "BO_Areia", collider: ColliderMode.None);

            // Pedras dos dois lados, de tempos em tempos, para dar a ideia de velocidade.
            var random = new System.Random(15);
            for (int i = 0; i < 70; i++)
            {
                float z = 280f + i * 19f;
                float t = Mathf.InverseLerp(300f, 1550f, z);
                float lane = z < 700f ? 400f : Mathf.Lerp(400f, 440f, Mathf.Clamp01((z - 700f) / 300f));
                float side = i % 2 == 0 ? -1f : 1f;
                float offset = 9f + (float)random.NextDouble() * 18f;
                float size = 1.2f + (float)random.NextDouble() * 2.2f;
                Prim(PrimitiveType.Cube, g, $"Pedra_{i + 1}", new Vector3(lane + side * offset, size * 0.4f, z), new Vector3(size, size * 0.8f, size * 1.3f), "BO_Rocha", yaw: i * 37f, collider: ColliderMode.None);
                if (t < 0f) break;
            }

            Transform points = Group(g, "Trilho");
            var route = RoadRoute.Select((p, i) => Point(points, $"Ponto_{i + 1}", p)).ToArray();
            SetField(ride, "route", route);
            SetField(ride, "roadGroup", g.gameObject);
            g.gameObject.SetActive(false);
        }

        // ---------- O embarque ----------

        static GameObject BuildBoarding(Transform g, TruckRide ride, out GameObject saveZone)
        {
            // Ponto de retorno e salvamento automático perto da traseira (liga com a tabuleta resolvida).
            Checkpoint(g, "Autosave_Embarque", BoardSaveCenter, BoardSaveSize, BoardSaveSpawn, 0f);
            saveZone = g.Find("Autosave_Embarque").gameObject;
            var save = saveZone.GetComponent<Save.Checkpoint>();
            saveZone.SetActive(false);

            GameObject zone = Zone(g, "Entrar_Na_Caminhonete", BoardZoneCenter, BoardZoneSize);
            var interact = zone.AddComponent<Interaction.InteractableEvent>();
            SetField(interact, "prompt", "Entrar na caminhonete");
            UnityEvent onInteract = GetField<UnityEvent>(interact, "onInteract");
            Call(onInteract, save.ActivateNow);
            Call(onInteract, ride.Begin);
            zone.SetActive(false);
            return zone;
        }

        // ---------- O nicho dos Guardiões, no túnel ----------

        static void BuildNiche(Transform g, Transform root, StoryEventRaiser raiser, StoryEvent devolveu, GameObject samirAtTruck)
        {
            float floor = FloorAt(new Vector3(NicheStand.x, -9f, NicheZ));

            // O nicho: pedra baixa na parede oeste, lamparina de barro (luz quente), o barco de junco sobre ondas em ouro.
            Prim(PrimitiveType.Cube, g, "Nicho_Pedra", new Vector3(30.55f, floor + 0.5f, NicheZ), new Vector3(1f, 1f, 1.4f), "BO_Rocha");
            Prim(PrimitiveType.Cube, g, "Nicho_Vao", new Vector3(30.2f, floor + 0.6f, NicheZ), new Vector3(0.3f, 0.5f, 0.8f), "BO_TijoloBarroEscuro", collider: ColliderMode.None);
            Prim(PrimitiveType.Cylinder, g, "Lamparina_Barro", new Vector3(30.95f, floor + 1.04f, NicheZ - 0.45f), new Vector3(0.22f, 0.06f, 0.22f), "BO_TijoloBarro", collider: ColliderMode.None);
            Prim(PrimitiveType.Sphere, g, "Lamparina_Chama", new Vector3(30.95f, floor + 1.2f, NicheZ - 0.45f), new Vector3(0.08f, 0.16f, 0.08f), "BO_Chama", collider: ColliderMode.None);
            var lightGo = new GameObject("Luz_Da_Lamparina");
            lightGo.transform.SetParent(g, false);
            lightGo.transform.position = new Vector3(31.1f, floor + 1.5f, NicheZ - 0.45f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.38f);
            light.intensity = 1.6f;
            light.range = 6f;
            light.shadows = LightShadows.None;

            float markY = floor + 1.75f;
            Prim(PrimitiveType.Cube, g, "Barco_Casco", new Vector3(30.1f, markY, NicheZ), new Vector3(0.08f, 0.18f, 0.9f), "BO_Ouro", collider: ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Barco_Vela", new Vector3(30.1f, markY + 0.3f, NicheZ), new Vector3(0.08f, 0.45f, 0.06f), "BO_Ouro", collider: ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Ondas", new Vector3(30.1f, markY - 0.2f, NicheZ), new Vector3(0.08f, 0.05f, 1.2f), "BO_Ouro", collider: ColliderMode.None);

            // A tabuleta enterrada: só depois de a Elena deixá-la.
            GameObject slab = Prim(PrimitiveType.Cube, g, "Tabuleta_Enterrada", new Vector3(30.95f, floor + 1.02f, NicheZ + 0.2f), new Vector3(0.34f, 0.06f, 0.24f), "BO_LapisLazuli", yaw: 12f, collider: ColliderMode.None);
            slab.SetActive(false);

            // O Samir espera ao lado do nicho, de frente para ele.
            var samir = new GameObject("Samir_No_Nicho");
            samir.transform.SetParent(g, false);
            samir.transform.SetPositionAndRotation(new Vector3(NicheSamir.x, floor, NicheSamir.z), Quaternion.Euler(0f, 250f, 0f));
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SamirSetup.PrefabPath) != null) SamirSetup.AddVisual(samir.transform, 0f);

            // A interação: de frente para o nicho, "Enterrar a tabuleta". Fala, e a tabuleta sai do inventário.
            GameObject zone = Zone(g, "Enterrar_A_Tabuleta", new Vector3(NicheStand.x, floor + 1f, NicheZ), new Vector3(1.6f, 2f, 2.2f));
            DialogueStarterOn(zone, nicheTalk, "Enterrar a tabuleta", interactable: true);

            // Ponto de retorno depois de enterrar, ao lado do nicho (sem área: só é marcado pela sequência).
            Checkpoint(g, "Autosave_Depois_Do_Nicho", new Vector3(NicheStand.x, floor + 1f, NicheZ), new Vector3(1f, 2f, 1f),
                new Vector3(NicheStand.x, floor, NicheZ), 270f);
            Transform saveZone = g.Find("Autosave_Depois_Do_Nicho");
            saveZone.GetComponent<BoxCollider>().enabled = false;
            var save = saveZone.GetComponent<Save.Checkpoint>();

            var remover = g.gameObject.AddComponent<ItemRemover>();
            SetField(remover, "item", Load<ItemDefinition>("Items/TabuletaDoJunco"));
            var sequence = root.gameObject.AddComponent<TimedSequence>();
            SetSteps(sequence,
                S("a tabuleta sai do inventário e o destino fica decidido", 0.3f, e =>
                {
                    Call(e, remover.Remove);
                    Call(e, raiser.Raise);
                }),
                S("ponto de retorno ao lado do nicho", 0.6f, e => Call(e, save.ActivateNow)));

            g.gameObject.SetActive(false);

            // Só para quem devolve: o nicho existe no túnel (também ao voltar de um ponto de retorno).
            Listener(root.gameObject, devolveu, e => SetActive(e, g.gameObject, true));
            // Depois de enterrar: a Elena não repete a fala, o Samir já foi, a tabuleta está no nicho e o
            // Samir da caminhonete aparece. Ao vivo, a sequência acima ainda faz a tabuleta sair do inventário.
            Listener(root.gameObject, ev.buried, e =>
            {
                SetActive(e, zone, false);
                SetActive(e, samir, false);
                SetActive(e, slab, true);
                SetActive(e, samirAtTruck, true);
            });
            Listener(root.gameObject, ev.buried, e => Call(e, sequence.Play));
            SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        // Altura do chão sob um ponto (o túnel é do blockout); sem colisão achada, usa a altura conhecida do piso.
        static float FloorAt(Vector3 above)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(above, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : -14f;
        }

        // ---------- Menu principal provisório ----------

        [MenuItem("Nibiru/Conteúdo/Criar menu principal provisório")]
        static void CreateMainMenu()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de criar o menu principal.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var cameraGo = new GameObject("Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.045f, 0.07f);
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();
            new GameObject("Menu").AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            EditorSceneManager.CloseScene(scene, true);

            // Primeira da lista do Build (um jogo exportado começa por ela); as outras seguem.
            var list = EditorBuildSettings.scenes.Where(s => s.path != MenuScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            string region = "Assets/_Nibiru/Scenes/Regiao1_SitioDeUr.unity";
            if (!list.Any(s => s.path == region)) list.Add(new EditorBuildSettingsScene(region, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("Menu principal criado: Assets/_Nibiru/Scenes/MenuPrincipal.unity (primeira cena do Build).");
        }
    }
}
