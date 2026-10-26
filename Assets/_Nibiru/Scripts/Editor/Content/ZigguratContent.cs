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
using UnityEngine.AI;
using UnityEngine.Events;

namespace Nibiru.EditorTools.Content
{
    // Dentro do namespace, para as ferramentas do kit valerem antes de nomes como Nibiru.Dialogue.
    using static ContentKit;

    /// <summary>
    /// Povoa a base da Fundação no pé do zigurate (área 4 do Sítio de Ur). Passo 10 da missão
    /// "Sobrevivente": a Elena entra pelo canto sudoeste, atravessa o pátio vigiado por uma dupla
    /// de soldados e pelo atirador de elite no alto do zigurate, lê na tenda de comando o e-mail do
    /// Kast sobre "a filha de Irena" e a foto de 1979, abre pelo painel da tenda o portão do muro
    /// oeste (o atalho para a Aldeia, que se vê abrir dali) e sai pelo nordeste, rumo ao Poço.
    ///
    /// Rotas: por baixo, de contêiner em contêiner; pela avenida, no aberto (combate); ou pela rota
    /// alta (escada no muro, por cima do muro, rampas até o 2º terraço), que chega por trás do
    /// atirador para o golpe de faca pelas costas.
    ///
    /// Também deixa pegáveis os antibióticos (tenda médica) e o 3º rolo de filme do Tom (tenda de
    /// provas), sem lógica nova de missão. Os arquivos de dados só são criados se ainda não
    /// existirem. Os objetos ficam em "Conteudo_Base" e são reconstruídos a cada execução.
    /// Rode depois da Aldeia (e do blockout, se ele mudou).
    /// Menu: Nibiru > Conteúdo > Povoar Base do Zigurate na cena aberta.
    /// </summary>
    static class ZigguratContent
    {
        const string RootName = "Conteudo_Base";
        const string LaserMaterialPath = "Assets/_Nibiru/Art/Materials/Blockout/BO_Laser.mat";

        // Atirador: no ninho de sacos de areia do 2º terraço (y = 14), olhando o pátio (sul).
        static readonly Vector3 SniperSpot = new Vector3(20f, 14f, 40.6f);

        // Dupla de patrulha: uma volta pela avenida e pelo corredor entre os contêineres e a cerca sul.
        static readonly Vector3[] PatrolLoop =
        {
            new Vector3(-6f, 0f, 9.5f), new Vector3(12f, 0f, 9.5f), new Vector3(9f, 0f, -3.5f), new Vector3(-4f, 0f, -3.5f),
        };

        // Líder de guarda na porta da tenda de comando, olhando os contêineres do sul.
        static readonly Vector3 LeaderSpot = new Vector3(20.5f, 0f, 5.5f);
        static readonly Vector3 LeaderLookAt = new Vector3(14f, 0f, -4f);

        // 2º líder: ronda curta no trecho final, a leste do heliponto, entre a tenda de comando e a saída
        // para o Poço. O corredor entre o zigurate (x = 40) e a cerca (x = 60) fica largo o bastante para
        // passar pelo lado oeste quando ele olha para o outro lado.
        static readonly Vector3[] EastLeaderRound = { new Vector3(53f, 0f, 9f), new Vector3(53f, 0f, 19f) };

        // Helicóptero que cruza a avenida (de leste para oeste), vindo de fora da base. Dispara quando a
        // Elena chega ao corredor atrás do último contêiner do sul (x = 14), ainda coberta. A rota passa
        // a 12,5 m do eixo do corredor, fora do raio de 14 m de quem está de pé atrás do contêiner.
        public const float PassHeight = 12f;
        public const float PassLaneZ = 12.5f;
        static readonly Vector3 PassTriggerCenter = new Vector3(14f, 1f, -2.5f);
        static readonly Vector3 PassTriggerSize = new Vector3(6f, 2f, 4f);
        static readonly Vector3[] PassRoute =
        {
            new Vector3(170f, 40f, 30f), new Vector3(75f, PassHeight + 2f, PassLaneZ), new Vector3(30f, PassHeight, PassLaneZ),
            new Vector3(-25f, PassHeight, PassLaneZ), new Vector3(-170f, 45f, 20f),
        };

