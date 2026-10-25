using Nibiru.AI;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Environment;
using Nibiru.Factions;
using Nibiru.Quests;
using Nibiru.Sequences;
using Nibiru.Tablets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Passo 14 da missão "Sobrevivente": o wadi. Tempestade de areia, o soldado ferido com o rádio, o
    /// Kast na frequência e a primeira escolha de facção ("O que fazer com a tabuleta").
    /// Menu: Nibiru > Conteúdo > Povoar Wadi na cena aberta. Os eventos, as conversas e os passos da
    /// missão são criados aqui (só se ainda não existirem: textos mudados no Inspector ficam).
    /// </summary>
    static class WadiContent
    {
        const string RootName = "Conteudo_Wadi";

        // O soldado ferido e o rádio caído, na base do trecho C, depois da curva (a Marca_Escolha_de_Faccao do blockout).
        static readonly Vector3 SoldierSpot = new Vector3(60f, 0f, 168f);
        static readonly Vector3 RadioSpot = new Vector3(58.9f, 0f, 168.5f);

        // Quem cruza esta faixa, na entrada do trecho C, chega ao soldado (começa o rádio).
        static readonly Vector3 ArrivalZoneCenter = new Vector3(60f, 1.5f, 162f);
        static readonly Vector3 ArrivalZoneSize = new Vector3(12f, 3f, 2f);

        // Ponto de retorno antes do rádio: no trecho B, a uns 8 m antes da curva, longe da patrulha.
        static readonly Vector3 PreRadioCheckpointCenter = new Vector3(53f, 1.5f, 157f);
        static readonly Vector3 PreRadioCheckpointSize = new Vector3(2f, 3f, 12f);
        static readonly Vector3 PreRadioCheckpointSpawn = new Vector3(51.5f, 0f, 157f);

        // Salvamento automático depois da escolha, junto do soldado.
        static readonly Vector3 PostChoiceCenter = new Vector3(60f, 1.5f, 171f);
        static readonly Vector3 PostChoiceSize = new Vector3(8f, 3f, 2f);
        static readonly Vector3 PostChoiceSpawn = new Vector3(60f, 0f, 172.5f);

        // Rota do líder de esquadra: vai e volta pela metade norte do trecho B, a oeste da curva.
        static readonly Vector3[] LeaderPatrol =
        {
            new Vector3(38f, 0f, 160.5f), new Vector3(49f, 0f, 160.5f),
        };

        internal class Events
        {
            public StoryEvent chegouAoSoldado, radioComecou, entregou, devolveu, ficou, escolheu;
        }

        internal static Events ev;
        internal static DialogueDefinition radioKast, radioKastDesconfia;
        internal static StormSettings stormSettings;

        [MenuItem("Nibiru/Conteúdo/Povoar Wadi na cena aberta")]
        static void Populate()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de povoar o Wadi.");
                return;
            }

            IdPrefix = "wadi";
            EditorTools.Audio.ProvisionalSounds.Generate(overwrite: false);
            CreateData();

            StoryEvent opened = Load<StoryEvent>("Events/AbriuAPassagem");
            if (opened == null)
            {
                Debug.LogError("Wadi: falta o evento AbriuAPassagem. Rode o menu da Boca do Poço antes.");
                return;
            }

            Transform root = NewRoot(RootName, "Povoar Wadi");
            Transform active = Group(root, "Wadi_Ativo");
            BuildStorm(Group(active, "Tempestade"));
            BuildRadio(Group(active, "Radio_Do_Soldado_Ferido"));
            BuildPatrol(Group(active, "Soldados"), opened);
            BuildAmbience(Group(active, "Ambiente"));
            BuildCheckpoints(Group(active, "Pontos_De_Retorno"));

            // O wadi só existe depois de a passagem abrir (o túnel e a rampa levam até ele).
            active.gameObject.SetActive(false);
            Listener(root.gameObject, opened, e => SetActive(e, active.gameObject, true));

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Wadi povoado.");
        }

        internal static void CreateData()
        {
            ev = new Events
            {
                chegouAoSoldado = Event("ChegouAoSoldadoFerido", "A Elena chegou ao soldado ferido no wadi, junto do rádio caído (passo 14)."),
                radioComecou = Event("RadioDoKastComecou", "O Kast começou a falar pela frequência do soldado ferido no wadi. Só começa com a fita de Irena nº 1 fora do ar."),
                entregou = Event("EntregouATabuleta", "Primeira escolha de facção: a Elena deixou a tabuleta no wadi para a Fundação. Fundação +15, Guardiões -15. O passo 15 e a região 2 leem isto."),
                devolveu = Event("DevolveuATabuleta", "Primeira escolha de facção: a Elena vai devolver a tabuleta ao Poço com o Samir. Guardiões +15, Fundação -15. O passo 15 e a região 2 leem isto."),
                ficou = Event("FicouComATabuleta", "Primeira escolha de facção: a Elena ficou com a tabuleta. Fundação -5, Guardiões -5, transformação +3 escondida. O passo 15 e a região 2 leem isto."),
                escolheu = Event("EscolheuONoWadi", "A Elena terminou a conversa do rádio e a primeira escolha de facção está feita (qualquer das três). Fecha o passo 14."),
            };

            if (!AssetDatabase.IsValidFolder($"{DataFolder}/Tempestade")) AssetDatabase.CreateFolder(DataFolder, "Tempestade");
            stormSettings = Asset<StormSettings>("Tempestade/TempestadeDoWadi", t => { });

            FactionDefinition foundation = Load<FactionDefinition>("Factions/FundacaoEnlil");
            FactionDefinition guardians = Load<FactionDefinition>("Factions/Guardioes");
            StoryEvent kastDistrusted = Load<StoryEvent>("Events/KastDesconfiou");

            radioKast = RadioDialogue("Wadi_RadioKast", "Doutora Varga. Sabia que a senhora atravessaria. Sua mãe também atravessava.", foundation, guardians);
            radioKastDesconfia = RadioDialogue("Wadi_RadioKast_Desconfia", "Doutora Varga. Eu disse que torcíamos pela senhora, e torcíamos. Agora a senhora está aí, e eu preciso ser franco.", foundation, guardians);
            if (kastDistrusted == null) Debug.LogWarning("Wadi: falta o evento KastDesconfiou. Rode o menu do Acampamento.");

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest != null)
                AppendSteps(mainQuest,
                    Step("Siga o wadi até o soldado ferido", ev.chegouAoSoldado),
                    Step("Ouça o rádio do soldado", ev.radioComecou),
                    Step("Decida o que fazer com a tabuleta", ev.escolheu));

            AssetDatabase.SaveAssets();
        }

        // O Kast fala pelo rádio do soldado; o Samir entra na mesma frequência (rádio de campo da caminhonete,
        // provisório) e a Voz sussurra. Ao fim, as três opções. Texto provisório.
        static DialogueDefinition RadioDialogue(string name, string opening, FactionDefinition foundation, FactionDefinition guardians) =>
            Dialogue(name, ev.escolheu,
                N("k1", "Kast (rádio)", opening, "k2"),
                N("k2", "Kast (rádio)", "Os moradores da Aldeia estão cercados. Todos: o rapaz doente, a senhora idosa, quem mais estiver nas casas. Meus homens esperam uma palavra minha.", "k3"),
                N("k3", "Kast (rádio)", "Deixe a tabuleta no chão do wadi, onde está, e siga seu caminho. Meus homens a buscam, soltam todos e saem da Aldeia. Ninguém mais se machuca.", "s1"),
                N("s1", "Samir (rádio)", "Doutora, não ouça. A tabuleta tem de voltar para o Poço, enterrada no nicho. Se ele a levar, não sobra ninguém para impedi-lo.", "v1"),
                N("v1", "Voz", "(baixinho, só para ela) Não abra mão de nada.", "escolha"),
                N("escolha", "Elena", "(O rádio chia. A tabuleta está morna na bolsa. O que fazer com ela?)", null,
                    Choice("Deixar a tabuleta no chão para o Kast.", "entrega", ev.entregou, (foundation, 15), (guardians, -15)),
                    Choice("Levar a tabuleta de volta ao Poço, com o Samir.", "devolve", ev.devolveu, (guardians, 15), (foundation, -15)),
                    Choice("Ficar com a tabuleta e não dizer nada.", "fica", ev.ficou, (foundation, -5), (guardians, -5))),
                N("entrega", "Kast (rádio)", "Sensata, doutora. Meus homens a buscam. Os soldados deixam a Aldeia ao amanhecer.", "entrega2"),
                N("entrega2", "Samir (rádio)", "Você deu uma chave para eles, doutora. Uma chave.", "fim"),
                N("devolve", "Kast (rádio)", "Lamentável.", "devolve2"),
                N("devolve2", "Samir (rádio)", "Venha. Eu espero na caminhonete.", "fim"),
                N("fica", "Voz", "Sábia.", "fim"),
                N("fim", "Elena", "(Ela desliga o rádio. O vento cobre o chiado.)"));

        // ---------- A tempestade ----------

        static void BuildStorm(Transform g)
        {
            var controller = g.gameObject.AddComponent<StormController>();
            SetField(controller, "settings", stormSettings);
            var windGo = new GameObject("Vento");
            windGo.transform.SetParent(g, false);
            var wind = windGo.AddComponent<AudioSource>();
            wind.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EditorTools.Audio.ProvisionalSounds.StormWind);
            wind.loop = true;
            wind.playOnAwake = false;
            wind.spatialBlend = 0f;
            wind.volume = 0f;
            SetField(controller, "wind", wind);
        }

        // ---------- O soldado ferido e o rádio ----------

        static void BuildRadio(Transform g)
        {
            Body(g, "Soldado_Ferido", SoldierSpot, 200f, "BO_VerdeOliva");

            // O rádio caído: chiado que se ouve de longe na tempestade e um LED que pisca (ponto de luz provisório).
            Transform radio = Point(g, "Radio_Caido", RadioSpot);
            Prim(PrimitiveType.Cube, radio, "Aparelho", RadioSpot + Vector3.up * 0.1f, new Vector3(0.22f, 0.2f, 0.1f), "BO_Metal", 25f, ColliderMode.None);
            Audio(radio.gameObject, EditorTools.Audio.ProvisionalSounds.RadioStatic, loop: true, playOnAwake: true, volume: 1f, minDistance: 3f, maxDistance: 60f);

            var ledGo = new GameObject("Led_Do_Radio");
            ledGo.transform.SetParent(radio, false);
            ledGo.transform.position = RadioSpot + Vector3.up * 0.35f;
            var led = ledGo.AddComponent<Light>();
            led.type = LightType.Point;
            led.color = new Color(1f, 0.2f, 0.12f);
            led.intensity = 2.5f;
            led.range = 7f;
            led.shadows = LightShadows.None;
            SetField(ledGo.AddComponent<BlinkingLight>(), "target", led);

            // O rádio do Kast: espera a fita de Irena nº 1 terminar, sem esperar para sempre.
            var radioTalk = g.gameObject.AddComponent<RadioAfterTape>();
            SetField(radioTalk, "dialogue", radioKast);
            SetField(radioTalk, "alternateDialogue", radioKastDesconfia);
            SetField(radioTalk, "alternateWhenRaised", Load<StoryEvent>("Events/KastDesconfiou"));
            SetField(radioTalk, "tape", Load<TapeDefinition>("Tapes/Fita_Irena_1"));
            SetField(radioTalk, "raiseOnStart", ev.radioComecou);

            EventZone(g, "Chegou_Ao_Soldado", ArrivalZoneCenter, ArrivalZoneSize, ev.chegouAoSoldado);
            Listener(g.gameObject, ev.chegouAoSoldado, e => Call(e, radioTalk.Begin));

            // Depois da escolha o rádio se cala e o LED apaga (também ao voltar de um salvamento).
            Listener(g.gameObject, ev.escolheu, e =>
            {
                SetEnabled(e, radio.GetComponent<AudioSource>(), false);
                SetActive(e, ledGo, false);
            });

            // "Ficar com a tabuleta": a transformação sobe 3 sem aviso nenhum (só a fala da Voz, "Sábia", dá a pista).
            var silent = g.gameObject.AddComponent<SilentTransformationGain>();
            Listener(g.gameObject, ev.ficou, e => Call(e, silent.Apply));
            StoryEventListener[] listeners = g.GetComponents<StoryEventListener>();
            SetField(listeners[listeners.Length - 1], "replayOnLoad", false);
        }

        // ---------- O líder de esquadra ----------

        static void BuildPatrol(Transform g, StoryEvent opened)
        {
            Transform route = Group(g, "Rota_Do_Lider");
            var points = new Transform[LeaderPatrol.Length];
            for (int i = 0; i < points.Length; i++) points[i] = Point(route, $"Ponto_{i + 1}", LeaderPatrol[i]);

            Transform soldiers = Group(g, "Lider");
            Soldier(soldiers, "Lider_Do_Wadi", LeaderPatrol[0], LeaderPatrol[1], points, leader: true);

            // Liga a IA quando o wadi acorda (o Start refaz a reação, pois a passagem já abriu).
            Listener(g.gameObject, opened, e => WakeSoldiers(e, soldiers));
        }

        // ---------- Faróis e silhuetas que aparecem e somem ----------

        static void BuildAmbience(Transform g)
        {
            // Um veículo da Fundação passa devagar sobre a margem norte do trecho B: só faróis e vulto na névoa.
            var vehicle = new GameObject("Veiculo_Distante");
            vehicle.transform.SetParent(g, false);
            Local(PrimitiveType.Cube, vehicle.transform, "Carroceria", new Vector3(0f, 0.9f, 0f), new Vector3(2.2f, 1.5f, 4.8f), "BO_Metal", ColliderMode.None);
            foreach (float side in new[] { -0.8f, 0.8f }) Headlight(vehicle.transform, side);
            MoveAlong(g, vehicle, "Percurso_Veiculo", 4f, new[] { new Vector3(14f, 5f, 167f), new Vector3(58f, 5f, 167f) }, 14f, 28f, 10f);

            // Três vultos de soldados caminham pela margem leste do trecho C.
            for (int i = 0; i < 3; i++)
            {
                var silhouette = new GameObject($"Vulto_{i + 1}");
                silhouette.transform.SetParent(g, false);
                Local(PrimitiveType.Capsule, silhouette.transform, "Corpo", new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.9f, 0.55f), "BO_Metal", ColliderMode.None);
                float z0 = 182f - i * 5f;
                MoveAlong(g, silhouette, $"Percurso_Vulto_{i + 1}", 1.4f, new[] { new Vector3(69f, 4f, z0), new Vector3(69f, 4f, z0 - 22f) }, 9f, 18f, 3f + i * 5f);
            }
        }

        static void Headlight(Transform vehicle, float side)
        {
            var go = new GameObject("Farol");
            go.transform.SetParent(vehicle, false);
            go.transform.localPosition = new Vector3(side, 0.8f, 2.4f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 55f;
            light.range = 22f;
            light.intensity = 6f;
            light.color = new Color(1f, 0.93f, 0.75f);
            light.shadows = LightShadows.None;
        }

        // Anda por um caminho de ida; o LoopingPasser liga o objeto, faz a passada e desliga, de tempos em tempos.
        static void MoveAlong(Transform parent, GameObject mover, string pathName, float speed, Vector3[] route, float minWait, float maxWait, float firstDelay)
        {
            Transform path = Group(parent, pathName);
            var points = new Transform[route.Length];
            for (int i = 0; i < route.Length; i++) points[i] = Point(path, $"Ponto_{i + 1}", route[i]);
            var move = mover.AddComponent<MoveAlongPath>();
            SetField(move, "points", points);
            SetField(move, "speed", speed);
            SetField(move, "slowDownDistance", 0f);
            var passer = path.gameObject.AddComponent<LoopingPasser>();
            SetField(passer, "mover", move);
            SetField(passer, "minSecondsBetween", minWait);
            SetField(passer, "maxSecondsBetween", maxWait);
            SetField(passer, "firstDelay", firstDelay);
        }

        // ---------- Pontos de retorno ----------

        static void BuildCheckpoints(Transform g)
        {
            // Antes do rádio: ao voltar dele, o rádio recomeça (a espera da fita se resolve sozinha).
            Checkpoint(g, "Checkpoint_Antes_Do_Radio", PreRadioCheckpointCenter, PreRadioCheckpointSize, PreRadioCheckpointSpawn, 90f);

            // Depois da escolha: salvamento automático junto do soldado. A área só liga com a escolha feita,
            // para a Elena não salvar ali antes da conversa.
            Checkpoint(g, "Autosave_Depois_Da_Escolha", PostChoiceCenter, PostChoiceSize, PostChoiceSpawn, 0f);
            Transform zone = g.Find("Autosave_Depois_Da_Escolha");
            var checkpoint = zone.GetComponent<Save.Checkpoint>();
            zone.gameObject.SetActive(false);

            var sequence = g.gameObject.AddComponent<TimedSequence>();
            SetSteps(sequence,
                S("liga a área do salvamento", 0.2f, e => SetActive(e, zone.gameObject, true)),
                S("salvamento automático depois da escolha", 0.1f, e => Call(e, checkpoint.ActivateNow)));
            Listener(g.gameObject, ev.escolheu, e => Call(e, sequence.Play));
            SetField(g.GetComponents<StoryEventListener>()[0], "replayOnLoad", false);
        }

        static DialogueChoice Choice(string text, string next, StoryEvent raise, params (FactionDefinition faction, int amount)[] changes)
        {
            var choice = C(text, next, raise);
            choice.reputation = System.Array.ConvertAll(changes, c => new ReputationChange { faction = c.faction, amount = c.amount });
            return choice;
        }
    }
}
