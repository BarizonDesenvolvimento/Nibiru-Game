using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Quests;
using Nibiru.Sequences;
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
    /// Povoa a Aldeia (área 3 do Sítio de Ur). Por enquanto, o passo 6 da missão "Sobrevivente":
    /// na entrada da Aldeia, um soldado encurrala a Elena, o Samir o derruba com a faca, conversa
    /// com ela ("O guia") e a leva até o pátio, diante da casa do altar.
    ///
    /// O encontro acontece numa das entradas da Aldeia: a que estiver à frente da Elena e mais
    /// perto dela quando ela entra na área. Os pontos são conferidos na malha de navegação e
    /// com espaço livre em volta a cada execução (avisos no Console).
    ///
    /// Os arquivos de dados só são criados se ainda não existirem, e os passos novos da missão
    /// só entram se ainda não estiverem lá. Os objetos da cena ficam em "Conteudo_Aldeia" e são
    /// reconstruídos a cada execução. Rode depois do Acampamento e das Trincheiras.
    /// Menu: Nibiru > Conteúdo > Povoar Aldeia na cena aberta.
    /// </summary>
    static class VillageContent
    {
        const string RootName = "Conteudo_Aldeia";

        // Entradas da Aldeia, do lado de fora das casas do blockout. O primeiro é onde a cena do
        // encontro fica montada antes de ser levada (e onde o corpo aparece ao voltar de um checkpoint).
        static readonly (string name, Vector3 position)[] EncounterSpots =
        {
            ("Entrada_Leste", new Vector3(-36f, 0f, -16f)),     // rua larga que dá no pátio
            ("Entrada_Sudeste", new Vector3(-33f, 0f, -47f)),   // quem vem direto das trincheiras
            ("Entrada_Sul", new Vector3(-55f, 0f, -46f)),       // vão entre as casas do sul
            ("Entrada_Norte", new Vector3(-46f, 0f, 8f)),       // entre a mesquita e a estrada da base
        };

        // Espaço livre pedido em volta de cada ponto: soldado, Samir atrás dele e o corpo no chão.
        const float EncounterClearance = 2.5f;

        // O Samir para no pátio, na frente da casa do altar, olhando para o leste (por onde a Elena chega).
        static readonly Vector3 SamirDestination = new Vector3(-47f, 0f, -23f);
        const float SamirDestinationYaw = 90f;

        static readonly Vector3 PatioCenter = new Vector3(-47f, 1f, -21f);

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent aproximouSeDaAldeia, chegouAAldeia, chegouAoPatioComSamir;
        }

        class Dialogues
        {
            public DialogueDefinition encurralada, oGuia, chegadaPatio;
        }

        static Events ev;
        static Dialogues dlg;

        [MenuItem("Nibiru/Conteúdo/Povoar Aldeia na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar a Aldeia.");
                return;
            }

            IdPrefix = "aldeia";
            CreateData();

            Transform root = NewRoot(RootName, "Povoar Aldeia");
            BuildSamirEncounter(Group(root, "Encontro_Samir"));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Aldeia povoada.");
        }

        static void CreateData()
        {
            ev = new Events
            {
                aproximouSeDaAldeia = Event("AproximouSeDaAldeia", "A Elena entrou na área da Aldeia (por qualquer lado). Começa o encontro com o Samir numa das entradas."),
                chegouAAldeia = Event("ChegouAAldeia", ""),
                chegouAoPatioComSamir = Event("ChegouAoPatioComSamir", "Seguindo o Samir, a Elena chegou ao pátio da Aldeia, na frente da casa do altar."),
            };

            // O evento já existia, criado pelas Trincheiras: a nota antiga dizia que o gatilho viria depois.
            const string oldNote = "A Elena chegou à Aldeia dos escavadores (o gatilho entra com o conteúdo da Aldeia).";
            if (string.IsNullOrEmpty(ev.chegouAAldeia.note) || ev.chegouAAldeia.note == oldNote)
            {
                ev.chegouAAldeia.note = "Na entrada da Aldeia, o Samir derrubou o soldado e terminou a conversa com a Elena (\"O guia\"). Fecha o objetivo das Trincheiras.";
                EditorUtility.SetDirty(ev.chegouAAldeia);
            }

            dlg = new Dialogues
            {
                encurralada = Dialogue("Aldeia_Encurralada", null,
                    N("s1", "Soldado", "Parada! Mãos onde eu possa ver.", "s2"),
                    N("s2", "Soldado", "Comando, Equipe Dois. Achei a doutora, na beira da aldeia. Viva.", "e1"),
                    N("e1", "Elena", "Eu vi o que vocês fizeram com o Hassan.", "s3"),
                    N("s3", "Soldado", "Ninguém perguntou, doutora. De joelhos. Devagar.")),

                oGuia = Dialogue("Aldeia_OGuia", ev.chegouAAldeia,
                    N("e1", "Elena", "Samir?! Você está vivo...", "s1"),
                    N("s1", "Samir", "Fale baixo. Daqui a pouco o rádio dele chama e ninguém responde.", "s2"),
                    N("s2", "Samir", "(Ele olha a luz azul que escapa da bolsa dela. Uma pausa.) Esconda isso. E não mostre a ninguém.", "e2"),
                    N("e2", "Elena", "Você sabe o que é isso?", "s3"),
                    N("s3", "Samir", "Sei que gente morre por menos.", "e3"),
                    N("e3", "Elena", "O Kast disse que era um resgate...", "s4"),
                    N("s4", "Samir", "Ninguém veio resgatar ninguém. Vieram apagar quem viu.", null,
                        C("Quem é você, Samir? Guia nenhum mata assim.", "s5", null),
                        C("Para onde vamos?", "s6", null)),
                    N("s5", "Samir", "Sou quem conhece o caminho. Por hoje, basta.", "s6"),
                    N("s6", "Samir", "Ainda tem gente escondida na aldeia. Venha atrás de mim, e longe da estrada.")),

                chegadaPatio = Dialogue("Aldeia_ChegadaPatio", null,
                    N("s1", "Samir", "Aqui. Os soldados ainda não entraram nas casas. Ainda.")),
            };

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("Siga o Samir até a Aldeia", ev.chegouAoPatioComSamir));

            AssetDatabase.SaveAssets();
        }

        // ---------- Encontro com o Samir (passo 6) ----------

        static void BuildSamirEncounter(Transform g)
        {
            // Confere os pontos antes de criar qualquer peça nova, para elas não contarem como obstáculo.
            Vector3[] spots = EncounterSpots.Select(s => OnNavMesh(s.position, s.name, EncounterClearance)).ToArray();
            Vector3 destination = OnNavMesh(SamirDestination, "Destino_Samir", 1f);

            Transform spotGroup = Group(g, "Pontos_Encontro");
            Transform[] points = EncounterSpots.Select((s, i) => Point(spotGroup, s.name, spots[i])).ToArray();

            // A cena do encontro é montada no primeiro ponto, virada para o leste (para as trincheiras).
            // Na hora, o PlaceAtPointAhead leva a cena inteira para o ponto escolhido, virada para a Elena.
            Transform scene = Group(g, "Cena");
            scene.SetPositionAndRotation(points[0].position, Quaternion.Euler(0f, 90f, 0f));
            Vector3 origin = scene.position;
            Vector3 forward = scene.forward;

            GameObject soldier = Soldier(scene, "Soldado_Encontro", origin, origin + forward, null, leader: false);
            soldier.SetActive(false);
            GameObject body = Body(scene, "Corpo_Soldado_Encontro", origin - forward * 0.4f, scene.eulerAngles.y + 90f, "BO_VerdeOliva");
            body.SetActive(false);

            Transform samirDestination = Point(g, "Destino_Samir", destination);
            samirDestination.rotation = Quaternion.Euler(0f, SamirDestinationYaw, 0f);
            GameObject samir = Samir(scene, origin - forward * 1.6f, samirDestination);

            var picker = g.gameObject.AddComponent<PlaceAtPointAhead>();
            SetField(picker, "points", points);
            SetField(picker, "target", scene);

            var cornered = DialogueTrigger(g, "Dialogo_Encurralada", dlg.encurralada);
            var guide = DialogueTrigger(g, "Dialogo_OGuia", dlg.oGuia);

            // Ao entrar na área: o soldado surge na entrada à frente da Elena e a encurrala; depois
            // da fala dele, o Samir aparece atrás e o derruba; então os dois conversam.
            var encounter = g.gameObject.AddComponent<TimedSequence>();
            SetField(encounter, "skipIfRaised", ev.chegouAAldeia);
            SetSteps(encounter,
                S("soldado surge na entrada à frente da Elena", 0f, e =>
                {
                    Call(e, picker.Place);
                    SetActive(e, soldier, true);
                    Call(e, cornered.StartDialogue);
                }),
                S("Samir surge atrás do soldado", 0.4f, e => SetActive(e, samir, true)),
                S("Samir derruba o soldado", 0.6f, e =>
                {
                    SetActive(e, soldier, false);
                    SetActive(e, body, true);
                }),
                S("conversa com o Samir", 1f, e => Call(e, guide.StartDialogue)));

            // A área cobre a Aldeia inteira com folga: de onde a Elena vier, entra por ela.
            EventZone(g, "Gatilho_Aproximacao", new Vector3(-57f, 1.5f, -20f), new Vector3(62f, 3f, 76f), ev.aproximouSeDaAldeia);
            Listener(g.gameObject, ev.aproximouSeDaAldeia, e => Call(e, encounter.Play));

            // Pátio: a fala do Samir na chegada (só ao vivo) e o ponto de retorno, um pouco mais
            // para dentro, para o evento já estar guardado quando o checkpoint salvar.
            GameObject patio = EventZone(g, "Gatilho_Patio", PatioCenter, new Vector3(12f, 2f, 10f), ev.chegouAoPatioComSamir);
            patio.SetActive(false);
            var arrival = DialogueTrigger(g, "Dialogo_ChegadaPatio", dlg.chegadaPatio);
            Listener(arrival.gameObject, ev.chegouAoPatioComSamir, e => Call(e, arrival.StartDialogue));
            SetField(arrival.GetComponent<StoryEventListener>(), "replayOnLoad", false);
            Checkpoint(g, "Checkpoint_Patio", PatioCenter, new Vector3(6f, 2f, 5f), new Vector3(-44.5f, 0f, -19.5f), 215f);

            // Depois da conversa, ao vivo e ao voltar de um checkpoint: soldado no chão, Samir
            // andando para o pátio (ou já lá, se a Elena já tinha chegado) e o pátio esperando.
            var walker = samir.GetComponent<WalkToPoint>();
            var walkOrSnap = g.gameObject.AddComponent<StoryEventBranch>();
            SetField(walkOrSnap, "storyEvent", ev.chegouAoPatioComSamir);
            Call(GetField<UnityEvent>(walkOrSnap, "ifRaised"), walker.SnapToDestination);
            Call(GetField<UnityEvent>(walkOrSnap, "ifNotRaised"), walker.Walk);
            Listener(g.gameObject, ev.chegouAAldeia, e =>
            {
                SetActive(e, soldier, false);
                SetActive(e, body, true);
                SetActive(e, samir, true);
                SetActive(e, patio, true);
                Call(e, walkOrSnap.Run);
            });
        }

        // Samir em formas simples: cápsula cinza com a faca na mão, andando pela malha de navegação.
        static GameObject Samir(Transform parent, Vector3 position, Transform destination)
        {
            var samir = new GameObject("Samir");
            samir.transform.SetParent(parent, false);
            samir.transform.SetPositionAndRotation(position, parent.rotation);
            Local(PrimitiveType.Capsule, samir.transform, "Corpo", new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.9f, 0.6f), "BO_Rocha", ColliderMode.Solid);
            Local(PrimitiveType.Cube, samir.transform, "Faca", new Vector3(0.38f, 0.95f, 0.3f), new Vector3(0.04f, 0.03f, 0.28f), "BO_Metal");

            var agent = samir.AddComponent<NavMeshAgent>();
            agent.speed = 3.2f;
            agent.angularSpeed = 360f;
            agent.acceleration = 10f;
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0.2f;

            SetField(samir.AddComponent<WalkToPoint>(), "destination", destination);
            samir.SetActive(false);
            return samir;
        }

        // Leva o ponto para a malha de navegação e confere se há espaço livre em volta dele.
        static Vector3 OnNavMesh(Vector3 wanted, string label, float clearance)
        {
            if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"Aldeia: o ponto {label} {wanted} está fora da malha de navegação. Mude a posição no script.");
                return wanted;
            }

            Vector3 point = hit.position;
            Physics.SyncTransforms();
            Collider[] blocking = Physics.OverlapBox(point + Vector3.up * 1.2f, new Vector3(clearance, 0.9f, clearance), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            float edge = NavMesh.FindClosestEdge(point, out NavMeshHit edgeHit, NavMesh.AllAreas) ? edgeHit.distance : 0f;

            if (blocking.Length > 0)
                Debug.LogWarning($"Aldeia: o ponto {label} não tem espaço livre ({clearance} m). No caminho: {string.Join(", ", blocking.Select(c => c.name))}.");
            else if (edge < clearance)
                Debug.LogWarning($"Aldeia: o ponto {label} está a {edge:0.0} m da beira da malha de navegação (pedido: {clearance} m).");
            else
                Debug.Log($"Aldeia: ponto {label} conferido (na malha, livre, beira a {edge:0.0} m).");
            return point;
        }
    }
}