        // Portão do muro oeste, corrido por dentro (x = -21,7): fechado no vão (z 6 a 12), aberto ao norte.
        static readonly Vector3 GateClosed = new Vector3(-21.7f, 1.45f, 9f);
        static readonly Vector3 GateOpen = new Vector3(-21.7f, 1.45f, 15f);

        // Tenda de comando (x 22,5 a 31,5, z 3,5 a 10,5), com a porta no oeste (x = 22,5).
        static readonly Vector3 TentInsideCenter = new Vector3(27.5f, 1f, 7f);
        static readonly Vector3 TentInsideSize = new Vector3(7.5f, 2f, 6f);
        static readonly Vector3 DeskCenter = new Vector3(30.3f, 0.4f, 7f);
        static readonly Vector3 PanelSpot = new Vector3(23.6f, 1.3f, 10.38f);
        static readonly Vector3 TentCheckpointCenter = new Vector3(24.2f, 1f, 7f);
        static readonly Vector3 TentCheckpointSize = new Vector3(2.6f, 2f, 5.5f);
        static readonly Vector3 TentCheckpointSpawn = new Vector3(25.5f, 0f, 7f);

        // Ponto de retorno logo depois da entrada, longe do alcance do atirador.
        static readonly Vector3 EntranceCheckpointCenter = new Vector3(-17f, 1f, 3.5f);
        static readonly Vector3 EntranceCheckpointSize = new Vector3(6f, 2f, 3f);
        static readonly Vector3 EntranceCheckpointSpawn = new Vector3(-19.5f, 0f, 3f);

        // Saída para o Poço: entre a face leste do zigurate (x = 40) e a cerca leste (x = 60).
        static readonly Vector3 ExitZoneCenter = new Vector3(50f, 1.5f, 34f);
        static readonly Vector3 ExitZoneSize = new Vector3(20f, 3f, 4f);

        // Itens soltos: antibióticos na tenda médica, 3º rolo de filme na tenda de provas.
        static readonly Vector3 MedicalTableCenter = new Vector3(-10f, 0.45f, 19.2f);
        static readonly Vector3 EvidenceTableCenter = new Vector3(32f, 0.45f, 23.7f);

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent entrouNaBase, chegouATenda, leuOEmail, viuAFoto, leuOsDocumentos, abriuOPortao, saiuDaBase, chegouAoConteiner, helicopteroPassou;
        }

        class Dialogues
        {
            public DialogueDefinition chegada, aFoto, oPortao;
        }

        static Events ev;
        static Dialogues dlg;
        static DocumentDefinition email, photo;
        static SniperSettings sniperSettings;

        [MenuItem("Nibiru/Conteúdo/Povoar Base do Zigurate na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar a base do zigurate.");
                return;
            }

            IdPrefix = "base";
            EditorTools.Audio.ProvisionalSounds.Generate(overwrite: false);
            CreateData();

            Transform root = NewRoot(RootName, "Povoar Base do Zigurate");
            GameObject gate = BuildGate(Group(root, "Portao_Aldeia"));
            BuildArrivalDialogue(Group(root, "Chegada"));
            Transform active = Group(root, "Base_Ativa");
            Checkpoint(active, "Checkpoint_Entrada_Base", EntranceCheckpointCenter, EntranceCheckpointSize, EntranceCheckpointSpawn, 45f);
            BuildSoldiers(Group(active, "Soldados"));
            BuildCommandTent(Group(active, "Tenda_De_Comando"), gate);
            BuildLooseItems(Group(active, "Itens"));
            BuildHelicopterPass(Group(active, "Passagem_Helicoptero"));

