using System.Linq;
using Nibiru.AI;
using Nibiru.Combat;
using Nibiru.Dialogue;
using Nibiru.Inventory;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Passo 12 da missão "Sobrevivente", na antecâmara do Poço: a luta com o Primeiro Servo. Montado
    /// pelo menu da Boca do Poço (WellContent), dentro de "Poco_Ativo".
    ///
    /// Quando a Voz do flash acaba, o Selo estala e o Servo sai das rachaduras, junto à porta, e urra;
    /// o Lullu, se vivo, foge para o fundo do poço e some. Ponto de retorno na boca da antecâmara (ao
    /// voltar, o Servo já está acordado perto do Selo, sem repetir flash nem urro). Três fases pela vida,
    /// cada uma com urro, máscara rachando, olhos mais acesos, o Selo clareando e os holofotes piscando
    /// (FirstServantFight). Quatro holofotes em postes caem com um tiro e atraem o Servo com o estrondo.
    /// Vencido, ele tomba diante do Selo, o azul enfraquece e o objetivo aponta a parede do barco de
    /// junco (que abre no passo 13).
    /// </summary>
    static class FirstServantContent
    {
        // O Servo acorda junto ao Selo (porta em z = 101,8), a uns 9 m de onde a Elena para no flash.
        static readonly Vector3 ServantHome = new Vector3(45f, -14f, 99f);

        // O Lullu foge para o fundo do poço, longe da antecâmara, e some.
        static readonly Vector3 LulluFleePoint = new Vector3(44f, -14f, 73.5f);

        // Ponto de retorno da luta: na boca da antecâmara, de frente para o Selo.
        static readonly Vector3 FightCheckpointCenter = new Vector3(45f, -13f, 83f);
        static readonly Vector3 FightCheckpointSpawn = new Vector3(45f, -14f, 83f);

        // Depois da vitória: no meio da antecâmara, de frente para a parede do barco (x = 36, z 94–98).
        static readonly Vector3 AfterCheckpointCenter = new Vector3(42f, -13f, 93f);
        static readonly Vector3 AfterCheckpointSpawn = new Vector3(42f, -14f, 93f);
        static readonly Vector3 BoatWallZoneCenter = new Vector3(38f, -12.5f, 96f);
        static readonly Vector3 BoatWallZoneSize = new Vector3(3f, 3f, 4.5f);

        // Para onde os holofotes apontam: o meio da arena.
        static readonly Vector3 ArenaCenter = new Vector3(45f, -14f, 93f);

        class Events
        {
            public StoryEvent flashDoPoco, acordou, venceu, paredeDoBarco;
        }

        static Events ev;
        static DialogueDefinition wakeTalk, fallTalk;

        // lullus: os Lullu do Poço (o do fundo e a sentinela da antecâmara). Todos fogem quando o Servo acorda.
        public static void Build(Transform active, params LulluAI[] lullus)
        {
            CreateData();
            Transform g = Group(active, "Primeiro_Servo");

            FallingFloodlight[] floodlights = BuildFloodlights(Group(g, "Holofotes"));
            FirstServantFight fight = BuildServant(Group(g, "Servo"), floodlights);

            Transform fightGroup = Group(g, "Luta");
            var fightCheckpoint = BuildFightGroup(fightGroup);
            Transform afterGroup = Group(g, "Depois_Da_Luta");
            var afterCheckpoint = BuildAfterGroup(afterGroup);

            BuildWake(Group(g, "Despertar"), fight, lullus, fightGroup, fightCheckpoint);
            BuildVictory(Group(g, "Vitoria"), afterGroup, afterCheckpoint);

            // Os Lullu não voltam à luta: ao voltar de um checkpoint depois do despertar, nenhum está mais lá.
            Listener(lullus[0].transform.parent.gameObject, ev.acordou, e =>
            {
                foreach (LulluAI lullu in lullus) SetActive(e, lullu.gameObject, false);
            });
            SetField(lullus[0].transform.parent.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
        }

        static void CreateData()
        {
            ev = new Events
            {
                flashDoPoco = Load<StoryEvent>("Events/FlashDoPoco"),
                acordou = Event("PrimeiroServoAcordou", "O Primeiro Servo saiu das rachaduras do Selo e acordou, depois do flash (passo 12). O Lullu, se vivo, fugiu."),
                venceu = Event("VenceuOPrimeiroServo", "A Elena venceu o Primeiro Servo: ele tombou diante do Selo e o azul enfraqueceu. Fecha a luta do passo 12."),
                paredeDoBarco = Event("ChegouAParedeDoBarco", "Depois da luta, a Elena chegou à parede com o barco de junco, na antecâmara. Fecha o passo 12; a parede abre no passo 13."),
            };

            wakeTalk = Dialogue("Poco_OPrimeiroServo", null,
                N("e1", "Elena", "e2"),
                N("e2", "Elena"));

            fallTalk = Dialogue("Poco_OServoCai", null,
                N("e1", "Elena", "e2"),
                N("e2", "Elena"));

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest != null)
                AppendSteps(mainQuest,
                    Step("s26", ev.venceu),
                    Step("s27", ev.paredeDoBarco));

            AssetDatabase.SaveAssets();
        }

        // ---------- Holofotes ----------

        // Postes com holofote nos andaimes e num canto do chão. Um tiro (ou a faca) derruba;
        // caídos, continuam acesos no chão.
        static FallingFloodlight[] BuildFloodlights(Transform g)
        {
            return new[]
            {
                Floodlight(g, "Holofote_Plataforma_Oeste", new Vector3(41.8f, -11f, 88.8f), Vector3.right),
                Floodlight(g, "Holofote_Plataforma_Leste_Sul", new Vector3(52.8f, -10f, 93.2f), Vector3.left),
                Floodlight(g, "Holofote_Plataforma_Leste_Norte", new Vector3(52.8f, -10f, 100.4f), Vector3.left),
                Floodlight(g, "Holofote_Canto_Noroeste", new Vector3(38.6f, -14f, 100.4f), new Vector3(1f, 0f, -1f)),
            };
        }

        static FallingFloodlight Floodlight(Transform parent, string name, Vector3 basePosition, Vector3 fallDirection)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = basePosition;

            Transform pivot = new GameObject("Pivo").transform;
            pivot.SetParent(root.transform, false);
            Local(PrimitiveType.Cylinder, pivot, "Poste", new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 1.3f, 0.12f), "BO_Metal", ColliderMode.Solid);
            Local(PrimitiveType.Cube, pivot, "Pe", new Vector3(0f, 0.05f, 0f), new Vector3(0.8f, 0.1f, 0.8f), "BO_Metal");
            GameObject head = Local(PrimitiveType.Cube, pivot, "Holofote", new Vector3(0f, 2.65f, 0f), new Vector3(0.55f, 0.45f, 0.35f), "BO_BrancoFundacao", ColliderMode.Solid);
            head.transform.rotation = Quaternion.LookRotation(Flat(ArenaCenter - basePosition));

            var lampGo = new GameObject("Luz");
            lampGo.transform.SetParent(pivot, false);
            lampGo.transform.position = head.transform.position + head.transform.forward * 0.25f;
            lampGo.transform.rotation = Quaternion.LookRotation(ArenaCenter - lampGo.transform.position);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.color = new Color(0.92f, 0.96f, 1f);
            lamp.intensity = 6f;
            lamp.range = 24f;
            lamp.spotAngle = 80f;
            lamp.shadows = LightShadows.None;

            var health = root.AddComponent<Health>();
            SetField(health, "maxHealth", 1f);
            var floodlight = root.AddComponent<FallingFloodlight>();
            SetField(floodlight, "pivot", pivot);
            SetField(floodlight, "fallDirection", fallDirection);
            SetField(floodlight, "lamp", lamp);
            Audio(root, EditorTools.Audio.ProvisionalSounds.FloodlightCrash, loop: false, playOnAwake: false, volume: 1f, minDistance: 5f, maxDistance: 45f);
            SetField(floodlight, "crashSound", root.GetComponent<AudioSource>());
            return floodlight;
        }

        // ---------- O Servo ----------

        static FirstServantFight BuildServant(Transform g, FallingFloodlight[] floodlights)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrimeiroServoSetup.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("Primeiro Servo: falta o prefab. Rode Nibiru > Personagens > Montar Primeiro Servo animado.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
            go.name = "Primeiro_Servo";
            Vector3 home = NavMesh.SamplePosition(ServantHome, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : ServantHome;
            go.transform.SetPositionAndRotation(home, Quaternion.Euler(0f, 180f, 0f));

            var fight = go.GetComponent<FirstServantFight>();
            SetField(fight, "raiseOnDefeat", ev.venceu);
            var seal = GameObject.Find("Luz_Selo");
            SetField(fight, "sealLights", seal != null ? new[] { seal.GetComponent<Light>() } : new Light[0]);
            SetField(fight, "floodlights", floodlights);
            PrefabUtility.RecordPrefabInstancePropertyModifications(fight);

            // Só aparece quando acorda. Ao voltar de um checkpoint: acordado perto do Selo, ou já caído.
            go.SetActive(false);
            Listener(g.gameObject, ev.acordou, e =>
            {
                SetActive(e, go, true);
                Call(e, fight.WakeQuiet);
            });
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            Listener(g.gameObject, ev.venceu, e =>
            {
                SetActive(e, go, true);
                Call(e, fight.LieDefeated);
            });
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            return fight;
        }

        // ---------- Durante a luta ----------

        // Ponto de retorno da boca da antecâmara e uns poucos recursos nos andaimes.
        static Save.Checkpoint BuildFightGroup(Transform g)
        {
            Checkpoint(g, "Checkpoint_Luta", FightCheckpointCenter, new Vector3(3f, 3f, 1f), FightCheckpointSpawn, 0f);

            var ammo = Load<ItemDefinition>("Items/Municao9mm");
            var bandage = Load<ItemDefinition>("Items/Curativo");
            GameObject rounds = Prim(PrimitiveType.Cube, g, "Municao_Plataforma_Oeste", new Vector3(38.5f, -10.95f, 88.6f), new Vector3(0.16f, 0.1f, 0.1f), "BO_TurquesaFundacao", 30f);
            Pickup(rounds, ammo, 8, null);
            GameObject bandage1 = Prim(PrimitiveType.Cube, g, "Curativo_Plataforma_Leste", new Vector3(50.5f, -9.96f, 100.6f), new Vector3(0.18f, 0.08f, 0.12f), "BO_BrancoFundacao", -20f);
            Pickup(bandage1, bandage, 1, null);
            GameObject bandage2 = Prim(PrimitiveType.Cube, g, "Curativo_Boca_Antecamara", new Vector3(51f, -13.96f, 82.2f), new Vector3(0.18f, 0.08f, 0.12f), "BO_BrancoFundacao", 15f);
            Pickup(bandage2, bandage, 1, null);

            // Tudo isso só existe depois que o Servo acorda (o ponto de retorno não marca antes da hora).
            g.gameObject.SetActive(false);
            Listener(g.parent.gameObject, ev.acordou, e => SetActive(e, g.gameObject, true));
            return g.Find("Checkpoint_Luta").GetComponent<Save.Checkpoint>();
        }

        static Save.Checkpoint BuildAfterGroup(Transform g)
        {
            Checkpoint(g, "Checkpoint_Depois_Do_Servo", AfterCheckpointCenter, new Vector3(3f, 3f, 3f), AfterCheckpointSpawn, 270f);
            EventZone(g, "Gatilho_Parede_Do_Barco", BoatWallZoneCenter, BoatWallZoneSize, ev.paredeDoBarco);
            g.gameObject.SetActive(false);
            Listener(g.parent.gameObject, ev.venceu, e => SetActive(e, g.gameObject, true));
            return g.Find("Checkpoint_Depois_Do_Servo").GetComponent<Save.Checkpoint>();
        }

        // ---------- O despertar (ao vivo, depois do flash) ----------

        static void BuildWake(Transform g, FirstServantFight fight, LulluAI[] lullus, Transform fightGroup, Save.Checkpoint checkpoint)
        {
            var talk = DialogueTrigger(g, "Dialogo_Despertar", wakeTalk);
            var raiser = talk.gameObject.AddComponent<StoryEventRaiser>();
            SetField(raiser, "storyEvent", ev.acordou);
            Transform fleePoint = Point(g, "Fuga_Do_Lullu", LulluFleePoint);

            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetField(sequence, "skipIfRaised", ev.acordou);
            SetSteps(sequence,
                S("o Selo estala e se apaga", 0.4f, e => Call(e, raiser.Raise)),
                S("o Servo sai das rachaduras e urra", 0.3f, e =>
                {
                    SetActive(e, fight.gameObject, true);
                    Call(e, fight.Wake);
                }),
                S("os Lullu, se vivos, fogem e não voltam", 0.4f, e =>
                {
                    foreach (LulluAI lullu in lullus) Call(e, lullu.FleeTo, fleePoint);
                }),
                S("a Elena vê o Servo", 0.4f, e => Call(e, talk.StartDialogue)),
                S("ponto de retorno na boca da antecâmara", 0.1f, e => Call(e, checkpoint.ActivateNow)));
            Listener(talk.gameObject, ev.flashDoPoco, e => Call(e, sequence.Play));
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        // ---------- A vitória ----------

        static void BuildVictory(Transform g, Transform afterGroup, Save.Checkpoint checkpoint)
        {
            var talk = DialogueTrigger(g, "Dialogo_Servo_Cai", fallTalk);
            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetSteps(sequence,
                S("o Servo tomba e o azul enfraquece", 2f, e => Call(e, talk.StartDialogue)),
                S("ponto de retorno diante da parede do barco", 0.1f, e => Call(e, checkpoint.ActivateNow)));
            Listener(talk.gameObject, ev.venceu, e => Call(e, sequence.Play));
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
