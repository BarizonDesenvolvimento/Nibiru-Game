using System.Linq;
using Nibiru.Dialogue;
using Nibiru.Factions;
using Nibiru.Inventory;
using Nibiru.Memories;
using Nibiru.Quests;
using Nibiru.Sequences;
using Nibiru.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Nibiru.EditorTools.Content
{
    // Dentro do namespace, para as ferramentas do kit valerem antes de nomes como Nibiru.Dialogue.
    using static ContentKit;

    /// <summary>
    /// Povoa a Aldeia (área 3 do Sítio de Ur). Passo 6 da missão "Sobrevivente": na entrada da
    /// Aldeia, um soldado encurrala a Elena, o Samir o derruba com a faca, conversa com ela
    /// ("O guia") e a leva até o pátio, diante da casa do altar. Passo 7: na casa do altar estão
    /// os sobreviventes (Yusuf e Umm Karim; começa "Remédio para Yusuf") e o Samir mostra o nicho
    /// dos Guardiões, que vira altar de salvamento. Passo 8: no nicho, a Elena lê a Tabuleta do
    /// Junco (a tabuleta reage com o azul de Kur), vive a memória de Ziusudra (cena
    /// Memoria_Shuruppak, montada pelo menu da memória), ganha o Eco do Abzu e a marca no pulso,
    /// e responde ao Samir, que está com a mão na faca. Passo 9: da beira norte da Aldeia, os dois
    /// veem a Fundação fechar as estradas; o Samir conta da passagem dos Guardiões no Poço e vai
    /// sozinho pelos canais do sul; a Elena passa pela patrulha da estrada e entra na base.
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

        // Casa do altar do blockout: aberta para o pátio (norte, z = -26), paredes por dentro de
        // x = -49,5 a -44,5 e até z = -31,5 no fundo, onde fica o nicho (frente em z = -30,7).
        static readonly Vector3 NicheFront = new Vector3(-47f, 0f, -30.7f);
        const float NicheTop = 1f;

        // Dentro da casa: o Samir encostado na parede leste, olhando para o oeste (para os sobreviventes).
        static readonly Vector3 SamirInHouse = new Vector3(-45.4f, 0f, -28.6f);
        const float SamirInHouseYaw = 270f;

        // Yusuf deitado na esteira, junto à parede oeste; Umm Karim em pé, perto da cabeça dele.
        static readonly Vector3 YusufSpot = new Vector3(-48.7f, 0f, -29.3f);
        static readonly Vector3 UmmKarimSpot = new Vector3(-47.9f, 0f, -30.7f);

        // Lamparina: luz quente, amarela e alaranjada. É a marca dos altares dos Guardiões (o azul é de Kur e dos deuses).
        static readonly Color LampLight = new Color(1f, 0.62f, 0.28f);
        static readonly Color FlameColor = new Color(1f, 0.55f, 0.15f);
        const string FlameMaterialPath = "Assets/_Nibiru/Art/Materials/Blockout/BO_Chama.mat";
        const float LampIntensity = 1.2f;

        // Passo 8: o azul de lápis-lazúli da tabuleta, a cor de Kur e dos deuses.
        static readonly Color KurBlue = new Color(0.15f, 0.35f, 1f);
        const float KurIntensity = 4.5f;

        // Na conversa depois da tabuleta, o Samir fica ao lado do nicho, de frente para a Elena.
        static readonly Vector3 SamirTalkSpot = new Vector3(-45.9f, 0f, -30.1f);

        // Face de dentro da parede do fundo da casa do altar.
        const float BackWallInside = -31.5f;

        // Passo 9. Beira norte da Aldeia, entre a mesquita e a estrada da base: o Samir olha a estrada.
        static readonly Vector3 LookoutSpot = new Vector3(-44.5f, 0f, 8f);
        static readonly Vector3 LookoutZoneCenter = new Vector3(-45.5f, 1f, 6f);
        static readonly Vector3 LookoutZoneSize = new Vector3(10f, 2f, 8f);

        // Ponto de retorno depois do plano, com a Elena virada para a estrada (leste), a mais de
        // 18 m (o alcance da visão dos soldados) da linha da patrulha.
        static readonly Vector3 CheckpointCenter = new Vector3(-45.5f, 1f, 5f);
        static readonly Vector3 CheckpointSize = new Vector3(12f, 2f, 10f);
        static readonly Vector3 CheckpointSpawn = new Vector3(-47.5f, 0f, 6.5f);

        // O Samir sai pelo sul da Aldeia, rumo aos canais secos.
        static readonly Vector3 SamirExitSpot = new Vector3(-52f, 0f, -46f);

        // Barreira da Fundação na estrada do norte: do oeste da Aldeia até o muro da base (x = -24).
        const float BarrierZ = 41.5f;
        const float BarrierWestX = -90f;
        const float BarrierEastX = -24f;
        static readonly Vector3 GuardSpot = new Vector3(-46f, 0f, 43.5f);
        static readonly Vector3[] TruckPath = { new Vector3(-95f, 0f, 46f), new Vector3(-48f, 0f, 46f) };

        // Patrulha rente ao muro da base, entre a ponta sul (z = -4) e o meio do muro.
        static readonly Vector3 PatrolSouth = new Vector3(-28f, 0f, -2f);
        static readonly Vector3 PatrolNorth = new Vector3(-28f, 0f, 32f);

        // Helicóptero: entra pelo sudoeste, cruza a Aldeia e pousa no heliponto da base.
        static readonly Vector3[] HeliPath = { new Vector3(-75f, 32f, -70f), new Vector3(-20f, 28f, -15f), new Vector3(40f, 8f, 12f), new Vector3(40f, 0f, 12f) };

        // Entrada da base: depois da ponta sul do muro, já dentro da base.
        static readonly Vector3 BaseZoneCenter = new Vector3(-13f, 1f, -8f);
        static readonly Vector3 BaseZoneSize = new Vector3(16f, 2f, 24f);

        // ---------- Dados ----------

        class Events
        {
            public StoryEvent aproximouSeDaAldeia, chegouAAldeia, chegouAoPatioComSamir,
                entrouNaCasaDoAltar, conheceuSobreviventes, samirMostrouONicho, descansouNoNicho, entregouAntibioticos,
                tocouATabuleta, decifrouTabuleta, contouAVerdadeAoSamir, mentiuAoSamir, samirRevelouGuardiao, conversouSobreATabuleta,
                chegouABeiraDaAldeia, ouviuOPlanoDeFuga, entrouNaBaseDaFundacao;
        }

        class Dialogues
        {
            public DialogueDefinition encurralada, oGuia, chegadaPatio,
                sobreviventes, oNicho, ummKarimEspera, yusufEspera,
                tabuletaDesperta, aMarca, depoisDaTabuleta,
                oCercoChega, oCerco;
        }

        static Events ev;
        static Dialogues dlg;
        static QuestDefinition yusufQuest;
        static MemoryDefinition ziusudraMemory;

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
            GameObject samir = BuildSamirEncounter(Group(root, "Encontro_Samir"));
            var (altar, lamp) = BuildSurvivorsAndAltar(Group(root, "Casa_Altar"), samir);
            BuildFirstTablet(Group(root, "Primeira_Tabuleta"), samir, altar, lamp);
            BuildEscapePlan(Group(root, "Plano_De_Fuga"), samir);
            RegisterQuest(yusufQuest);
            LockTabletUntilAltar();

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
                entrouNaCasaDoAltar = Event("EntrouNaCasaDoAltar", "A Elena entrou na casa do altar, onde os sobreviventes estão escondidos. Começa a conversa \"Os sobreviventes\"."),
                conheceuSobreviventes = Event("ConheceuSobreviventes", "A Elena conheceu Yusuf e Umm Karim na casa do altar. Começa \"Remédio para Yusuf\"."),
                samirMostrouONicho = Event("SamirMostrouONicho", "O Samir mostrou o nicho dos Guardiões e acendeu a lamparina. O nicho passa a ser altar de salvamento."),
                descansouNoNicho = Event("DescansouNoNicho", "A Elena descansou no nicho dos Guardiões (altar de salvamento da Aldeia)."),
                entregouAntibioticos = Event("EntregouAntibioticos", "A Elena entregou os antibióticos ao Yusuf (a entrega entra num passo seguinte)."),
                tocouATabuleta = Event("TocouATabuleta", "A Elena pousou a Tabuleta do Junco no nicho e começou a ler. A tabuleta reage com o azul de Kur e a lamparina encolhe."),
                decifrouTabuleta = Event("DecifrouTabuletaDoJunco", "A Elena decifrou a primeira tabuleta (passo 8 da missão principal)."),
                contouAVerdadeAoSamir = Event("ContouAVerdadeAoSamir", "Depois da tabuleta, a Elena contou ao Samir que ouviu uma voz (Guardiões +5)."),
                mentiuAoSamir = Event("MentiuAoSamir", "Depois da tabuleta, a Elena mentiu ao Samir (\"só li um mito\"). Ele percebeu e vai lembrar disso mais tarde."),
                samirRevelouGuardiao = Event("SamirRevelouGuardiao", "A Elena perguntou por que ele estava com a mão na faca, e o Samir revelou que é Guardião."),
                conversouSobreATabuleta = Event("ConversouSobreATabuleta", "A Elena respondeu ao Samir depois de ler a tabuleta. Fecha o passo 8."),
                chegouABeiraDaAldeia = Event("ChegouABeiraDaAldeia", "Seguindo o Samir, a Elena chegou à beira norte da Aldeia. Começa a cena do cerco (passo 9)."),
                ouviuOPlanoDeFuga = Event("OuviuOPlanoDeFuga", "O Samir contou da passagem dos Guardiões no Poço e foi embora pelos canais do sul. A Fundação fechou as estradas."),
                entrouNaBaseDaFundacao = Event("EntrouNaBaseDaFundacao", "A Elena passou pela patrulha da estrada e entrou na base da Fundação, no pé do zigurate. Fecha o passo 9."),
            };
            ziusudraMemory = ShuruppakMemoryContent.MemoryData();

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

                sobreviventes = Dialogue("Aldeia_Sobreviventes", ev.conheceuSobreviventes,
                    N("u1", "Mulher", "Quem está aí? Fora! Não temos mais nada!", "s1"),
                    N("s1", "Samir", "Calma, Umm Karim. Sou eu. Ela é da equipe dos estrangeiros. Está sozinha.", "u2"),
                    N("u2", "Umm Karim", "Da equipe... Então foi a sua gente que abriu aquele buraco no chão.", "e1"),
                    N("e1", "Elena", "Nós escavamos onde a Fundação mandou. Quem é ele?", "u3"),
                    N("u3", "Umm Karim", "Yusuf. Cavava com vocês. Voltou do poço com o braço aberto, e agora a ferida cheira mal. A febre não baixa.", "y1"),
                    N("y1", "Yusuf", "(Ele fala de olhos fechados.) Doutora... lá embaixo... eu ouvi alguém respirando. Debaixo das pedras.", "u4"),
                    N("u4", "Umm Karim", "Esta aldeia tinha medo daquele lugar muito antes da Fundação chegar. Meu avô dizia que lá embaixo alguém dorme. E que não se acorda quem dorme ali.", "e2"),
                    N("e2", "Elena", "A ferida infeccionou. Ele precisa de antibióticos, senão a infecção se espalha.", "u5"),
                    N("u5", "Umm Karim", "Os soldados levaram tudo. Se quer ajudar, ache o remédio. Lá fora, onde nós não podemos ir.", "s2"),
                    N("s2", "Samir", "A Fundação tem uma tenda médica no pé do zigurate. Tem remédio lá. E tem guardas.")),

                oNicho = Dialogue("Aldeia_ONicho", ev.samirMostrouONicho,
                    N("s1", "Samir", "Venha ver uma coisa. (Ele se ajoelha diante de um nicho na parede do fundo: tijolo cozido, muito mais antigo que a casa. Na pedra, um barco de junco sobre ondas.)", "e1"),
                    N("e1", "Elena", "Isso é muito mais velho que a casa. Construíram a casa em volta dele?", "s2"),
                    N("s2", "Samir", "Gente que sabia guardar segredo. A Fundação revirou a aldeia inteira e passou por aqui sem ver.", "s3"),
                    N("s3", "Samir", "(Ele acende uma lamparina de barro e a deixa sobre o nicho.) Aqui você pode descansar. Se der errado lá fora, é para cá que você volta.", "e2"),
                    N("e2", "Elena", "(A tabuleta fica morna dentro da bolsa, como no acampamento.)", "s4"),
                    N("s4", "Samir", "Descanse. Depois a gente fala sobre o que você carrega nessa bolsa.")),

                ummKarimEspera = Dialogue("Aldeia_UmmKarimEspera", null,
                    N("u1", "Umm Karim", "Ache o remédio, doutora. O menino não tem muitos dias.")),

                yusufEspera = Dialogue("Aldeia_YusufEspera", null,
                    N("y1", "Yusuf", "(Ele treme de febre.) Está tão quente... ou tão frio. Não sei mais.")),

                tabuletaDesperta = Dialogue("Aldeia_ATabuletaDesperta", null,
                    N("e1", "Elena", "(Ela pousa a tabuleta no nicho, ao lado da lamparina. Sob os dedos dela, as linhas de ouro acendem uma a uma, e a pedra se enche de uma luz azul que não é deste mundo.)", "s1"),
                    N("s1", "Samir", "(Ele dá um passo para trás. A chama da lamparina encolhe.) Doutora... não.", "v1"),
                    N("v1", "Voz", "Tila. Você está viva, e está segura. Não tenha medo.", "v2"),
                    N("v2", "Voz", "Agora, olhe.")),

                aMarca = Dialogue("Aldeia_AMarca", null,
                    N("e1", "Elena", "(Ela pisca. A casa, a lamparina, o Samir. Passou só um minuto. No pulso esquerdo, uma marca azul, fina como tinta debaixo da pele. Não sai quando ela esfrega.)", "v1"),
                    N("v1", "Voz", "Não tenha medo da marca. Ela só quer dizer que eu estou com você.")),

                depoisDaTabuleta = Dialogue("Aldeia_DepoisDaTabuleta", ev.conversouSobreATabuleta,
                    N("s1", "Samir", "(Ele está de pé ao lado do nicho, perto demais, olhando para você. A mão direita está no cabo da faca.)", "s2"),
                    N("s2", "Samir", "Você ficou um minuto inteiro de olhos abertos, falando uma língua que ninguém fala há quatro mil anos. O que você ouviu?", null,
                        new DialogueChoice
                        {
                            text = "Uma voz. Ela salvou um homem do dilúvio... e falou comigo como se me conhecesse.",
                            next = "verdade",
                            raise = ev.contouAVerdadeAoSamir,
                            reputation = new[] { new ReputationChange { faction = Load<FactionDefinition>("Factions/Guardioes"), amount = 5 } },
                        },
                        C("Nada. Só li o texto. É um mito do dilúvio.", "mentira", ev.mentiuAoSamir),
                        C("Por que você está com a mão na faca?", "faca", ev.samirRevelouGuardiao)),
                    N("verdade", "Samir", "(A mão dele afrouxa no cabo, mas não sai dele.) Obrigado por não mentir. Pouca gente consegue, depois de ouvir essa voz.", "fim"),
                    N("mentira", "Samir", "(Ele olha a marca azul no seu pulso, depois os seus olhos. Demora a responder.) Se você diz.", "fim"),
                    N("faca", "Samir", "Porque eu sou um Guardião. Minha família vigia este lugar desde antes de Ur ter nome. Juramos que nada do que dorme lá embaixo volta a andar por aqui.", "faca2"),
                    N("faca2", "Samir", "Quem ouve essa voz muda, doutora. Devagar. Se um dia ela falar mais alto que você, eu vou perceber.", "faca3"),
                    N("faca3", "Samir", "(Ele tira a mão da faca.) Hoje você ainda é você.", "fim"),
                    N("fim", "Samir", "Guarde essa pedra onde ninguém veja. A Fundação está fechando as estradas, e nós dois precisamos sair desta aldeia.")),

                oCercoChega = Dialogue("Aldeia_OCercoChega", null,
                    N("s1", "Samir", "(Ele se encosta na parede da última casa e aponta para o norte, para a estrada.) Abaixe-se. Olhe.")),

                oCerco = Dialogue("Aldeia_OCerco", ev.ouviuOPlanoDeFuga,
                    N("s1", "Samir", "Fecharam a estrada do norte. A do rio também: ouvi os caminhões de madrugada. E olhe o helicóptero descendo na base.", "e1"),
                    N("e1", "Elena", "Então estamos presos.", "s2"),
                    N("s2", "Samir", "A aldeia está. Nós, não. Existe um caminho mais velho que qualquer estrada. Começa lá embaixo, no Poço, e sai no leito seco do wadi, longe do cerco. Minha caminhonete está lá.", "e2"),
                    N("e2", "Elena", "No Poço? Onde a minha equipe morreu?", "s3"),
                    N("s3", "Samir", "Na antecâmara tem uma parede com o barco de junco, igual ao do nicho. A passagem fica atrás dela.", "s4"),
                    N("s4", "Samir", "Mas para chegar ao Poço, você vai ter que atravessar a base deles, no pé do zigurate.", null,
                        C("Você não vem comigo?", "s5", null),
                        new DialogueChoice { text = "Os Guardiões sempre souberam desse caminho?", next = "g1", requires = ev.samirRevelouGuardiao }),
                    N("g1", "Samir", "Nós cavamos esse caminho, doutora. Há muito tempo. Para sair lá de baixo... ou para levar alguma coisa de volta.", "s5"),
                    N("s5", "Samir", "Sozinho, eu passo pelos canais secos do sul até o wadi. Com você e essa pedra acendendo a cada passo, não. Eu espero você na caminhonete.", "s6"),
                    N("s6", "Samir", "A tenda médica também fica na base. Se ainda quer salvar o Yusuf, o remédio está lá.", "s7"),
                    N("s7", "Samir", "A patrulha anda rente ao muro. Espere os dois virarem de costas e contorne a ponta sul do muro.")),
            };

            var antibiotics = Asset<ItemDefinition>("Items/Antibioticos", item =>
            {
                item.displayName = "Antibióticos";
                item.description = "Uma caixa de amoxicilina com o selo da Fundação. Para a infecção do Yusuf.";
                item.category = ItemCategory.Chave;
            });

            yusufQuest = Asset<QuestDefinition>("Quests/RemedioParaYusuf", quest =>
            {
                quest.displayName = "Remédio para Yusuf";
                quest.kind = QuestKind.Secundaria;
                quest.startOn = ev.conheceuSobreviventes;
                quest.steps = new[]
                {
                    Step("Consiga antibióticos para o Yusuf", item: antibiotics),
                    Step("Leve os antibióticos ao Yusuf", ev.entregouAntibioticos),
                };
            });

            var mainQuest = Load<QuestDefinition>("Quests/Sobrevivente");
            if (mainQuest == null)
                Debug.LogWarning("Missão Sobrevivente não encontrada: rode antes o menu do Acampamento.");
            else
                AppendSteps(mainQuest,
                    Step("Siga o Samir até a Aldeia", ev.chegouAoPatioComSamir),
                    Step("Siga o Samir até a casa", ev.conheceuSobreviventes),
                    Step("Descanse junto ao nicho antigo (E ou A)", ev.descansouNoNicho),
                    Step("Leia a tabuleta no nicho", ShuruppakMemoryContent.EnteredMemory()),
                    Step("Vá até a cabana de junco, à beira do rio", ShuruppakMemoryContent.HeardTheReedWall()),
                    Step("Responda ao Samir", ev.conversouSobreATabuleta),
                    Step("Siga o Samir até a beira da aldeia", ev.chegouABeiraDaAldeia),
                    Step("Passe pela patrulha da estrada e entre na base da Fundação, no pé do zigurate", ev.entrouNaBaseDaFundacao));

            AssetDatabase.SaveAssets();
        }

        // ---------- Encontro com o Samir (passo 6) ----------

        static GameObject BuildSamirEncounter(Transform g)
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
            // Perto o bastante do soldado para a facada alcançar, logo atrás do corpo no chão.
            GameObject samir = Samir(scene, origin - forward * 1.1f, samirDestination);
            var samirAnimator = SamirAnimator(samir);

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
                S("Samir surge atrás do soldado e dá a facada", 0.4f, e =>
                {
                    SetActive(e, samir, true);
                    if (samirAnimator != null) Call(e, samirAnimator.Strike);
                }),
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
            return samir;
        }

        // ---------- Sobreviventes e o nicho dos Guardiões (passo 7) ----------

        // Devolve o altar (o nicho com o Checkpoint) e a luz da lamparina, usados pelo passo 8.
        static (GameObject altar, Light lamp) BuildSurvivorsAndAltar(Transform g, GameObject samir)
        {
            Transform samirPoint = Point(g, "Samir_NaCasa", OnNavMesh(SamirInHouse, "Samir_NaCasa", 0.4f));
            samirPoint.rotation = Quaternion.Euler(0f, SamirInHouseYaw, 0f);

            // Yusuf deitado numa esteira, Umm Karim em pé ao lado (cápsulas, por enquanto).
            Prim(PrimitiveType.Cube, g, "Esteira_Yusuf", YusufSpot + Vector3.up * 0.02f, new Vector3(1f, 0.04f, 2.1f), "BO_LonaLaranja", collider: ColliderMode.None);
            GameObject yusuf = Body(g, "Yusuf", YusufSpot + Vector3.up * 0.04f, 0f, "BO_Areia");
            GameObject ummKarim = Prim(PrimitiveType.Capsule, g, "Umm_Karim", UmmKarimSpot + Vector3.up * 0.8f, new Vector3(0.55f, 0.8f, 0.55f), "BO_Madeira", 40f);

            // Conversas curtas depois do encontro, quantas vezes a Elena quiser.
            GameObject talkUmm = Zone(g, "Falar_UmmKarim", UmmKarimSpot + Vector3.up * 0.8f, new Vector3(1.2f, 1.6f, 1.2f));
            SetField(DialogueStarterOn(talkUmm, dlg.ummKarimEspera, "Falar com Umm Karim", interactable: true), "onlyOnce", false);
            talkUmm.SetActive(false);
            GameObject talkYusuf = Zone(g, "Falar_Yusuf", YusufSpot + Vector3.up * 0.4f, new Vector3(0.9f, 0.8f, 1.8f));
            SetField(DialogueStarterOn(talkYusuf, dlg.yusufEspera, "Falar com Yusuf", interactable: true), "onlyOnce", false);
            talkYusuf.SetActive(false);

            GameObject lampLight = NicheMark(g);

            // O altar: interagir com o nicho marca o ponto de retorno, de frente para ele.
            GameObject altar = Zone(g, "Altar_Nicho", NicheFront + new Vector3(0f, 0.6f, 0.15f), new Vector3(1.2f, 1.2f, 0.6f));
            Transform spawn = Point(altar.transform, "Retorno", NicheFront + new Vector3(0f, 0f, 1.4f));
            spawn.rotation = Quaternion.Euler(0f, 180f, 0f);
            var checkpoint = altar.AddComponent<Save.Checkpoint>();
            SetField(checkpoint, "activation", Save.Checkpoint.Activation.Altar);
            SetField(checkpoint, "spawnPoint", spawn);
            SetField(checkpoint, "altarPrompt", "Descansar no nicho");
            SetField(checkpoint, "raiseOnActivate", ev.descansouNoNicho);
            StableId(altar);
            altar.SetActive(false);

            // Do pátio, o Samir entra na casa (meio segundo depois do evento, para correr depois da
            // fala do pátio e depois do Samir ser posto no pátio ao voltar de um checkpoint).
            var walker = samir.GetComponent<WalkToPoint>();
            var walkOrSnap = g.gameObject.AddComponent<StoryEventBranch>();
            SetField(walkOrSnap, "storyEvent", ev.conheceuSobreviventes);
            Call(GetField<UnityEvent>(walkOrSnap, "ifRaised"), walker.SnapTo, samirPoint);
            Call(GetField<UnityEvent>(walkOrSnap, "ifNotRaised"), walker.WalkTo, samirPoint);
            var intoHouse = g.gameObject.AddComponent<TimedSequence>();
            SetSteps(intoHouse, S("Samir entra na casa do altar", 0.5f, e => Call(e, walkOrSnap.Run)));

            // No fundo da casa, os sobreviventes se assustam com a Elena (só depois do pátio).
            GameObject house = EventZone(g, "Gatilho_CasaAltar", new Vector3(-47f, 1f, -29.3f), new Vector3(4.6f, 2f, 3.4f), ev.entrouNaCasaDoAltar);
            house.SetActive(false);
            Listener(g.gameObject, ev.chegouAoPatioComSamir, e =>
            {
                Call(e, intoHouse.Play);
                SetActive(e, house, true);
            });
            var survivors = DialogueTrigger(g, "Dialogo_Sobreviventes", dlg.sobreviventes);
            Listener(survivors.gameObject, ev.entrouNaCasaDoAltar, e => Call(e, survivors.StartDialogue));
            SetField(survivors.GetComponent<StoryEventListener>(), "replayOnLoad", false);

            // Logo depois, o Samir mostra o nicho e acende a lamparina.
            var niche = DialogueTrigger(g, "Dialogo_ONicho", dlg.oNicho);
            var showNiche = niche.gameObject.AddComponent<TimedSequence>();
            SetField(showNiche, "skipIfRaised", ev.samirMostrouONicho);
            SetSteps(showNiche, S("Samir mostra o nicho", 0.6f, e => Call(e, niche.StartDialogue)));
            Listener(niche.gameObject, ev.conheceuSobreviventes, e =>
            {
                Call(e, showNiche.Play);
                SetActive(e, talkUmm, true);
                SetActive(e, talkYusuf, true);
            });

            // Lamparina acesa = altar pronto (ao vivo e ao voltar de um checkpoint).
            Listener(g.gameObject, ev.samirMostrouONicho, e =>
            {
                SetActive(e, lampLight, true);
                SetActive(e, altar, true);
            });
            return (altar, lampLight.GetComponentInChildren<Light>(true));
        }

        // ---------- A primeira tabuleta (passo 8) ----------

        static void BuildFirstTablet(Transform g, GameObject samir, GameObject altar, Light lamp)
        {
            var tablet = Load<Tablets.TabletDefinition>("Tablets/TabuletaDoJunco");
            var reader = Object.FindFirstObjectByType<Tablets.TabletReader>();
            if (reader == null) Debug.LogWarning("Aldeia: TabletReader não encontrado. A Elena está na cena?");

            // A tabuleta pousada no nicho e a luz azul dela; a lamparina treme e encolhe quando o azul sobe.
            GameObject stone = TabletOnNiche(g);
            var kurFade = stone.GetComponentInChildren<LightFade>(true);
            var lampFade = lamp.gameObject.AddComponent<LightFade>();
            SetField(lampFade, "duration", 2f);
            SetField(lampFade, "flicker", 0.2f);

            // Ler a tabuleta no mesmo lugar do altar. Entre descansar e ler, o altar sai do ar (só
            // o componente: quem liga o objeto é a lamparina acesa), para o aviso ser um só. A
            // área avança na direção da Elena, para ganhar da conversa com a Umm Karim, ao lado.
            GameObject read = Zone(g, "Ler_Tabuleta", NicheFront + new Vector3(0f, 0.6f, 0.5f), new Vector3(1.2f, 1.2f, 1.3f));
            read.SetActive(false);
            var checkpoint = altar.GetComponent<Save.Checkpoint>();
            var gate = g.gameObject.AddComponent<StoryEventBranch>();
            SetField(gate, "storyEvent", ev.decifrouTabuleta);
            UnityEvent ifRead = GetField<UnityEvent>(gate, "ifRaised");
            SetActive(ifRead, read, false);
            SetEnabled(ifRead, checkpoint, true);
            UnityEvent ifNotRead = GetField<UnityEvent>(gate, "ifNotRaised");
            SetActive(ifNotRead, read, true);
            SetEnabled(ifNotRead, checkpoint, false);
            Listener(g.gameObject, ev.descansouNoNicho, e => Call(e, gate.Run));
            Listener(g.gameObject, ev.decifrouTabuleta, e => Call(e, gate.Run));

            var memory = new GameObject("Memoria_Ziusudra").AddComponent<PlayableMemory>();
            memory.transform.SetParent(g, false);
            SetField(memory, "memory", ziusudraMemory);

            Transform talkPoint = Point(g, "Samir_Conversa", OnNavMesh(SamirTalkSpot, "Samir_Conversa", 0.4f));
            var walker = samir.GetComponent<WalkToPoint>();
            var awakening = DialogueTrigger(g, "Dialogo_TabuletaDesperta", dlg.tabuletaDesperta);
            var mark = DialogueTrigger(g, "Dialogo_AMarca", dlg.aMarca);
            var answer = DialogueTrigger(g, "Dialogo_DepoisDaTabuleta", dlg.depoisDaTabuleta);

            // Ao ler: a tabuleta acende em azul (a primeira vez que o azul de Kur aparece), a
            // lamparina recua, a voz fala e a memória começa.
            var readingGo = new GameObject("Sequencia_Leitura");
            readingGo.transform.SetParent(g, false);
            var touched = readingGo.AddComponent<StoryEventRaiser>();
            SetField(touched, "storyEvent", ev.tocouATabuleta);
            var reading = readingGo.AddComponent<TimedSequence>();
            SetField(reading, "skipIfRaised", ev.decifrouTabuleta);
            SetSteps(reading,
                S("a Elena pousa a tabuleta no nicho", 0f, e =>
                {
                    SetActive(e, read, false);
                    SetActive(e, stone, true);
                    Call(e, touched.Raise);
                }),
                S("a tabuleta acende em azul e a lamparina encolhe", 0.3f, e =>
                {
                    UnityEventTools.AddFloatPersistentListener(e, kurFade.FadeTo, KurIntensity);
                    UnityEventTools.AddFloatPersistentListener(e, lampFade.FadeTo, 0.25f);
                }),
                S("a voz fala", 1.4f, e => Call(e, awakening.StartDialogue)),
                S("a memória de Ziusudra começa", 0.3f, e => Call(e, memory.Enter)));
            var interact = read.AddComponent<Interaction.InteractableEvent>();
            SetField(interact, "prompt", "Ler a tabuleta");
            SetField(interact, "singleUse", true);
            Call(GetField<UnityEvent>(interact, "onInteract"), reading.Play);

            // Na volta: a leitura (poder, transformação e o texto), a marca no pulso e o Samir.
            var returnGo = new GameObject("Sequencia_Volta");
            returnGo.transform.SetParent(g, false);
            var afterwards = returnGo.AddComponent<TimedSequence>();
            SetField(afterwards, "skipIfRaised", ev.decifrouTabuleta);
            SetSteps(afterwards,
                S("a leitura: Eco do Abzu, transformação e o texto", 0.4f, e =>
                {
                    if (reader != null) Call(e, reader.Read, tablet);
                    UnityEventTools.AddFloatPersistentListener(e, kurFade.FadeTo, 0f);
                    UnityEventTools.AddFloatPersistentListener(e, lampFade.FadeTo, LampIntensity);
                }),
                S("a Elena guarda a tabuleta e vê a marca no pulso", 0.6f, e =>
                {
                    SetActive(e, stone, false);
                    Call(e, walker.SnapTo, talkPoint);
                    Call(e, mark.StartDialogue);
                }),
                S("o Samir, de mão na faca", 0.4f, e => Call(e, answer.StartDialogue)));
            Call(GetField<UnityEvent>(memory, "onReturned"), afterwards.Play);

            // Dica do poder, só ao vivo.
            var poster = returnGo.AddComponent<NotificationPoster>();
            Listener(returnGo, ev.conversouSobreATabuleta, e => Notify(e, poster, "Eco do Abzu: aperte Q (LB no controle) para ver através das paredes"));
            SetField(returnGo.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            HiddenInscription(g);
        }

        // Pedra de lápis-lazúli do tamanho de uma mão, encostada na parede em cima do nicho, ao lado
        // da lamparina, com linhas de escrita em ouro. A luz azul dela começa apagada.
        static GameObject TabletOnNiche(Transform g)
        {
            Transform stone = Group(g, "Tabuleta_NoNicho");
            stone.SetPositionAndRotation(NicheFront + new Vector3(0.3f, NicheTop + 0.1f, -0.25f), Quaternion.Euler(-12f, 0f, 0f));
            Local(PrimitiveType.Cube, stone, "Pedra", Vector3.zero, new Vector3(0.14f, 0.18f, 0.03f), "BO_LapisLazuli");
            for (int i = 0; i < 5; i++)
            {
                float width = 0.1f - (i % 2) * 0.025f;
                Local(PrimitiveType.Cube, stone, $"Linha_Ouro_{i + 1}", new Vector3(-0.005f * (i % 2), 0.064f - i * 0.032f, 0.016f), new Vector3(width, 0.008f, 0.004f), "BO_Ouro");
            }

            var kur = new GameObject("Luz_de_Kur").AddComponent<Light>();
            kur.transform.SetParent(stone, false);
            kur.transform.localPosition = new Vector3(0f, 0.08f, 0.3f);
            kur.type = LightType.Point;
            kur.color = KurBlue;
            kur.range = 6f;
            kur.intensity = 0f;
            kur.shadows = LightShadows.Soft;
            var fade = kur.gameObject.AddComponent<LightFade>();
            SetField(fade, "duration", 2f);
            SetField(fade, "flicker", 0.15f);

            stone.gameObject.SetActive(false);
            return stone.gameObject;
        }

        // Escrita cuneiforme escondida na parede do fundo, que só o Eco do Abzu acende: a recompensa
        // de usar o poder logo ali, onde ele foi ganho.
        static void HiddenInscription(Transform g)
        {
            Transform inscription = Group(g, "Inscricao_Escondida");
            inscription.position = new Vector3(-45.6f, 1.4f, BackWallInside + 0.02f);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 4; i++)
                {
                    var wedge = Local(PrimitiveType.Cube, inscription, $"Cunha_{row + 1}_{i + 1}", new Vector3(-0.3f + i * 0.2f, 0.2f - row * 0.2f, 0f), new Vector3(0.025f, 0.12f, 0.01f), "BO_Ouro");
                    wedge.transform.localRotation = Quaternion.Euler(0f, 0f, ((row * 4 + i) * 47 % 3 - 1) * 35f);
                }
            inscription.gameObject.AddComponent<Powers.EchoHiddenMark>();
        }

        // Na região, a tabuleta só se decifra no nicho. Pelo inventário, só depois (para reler).
        static void LockTabletUntilAltar()
        {
            var reader = Object.FindFirstObjectByType<Tablets.TabletReader>();
            if (reader == null) return;
            var serialized = new SerializedObject(reader);
            serialized.FindProperty("decipherFromInventory").boolValue = false;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(reader);
        }

        // ---------- Plano de fuga (passo 9) ----------

        static void BuildEscapePlan(Transform g, GameObject samir)
        {
            // Pontos conferidos antes de criar peças novas, para elas não contarem como obstáculo.
            Vector3 lookout = OnNavMesh(LookoutSpot, "Samir_Beira", 0.5f);
            Vector3 exit = OnNavMesh(SamirExitSpot, "Samir_Saida", 0.5f);
            Vector3 patrolSouth = OnNavMesh(PatrolSouth, "Patrulha_Sul", 0.5f);
            Vector3 patrolNorth = OnNavMesh(PatrolNorth, "Patrulha_Norte", 0.5f);
            Vector3 guardSpot = OnNavMesh(GuardSpot, "Guarda_Barreira", 0.5f);

            var walker = samir.GetComponent<WalkToPoint>();
            Transform samirLookout = Point(g, "Samir_Beira", lookout);
            samirLookout.rotation = Quaternion.Euler(0f, 0f, 0f);
            Transform samirExit = Point(g, "Samir_Saida", exit);

            // Depois da conversa da tabuleta, o Samir sai da casa e anda até a beira norte (ao
            // voltar de um checkpoint, ele já está lá). Os 0,6 s deixam o resto da cena se arrumar antes.
            var toLookout = g.gameObject.AddComponent<StoryEventBranch>();
            SetField(toLookout, "storyEvent", ev.chegouABeiraDaAldeia);
            Call(GetField<UnityEvent>(toLookout, "ifRaised"), walker.SnapTo, samirLookout);
            Call(GetField<UnityEvent>(toLookout, "ifNotRaised"), walker.WalkTo, samirLookout);
            var goToLookout = g.gameObject.AddComponent<TimedSequence>();
            SetField(goToLookout, "skipIfRaised", ev.ouviuOPlanoDeFuga);
            SetSteps(goToLookout, S("Samir vai até a beira da aldeia", 0.6f, e => Call(e, toLookout.Run)));

            GameObject lookoutZone = EventZone(g, "Gatilho_Beira", LookoutZoneCenter, LookoutZoneSize, ev.chegouABeiraDaAldeia);
            lookoutZone.SetActive(false);
            Listener(g.gameObject, ev.conversouSobreATabuleta, e =>
            {
                Call(e, goToLookout.Play);
                SetActive(e, lookoutZone, true);
            });

            // O cerco: caminhão, barreira com guarda, helicóptero pousando na base e a patrulha.
            GameObject truck = FoundationTruck(g, out MoveAlongPath truckMove);
            GameObject barrier = Barrier(Group(g, "Barreira_Norte"));
            Transform guards = Group(g, "Guarda_Barreira");
            Soldier(guards, "Soldado_Barreira", guardSpot, guardSpot + Vector3.back, null, leader: false);
            guards.gameObject.SetActive(false);

            Transform patrol = Group(g, "Patrulha_Estrada");
            Transform[] route = { Point(g, "Patrulha_Ponto_Sul", patrolSouth), Point(g, "Patrulha_Ponto_Norte", patrolNorth) };
            Soldier(patrol, "Soldado_Estrada_A", patrolSouth, patrolNorth, route, leader: false);
            Soldier(patrol, "Soldado_Estrada_B", patrolNorth, patrolSouth, new[] { route[1], route[0] }, leader: false);
            patrol.gameObject.SetActive(false);

            GameObject heli = LandingHelicopter(g, out MoveAlongPath heliMove);

            var notice = DialogueTrigger(g, "Dialogo_OCercoChega", dlg.oCercoChega);
            var plan = DialogueTrigger(g, "Dialogo_OCerco", dlg.oCerco);

            // Depois do plano: ponto de retorno ali mesmo e a área da base esperando a Elena.
            Transform checkpointGroup = Group(g, "Ponto_De_Retorno");
            Checkpoint(checkpointGroup, "Checkpoint_Beira", CheckpointCenter, CheckpointSize, CheckpointSpawn, 90f);
            checkpointGroup.gameObject.SetActive(false);
            GameObject baseZone = EventZone(g, "Gatilho_Base", BaseZoneCenter, BaseZoneSize, ev.entrouNaBaseDaFundacao);
            baseZone.SetActive(false);

            // Ao vivo: o Samir mostra a estrada, a Fundação fecha o cerco aos poucos e ele explica o plano.
            var siege = g.gameObject.AddComponent<TimedSequence>();
            SetField(siege, "skipIfRaised", ev.ouviuOPlanoDeFuga);
            SetSteps(siege,
                S("o Samir manda a Elena olhar a estrada", 0.3f, e => Call(e, notice.StartDialogue)),
                S("caminhão na estrada e helicóptero no céu", 0.3f, e =>
                {
                    SetActive(e, truck, true);
                    Call(e, truckMove.Play);
                    SetActive(e, heli, true);
                    Call(e, heliMove.Play);
                }),
                S("a barreira fecha a estrada, com um guarda", 5f, e =>
                {
                    SetActive(e, barrier, true);
                    SetActive(e, guards.gameObject, true);
                    WakeSoldiers(e, guards);
                }),
                S("a patrulha começa a andar rente ao muro", 1f, e =>
                {
                    SetActive(e, patrol.gameObject, true);
                    WakeSoldiers(e, patrol);
                }),
                S("conversa O cerco", 2.5f, e => Call(e, plan.StartDialogue)),
                S("o Samir vai embora pelos canais do sul, agachado por causa da patrulha", 0.5f, e =>
                {
                    Call(e, walker.LeaveTo, samirExit);
                    var samirAnimator = SamirAnimator(samir);
                    if (samirAnimator != null) Call(e, samirAnimator.Sneak);
                    SetActive(e, checkpointGroup.gameObject, true);
                    SetActive(e, baseZone, true);
                }));
            Listener(siege.gameObject, ev.chegouABeiraDaAldeia, e => Call(e, siege.Play));
            SetField(siege.GetComponents<StoryEventListener>().Last(), "replayOnLoad", false);

            // Ao voltar de um checkpoint depois do plano: tudo direto no estado final, e o Samir longe.
            var samirGone = new GameObject("Samir_Foi_Embora").AddComponent<TimedSequence>();
            samirGone.transform.SetParent(g, false);
            SetSteps(samirGone, S("o Samir já foi para o wadi", 0.8f, e => SetActive(e, samir, false)));
            Listener(g.gameObject, ev.ouviuOPlanoDeFuga, e =>
            {
                SetActive(e, truck, true);
                Call(e, truckMove.SnapToEnd);
                SetActive(e, heli, true);
                Call(e, heliMove.SnapToEnd);
                SilenceHelicopter(e, heli);
                SetActive(e, barrier, true);
                SetActive(e, guards.gameObject, true);
                WakeSoldiers(e, guards);
                SetActive(e, patrol.gameObject, true);
                WakeSoldiers(e, patrol);
                SetActive(e, checkpointGroup.gameObject, true);
                SetActive(e, baseZone, true);
                Call(e, samirGone.Play);
            });
            SetField(g.GetComponents<StoryEventListener>().Last(), "onlyOnLoad", true);
        }

        // Caminhão branco da Fundação com faixa turquesa, chegando pela estrada do norte.
        static GameObject FoundationTruck(Transform g, out MoveAlongPath move)
        {
            Transform path = Group(g, "Caminho_Caminhao");
            Transform[] points = TruckPath.Select((p, i) => Point(path, $"Ponto_{i + 1}", p)).ToArray();

            var truck = new GameObject("Caminhao_Fundacao");
            truck.transform.SetParent(g, false);
            truck.transform.SetPositionAndRotation(TruckPath[0], Quaternion.Euler(0f, 90f, 0f));
            Transform t = truck.transform;
            Local(PrimitiveType.Cube, t, "Cabine", new Vector3(0f, 1.3f, 2.2f), new Vector3(2.3f, 2f, 1.8f), "BO_BrancoFundacao", ColliderMode.Solid);
            Local(PrimitiveType.Cube, t, "Carroceria", new Vector3(0f, 1.5f, -1.1f), new Vector3(2.4f, 2.4f, 4.6f), "BO_BrancoFundacao", ColliderMode.Solid);
            Local(PrimitiveType.Cube, t, "Faixa", new Vector3(0f, 1.1f, -1.1f), new Vector3(2.45f, 0.3f, 4.65f), "BO_TurquesaFundacao");
            foreach (float z in new[] { 2.1f, -0.4f, -2.4f })
                foreach (float x in new[] { -1.1f, 1.1f })
                {
                    GameObject wheel = Local(PrimitiveType.Cylinder, t, "Roda", new Vector3(x, 0.45f, z), new Vector3(0.9f, 0.15f, 0.9f), "BO_Metal");
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }

            move = truck.AddComponent<MoveAlongPath>();
            SetField(move, "points", points);
            SetField(move, "speed", 9f);
            SetField(move, "slowDownDistance", 12f);
            truck.SetActive(false);
            return truck;
        }

        // Cavaletes listrados ao longo de toda a estrada do norte, do oeste da aldeia até o muro da
        // base, com uma parede invisível atrás: o caminho norte para o Poço fica fechado.
        static GameObject Barrier(Transform g)
        {
            float length = BarrierEastX - BarrierWestX;
            for (float x = BarrierWestX + 1.5f; x < BarrierEastX; x += 4f)
            {
                Transform horse = Group(g, $"Cavalete_{Mathf.RoundToInt(x)}");
                horse.position = new Vector3(x, 0f, BarrierZ);
                Local(PrimitiveType.Cube, horse, "Perna_E", new Vector3(-1.1f, 0.5f, 0f), new Vector3(0.12f, 1f, 0.5f), "BO_Metal");
                Local(PrimitiveType.Cube, horse, "Perna_D", new Vector3(1.1f, 0.5f, 0f), new Vector3(0.12f, 1f, 0.5f), "BO_Metal");
                Local(PrimitiveType.Cube, horse, "Barra_Alta", new Vector3(0f, 0.95f, 0f), new Vector3(2.6f, 0.22f, 0.08f), "BO_BrancoFundacao");
                Local(PrimitiveType.Cube, horse, "Barra_Baixa", new Vector3(0f, 0.55f, 0f), new Vector3(2.6f, 0.22f, 0.08f), "BO_TurquesaFundacao");
            }
            var wall = new GameObject("Parede_Invisivel");
            wall.transform.SetParent(g, false);
            wall.transform.position = new Vector3(BarrierWestX + length * 0.5f, 1.5f, BarrierZ);
            wall.AddComponent<BoxCollider>().size = new Vector3(length, 3f, 0.6f);
            g.gameObject.SetActive(false);
            return g.gameObject;
        }

        // Helicóptero que cruza o céu da Aldeia e pousa no heliponto da base. Ao pousar, desliga.
        static GameObject LandingHelicopter(Transform g, out MoveAlongPath move)
        {
            Transform path = Group(g, "Caminho_Helicoptero");
            Transform[] points = HeliPath.Select((p, i) => Point(path, $"Ponto_{i + 1}", p)).ToArray();
            GameObject heli = Helicopter(g, "Helicoptero_Base", HeliPath[0], 45f);
            move = heli.AddComponent<MoveAlongPath>();
            SetField(move, "points", points);
            SetField(move, "speed", 20f);
            SetField(move, "slowDownDistance", 18f);
            SilenceHelicopter(GetField<UnityEvent>(move, "onArrived"), heli);
            heli.SetActive(false);
            return heli;
        }

        static void SilenceHelicopter(UnityEvent e, GameObject heli)
        {
            Call(e, heli.GetComponent<AudioSource>().Stop);
            foreach (Spinner spinner in heli.GetComponentsInChildren<Spinner>()) SetEnabled(e, spinner, false);
        }

        // Marca dos altares dos Guardiões: o barco de junco sobre ondas entalhado na frente do nicho
        // e uma lamparina de barro em cima, de luz quente. Devolve a chama com a luz (apagada até o Samir acender).
        static GameObject NicheMark(Transform parent)
        {
            Transform mark = Group(parent, "Marca_Guardioes");
            mark.position = NicheFront;

            // Entalhe: peças finas e escuras quase encostadas na face do nicho (que fica para o norte, +z).
            Transform carving = Group(mark, "Entalhe_BarcoDeJunco");
            carving.localPosition = new Vector3(0f, 0f, 0.012f);
            const string groove = "BO_TijoloBarroEscuro";
            Local(PrimitiveType.Cube, carving, "Casco", new Vector3(0f, 0.6f, 0f), new Vector3(0.42f, 0.05f, 0.02f), groove);
            Local(PrimitiveType.Cube, carving, "Proa", new Vector3(0.24f, 0.67f, 0f), new Vector3(0.05f, 0.17f, 0.02f), groove).transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            Local(PrimitiveType.Cube, carving, "Popa", new Vector3(-0.24f, 0.67f, 0f), new Vector3(0.05f, 0.17f, 0.02f), groove).transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            Local(PrimitiveType.Cube, carving, "Feixe_Junco", new Vector3(0f, 0.64f, 0f), new Vector3(0.3f, 0.02f, 0.02f), groove);
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.24f + i * 0.16f;
                    float y = 0.48f - row * 0.1f;
                    Local(PrimitiveType.Cube, carving, $"Onda_{row + 1}_{i + 1}a", new Vector3(x, y, 0f), new Vector3(0.1f, 0.022f, 0.02f), groove).transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
                    Local(PrimitiveType.Cube, carving, $"Onda_{row + 1}_{i + 1}b", new Vector3(x + 0.08f, y, 0f), new Vector3(0.1f, 0.022f, 0.02f), groove).transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
                }

            // Lamparina de barro (pires com bico) em cima do nicho.
            Transform lamp = Group(mark, "Lamparina");
            lamp.localPosition = new Vector3(0f, NicheTop, -0.25f);
            Local(PrimitiveType.Sphere, lamp, "Pires", new Vector3(0f, 0.03f, 0f), new Vector3(0.16f, 0.06f, 0.12f), "BO_TijoloBarro");
            Local(PrimitiveType.Cylinder, lamp, "Bico", new Vector3(0f, 0.045f, 0.08f), new Vector3(0.035f, 0.035f, 0.035f), "BO_TijoloBarro").transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            var flame = new GameObject("Chama");
            flame.transform.SetParent(lamp, false);
            flame.transform.localPosition = new Vector3(0f, 0.09f, 0.1f);
            GameObject tip = Local(PrimitiveType.Sphere, flame.transform, "Fogo", Vector3.zero, new Vector3(0.025f, 0.05f, 0.025f), "BO_Areia");
            tip.GetComponent<Renderer>().sharedMaterial = FlameMaterial();
            var light = new GameObject("Luz_Quente").AddComponent<Light>();
            light.transform.SetParent(flame.transform, false);
            light.transform.localPosition = new Vector3(0f, 0.3f, 0.25f);
            light.type = LightType.Point;
            light.color = LampLight;
            light.range = 5f;
            light.intensity = LampIntensity;
            light.shadows = LightShadows.Soft;
            flame.SetActive(false);
            return flame;
        }

        // Material da chama: sem sombra nem luz, laranja-amarelado. Criado só se ainda não existir.
        static Material FlameMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", FlameColor);
            AssetDatabase.CreateAsset(material, FlameMaterialPath);
            return material;
        }

        // Põe a missão na lista do QuestManager da cena (criado pelo Acampamento), se ainda não estiver.
        static void RegisterQuest(QuestDefinition quest)
        {
            var manager = Object.FindFirstObjectByType<QuestManager>();
            if (manager == null)
            {
                Debug.LogWarning("Aldeia: QuestManager não encontrado na cena. Rode antes o menu do Acampamento.");
                return;
            }

            var quests = GetField<QuestDefinition[]>(manager, "quests");
            if (quests.Contains(quest)) return;
            Undo.RecordObject(manager, "Registrar missão");
            SetField(manager, "quests", quests.Append(quest).ToArray());
        }

        // Samir: o modelo animado (prefab do menu Nibiru > Personagens > Montar Samir animado),
        // andando pela malha de navegação. Corre para alcançar a Elena quando ela vai longe à frente.
        static GameObject Samir(Transform parent, Vector3 position, Transform destination)
        {
            var samir = new GameObject("Samir");
            samir.transform.SetParent(parent, false);
            samir.transform.SetPositionAndRotation(position, parent.rotation);
            var body = samir.AddComponent<CapsuleCollider>();
            body.radius = 0.3f;
            body.height = 1.75f;
            body.center = new Vector3(0f, 0.875f, 0f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SamirSetup.PrefabPath) != null)
                SamirSetup.AddVisual(samir.transform, 0f);
            else
                Debug.LogWarning("Aldeia: prefab do Samir não encontrado. Rode Nibiru > Personagens > Montar Samir animado e povoe a Aldeia de novo.");

            var agent = samir.AddComponent<NavMeshAgent>();
            agent.speed = SamirSetup.WalkSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 10f;
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0.2f;

            var walker = samir.AddComponent<WalkToPoint>();
            SetField(walker, "destination", destination);
            SetField(walker, "catchUpSpeed", SamirSetup.RunSpeed);
            samir.SetActive(false);
            return samir;
        }

        // O NpcAnimator do Samir (golpe de faca, agachar), ou null se o modelo não estiver nele.
        static AI.NpcAnimator SamirAnimator(GameObject samir) => samir.GetComponentInChildren<AI.NpcAnimator>(true);

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
