using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Memories;
using Nibiru.Quests;
using Nibiru.Sequences;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Nibiru.EditorTools.Content
{
    // Dentro do namespace, para as ferramentas do kit valerem antes de nomes como Nibiru.Dialogue.
    using static ContentKit;

    /// <summary>
    /// Monta a cena da memória jogável da Tabuleta do Junco (passo 8 da missão "Sobrevivente"):
    /// Shuruppak ao pôr do sol, antes do Dilúvio. O jogador é Ziusudra e anda por uma rua de
    /// casas de barro, toldos coloridos e palmeiras até a cabana de junco à beira do rio, onde a
    /// voz de Enki fala através da parede. Quando a voz se cala, a memória termina.
    ///
    /// A cena fica longe da origem (em <see cref="Origin"/>), para não encostar na região quando
    /// é carregada por cima dela. Ela é refeita do zero a cada execução; os arquivos de dados
    /// (eventos, diálogos, a memória) só são criados se ainda não existirem.
    /// Menu: Nibiru > Conteúdo > Construir memória de Shuruppak.
    /// </summary>
    static class ShuruppakMemoryContent
    {
        public const string SceneName = "Memoria_Shuruppak";
        const string ScenePath = "Assets/_Nibiru/Scenes/" + SceneName + ".unity";
        const string MaterialFolder = "Assets/_Nibiru/Art/Materials/Memoria";

        /// <summary>Canto da cena da memória no mundo: longe o bastante para a região não aparecer.</summary>
        static readonly Vector3 Origin = new Vector3(2000f, 0f, 2000f);

        // Rua de norte a sul (x de -3 a 3); a beira do rio fica em z = 30.
        const float StreetHalfWidth = 3f;
        const float RiverBank = 30f;

        // Cabana de junco na beira do rio, a leste do fim da rua, com a porta para o oeste.
        static readonly Vector3 HutCenter = new Vector3(10f, 0f, 24f);
        const float HutLength = 6f;   // leste-oeste
        const float HutWidth = 4f;    // norte-sul

        // Pôr do sol: luz baixa e alaranjada vindo do oeste.
        static readonly Color SunColor = new Color(1f, 0.62f, 0.38f);
        static readonly Color KurBlue = new Color(0.15f, 0.35f, 1f);

        static Vector3 W(float x, float y, float z) => Origin + new Vector3(x, y, z);

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent entrouNaMemoria, entrouNaCabana, paredeFalou, ouviuAParede;
        }

        class Dialogues
        {
            public DialogueDefinition chamado, pescador, menina, paredeDeJunco;
        }

        static Events ev;
        static Dialogues dlg;

        /// <summary>A memória da Tabuleta do Junco (criada se ainda não existir). A Aldeia também usa.</summary>
        public static MemoryDefinition MemoryData()
        {
            CreateEvents();
            if (!AssetDatabase.IsValidFolder(DataFolder + "/Memories"))
                AssetDatabase.CreateFolder(DataFolder, "Memories");
            return Asset<MemoryDefinition>("Memories/Memoria_Ziusudra", m =>
            {
                m.sceneName = SceneName;
                m.characterName = "Ziusudra";
                m.placeTitle = "Shuruppak, antes do Dilúvio";
                m.raiseOnEnter = ev.entrouNaMemoria;
                m.endOn = ev.ouviuAParede;
                m.endDelay = 2.5f;
            });
        }

        /// <summary>Evento do fim da memória: a voz terminou de falar através da parede de junco.</summary>
        public static StoryEvent HeardTheReedWall()
        {
            CreateEvents();
            return ev.ouviuAParede;
        }

        /// <summary>Evento da entrada na memória.</summary>
        public static StoryEvent EnteredMemory()
        {
            CreateEvents();
            return ev.entrouNaMemoria;
        }

        static void CreateEvents()
        {
            ev = new Events
            {
                entrouNaMemoria = Event("EntrouNaMemoriaDeZiusudra", "A Elena tocou a Tabuleta do Junco e entrou na memória: ela é Ziusudra, em Shuruppak, antes do Dilúvio."),
                entrouNaCabana = Event("EntrouNaCabanaDeJunco", "Na memória, Ziusudra entrou na cabana de junco à beira do rio. A voz começa a falar."),
                paredeFalou = Event("AParedeDeJuncoFalou", "A voz falou através da parede de junco, que acende com o azul de Kur."),
                ouviuAParede = Event("OuviuAParedeDeJunco", "Ziusudra ouviu a voz até o fim. A memória termina e a Elena volta ao nicho."),
            };
        }

        static void CreateData()
        {
            CreateEvents();

            dlg = new Dialogues
            {
                chamado = Dialogue("Memoria_Chamado", null,
                    N("z1", "Ziusudra", "(O sol se põe sobre Shuruppak. O rio subiu de novo esta semana, e ninguém sabe dizer por quê.)", "v1"),
                    N("v1", "Voz", "Ziusudra...", "z2"),
                    N("z2", "Ziusudra", "(A voz vem da beira do rio, da cabana de junco. Ninguém mais na rua parece ouvir.)")),

                pescador = Dialogue("Memoria_Pescador", null,
                    N("p1", "Pescador", "O rio subiu dois palmos desde a última lua, Ziusudra. E nem choveu lá no norte.", "p2"),
                    N("p2", "Pescador", "Os velhos dizem que é sinal. Que os deuses estão cansados de nós.")),

                menina = Dialogue("Memoria_Menina", null,
                    N("m1", "Menina", "Ziusudra! Meu pai disse que os deuses lá de cima não conseguem dormir por causa do nosso barulho.", "m2"),
                    N("m2", "Menina", "Se eu ficar bem quietinha, eles param de ficar bravos?")),

                paredeDeJunco = Dialogue("Memoria_ParedeDeJunco", ev.ouviuAParede,
                    NR("v1", "Voz", "Parede de junco, escuta. Parede de barro, presta atenção.", "z1", ev.paredeFalou),
                    N("z1", "Ziusudra", "Senhor das águas profundas? Por que fala com a parede, e não comigo?", "v2"),
                    N("v2", "Voz", "Porque jurei aos outros deuses que não contaria nada a homem nenhum. Então falo à parede. A parede não é homem.", "v3"),
                    N("v3", "Voz", "O conselho decidiu afogar a terra. As águas virão por sete dias e sete noites, e nada que respira vai sobrar.", "v4"),
                    N("v4", "Voz", "Derruba tua casa e constrói um barco. Abandona os bens e salva a vida. Leva contigo a semente de todo ser que respira.", "z2"),
                    N("z2", "Ziusudra", "Os outros deuses querem a nossa morte. Por que o senhor nos salva?", "v5"),
                    N("v5", "Voz", "(Um silêncio longo demais, como se a parede pensasse.) Porque vocês são meus.", "v6"),
                    N("v6", "Voz", "E guarda bem o teu sangue, Ziusudra. Passa de filho para filho, sem deixar que se perca.", "z3"),
                    N("z3", "Ziusudra", "Farei como o senhor diz.", "v7"),
                    N("v7", "Voz", "Tila. Viva.")),
            };

            MemoryData();
            AssetDatabase.SaveAssets();
        }

        // Fala que dispara um evento ao aparecer.
        static DialogueNode NR(string id, string speaker, string text, string next, StoryEvent raise)
        {
            DialogueNode node = N(id, speaker, text, next);
            node.raise = raise;
            return node;
        }

        // ---------- Cena ----------

        [MenuItem("Nibiru/Conteúdo/Construir memória de Shuruppak")]
        static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Saia do Play antes de construir a memória de Shuruppak.");
                return;
            }

            IdPrefix = "memoria_shuruppak";
            CreateData();

            // Se a cena da memória estiver aberta, fecha: ela é refeita do zero.
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (open.path == ScenePath) EditorSceneManager.CloseScene(open, true);
            }

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            Transform root = new GameObject("Shuruppak").transform;
            Lighting(root);
            BuildGround(Group(root, "Chao_e_Rio"));
            BuildStreet(Group(root, "Rua"));
            BuildRiverbank(Group(root, "Beira_do_Rio"));
            BuildZiggurat(Group(root, "Zigurate"));
            BuildPeople(Group(root, "Moradores"));
            BuildHut(Group(root, "Cabana_de_Junco"));
            BuildPlayerParts(Group(root, "Ziusudra"));
            BuildBounds(Group(root, "Limites"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            AddToBuildSettings();
            Debug.Log("Memória de Shuruppak construída em " + ScenePath + ".");
        }

        // A cena precisa estar na lista do Build para ser carregada durante o jogo.
        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // Sol baixo, céu de fim de tarde e névoa quente que esconde a borda do mundo.
        static void Lighting(Transform root)
        {
            var sun = new GameObject("Sol_PorDoSol").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(9f, 78f, 0f);
            sun.type = LightType.Directional;
            sun.color = SunColor;
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.skybox = SkyMaterial();
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.5f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.4f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.17f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.86f, 0.56f, 0.38f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 260f;
        }

        static void BuildGround(Transform g)
        {
            // Chão de terra batida até a beira do rio; a outra margem, com o zigurate, depois da água.
            Box(g, "Chao", W(0f, -0.5f, -15f), new Vector3(160f, 1f, 90f), Mat("BO_Areia"));
            Box(g, "Rua_Terra", W(0f, 0.01f, -7f), new Vector3(StreetHalfWidth * 2f, 0.02f, 62f), Mat("BO_Terra"), collider: false);
            Box(g, "Outra_Margem", W(0f, -0.5f, 110f), new Vector3(320f, 1f, 100f), Mat("BO_Areia"));
            Box(g, "Rio", W(0f, -0.35f, 45f), new Vector3(320f, 0.2f, 30f), MemMat("MEM_Agua", new Color(0.17f, 0.24f, 0.17f), 0.6f), collider: false);
            Box(g, "Leito", W(0f, -1.2f, 45f), new Vector3(320f, 1f, 30f), Mat("BO_Terra"), collider: false);
        }

        static void BuildStreet(Transform g)
        {
            // (centro em z, profundidade, altura, material da casa, cor do toldo)
            var west = new (float z, float depth, float height, string wall, Material cloth)[]
            {
                (-31f, 8f, 3.5f, "BO_TijoloBarro", Red()), (-21f, 9f, 4.5f, "BO_Areia", Yellow()),
                (-10f, 8f, 3.2f, "BO_TijoloBarro", Mat("BO_LonaVerde")), (0f, 9f, 4f, "BO_TijoloBarroEscuro", Purple()),
                (10f, 8f, 3.5f, "BO_Areia", Mat("BO_LonaLaranja")),
            };
            var east = new (float z, float depth, float height, string wall, Material cloth)[]
            {
                (-32f, 7f, 4f, "BO_Areia", Mat("BO_LonaVerde")), (-22f, 9f, 3.4f, "BO_TijoloBarro", Mat("BO_LonaLaranja")),
                (-12f, 8f, 4.6f, "BO_TijoloBarro", Red()), (7f, 8f, 3.6f, "BO_Areia", Yellow()),
                (15.5f, 5f, 3f, "BO_TijoloBarro", Purple()),
            };
            for (int i = 0; i < west.Length; i++) House(g, $"Casa_Oeste_{i + 1}", -1, west[i].z, west[i].depth, west[i].height, west[i].wall, west[i].cloth);
            for (int i = 0; i < east.Length; i++) House(g, $"Casa_Leste_{i + 1}", 1, east[i].z, east[i].depth, east[i].height, east[i].wall, east[i].cloth);

            // Fila de trás, sem detalhe, para os becos não darem no vazio.
            for (int i = 0; i < 7; i++)
            {
                float z = -34f + i * 9f;
                Box(g, $"Fundo_Oeste_{i + 1}", W(-17f, 2f + (i % 3) * 0.6f, z), new Vector3(6f, 4f + (i % 3) * 1.2f, 8f), Mat(i % 2 == 0 ? "BO_TijoloBarro" : "BO_Areia"));
                Box(g, $"Fundo_Leste_{i + 1}", W(17f, 2.2f + (i % 2) * 0.5f, z + 3f), new Vector3(6f, 4.4f + (i % 2) * 1f, 8f), Mat(i % 2 == 0 ? "BO_Areia" : "BO_TijoloBarro"));
            }

            // Panos coloridos pendurados em cordas atravessando a rua.
            foreach (float z in new[] { -16.5f, 3f })
            {
                Box(g, $"Corda_{z}", W(0f, 3.6f, z), new Vector3(StreetHalfWidth * 2f, 0.03f, 0.03f), Mat("BO_Madeira"), collider: false);
                Material[] cloths = { Red(), Yellow(), Mat("BO_LonaVerde"), Purple(), Mat("BO_LonaLaranja") };
                for (int i = 0; i < 5; i++)
                    Box(g, $"Pano_{z}_{i + 1}", W(-2.4f + i * 1.2f, 3.1f, z), new Vector3(0.75f, 0.95f, 0.02f), cloths[i], collider: false);
            }

            // Praça do mercado, no vão do lado leste: banca com toldo e potes de barro.
            Box(g, "Banca", W(6.5f, 0.45f, -2.5f), new Vector3(1.2f, 0.9f, 3f), Mat("BO_Madeira"));
            Box(g, "Toldo_Banca", W(6.8f, 2.4f, -2.5f), new Vector3(2.4f, 0.05f, 3.6f), Yellow(), new Vector3(0f, 0f, -10f), collider: false);
            foreach (float z in new[] { -4.2f, -0.8f })
                foreach (float x in new[] { 5.7f, 7.9f })
                    Piece(PrimitiveType.Cylinder, g, "Poste_Banca", W(x, 1.2f, z), new Vector3(0.08f, 1.2f, 0.08f), Mat("BO_Madeira"), Vector3.zero, false);
            for (int i = 0; i < 5; i++)
                Piece(PrimitiveType.Sphere, g, $"Pote_{i + 1}", W(6.3f + (i % 2) * 0.3f, 1.05f, -3.6f + i * 0.55f), new Vector3(0.32f, 0.38f, 0.32f), Mat("BO_TijoloBarro"), Vector3.zero, false);
            Palm(g, "Palmeira_Praca", W(10f, 0f, -4.5f), 4f);
        }

        // Casa de barro de um lado da rua (side = -1 oeste, 1 leste), com porta e toldo virados para a rua.
        static void House(Transform g, string name, int side, float z, float depth, float height, string wall, Material cloth)
        {
            const float width = 8f;
            float face = side * StreetHalfWidth;
            float centerX = face + side * width * 0.5f;
            Transform house = Group(g, name);

            Box(house, "Paredes", W(centerX, height * 0.5f, z), new Vector3(width, height, depth), Mat(wall));
            Box(house, "Parapeito", W(centerX, height + 0.15f, z), new Vector3(width + 0.2f, 0.3f, depth + 0.2f), Mat("BO_TijoloBarroEscuro"), collider: false);
            Box(house, "Porta", W(face + side * -0.03f, 1.05f, z), new Vector3(0.1f, 2.1f, 1.2f), Mat("BO_TijoloBarroEscuro"), collider: false);
            // Toldo inclinado para a rua, sobre a porta, com duas varas.
            Box(house, "Toldo", W(face - side * 0.7f, 2.45f, z), new Vector3(1.4f, 0.05f, 2.4f), cloth, new Vector3(0f, 0f, side * 14f), collider: false);
            foreach (float dz in new[] { -1.1f, 1.1f })
                Piece(PrimitiveType.Cylinder, house, "Vara", W(face - side * 1.35f, 1.15f, z + dz), new Vector3(0.06f, 1.15f, 0.06f), Mat("BO_Madeira"), Vector3.zero, false);
        }

        static void BuildRiverbank(Transform g)
        {
            foreach (float x in new[] { -24f, -15f, -7f, 16f, 24f })
                Palm(g, $"Palmeira_{x}", W(x, 0f, 27.5f + (x > 0 ? 0.5f : 0f)), 5.5f + Mathf.Abs(x) % 3f);

            // Touceiras de junco na beira da água.
            Material reed = Reed();
            int n = 0;
            foreach (float x in new[] { -20f, -11f, -3f, 4f, 15f, 20f })
                for (int i = 0; i < 9; i++)
                {
                    float dx = (i % 3) * 0.25f - 0.25f;
                    float dz = (i / 3) * 0.2f;
                    float h = 0.9f + (i * 37 % 5) * 0.12f;
                    Piece(PrimitiveType.Cylinder, g, $"Junco_{++n}", W(x + dx, h * 0.5f, 29.2f + dz), new Vector3(0.04f, h * 0.5f, 0.04f), reed, new Vector3((i % 2) * 6f - 3f, 0f, (i % 3) * 4f - 4f), false);
                }

            // Barco de pesca na margem.
            Box(g, "Barco", W(-9f, 0.15f, 28.8f), new Vector3(3.2f, 0.3f, 0.9f), Reed(), new Vector3(0f, 12f, 0f), collider: false);
        }

        // O templo de Shuruppak do outro lado do rio: terraços de barro e o santuário dourado no topo.
        static void BuildZiggurat(Transform g)
        {
            Vector3 c = W(-25f, 0f, 125f);
            Box(g, "Terraco_1", c + new Vector3(0f, 4f, 0f), new Vector3(44f, 8f, 44f), Mat("BO_TijoloBarro"), collider: false);
            Box(g, "Terraco_2", c + new Vector3(0f, 11.5f, 0f), new Vector3(32f, 7f, 32f), Mat("BO_TijoloBarro"), collider: false);
            Box(g, "Terraco_3", c + new Vector3(0f, 18f, 0f), new Vector3(20f, 6f, 20f), Mat("BO_TijoloBarro"), collider: false);
            Box(g, "Santuario", c + new Vector3(0f, 23f, 0f), new Vector3(8f, 4f, 8f), Mat("BO_Areia"), collider: false);
            Box(g, "Teto_Dourado", c + new Vector3(0f, 25.2f, 0f), new Vector3(8.6f, 0.4f, 8.6f), Mat("BO_Ouro"), collider: false);
            Box(g, "Escadaria", c + new Vector3(0f, 6f, -26f), new Vector3(6f, 0.6f, 22f), Mat("BO_TijoloBarroEscuro"), new Vector3(-28f, 0f, 0f), collider: false);
            for (int i = 0; i < 6; i++)
                Box(g, $"Casa_Distante_{i + 1}", W(-70f + i * 26f, 2.5f, 78f + (i % 2) * 8f), new Vector3(10f, 5f + (i % 3) * 2f, 8f), Mat(i % 2 == 0 ? "BO_Areia" : "BO_TijoloBarro"), collider: false);
        }

        static void BuildPeople(Transform g)
        {
            Person(g, "Mulher_na_Porta", W(-3.7f, 0f, -24f), 90f, Red());
            Person(g, "Homem_na_Porta", W(3.7f, 0f, -14f), -90f, Tunic());
            Person(g, "Mercador", W(7.6f, 0f, -2.5f), -90f, Yellow());
            Person(g, "Mulher_com_Jarro", W(-2.2f, 0f, 9f), 160f, Mat("BO_LonaVerde"));
            Piece(PrimitiveType.Sphere, g, "Jarro", W(-2.2f, 2.05f, 9f), new Vector3(0.3f, 0.36f, 0.3f), Mat("BO_TijoloBarro"), Vector3.zero, false);
            Person(g, "Velho_Sentado", W(-3.5f, 0f, 3.5f), 90f, Tunic(), height: 0.7f);
            Person(g, "Menino", W(4.2f, 0f, 12f), -60f, Mat("BO_LonaLaranja"), height: 0.65f);

            GameObject girl = Person(g, "Menina", W(1.8f, 0f, -7f), -130f, Purple(), height: 0.65f);
            DialogueStarterOn(girl, dlg.menina, "Falar com a menina", interactable: true);

            GameObject fisher = Person(g, "Pescador", W(-4.5f, 0f, 27.6f), 10f, Tunic());
            Piece(PrimitiveType.Cylinder, fisher.transform, "Vara_de_Pesca", W(-4.3f, 1.6f, 28.8f), new Vector3(0.03f, 1.4f, 0.03f), Mat("BO_Madeira"), new Vector3(55f, 10f, 0f), false);
            DialogueStarterOn(fisher, dlg.pescador, "Falar com o pescador", interactable: true);
        }

        // Morador em formas simples: túnica (cápsula) e cabeça. A cápsula tem colisão.
        static GameObject Person(Transform parent, string name, Vector3 feet, float yaw, Material tunic, float height = 1f)
        {
            var person = new GameObject(name);
            person.transform.SetParent(parent, false);
            person.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
            Local(PrimitiveType.Capsule, person.transform, "Corpo", new Vector3(0f, 0.8f * height, 0f), new Vector3(0.55f, 0.8f, 0.55f) * height, "BO_Areia", ColliderMode.Solid)
                .GetComponent<Renderer>().sharedMaterial = tunic;
            Local(PrimitiveType.Sphere, person.transform, "Cabeca", new Vector3(0f, 1.72f * height, 0f), Vector3.one * 0.26f * Mathf.Max(height, 0.8f), "BO_Areia")
                .GetComponent<Renderer>().sharedMaterial = Skin();
            return person;
        }

        static void Palm(Transform g, string name, Vector3 feet, float height)
        {
            Transform palm = Group(g, name);
            palm.position = feet;
            Local(PrimitiveType.Cylinder, palm, "Tronco", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.32f, height * 0.5f, 0.32f), "BO_Madeira", ColliderMode.Solid);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 360f / 7f;
                Quaternion turn = Quaternion.Euler(0f, angle, 0f);
                GameObject leaf = Local(PrimitiveType.Cube, palm, $"Folha_{i + 1}", new Vector3(0f, height, 0f) + turn * new Vector3(0f, -0.35f, 1.2f), new Vector3(0.55f, 0.06f, 2.6f), "BO_Palmeira");
                leaf.transform.localRotation = turn * Quaternion.Euler(22f, 0f, 0f);
            }
        }

        // A cabana de junco (como os mudhif do sul do Iraque): paredes de feixes de junco, colunas
        // grossas na porta e telhado inclinado. Atrás da parede do fundo, a luz azul de Kur espera.
        static void BuildHut(Transform g)
        {
            Material reed = Reed();
            float x0 = HutCenter.x - HutLength * 0.5f, x1 = HutCenter.x + HutLength * 0.5f;
            float z0 = HutCenter.z - HutWidth * 0.5f, z1 = HutCenter.z + HutWidth * 0.5f;
            const float wallHeight = 2.4f;

            // Paredes norte e sul.
            Transform walls = Group(g, "Paredes");
            int n = 0;
            for (float x = x0 + 0.11f; x < x1; x += 0.22f)
                foreach (float z in new[] { z0, z1 })
                    Piece(PrimitiveType.Cylinder, walls, $"Feixe_{++n}", W(x, wallHeight * 0.5f, z), new Vector3(0.13f, wallHeight * 0.5f, 0.13f), reed, Vector3.zero, false);
            // Parede da porta (oeste), com o vão no meio.
            for (float z = z0 + 0.11f; z < z1; z += 0.22f)
                if (Mathf.Abs(z - HutCenter.z) > 0.8f)
                    Piece(PrimitiveType.Cylinder, walls, $"Feixe_{++n}", W(x0, wallHeight * 0.5f, z), new Vector3(0.13f, wallHeight * 0.5f, 0.13f), reed, Vector3.zero, false);
            // A parede de junco do fundo (leste): feixes com frestas, por onde o azul vaza.
            Transform reedWall = Group(g, "Parede_de_Junco");
            for (float z = z0 + 0.13f; z < z1; z += 0.27f)
                Piece(PrimitiveType.Cylinder, reedWall, $"Feixe_{++n}", W(x1, 1.4f, z), new Vector3(0.2f, 1.4f, 0.2f), reed, Vector3.zero, false);

            // Colisão das paredes em caixas invisíveis (uma por parede, em vez de uma por feixe).
            Wall(g, "Colisao_Norte", W(HutCenter.x, 1.4f, z1), new Vector3(HutLength, 2.8f, 0.3f));
            Wall(g, "Colisao_Sul", W(HutCenter.x, 1.4f, z0), new Vector3(HutLength, 2.8f, 0.3f));
            Wall(g, "Colisao_Fundo", W(x1, 1.4f, HutCenter.z), new Vector3(0.4f, 2.8f, HutWidth));
            Wall(g, "Colisao_Porta_N", W(x0, 1.4f, (z1 + HutCenter.z + 0.8f) * 0.5f), new Vector3(0.3f, 2.8f, z1 - HutCenter.z - 0.8f));
            Wall(g, "Colisao_Porta_S", W(x0, 1.4f, (z0 + HutCenter.z - 0.8f) * 0.5f), new Vector3(0.3f, 2.8f, HutCenter.z - 0.8f - z0));

            // Colunas grossas de junco amarrado dos dois lados da porta, e o telhado de duas águas.
            foreach (float dz in new[] { -0.95f, 0.95f })
                Piece(PrimitiveType.Cylinder, g, "Coluna_Porta", W(x0 - 0.1f, 1.7f, HutCenter.z + dz), new Vector3(0.38f, 1.7f, 0.38f), reed, Vector3.zero, false);
            foreach (int side in new[] { -1, 1 })
                Box(g, side < 0 ? "Telhado_Sul" : "Telhado_Norte", W(HutCenter.x, 2.95f, HutCenter.z + side * 1.1f), new Vector3(HutLength + 0.6f, 0.12f, 2.7f), reed, new Vector3(side * 32f, 0f, 0f), collider: false);
            Box(g, "Esteira", W(HutCenter.x, 0.02f, HutCenter.z), new Vector3(HutLength - 0.6f, 0.03f, HutWidth - 0.6f), Mat("BO_Madeira"), collider: false);

            // Luz quente de dentro (uma brasa num canto) e o azul que vai crescer na parede do fundo.
            var ember = new GameObject("Luz_Brasa").AddComponent<Light>();
            ember.transform.SetParent(g, false);
            ember.transform.position = W(x0 + 1f, 0.6f, z0 + 0.8f);
            ember.type = LightType.Point;
            ember.color = new Color(1f, 0.55f, 0.25f);
            ember.range = 4f;
            ember.intensity = 0.8f;
            Piece(PrimitiveType.Sphere, g, "Brasa", W(x0 + 1f, 0.08f, z0 + 0.8f), new Vector3(0.3f, 0.12f, 0.3f), AssetDatabase.LoadAssetAtPath<Material>("Assets/_Nibiru/Art/Materials/Blockout/BO_Chama.mat"), Vector3.zero, false);

            GameObject glow = Box(g, "Brilho_de_Kur", W(x1 + 0.22f, 1.4f, HutCenter.z), new Vector3(0.02f, 2.6f, HutWidth - 0.2f), KurGlow(), collider: false);
            glow.SetActive(false);
            var blue = new GameObject("Luz_de_Kur").AddComponent<Light>();
            blue.transform.SetParent(g, false);
            blue.transform.position = W(x1 - 0.6f, 1.6f, HutCenter.z);
            blue.type = LightType.Point;
            blue.color = KurBlue;
            blue.range = 7f;
            blue.intensity = 0f;
            var blueFade = blue.gameObject.AddComponent<LightFade>();
            SetField(blueFade, "duration", 2.5f);
            SetField(blueFade, "flicker", 0.12f);

            // Ao entrar, a voz fala através da parede; na primeira fala, o azul acende; no fim, explode.
            EventZone(g, "Gatilho_Cabana", W(HutCenter.x + 0.4f, 1f, HutCenter.z), new Vector3(HutLength - 1.4f, 2f, HutWidth - 0.8f), ev.entrouNaCabana);
            var voice = DialogueTrigger(g, "Dialogo_ParedeDeJunco", dlg.paredeDeJunco);
            Listener(voice.gameObject, ev.entrouNaCabana, e => Call(e, voice.StartDialogue));
            Listener(g.gameObject, ev.paredeFalou, e =>
            {
                SetActive(e, glow, true);
                UnityEditor.Events.UnityEventTools.AddFloatPersistentListener(e, blueFade.FadeTo, 6f);
            });
            Listener(blue.gameObject, ev.ouviuAParede, e => UnityEditor.Events.UnityEventTools.AddFloatPersistentListener(e, blueFade.FadeTo, 12f));
        }

        // Onde o jogador começa, a aparência de Ziusudra e o chamado da voz logo depois da abertura.
        static void BuildPlayerParts(Transform g)
        {
            Transform spawn = Point(g, "Inicio", W(0f, 1.1f, -36f));
            spawn.rotation = Quaternion.identity;
            spawn.gameObject.AddComponent<MemorySpawn>();

            // Ziusudra: túnica clara, manto vermelho nos ombros, barba escura e um cajado. Sem colisão.
            var avatar = new GameObject("Aparencia_Ziusudra");
            avatar.transform.SetParent(g, false);
            avatar.transform.position = W(0f, 0f, -45f);
            avatar.AddComponent<MemoryAvatar>();
            Transform a = avatar.transform;
            Local(PrimitiveType.Capsule, a, "Tunica", new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.9f, 0.5f), "BO_Areia").GetComponent<Renderer>().sharedMaterial = Tunic();
            Local(PrimitiveType.Cylinder, a, "Manto", new Vector3(0f, 1.38f, 0f), new Vector3(0.55f, 0.12f, 0.55f), "BO_Areia").GetComponent<Renderer>().sharedMaterial = Red();
            Local(PrimitiveType.Sphere, a, "Cabeca", new Vector3(0f, 1.78f, 0f), Vector3.one * 0.27f, "BO_Areia").GetComponent<Renderer>().sharedMaterial = Skin();
            Local(PrimitiveType.Cube, a, "Barba", new Vector3(0f, 1.66f, 0.12f), new Vector3(0.17f, 0.16f, 0.08f), "BO_TijoloBarroEscuro");
            Local(PrimitiveType.Cylinder, a, "Cajado", new Vector3(0.34f, 0.95f, 0.18f), new Vector3(0.045f, 0.95f, 0.045f), "BO_Madeira");

            var call = DialogueTrigger(g, "Dialogo_Chamado", dlg.chamado);
            var intro = g.gameObject.AddComponent<TimedSequence>();
            SetField(intro, "playOnStart", true);
            SetField(intro, "skipIfRaised", ev.ouviuAParede);
            SetSteps(intro, S("a voz chama Ziusudra, depois da abertura", 5.5f, e => Call(e, call.StartDialogue)));
        }

        // Paredes invisíveis: atrás das casas, atrás do começo da rua e na beira do rio.
        static void BuildBounds(Transform g)
        {
            Wall(g, "Limite_Oeste", W(-13.5f, 2f, -5f), new Vector3(1f, 4f, 72f));
            Wall(g, "Limite_Leste", W(14.5f, 2f, -5f), new Vector3(1f, 4f, 72f));
            Wall(g, "Limite_Sul", W(0f, 2f, -40.5f), new Vector3(30f, 4f, 1f));
            Wall(g, "Limite_Rio", W(0f, 2f, RiverBank - 0.5f), new Vector3(30f, 4f, 1f));
        }

        // ---------- Peças ----------

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler = default, bool collider = true) =>
            Piece(PrimitiveType.Cube, parent, name, position, scale, material, euler, collider);

        static GameObject Piece(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3 euler, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Wall(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        // ---------- Materiais da memória (criados só se ainda não existirem) ----------

        static Material Red() => MemMat("MEM_TecidoVermelho", new Color(0.72f, 0.14f, 0.1f));
        static Material Yellow() => MemMat("MEM_TecidoAmarelo", new Color(0.95f, 0.72f, 0.18f));
        static Material Purple() => MemMat("MEM_TecidoRoxo", new Color(0.42f, 0.18f, 0.42f));
        static Material Tunic() => MemMat("MEM_Tunica", new Color(0.9f, 0.86f, 0.75f));
        static Material Skin() => MemMat("MEM_Pele", new Color(0.55f, 0.37f, 0.25f));
        static Material Reed() => MemMat("MEM_Junco", new Color(0.7f, 0.6f, 0.32f));

        static Material MemMat(string name, Color color, float smoothness = 0.15f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureMaterialFolder();
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // O azul de Kur: sem sombra nem luz, brilha sozinho.
        static Material KurGlow()
        {
            string path = $"{MaterialFolder}/MEM_BrilhoDeKur.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureMaterialFolder();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(0.4f, 0.65f, 1f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material SkyMaterial()
        {
            string path = $"{MaterialFolder}/MEM_CeuPorDoSol.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureMaterialFolder();
            material = new Material(Shader.Find("Skybox/Procedural"));
            material.SetFloat("_SunSize", 0.06f);
            material.SetFloat("_SunSizeConvergence", 4f);
            material.SetFloat("_AtmosphereThickness", 1.9f);
            material.SetColor("_SkyTint", new Color(0.62f, 0.45f, 0.45f));
            material.SetColor("_GroundColor", new Color(0.36f, 0.26f, 0.2f));
            material.SetFloat("_Exposure", 1.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Nibiru/Art/Materials", "Memoria");
        }
    }
}
