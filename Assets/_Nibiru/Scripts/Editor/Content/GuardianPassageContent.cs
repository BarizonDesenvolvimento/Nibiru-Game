using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Documents;
using Nibiru.Environment;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEngine;

namespace Nibiru.EditorTools.Content
{
    using static ContentKit;

    /// <summary>
    /// Passo 13 da missão "Sobrevivente": a passagem dos Guardiões atrás da parede do barco de junco.
    /// Montado pelo menu da Boca do Poço (WellContent), dentro de "Poco_Ativo/Passagem_Dos_Guardioes".
    ///
    /// Na parede, sob o barco, há uma inscrição: a Elena a lê em voz alta, a tabuleta pulsa numa luz
    /// azul e a parede afunda no chão (GuardianPassage). Um ponto de retorno marca a abertura. O túnel
    /// tem lamparinas acesas (alguém cuida da passagem), a fita de Irena nº 1 (pega e toca enquanto a
    /// Elena anda, sem travar o controle) e, no topo da rampa que sai para o wadi, um salvamento
    /// automático. Os valores ajustáveis ficam em Data/Passagem/PassagemDosGuardioes e a fita em
    /// Data/Tapes/Fita_Irena_1.
    /// </summary>
    static class GuardianPassageContent
    {
        // Quem lê fica de frente para a parede, no meio da antecâmara.
        static readonly Vector3 ReadZoneCenter = new Vector3(36.7f, -12.8f, 96f);
        static readonly Vector3 ReadZoneSize = new Vector3(1.4f, 2.4f, 3.6f);

        // Depois de abrir: ponto de retorno diante do vão, de frente para a passagem (oeste).
        static readonly Vector3 OpenCheckpointCenter = new Vector3(33.2f, -13f, 96f);
        static readonly Vector3 OpenCheckpointSize = new Vector3(2f, 3f, 3f);
        static readonly Vector3 OpenCheckpointSpawn = new Vector3(38f, -14f, 96f);

        // As lamparinas: uma na entrada e três ao longo do túnel (posição e para que lado a pedra está).
        static readonly (Vector3 spot, Vector3 wallSide)[] Lamps =
        {
            (new Vector3(33f, -12.4f, 94.3f), Vector3.back),
            (new Vector3(30.3f, -12.4f, 100.5f), Vector3.left),
            (new Vector3(33.7f, -12.4f, 104.5f), Vector3.right),
            (new Vector3(30.3f, -12.4f, 108f), Vector3.left),
        };

        // A fita de Irena: num degrau de pedra no pé da parede oeste, um pouco antes do fim do túnel reto.
        static readonly Vector3 TapeSpot = new Vector3(30.6f, -13.8f, 106.6f);

        // Topo da rampa: a saída para o wadi (a rampa sobe de z = 110 a z = 140).
        static readonly Vector3 ExitZoneCenter = new Vector3(32f, 0.5f, 139f);
        static readonly Vector3 ExitZoneSize = new Vector3(3.6f, 3f, 1.2f);
        static readonly Vector3 ExitCheckpointSpawn = new Vector3(32f, 0f, 141f);

        class Events
        {
            public StoryEvent leuInscricao, abriu, coletouFita, fitaTerminou, saiu;
        }

        static Events ev;
        static DialogueDefinition inscriptionTalk, openTalk;
        static TapeDefinition tape;
        static GuardianPassageSettings settings;

        public static void Build(Transform active)
        {
            CreateData();
            Transform g = Group(active, "Passagem_Dos_Guardioes");

            GameObject wall = GameObject.Find("Parede_Movel");
            if (wall == null)
            {
                Debug.LogError("Passagem dos Guardiões: falta a Parede_Movel. Rode Nibiru > Blockout > Construir Sítio de Ur na cena aberta.");
                return;
            }

            GuardianPassage passage = BuildPassage(Group(g, "Parede_E_Luz"), wall);
            BuildInscription(Group(g, "Inscricao"), passage);
            BuildTunnel(Group(g, "Tunel"), passage);
            BuildTape(Group(g, "Fita"));
            BuildExit(Group(g, "Saida"));
            BuildAfterOpening(Group(g, "Depois_De_Abrir"), passage);
        }