            // Tudo da base (soldados, gatilhos, pontos de retorno) só existe depois de a Elena entrar
            // pela ponta do muro (fim do passo 9). O portão fechado está sempre lá.
            active.gameObject.SetActive(false);
            Listener(root.gameObject, ev.entrouNaBase, e => SetActive(e, active.gameObject, true));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Base do zigurate povoada.");
        }

        static void CreateData()
        {
            ev = new Events
            {
                entrouNaBase = Event("EntrouNaBaseDaFundacao", "A Elena passou pela patrulha da estrada e entrou na base da Fundação, no pé do zigurate. Fecha o passo 9."),
                chegouATenda = Event("ChegouATendaDeComando", "A Elena atravessou o pátio da base e entrou na tenda de comando (passo 10)."),
                leuOEmail = Event("LeuOEmailDoKast", "A Elena leu o e-mail do Kast sobre \"a filha de Irena\", na mesa da tenda de comando."),
                viuAFoto = Event("ViuAFotoDe1979", "A Elena viu a foto de 1979: a mãe dela, Irena, ao lado de um rapaz (o Kast, mas ela não o reconhece). Abre a pergunta \"O que aconteceu com Irena?\"."),
                leuOsDocumentos = Event("LeuOsDocumentosDoKast", "Depois do e-mail e da foto, a Elena comentou o que viu. Marca o ponto de retorno da tenda e acende o painel do portão."),
                abriuOPortao = Event("AbriuOPortaoDaAldeia", "A Elena abriu pelo painel da tenda de comando o portão do muro oeste: atalho entre a base e a Aldeia."),
                saiuDaBase = Event("SaiuDaBaseRumoAoPoco", "A Elena saiu da base pelo nordeste, rumo à boca do Poço. Fecha o passo 10."),
                chegouAoConteiner = Event("ChegouAoUltimoConteinerDaBase", "A Elena chegou ao corredor atrás do último contêiner do sul da base, ainda coberta. Chama o helicóptero que cruza a avenida."),
                helicopteroPassou = Event("HelicopteroPassouNaBase", "O segundo helicóptero da Fundação terminou de cruzar a avenida da base. Acontece uma vez só."),
            };

            if (!AssetDatabase.IsValidFolder(DataFolder + "/Documents")) AssetDatabase.CreateFolder(DataFolder, "Documents");
            email = Asset<DocumentDefinition>("Documents/Base_EmailDoKast", d =>
            {
                d.raiseOnRead = ev.leuOEmail;
            });

            photo = Asset<DocumentDefinition>("Documents/Base_Foto1979", d =>
            {
                d.raiseOnRead = ev.viuAFoto;
            });

            dlg = new Dialogues
            {
                chegada = Dialogue("Base_Chegada", null,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "e3"),
                    N("e3", "Elena")),

                aFoto = Dialogue("Base_AFoto", ev.leuOsDocumentos,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "e3"),
                    N("e3", "Elena", "e4"),
                    N("e4", "Elena")),

                oPortao = Dialogue("Base_OPortao", null,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena")),
            };

            sniperSettings = Asset<SniperSettings>("AI/AtiradorElite", s =>
            {
                s.patrolSpeed = 0f;
                s.chaseSpeed = 0f;
                s.turnSpeed = 90f;
                s.viewDistance = 45f;
                s.viewAngle = 50f;
                s.timeToSpot = 1f;
                s.crouchedVisibility = 0.5f;
                s.awarenessDecay = 0.25f;
                s.hearingMultiplier = 0.5f;
                s.attackRange = 70f;
                s.preferredDistance = 0f;
                s.fireInterval = 2.5f;
                s.damage = 34f;
                s.accuracy = 0.8f;
                s.shotNoiseRadius = 40f;
                s.magazineSize = 5;
                s.reloadTime = 3f;
                s.loseSightTime = 2f;
                s.searchTime = 6f;
            });

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("s18", ev.chegouATenda),
                    Step("s19", ev.leuOsDocumentos),
                    Step("s20", ev.abriuOPortao),
                    Step("s21", ev.saiuDaBase));

            AssetDatabase.SaveAssets();
        }

        // ---------- Chegada e entrada ----------

        // Fica fora da Base_Ativa: um ouvinte só ao vivo precisa já estar ligado quando o evento acontece.
        static void BuildArrivalDialogue(Transform g)
        {
            // Ao entrar, só ao vivo: a Elena vê o laser e diz para onde vai; depois, a dica da faca.
            var arrival = DialogueTrigger(g, "Dialogo_Chegada", dlg.chegada);
            var poster = arrival.gameObject.AddComponent<NotificationPoster>();
            var sequence = arrival.gameObject.AddComponent<TimedSequence>();
            SetField(sequence, "skipIfRaised", ev.chegouATenda);
            SetSteps(sequence,
                S("a Elena vê a base e o laser do atirador", 0.8f, e => Call(e, arrival.StartDialogue)),
                S("dica do golpe pelas costas", 0.5f, e => Notify(e, poster, "Dica: por trás de um soldado distraído, a faca derruba de uma vez")));
            Listener(arrival.gameObject, ev.entrouNaBase, e => Call(e, sequence.Play));
            SetField(arrival.GetComponent<StoryEventListener>(), "replayOnLoad", false);
        }

        // ---------- Soldados ----------

        static void BuildSoldiers(Transform g)
        {
            Vector3[] loop = PatrolLoop.Select((p, i) => OnNavMesh(p, $"Patrulha_{i + 1}")).ToArray();
            Transform[] points = loop.Select((p, i) => Point(g, $"Patrulha_Ponto_{i + 1}", p)).ToArray();
            Vector3 leaderSpot = OnNavMesh(LeaderSpot, "Lider_Tenda");

            Transform soldiers = Group(g, "Patrulha_E_Guarda");
            Soldier(soldiers, "Soldado_Base_A", loop[0], loop[1], points, leader: false);
            Soldier(soldiers, "Soldado_Base_B", loop[2], loop[3], new[] { points[2], points[3], points[0], points[1] }, leader: false);
            // O líder chama reforços pelo rádio se o combate durar: derrubá-lo primeiro é a tática.
            Soldier(soldiers, "Lider_Tenda_Comando", leaderSpot, LeaderLookAt, null, leader: true);

            // O 2º líder guarda o fim do caminho: ronda curta a leste do heliponto. Código fixo para o salvamento.
            Vector3[] round = EastLeaderRound.Select((p, i) => OnNavMesh(p, $"Ronda_Lider_Leste_{i + 1}")).ToArray();
            Transform[] roundPoints = round.Select((p, i) => Point(g, $"Ronda_Lider_Leste_Ponto_{i + 1}", p)).ToArray();
            GameObject eastLeader = Soldier(soldiers, "Lider_Leste_Heliponto", round[0], round[1], roundPoints, leader: true);
            StableId(eastLeader);

            GameObject sniper = Sniper(g, "Atirador_De_Elite", SniperSpot, SniperSpot + Vector3.back * 10f);

            // A Base_Ativa liga junto com a entrada; aqui só se acorda a IA.
            Listener(g.gameObject, ev.entrouNaBase, e =>
            {
                WakeSoldiers(e, soldiers);
                SetEnabled(e, sniper.GetComponent<SoldierPerception>(), true);
                SetEnabled(e, sniper.GetComponent<SniperAI>(), true);
            });
        }

        // ---------- Helicóptero cruzando a avenida ----------

        // Um segundo helicóptero, vindo de fora da base (o do heliponto, do passo 9, continua pousado).
        // Quem espera coberto vê a ameaça passar; quem corre pela avenida é avistado e chama os soldados
        // (o atirador não, ele reage por conta própria). Uma passagem só.
        static void BuildHelicopterPass(Transform g)
        {
            Transform route = Group(g, "Rota_Passagem");
            Transform[] points = PassRoute.Select((p, i) => Point(route, $"P{i + 1}", p)).ToArray();

            GameObject heli = Helicopter(g, "Helicoptero_Passagem", points[0].position, -100f);
            var move = heli.AddComponent<MoveAlongPath>();
            SetField(move, "points", points);
            SetField(move, "speed", 13f);
            SetField(move, "turnSpeed", 35f);
            SetField(move, "slowDownDistance", 0f);
            var spotter = heli.AddComponent<HelicopterSpotter>();
            SetField(spotter, "radioMessageKey", "ui.radio.helicoptero_base_avistou");
            heli.SetActive(false);

            var passed = g.gameObject.AddComponent<StoryEventRaiser>();
            SetField(passed, "storyEvent", ev.helicopteroPassou);
            UnityEvent arrived = GetField<UnityEvent>(move, "onArrived");
            SetActive(arrived, heli, false);
            Call(arrived, passed.Raise);

            var pass = g.gameObject.AddComponent<TimedSequence>();
            SetField(pass, "skipIfRaised", ev.helicopteroPassou);
            SetSteps(pass, S("o helicóptero entra de longe", 1f, e =>
            {
                SetActive(e, heli, true);
                Call(e, move.Play);
            }));

            GameObject trigger = EventZone(g, "Gatilho_Ultimo_Conteiner", PassTriggerCenter, PassTriggerSize, ev.chegouAoConteiner);
            Listener(trigger, ev.chegouAoConteiner, e => Call(e, pass.Play));
            SetField(trigger.GetComponent<StoryEventListener>(), "replayOnLoad", false);
        }

        /// <summary>
        /// O atirador de elite: o prefab Soldado_Atirador com a IA de soldado trocada pela do
        /// atirador (posto fixo, sem andar), a mira laser, o ponto vermelho e o som.
        /// </summary>
        static GameObject Sniper(Transform parent, string name, Vector3 position, Vector3 lookAt)
        {
            GameObject go = Soldier(parent, name, position, lookAt, null, leader: false, SoldierModel.Atirador);
            var oldAi = go.GetComponent<SoldierAI>();
            var oldSo = new SerializedObject(oldAi);
            var muzzle = oldSo.FindProperty("muzzle").objectReferenceValue as Transform;
            var tracer = oldSo.FindProperty("tracer").objectReferenceValue as LineRenderer;
            Object.DestroyImmediate(oldAi);

            var perception = go.GetComponent<SoldierPerception>();
            SetField(perception, "settings", sniperSettings);

            // Mira laser: linha vermelha do cano até onde bate, com um ponto vermelho aceso.
            Material laserMaterial = LaserMaterial();
            var laserGo = new GameObject("Mira_Laser");
            laserGo.transform.SetParent(go.transform, false);
            var laser = laserGo.AddComponent<LineRenderer>();
            laser.positionCount = 2;
            laser.useWorldSpace = true;
            laser.sharedMaterial = laserMaterial;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            laser.numCapVertices = 2;
            laser.enabled = false;
            GameObject dot = Local(PrimitiveType.Sphere, go.transform, "Ponto_Laser", Vector3.zero, Vector3.one * 0.1f, "BO_Areia");
            dot.GetComponent<Renderer>().sharedMaterial = laserMaterial;
            dot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glow = new GameObject("Brilho").AddComponent<Light>();
            glow.transform.SetParent(dot.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.1f, 0.08f);
            glow.range = 1.5f;
            glow.intensity = 2f;
            glow.shadows = LightShadows.None;
            dot.SetActive(false);

            // Som: o bipe do laser travando e o tiro, meio "na cabeça" para se ouvir de longe.
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.5f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 20f;
            source.maxDistance = 150f;

            var ai = go.AddComponent<SniperAI>();
            SetField(ai, "settings", sniperSettings);
            SetField(ai, "muzzle", muzzle);
            SetField(ai, "tracer", tracer);
            SetField(ai, "laser", laser);
            SetField(ai, "laserDot", dot.transform);
            SetField(ai, "audioSource", source);
            SetField(ai, "warningSound", AssetDatabase.LoadAssetAtPath<AudioClip>(EditorTools.Audio.ProvisionalSounds.LaserWarning));
            SetField(ai, "shotSound", AssetDatabase.LoadAssetAtPath<AudioClip>(EditorTools.Audio.ProvisionalSounds.Gunshot));
            ai.enabled = false;

            // O "?" e o "!" sobre a cabeça passam a ler a IA do atirador.
            foreach (var indicator in go.GetComponentsInChildren<AwarenessIndicator>(true))
            {
                SetField(indicator, "soldier", ai);
                PrefabUtility.RecordPrefabInstancePropertyModifications(indicator);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(perception);
            return go;
        }

        // Vermelho que brilha sozinho (sem sombra e sem luz), criado só se ainda não existir.
        static Material LaserMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LaserMaterialPath);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(1f, 0.08f, 0.05f));
            AssetDatabase.CreateAsset(material, LaserMaterialPath);
            return material;
        }

        // ---------- Portão do muro oeste ----------

        // Portão de ferro branco com faixa turquesa, correndo por dentro do muro. Bloqueia a Elena e
        // (pela malha de navegação recortada) os soldados; aberto, liga a base à Aldeia.
        static GameObject BuildGate(Transform g)
        {
            Transform path = Group(g, "Caminho");
            Transform closed = Point(path, "Fechado", GateClosed);
            Transform open = Point(path, "Aberto", GateOpen);

            var gate = new GameObject("Portao");
            gate.transform.SetParent(g, false);
            gate.transform.position = GateClosed;
            Local(PrimitiveType.Cube, gate.transform, "Folha", Vector3.zero, new Vector3(0.3f, 2.9f, 6f), "BO_BrancoFundacao", ColliderMode.Solid);
            Local(PrimitiveType.Cube, gate.transform, "Faixa", new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.35f, 6.02f), "BO_TurquesaFundacao");
            foreach (float z in new[] { -2f, 0f, 2f })
                Local(PrimitiveType.Cube, gate.transform, "Travessa", new Vector3(0f, 0f, z), new Vector3(0.36f, 2.9f, 0.12f), "BO_Metal");

            var obstacle = gate.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(0.6f, 2.9f, 6f);
            obstacle.carving = true;

            var move = gate.AddComponent<MoveAlongPath>();
            SetField(move, "points", new[] { closed, open });
            SetField(move, "speed", 1.7f);
            SetField(move, "turnSpeed", 0f);
            SetField(move, "slowDownDistance", 0.8f);

            // Ouve-se de longe: da tenda de comando até o muro são uns 50 m.
            Audio(gate, EditorTools.Audio.ProvisionalSounds.GateOpening, loop: false, playOnAwake: false, volume: 1f, minDistance: 15f, maxDistance: 160f);

            // Ao vivo: abre devagar, com o som. Ao voltar de um checkpoint: já aberto, em silêncio.
            Listener(g.gameObject, ev.abriuOPortao, e =>
            {
                Call(e, move.Play);
                Call(e, gate.GetComponent<AudioSource>().Play);
            });
            SetField(g.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
            Listener(g.gameObject, ev.abriuOPortao, e => Call(e, move.SnapToEnd));
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            return gate;
        }

        // ---------- Tenda de comando ----------

        static void BuildCommandTent(Transform g, GameObject gate)
        {
            EventZone(g, "Gatilho_Dentro_Da_Tenda", TentInsideCenter, TentInsideSize, ev.chegouATenda);

            // A mesa do Kast: papéis, a foto presa com clipe e um rádio.
            Prim(PrimitiveType.Cube, g, "Mesa_Kast", DeskCenter, new Vector3(1.2f, 0.8f, 2.4f), "BO_Madeira");
            float top = DeskCenter.y + 0.4f;
            Prim(PrimitiveType.Cube, g, "Papeis", new Vector3(30.2f, top + 0.01f, 6.6f), new Vector3(0.3f, 0.02f, 0.22f), "BO_BrancoFundacao", 8f, ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Foto_1979", new Vector3(30.1f, top + 0.025f, 6.75f), new Vector3(0.11f, 0.01f, 0.15f), "BO_LonaLaranja", -14f, ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Radio_Mesa", new Vector3(30.4f, top + 0.12f, 7.8f), new Vector3(0.3f, 0.24f, 0.2f), "BO_VerdeOliva", collider: ColliderMode.None);

            // Examinar a mesa: o e-mail, depois a foto, depois a Elena comenta (cada um espera o anterior fechar).
            GameObject desk = Zone(g, "Examinar_Mesa", DeskCenter + new Vector3(-0.3f, 0.5f, 0f), new Vector3(1f, 0.6f, 2.4f));
            var emailView = desk.AddComponent<DocumentView>();
            SetField(emailView, "document", email);
            var photoGo = new GameObject("Foto");
            photoGo.transform.SetParent(desk.transform, false);
            var photoView = photoGo.AddComponent<DocumentView>();
            SetField(photoView, "document", photo);
            var comment = DialogueTrigger(g, "Dialogo_AFoto", dlg.aFoto);

            var reading = desk.AddComponent<TimedSequence>();
            SetField(reading, "skipIfRaised", ev.leuOsDocumentos);
            SetSteps(reading,
                S("o e-mail do Kast", 0f, e => Call(e, emailView.Show)),
                S("a foto de 1979 (depois de fechar o e-mail)", 0.3f, e => Call(e, photoView.Show)),
                S("a Elena comenta a foto", 0.4f, e => Call(e, comment.StartDialogue)));
            var interact = desk.AddComponent<Interaction.InteractableEvent>();
            SetField(interact, "prompt", "Examinar a mesa");
            SetField(interact, "singleUse", true);
            Call(GetField<UnityEvent>(interact, "onInteract"), reading.Play);

            // O painel do portão, na parede norte junto da porta. Acende depois da leitura.
            Prim(PrimitiveType.Cube, g, "Painel_Portao", PanelSpot, new Vector3(0.6f, 0.8f, 0.12f), "BO_Metal", collider: ColliderMode.None);
            GameObject button = Prim(PrimitiveType.Cube, g, "Botao_Portao", PanelSpot + new Vector3(0f, 0.1f, -0.08f), new Vector3(0.14f, 0.14f, 0.05f), "BO_TurquesaFundacao", collider: ColliderMode.None);
            button.GetComponent<Renderer>().sharedMaterial = LaserMaterial();
            GameObject panel = Zone(g, "Usar_Painel", PanelSpot + new Vector3(0f, 0f, -0.3f), new Vector3(1f, 1.2f, 0.6f));
            var raiser = panel.AddComponent<StoryEventRaiser>();
            SetField(raiser, "storyEvent", ev.abriuOPortao);
            var press = panel.AddComponent<Interaction.InteractableEvent>();
            SetField(press, "prompt", "Abrir o portão para a aldeia");
            SetField(press, "singleUse", true);
            Call(GetField<UnityEvent>(press, "onInteract"), raiser.Raise);
            panel.SetActive(false);

            // Ponto de retorno dentro da tenda, entre a mesa e a porta, só depois da leitura.
            Transform checkpointGroup = Group(g, "Ponto_De_Retorno");
            Checkpoint(checkpointGroup, "Checkpoint_Tenda_Comando", TentCheckpointCenter, TentCheckpointSize, TentCheckpointSpawn, 270f);
            checkpointGroup.gameObject.SetActive(false);

            // Lidos o e-mail e a foto (ao vivo e ao voltar de um checkpoint): a mesa já foi vista, o
            // painel acende e o ponto de retorno passa a valer.
            Listener(g.gameObject, ev.leuOsDocumentos, e =>
            {
                SetActive(e, desk, false);
                SetActive(e, panel, true);
                SetActive(e, checkpointGroup.gameObject, true);
            });

            // Portão aberto: o painel apaga, a Elena comenta (só ao vivo) e a saída nordeste passa a valer.
            GameObject exitZone = EventZone(g, "Gatilho_Saida_Nordeste", ExitZoneCenter, ExitZoneSize, ev.saiuDaBase);
            exitZone.SetActive(false);
            Listener(g.gameObject, ev.abriuOPortao, e =>
            {
                SetActive(e, panel, false);
                SetActive(e, exitZone, true);
            });
            var gateLine = DialogueTrigger(g, "Dialogo_OPortao", dlg.oPortao);
            var afterGate = gateLine.gameObject.AddComponent<TimedSequence>();
            SetSteps(afterGate, S("a Elena ouve o portão abrir ao longe", 1.6f, e => Call(e, gateLine.StartDialogue)));
            Listener(gateLine.gameObject, ev.abriuOPortao, e => Call(e, afterGate.Play));
            SetField(gateLine.GetComponent<StoryEventListener>(), "replayOnLoad", false);
        }

        // ---------- Itens soltos (sem lógica nova de missão) ----------

        static void BuildLooseItems(Transform g)
        {
            var antibiotics = Load<ItemDefinition>("Items/Antibioticos");
            var filmRoll = Load<ItemDefinition>("Items/RoloDeFilmeDoTom");

            Prim(PrimitiveType.Cube, g, "Mesa_Tenda_Medica", MedicalTableCenter, new Vector3(1.6f, 0.9f, 0.7f), "BO_BrancoFundacao");
            GameObject box = Prim(PrimitiveType.Cube, g, "Antibioticos", MedicalTableCenter + new Vector3(0.2f, 0.51f, 0f), new Vector3(0.2f, 0.12f, 0.14f), "BO_TurquesaFundacao", 20f);
            Pickup(box, antibiotics, 1, null);

            Prim(PrimitiveType.Cube, g, "Mesa_Tenda_Provas", EvidenceTableCenter, new Vector3(1.6f, 0.9f, 0.7f), "BO_Madeira");
            GameObject roll = Prim(PrimitiveType.Cylinder, g, "Rolo_De_Filme_3", EvidenceTableCenter + new Vector3(-0.3f, 0.51f, 0f), new Vector3(0.12f, 0.06f, 0.12f), "BO_Metal");
            Pickup(roll, filmRoll, 1, null);
        }

        // ---------- Ajudas ----------

        // Leva o ponto para a malha de navegação (avisa no Console se estiver fora dela).
        static Vector3 OnNavMesh(Vector3 wanted, string label)
        {
            if (NavMesh.SamplePosition(wanted, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return hit.position;
            Debug.LogWarning($"Base: o ponto {label} {wanted} está fora da malha de navegação. Mude a posição no script.");
            return wanted;
        }
    }
}
