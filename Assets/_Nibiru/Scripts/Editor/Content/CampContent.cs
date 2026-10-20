using System;
using System.Reflection;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.EditorTools.Audio;
using Nibiru.Interaction;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Save;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Nibiru.EditorTools.Content
{
    /// <summary>
    /// Povoa o Acampamento (área 1 do Sítio de Ur) com os passos 1 a 4 da missão "Sobrevivente":
    /// a Elena acorda na barraca, pega a faca, encontra os corpos e a bolsa do Tom, atende o Kast
    /// pelo rádio e vê (ou ouve) o "resgate" executar um trabalhador. Depois os soldados sobem
    /// procurando por ela.
    ///
    /// Os arquivos de dados (eventos, itens, diálogos, missões) só são criados se ainda não
    /// existirem: textos mudados no Inspector não são apagados. Os objetos da cena ficam todos em
    /// "Conteudo_Acampamento" e são reconstruídos a cada execução, como no blockout.
    /// Menu: Nibiru > Conteúdo > Povoar Acampamento na cena aberta.
    /// </summary>
    static class CampContent
    {
        const string RootName = "Conteudo_Acampamento";
        const string DataFolder = "Assets/_Nibiru/Data";
        const string MaterialFolder = "Assets/_Nibiru/Art/Materials/Blockout";
        const string SoldierPrefab = "Assets/_Nibiru/Prefabs/AI/Soldado.prefab";

        // Altura do topo da elevação do acampamento (igual ao blockout).
        const float Top = 3f;

        static readonly Vector3 HelipadPoint = new Vector3(30f, 0.1f, -82f);

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent saiuDaBarraca, examinouOsCorpos, achouBolsaDoTom, falouComKast, kastConfiou,
                kastDesconfiou, chegouAoMirante, viuExecucao, chegouAsTrincheiras, revelouFilmesDoTom;
        }

        class Dialogues
        {
            public DialogueDefinition sussurro, corpos, bolsaDoTom, radioKast, helicoptero, execucaoVista, execucaoOuvida;
        }

        static Events ev;
        static Dialogues dlg;
        static ItemDefinition knife, filmRoll, tablet;
        static QuestDefinition mainQuest, tomQuest;

        [MenuItem("Nibiru/Conteúdo/Povoar Acampamento na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar o Acampamento.");
                return;
            }

            ProvisionalSounds.Generate(overwrite: false);
            CreateData();

            GameObject old = GameObject.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Povoar Acampamento");

            GameObject elena = SetupElena();
            BuildSystems(root, elena);
            BuildTent(Group(root, "Barraca_Elena"));
            BuildCircle(Group(root, "Circulo_Corpos"));
            BuildRadio(Group(root, "Radio"));
            GameObject limits = BuildLimits(Group(root, "Limites_Acampamento"));
            BuildRescue(Group(root, "Resgate"), limits);
            BuildExits(Group(root, "Saidas"));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Acampamento povoado.");
        }

        static void CreateData()
        {
            ev = new Events
            {
                saiuDaBarraca = Event("SaiuDaBarraca", "A Elena saiu da barraca onde acordou (Acampamento)."),
                examinouOsCorpos = Event("ExaminouOsCorpos", "A Elena examinou os corpos da equipe em círculo (Acampamento)."),
                achouBolsaDoTom = Event("AchouBolsaDoTom", "A Elena achou a bolsa vazia da câmera do Tom. Começa \"O que o Tom viu\"."),
                falouComKast = Event("FalouComKast", "Fim da conversa com o Kast pelo rádio do acampamento. O helicóptero vem em seguida."),
                kastConfiou = Event("KastConfiou", "No rádio, a Elena respondeu \"Estou esperando\". Muda uma fala do Kast mais tarde."),
                kastDesconfiou = Event("KastDesconfiou", "No rádio, a Elena perguntou como sabiam que ela estava viva. Muda uma fala do Kast mais tarde."),
                chegouAoMirante = Event("ChegouAoMirante", "A Elena chegou à beira da elevação, de onde se vê o heliponto."),
                viuExecucao = Event("ViuExecucao", "Os soldados executaram o trabalhador ferido (vista do mirante ou ouvida de longe). Os soldados começam a procurar a Elena."),
                chegouAsTrincheiras = Event("ChegouAsTrincheiras", "A Elena desceu da elevação e chegou às trincheiras."),
                revelouFilmesDoTom = Event("RevelouFilmesDoTom", "Os filmes do Tom foram revelados na Aldeia (fim de \"O que o Tom viu\")."),
            };

            knife = Asset<ItemDefinition>("Items/FacaDeCampo", item =>
            {
                item.displayName = "Faca de campo";
                item.description = "Faca de campo da expedição. Silenciosa.";
                item.category = ItemCategory.Chave;
                item.pickupHint = "Ataque com a faca: clique esquerdo ou F (RB no controle)";
            });
            filmRoll = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DataFolder}/Items/RoloDeFilmeDoTom.asset");
            tablet = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DataFolder}/Items/TabuletaDoJunco.asset");

            dlg = new Dialogues
            {
                sussurro = Dialogue("Acampamento_Sussurro", null,
                    N("voz", "Voz", "Tila.", "e1"),
                    N("e1", "Elena", "\"Viva.\" Isso é sumério. E eu entendi sem pensar.", "e2"),
                    N("e2", "Elena", "De onde veio essa tabuleta? Eu não peguei isso ontem.", "e3"),
                    N("e3", "Elena", "Ainda está morna... como se alguém tivesse acabado de soltar.")),

                corpos = Dialogue("Acampamento_Corpos", ev.examinouOsCorpos,
                    N("e1", "Elena", "Não... Não, não. Todos eles.", "e2"),
                    N("e2", "Elena", "Nenhum tiro. Nenhum sangue. Só essas queimaduras...", "e3"),
                    N("e3", "Elena", "Não são queimaduras comuns. São linhas e ângulos, como cunhas. Parece escrita.", "e4"),
                    N("e4", "Elena", "E estão todos virados para a minha barraca.", "e5"),
                    N("e5", "Elena", "Sete. Falta um. Cadê o Tom?")),

                bolsaDoTom = Dialogue("Acampamento_BolsaDoTom", ev.achouBolsaDoTom,
                    N("e1", "Elena", "A bolsa da câmera do Tom. Vazia.", "e2"),
                    N("e2", "Elena", "Ele não largava essa câmera nem para dormir. Se ela sumiu, talvez ele tenha saído daqui andando.", "e3"),
                    N("e3", "Elena", "E os filmes? Ele trocava de rolo toda noite. Devem estar por aí.")),

                radioKast = Dialogue("Acampamento_RadioKast", ev.falouComKast,
                    N("k1", "Kast (rádio)", "Acampamento de Ur, aqui é o Dr. Aurelian Kast, da Fundação Enlil. Doutora Varga, está me ouvindo? Câmbio.", "e1"),
                    N("e1", "Elena", "Estou ouvindo. Aqui é Elena Varga. A minha equipe... estão todos mortos.", "k2"),
                    N("k2", "Kast (rádio)", "Graças a Deus a senhora está bem. Sinto muito pela sua equipe. Sinto de verdade.", "k3"),
                    N("k3", "Kast (rádio)", "Uma equipe de resgate já está a caminho. Vinte minutos. Fique onde está e não toque em nada, por favor.", "k4"),
                    N("k4", "Kast (rádio)", "Sua mãe também tinha esse talento: estar no lugar errado na hora certa. Aguente firme, doutora.", null,
                        C("Estou esperando.", "confia", ev.kastConfiou),
                        C("Como vocês sabem que eu estou viva?", "desconfia", ev.kastDesconfiou)),
                    N("confia", "Kast (rádio)", "Ótimo. Vinte minutos, doutora. Câmbio, desligo.", "e2"),
                    N("desconfia", "Kast (rádio)", "...Não sabíamos, doutora. Torcíamos. Monitoramos a frequência da expedição desde a invasão, e o seu nome estava na lista. Vinte minutos. Câmbio, desligo.", "e2"),
                    N("e2", "Elena", "Ele conhecia a minha mãe?")),

                helicoptero = Dialogue("Acampamento_Helicoptero", null,
                    N("e1", "Elena", "Um helicóptero? Já? Ele disse vinte minutos.", "e2"),
                    N("e2", "Elena", "Da beira da elevação dá para ver o heliponto.")),

                execucaoVista = Dialogue("Acampamento_ExecucaoVista", null,
                    N("e1", "Elena", "Não... Eles atiraram nele. O Hassan só estava pedindo ajuda.", "e2"),
                    N("e2", "Elena", "Nem hesitaram. Isso não é um resgate.", "e3"),
                    N("e3", "Elena", "Eles vão subir. Preciso sair daqui, para as trincheiras.")),

                execucaoOuvida = Dialogue("Acampamento_ExecucaoOuvida", null,
                    N("e1", "Elena", "Um tiro. Lá embaixo, perto do helicóptero.", "e2"),
                    N("e2", "Elena", "Resgate não chega atirando. Preciso sair daqui, para as trincheiras.")),
            };

            mainQuest = Asset<QuestDefinition>("Quests/Sobrevivente", quest =>
            {
                quest.displayName = "Sobrevivente";
                quest.kind = QuestKind.Principal;
                quest.steps = new[]
                {
                    Step("Pegue a faca de campo (E ou A para interagir)", item: knife),
                    Step("Saia da barraca (WASD ou analógico para andar; Shift para correr)", ev.saiuDaBarraca),
                    Step("Procure a equipe", ev.examinouOsCorpos),
                    Step("Procure o Tom", ev.achouBolsaDoTom),
                    Step("Atenda o rádio do acampamento", ev.falouComKast),
                    Step("Espere o resgate", ev.viuExecucao),
                    Step("Fuja para as trincheiras, ao norte", ev.chegouAsTrincheiras),
                };
            });

            tomQuest = Asset<QuestDefinition>("Quests/OQueOTomViu", quest =>
            {
                quest.displayName = "O que o Tom viu";
                quest.kind = QuestKind.Secundaria;
                quest.startOn = ev.achouBolsaDoTom;
                quest.steps = new[]
                {
                    Step("Encontre os rolos de filme do Tom", item: filmRoll, count: 3),
                    Step("Leve os filmes a alguém que saiba revelá-los", ev.revelouFilmesDoTom),
                };
            });

            AssetDatabase.SaveAssets();
        }

        // ---------- Elena e sistemas ----------

        // Põe a Elena dentro da barraca, com a tabuleta na mão e a faca travada até ela pegar.
        static GameObject SetupElena()
        {
            GameObject elena = GameObject.FindWithTag("Player");
            if (elena == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Nibiru/Prefabs/Player/Elena.prefab");
                elena = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(elena, "Colocar Elena");
            }
            elena = elena.transform.root.gameObject;

            Undo.RecordObject(elena.transform, "Posicionar Elena");
            elena.transform.SetPositionAndRotation(new Vector3(-12f, Top, -120f), Quaternion.identity);
            PrefabUtility.RecordPrefabInstancePropertyModifications(elena.transform);

            var inventory = elena.GetComponentInChildren<PlayerInventory>();
            var inventorySo = new SerializedObject(inventory);
            SerializedProperty items = inventorySo.FindProperty("startingItems");
            bool hasTablet = false;
            for (int i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue == tablet) hasTablet = true;
            if (!hasTablet && tablet != null)
            {
                items.arraySize++;
                SerializedProperty added = items.GetArrayElementAtIndex(items.arraySize - 1);
                added.FindPropertyRelative("item").objectReferenceValue = tablet;
                added.FindPropertyRelative("count").intValue = 1;
            }
            inventorySo.ApplyModifiedProperties();

            var melee = elena.GetComponentInChildren<Combat.PlayerMeleeAttack>();
            var meleeSo = new SerializedObject(melee);
            meleeSo.FindProperty("requiredItem").objectReferenceValue = knife;
            meleeSo.FindProperty("inventory").objectReferenceValue = inventory;
            meleeSo.ApplyModifiedProperties();

            return elena;
        }

        static void BuildSystems(Transform root, GameObject elena)
        {
            var go = new GameObject("Missoes");
            go.transform.SetParent(root, false);
            var manager = go.AddComponent<QuestManager>();
            SetField(manager, "quests", new[] { mainQuest, tomQuest });
            SetField(manager, "inventory", elena.GetComponentInChildren<PlayerInventory>());

            // O sussurro da voz logo depois de acordar.
            var whisper = DialogueTrigger(go.transform, "Dialogo_Sussurro", dlg.sussurro);
            var intro = go.AddComponent<TimedSequence>();
            SetField(intro, "playOnStart", true);
            SetField(intro, "skipIfRaised", ev.saiuDaBarraca);
            SetSteps(intro, S("sussurro ao acordar", 1.5f, e => Call(e, whisper.StartDialogue)));
        }

        // ---------- Barraca da Elena ----------

        static void BuildTent(Transform g)
        {
            Prim(PrimitiveType.Cube, g, "Catre", new Vector3(-14f, Top + 0.25f, -120f), new Vector3(0.9f, 0.5f, 2f), "BO_LonaVerde");
            Prim(PrimitiveType.Cube, g, "Caixa_Faca", new Vector3(-10.2f, Top + 0.3f, -117.6f), Vector3.one * 0.6f, "BO_Madeira");

            GameObject faca = Prim(PrimitiveType.Cube, g, "Faca_De_Campo", new Vector3(-10.2f, Top + 0.63f, -117.6f), new Vector3(0.06f, 0.04f, 0.32f), "BO_Metal", 30f);
            Pickup(faca, knife, 1, null);

            // Gatilhos fora da porta: o objetivo "sair da barraca" e o primeiro ponto de retorno.
            GameObject exit = Zone(g, "Gatilho_SaiuDaBarraca", new Vector3(-12f, Top + 1f, -115.6f), new Vector3(3f, 2f, 1f));
            SetField(exit.AddComponent<StoryEventTrigger>(), "storyEvent", ev.saiuDaBarraca);

            Checkpoint(g, "Checkpoint_Barraca", new Vector3(-12f, Top + 1f, -114.4f), new Vector3(4f, 2f, 1f), new Vector3(-12f, Top, -114.4f), 0f);
        }

        // ---------- Corpos e bolsa do Tom ----------

        static void BuildCircle(Transform g)
        {
            // Mesmas posições dos corpos do blockout; aqui só entram as áreas de "Examinar".
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                var pos = new Vector3(Mathf.Cos(angle) * 3.2f, Top + 0.4f, -112f + Mathf.Sin(angle) * 3.2f);
                GameObject zone = Zone(g, $"Examinar_Corpo_{i + 1}", pos, new Vector3(0.9f, 0.8f, 2.2f), -angle * Mathf.Rad2Deg + 90f);
                DialogueStarterOn(zone, dlg.corpos, "Examinar", interactable: true);
            }

            // A bolsa fica na frente da barraca do Tom (Tenda_Equipe_2).
            GameObject bag = Prim(PrimitiveType.Cube, g, "Bolsa_Camera_Tom", new Vector3(4.4f, Top + 0.15f, -117.4f), new Vector3(0.4f, 0.3f, 0.25f), "BO_VerdeOliva", 20f);
            DialogueStarterOn(bag, dlg.bolsaDoTom, "Examinar a bolsa", interactable: true);

            // Primeiro rolo de filme, atrás do caminhão.
            GameObject roll = Prim(PrimitiveType.Cylinder, g, "Rolo_De_Filme_1", new Vector3(17.9f, Top + 0.06f, -108.4f), new Vector3(0.12f, 0.06f, 0.12f), "BO_Metal");
            Pickup(roll, filmRoll, 1, null);
        }

        // ---------- Rádio ----------

        static void BuildRadio(Transform g)
        {
            Prim(PrimitiveType.Cube, g, "Caixa_Radio", new Vector3(-16.8f, Top + 0.4f, -98.6f), Vector3.one * 0.8f, "BO_Madeira");
            Prim(PrimitiveType.Cube, g, "Aparelho_Radio", new Vector3(-16.8f, Top + 0.95f, -98.6f), new Vector3(0.5f, 0.3f, 0.3f), "BO_Metal");

            // Ligado só quando o Kast está chamando: chiado e o aviso "Atender o rádio".
            GameObject calling = Zone(g, "Radio_Chamando", new Vector3(-16.8f, Top + 0.9f, -98.6f), new Vector3(1f, 1f, 1f));
            DialogueStarterOn(calling, dlg.radioKast, "Atender o rádio", interactable: true);
            Audio(calling, ProvisionalSounds.RadioStatic, loop: true, playOnAwake: true, volume: 0.5f, minDistance: 1.5f, maxDistance: 18f);
            calling.SetActive(false);

            var poster = g.gameObject.AddComponent<NotificationPoster>();

            // Ao achar a bolsa, o rádio começa a chamar (a não ser que a conversa já tenha acontecido).
            var branch = g.gameObject.AddComponent<StoryEventBranch>();
            SetField(branch, "storyEvent", ev.falouComKast);
            var startCalling = GetField<UnityEvent>(branch, "ifNotRaised");
            SetActive(startCalling, calling, true);
            Notify(startCalling, poster, "O rádio do acampamento está chiando");

            Listener(g.gameObject, ev.achouBolsaDoTom, e => Call(e, branch.Run));
            Listener(g.gameObject, ev.falouComKast, e => SetActive(e, calling, false));
        }

        // ---------- Limites ----------

        // Paredes invisíveis na beira da elevação até a execução: a Elena não sai antes da hora,
        // e ninguém consegue impedir a cena lá embaixo.
        static GameObject BuildLimits(Transform g)
        {
            const float minX = -25f, maxX = 25f, minZ = -128f, maxZ = -92f, height = 4f;
            var walls = new GameObject("Paredes");
            walls.transform.SetParent(g, false);
            var warning = new GameObject("Aviso");
            warning.transform.SetParent(g, false);

            void Edge(Vector3 center, Vector3 size, Vector3 inward)
            {
                var wall = walls.AddComponent<BoxCollider>();
                wall.center = center;
                wall.size = size;
                var zone = warning.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.center = center + inward * 0.9f;
                zone.size = size;
            }

            float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f, y = Top + height / 2f;
            Edge(new Vector3(cx, y, maxZ - 0.2f), new Vector3(maxX - minX, height, 0.4f), Vector3.back);
            Edge(new Vector3(cx, y, minZ + 0.2f), new Vector3(maxX - minX, height, 0.4f), Vector3.forward);
            Edge(new Vector3(maxX - 0.2f, y, cz), new Vector3(0.4f, height, maxZ - minZ), Vector3.left);
            Edge(new Vector3(minX + 0.2f, y, cz), new Vector3(0.4f, height, maxZ - minZ), Vector3.right);

            var poster = warning.AddComponent<NotificationPoster>();
            var zoneComponent = warning.AddComponent<PlayerZone>();
            Notify(GetField<UnityEvent>(zoneComponent, "onEnter"), poster, "Melhor não sair do acampamento agora");

            return g.gameObject;
        }

        // ---------- Resgate: helicóptero, trabalhador, soldados ----------

        static void BuildRescue(Transform g, GameObject limits)
        {
            // Caminho do helicóptero: chega do sudeste, alto, e pousa no heliponto.
            Transform path = Group(g, "Rota_Helicoptero");
            Transform[] heliPoints =
            {
                Point(path, "P1", new Vector3(170f, 70f, -260f)),
                Point(path, "P2", new Vector3(80f, 35f, -150f)),
                Point(path, "P3", new Vector3(38f, 10f, -90f)),
                Point(path, "P4_Pouso", HelipadPoint),
            };

            GameObject heli = BuildHelicopter(g);
            var heliMove = heli.AddComponent<MoveAlongPath>();
            SetField(heliMove, "points", heliPoints);
            SetField(heliMove, "speed", 22f);
            SetField(heliMove, "turnSpeed", 60f);
            SetField(heliMove, "slowDownDistance", 30f);
            heli.SetActive(false);

            // O trabalhador ferido que voltou ao acampamento, vindo do leste até o helicóptero.
            Transform workerPath = Group(g, "Rota_Trabalhador");
            Transform[] workerPoints =
            {
                Point(workerPath, "P1", new Vector3(39f, 0.9f, -93f)),
                Point(workerPath, "P2", new Vector3(34f, 0.9f, -86.5f)),
            };
            GameObject worker = Prim(PrimitiveType.Capsule, g, "Trabalhador_Ferido", workerPoints[0].position, new Vector3(0.5f, 0.9f, 0.5f), "BO_TijoloBarro", collider: ColliderMode.None);
            var workerMove = worker.AddComponent<MoveAlongPath>();
            SetField(workerMove, "points", workerPoints);
            SetField(workerMove, "speed", 1.3f);
            SetField(workerMove, "turnSpeed", 180f);
            SetField(workerMove, "slowDownDistance", 0f);
            worker.SetActive(false);

            GameObject workerBody = Prim(PrimitiveType.Capsule, g, "Corpo_Trabalhador", new Vector3(34f, 0.25f, -86.5f), new Vector3(0.5f, 0.9f, 0.5f), "BO_TijoloBarro", collider: ColliderMode.None);
            workerBody.transform.rotation = Quaternion.Euler(90f, 40f, 0f);
            workerBody.SetActive(false);

            // Soldados: descem do helicóptero só como figurantes (sem IA) e começam a procurar
            // a Elena depois da execução.
            Transform soldiers = Group(g, "Soldados");
            Transform routeA = Group(g, "Rota_Soldado_A");
            Transform routeB = Group(g, "Rota_Soldado_B");
            Transform[] patrolA =
            {
                Point(routeA, "P1_PeDaRampa", new Vector3(2f, 0f, -77f)),
                Point(routeA, "P2_TopoDaRampa", new Vector3(0f, Top, -95f)),
                Point(routeA, "P3_Circulo", new Vector3(0f, Top, -107f)),
                Point(routeA, "P4_BarracaElena", new Vector3(-12f, Top, -113f)),
                Point(routeA, "P5_Radio", new Vector3(-15f, Top, -100f)),
            };
            Transform[] patrolB =
            {
                Point(routeB, "P1_PeDaRampa", new Vector3(-2f, 0f, -77f)),
                Point(routeB, "P2_Caminhoes", new Vector3(13f, Top, -110f)),
                Point(routeB, "P3_Barracas", new Vector3(4f, Top, -115f)),
                Point(routeB, "P4_Triagem", new Vector3(-8f, Top, -104f)),
            };

            Vector3 workerEnd = workerPoints[1].position;
            GameObject shooter = Soldier(soldiers, "Soldado_A", new Vector3(30.5f, 0f, -86f), workerEnd, patrolA, leader: false);
            Soldier(soldiers, "Soldado_B", new Vector3(27f, 0f, -85f), workerEnd, patrolB, leader: false);
            Soldier(soldiers, "Lider_Heliponto", new Vector3(33f, 0f, -79f), new Vector3(10f, 0f, -95f), null, leader: true);
            soldiers.gameObject.SetActive(false);

            // O tiro: risco de luz do cano do soldado até o trabalhador, e o som ouvido de longe.
            var shotGo = new GameObject("Tiro_Execucao");
            shotGo.transform.SetParent(g, false);
            shotGo.transform.position = workerEnd;
            var tracer = shotGo.AddComponent<LineRenderer>();
            tracer.widthMultiplier = 0.04f;
            tracer.sharedMaterial = Mat("BO_Ouro");
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.useWorldSpace = true;
            Audio(shotGo, ProvisionalSounds.Gunshot, loop: false, playOnAwake: false, volume: 1f, minDistance: 25f, maxDistance: 700f);
            var shot = shotGo.AddComponent<ScriptedShot>();
            SetField(shot, "origin", MuzzleOf(shooter));
            SetField(shot, "target", worker.transform);
            SetField(shot, "tracer", tracer);
            SetField(shot, "sound", shotGo.GetComponent<AudioSource>());

            // A fala da Elena depois do tiro: o que ela viu (no mirante) ou o que ouviu.
            var reaction = new GameObject("Reacao_Execucao");
            reaction.transform.SetParent(g, false);
            var seen = DialogueTrigger(reaction.transform, "Dialogo_Vista", dlg.execucaoVista);
            var heard = DialogueTrigger(reaction.transform, "Dialogo_Ouvida", dlg.execucaoOuvida);
            var branch = reaction.AddComponent<StoryEventBranch>();
            SetField(branch, "storyEvent", ev.chegouAoMirante);
            Call(GetField<UnityEvent>(branch, "ifRaised"), seen.StartDialogue);
            Call(GetField<UnityEvent>(branch, "ifNotRaised"), heard.StartDialogue);
            var raiser = reaction.AddComponent<StoryEventRaiser>();
            SetField(raiser, "storyEvent", ev.viuExecucao);

            var heliDialogue = DialogueTrigger(g, "Dialogo_Helicoptero", dlg.helicoptero);

            // Sequência 1: depois do rádio, o helicóptero aparece e vem pousar.
            var arrival = g.gameObject.AddComponent<TimedSequence>();
            SetField(arrival, "skipIfRaised", ev.viuExecucao);
            SetSteps(arrival,
                S("helicóptero aparece ao longe", 15f, e =>
                {
                    SetActive(e, heli, true);
                    Call(e, heliMove.Play);
                }),
                S("Elena comenta", 4f, e => Call(e, heliDialogue.StartDialogue)));
            Listener(g.gameObject, ev.falouComKast, e => Call(e, arrival.Play));

            // Sequência 2: no pouso, o trabalhador se aproxima e é executado. A cena dispara quando
            // a Elena chega ao mirante ou, se ela não for, depois de um tempo (aí ela só ouve).
            var execution = g.gameObject.AddComponent<TimedSequence>();
            SetField(execution, "skipIfRaised", ev.viuExecucao);
            SetSteps(execution,
                S("soldados descem, trabalhador vem pedir ajuda", 1f, e =>
                {
                    SetActive(e, soldiers.gameObject, true);
                    SetActive(e, worker, true);
                    Call(e, workerMove.Play);
                }),
                S("trabalhador chega perto", 5f, null),
                S("tiro: no mirante ou depois do tempo", 20f, e =>
                {
                    Call(e, shot.Fire);
                    SetActive(e, worker, false);
                    SetActive(e, workerBody, true);
                }, ev.chegouAoMirante),
                S("fala da Elena e começa a busca", 1.5f, e =>
                {
                    Call(e, branch.Run);
                    Call(e, raiser.Raise);
                }));
            Call(GetField<UnityEvent>(heliMove, "onArrived"), execution.Play);

            // Estado depois da execução, ao vivo e ao voltar de um checkpoint: helicóptero pousado,
            // trabalhador morto, soldados procurando e limites abertos.
            Listener(g.gameObject, ev.viuExecucao, e =>
            {
                SetActive(e, heli, true);
                Call(e, heliMove.SnapToEnd);
                SetActive(e, worker, false);
                SetActive(e, workerBody, true);
                SetActive(e, soldiers.gameObject, true);
                foreach (Transform soldier in soldiers)
                {
                    SetEnabled(e, soldier.GetComponent<NavMeshAgent>(), true);
                    SetEnabled(e, soldier.GetComponent<SoldierPerception>(), true);
                    SetEnabled(e, soldier.GetComponent<SoldierAI>(), true);
                }
                SetActive(e, limits, false);
            });

            // Mirante: canto nordeste da elevação, de frente para o heliponto.
            // Caixotes ao lado (e não na frente) da vista para o heliponto.
            Prim(PrimitiveType.Cube, g, "Caixotes_Mirante_1", new Vector3(15.5f, Top + 0.4f, -93.4f), Vector3.one * 0.8f, "BO_Madeira", 10f);
            Prim(PrimitiveType.Cube, g, "Caixotes_Mirante_2", new Vector3(16.6f, Top + 0.3f, -93.2f), Vector3.one * 0.6f, "BO_Madeira", -15f);
            GameObject lookout = Zone(g, "Gatilho_Mirante", new Vector3(19f, Top + 1f, -96.5f), new Vector3(11f, 2f, 8f));
            SetField(lookout.AddComponent<StoryEventTrigger>(), "storyEvent", ev.chegouAoMirante);
            Checkpoint(g, "Checkpoint_Mirante", new Vector3(19f, Top + 1f, -96.5f), new Vector3(11f, 2f, 8f), new Vector3(19f, Top, -97f), 45f);
        }

        static GameObject BuildHelicopter(Transform g)
        {
            // Formas simples nas cores da Fundação: branco com faixa turquesa.
            var heli = new GameObject("Helicoptero");
            heli.transform.SetParent(g, false);
            heli.transform.SetPositionAndRotation(HelipadPoint, Quaternion.Euler(0f, -45f, 0f));
            Transform h = heli.transform;

            Local(PrimitiveType.Cube, h, "Cabine", new Vector3(0f, 1.6f, 0.4f), new Vector3(2.4f, 2.2f, 5f), "BO_BrancoFundacao", ColliderMode.Solid);
            Local(PrimitiveType.Cube, h, "Faixa", new Vector3(0f, 1.3f, 0.4f), new Vector3(2.45f, 0.35f, 5.05f), "BO_TurquesaFundacao");
            Local(PrimitiveType.Cube, h, "Cauda", new Vector3(0f, 2.1f, -4.3f), new Vector3(0.5f, 0.6f, 5f), "BO_BrancoFundacao");
            Local(PrimitiveType.Cube, h, "Leme", new Vector3(0f, 2.8f, -6.6f), new Vector3(0.15f, 1.4f, 0.8f), "BO_TurquesaFundacao");
            Local(PrimitiveType.Cube, h, "Esqui_E", new Vector3(-1.1f, 0.08f, 0.4f), new Vector3(0.15f, 0.15f, 4.6f), "BO_Metal");
            Local(PrimitiveType.Cube, h, "Esqui_D", new Vector3(1.1f, 0.08f, 0.4f), new Vector3(0.15f, 0.15f, 4.6f), "BO_Metal");

            var rotor = new GameObject("Rotor");
            rotor.transform.SetParent(h, false);
            rotor.transform.localPosition = new Vector3(0f, 2.85f, 0.4f);
            Local(PrimitiveType.Cube, rotor.transform, "Pa_1", Vector3.zero, new Vector3(11f, 0.05f, 0.35f), "BO_Metal");
            Local(PrimitiveType.Cube, rotor.transform, "Pa_2", Vector3.zero, new Vector3(0.35f, 0.05f, 11f), "BO_Metal");
            SetField(rotor.AddComponent<Spinner>(), "speed", 900f);

            var tailRotor = new GameObject("Rotor_Cauda");
            tailRotor.transform.SetParent(h, false);
            tailRotor.transform.localPosition = new Vector3(0.3f, 2.4f, -6.6f);
            Local(PrimitiveType.Cube, tailRotor.transform, "Pa", Vector3.zero, new Vector3(0.05f, 1.6f, 0.2f), "BO_Metal");
            var tailSpin = tailRotor.AddComponent<Spinner>();
            SetField(tailSpin, "axis", Vector3.right);
            SetField(tailSpin, "speed", 1400f);

            Audio(heli, ProvisionalSounds.Helicopter, loop: true, playOnAwake: true, volume: 1f, minDistance: 20f, maxDistance: 500f);
            return heli;
        }

        static GameObject Soldier(Transform parent, string name, Vector3 position, Vector3 lookAt, Transform[] patrol, bool leader)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            Vector3 flat = Vector3.ProjectOnPlane(lookAt - position, Vector3.up);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(flat));

            var settings = AssetDatabase.LoadAssetAtPath<SoldierSettings>($"{DataFolder}/AI/{(leader ? "LiderEsquadrao" : "SoldadoContencao")}.asset");
            var ai = go.GetComponent<SoldierAI>();
            var aiSo = new SerializedObject(ai);
            aiSo.FindProperty("settings").objectReferenceValue = settings;
            aiSo.FindProperty("isLeader").boolValue = leader;
            SerializedProperty waypoints = aiSo.FindProperty("waypoints");
            waypoints.arraySize = patrol?.Length ?? 0;
            for (int i = 0; i < waypoints.arraySize; i++) waypoints.GetArrayElementAtIndex(i).objectReferenceValue = patrol[i];
            aiSo.ApplyModifiedProperties();

            var perception = go.GetComponent<SoldierPerception>();
            var perceptionSo = new SerializedObject(perception);
            perceptionSo.FindProperty("settings").objectReferenceValue = settings;
            perceptionSo.ApplyModifiedProperties();

            // Figurante até a execução: sem IA, sem percepção e sem malha de navegação.
            ai.enabled = false;
            perception.enabled = false;
            // Sem o agente ligado, ninguém levanta o soldado até a altura certa: põe à mão.
            var agent = go.GetComponent<NavMeshAgent>();
            go.transform.position = position + Vector3.up * agent.baseOffset;
            agent.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
            PrefabUtility.RecordPrefabInstancePropertyModifications(perception);
            PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
            return go;
        }

        static Transform MuzzleOf(GameObject soldier)
        {
            var so = new SerializedObject(soldier.GetComponent<SoldierAI>());
            var muzzle = so.FindProperty("muzzle").objectReferenceValue as Transform;
            return muzzle != null ? muzzle : soldier.transform;
        }

        // ---------- Saídas ----------

        static void BuildExits(Transform g)
        {
            // Entrada das trincheiras, ao norte do pé da rampa: fim da parte do Acampamento.
            GameObject trenches = Zone(g, "Gatilho_Trincheiras", new Vector3(0f, 1.5f, -77.5f), new Vector3(50f, 3f, 2f));
            SetField(trenches.AddComponent<StoryEventTrigger>(), "storyEvent", ev.chegouAsTrincheiras);
        }

        // ---------- Peças da cena ----------

        enum ColliderMode { Solid, Trigger, None }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Transform Point(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = position;
            return t;
        }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, string material, float yaw = 0f, ColliderMode collider = ColliderMode.Solid)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            ApplyCollider(go, collider);
            return go;
        }

        static GameObject Local(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 scale, string material, ColliderMode collider = ColliderMode.None)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            ApplyCollider(go, collider);
            return go;
        }

        static void ApplyCollider(GameObject go, ColliderMode mode)
        {
            var collider = go.GetComponent<Collider>();
            if (mode == ColliderMode.None) Object.DestroyImmediate(collider);
            else collider.isTrigger = mode == ColliderMode.Trigger;
        }

        // Caixa invisível marcada como gatilho.
        static GameObject Zone(Transform parent, string name, Vector3 center, Vector3 size, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        static void Pickup(GameObject go, ItemDefinition item, int amount, StoryEvent raise)
        {
            var pickup = go.AddComponent<ItemPickup>();
            SetField(pickup, "item", item);
            SetField(pickup, "amount", amount);
            SetField(pickup, "raiseOnPickup", raise);
            StableId(go);
        }

        static void Checkpoint(Transform parent, string name, Vector3 center, Vector3 size, Vector3 spawn, float spawnYaw)
        {
            GameObject zone = Zone(parent, name, center, size);
            Transform spawnPoint = Point(zone.transform, "Retorno", spawn);
            spawnPoint.rotation = Quaternion.Euler(0f, spawnYaw, 0f);
            var checkpoint = zone.AddComponent<Save.Checkpoint>();
            SetField(checkpoint, "spawnPoint", spawnPoint);
            StableId(zone);
        }

        // Código fixo a partir do nome, para continuar o mesmo quando o menu reconstrói a cena.
        static void StableId(GameObject go)
        {
            var id = go.GetComponent<PersistentId>();
            if (id == null) id = go.AddComponent<PersistentId>();
            SetField(id, "id", "acampamento." + go.name.ToLowerInvariant());
        }

        static DialogueStarter DialogueStarterOn(GameObject go, DialogueDefinition dialogue, string prompt, bool interactable)
        {
            var starter = go.AddComponent<DialogueStarter>();
            SetField(starter, "dialogue", dialogue);
            SetField(starter, "prompt", prompt);
            SetField(starter, "interactable", interactable);
            SetField(starter, "onlyOnce", true);
            return starter;
        }

        // Conversa sem objeto para interagir, começada por uma sequência ou um evento.
        static DialogueStarter DialogueTrigger(Transform parent, string name, DialogueDefinition dialogue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return DialogueStarterOn(go, dialogue, "", interactable: false);
        }

        static void Audio(GameObject go, string clipPath, bool loop, bool playOnAwake, float volume, float minDistance, float maxDistance)
        {
            var source = go.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0.3f;
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        // ---------- Eventos do Inspector ----------

        static void Listener(GameObject go, StoryEvent storyEvent, Action<UnityEvent> fill)
        {
            var listener = go.AddComponent<StoryEventListener>();
            SetField(listener, "storyEvent", storyEvent);
            fill(GetField<UnityEvent>(listener, "response"));
        }

        static TimedSequence.Step S(string note, float wait, Action<UnityEvent> fill, StoryEvent orUntil = null)
        {
            var step = new TimedSequence.Step { note = note, wait = wait, orUntil = orUntil };
            fill?.Invoke(step.action);
            return step;
        }

        static void SetSteps(TimedSequence sequence, params TimedSequence.Step[] steps) => SetField(sequence, "steps", steps);

        static void Call(UnityEvent e, UnityAction action) => UnityEventTools.AddPersistentListener(e, action);

        static void SetActive(UnityEvent e, GameObject go, bool active) =>
            UnityEventTools.AddBoolPersistentListener(e, go.SetActive, active);

        static void SetEnabled(UnityEvent e, Behaviour behaviour, bool enabled)
        {
            var setter = (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), behaviour, "set_enabled");
            UnityEventTools.AddBoolPersistentListener(e, setter, enabled);
        }

        static void Notify(UnityEvent e, NotificationPoster poster, string message) =>
            UnityEventTools.AddStringPersistentListener(e, poster.Post, message);

        // ---------- Campos privados ----------

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static void SetField(Object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, Flags) ?? throw new MissingFieldException(target.GetType().Name, field);
            info.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        static T GetField<T>(Object target, string field) where T : class
        {
            FieldInfo info = target.GetType().GetField(field, Flags) ?? throw new MissingFieldException(target.GetType().Name, field);
            return (T)info.GetValue(target);
        }

        // ---------- Arquivos de dados ----------

        // Carrega o arquivo se existir; se não, cria e preenche. Nunca sobrescreve um que já existe.
        static T Asset<T>(string relativePath, Action<T> fill) where T : ScriptableObject
        {
            string path = $"{DataFolder}/{relativePath}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static StoryEvent Event(string name, string note) =>
            Asset<StoryEvent>("Events/" + name, e => e.note = note);

        static DialogueDefinition Dialogue(string name, StoryEvent raiseOnEnd, params DialogueNode[] nodes) =>
            Asset<DialogueDefinition>("Dialogues/" + name, d =>
            {
                d.nodes = nodes;
                d.raiseOnEnd = raiseOnEnd;
            });

        static DialogueNode N(string id, string speaker, string text, string next = null, params DialogueChoice[] choices) =>
            new DialogueNode { id = id, speaker = speaker, text = text, next = next ?? "", choices = choices };

        static DialogueChoice C(string text, string next, StoryEvent raise) =>
            new DialogueChoice { text = text, next = next, raise = raise };

        static QuestStep Step(string objective, StoryEvent completeOn = null, ItemDefinition item = null, int count = 1) =>
            new QuestStep { objective = objective, completeOn = completeOn, requiredItem = item, requiredCount = count };
    }
}
