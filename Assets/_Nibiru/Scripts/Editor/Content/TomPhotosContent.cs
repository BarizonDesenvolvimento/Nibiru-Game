using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Final da secundária "O que o Tom viu": com os três rolos na bolsa, a Umm Karim leva a Elena a Abu Faris, que tinha um
    /// estúdio de fotos em Nasiriyah e revela os filmes num quarto escuro improvisado (casinha perto da casa do altar, luz
    /// vermelha, bacias em formas simples). A recompensa é o item-chave "Fotos do Tom" (código estável fotos_do_tom), que
    /// abre o documento das seis fotos na tela de leitura.
    /// Menu: Nibiru > Conteúdo > Povoar O que o Tom viu (final) na cena aberta. Rode depois do menu da Aldeia.
    ///
    /// Os textos (conversas, documento, item, avisos) moram no pt-BR.json; este script só diz os códigos.
    /// PROVISÓRIO — revisar na governança: as falas do quarto escuro; o documento das fotos é o texto final do dono.
    /// </summary>
    static class TomPhotosContent
    {
        const string RootName = "Conteudo_QuartoEscuro";

        // A casa do altar fica em x de -50 a -44; a casinha do quarto escuro, ao lado, a oeste, aberta para o leste.
        static readonly Vector3 HouseCenter = new Vector3(-56f, 0f, -28f);
        static readonly Vector3 AbuFarisSpot = new Vector3(-56.4f, 0f, -28.2f);
        static readonly Vector3 UmmKarimDarkroomSpot = new Vector3(-55.2f, 0f, -27.1f);
        static readonly Vector3 UmmKarimHomeSpot = new Vector3(-47.9f, 0f, -30.7f);

        class Events
        {
            public StoryEvent led, revealed, readPhotos;
        }

        static Events ev;
        static DialogueDefinition ummFilms, darkroom, abuWaiting, abuAfter;
        static ItemDefinition filmRoll, photos;

        [MenuItem("Nibiru/Conteúdo/Povoar O que o Tom viu (final) na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar o final de \"O que o Tom viu\".");
                return;
            }

            IdPrefix = "quarto_escuro";
            if (!CreateData()) return;

            Transform root = NewRoot(RootName, "Povoar O que o Tom viu (final)");
            BuildHouse(Group(root, "Casinha"));
            GameObject interior = new GameObject("Interior_Quarto_Escuro");
            interior.transform.SetParent(root, false);
            BuildInterior(interior.transform);
            interior.SetActive(false);

            GameObject ummZone = Obj("Falar_UmmKarim");
            GameObject ummBody = Obj("Umm_Karim");
            if (ummZone == null || ummBody == null)
            {
                Debug.LogWarning("O final de \"O que o Tom viu\": a Umm Karim não está na cena. Rode antes o menu da Aldeia.");
            }
            else
            {
                WalkToPoint walker = MakeWalker(ummBody, ummZone);
                Transform home = Point(root, "Umm_Karim_Casa", SampleNavMesh(UmmKarimHomeSpot));
                home.rotation = Quaternion.Euler(0f, 40f, 0f);
                Transform dark = Point(root, "Umm_Karim_Quarto_Escuro", SampleNavMesh(UmmKarimDarkroomSpot));
                dark.rotation = Quaternion.Euler(0f, 250f, 0f);

                // Com os três rolos, a conversa da Umm Karim vira a que leva a Elena ao quarto escuro (uma vez).
                var starter = ummZone.GetComponent<DialogueStarter>();
                SetField(starter, "alternateDialogue", ummFilms);
                SetField(starter, "alternateItem", filmRoll);
                SetField(starter, "alternateItemCount", 3);
                SetField(starter, "alternateUnlessRaised", new[] { ev.led, ev.revealed });

                // Ela anda até o quarto escuro; voltando de um checkpoint, já está no lugar certo.
                Listener(root.gameObject, ev.led, e => Call(e, walker.WalkTo, dark));
                SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
                Listener(root.gameObject, ev.revealed, e => Call(e, walker.WalkTo, home));
                SetField(root.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

                // Ao voltar de um checkpoint: depois da revelação ela está em casa; antes, no quarto escuro. Uma só decisão,
                // para a ordem em que os componentes começam não importar.
                var whereToSnap = root.gameObject.AddComponent<StoryEventBranch>();
                SetField(whereToSnap, "storyEvent", ev.revealed);
                Call(GetField<UnityEngine.Events.UnityEvent>(whereToSnap, "ifRaised"), walker.SnapTo, home);
                Call(GetField<UnityEngine.Events.UnityEvent>(whereToSnap, "ifNotRaised"), walker.SnapTo, dark);
                Listener(root.gameObject, ev.led, e => Call(e, whereToSnap.Run));
                SetField(root.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            }

            // O quarto escuro (Abu Faris, luz vermelha, bacias) aparece quando a Umm Karim leva a Elena.
            Listener(root.gameObject, ev.led, e => SetActive(e, interior, true));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            AssetDatabase.SaveAssets();
            Debug.Log("O final de \"O que o Tom viu\" povoado.");
        }

        // ---------- Dados ----------

        static bool CreateData()
        {
            var revealed = Load<StoryEvent>("Events/RevelouFilmesDoTom");
            filmRoll = Load<ItemDefinition>("Items/RoloDeFilmeDoTom");
            if (revealed == null || filmRoll == null)
            {
                Debug.LogError("O final de \"O que o Tom viu\": faltam dados do Acampamento. Rode o menu dele antes.");
                return false;
            }

            ev = new Events
            {
                revealed = revealed,
                led = Event("UmmKarimLevouAoQuartoEscuro", "Com os três rolos na bolsa, a Umm Karim levou a Elena a Abu Faris, no quarto escuro (secundária O que o Tom viu)."),
                readPhotos = Event("LeuAsFotosDoTom", "A Elena abriu e leu as Fotos do Tom. A última foto mostra uma figura cercada de luz azul no meio do círculo do Poço; o Tom pode estar vivo."),
            };

            var document = Asset<DocumentDefinition>("Documents/Fotos_Do_Tom", d => d.raiseOnRead = ev.readPhotos);
            photos = Asset<ItemDefinition>("Items/FotosDoTom", item =>
            {
                item.id = "fotos_do_tom";
                item.category = ItemCategory.Chave;
                item.document = document;
            });

            ummFilms = Dialogue("Aldeia_UmmKarimFilmes", ev.led,
                N("f1", "Elena", "f2"),
                N("f2", "Umm Karim"));

            darkroom = Dialogue("Aldeia_QuartoEscuro", null,
                N("q1", "Umm Karim", "q2"),
                N("q2", "Abu Faris", "q3"),
                N("q3", "Elena", "q4"),
                N("q4", "Abu Faris", "q5"),
                N("q5", "Abu Faris", "q6"),
                N("q6", "Abu Faris", "q7"),
                N("q7", "Umm Karim", "q8"),
                N("q8", "Elena"));

            abuWaiting = Dialogue("Aldeia_AbuFarisEspera", null, N("a1", "Abu Faris"));
            abuAfter = Dialogue("Aldeia_AbuFarisDepois", null, N("a1", "Abu Faris"));
            return true;
        }

        // ---------- Cena ----------

        static void BuildHouse(Transform g)
        {
            const float height = 3f;
            // Aberta para o leste (para a rua da casa do altar); paredes de tijolo de barro, teto escuro.
            Prim(PrimitiveType.Cube, g, "Parede_Oeste", new Vector3(HouseCenter.x - 1.85f, height / 2f, HouseCenter.z), new Vector3(0.3f, height, 4f), "BO_TijoloBarro");
            Prim(PrimitiveType.Cube, g, "Parede_Norte", new Vector3(HouseCenter.x, height / 2f, HouseCenter.z + 1.85f), new Vector3(4f, height, 0.3f), "BO_TijoloBarro");
            Prim(PrimitiveType.Cube, g, "Parede_Sul", new Vector3(HouseCenter.x, height / 2f, HouseCenter.z - 1.85f), new Vector3(4f, height, 0.3f), "BO_TijoloBarro");
            Prim(PrimitiveType.Cube, g, "Teto", new Vector3(HouseCenter.x, height + 0.15f, HouseCenter.z), new Vector3(4.4f, 0.3f, 4.4f), "BO_TijoloBarroEscuro");
        }

        static void BuildInterior(Transform g)
        {
            // Mesa com as bacias (formas simples) e Abu Faris (cápsula com o nome, até haver modelo).
            Prim(PrimitiveType.Cube, g, "Mesa", new Vector3(HouseCenter.x - 1.0f, 0.45f, HouseCenter.z - 0.7f), new Vector3(0.8f, 0.9f, 1.8f), "BO_Madeira");
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Cylinder, g, $"Bacia_{i + 1}", new Vector3(HouseCenter.x - 1.0f, 0.93f, HouseCenter.z - 1.25f + i * 0.55f),
                    new Vector3(0.42f, 0.03f, 0.42f), "BO_Metal", collider: ColliderMode.None);
            Prim(PrimitiveType.Capsule, g, "Abu_Faris", AbuFarisSpot + Vector3.up * 0.8f, new Vector3(0.55f, 0.8f, 0.55f), "BO_Areia", 270f);

            // Luz vermelha de quarto escuro.
            var lightGo = new GameObject("Luz_Vermelha");
            lightGo.transform.SetParent(g, false);
            lightGo.transform.position = new Vector3(HouseCenter.x - 0.4f, 2.4f, HouseCenter.z);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.08f, 0.04f);
            light.intensity = 3f;
            light.range = 7f;
            light.shadows = LightShadows.None;

            // Falar com Abu Faris; com os três rolos aparece "Entregar os rolos" e a revelação acontece.
            GameObject talk = Zone(g, "Falar_AbuFaris", AbuFarisSpot + Vector3.up * 0.8f, new Vector3(1.4f, 1.6f, 1.4f));
            var trade = talk.AddComponent<ItemTrade>();
            SetField(trade, "tradePromptKey", "ui.interact.abu_faris_give_rolls");
            SetField(trade, "talkPromptKey", "ui.interact.abu_faris_talk");
            SetField(trade, "takeItem", filmRoll);
            SetField(trade, "takeCount", 3);
            SetField(trade, "giveItem", photos);
            SetField(trade, "raiseOnTrade", ev.revealed);
            SetField(trade, "dialogueWithGift", darkroom);
            SetField(trade, "dialogueWithoutGift", darkroom);
            SetField(trade, "waitingDialogue", abuWaiting);
            SetField(trade, "afterDialogue", abuAfter);
        }

        // Faz a Umm Karim andar: a cápsula e a zona de conversa vão para um objeto-raiz com NavMeshAgent no chão.
        static WalkToPoint MakeWalker(GameObject body, GameObject talkZone)
        {
            Transform existing = body.transform.parent != null && body.transform.parent.name == "Umm_Karim_Raiz" ? body.transform.parent : null;
            if (existing != null) return existing.GetComponent<WalkToPoint>();

            var rootGo = new GameObject("Umm_Karim_Raiz");
            rootGo.transform.SetParent(body.transform.parent, false);
            rootGo.transform.SetPositionAndRotation(SampleNavMesh(UmmKarimHomeSpot), Quaternion.Euler(0f, 40f, 0f));
            Undo.SetTransformParent(body.transform, rootGo.transform, "Umm Karim anda");
            Undo.SetTransformParent(talkZone.transform, rootGo.transform, "Umm Karim anda");
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localRotation = Quaternion.identity;
            talkZone.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            talkZone.transform.localRotation = Quaternion.identity;

            var agent = rootGo.AddComponent<NavMeshAgent>();
            agent.speed = 1.7f;
            agent.acceleration = 8f;
            agent.angularSpeed = 240f;
            agent.radius = 0.3f;
            agent.height = 1.6f;
            agent.stoppingDistance = 0.2f;
            var walker = rootGo.AddComponent<WalkToPoint>();
            SetField(walker, "waitIfPlayerFartherThan", 8f);
            SetField(walker, "facePlayerAfterArriving", true);
            return walker;
        }

        static Vector3 SampleNavMesh(Vector3 spot)
        {
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return hit.position;
            Debug.LogWarning($"O final de \"O que o Tom viu\": {spot} está fora da malha de navegação. Rode o menu do blockout.");
            return spot;
        }

        static GameObject Obj(string name) => Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(g => g.name == name && g.scene.IsValid() && g.scene.isLoaded && !EditorUtility.IsPersistent(g));
    }
}
