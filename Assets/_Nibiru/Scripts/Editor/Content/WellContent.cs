using System.Linq;
using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Quests;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

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
            public StoryEvent saiuDaBase, chegouABoca, desceuOPoco, passouPeloLullu, chegouPertoDoSelo, flashDoPoco;
        }

        class Dialogues
        {
            public DialogueDefinition boca, flash;
        }

        static Events ev;
        static Dialogues dlg;

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
            LulluAI lullu = BuildLullu(Group(active, "Lullu"));
            BuildDescent(Group(active, "Descida"), lullu);
            BuildFlash(Group(active, "Flash_Do_Selo"), lullu);
            // Passo 12: o Primeiro Servo acorda quando a Voz do flash acaba.
            FirstServantContent.Build(active, lullu);

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
            };

            dlg = new Dialogues
            {
                boca = Dialogue("Poco_Boca", null,
                    N("e1", "Elena", "(O poço abre no chão como uma boca: andaimes de ferro, um guincho parado, um holofote da Fundação apontado para baixo. Do fundo sobe um ar frio e, de vez em quando, um raspar de pedra contra pedra.)", "e2"),
                    N("e2", "Elena", "Tem alguma coisa lá embaixo. E não é gente.", "e3"),
                    N("e3", "Elena", "O Samir disse que a passagem sai da antecâmara. Não existe outro caminho.")),

                flash = Dialogue("Poco_Flash", ev.flashDoPoco,
                    N("e1", "Elena", "(A tabuleta pulsa dentro da bolsa, quente como uma mão. No fundo da antecâmara, o azul nas rachaduras do Selo bate no mesmo ritmo.)", "e2"),
                    N("e2", "Elena", "(O ar clareia. O chão, as paredes e os andaimes somem, e só sobram luz e mãos. Muitas mãos, de todos os lados, segurando os braços dela.)", "v1"),
                    N("v1", "Voz", "Você já esteve aqui.", "v2"),
                    N("v2", "Voz", "Não tem problema não lembrar. Eu lembro por você.")),
            };

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("Siga o corredor nordeste até a boca do Poço", ev.chegouABoca),
                    Step("Desça o Poço pelos andaimes", ev.desceuOPoco),
                    Step("Atravesse o fundo do Poço sem acordar o Lullu, ou livre-se dele", ev.passouPeloLullu),
                    Step("Avance pela antecâmara até o Selo de Kur", ev.flashDoPoco));

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

        // ---------- A descida e a antecâmara ----------

        static void BuildDescent(Transform g, LulluAI lullu)
        {
            EventZone(g, "Gatilho_Pe_Da_Descida", FootZoneCenter, FootZoneSize, ev.desceuOPoco);
            EventZone(g, "Gatilho_Passou_Pelo_Lullu", PassedZoneCenter, PassedZoneSize, ev.passouPeloLullu);

            Checkpoint(g, "Checkpoint_Pe_Da_Descida", FootCheckpointCenter, FootCheckpointSize, FootCheckpointSpawn, 270f);
            Checkpoint(g, "Checkpoint_Antecamara", HallCheckpointCenter, HallCheckpointSize, HallCheckpointSpawn, 0f);
        }

        // ---------- O flash ----------

        static void BuildFlash(Transform g, LulluAI lullu)
        {
            EventZone(g, "Gatilho_Flash", FlashZoneCenter, FlashZoneSize, ev.chegouPertoDoSelo);

            var talk = DialogueTrigger(g, "Dialogo_Flash", dlg.flash);
            var flash = talk.gameObject.AddComponent<ScreenFlash>();
            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetField(sequence, "skipIfRaised", ev.flashDoPoco);

            // Nenhum inimigo age durante o flash: o Lullu, se ainda vivo, congela até a Voz falar.
            SetSteps(sequence,
                S("congela o Lullu", 0f, e => Call(e, lullu.Pause, true)),
                S("a luz do Selo toma a tela", 0f, e => Call(e, flash.FadeIn)),
                S("a Elena vê o flash e a Voz fala", 1.4f, e => Call(e, talk.StartDialogue)));
            Listener(talk.gameObject, ev.chegouPertoDoSelo, e => Call(e, sequence.Play));
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            // A Voz falou: a luz volta ao normal e o Lullu (se vivo) solta. O passo 11 acabou.
            Listener(talk.gameObject, ev.flashDoPoco, e =>
            {
                Call(e, flash.FadeOut);
                Call(e, lullu.Pause, false);
            });
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
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
