using System.Linq;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Documents;
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
    /// Povoa a Boca do Poço (área 5 do Sítio de Ur). Passo 11 da missão "Sobrevivente": a Elena sai
    /// da base pelo nordeste, chega à boca do poço, desce 14 m pelos andaimes ouvindo o Lullu raspar
    /// a pedra lá embaixo, passa por ele (agachada, ou livrando-se dele) até a antecâmara e, a uns
    /// 12 m do Selo de Kur, tem o flash da noite que esqueceu: só luz e mãos, e a Voz.
    ///
    /// O Lullu fica preso ao chão do fundo do poço e da antecâmara (não sobe os andaimes). A ordem é
    /// descida, Lullu, ponto de retorno, flash. Durante o flash não há inimigo agindo: o Lullu, se
    /// ainda vivo, fica congelado até a Voz falar. O passo 11 acaba aí; o 12 (o Primeiro Servo,
    /// montado por FirstServantContent) começa daí.
    ///
    /// Os arquivos de
    /// dados só são criados se ainda não existirem. Os objetos ficam em "Conteudo_Poco" e são
    /// reconstruídos a cada execução. Rode depois da Base do Zigurate.
    /// Menu: Nibiru > Conteúdo > Povoar Boca do Poço na cena aberta.
    /// </summary>
    static class WellContent
    {
        const string RootName = "Conteudo_Poco";
        const string LulluPrefabPath = "Assets/_Nibiru/Prefabs/AI/Lullu.prefab";

        // Boca do poço: o buraco é x 40–50, z 70–80. Antes dele, o corredor entre o zigurate e a cerca.
        static readonly Vector3 MouthZoneCenter = new Vector3(50f, 1.5f, 60f);
        static readonly Vector3 MouthZoneSize = new Vector3(20f, 3f, 4f);

        // Pé da descida: o chão do fundo do poço, onde acaba o último lance.
        static readonly Vector3 FootZoneCenter = new Vector3(44f, -13f, 73f);
        static readonly Vector3 FootZoneSize = new Vector3(10f, 3f, 5f);

        // O Lullu vagueia na boca da antecâmara, entre o fundo do poço e o começo dela.
        static readonly Vector3 LulluHome = new Vector3(44f, -14f, 78f);

        // A sentinela (2º Lullu): guarda o lado leste da antecâmara, entre o ponto de retorno (x = 45) e a
        // inscrição. Vagueia pouco e ouve mais que o do fundo (dados em Data/AI/Lullu_Sentinela).
        static readonly Vector3 SentinelHome = new Vector3(52f, -14f, 87f);

        // A inscrição dos Guardiões do Poço: na parede leste da antecâmara, a uns 4 m da sentinela.
        static readonly Vector3 InscriptionWall = new Vector3(53.95f, -12.6f, 83.9f);
        static readonly Vector3 InscriptionLamp = new Vector3(53.2f, -14f, 82.9f);
        static readonly Vector3 InscriptionZoneCenter = new Vector3(52.6f, -13f, 83.9f);
        static readonly Vector3 InscriptionZoneSize = new Vector3(2f, 2.2f, 2.4f);

        // Ponto de retorno do pé da descida: no último patamar de andaime, longe do Lullu e em silêncio.
        static readonly Vector3 FootCheckpointCenter = new Vector3(49f, -9f, 73.5f);
        static readonly Vector3 FootCheckpointSize = new Vector3(3f, 5f, 5f);
        static readonly Vector3 FootCheckpointSpawn = new Vector3(49f, -10.5f, 71f);

        // Passou do Lullu: já dentro da antecâmara (x 36–54), uns 5 m depois da boca.
        static readonly Vector3 PassedZoneCenter = new Vector3(45f, -12.5f, 84.5f);
        static readonly Vector3 PassedZoneSize = new Vector3(18f, 4f, 1f);

        // Ponto de retorno depois do Lullu, antes do flash.
        static readonly Vector3 HallCheckpointCenter = new Vector3(45f, -13f, 87.5f);
        static readonly Vector3 HallCheckpointSize = new Vector3(14f, 3f, 2.5f);
        static readonly Vector3 HallCheckpointSpawn = new Vector3(45f, -14f, 87.5f);

        // O flash: a uns 12 m da porta com o Selo (z = 101,8).
        static readonly Vector3 FlashZoneCenter = new Vector3(45f, -12.5f, 90.5f);
        static readonly Vector3 FlashZoneSize = new Vector3(18f, 4f, 1.2f);

        class Events
        {
            public StoryEvent saiuDaBase, chegouABoca, desceuOPoco, passouPeloLullu, chegouPertoDoSelo, flashDoPoco, leuAInscricao;
        }

        class Dialogues
        {
            public DialogueDefinition boca, flash;
        }

        static Events ev;
        static Dialogues dlg;
        static DocumentDefinition inscription;
        static LulluSettings sentinelSettings;

        [MenuItem("Nibiru/Conteúdo/Povoar Boca do Poço na cena aberta")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar a Boca do Poço.");
                return;
            }

            IdPrefix = "poco";
            EditorTools.Audio.ProvisionalSounds.Generate(overwrite: false);
            CreateData();

            Transform root = NewRoot(RootName, "Povoar Boca do Poço");
            Transform active = Group(root, "Poco_Ativo");
            BuildAmbience(Group(active, "Ambiente"));
            BuildMouth(Group(active, "Boca"));
            Transform lulluGroup = Group(active, "Lullu");
            LulluAI lullu = BuildLullu(lulluGroup);
            LulluAI sentinel = BuildSentinel(lulluGroup);
            BuildDescent(Group(active, "Descida"), lullu);
            BuildFlash(Group(active, "Flash_Do_Selo"), lullu, sentinel);
            BuildInscription(Group(active, "Inscricao_Do_Poco"));
            // Passo 12: o Primeiro Servo acorda quando a Voz do flash acaba.
            FirstServantContent.Build(active, lullu, sentinel);
            // Passo 13: a passagem dos Guardiões atrás da parede do barco de junco.
            GuardianPassageContent.Build(active);

            // Tudo do Poço só existe depois de a Elena sair da base pelo nordeste (fim do passo 10).
            active.gameObject.SetActive(false);
            Listener(root.gameObject, ev.saiuDaBase, e => SetActive(e, active.gameObject, true));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Boca do Poço povoada.");
        }

        static void CreateData()
        {
            ev = new Events
            {
                saiuDaBase = Event("SaiuDaBaseRumoAoPoco", "A Elena saiu da base pelo nordeste, rumo à boca do Poço. Fecha o passo 10."),
                chegouABoca = Event("ChegouABocaDoPoco", "A Elena chegou à boca do Poço, no alto dos andaimes (passo 11)."),
                desceuOPoco = Event("DesceuOPoco", "A Elena desceu os 14 m de andaimes e pisou no fundo do Poço, onde o Lullu vagueia."),
                passouPeloLullu = Event("PassouPeloLullu", "A Elena atravessou a boca da antecâmara, passando pelo Lullu (agachada ou livrando-se dele)."),
                chegouPertoDoSelo = Event("ChegouPertoDoSelo", "A Elena chegou a uns 12 m do Selo de Kur, na antecâmara. Começa o flash."),
                flashDoPoco = Event("FlashDoPoco", "O flash da noite esquecida acabou e a Voz falou. Fecha o passo 11; o 12 (o Primeiro Servo) começa daí."),
                leuAInscricao = Event("LeuAInscricaoDoPoco", "A Elena leu a inscrição dos Guardiões na parede leste da antecâmara do Poço (colecionável, opcional)."),
            };

            if (!AssetDatabase.IsValidFolder(DataFolder + "/Documents")) AssetDatabase.CreateFolder(DataFolder, "Documents");
            inscription = Asset<DocumentDefinition>("Documents/Poco_Inscricao", d => d.raiseOnRead = ev.leuAInscricao);

            // A sentinela é um Lullu com os mesmos números do primeiro, mas que fica mais parado e ouve mais.
            sentinelSettings = Asset<LulluSettings>("AI/Lullu_Sentinela", s =>
            {
                var basic = Load<LulluSettings>("AI/Lullu");
                if (basic != null) EditorUtility.CopySerialized(basic, s);
                s.wanderRadius = 1.5f;
                s.hearingMultiplier = (basic != null ? basic.hearingMultiplier : 1.2f) * 1.4f;
            });

            dlg = new Dialogues
            {
                boca = Dialogue("Poco_Boca", null,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "e3"),
                    N("e3", "Elena")),

                flash = Dialogue("Poco_Flash", ev.flashDoPoco,
                    N("e1", "Elena", "e2"),
                    N("e2", "Elena", "v1"),
                    N("v1", "Voz", "v2"),
                    N("v2", "Voz")),
            };

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("s22", ev.chegouABoca),
                    Step("s23", ev.desceuOPoco),
                    Step("s24", ev.passouPeloLullu),
                    Step("s25", ev.flashDoPoco));

            AssetDatabase.SaveAssets();
        }

        // ---------- Som do Poço ----------

        // Ronco grave e pedras caindo, ouvidos já da boca do poço.
        static void BuildAmbience(Transform g)
        {
            GameObject go = Point(g, "Ambiente_Do_Poco", new Vector3(45f, -8f, 76f)).gameObject;
            Audio(go, EditorTools.Audio.ProvisionalSounds.WellAmbience, loop: true, playOnAwake: true, volume: 0.7f, minDistance: 8f, maxDistance: 50f);
        }

        // ---------- A boca do poço ----------

        static void BuildMouth(Transform g)
        {
            EventZone(g, "Gatilho_Boca_Do_Poco", MouthZoneCenter, MouthZoneSize, ev.chegouABoca);

            // Ao chegar (só ao vivo): a Elena vê o poço e ouve o raspar; depois, a dica do som.
            var talk = DialogueTrigger(g, "Dialogo_Boca", dlg.boca);
            var poster = talk.gameObject.AddComponent<NotificationPoster>();
            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetField(sequence, "skipIfRaised", ev.desceuOPoco);
            SetSteps(sequence,
                S("a Elena vê o poço e ouve o raspar", 0.5f, e => Call(e, talk.StartDialogue)),
                S("dica do som", 0.6f, e => Notify(e, poster, "Dica: o Lullu quase não enxerga, mas ouve tudo. Agachada se faz pouco barulho; correr se ouve de longe")));
            Listener(talk.gameObject, ev.chegouABoca, e => Call(e, sequence.Play));
            SetField(talk.GetComponent<StoryEventListener>(), "replayOnLoad", false);
        }

        // ---------- O Lullu ----------

        // O Lullu do prefab (cápsula provisória), preso ao chão: não sobe andaime nem rampa.
        static LulluAI BuildLullu(Transform g)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LulluPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.name = "Lullu_Poco";
            Vector3 home = OnNavMesh(LulluHome, "Lullu");
            go.transform.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.right));

            var ai = go.GetComponent<LulluAI>();
            SetField(ai, "maxClimb", 2f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);

            // Respiração que não é de gente e o raspar de unhas na pedra, ouvidos da descida.
            Audio(go, EditorTools.Audio.ProvisionalSounds.LulluBreathing, loop: true, playOnAwake: true, volume: 0.9f, minDistance: 3f, maxDistance: 30f);

            // Já passou por ele e voltou de um ponto de retorno: o encontro acabou, ele não volta.
            Listener(g.gameObject, ev.passouPeloLullu, e => SetActive(e, go, false));
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            return ai;
        }

        // O 2º Lullu: a sentinela da antecâmara. Mesmo prefab, mas com os dados Lullu_Sentinela (vagueia
        // 1,5 m e ouve 40% mais longe que o do fundo). Tem de ser contornado ou enfrentado antes do flash.
        // O Lullu do fundo some ao passar pela boca da antecâmara; a sentinela fica até o flash e foge com ele.
        static LulluAI BuildSentinel(Transform g)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LulluPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.name = "Lullu_Sentinela";
            Vector3 home = OnNavMesh(SentinelHome, "da sentinela");
            go.transform.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.left));

            var ai = go.GetComponent<LulluAI>();
            SetField(ai, "settings", sentinelSettings);
            SetField(ai, "maxClimb", 2f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);

            Audio(go, EditorTools.Audio.ProvisionalSounds.LulluBreathing, loop: true, playOnAwake: true, volume: 0.8f, minDistance: 3f, maxDistance: 22f);
            StableId(go);
            return ai;
        }

        // ---------- A descida e a antecâmara ----------

        static void BuildDescent(Transform g, LulluAI lullu)
        {
            EventZone(g, "Gatilho_Pe_Da_Descida", FootZoneCenter, FootZoneSize, ev.desceuOPoco);
            EventZone(g, "Gatilho_Passou_Pelo_Lullu", PassedZoneCenter, PassedZoneSize, ev.passouPeloLullu);

            Checkpoint(g, "Checkpoint_Pe_Da_Descida", FootCheckpointCenter, FootCheckpointSize, FootCheckpointSpawn, 270f);
            Checkpoint(g, "Checkpoint_Antecamara", HallCheckpointCenter, HallCheckpointSize, HallCheckpointSpawn, 0f);
        }

        // ---------- O flash ----------

        static void BuildFlash(Transform g, LulluAI lullu, LulluAI sentinel)
        {
            EventZone(g, "Gatilho_Flash", FlashZoneCenter, FlashZoneSize, ev.chegouPertoDoSelo);

            var talk = DialogueTrigger(g, "Dialogo_Flash", dlg.flash);
            var flash = talk.gameObject.AddComponent<ScreenFlash>();
            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetField(sequence, "skipIfRaised", ev.flashDoPoco);

            // Nenhum inimigo age durante o flash: os Lullu, se ainda vivos, congelam até a Voz falar.
            SetSteps(sequence,
                S("congela os Lullu", 0f, e =>
                {
                    Call(e, lullu.Pause, true);
                    Call(e, sentinel.Pause, true);
                }),
                S("a luz do Selo toma a tela", 0f, e => Call(e, flash.FadeIn)),
                S("a Elena vê o flash e a Voz fala", 1.4f, e => Call(e, talk.StartDialogue)));
            Listener(talk.gameObject, ev.chegouPertoDoSelo, e => Call(e, sequence.Play));
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            // A Voz falou: a luz volta ao normal e o Lullu (se vivo) solta. O passo 11 acabou.
            Listener(talk.gameObject, ev.flashDoPoco, e =>
            {
                Call(e, flash.FadeOut);
                Call(e, lullu.Pause, false);
                Call(e, sentinel.Pause, false);
            });
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        // ---------- A inscrição dos Guardiões ----------

        // Colecionável opcional: o barco de junco e uma faixa de glifos em ouro na parede leste, com a
        // lamparina de barro (luz quente, nunca azul). Ler abre o documento na tela de leitura.
        static void BuildInscription(Transform g)
        {
            Prim(PrimitiveType.Cube, g, "Pedra_Da_Inscricao", InscriptionWall, new Vector3(0.1f, 1.5f, 2.2f), "BO_Rocha", collider: ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Barco_Casco", InscriptionWall + new Vector3(-0.07f, -0.45f, 0f), new Vector3(0.08f, 0.2f, 1.2f), "BO_Ouro", collider: ColliderMode.None);
            Prim(PrimitiveType.Cube, g, "Barco_Mastro", InscriptionWall + new Vector3(-0.07f, -0.1f, 0f), new Vector3(0.08f, 0.55f, 0.07f), "BO_Ouro", collider: ColliderMode.None);
            float[] heights = { 0.2f, 0.14f, 0.22f, 0.16f, 0.2f, 0.12f, 0.18f };
            for (int i = 0; i < heights.Length; i++)
                Prim(PrimitiveType.Cube, g, $"Glifo_{i + 1}", InscriptionWall + new Vector3(-0.07f, 0.45f, -0.9f + i * 0.3f), new Vector3(0.07f, heights[i], 0.1f), "BO_Ouro", collider: ColliderMode.None);

            // Lamparina de barro com a chama acesa, marca dos altares dos Guardiões.
            Prim(PrimitiveType.Cylinder, g, "Lamparina_Barro", InscriptionLamp + new Vector3(0f, 0.05f, 0f), new Vector3(0.22f, 0.06f, 0.22f), "BO_TijoloBarro", collider: ColliderMode.None);
            Prim(PrimitiveType.Sphere, g, "Lamparina_Chama", InscriptionLamp + new Vector3(0f, 0.21f, 0f), new Vector3(0.08f, 0.16f, 0.08f), "BO_Chama", collider: ColliderMode.None);
            var lightGo = new GameObject("Lamparina_Luz");
            lightGo.transform.SetParent(g, false);
            lightGo.transform.position = InscriptionLamp + new Vector3(-0.2f, 0.5f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.45f);
            light.intensity = 1.2f;
            light.range = 4.5f;
            light.shadows = LightShadows.None;

            GameObject zone = Zone(g, "Ler_Inscricao", InscriptionZoneCenter, InscriptionZoneSize);
            var view = zone.AddComponent<DocumentView>();
            SetField(view, "document", inscription);
            var interact = zone.AddComponent<Interaction.InteractableEvent>();
            SetField(interact, "prompt", "Ler a inscrição");
            SetField(interact, "promptKey", "ui.interact.poco_ler_inscricao");
            Call(GetField<UnityEvent>(interact, "onInteract"), view.Show);
        }

        // ---------- Ajudas ----------

        // Leva o ponto para a malha de navegação (avisa no Console se estiver fora dela).
        static Vector3 OnNavMesh(Vector3 wanted, string label)
        {
            if (NavMesh.SamplePosition(wanted, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return hit.position;
            Debug.LogWarning($"Poço: o ponto {label} {wanted} está fora da malha de navegação. Mude a posição no script.");
            return wanted;
        }
    }
}