        static void CreateData()
        {
            ev = new Events
            {
                leuInscricao = Event("LeuAInscricaoDaPassagem", "A Elena leu em voz alta a inscrição dos Guardiões na parede do barco de junco (passo 13). A tabuleta pulsa e a parede abre."),
                abriu = Event("AbriuAPassagem", "A parede do barco de junco terminou de afundar no chão: a passagem dos Guardiões está aberta (passo 13)."),
                coletouFita = Event("ColetouAFitaDeIrena1", "A Elena pegou a fita de Irena nº 1 na passagem dos Guardiões. Registro da coleta, para a futura tela de documentos e fitas."),
                fitaTerminou = Event("FitaDeIrena1Terminou", "A fita de Irena nº 1 terminou de tocar. O rádio do Kast no wadi (passo 14) espera por este evento."),
                saiu = Event("SaiuDaPassagem", "A Elena chegou ao topo da rampa da passagem dos Guardiões, na saída para o wadi. Fecha o passo 13; o 14 começa daí."),
            };

            inscriptionTalk = Dialogue("Passagem_Inscricao", ev.leuInscricao,
                N("e1", "Elena", "e2"),
                N("e2", "Elena", "e3"),
                N("e3", "Elena"));

            openTalk = Dialogue("Passagem_Aberta", null,
                N("e1", "Elena", "e2"),
                N("e2", "Elena"));

            EnsureFolder("Passagem");
            EnsureFolder("Tapes");
            settings = Asset<GuardianPassageSettings>("Passagem/PassagemDosGuardioes", s => { });
            tape = Asset<TapeDefinition>("Tapes/Fita_Irena_1", t =>
            {
                t.lines = new[] { "l1", "l2", "l3", "l4", "l5", "l6", "l7", "l8" }.Select(id => new TapeDefinition.Line { id = id }).ToArray();
                t.raiseOnCollected = ev.coletouFita;
                t.raiseOnFinished = ev.fitaTerminou;
            });

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest != null)
                AppendSteps(mainQuest,
                    Step("s28", ev.abriu),
                    Step("s29", ev.saiu));

            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder($"{DataFolder}/{name}")) AssetDatabase.CreateFolder(DataFolder, name);
        }

        // ---------- A parede e as luzes que reagem ----------

        static GuardianPassage BuildPassage(Transform g, GameObject wall)
        {
            var passage = g.gameObject.AddComponent<GuardianPassage>();
            SetField(passage, "settings", settings);
            SetField(passage, "wall", wall.transform);
            SetField(passage, "raiseWhenOpen", ev.abriu);

            // A luz azul da tabuleta, sobre a inscrição (apagada até a tabuleta pulsar).
            var pulse = new GameObject("Luz_Da_Tabuleta");
            pulse.transform.SetParent(g, false);
            pulse.transform.position = new Vector3(36.9f, -11.2f, 96f);
            var pulseLight = pulse.AddComponent<Light>();
            pulseLight.type = LightType.Point;
            pulseLight.color = settings.tabletPulseColor;
            pulseLight.intensity = 0f;
            pulseLight.range = 6f;
            SetField(passage, "pulseLight", pulseLight);

            // Som da pedra e poeira, no pé da parede.
            Transform soundPoint = Point(g, "Som_Parede", new Vector3(36f, -12f, 96f));
            Audio(soundPoint.gameObject, EditorTools.Audio.ProvisionalSounds.WallOpening, loop: false, playOnAwake: false, volume: 1f, minDistance: 6f, maxDistance: 45f);
            SetField(passage, "wallSound", soundPoint.GetComponent<AudioSource>());
            SetField(passage, "dust", BuildDust(g));

            // Ao voltar de um checkpoint depois da abertura: parede já aberta, sem pulso nem som.
            Listener(g.gameObject, ev.abriu, e => Call(e, passage.SnapOpen));
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
            return passage;
        }

        static ParticleSystem BuildDust(Transform g)
        {
            var go = new GameObject("Poeira");
            go.transform.SetParent(g, false);
            go.transform.position = new Vector3(36.4f, -13.8f, 96f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 4.5f;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startColor = new Color(0.8f, 0.7f, 0.52f, 0.35f);
            main.gravityModifier = -0.02f;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 30f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.3f, 0.2f, 3.6f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = DustMaterial();
            return ps;
        }

        // Material simples e sem luz para a poeira (criado uma vez).
        static Material DustMaterial()
        {
            const string path = "Assets/_Nibiru/Art/Materials/Blockout/BO_Poeira.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------- A inscrição na parede ----------

        static void BuildInscription(Transform g, GuardianPassage passage)
        {
            // Os glifos em ouro (a faixa sobre o barco) são do blockout, no grupo da parede, e descem com ela.
            // O alvo da interação: de frente para a parede. Some quando a passagem abre.
            GameObject zone = Zone(g, "Ler_Inscricao", ReadZoneCenter, ReadZoneSize);
            DialogueStarterOn(zone, inscriptionTalk, "Ler a inscrição", interactable: true);

            Listener(g.gameObject, ev.leuInscricao, e => Call(e, passage.BeginOpening));
            SetField(g.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
            Listener(g.gameObject, ev.abriu, e => SetActive(e, zone, false));
        }

        // ---------- O túnel: lamparinas e som ----------

        static void BuildTunnel(Transform g, GuardianPassage passage)
        {
            var lights = new System.Collections.Generic.List<Light>();
            for (int i = 0; i < Lamps.Length; i++) lights.Add(Lamp(g, $"Lamparina_{i + 1}", Lamps[i].spot, Lamps[i].wallSide));
            SetField(passage, "lamps", lights.ToArray());

            Transform ambience = Point(g, "Ambiente_Do_Tunel", new Vector3(32f, -12f, 104f));
            Audio(ambience.gameObject, EditorTools.Audio.ProvisionalSounds.TunnelAmbience, loop: true, playOnAwake: true, volume: settings.tunnelAmbienceVolume, minDistance: 6f, maxDistance: 32f);
            SetField(passage, "ambience", ambience.GetComponent<AudioSource>());
        }

        // Lamparina de barro num degrau da pedra, com a chama acesa e a luz quente.
        static Light Lamp(Transform parent, string name, Vector3 spot, Vector3 wallSide)
        {
            Transform lamp = Group(parent, name);
            lamp.position = spot;
            Local(PrimitiveType.Cube, lamp, "Degrau", new Vector3(0f, -0.14f, 0f), new Vector3(0.45f, 0.08f, 0.45f), "BO_Rocha", ColliderMode.None);
            Local(PrimitiveType.Cylinder, lamp, "Barro", new Vector3(0f, -0.04f, 0f), new Vector3(0.22f, 0.06f, 0.22f), "BO_TijoloBarro");
            Local(PrimitiveType.Sphere, lamp, "Chama", new Vector3(0f, 0.12f, 0f), new Vector3(0.08f, 0.16f, 0.08f), "BO_Chama");

            var lightGo = new GameObject("Luz");
            lightGo.transform.SetParent(lamp, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.3f, 0f) - wallSide * 0.25f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = settings.lampColor;
            light.intensity = settings.lampIntensity;
            light.range = settings.lampRange;
            light.shadows = LightShadows.None;
            return light;
        }

        // ---------- A fita de Irena ----------

        static void BuildTape(Transform g)
        {
            var player = Group(g, "Gravador_De_Fitas");
            var tapePlayer = player.gameObject.AddComponent<TapePlayer>();
            SetField(tapePlayer, "resumeOnLoad", tape);

            // Chiado de fita por baixo da fala: 2D, baixo.
            var hissGo = new GameObject("Chiado_Da_Fita");
            hissGo.transform.SetParent(player, false);
            var hiss = hissGo.AddComponent<AudioSource>();
            hiss.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EditorTools.Audio.ProvisionalSounds.RadioStatic);
            hiss.loop = true;
            hiss.playOnAwake = false;
            hiss.volume = 0.14f;
            hiss.spatialBlend = 0f;
            SetField(tapePlayer, "hiss", hiss);

            // A fita num degrau de pedra: caixa pequena e um gatilho maior para ficar fácil de pegar.
            Transform pickupGroup = Group(g, "Fita_De_Irena_1");
            Prim(PrimitiveType.Cube, pickupGroup, "Degrau", TapeSpot + new Vector3(0f, -0.1f, 0f), new Vector3(0.6f, 0.2f, 0.6f), "BO_Rocha", collider: ColliderMode.None);
            GameObject zone = Zone(pickupGroup, "Pegar_Fita", TapeSpot + new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 1f, 0.9f));
            // A fita em si é filha da área: some junto quando é pega.
            Prim(PrimitiveType.Cube, zone.transform, "Fita", TapeSpot + new Vector3(0f, 0.04f, 0f), new Vector3(0.2f, 0.05f, 0.13f), "BO_LonaVerde", 20f, ColliderMode.None);
            var pickup = zone.AddComponent<TapePickup>();
            SetField(pickup, "tape", tape);
            SetField(pickup, "player", tapePlayer);
        }

        // ---------- A saída, no topo da rampa ----------

        static void BuildExit(Transform g)
        {
            EventZone(g, "Gatilho_Saida_Da_Passagem", ExitZoneCenter, ExitZoneSize, ev.saiu);
            // Salvamento automático no topo da rampa. A fita, se ainda tocar, não é cortada.
            Checkpoint(g, "Autosave_Topo_Da_Rampa", ExitZoneCenter, ExitZoneSize, ExitCheckpointSpawn, 0f);
        }

        // ---------- Depois de abrir ----------

        // Um só ponto de retorno, logo depois de abrir a parede (nada no meio do túnel).
        static void BuildAfterOpening(Transform g, GuardianPassage passage)
        {
            Checkpoint(g, "Checkpoint_Passagem_Aberta", OpenCheckpointCenter, OpenCheckpointSize, OpenCheckpointSpawn, 270f);
            var checkpoint = g.Find("Checkpoint_Passagem_Aberta").GetComponent<Save.Checkpoint>();

            var talk = DialogueTrigger(g, "Dialogo_Passagem_Aberta", openTalk);
            var sequence = talk.gameObject.AddComponent<TimedSequence>();
            SetSteps(sequence,
                S("a Elena vê o túnel e as lamparinas", 0.8f, e => Call(e, talk.StartDialogue)),
                S("ponto de retorno diante da passagem aberta", 0.1f, e => Call(e, checkpoint.ActivateNow)));
            Listener(talk.gameObject, ev.abriu, e => Call(e, sequence.Play));
            SetField(talk.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);
        }
    }
}
