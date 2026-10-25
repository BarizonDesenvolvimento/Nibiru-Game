using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Nibiru.EditorTools.Blockout
{
    /// <summary>
    /// Constrói o blockout (mapa em formas simples) do Sítio de Ur na cena aberta, seguindo
    /// Docs/Regiao-1-Sitio-de-Ur.md. Rodar de novo apaga o blockout anterior e reconstrói,
    /// então ajustes de posição e tamanho se fazem aqui e não à mão na cena.
    /// Menu: Nibiru > Blockout > Construir Sítio de Ur na cena aberta.
    ///
    /// Eixos: Z aponta para o norte. O caminho vai do Acampamento (sul) ao Wadi (norte):
    /// Acampamento → Trincheiras → Aldeia (oeste) → Zigurate → Boca do Poço (subsolo) → Passagem → Wadi.
    /// </summary>
    static class SitioDeUrBlockout
    {
        const string RootName = "Blockout_SitioDeUr";
        const string MaterialFolder = "Assets/_Nibiru/Art/Materials/Blockout";

        // Paleta: colorida, de dia. Poeira e ruína em cima; lápis-lazúli e ouro embaixo.
        static Material sand, earth, adobe, adoberDark, orangeTarp, greenTarp, white, turquoise, doorBlue, wood, metal, palm, lapis, gold, rock, olive;

        [MenuItem("Nibiru/Blockout/Construir Sítio de Ur na cena aberta")]
        static void Build()
        {
            LoadMaterials();

            GameObject old = GameObject.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Construir blockout do Sítio de Ur");

            BuildGround(Group(root, "Chao"));
            BuildCamp(Group(root, "1_Acampamento"));
            BuildTrenches(Group(root, "2_Trincheiras"));
            BuildVillage(Group(root, "3_Aldeia"));
            BuildZiggurat(Group(root, "4_Zigurate_e_Base"));
            BuildWell(Group(root, "5_Boca_do_Poco"));
            BuildPassageAndWadi(Group(root, "6_Passagem_e_Wadi"));
            BuildLabels(Group(root, "Rotulos"));
            BakeNavMesh(root);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Blockout do Sítio de Ur construído.");
        }

        // ---------- Chão ----------

        // O chão é feito de peças com buracos para a boca do poço e a saída do túnel no wadi.
        static void BuildGround(Transform g)
        {
            Slab(g, "Chao_Sul", -90, 70, -130, 70, sand);
            Slab(g, "Chao_OestePoco", -90, 40, 70, 80, sand);
            Slab(g, "Chao_LestePoco", 50, 70, 70, 80, sand);
            Slab(g, "Chao_Meio", -90, 70, 80, 110, sand);
            Slab(g, "Chao_NorteOeste", -90, 30, 110, 150, sand);
            Slab(g, "Chao_NorteLeste", 34, 70, 110, 150, sand);
            Slab(g, "Chao_FimDaRampa", 30, 34, 140, 150, sand);
        }

        // ---------- 1. Acampamento (elevação ao sul) ----------

        static void BuildCamp(Transform g)
        {
            // Elevação de 3 m com rampa descendo para o norte.
            Box(g, "Elevacao", new Vector3(-25, 0, -128), new Vector3(25, 3, -92), sand);
            Ramp(g, "Rampa_Descida", new Vector3(0, 3, -92), new Vector3(0, 0, -79), 8, sand);

            const float top = 3f;
            // A da Elena é oca, com porta para o norte: ela acorda lá dentro (passo 1 da missão).
            HollowTent(g, "Tenda_Elena", new Vector3(-12, top, -119), new Vector3(6, 2.5f, 5), 1.6f, orangeTarp);
            BoxC(g, "Tenda_Equipe_1", new Vector3(-4, top + 1.25f, -120), new Vector3(5, 2.5f, 4), greenTarp);
            BoxC(g, "Tenda_Equipe_2", new Vector3(4, top + 1.25f, -120), new Vector3(5, 2.5f, 4), orangeTarp);
            BoxC(g, "Tenda_Equipe_3", new Vector3(12, top + 1.25f, -118), new Vector3(5, 2.5f, 4), greenTarp);
            BoxC(g, "Tenda_Triagem", new Vector3(-14, top + 1.5f, -104), new Vector3(7, 3, 6), white);
            BoxC(g, "Mesa_Triagem_1", new Vector3(2, top + 0.45f, -108), new Vector3(3, 0.9f, 1.2f), wood);
            BoxC(g, "Mesa_Triagem_2", new Vector3(6, top + 0.45f, -108), new Vector3(3, 0.9f, 1.2f), wood);
            BoxC(g, "Caminhao_1", new Vector3(16, top + 1.5f, -104), new Vector3(3, 3, 7), olive);
            BoxC(g, "Caminhao_2", new Vector3(19, top + 1.5f, -114), new Vector3(3, 3, 7), olive, 15);
            BoxC(g, "Gerador", new Vector3(-6, top + 0.7f, -100), new Vector3(1.5f, 1.4f, 1), metal);
            Cyl(g, "Antena", new Vector3(-18, top + 6, -98), 0.25f, 12, metal);
            Cyl(g, "Fogueira", new Vector3(0, top + 0.15f, -112), 0.6f, 0.3f, adoberDark);

            // Os corpos da equipe em círculo, virados para a tenda da Elena (passo 2 da missão).
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                var pos = new Vector3(Mathf.Cos(angle) * 3.2f, top + 0.2f, -112 + Mathf.Sin(angle) * 3.2f);
                BoxC(g, $"Corpo_{i + 1}", pos, new Vector3(0.5f, 0.35f, 1.8f), greenTarp, -angle * Mathf.Rad2Deg + 90f);
            }

            // Heliponto improvisado embaixo da elevação: de cima, a Elena vê o "resgate" (passo 4).
            Cyl(g, "Heliponto", new Vector3(30, 0.05f, -82), 6, 0.1f, white);
            Marker(g, "Ponto_Inicio_Elena", new Vector3(-12, top + 0.03f, -120), 0);
        }

        // ---------- 2. Trincheiras ----------

        // Grade de taludes de terra com passagens; bandeirinhas marcam as quadrículas.
        static void BuildTrenches(Transform g)
        {
            float[] rows = { -75, -65, -55, -47 };
            float[] cols = { -20, -10, 0, 10, 20 };
            const float height = 1.6f, thickness = 1.5f, gap = 3f;

            for (int r = 0; r < rows.Length; r++)
            {
                // Cada talude horizontal tem uma abertura que muda de lugar a cada fila.
                float opening = cols[(r * 2 + 1) % cols.Length] + 5f;
                Box(g, $"Talude_H{r + 1}_A", new Vector3(-25, 0, rows[r] - thickness / 2), new Vector3(opening - gap / 2, height, rows[r] + thickness / 2), earth);
                Box(g, $"Talude_H{r + 1}_B", new Vector3(opening + gap / 2, 0, rows[r] - thickness / 2), new Vector3(25, height, rows[r] + thickness / 2), earth);
            }
            for (int c = 0; c < cols.Length; c++)
            {
                if (c % 2 == 1) continue;
                Box(g, $"Talude_V{c + 1}_A", new Vector3(cols[c] - thickness / 2, 0, -74), new Vector3(cols[c] + thickness / 2, height, -68), earth);
                Box(g, $"Talude_V{c + 1}_B", new Vector3(cols[c] - thickness / 2, 0, -63), new Vector3(cols[c] + thickness / 2, height, -57), earth);
            }

            // Montes de terra peneirada e passarelas.
            Cyl(g, "Monte_Terra_1", new Vector3(-22, 1, -50), 2.5f, 2, earth);
            Cyl(g, "Monte_Terra_2", new Vector3(22, 0.8f, -78), 2, 1.6f, earth);
            BoxC(g, "Passarela_1", new Vector3(-5, height + 0.1f, -65), new Vector3(1.2f, 0.2f, 6), wood);
            BoxC(g, "Passarela_2", new Vector3(15, height + 0.1f, -55), new Vector3(1.2f, 0.2f, 6), wood);
            Ramp(g, "Escada_Passarela_1", new Vector3(-5, height + 0.2f, -68), new Vector3(-5, 0, -71.5f), 1.2f, wood);
            Ramp(g, "Escada_Passarela_2", new Vector3(15, height + 0.2f, -58), new Vector3(15, 0, -61.5f), 1.2f, wood);

            for (int i = 0; i < 10; i++)
                Cyl(g, $"Bandeirinha_{i + 1}", new Vector3(-22 + i * 5, 0.5f, -70 + (i % 3) * 9), 0.04f, 1, i % 2 == 0 ? orangeTarp : turquoise);

            BoxC(g, "Tenda_Medica_Escondida", new Vector3(-20, 1.25f, -49), new Vector3(4, 2.5f, 3), white);
            Marker(g, "Corpo_ChefeSeguranca_Pistola", new Vector3(8, 0.3f, -60), 0);
            Marker(g, "Esconderijo_DraWeiss", new Vector3(-20, 0.3f, -51), 0);
        }

        // ---------- 3. Aldeia (oeste) ----------

        static void BuildVillage(Transform g)
        {
            // Casas de tijolo de barro em quarteirões soltos, ruas entre elas.
            Vector2[] houses =
            {
                new Vector2(-70, -40), new Vector2(-62, -40), new Vector2(-48, -40), new Vector2(-40, -38),
                new Vector2(-70, -30), new Vector2(-40, -28), new Vector2(-70, -12), new Vector2(-46, -12),
                new Vector2(-40, -4), new Vector2(-52, 2), new Vector2(-72, 2),
            };
            for (int i = 0; i < houses.Length; i++)
            {
                float h = 3.2f + (i % 3) * 0.6f;
                var center = new Vector3(houses[i].x, h / 2, houses[i].y);
                BoxC(g, $"Casa_{i + 1}", center, new Vector3(6, h, 6), i % 4 == 0 ? adoberDark : adobe);
                // Porta azul-turquesa no lado leste (voltada para a rua).
                BoxC(g, $"Porta_{i + 1}", center + new Vector3(3.02f, -h / 2 + 1.1f, 0), new Vector3(0.05f, 2.2f, 1.2f), doorBlue);
            }

            // Casa do altar: aberta na frente, com o nicho dos Guardiões dentro (passos 7 e 8).
            Box(g, "CasaAltar_Fundo", new Vector3(-50, 0, -32), new Vector3(-44, 3.5f, -31.5f), adobe);
            Box(g, "CasaAltar_Oeste", new Vector3(-50, 0, -31.5f), new Vector3(-49.5f, 3.5f, -26), adobe);
            Box(g, "CasaAltar_Leste", new Vector3(-44.5f, 0, -31.5f), new Vector3(-44, 3.5f, -26), adobe);
            Box(g, "CasaAltar_Teto", new Vector3(-50, 3.5f, -32), new Vector3(-44, 3.8f, -26), adoberDark);
            BoxC(g, "Nicho_Guardioes", new Vector3(-47, 0.5f, -31), new Vector3(0.9f, 1, 0.6f), gold);
            Marker(g, "Altar_Checkpoint", new Vector3(-47, 1, -29.5f), 180);

            // Pátio com poço d'água, mercado e mesquita com minarete.
            Cyl(g, "Poco_Agua", new Vector3(-55, 0.5f, -22), 1.2f, 1, adoberDark);
            BoxC(g, "Barraca_Mercado_1", new Vector3(-58, 1.1f, -16), new Vector3(3, 2.2f, 2), orangeTarp);
            BoxC(g, "Barraca_Mercado_2", new Vector3(-52, 1.1f, -16), new Vector3(3, 2.2f, 2), turquoise);
            BoxC(g, "Mesquita", new Vector3(-64, 3, -2), new Vector3(10, 6, 10), adobe);
            Sphere(g, "Mesquita_Cupula", new Vector3(-64, 6, -2), 6, turquoise);
            Cyl(g, "Minarete", new Vector3(-58, 7.5f, 4), 0.8f, 15, adobe);

            // Casa trancada com a espingarda (opcional).
            Marker(g, "Casa_Trancada_Espingarda", new Vector3(-70, 0.5f, -30), 0);

            Palm(g, "Palmeira_1", new Vector3(-50, 0, -20));
            Palm(g, "Palmeira_2", new Vector3(-60, 0, -26));
            Palm(g, "Palmeira_3", new Vector3(-44, 0, -18));
            Palm(g, "Palmeira_4", new Vector3(-66, 0, -20));
        }

        // ---------- 4. Zigurate e base avançada ----------

        // O pátio da base vai do muro oeste (x = -22) à cerca leste (x = 60) e da cerca sul (z = -6)
        // ao pé do zigurate (z = 30). Entra-se pelo canto sudoeste, entre a ponta do muro e a cerca.
        // Uma avenida livre (z de 4 a 13) liga o portão do muro à tenda de comando: de lá se vê o
        // portão abrir. A saída para o Poço é pelo nordeste, entre o zigurate e a cerca leste.
        static void BuildZiggurat(Transform g)
        {
            // Três terraços de tijolo dourado; escadaria na face sul.
            Box(g, "Zigurate_Terraco_1", new Vector3(-20, 0, 30), new Vector3(40, 8, 70), adobe);
            Box(g, "Zigurate_Terraco_2", new Vector3(-10, 8, 39), new Vector3(30, 14, 65), adobe);
            Box(g, "Zigurate_Terraco_3", new Vector3(0, 14, 47), new Vector3(20, 19, 61), adoberDark);
            Ramp(g, "Escadaria_1", new Vector3(10, 8, 30), new Vector3(10, 0, 13), 8, adoberDark);
            Ramp(g, "Escadaria_2", new Vector3(10, 14, 39), new Vector3(10, 8, 31), 6, adoberDark);

            // Ninho do atirador na beira sul do 2º terraço, a leste da escadaria: sacos de areia na
            // frente e dos lados, uma caixa de munição atrás. Ele olha o pátio inteiro de cima.
            BoxC(g, "Posto_Atirador_Frente", new Vector3(20, 14.35f, 39.5f), new Vector3(4, 0.7f, 0.8f), olive);
            BoxC(g, "Posto_Atirador_Lado_Oeste", new Vector3(17.6f, 14.35f, 40.6f), new Vector3(0.8f, 0.7f, 2.2f), olive);
            BoxC(g, "Posto_Atirador_Lado_Leste", new Vector3(22.4f, 14.35f, 40.6f), new Vector3(0.8f, 0.7f, 2.2f), olive);
            BoxC(g, "Posto_Atirador_Caixa", new Vector3(21.5f, 14.3f, 42.6f), new Vector3(1, 0.6f, 0.6f), olive);
            Marker(g, "Atirador_De_Elite", new Vector3(20, 14.03f, 40.6f), 180);

            // Tendas da Fundação: ocas, com porta. Comando com a porta para a avenida (oeste);
            // médica e de provas com a porta para o sul. O alojamento é fechado.
            HollowTent(g, "Tenda_Comando", new Vector3(27, 0, 7), new Vector3(7, 3, 9), 1.8f, white, -90f);
            HollowTent(g, "Tenda_Medica", new Vector3(-10, 0, 17), new Vector3(7, 3, 6), 1.6f, white, 180f);
            HollowTent(g, "Tenda_Provas", new Vector3(32, 0, 22), new Vector3(6, 3, 5), 1.6f, white, 180f);
            BoxC(g, "Tenda_Alojamento", new Vector3(-16, 1.5f, 25), new Vector3(8, 3, 6), white);
            // Toldo na porta da tenda de comando: de cima, o atirador não vê quem está embaixo dele.
            BoxC(g, "Toldo_Tenda_Comando", new Vector3(20.5f, 2.75f, 7), new Vector3(4, 0.1f, 6), turquoise);
            Cyl(g, "Toldo_Poste_Sul", new Vector3(18.6f, 1.35f, 4.2f), 0.06f, 2.7f, metal);
            Cyl(g, "Toldo_Poste_Norte", new Vector3(18.6f, 1.35f, 9.8f), 0.06f, 2.7f, metal);

            // Contêineres: uma fileira ao sul da avenida (a cobertura de quem entra), dois ao norte e um
            // em pé junto da tenda de comando. Nenhum atravessa a avenida.
            Vector3[] containers =
            {
                new Vector3(-6, 1.3f, 1), new Vector3(4, 1.3f, 1), new Vector3(14, 1.3f, 1),
                new Vector3(0, 1.3f, 17), new Vector3(22, 1.3f, 17), new Vector3(18, 1.3f, 13),
            };
            for (int i = 0; i < containers.Length; i++)
                BoxC(g, $"Conteiner_{i + 1}", containers[i], new Vector3(6, 2.6f, 2.5f), i % 2 == 0 ? turquoise : white, i == 5 ? 90f : 0f);
            Cyl(g, "Heliponto_Base", new Vector3(40, 0.05f, 12), 6, 0.1f, white);
            for (int i = 0; i < 4; i++)
                Cyl(g, $"Bandeira_Fundacao_{i + 1}", new Vector3(-16 + i * 12, 3, -2), 0.08f, 6, turquoise);

            // Cerca da Fundação: ao sul (deixando a entrada no canto sudoeste) e a leste, até o wadi.
            Box(g, "Cerca_Sul", new Vector3(-14, 0, -6.15f), new Vector3(60, 2.4f, -5.85f), metal);
            Box(g, "Cerca_Leste", new Vector3(59.85f, 0, -6), new Vector3(60.15f, 2.4f, 112), metal);

            // Muralha baixa a oeste, com o vão do portão que abre para a Aldeia (o portão em si é do
            // conteúdo do passo 10, porque se move). Fecha o vão entre o muro e o zigurate.
            Box(g, "Muralha_Sul", new Vector3(-24, 0, -4), new Vector3(-22, 3, 6), adoberDark);
            Box(g, "Muralha_Norte", new Vector3(-24, 0, 12), new Vector3(-22, 3, 40), adoberDark);
            Box(g, "Muralha_Canto", new Vector3(-22, 0, 30), new Vector3(-20, 3, 40), adoberDark);

            // Rota alta: escada de madeira por dentro do muro, perto da entrada; por cima do muro
            // (com uma passarela sobre o portão); rampa sobre o muro até um patamar no 1º terraço;
            // e rampa de andaime pela face oeste até o 2º terraço, atrás do atirador.
            Ramp(g, "Rota_Alta_Escada_Muro", new Vector3(-21.2f, 0, -2), new Vector3(-21.2f, 3, 4), 1.6f, wood);
            BoxC(g, "Rota_Alta_Passarela_Portao", new Vector3(-23, 2.9f, 9), new Vector3(2, 0.2f, 6), wood);
            Ramp(g, "Rota_Alta_Rampa_Terraco_1", new Vector3(-23, 3, 26), new Vector3(-23, 8, 36), 2, wood);
            BoxC(g, "Rota_Alta_Patamar", new Vector3(-22, 7.9f, 37.5f), new Vector3(4, 0.2f, 3), wood);
            Ramp(g, "Rota_Alta_Rampa_Terraco_2", new Vector3(-11, 8, 44), new Vector3(-11, 14, 56), 2, wood);
        }

        // ---------- 5. Boca do Poço (desce ao subsolo) ----------

        static void BuildWell(Transform g)
        {
            // Boca do poço: buraco de 10 x 10 m no chão (x 40–50, z 70–80), cercado de andaimes.
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? 39.5f : 50.5f;
                float z = i % 2 == 0 ? 69.5f : 80.5f;
                Cyl(g, $"Andaime_Poste_{i + 1}", new Vector3(x, 2.5f, z), 0.12f, 5, metal);
            }
            BoxC(g, "Andaime_Viga_Sul", new Vector3(45, 5, 69.5f), new Vector3(11, 0.2f, 0.2f), metal);
            BoxC(g, "Andaime_Viga_Norte", new Vector3(45, 5, 80.5f), new Vector3(11, 0.2f, 0.2f), metal);
            BoxC(g, "Guincho", new Vector3(45, 5.4f, 75), new Vector3(1.2f, 0.6f, 11), metal);
            BoxC(g, "Holofote_Fundacao_Superficie", new Vector3(52, 1, 67), new Vector3(1, 2, 1), white);

            // Paredes do poço (rocha) do chão até -14 m; a norte abre para a antecâmara.
            Box(g, "Poco_Parede_Sul", new Vector3(39, -14, 69), new Vector3(51, 0, 70), rock);
            Box(g, "Poco_Parede_Oeste", new Vector3(39, -14, 70), new Vector3(40, 0, 80), rock);
            Box(g, "Poco_Parede_Leste", new Vector3(50, -14, 70), new Vector3(51, 0, 80), rock);
            Box(g, "Poco_Parede_Norte_Alta", new Vector3(40, -6, 80), new Vector3(50, 0, 81), rock);
            Box(g, "Poco_Fundo", new Vector3(40, -15, 70), new Vector3(50, -14, 80), rock);

            // Descida em zigue-zague por andaimes, 3,5 m por lance.
            Ramp(g, "Lance_1", new Vector3(41, 0, 70.5f), new Vector3(41, -3.5f, 78.5f), 2, wood);
            BoxC(g, "Patamar_1", new Vector3(41, -3.65f, 79), new Vector3(2, 0.3f, 2), wood);
            Ramp(g, "Lance_2", new Vector3(42, -3.5f, 79), new Vector3(48.5f, -7, 79), 2, wood);
            BoxC(g, "Patamar_2", new Vector3(49, -7.15f, 79), new Vector3(2, 0.3f, 2), wood);
            Ramp(g, "Lance_3", new Vector3(49, -7, 78), new Vector3(49, -10.5f, 71.5f), 2, wood);
            BoxC(g, "Patamar_3", new Vector3(49, -10.65f, 71), new Vector3(2, 0.3f, 2), wood);
            Ramp(g, "Lance_4", new Vector3(48, -10.5f, 71), new Vector3(41.5f, -14, 71), 2, wood);

            // Antecâmara (arena do Primeiro Servo): x 36–54, z 80–102, chão a -14 m, teto a -5 m.
            Box(g, "Ante_Chao", new Vector3(36, -15, 80), new Vector3(54, -14, 102), rock);
            Box(g, "Ante_Teto", new Vector3(35, -5, 80), new Vector3(55, -4, 103), rock);
            Box(g, "Ante_Parede_Leste", new Vector3(54, -14, 80), new Vector3(55, -5, 102), rock);
            Box(g, "Ante_Parede_Oeste_Sul", new Vector3(35, -14, 80), new Vector3(36, -5, 94), rock);
            Box(g, "Ante_Parede_Oeste_Norte", new Vector3(35, -14, 98), new Vector3(36, -5, 102), rock);
            Box(g, "Ante_Parede_Oeste_Topo", new Vector3(35, -10, 94), new Vector3(36, -5, 98), rock);
            // Parede do barco de junco: fecha o vão da passagem dos Guardiões (abre só no passo 13).
            // Na face de dentro (x = 36), o barco sobre as ondas, entalhado em ouro, e a lamparina.
            Box(g, "Parede_Barco_Junco", new Vector3(35, -14, 94), new Vector3(36, -10, 98), rock);
            BoxC(g, "Barco_Casco", new Vector3(36.06f, -12.3f, 96f), new Vector3(0.12f, 0.35f, 1.7f), gold);
            BoxC(g, "Barco_Mastro", new Vector3(36.06f, -11.6f, 96f), new Vector3(0.12f, 0.9f, 0.1f), gold);
            for (int i = 0; i < 3; i++)
                BoxC(g, $"Barco_Onda_{i + 1}", new Vector3(36.06f, -13.1f + i * 0.0f, 95.2f + i * 0.8f), new Vector3(0.12f, 0.1f, 0.5f), gold);
            Light(g, "Luz_Barco_Junco", new Vector3(37.2f, -12f, 96f), new Color(1f, 0.75f, 0.45f), 1.5f, 5);
            Box(g, "Ante_Parede_Sul_Oeste", new Vector3(36, -14, 79), new Vector3(40, -5, 80), rock);
            Box(g, "Ante_Parede_Sul_Leste", new Vector3(50, -14, 79), new Vector3(54, -5, 80), rock);
            Box(g, "Ante_Parede_Norte", new Vector3(36, -14, 102), new Vector3(54, -5, 103), rock);

            // A porta com o Selo de Kur, rachada: lápis-lazúli e ouro brilhando pelas frestas.
            BoxC(g, "Porta_Selo_de_Kur", new Vector3(45, -10.5f, 101.8f), new Vector3(6, 7, 0.3f), lapis);
            BoxC(g, "Selo_Ouro", new Vector3(45, -10, 101.6f), new Vector3(2, 2, 0.1f), gold);
            Light(g, "Luz_Selo", new Vector3(45, -10, 100), new Color(0.35f, 0.55f, 1f), 3, 12);

            // Andaimes em dois níveis na arena, com rampas (a luta usa altura e barulho).
            // Sem canto que prenda a Elena (vale para o Lullu e para o Primeiro Servo): as plataformas
            // vão até a parede (sem fresta estreita), cada uma tem duas rampas (dá para dar a volta
            // por cima) e os cantos da sala são cortados em diagonal. O menu Nibiru > Testes > Conferir
            // cantos sem saída (Poço) mostra o que ainda prende.
            Box(g, "Plataforma_Oeste", new Vector3(36, -14, 84), new Vector3(43, -11, 90), wood);
            Ramp(g, "Rampa_Plataforma_Oeste", new Vector3(37.5f, -11, 90), new Vector3(37.5f, -14, 96), 3, wood);
            Ramp(g, "Rampa_Plataforma_Oeste_2", new Vector3(43, -11, 86), new Vector3(47.5f, -14, 86), 2, wood);
            Box(g, "Plataforma_Leste", new Vector3(47, -14, 92), new Vector3(54, -10, 102), wood);
            Ramp(g, "Rampa_Plataforma_Leste", new Vector3(50, -10, 92), new Vector3(50, -14, 84), 2, wood);
            Ramp(g, "Rampa_Plataforma_Leste_2", new Vector3(47, -10, 96), new Vector3(41, -14, 96), 2, wood);
            foreach (var (name, corner) in new[]
            {
                ("Ante_Canto_SO", new Vector3(36, 80)), ("Ante_Canto_SE", new Vector3(54, 80)),
                ("Ante_Canto_NO", new Vector3(36, 102)), ("Ante_Canto_NE", new Vector3(54, 102)),
                ("Poco_Canto_SO", new Vector3(40, 70)), ("Poco_Canto_SE", new Vector3(50, 70)),
                ("Ante_Canto_Plat_O", new Vector3(36, 84)), ("Ante_Canto_Plat_L", new Vector3(54, 92)),
            })
                BoxC(g, name, new Vector3(corner.x, -9.5f, corner.y), new Vector3(3f, 9f, 3f), rock, 45f);
            // Sob a 4ª rampa da descida, junto da parede sul do poço, ficava um corredor sem saída: a guarda o fecha.
            // O mesmo no pé da 3ª rampa (canto sudeste do poço): um pilar de andaime fecha o vão.
            Box(g, "Patamar_3_Base", new Vector3(48, -14, 70), new Vector3(50, -10.8f, 73.5f), wood);
            Box(g, "Lance_4_Guarda", new Vector3(44, -14, 72), new Vector3(48.5f, -10, 72.25f), wood);
            Light(g, "Luz_Holofote_1", new Vector3(40, -7, 86), new Color(1f, 0.86f, 0.6f), 4, 14);
            Light(g, "Luz_Holofote_2", new Vector3(50, -7, 96), new Color(1f, 0.86f, 0.6f), 4, 14);
            Light(g, "Luz_Poco", new Vector3(45, -6, 75), new Color(1f, 0.9f, 0.7f), 3, 16);
            Marker(g, "Encontro_Lullu_1", new Vector3(44, -13, 74), 0);
            Marker(g, "Chefe_Primeiro_Servo", new Vector3(45, -13, 92), 180);
        }

        // ---------- 6. Passagem dos Guardiões e Wadi ----------

        static void BuildPassageAndWadi(Transform g)
        {
            // Túnel estreito saindo da antecâmara para oeste e depois para o norte, teto baixo.
            Box(g, "Tunel_Chao_Entrada", new Vector3(30, -15, 94), new Vector3(36, -14, 98), rock);
            Box(g, "Tunel_Chao", new Vector3(30, -15, 98), new Vector3(34, -14, 110), rock);
            Box(g, "Tunel_Teto", new Vector3(29, -10, 93), new Vector3(35, -9, 110), rock);
            Box(g, "Tunel_Parede_Sul", new Vector3(29, -14, 93), new Vector3(35, -10, 94), rock);
            Box(g, "Tunel_Parede_Oeste", new Vector3(29, -14, 94), new Vector3(30, -10, 110), rock);
            Box(g, "Tunel_Parede_Leste", new Vector3(34, -14, 98), new Vector3(35, -10, 110), rock);
            BoxC(g, "Inscricao_Guardioes", new Vector3(30.05f, -12, 104), new Vector3(0.1f, 1, 1.6f), gold);
            Marker(g, "Fita_Irena_1", new Vector3(31, -13.5f, 107), 0);
            Light(g, "Luz_Tunel", new Vector3(32, -10.5f, 102), new Color(1f, 0.75f, 0.45f), 2, 8);

            // Rampa subindo ao wadi, com paredes de terra dos lados.
            Ramp(g, "Tunel_Rampa_Saida", new Vector3(32, -14, 110), new Vector3(32, 0, 140), 4, earth);
            Box(g, "Rampa_Parede_Oeste", new Vector3(29, -14, 110), new Vector3(30, 0, 140), earth);
            Box(g, "Rampa_Parede_Leste", new Vector3(34, -14, 110), new Vector3(35, 0, 140), earth);

            // Wadi: leito de rio seco entre duas margens altas.
            Box(g, "Wadi_Margem_Oeste", new Vector3(18, 0, 112), new Vector3(24, 3, 150), earth);
            Box(g, "Wadi_Margem_Leste", new Vector3(42, 0, 112), new Vector3(48, 3, 150), earth);
            BoxC(g, "Caminhonete_Samir", new Vector3(33, 1, 146), new Vector3(2.2f, 2, 5), sand);
            Palm(g, "Palmeira_Wadi_1", new Vector3(26, 0, 132));
            Palm(g, "Palmeira_Wadi_2", new Vector3(40, 0, 126));
            Marker(g, "Escolha_de_Faccao", new Vector3(33, 0.5f, 142), 0);
        }

        // ---------- Malha de navegação ----------

        // A malha por onde a IA anda é feita a partir das colisões do blockout e guardada ao lado
        // da cena, para continuar valendo depois de reconstruir o mapa.
        static void BakeNavMesh(Transform root)
        {
            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            string scenePath = root.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath)) return;
            string folder = scenePath.Substring(0, scenePath.Length - ".unity".Length);
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            string dataPath = folder + "/NavMesh-Blockout.asset";
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
        }

        // ---------- Rótulos das áreas ----------

        static void BuildLabels(Transform g)
        {
            Label(g, "1 · ACAMPAMENTO", new Vector3(0, 14, -110));
            Label(g, "2 · TRINCHEIRAS", new Vector3(0, 10, -62));
            Label(g, "3 · ALDEIA", new Vector3(-56, 16, -20));
            Label(g, "4 · ZIGURATE E BASE", new Vector3(10, 26, 20));
            Label(g, "5 · BOCA DO POÇO", new Vector3(45, 12, 75));
            Label(g, "6 · WADI", new Vector3(33, 10, 130));
        }

        // ---------- Peças ----------

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // Laje de chão de 1 m de espessura com o topo em y = 0.
        static void Slab(Transform parent, string name, float minX, float maxX, float minZ, float maxZ, Material m) =>
            Box(parent, name, new Vector3(minX, -1, minZ), new Vector3(maxX, 0, maxZ), m);

        // Caixa pelos cantos (mínimo e máximo), o jeito mais fácil de pensar em paredes e pisos.
        static GameObject Box(Transform parent, string name, Vector3 min, Vector3 max, Material m) =>
            BoxC(parent, name, (min + max) / 2, max - min, m);

        static GameObject BoxC(Transform parent, string name, Vector3 center, Vector3 size, Material m, float yaw = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Setup(go, parent, name, m);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = size;
            return go;
        }

        // Rampa entre dois pontos da superfície (do alto para baixo ou ao contrário).
        static void Ramp(Transform parent, string name, Vector3 from, Vector3 to, float width, Material m)
        {
            const float thickness = 0.4f;
            Vector3 direction = to - from;
            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Setup(go, parent, name, m);
            // Desce meia espessura para a face de cima passar pelos dois pontos.
            go.transform.SetPositionAndRotation((from + to) / 2 - rotation * Vector3.up * (thickness / 2), rotation);
            go.transform.localScale = new Vector3(width, thickness, direction.magnitude);
        }

        // Barraca por dentro: três paredes, frente com vão de porta e teto. Sem giro, a porta fica
        // virada para o norte; "yaw" gira a barraca inteira (-90 = porta para o oeste, 180 = para o sul).
        // "floorCenter" é o centro do chão; "size" é largura, altura e fundo (antes de girar).
        static void HollowTent(Transform parent, string name, Vector3 floorCenter, Vector3 size, float doorWidth, Material m, float yaw = 0f)
        {
            const float wall = 0.12f;
            Transform tent = new GameObject(name).transform;
            tent.SetParent(parent, false);
            tent.SetPositionAndRotation(floorCenter, Quaternion.Euler(0f, yaw, 0f));
            float halfW = size.x / 2, halfD = size.z / 2, y = size.y / 2;
            float sideWidth = (size.x - doorWidth) / 2;

            void Piece(string piece, Vector3 local, Vector3 pieceSize) => BoxC(tent, piece, tent.TransformPoint(local), pieceSize, m, yaw);
            Piece("Fundo", new Vector3(0, y, -halfD + wall / 2), new Vector3(size.x, size.y, wall));
            Piece("Lado_Esquerdo", new Vector3(-halfW + wall / 2, y, 0), new Vector3(wall, size.y, size.z));
            Piece("Lado_Direito", new Vector3(halfW - wall / 2, y, 0), new Vector3(wall, size.y, size.z));
            Piece("Frente_Esquerda", new Vector3(-halfW + sideWidth / 2, y, halfD - wall / 2), new Vector3(sideWidth, size.y, wall));
            Piece("Frente_Direita", new Vector3(halfW - sideWidth / 2, y, halfD - wall / 2), new Vector3(sideWidth, size.y, wall));
            Piece("Teto", new Vector3(0, size.y + wall / 2, 0), new Vector3(size.x + 0.3f, wall, size.z + 0.3f));
        }

        static void Cyl(Transform parent, string name, Vector3 center, float radius, float height, Material m)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Setup(go, parent, name, m);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radius * 2, height / 2, radius * 2);

            // A colisão padrão do cilindro é uma cápsula, que num disco largo e baixo vira uma
            // bola enorme (e um obstáculo invisível). A malha do próprio cilindro é exata.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mesh = go.AddComponent<MeshCollider>();
            mesh.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            mesh.convex = true;
        }

        static void Sphere(Transform parent, string name, Vector3 center, float diameter, Material m)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Setup(go, parent, name, m);
            go.transform.position = center;
            go.transform.localScale = Vector3.one * diameter;
        }

        static void Palm(Transform parent, string name, Vector3 baseCenter)
        {
            Cyl(parent, name + "_Tronco", baseCenter + Vector3.up * 3, 0.25f, 6, wood);
            Sphere(parent, name + "_Copa", baseCenter + Vector3.up * 6.3f, 3, palm);
        }

        // Ponto marcado para conteúdo que entra depois (itens, inimigos, gatilhos). Sem colisão.
        static void Marker(Transform parent, string name, Vector3 position, float yaw)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Marca_" + name;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
            go.GetComponent<Renderer>().sharedMaterial = gold;
        }

        static void Light(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var light = go.AddComponent<UnityEngine.Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        static void Label(Transform parent, string text, Vector3 position)
        {
            var go = new GameObject("Rotulo_" + text);
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.font = font;
            mesh.text = text;
            mesh.fontSize = 80;
            mesh.characterSize = 0.25f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(1f, 0.85f, 0.45f);
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        static void Setup(GameObject go, Transform parent, string name, Material m)
        {
            go.name = name;
            go.transform.SetParent(parent, true);
            go.GetComponent<Renderer>().sharedMaterial = m;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ---------- Materiais ----------

        static void LoadMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Nibiru/Art/Materials", "Blockout");
            sand = Mat("BO_Areia", new Color(0.82f, 0.71f, 0.52f));
            earth = Mat("BO_Terra", new Color(0.66f, 0.50f, 0.36f));
            adobe = Mat("BO_TijoloBarro", new Color(0.87f, 0.68f, 0.44f));
            adoberDark = Mat("BO_TijoloBarroEscuro", new Color(0.72f, 0.52f, 0.34f));
            orangeTarp = Mat("BO_LonaLaranja", new Color(0.95f, 0.52f, 0.18f));
            greenTarp = Mat("BO_LonaVerde", new Color(0.38f, 0.56f, 0.32f));
            white = Mat("BO_BrancoFundacao", new Color(0.94f, 0.94f, 0.92f));
            turquoise = Mat("BO_TurquesaFundacao", new Color(0.18f, 0.74f, 0.72f));
            doorBlue = Mat("BO_PortaAzul", new Color(0.12f, 0.55f, 0.75f));
            wood = Mat("BO_Madeira", new Color(0.55f, 0.38f, 0.22f));
            metal = Mat("BO_Metal", new Color(0.55f, 0.6f, 0.66f), 0.5f);
            palm = Mat("BO_Palmeira", new Color(0.30f, 0.55f, 0.25f));
            rock = Mat("BO_Rocha", new Color(0.52f, 0.44f, 0.40f));
            olive = Mat("BO_VerdeOliva", new Color(0.42f, 0.45f, 0.28f));
            lapis = Mat("BO_LapisLazuli", new Color(0.12f, 0.25f, 0.75f), 0.7f, new Color(0.25f, 0.5f, 1.8f));
            gold = Mat("BO_Ouro", new Color(1f, 0.8f, 0.3f), 0.7f, new Color(1.6f, 1.2f, 0.4f));
            AssetDatabase.SaveAssets();
        }

        static Material Mat(string name, Color color, float smoothness = 0.15f, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
