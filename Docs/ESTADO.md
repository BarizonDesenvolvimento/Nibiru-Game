# Estado do projeto

Retrato de onde o projeto está, para retomar o trabalho numa conversa nova sem precisar reler o histórico. Atualize este arquivo ao fim de cada etapa grande.

Última atualização: 25 de outubro de 2026.

## Onde estamos

Fase 3 do plano (construir a base), indo para a fatia vertical da região 1, o Sítio de Ur.

- O protótipo do personagem está pronto.
- Os **14 sistemas essenciais** da fatia (Grupo 1 de `Docs/Regiao-1-Sitio-de-Ur.md`) estão prontos e testados.
- O **blockout** do mapa do Sítio de Ur está construído.
- O **Acampamento** (área 1) está povoado e testado pelo dono: passos 1 a 4 da missão "Sobrevivente" e o começo de "O que o Tom viu".
- As **Trincheiras** (área 2) estão povoadas e testadas pelo dono: passo 5 da missão (ordem pelo rádio, primeira furtividade, pistola), helicóptero passando baixo, Dra. Weiss (só a primeira conversa), 2º rolo de filme e rádio de campanha.
- A **Aldeia** (área 3) começou: o encontro com o Samir (passo 6) e os sobreviventes com o nicho dos Guardiões como altar de salvamento (passo 7) estão prontos e testados pelo dono. O **passo 8** (a primeira tabuleta no nicho, a memória jogável como Ziusudra, o Eco do Abzu, a marca no pulso e a conversa com o Samir de mão na faca) está pronto e testado pelo dono. O **passo 9** (o plano de fuga: o cerco da Fundação, a conversa com o Samir e a passagem pela patrulha até a base) está pronto e testado pelo dono.
- A **base do zigurate** (área 4) começou: o **passo 10** (atravessar a base, com a patrulha, o líder, o atirador de elite no alto do zigurate, o golpe de faca pelas costas, o e-mail do Kast e a foto de 1979, o portão para a Aldeia e a saída para o Poço) está pronto e testado pela ponte, esperando o teste do dono. Veja "Base do zigurate, passo 10".
- A **Boca do Poço** (área 5) começou: o **passo 11** (descer o Poço ouvindo o Lullu lá embaixo, passar por ele, ponto de retorno e o flash a uns 12 m do Selo de Kur, com a Voz) está pronto e testado pela ponte, esperando o teste do dono. Veja "Boca do Poço, passo 11".
- A **Ninsar** tem uma prévia animada na Unity (corpo, saia e lança, sem manto), na cena `Teste_Ninsar`. O manto dela está pausado. Veja "Ninsar" em Personagens 3D.
- A **Elena definitiva** (Meshy + AccuRig, com dedos, 1,70 m) substituiu a provisória no prefab `Prefabs/Player/Elena`, com animações do Mixamo: parada, andar, correr, agachar, rolar, mirar, tiro (coice), recarregar, faca, dano e morte. Testada pelo dono. Veja "Elena" em Personagens 3D.
- O **Samir definitivo** (Meshy + AccuRig, com dedos, 1,75 m) substituiu a cápsula em todos os lugares onde ele aparece (Aldeia, passos 6 a 9, e laboratório), com animações do Mixamo: parado, andar, correr, agachado, golpe de faca e 4 gestos de conversa. Testado e aprovado pelo dono. Veja "Samir" em Personagens 3D.
- Os **4 soldados da Fundação** (Soldado, Líder de esquadra, Atirador de elite e Piloto; Meshy + AccuRig, com dedos) substituíram as cápsulas no prefab `Prefabs/AI/Soldado` e em todas as cenas, com animações de fuzil do Mixamo (parado, patrulha, corrida, mira, tiro, recarga, dano e morte), fuzil provisório e a IA e os eventos de antes. O piloto vai sentado nos helicópteros; o atirador de elite está no posto dele, no passo 10. Testados e aprovados pelo dono. Veja "Soldados da Fundação" em Personagens 3D.
- O **Lullu definitivo** (Meshy + AccuRig, com dedos, 2,10 m em pé) substituiu a cápsula no prefab `Prefabs/AI/Lullu` (e, de carona, no Primeiro Servo do laboratório, que é o mesmo prefab em escala 1,25), sem mudar a `LulluAI`. Testado pela ponte; falta o teste do dono. Veja "Lullu" em Personagens 3D.
- Os outros personagens (Yusuf, Umm Karim, Dra. Weiss, equipe morta e Primeiro Servo) estão prontos na `ArteFonte`, com esqueleto e dedos, esperando a importação, um por conversa. Lista e alturas em Personagens 3D.

A próxima prioridade é o teste do dono nos passos 10 e 11, depois o passo 12 (o Primeiro Servo, na mesma antecâmara), e importar os outros personagens (o kit comum já serve para eles). A corrida da Elena ainda é provisória (veja "Elena").

## Documentos

| Arquivo | O que tem |
| --- | --- |
| `Docs/GDD.md` | Design do jogo inteiro. Vale acima de tudo |
| `Docs/Biblia.md` | História canônica: eras, panteão, personagens, mistérios |
| `Docs/Regiao-1-Sitio-de-Ur.md` | Região 1 aprovada: mapa, missões, inimigos, itens, diálogos, sistemas (Grupos 1 e 2), controles, blockout e perguntas abertas |
| `Docs/Arte/Regiao1_SitioDeUr_Arte.md` | Direção de arte da região 1: paleta (o azul de lápis-lazúli só onde Kur vaza), luz e lista de peças por área |
| `Docs/Guia-MCP-Unity.md` | Como a Unity conversa com o Claude Code |
| `Docs/ESTADO.md` | Este arquivo |

## Cenas

| Cena | Para que serve |
| --- | --- |
| `Scenes/Teste_Personagem` | Laboratório de sistemas: tem conteúdo de teste de tudo (missão Treinamento, Samir animado com conversa de teste, soldados, Lullu, chefe, bonecos, brasas, altar). Os arquivos de teste têm prefixo `Teste_` |
| `Scenes/Regiao1_SitioDeUr` | A região de verdade: blockout, Elena e o conteúdo do Acampamento, das Trincheiras e da Aldeia |
| `Scenes/Teste_Ninsar` | Prévia da Ninsar animada: troca de animação por teclas (setas, 1 a 0, espaço recomeça) ou pelo menu na tela; câmera gira com o botão direito do mouse |
| `Scenes/Memoria_Shuruppak` | Memória jogável da Tabuleta do Junco (Shuruppak ao pôr do sol). Não se joga sozinha: é carregada por cima da região no passo 8. Fica longe da origem, em (2000, 0, 2000) |

O blockout é gerado pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (`Scripts/Editor/Blockout/SitioDeUrBlockout.cs`). Para mudar o mapa, mude o script e rode o menu de novo; o menu também refaz a malha de navegação. Os discos dourados `Marca_...` marcam onde entra cada conteúdo.

## Conteúdo das áreas

Cada área tem um script de editor que monta o conteúdo num grupo próprio da cena (`Conteudo_...`), apagando e reconstruindo a cada execução, como o blockout. Os arquivos de dados (missões, diálogos, eventos, itens) só são criados se ainda não existirem: textos mudados no Inspector não se perdem.

| Área | Menu | Script |
| --- | --- | --- |
| 1. Acampamento | **Nibiru > Conteúdo > Povoar Acampamento na cena aberta** | `Scripts/Editor/Content/CampContent.cs` |
| 2. Trincheiras | **Nibiru > Conteúdo > Povoar Trincheiras na cena aberta** | `Scripts/Editor/Content/TrenchContent.cs` |
| 3. Aldeia | **Nibiru > Conteúdo > Povoar Aldeia na cena aberta** | `Scripts/Editor/Content/VillageContent.cs` |
| 4. Base do zigurate | **Nibiru > Conteúdo > Povoar Base do Zigurate na cena aberta** | `Scripts/Editor/Content/ZigguratContent.cs` |
| 5. Boca do Poço | **Nibiru > Conteúdo > Povoar Boca do Poço na cena aberta** | `Scripts/Editor/Content/WellContent.cs` |
| Memória de Shuruppak (passo 8) | **Nibiru > Conteúdo > Construir memória de Shuruppak** (refaz a cena `Memoria_Shuruppak` do zero e a põe na lista do Build) | `Scripts/Editor/Content/ShuruppakMemoryContent.cs` |

As ferramentas comuns desses scripts (peças, gatilhos, itens, soldados, helicóptero, diálogos, passos de missão) ficam em `Scripts/Editor/Content/ContentKit.cs`. Rode as áreas em ordem (Acampamento, Trincheiras, Aldeia): o Acampamento cria a missão "Sobrevivente", e cada área seguinte só acrescenta os passos dela no fim (se ainda não estiverem lá).

**Acampamento, como funciona:**
- A Elena acorda dentro da barraca (tenda oca no blockout) com a Tabuleta do Junco; a voz sussurra "Tila" ("viva"; a palavra pode mudar em `Data/Dialogues/Acampamento_Sussurro`).
- Faca de campo numa caixa da barraca: a faca só ataca depois de pegar (campo "Item necessário" do `PlayerMeleeAttack`, ligado só nesta cena; no laboratório continua sempre disponível).
- Objetivos: pegar a faca → sair da barraca → examinar os corpos → bolsa do Tom (começa "O que o Tom viu"; 1º rolo atrás do caminhão) → atender o rádio (Kast, com escolha confiar/desconfiar) → esperar o resgate → fugir para as trincheiras.
- O helicóptero chega 15 s depois do rádio e pousa no heliponto. A execução do trabalhador (Hassan) dispara quando a Elena chega ao mirante (canto nordeste da elevação) ou, se ela não for, uns 26 s depois do pouso, só ouvida. Até lá, paredes invisíveis na beira da elevação seguram a Elena.
- Depois da execução: limites abertos, dois soldados sobem a rampa patrulhando o acampamento e o líder fica no heliponto.
- Pontos de retorno na porta da barraca e no mirante. Ao voltar depois da execução, a cena se refaz no estado final.
- Sons provisórios sintetizados (helicóptero, tiro, chiado de rádio) em `Art/Audio/Provisorio`, gerados pelo menu **Nibiru > Áudio > Gerar sons provisórios**. Para trocar, substitua o arquivo com o mesmo nome.

**Trincheiras, como funciona:**
- Ao entrar (gatilho `ChegouAsTrincheiras`), o rádio de um soldado diz "a doutora, viva; o resto, não" e aparece a dica de agachar. Ponto de retorno na entrada.
- Pistola Makarov e 8 balas no corpo do chefe de segurança (centro-leste). Ao pegar: fala da Elena, dica de mirar/atirar/recarregar e outro ponto de retorno.
- Soldados chegam com a execução: uma dupla patrulhando e o líder de guarda no canto nordeste. O líder larga o rádio de campanha e 4 balas ao morrer (`Combat/DropOnDeath`).
- Um segundo helicóptero passa baixo depois da pistola (ou uns 35 s depois do rádio). Se vir a Elena a céu aberto, chama os soldados (`AI/HelicopterSpotter`). Lonas de sombra e passarelas escondem; agachada, ela só é vista bem embaixo dele.
- Dra. Weiss atrás da tenda médica (oeste): primeira conversa, com escolha de contar ou não a execução; depois só repete o pedido de curativos. Entregar curativos, receber antibióticos e a escolha com o Samir entram com a Aldeia, onde começa "Remédio para Yusuf".
- 2º rolo de filme do Tom no canto nordeste, perto do líder; 2 curativos (caixote no sudoeste e pé da escada da passarela leste).
- O passo "Atravesse as trincheiras e siga para a Aldeia" completa com `ChegouAAldeia`, no fim da conversa com o Samir (veja a Aldeia).
- O rádio de campanha ainda não faz nada (avisar patrulhas fica para depois).

**Aldeia, como funciona (até agora, passos 6 e 7):**
- Uma área grande cobre a Aldeia com folga (evento `AproximouSeDaAldeia`): de onde a Elena vier, o encontro começa ao entrar nela.
- Há 4 pontos fixos de encontro nas entradas (leste, sudeste, sul e norte). O componente `Sequences/PlaceAtPointAhead` escolhe o mais perto entre os que estão à frente da Elena e leva a cena (soldado, corpo e Samir) para lá, virada para ela. A cada execução, o menu confere se cada ponto está na malha de navegação e com 2,5 m livres em volta (avisos no Console).
- Um soldado (figurante, sem IA) a encurrala (`Aldeia_Encurralada`). Depois da fala dele, o Samir aparece logo atrás e o derruba com uma facada (animação de golpe, faca na mão direita). Em seguida vem a conversa "O guia" (`Aldeia_OGuia`): o Samir nota a luz da tabuleta na bolsa ("Esconda isso"), diz que ninguém veio resgatar ninguém e desconversa se a Elena perguntar quem ele é. No fim, dispara `ChegouAAldeia`.
- Objetivo "Siga o Samir até a Aldeia": ele anda pela malha de navegação até o pátio, na frente da casa do altar (`Sequences/WalkToPoint`). Ele espera quando a Elena fica mais de 10 m para trás e corre (5 m/s) para alcançá-la quando ela vai mais de 10 m à frente (campo "Catch Up Speed" do `WalkToPoint`).
- No pátio: fala curta do Samir (`Aldeia_ChegadaPatio`), evento `ChegouAoPatioComSamir` e ponto de retorno. Ao voltar dele, o Samir já está no pátio e o soldado, no chão.
- Se a Elena morrer antes do pátio, o encontro se repete.
- Passo 7: depois da fala do pátio, o Samir entra na casa do altar (`WalkToPoint.WalkTo`) e aparece o objetivo "Siga o Samir até a casa". Dentro dela estão os sobreviventes, em cápsulas: Yusuf deitado numa esteira (parede oeste) e Umm Karim em pé, perto da cabeça dele.
- No fundo da casa (gatilho `EntrouNaCasaDoAltar`, ligado só depois do pátio), começa "Os sobreviventes" (`Aldeia_Sobreviventes`): a Umm Karim se assusta, o Yusuf delira de febre ("ouvi alguém respirando debaixo das pedras"), a Umm Karim conta do avô ("lá embaixo alguém dorme") e o Samir aponta a tenda médica do zigurate. No fim, `ConheceuSobreviventes` começa a secundária "Remédio para Yusuf" (`Data/Quests/RemedioParaYusuf`): conseguir o item `Antibioticos` e entregar ao Yusuf (evento `EntregouAntibioticos`, que ainda nada dispara).
- Logo depois, "O nicho" (`Aldeia_ONicho`): o Samir mostra o nicho, acende a lamparina e diz que ali ela pode descansar; a tabuleta fica morna (gancho do passo 8). Ele não revela que é Guardião. Evento `SamirMostrouONicho`.
- Com a lamparina acesa, o nicho vira altar (`Checkpoint` no modo Altar, "Descansar no nicho"): marca o ponto de retorno de frente para o nicho, quantas vezes a Elena quiser, e dispara `DescansouNoNicho` (campo novo "Evento ao ativar" do `Checkpoint`), que fecha o objetivo "Descanse junto ao nicho antigo".
- Depois disso, a Umm Karim e o Yusuf têm uma fala curta cada (`Aldeia_UmmKarimEspera`, `Aldeia_YusufEspera`), repetível.
- Ao voltar de um checkpoint, o Samir já está dentro da casa (se ela já conheceu os sobreviventes), a lamparina continua acesa e as conversas não se repetem.
- A missão "Remédio para Yusuf" entra na lista do `QuestManager` (que fica no Acampamento) pelo menu da Aldeia. Se rodar o menu do Acampamento de novo, rode o da Aldeia depois (ele também trava a leitura da tabuleta pelo inventário).

**Aldeia, passo 8 (a primeira tabuleta):**
- Na região, a tabuleta não se decifra mais pelo inventário antes da hora: aparece "A escrita brilha, mas ainda não faz sentido. Não aqui." (campo "Decipher From Inventory" do `TabletReader`, desligado só nesta cena pelo menu da Aldeia; no laboratório continua ligado). Depois de decifrada, ela pode ser relida pelo inventário.
- Depois de descansar no nicho, o objetivo vira "Leia a tabuleta no nicho" e o aviso do nicho troca para "Ler a tabuleta" (o altar sai do ar até a leitura acabar e volta sozinho depois). A área de leitura avança na direção da Elena, para ganhar do aviso "Falar com Umm Karim", que fica ao lado.
- Ao ler (grupo `Primeira_Tabuleta`, `Sequencia_Leitura`): a tabuleta aparece em cima do nicho, a luz azul de Kur cresce nela (primeira vez que o azul aparece no jogo) e a chama da lamparina encolhe e treme (`Sequences/LightFade`). Conversa "A tabuleta desperta" (`Aldeia_ATabuletaDesperta`): o Samir diz "Doutora... não." e a Voz: "Tila. Você está viva, e está segura. [...] Agora, olhe."
- A memória (`Memories/PlayableMemory`, dados em `Data/Memories/Memoria_Ziusudra`): a tela clareia em azul, a cena `Memoria_Shuruppak` é carregada por cima da região, a Elena vira o Ziusudra (cápsula de túnica clara, manto vermelho, barba e cajado), some o HUD de combate e ficam desligados esquiva, faca, armas, poder e inventário (`PlayerInputReader.SetCombatEnabled`). Abertura na tela: "Shuruppak, antes do Dilúvio / Você é Ziusudra". Objetivo: "Vá até a cabana de junco, à beira do rio".
- Shuruppak: rua de casas de barro com toldos e panos coloridos, mercado, palmeiras, juncos, rio e o zigurate de topo dourado do outro lado. Conversas opcionais com a menina (o barulho que não deixa os deuses dormirem) e o pescador (o rio subindo). Paredes invisíveis atrás das casas e na beira do rio.
- Na cabana de junco, a Voz fala através da parede (`Memoria_ParedeDeJunco`). Na primeira fala, o azul vaza pelas frestas da parede do fundo; no fim, o azul cresce e a memória termina. Pistas de que a voz esconde algo: "Porque vocês são meus" e "guarda bem o teu sangue... passa de filho para filho" (a Elena descende de Ziusudra). Fecha com "Tila. Viva.", a palavra do passo 1.
- Na volta (`Sequencia_Volta`), a Elena está no mesmo lugar: tela da tabuleta com o texto, "Novo poder: Eco do Abzu" e "Transformação +5" (de 0 para 5). Aparece a **marca azul no pulso esquerdo** (`Tablets/TransformationMark`, no prefab da Elena; acesa sempre que a transformação passar de zero; material `Art/Personagens/Elena/Materiais/Elena_MarcaKur`). Fala "A marca" (`Aldeia_AMarca`): "Não tenha medo da marca. Ela só quer dizer que eu estou com você."
- Conversa com o Samir (`Aldeia_DepoisDaTabuleta`), ele ao lado do nicho, de mão na faca: contar a verdade (Guardiões +5, evento `ContouAVerdadeAoSamir`), mentir (`MentiuAoSamir`, sem mudança agora; ele lembra depois) ou "Por que você está com a mão na faca?" (`SamirRevelouGuardiao`: ele revela que é Guardião). As três terminam em "precisamos sair desta aldeia" (gancho do passo 9) e disparam `ConversouSobreATabuleta`. Depois vem a dica do Eco do Abzu (Q ou LB).
- Uma escrita cuneiforme escondida na parede do fundo da casa só aparece com o Eco (`Inscricao_Escondida`, `EchoHiddenMark`).
- Ao voltar de um checkpoint depois da leitura: o poder, a marca e as escolhas continuam valendo, e nada se repete.
- Teste feito pela ponte: leitura, memória (andando com controle virtual), as conversas, a volta, as escolhas da verdade e da faca, a escrita revelada pelo Eco e a volta de um checkpoint. Testado e aprovado pelo dono no Play.

**Memória jogável, como funciona (vale para as próximas):** um arquivo `MemoryDefinition` (cena, quem a Elena é, lugar, evento de entrada, evento de fim, cor da passagem) e um `PlayableMemory` na cena da região, chamado por um evento. Na cena da memória: um `MemorySpawn` (onde começa) e um `MemoryAvatar` (a aparência, com a origem nos pés e sem colisão). A cena da memória precisa estar na lista do Build e ficar longe da região, porque o sol da região é apagado durante a memória, mas o resto continua lá, parado.

**Aldeia, passo 9 (o plano de fuga):**
- Depois da conversa da tabuleta (`ConversouSobreATabuleta`), o Samir sai da casa do altar e anda até a beira norte da Aldeia, entre a mesquita e a estrada (objetivo "Siga o Samir até a beira da aldeia"). Ele espera quando a Elena fica para trás.
- Ao chegar perto dele (gatilho `ChegouABeiraDaAldeia`, grupo `Plano_De_Fuga`), começa a cena do cerco: o Samir manda olhar a estrada (`Aldeia_OCercoChega`); um caminhão branco da Fundação chega pela estrada do norte; aparece a barreira de cavaletes de z = 41,5 (do oeste da Aldeia até o muro da base), com uma parede invisível e um soldado de guarda; um helicóptero cruza o céu e pousa no heliponto da base (e desliga, para o passo 10); dois soldados passam a patrulhar rente ao muro da base (x = -28, de z = -2 a 32).
- Conversa "O cerco" (`Aldeia_OCerco`): estradas fechadas, a passagem dos Guardiões no Poço (atrás da parede com o barco de junco, na antecâmara) e o caminho pela base. Escolhas: "Você não vem comigo?" e, só se o Samir revelou que é Guardião no passo 8, "Os Guardiões sempre souberam desse caminho?" (campo "Requires" da escolha). Ele vai sozinho pelos canais secos do sul até o wadi, lembra do remédio do Yusuf na tenda médica e dá a dica de esperar a patrulha virar de costas. Evento `OuviuOPlanoDeFuga`.
- Depois: o Samir vai embora para o sul, agachado por causa da patrulha, e some (`WalkToPoint.LeaveTo` + `NpcAnimator.Sneak`), um ponto de retorno é marcado na beira da aldeia (longe da visão da patrulha) e o objetivo vira "Passe pela patrulha da estrada e entre na base da Fundação, no pé do zigurate". Ele fecha ao entrar na base pela ponta sul do muro (`EntrouNaBaseDaFundacao`). O portão do muro continua fechado (abre por dentro, no passo 10).
- Ao voltar de um checkpoint depois do plano, tudo fica direto no estado final (caminhão parado, barreira, guarda, patrulha, helicóptero pousado e quieto, Samir longe), sem repetir a cena: o `StoryEventListener` ganhou a opção "Only On Load" (só ao carregar a cena) para isso.
- Enquanto o passo 10 não existe, a missão "Sobrevivente" aparece como concluída ao entrar na base.
- Teste feito pela ponte: o Samir indo até a beira, a cena do cerco, as duas versões da conversa (com e sem a revelação), o Samir indo embora, o ponto de retorno, a volta de um checkpoint e a entrada na base andando com controle virtual. Testado e aprovado pelo dono no Play.

**Base do zigurate, passo 10 (atravessar a base):**
- Blockout novo da base (`BuildZiggurat` em `SitioDeUrBlockout.cs`): pátio do muro oeste (x = -22) à cerca leste (x = 60) e da cerca sul (z = -6) ao pé do zigurate (z = 30). Entra-se pelo canto sudoeste, no vão entre a ponta do muro e a cerca (a área `EntrouNaBaseDaFundacao` do passo 9 foi movida para esse vão). Uma avenida livre (z de 4 a 13) liga o portão do muro à porta da tenda de comando: de lá se vê o portão. Tendas ocas com porta: comando (porta para o oeste, com toldo), médica e de provas (porta para o sul); alojamento fechado. Seis contêineres (três ao sul da avenida, cobertura de quem entra). A saída para o Poço é pelo nordeste, entre o zigurate e a cerca leste.
- Tudo da base fica no grupo `Conteudo_Base/Base_Ativa`, que só liga quando a Elena entra (o portão fechado está sempre lá). Ao entrar, ao vivo, a fala "Base_Chegada" (ela vê o laser) e a dica do golpe pelas costas. Ponto de retorno logo depois da entrada, fora do alcance do atirador.
- Objetivos (passos 17 a 20 da missão): "Atravesse o pátio até a tenda de comando" (`ChegouATendaDeComando`), "Procure na mesa da tenda de comando" (`LeuOsDocumentosDoKast`), "Abra o portão para a aldeia pelo painel da tenda" (`AbriuOPortaoDaAldeia`) e "Saia da base pelo nordeste, rumo à boca do Poço" (`SaiuDaBaseRumoAoPoco`). Enquanto o passo 11 não existe, a missão aparece como concluída na saída.
- Soldados: uma dupla dando a volta pela avenida e pelo corredor entre os contêineres e a cerca sul; o líder de esquadra de guarda na porta da tenda de comando (chama reforços se o combate durar).
- **Atirador de elite** (`AI/SniperAI`, dados em `Data/AI/AtiradorElite`): no ninho de sacos de areia do 2º terraço, a leste da escadaria, olhando o pátio. Não sai do lugar. Vigiando, a mira laser vermelha varre o pátio e mostra para onde ele olha; desconfiado, o laser vai até o vulto ou o som. Ao perceber a Elena: aviso pelo rádio na tela, os soldados até 60 m vão até onde ela está, o laser trava nela com um bipe e o primeiro tiro só sai depois do **tempo de aviso** (1,5 s), de novo cada vez que ele a reencontra. Perdendo-a de vista, mira onde a viu e depois volta a vigiar. O pé do zigurate (uns 10 m antes da face) e quem está sob o toldo da tenda ficam fora da vista dele.
- **Para ajustar o atirador jogando:** selecione `Data/AI/AtiradorElite` no Project. Os campos são os de um soldado ("Até onde enxerga" = alcance, "Tempo entre tiros", "Dano"...) mais o grupo "Atirador de elite" ("Tempo de aviso", varredura do laser, raio do aviso pelo rádio). Mudanças com o Play ligado continuam valendo depois.
- Rotas: por baixo, de contêiner em contêiner; pela avenida, no aberto (combate); ou a **rota alta**: escada de madeira por dentro do muro, perto da entrada; por cima do muro (passarela sobre o portão); rampa sobre o muro até um patamar no 1º terraço; rampa de andaime pela face oeste até o 2º terraço; e por trás do 3º terraço até as costas do atirador.
- **Golpe de faca pelas costas** (vale para todos os soldados): atrás de um soldado que não está em combate e não viu a Elena nos últimos 5 s (campo "Golpe pelas costas" nos dados de cada tipo de soldado), aparece "[botão da faca]  Golpe pelas costas". A faca vira a Elena para ele e ele cai de frente, morto, quase sem barulho (1 m). Distância e abertura das costas no `PlayerMeleeAttack` da Elena. A animação é a mesma estocada da faca.
- Tenda de comando: "Examinar a mesa" mostra o e-mail do Kast (`Data/Documents/Base_EmailDoKast`: "a filha de Irena", "a herança precisa estar presente") e depois a foto de 1979 (`Base_Foto1979`), na tela de leitura das tabuletas; em seguida a Elena comenta a foto (`Base_AFoto`). Depois da leitura: ponto de retorno dentro da tenda e o painel "Abrir o portão para a aldeia" na parede norte, junto da porta.
- O portão do muro oeste (`Conteudo_Base/Portao_Aldeia`) é branco com faixa turquesa e corre por dentro do muro, com som provisório de motor e trilho, ouvido da tenda. A Elena comenta (`Base_OPortao`). Recorta a malha de navegação: fechado, os soldados não passam. Ao voltar de um checkpoint, já aberto.
- Antibióticos na tenda médica e o 3º rolo de filme na tenda de provas: só pegáveis. Os passos de item que já existiam nas secundárias valem (pegar os antibióticos fecha "Consiga antibióticos"; o rolo conta para "O que o Tom viu"). A entrega ao Yusuf e a revelação dos filmes ficam para depois.
- Documentos (`Documents/DocumentDefinition` e `DocumentView`): cada documento é um arquivo em `Data/Documents` (título, texto, linha de baixo, evento ao ler). Os outros documentos da Fundação podem usar o mesmo.
- Ficam para depois: o segundo líder de esquadra, a passagem do helicóptero sobre a base e a foto como item no inventário.
- Teste feito pela ponte (teste automático, tudo OK): chegada ao vivo vindo do passo 9, rota alta caminhável até atrás do atirador, golpe pelas costas no líder e no atirador (cai de frente, ninguém entra em combate, o laser apaga), mesa da tenda, painel e portão, saída. À parte: na avenida, o atirador viu, travou, atirou depois do aviso e a patrulha veio.

**Boca do Poço, passo 11 (descer o Poço):**
- Tudo fica em `Conteudo_Poco/Poco_Ativo`, que só liga quando a Elena sai da base pelo nordeste (`SaiuDaBaseRumoAoPoco`). Rode o menu da Base antes (o Poço usa o evento dela).
- Objetivos (passos 21 a 24 da missão): "Siga o corredor nordeste até a boca do Poço" (`ChegouABocaDoPoco`), "Desça o Poço pelos andaimes" (`DesceuOPoco`, ao pisar no fundo), "Atravesse o fundo do Poço sem acordar o Lullu, ou livre-se dele" (`PassouPeloLullu`, ao cruzar a boca da antecâmara) e "Avance pela antecâmara até o Selo de Kur" (`FlashDoPoco`). Enquanto o passo 12 não existe, a missão aparece como concluída depois do flash.
- **Boca:** ao chegar (só ao vivo), a Elena vê o poço e ouve o raspar (`Poco_Boca`), e vem a dica de que o Lullu caça pelo som. Ronco grave e pedras caindo no Poço (`Poco_Ambiente_Loop`).
- **Lullu** (prefab `AI/Lullu`, agora com o modelo definitivo): vagueia na boca da antecâmara (44, -14, 78), com respiração e raspar ouvidos da descida (`Lullu_Respiracao_Loop`, até 30 m). Preso ao chão: o campo novo **"Max Climb"** do `LulluAI` (2 m aqui; 0 = sem limite, como no laboratório) faz sons vindos de andaimes virarem pontos no chão, e a Elena a mais de 2,4 m acima dele fica fora do alcance. Andando agachada ele quase não ouve (1,8 m); andando, 6 m; correndo, 14 m.
- **Pontos de retorno:** um no último patamar de andaime (`Checkpoint_Pe_Da_Descida`, a ~9 m do Lullu e em silêncio, de frente para a descida) e outro dentro da antecâmara, a ~10 m dele e antes do flash (`Checkpoint_Antecamara`). Ao voltar de um checkpoint depois de passar pelo Lullu, ele não volta (`StoryEventListener` só ao carregar).
- **Flash** (`Flash_Do_Selo`): a uns 12 m do Selo (z = 90,5), a Elena para de ser perseguida: o Lullu, se vivo, congela (`LulluAI.Pause`), a tela clareia (`UI/ScreenFlash`, atrás das conversas) e vem `Poco_Flash`: a tabuleta pulsa, "só luz e mãos" e a Voz, só legenda: "Você já esteve aqui." / "Não tem problema não lembrar. Eu lembro por você." Quando a Voz acaba (`FlashDoPoco`), a luz volta e o Lullu solta. O passo 12 começa daí.
- **Blockout:** o vão da passagem dos Guardiões na parede oeste da antecâmara agora está fechado pela `Parede_Barco_Junco`, com o barco de junco sobre ondas em ouro e a lamparina quente. O passo 13 vai abri-la.
- **Pendências do Poço:** o Lullu já tem modelo e animações (veja "Lullu" em Personagens 3D); sons são sintetizados; a Voz é só legenda; o Lullu que sobrevive ao flash segue vagando para o passo 12; a inscrição dos Guardiões do Poço (colecionável) e o segundo Lullu ficam para depois.
- Teste feito pela ponte (teste automático, tudo OK): chegada à boca, descida caminhável pelos 4 lances, retorno em silêncio, Lullu preso ao chão (barulho do andaime: caça, não sobe, não golpeia), parede do barco, flash com o Lullu vivo (congela e solta) e sem ele.

**Marca visual dos altares dos Guardiões (vale para o jogo inteiro):** uma lamparina de barro com luz quente (amarela e alaranjada) e o símbolo do **barco de junco sobre ondas** entalhado no nicho ou na pedra. Os altares nunca usam luz azul: o azul é a cor de Kur e dos deuses (veja `Docs/Arte/Regiao1_SitioDeUr_Arte.md`) e só aparece quando algo deles reage, como a tabuleta no passo 8. Na Aldeia, a marca é montada em formas simples por `NicheMark` em `VillageContent.cs` (cor da luz em `LampLight`; chama com o material `Art/Materials/Blockout/BO_Chama`).

## Sistemas prontos

Tudo em `Assets/_Nibiru/Scripts`, por pasta. Valores e conteúdo ficam em arquivos de dados (`Assets/_Nibiru/Data`).

| Sistema | Onde | Dados |
| --- | --- | --- |
| Movimento: andar, correr, esquivar, agachar | `Player/PlayerMotor` | `Data/Player/MovimentoJogador` |
| Controles (novo Input System) | `Player/PlayerInputReader` | `Data/Input/NibiruControles` (mapas Jogador e Interface) |
| Câmera sobre o ombro, mira | `Camera/` (`ICameraMode` + `ThirdPersonCameraMode`) | `Data/Camera/CameraJogador` |
| Interação | `Interaction/` | — |
| Itens e inventário | `Inventory/` | `Data/Items` |
| Vida, dano, morte | `Combat/Health`, `Player/PlayerDeathHandler` | — |
| Checkpoints e estado do mundo | `Save/` (`WorldState`, `Checkpoints`, `PersistentId`) | — |
| Faca | `Combat/PlayerMeleeAttack` | `Data/Combat/Faca` |
| Armas de fogo | `Combat/PlayerWeapons` | `Data/Combat/Pistola`, `Espingarda` |
| Furtividade e ruído | `Stealth/` | valores em `MovimentoJogador` e nas armas |
| IA dos soldados (com carregador: recarrega a cada 8 tiros, em 2,4 s) | `AI/SoldierAI`, `SoldierPerception` | `Data/AI/SoldadoContencao`, `LiderEsquadrao` |
| Missões e eventos | `Quests/` | `Data/Quests`, `Data/Events` |
| Diálogos | `Dialogue/` | `Data/Dialogues` |
| Facções e reputação | `Factions/` | `Data/Factions` |
| Tabuletas, transformação, Eco do Abzu | `Tablets/`, `Powers/` | `Data/Tablets`, `Data/Powers` |
| Lullu e o chefe Primeiro Servo | `AI/LulluAI` | `Data/AI/Lullu`, `PrimeiroServo` |
| HUD, telas e pausa | `UI/` | opções do jogador em `GameOptions` (salvas no computador) |
| Helicóptero que avista a Elena, item largado ao morrer | `AI/HelicopterSpotter`, `Combat/DropOnDeath` | configurados no Inspector |
| Atirador de elite (posto fixo, mira laser, tempo de aviso) | `AI/SniperAI` (o animador e o símbolo sobre a cabeça leem qualquer IA de soldado por `AI/ISoldierBrain`) | `Data/AI/AtiradorElite` (`SniperSettings`) |
| Golpe de faca pelas costas | `Combat/PlayerMeleeAttack`, `Combat/ITakedownTarget` (nos soldados), aviso em `UI/InteractionPromptUI` | "Golpe pelas costas" nos dados de cada soldado |
| Lullu preso ao chão (`maxClimb`) e congelável (`Pause`) | `AI/LulluAI` | `Data/AI/Lullu`; o campo "Max Climb" fica no componente |
| Flash de luz na tela (atrás das conversas) | `UI/ScreenFlash` (`FadeIn`, `FadeOut`) | cor e tempos no Inspector |
| Documentos (e-mail, foto, cartas) na tela de leitura | `Documents/` (`DocumentDefinition`, `DocumentView`) | `Data/Documents` |
| Cenas roteirizadas: sequência com tempo, mover por pontos, tiro de cena, escolha por evento, área de aviso, hélice, ponto à frente da Elena, personagem que anda até um ponto (ou vai embora e some), luz que acende ou apaga aos poucos | `Sequences/` (`TimedSequence`, `MoveAlongPath`, `ScriptedShot`, `StoryEventBranch`, `PlayerZone`, `Spinner`, `PlaceAtPointAhead`, `WalkToPoint`, `LightFade`) e `UI/NotificationPoster` | configurados no Inspector |
| Memória jogável (versão simples do Grupo 2): passagem para a cena do passado e volta | `Memories/` (`MemoryDefinition`, `PlayableMemory`, `MemorySpawn`, `MemoryAvatar`) | `Data/Memories` |
| Marca da transformação no pulso | `Tablets/TransformationMark` (no prefab da Elena) | material `Elena_MarcaKur` |
| Animação dos soldados: velocidade pelo deslocamento (malha de navegação ou cena), mira com o tronco girado para o cano apontar ao alvo, tiro, recarga, dano, morte para trás e de frente, e mira/tiro/queda chamados por cenas | `AI/SoldierAnimator` | configurado no prefab do soldado |
| Animação de personagens que não são a Elena: andar e correr pela velocidade do NavMeshAgent, agachar, golpe de faca e gestos nas falas dele (continua animado com o jogo pausado no diálogo) | `AI/NpcAnimator` | configurado no prefab do personagem |

Prefabs: `Prefabs/Player/Elena` (Elena + câmera + HUD + diálogos; use este em toda cena), `Prefabs/Personagens/Samir` (modelo + `NpcAnimator` + faca), `Prefabs/AI/Soldado` (com as variantes `Soldado_Lider` e `Soldado_Atirador`), `Prefabs/Personagens/Piloto` (sentado, nos helicópteros), `Prefabs/AI/Lullu` (colisor, agente, `LulluAI`, `LulluAnimator` e o filho `Corpo` com o modelo).

## Personagens 3D

Arte-fonte em `ArteFonte/<Personagem>/`, fora de `Assets`: `Referencias/` (imagem de referência), `1_Meshy/` (FBX e texturas PNG baixados do Meshy; fora do Git) e `2_Rig/` (FBX do AccuRig com esqueleto e dedos). Cada personagem entra na Unity numa conversa própria.

**Como chegam (conferido em 25/10/2026, no Blender):** todos têm o esqueleto do AccuRig com 101 ossos (30 de dedo), pose T ou A, todos os vértices com peso e de 19 a 31 mil triângulos (bom para jogo). O Meshy entrega todos com cerca de **0,95 m**: a altura certa vem da escala de importação (tabela abaixo). No FBX do rig, a cor e o normal (4096) estão embutidos e só metal e rugosidade (2048) saem soltos; as PNG completas estão em `1_Meshy/` (os mesmos UVs; se sumirem, baixe de novo no Meshy).

| Personagem | Pasta | Altura | Situação |
| --- | --- | --- | --- |
| Elena Varga | `Elena` | 1,70 m | **No jogo** (prefab `Player/Elena`) |
| Samir Haddad | `Samir` | 1,75 m | **No jogo** (prefab `Personagens/Samir`) |
| Yusuf | `Yusuf` | 1,74 m | Pronto para importar |
| Umm Karim | `Umm Karim` | 1,58 m | Pronto para importar |
| Dra. Weiss | `Dra. Weiss` | 1,68 m | Pronto; pose A (braços 51° abaixo) e 4 ossos de dedo sem peso (o `FingerWeightFix` corrige na importação) |
| Soldado da Contenção | `Soldado` | 1,82 m | **No jogo** (prefab `AI/Soldado`); o osso de dedo sem peso é corrigido na importação |
| Líder de esquadra | `LiderDeEsquadra` | 1,80 m | **No jogo** (prefab `AI/Soldado_Lider`) |
| Atirador de elite | `AtiradorDeElite` | 1,78 m | **No jogo** (prefab `AI/Soldado_Atirador`, com a IA trocada pela `SniperAI` no passo 10) |
| Piloto | `Piloto` | 1,78 m | **No jogo** (prefab `Personagens/Piloto`, sentado nos helicópteros) |
| Arqueólogo veterano (equipe morta) | `ArqueologoVeterano` | 1,76 m | Pronto para importar |
| Arqueóloga jovem (equipe morta) | `ArqueologaJovem` | 1,68 m | Pronto (2 ossos de dedo sem peso) |
| Trabalhador da escavação (também o Hassan) | `TrabalhadorEscavacao` | 1,72 m | Pronto para importar |
| Lullu | `Lullu` | 2,10 m em pé | **No jogo** (prefab `AI/Lullu`); pose A (41°) e pernas longas de criatura. Curvado, a cabeça fica a uns 1,7 m |
| Primeiro Servo | `PrimeiroServo` | 2,50 m | Pronto (5 ossos de dedo sem peso) |
| Ninsar | `Ninsar` | 1,90 m | Prévia na Unity; manto pausado (Ato 2). Veja "Ninsar" abaixo |

As alturas ficam também em `Scripts/Editor/Personagens/CharacterHeights.cs`: `CharacterHeights.FitImportScale(fbx, "Nome")` ajusta o "Scale Factor" da importação até o modelo medir a altura da tabela (use na montagem de cada personagem).

A Elena e o Samir antigos (`Elena_Varga`, `Samir_Haddad`, 1,5 a 1,8 milhão de triângulos, sem esqueleto) e a Elena provisória do Meshy saíram do projeto (os arquivos do `Samir_Haddad` que ainda estavam em `Art/Personagens/Samir` foram apagados com a entrada do Samir novo).

**Dedos sem peso:** `Scripts/Editor/Personagens/FingerWeightFix.cs` corrige na importação, em todo modelo de `Art/Personagens`, as falanges do AccuRig que vieram sem peso: cada uma recebe a pele do trecho dela, tirada das falanges vizinhas (e da mão, na primeira), com transição suave nas juntas. O FBX original não muda; o Console mostra quais ossos foram corrigidos.

**Kit comum para os próximos personagens:** `Scripts/Editor/Personagens/CharacterSetupKit.cs` (texturas e material URP de dois lados, importação Humanoid na altura da tabela, cortes do Mixamo com o avatar do X Bot e árvore de velocidades). Os cortes são acrescentados ao arquivo do Mixamo sem apagar os de outros personagens. O `SamirSetup` é o exemplo de uso. A Elena ainda usa o `ElenaSetup` próprio, com as mesmas regras. O Tom não entra agora.

### Animações do Mixamo

Em `ArteFonte/Animacoes/Mixamo` (baixadas "sem pele"; o `X Bot.fbx` é o boneco em pose T). As usadas são copiadas para `Assets/_Nibiru/Art/Animacoes/Mixamo`.

| Animação | Situação |
| --- | --- |
| Idle, WalkForward, Fast Run, Sneak Walk, Stand To Roll, Pistol Aim, Reloading, Stabbing, Hit Reaction, Standing React Death Backward | Usadas na Elena (veja a tabela dela). Parada, andar, correr, agachar e golpe de faca também no Samir (os mesmos cortes) |
| Talking, Talking2, Talking3, Talking4 | Usadas no Samir (gestos de conversa); servem para os outros NPCs |
| Female Walk | Andar de passarela, com o queixo erguido e o tronco para trás. Não serviu para a Elena; pode servir para outra personagem |
| Run Forward | Ainda não conferida (candidata a corrida da Elena) |
| Laying Idle, Laying Shaking Head, Look Around | Boas, para NPCs (Yusuf deitado na esteira) |
| Standing React Death Backward | Morte para trás (Elena e soldados) |
| Run Look Back, Unarmed Run Back, jump forward/backward | Corrida olhando para trás, corrida de costas e pulos; sem uso por enquanto |
| Walking, firing rifle, rifle aiming idle, rifle run, walking to dying | **Usadas nos soldados** (veja a tabela deles). O "Walking" anda segurando fuzil |
| strafe, walking backwards, start/stop walking, run backwards | Resto do pacote de fuzil; sem uso por enquanto |
| Run | Corrida de pernas abertas e cruzadas, com o pé virando para fora. Não usar |
| Stable Sword Inward/Outward Slash | Golpes de espada que giram o tronco quase 180°. Não servem para faca |

**Como importar animações do Mixamo (vale para todos os personagens):**
- Humanoid com **"Copy From Other Avatar" do `X Bot.fbx`**. O avatar feito da própria animação sem pele parte do primeiro quadro, não da pose T, e entorta a animação inteira (a corrida ficava girada 40°).
- Giro da raiz: **"Based Upon: Body Orientation"** para andar, correr e agachar (a Run e a Sneak Walk correm na diagonal) e **"Original"** para golpes em que o corpo fica de lado (Stabbing), senão o braço vai para o lado.
- Antes de usar, ver a animação no X Bot: os nomes enganam (o "Walking" é com fuzil).
- **Exceção, animações de fuzil:** giro "Original" em todas. Pela direção do corpo, o corpo fica de frente e o fuzil atravessado (cano 70° para a esquerda); no giro original elas andam reto e o fuzil fica só 27° à esquerda (postura de lado), compensado girando o tronco na mira.
- No editor, sem câmera olhando, um Animator só mexe nos ossos em "Always Animate" (senão fica em pose T): ao medir poses por script, ligue isso antes.

### Elena

O menu **Nibiru > Personagens > Montar Elena animada** (`Scripts/Editor/Personagens/ElenaSetup.cs`) refaz tudo (saia do Play antes):
- Modelo `Art/Personagens/Elena/Elena.fbx` (cópia de `ArteFonte/Elena/2_Rig/Elena.rig.fbx`): Humanoid com dedos (52 ossos mapeados), escala de importação 1,787 (1,70 m), sem blendshapes.
- Texturas `Elena_BaseColor/Normal/Metallic/Roughness.png` (cópias das PNG de `1_Meshy`). Material URP Lit `Materiais/Elena.mat`, de dois lados (como no AccuRig). Metal e lisura vão juntos em `Elena_MetalSmooth.png` (R = metal, A = 1 - rugosidade), gerado só se ainda não existir; para refazer, apague o arquivo e rode o menu.
- Animações: avanço do clipe descartado (quem anda é o `PlayerMotor`), altura e giro na pose. A velocidade natural de cada andar e corrida é lida do próprio clipe.
- Controlador `Elena.controller`. Camada base: "Locomocao" (em pé e agachada pelo `Agachar`, cada uma pela `Velocidade`), "Esquiva", "Golpe_Faca" e "Morte" (cada uma no tempo exato da ação do jogo). Camadas do tronco (`Elena_Tronco.mask`): "Mira", "Recarregar" (no tempo da recarga da pistola) e "Reacoes" (aditiva, peso 0,55: o tranco do dano por cima de tudo).
- No prefab, o filho "Modelo" fica sob o "Jogador", com os pés no fundo do colisor. A faca provisória (`PivoFaca/Lamina`) virou uma lâmina curta.
- `Player/PlayerAnimator` passa o estado do movimento, da mira e do combate ao Animator. Ele também cuida de:
  - endireitar o tronco ao mirar;
  - dar o coice a cada tiro (evento `PlayerWeapons.Fired`);
  - levar a arma e a faca para a mão direita;
  - tocar dano e morte (eventos do `Health`);
  - esconder o corpo (só a sombra) quando a câmera encosta nela.
- `PlayerDeathHandler` só tomba o corpo quando não há animação de morte.

| Trecho | Vem de | Observação |
| --- | --- | --- |
| Parada | `Idle` | Respirando |
| Andar | `WalkForward` | Tronco reto. Natural a 1,7 m/s; a 3 m/s toca 1,3 vez mais rápido |
| Correr | `Fast Run` | Pique a 5,4 m/s (o jogo corre a 6). **Provisória:** os braços abrem demais; trocar por uma corrida mais atlética |
| Agachada_Parada | `Sneak Walk`, quadro 17 | Quadro parado do andar furtivo (falta "Crouch Idle") |
| Agachada_Andando | `Sneak Walk` | Altura pelos pés |
| Esquiva | `Stand To Roll`, quadros 10 a 50 | Mergulho e cambalhota, em 0,4 s |
| Mira | `Pistol Aim`, quadro 20 | Pistola com as duas mãos |
| Recarregar | `Reloading`, quadros 8 a 92 | Gesto de fuzil; trocar por recarga de pistola quando houver |
| Golpe_Faca | `Stabbing`, quadros 15 a 37, espelhada | Estocada com a mão direita, giro "Original" |
| Dano | `Hit Reaction`, quadros 13 a 60 | Aditiva |
| Morte | `Standing React Death Backward` | Cai para trás e fica deitada |

Animações que faltam: corrida mais atlética (a Run Forward está na pasta para conferir), agachada parada ("Crouch Idle"), tiro e recarga de pistola, andar de costas mirando (hoje é o andar tocado ao contrário) e interagir (pegar item). A arma continua sendo o bloco provisório.

**Como testar a Elena:** menu **Nibiru > Testes > Elena** (`Scripts/Editor/ElenaShortcuts.cs`):

| Atalho | O que faz |
| --- | --- |
| 1. Laboratório (Teste_Personagem) com pistola e faca | Abre o laboratório com pistola, 24 balas e faca, e os inimigos parados (só nesta partida; senão eles vêm atrás dela e a matam) |
| 2. Região, na beira da Aldeia, com pistola e faca | Depois do plano de fuga, com a patrulha andando rente ao muro |
| 3. Levar dano (10 de vida) | Com o Play ligado |
| 4. Morrer | Com o Play ligado |

A base dos atalhos (`TestShortcuts.Go`) agora aceita a cena: atalhos de outras cenas além da região.

### Samir

O modelo do Meshy **não tem túnica**: jaqueta, calça cargo, botas, lenço, bolsa e uma adaga curva no cinto (igual à referência). Jaqueta, bolsa e calça deformam bem andando e correndo; os pesos do AccuRig ficaram como vieram.

O menu **Nibiru > Personagens > Montar Samir animado** (`Scripts/Editor/Personagens/SamirSetup.cs`) refaz tudo (saia do Play antes; rode antes o menu da Elena, porque o Samir usa os cortes dela):
- Modelo `Art/Personagens/Samir/Samir.fbx` (cópia de `ArteFonte/Samir/2_Rig/Samir.Rig.fbx`): Humanoid com dedos (52 ossos mapeados), escala de importação 1,835 (1,75 m).
- Texturas `Samir_BaseColor/Normal/Metallic/Roughness.png` (cópias das PNG de `1_Meshy`) e `Samir_MetalSmooth.png` (gerado só se não existir). Material URP Lit `Materiais/Samir.mat`, de dois lados.
- Controlador `Samir.controller`: "Locomocao" (em pé e agachado pelo `Agachar`, cada um pela `Velocidade`), "Golpe_Faca" (pelo gatilho `Golpe`) e "Conversa_1" a "Conversa_4" (Talking 1 a 4).
- Prefab `Prefabs/Personagens/Samir`: o modelo, o `NpcAnimator` e uma faca provisória (cabo e lâmina), que só aparece na mão direita durante o golpe.
- O conteúdo da Aldeia (`VillageContent`) põe o prefab no Samir que anda pela malha de navegação, como filho "Visual" (`SamirSetup.AddVisual`), com um colisor de cápsula. Se o prefab mudar, rode o menu da Aldeia de novo.
- No laboratório (`Teste_Personagem`), o menu troca a cápsula pelo modelo e mantém o colisor e a conversa de teste.

`AI/NpcAnimator`:
- Velocidade do NavMeshAgent para o Animator.
- `Strike()`: golpe de faca.
- `Sneak()` e `StandUp()`: agacha (e anda mais devagar, 1,9 m/s) ou levanta.
- Nas falas cujo "Speaker" é o nome dele ("Samir"), toca um gesto de conversa diferente a cada fala. Nas falas dos outros, ele escuta parado.
- Durante o diálogo, o Animator passa para o tempo real (o jogo pausa, mas ele continua respirando e gesticulando).

O `WalkToPoint` também vira o personagem para a Elena no tempo real durante os diálogos (antes, o Samir falava de costas quando chegava junto com a fala do pátio).

Pendências do Samir:
- Na conversa depois da memória, o texto diz "de mão na faca", mas nenhuma animação põe a mão dele na faca (falta uma pose).
- A adaga do cinto faz parte do modelo; no golpe aparecem as duas (a do cinto e a provisória na mão).
- Em alguns gestos de conversa ele ergue um pouco a cabeça (é do gesto do Mixamo).

**Como testar o Samir:** menu **Nibiru > Testes > Samir** (`Scripts/Editor/SamirShortcuts.cs`):

| Atalho | O que mostra |
| --- | --- |
| 1. Encontro na Aldeia: a facada no soldado | Perto da Aldeia: ande para o oeste. O soldado encurrala a Elena e o Samir o derruba |
| 2. Seguindo o Samir até o pátio (andar e correr) | Depois da conversa do guia. Corra à frente dele para vê-lo correr |
| 3. Casa do altar: os sobreviventes (gestos de conversa) | No pátio: siga o Samir para dentro da casa |
| 4. Volta da memória: o Samir de mão na faca | O mesmo do passo 8, atalho 4 |
| 5. Depois do cerco: o Samir saindo agachado | Ao sul da beira da aldeia; ele passa agachado ao lado da Elena. Não ande para o norte (lá começa o cerco) |
| 6. Laboratório: falar com o Samir | De frente para ele, com os inimigos parados |
| 7. Vitrine: todas as animações lado a lado | Um chão vazio com uma cópia do Samir por animação, com o nome em cima; o golpe se repete |

### Lullu

O menu **Nibiru > Personagens > Montar Lullu animado** (`Scripts/Editor/Personagens/LulluSetup.cs`) refaz tudo (saia do Play antes; rode antes o menu da Elena, porque ele usa os cortes dela):
- Copia da `ArteFonte/Lullu` o FBX do AccuRig (`2_Rig/Lullu.rig.fbx`) e as texturas do Meshy para `Art/Personagens/Lullu` (usa o `SoldiersSetup.ImportCharacter`, que agora é público). Humanoid com dedos (53 ossos), escala de importação 2,204 (2,10 m em pé), material URP de dois lados.
- Controlador `Lullu.controller`: "Locomocao" pela velocidade (parado = `Idle`, andar = `WalkForward`, correr = `Fast Run`, nas velocidades dos dados `Data/AI/Lullu`), "Golpe" (`Stabbing`, pelo gatilho), "Morte" (cai para trás) e a camada aditiva "Reacoes" (`Hit Reaction`, peso 0,6).
- O **prefab é editado no lugar** (mesmo arquivo, então as cenas já pegam o modelo): `Lullu` (colisor, agente, vida, `LulluAI`, `LulluAnimator`) > `Corpo` (o "body" da IA, com o `EchoRevealable`; os pés ficam 1 m abaixo da raiz) > `Modelo`. Saíram os olhos e as faixas da cápsula.
- `AI/LulluAnimator` (novo, a IA não mudou): lê o estado da `LulluAI` e a vida; passa a velocidade (pelo deslocamento), o golpe (entrando em Golpe ou Investida), o dano e a morte ao Animator. **Postura de criatura:** o modelo é em pé, então o tronco, o pescoço e a cabeça são dobrados para a frente por cima da animação (22°, 22°, -18°, -12°; mais fraco andando e correndo, `movingWeight`). Morto, ele segura o `Corpo` de pé onde estava, porque a `LulluAI` tomba a raiz e a animação de morte já o deita.
- **Colisor:** cápsula de 0,4 m de raio e 1,8 m de altura, do chão para cima (antes 0,35 x 2 m). **Audição:** a `LulluAI` mede distância e altura a partir da raiz, a 1 m do chão (cintura dele); a cabeça curvada fica a uns 1,7 m, então o "ouvido" fica 0,7 m abaixo da cabeça, sem efeito prático nos alcances (6, 14, 1,8 m). O alcance vertical da Elena nos andaimes (2,4 m acima da raiz) não mudou.
- O andar agachado (`Sneak Walk`) não serviu: nas pernas longas dele o corpo vira uma rampa quase deitada. Por isso o andar é o normal, encurvado pelo `LulluAnimator`.
- Pendências: o golpe é a estocada de faca do Mixamo (falta um golpe de garra); o atordoado do Eco ainda é só o tremor do corpo da `LulluAI`; falta um andar de criatura de verdade (quatro apoios ou arrastado); a investida do Primeiro Servo usa o mesmo golpe.
- **Como testar:** os atalhos do passo 11 (**Nibiru > Testes > Passo 11**, 4 e 6) levam ao Lullu; corra ou atire perto dele para vê-lo caçar e golpear (ou fique agachada para ele não ouvir). No laboratório (**Nibiru > Testes > Elena > 1**, sem o `StopEnemies`, ou abrindo `Teste_Personagem`) o Primeiro Servo é o mesmo modelo maior.

### Soldados da Fundação

O menu **Nibiru > Personagens > Montar soldados da Fundação** (`Scripts/Editor/Personagens/SoldiersSetup.cs`) refaz tudo (saia do Play antes; rode antes o menu da Elena, porque os soldados usam os cortes de recarga, dano e morte dela):
- Copia da `ArteFonte`, se ainda não estiverem na Unity, o FBX do AccuRig (`<Nome>.Rig.fbx`) e as texturas PBR do Meshy (`1_Meshy`) para `Art/Personagens/<Nome>/`, e as animações de fuzil para `Art/Animacoes/Mixamo`. Importa como Humanoid com dedos, na altura da tabela, com material URP de dois lados (como o Samir).
- Controlador `Art/Personagens/Soldado/Soldados.controller` (o mesmo para os 3 armados): "Locomocao" pela `Velocidade` (parado, patrulha a 1,6 m/s, corrida na velocidade da perseguição), "Morte" e "Morte_Frente" na camada base; camadas do tronco (`Soldado_Tronco.mask`) "Mira" (com "Disparo" pelo gatilho `Atirar`), "Recarregar" (no tempo da recarga da IA) e "Reacoes" (aditiva, o tranco do `Dano`).
- Fuzil provisório em formas simples (coronha e guarda-mão de madeira, corpo, carregador e cano), preso ao osso da mão direita. O encaixe é medido na pose de mira: empunhadura na palma direita e cano na direção da mão esquerda. O filho `Fuzil/Cano` é de onde sai o tiro (`muzzle` do `SoldierAI`).
- Prefabs dos modelos `Prefabs/Personagens/Soldado`, `LiderDeEsquadra`, `AtiradorDeElite` (modelo + `SoldierAnimator` + fuzil) e `Piloto` (modelo sentado, sem fuzil).
- Prefab com IA `Prefabs/AI/Soldado` (o de antes: sai a cápsula, o capacete, a faixa e a arma; entra o modelo como filho "Visual", com os pés 1 m abaixo da raiz; colisor na altura dele) e as variantes `Soldado_Lider` e `Soldado_Atirador` (a mesma IA, vida e colisor, com outro modelo).
- No laboratório, o "Lider" passa a usar o `Soldado_Lider`; os outros soldados já são o prefab Soldado.
- Os controladores são apagados e criados de novo a cada execução; o menu reimporta os prefabs no fim (sem isso, a cópia deles na memória fica sem controlador e as variantes gravam o controlador vazio).

| Trecho | Vem de | Observação |
| --- | --- | --- |
| Parado | `rifle aiming idle` | Fuzil em prontidão (não há "parado com fuzil baixo" na pasta) |
| Patrulha | `Walking` | Andar com fuzil, natural a 0,95 m/s; na patrulha toca mais rápido |
| Correr | `rifle run` | Perseguição |
| Mira | `rifle aiming idle` | Só o tronco; o `SoldierAnimator` gira o tronco 27° para o cano apontar para a frente |
| Disparo | `firing rifle` | Coice, a cada tiro |
| Recarregar | `Reloading` (corte da Elena) | Fuzil atravessado, mão no carregador |
| Dano | `Hit Reaction` (corte da Elena) | Aditiva, peso 0,6 |
| Morte | `Standing React Death Backward` (corte da Elena) | Cai de costas (tiro, golpe de frente) |
| Morte_Frente | `walking to dying`, quadros 40 a 92 | Cai de bruços (golpe pelas costas: a facada do Samir) |
| Piloto sentado | `Piloto_Sentado.anim`, feito pelo menu | Pose montada pelos músculos do Humanoid (não há ninguém sentado na pasta do Mixamo). Para trocar, baixe um "Sitting Idle" no Mixamo |

`AI/SoldierAnimator` (no modelo de cada soldado):
- Velocidade pelo deslocamento do soldado: vale para a malha de navegação e para cenas que levam o figurante.
- Com a IA ligada: mira enquanto o `SoldierAI` está em combate vendo a Elena, coice a cada tiro (`SoldierAI.Fired`), recarga (`SoldierAI.ReloadStarted`), dano e morte pela vida (`Health`).
- Para cenas, sem IA: `AimAt(true)`, `Fire()`, `Die()` (para trás), `DieForward()` (de frente), `LieDead()` e `LieDeadForward()` (já no chão, ao voltar de um checkpoint). Morto, o colisor em pé do soldado é desligado.
- Continua respirando durante os diálogos (tempo real).

Outras mudanças que vieram com os soldados:
- `SoldierAI`: carregador (`magazineSize` 8 e `reloadTime` 2,4 s nos dados do soldado; o primeiro carregador começa num ponto aleatório para eles não recarregarem juntos). Com modelo animado, a morte não tomba mais a raiz.
- `EchoRevealable`: num personagem animado, a silhueta do Eco é uma cópia da malha presa aos mesmos ossos.
- `ContentKit.Soldier` escolhe o modelo (`SoldierModel`: Soldado, Lider, Atirador; sem dizer, o líder usa o do Líder). `ContentKit.Helicopter` ganhou cabine com vidro (`BO_Vidro`, criado se não existir), banco e o piloto.
- Acampamento: o soldado aponta o fuzil para o trabalhador 2 s antes do tiro e o disparo tem coice; o líder de esquadra comanda do heliponto.
- Trincheiras: o líder é o modelo do Líder de esquadra (ainda larga o rádio e a munição ao morrer).
- Aldeia: o soldado do encontro aparece de fuzil apontado para a Elena e cai de bruços com a facada (saiu o corpo-cápsula `Corpo_Soldado_Encontro`); ao voltar de um checkpoint, ele já está caído. O guarda da barreira do cerco é o modelo do Líder de esquadra, com a IA de soldado comum.

**Como testar os soldados:** menu **Nibiru > Testes > Soldados** (`Scripts/Editor/SoldierShortcuts.cs`):

| Atalho | O que mostra |
| --- | --- |
| 1. Acampamento: a execução vista do mirante | Em uns 15 s o helicóptero pousa; o soldado mira e atira no trabalhador. Depois, a patrulha |
| 2. Acampamento: o piloto no helicóptero pousado | O piloto pelo vidro, com os soldados parados nesta partida |
| 3. Trincheiras: patrulha e o líder, com pistola | Combate: tiro, recarga, dano, morte e o rádio que o líder larga |
| 4. Aldeia: o soldado do encontro (mira e cai com a facada) | O mesmo do Samir, atalho 1: ande para o oeste |
| 5. Aldeia: volta de checkpoint com o soldado caído | O mesmo do Samir, atalho 2 |
| 6. Cerco: o líder na barreira, a patrulha e o piloto | Na Aldeia, com pistola, depois do plano de fuga |
| 7. Laboratório: combate com os soldados | Os três atacam a Elena |
| 8. Vitrine: os 4 soldados e as animações lado a lado | Tiro, recarga, dano e mortes se repetem |
| 9. Ferir o soldado mais perto / 10. Matar o soldado mais perto | Com o Play ligado |

Pendências dos soldados:
- O fuzil é provisório (formas simples).
- O piloto fica parado sentado, com a pose feita por script.
- No atalho 7 (laboratório), o contador da pistola apareceu "0 / 0" num teste; não foi investigado.

### Ninsar

Arte-fonte em `ArteFonte/Ninsar/`:

| Pasta | O que tem | No Git? |
| --- | --- | --- |
| `1_Meshy/` | Arquivos brutos baixados do Meshy | Não, exceto os .blend de 154 mil triângulos do manto e da saia e as texturas PBR do corpo (`Corpo/Ninsar Corpo/*.png`). O resto se baixa de novo no Meshy |
| `2_Rig/` | Corpo com esqueleto do AccuRig (`Ninsar_Corpo_Rig.fbx`, 101 ossos, com dedos) e as 19 animações do Meshy (`Ninsar_Animacoes/Ninsar_Animacoes.fbx`, esqueleto Mixamo de 28 ossos, **sem dedos**) | Sim |
| `3_Blender/` | `Ninsar_Montagem.blend` (montagem original do dono, não mexer), `Ninsar_Final.blend` (cópia de trabalho), `Texturas/` (texturas das peças tiradas de dentro do .blend), `Scripts/` (Python do Blender) e `Renders/` (conferência, fora do Git) | Sim, menos `Renders/` |
| `Imagens De Referencia/` | Folhas de referência do corpo, manto, saia e lança | Sim |
| `Texturas/` | Texturas do manto enviadas pelo dono (os nomes dos dois arquivos vieram trocados: `Ninsar_Tecido_Manto.png` é a faixa de bordado e `Ninsar_Faixa_Bordado.png` é o tecido) e, em `Processadas/`, as versões prontas: tecido repetível sem emenda (5 x 5 losangos), faixa recortada em um ciclo completo estrela–cuneiforme–estrela–cuneiforme, as duas no verdete do manto do Meshy, com normal map e máscara do ouro (`Scripts/preparar_texturas_manto.py`) | Sim |

Os scripts refazem tudo do zero, nesta ordem (no terminal, de dentro de `3_Blender`, com o `blender.exe` do Blender 5.2):
1. `blender -b Ninsar_Montagem.blend -P Scripts/etapa1_escala.py`: cria o `Ninsar_Final.blend`. A malha do corpo já media 1,90 m; só a Armature tinha escala 0,01. Tudo fica com escala 1. Tira a ação "0_T-Pose" do AccuRig do esqueleto (fica guardada): o encaixe, os pesos e o FBX usam a pose de repouso. Põe as texturas PBR do Meshy no corpo (mesmos UVs).
2. `blender -b Ninsar_Final.blend -P Scripts/etapa1_encaixe.py`: veste as peças. Os ajustes ficam no topo do script. O manto recua 9,5 cm, inclina 1° e cresce 3%; a saia vai da cintura (1,17 m) até 2 cm do chão, 8% mais larga. Onde o corpo ainda atravessa o pano, um empurrão suave leva o pano para fora (folga de 1,2 cm no manto e 0,8 cm na saia), mexendo as duas faces do pano juntas para não deformar o relevo. A parte da saia na altura do cinto de couro (que é do corpo) entra 4 mm sob a pele, para o cinto e o medalhão aparecerem por cima. Nas pernas (abaixo de 0,95 m, com transição até 1,12 m) não há empurrão: o pano fica com a forma original do Meshy, sem esticar os bordados; a física de tecido trata as colisões ali. O capuz é aproximado do crânio (folga de 1 a 2 cm no topo e nos lados, 3 a 4 cm em volta do rosto), fechando 85% do vão original, só do pescoço para cima. A lança vai para a mão direita: o cabo passa por baixo da palma, deitado e com a ponta para a frente na pose de braços abertos (fica em pé com o braço baixado e o antebraço para a frente).
3. Conferência: `Scripts/renderizar_vistas.py -- <prefixo> [objetos escondidos]` (frente, lado e costas) e `Scripts/renderizar_pose_lanca.py` (braço baixado segurando a lança). Ferramentas de medida: `medir.py`, `fatias.py`, `otimizar_encaixe.py`.

O `Ninsar_Final.blend` guarda essa versão vestida (o manto do Meshy encaixado fica só como referência). A pose de repouso continua a de braços abertos do AccuRig.

**Manto da Ninsar pausado; o manto gerado por script não alcançou a referência. Alternativas: capa curta com capuz abaixado, roupa feita no Marvelous Designer ou artista 3D freelancer; as texturas em `ArteFonte/Ninsar/Texturas/Processadas` servem para qualquer uma.** O que foi tentado (scripts guardados como histórico):
- `etapa2_manto_leve.py`: versão leve tirada do manto do Meshy. Falhou: a casca do Meshy é fina demais (cerca de 2 mm) para refazer a malha.
- `etapa2_manto_novo.py`: manto longo feito como molde (malha de quadrados, cavas, UV planificada por painéis, simulação de tecido). Ficou com cara de saco reto.
- `etapa2_manto_poncho.py`: capa-poncho com frente aberta, aba com franja nos ombros e capuz largo, simulada com os braços baixados (só na pose). Também não chegou à silhueta da referência.
- Ferramentas usadas: `comparar_referencia.py` (render ao lado da referência), `analisar_pano.py`, `medir_capuz.py`.

**Prévia na Unity** (`Art/Personagens/Ninsar`): `Ninsar_Previa.fbx` sai do Blender por `Scripts/exportar_previa_unity.py` (corpo, saia com pesos simples na pelve, coxas e canelas, sem física, e a lança como filha do osso da mão direita) junto com as texturas no formato do URP (`*_MetalSmooth.png`: R = metal, A = lisura). `Ninsar_Animacoes.fbx` é a cópia das 19 animações do Meshy. O menu **Nibiru > Personagens > Montar prévia da Ninsar** (`Scripts/Editor/Personagens/NinsarPreviewSetup.cs`) configura os dois FBX como Humanoid (cada um com o próprio avatar), cria os materiais URP, o controlador `Ninsar_Previa.controller` (um estado por animação, parada no lugar e seguindo a orientação do corpo), o prefab `Prefabs/Personagens/Ninsar_Previa` e a cena `Teste_Ninsar`. Componentes de teste em `Scripts/Testes` (`AnimationPreview`, `OrbitPreviewCamera`). Limitações: os dedos não fecham na lança (as animações do Meshy não têm dedos) e a saia não tem física.

Etapas que ficaram para depois: versão leve da saia com bake em 4096; pesos e ossos extras na faixa da saia; física de tecido (conferir a faixa verde da saia na coxa esquerda); poses de mão para os dedos segurarem a lança. A fenda da saia que mostra a coxa é proposital.

Pendências vistas na prévia (ainda não corrigidas):
- A saia, com os pesos simples, rasga nas costas: as faixas verdes se partem em pedaços durante a animação. Precisa de pesos com transição suave entre quadril e pernas, mais física de tecido.
- A lança fica deitada e com aparência esbranquiçada. A posição depende das poses de mão; a cor pede conferir o material dela (texturas e mapa de metal/lisura).

## Git

- Repositório: `github.com/BarizonDesenvolvimento/Nibiru-Game`, branch `main`.
- **Git LFS** ativo para modelos, imagens, áudio, vídeo e tipografia, e para todo binário em `ArteFonte/` (veja `.gitattributes`). O LFS guarda cerca de 800 MB (450 MB até a Aldeia, 260 MB da arte-fonte da Ninsar e 90 MB da prévia dela na Unity e das texturas do manto). Fora do LFS, o maior arquivo é a cena `Regiao1_SitioDeUr` (cerca de 1,2 MB); a `Memoria_Shuruppak` tem cerca de 0,9 MB. Cada nova versão de um arquivo grande ocupa espaço de novo na cota de LFS da conta.
- Tudo enviado ao GitHub até os soldados da Fundação, testados pelo dono (25 de outubro de 2026), inclusive a arte-fonte nova dos personagens (`Referencias/` e `2_Rig/`) e as animações do Mixamo em `ArteFonte/`. Os 4 soldados somaram cerca de 250 MB ao LFS (modelos e texturas em `Art/Personagens`). Commits em português, pequenos e frequentes.
- Nome da empresa no projeto (Project Settings): "Barizon Desenvolvimento".

## Como testar sozinho pela ponte MCP

- A ponte é o `unity-editor-mcp` (porta 7800). Se cair, a Unity provavelmente está reabrindo o projeto; espere ela terminar.
- Para testar comandos, crie um controle virtual (`InputSystem.AddDevice<Gamepad>`) e aplique o estado a cada quadro em `InputSystem.onBeforeUpdate`. O script `Scripts/Editor/BackgroundInputInEditor.cs` (menu **Nibiru > Testes**) faz o jogo aceitar comandos com a Unity em segundo plano, só no editor.
- Nunca recompile com o Play ligado: isso recarrega os scripts no meio da partida e quebra estados. Saia do Play antes e espere ele desligar de fato (`EditorApplication.isPlaying` falso): recompilar logo depois do comando de parar ainda pegou o Play saindo. Depois de recompilar pela ponte, espere a ponte voltar (ela cai durante a recarga) antes de ligar o Play; senão a recarga acontece no meio do Play (sinal: erros repetidos a cada quadro, como no `TrainingDummy`).
- Para olhar animações sem atrapalhar quem está jogando: uma cópia do modelo longe da cena, com o mesmo controlador e Animator em "Always Animate", posta num quadro exato com `Play(estado, 0, t)` + `Update(0)`, e uma câmera de teste própria.
- O dono do projeto às vezes joga ao mesmo tempo que os testes (inclusive com um controle de verdade): posição, vida e munição podem mudar entre um comando e outro.
- No prefab Elena, a raiz ("Elena") é só o grupo; o personagem é o filho "Jogador", com a etiqueta Player. Para reconhecer a Elena numa área, procure o `PlayerMotor` no objeto que entrou, não a etiqueta da raiz.
- Diálogos pausam o tempo do jogo (as sequências também param). Para avançar pela ponte, chame `DialogueRunner.Continue()` ou `Choose(i)`, um comando por vez.

## Perguntas ainda abertas

- O Kast aparece em pessoa ou só pelo rádio na região 1?
- Qual o peso da consequência na Aldeia ao devolver a tabuleta ao Poço?
- Nome definitivo do jogo, dublagem da Elena e idiomas de lançamento (do GDD).

## Próximos passos

1. Teste do dono nos passos 10 e 11; depois o passo 12 (o Primeiro Servo na antecâmara, com os andaimes e holofotes da arena), o passo 13 (a passagem atrás da parede do barco), o segundo líder e a passagem do helicóptero na base, o resto de "Remédio para Yusuf" (curativos à Dra. Weiss, antibióticos, entrega ao Yusuf e a escolha com o Samir) e a revelação dos filmes do Tom.
2. Seguir área por área: Zigurate, Poço e Wadi.
3. Pendências das Trincheiras: documentos da Fundação (falta uma tela de leitura de documentos) e o rádio de campanha avisando onde estão as patrulhas.
4. Pendência da Aldeia: ao voltar de um checkpoint depois do encontro, o soldado caído reaparece sempre na entrada leste, e não na entrada onde o encontro aconteceu (guardar a entrada escolhida no estado do mundo).
5. Personagens 3D, um por conversa: Yusuf, Umm Karim, Dra. Weiss, equipe morta (arqueólogos e trabalhador, que também serve de Hassan) e Primeiro Servo, cada um com a altura da tabela e trocando a cápsula onde ele aparece (use o `CharacterSetupKit` e o `NpcAnimator`, como no Samir). Soldados: fuzil de verdade e as pendências em "Soldados da Fundação". Samir: pose de mão na faca e as outras pendências em "Samir". Elena: corrida mais atlética (conferir a Run Forward), agachada parada e tiro/recarga de pistola (lista em "Elena"). Ninsar: decidir o caminho do manto (veja as alternativas em Personagens 3D) e terminar saia, física e poses de mão.
6. Versões simples do Grupo 2 (cenas, diário, crafting, save em disco). A memória jogável já tem a versão simples.
7. Memória de Shuruppak: hoje em formas simples; trocar por arte de verdade, com som de cidade, rio e a voz, quando a fatia ganhar arte.

### Como testar o passo 11

Atalhos no menu **Nibiru > Testes > Passo 11** (`Scripts/Editor/Step11Shortcuts.cs`), com o mundo como se a Elena tivesse atravessado a base e saído pelo nordeste, e com pistola, 24 balas e faca:

| Atalho | Onde começa |
| --- | --- |
| 1. Saindo da base, a caminho do Poço | No corredor nordeste; ande para o norte até a boca |
| 2. Na boca do Poço | No alto dos andaimes; desça pelos lances ouvindo o Lullu |
| 3. Descendo os andaimes, ouvindo o Lullu | No primeiro patamar |
| 4. Ponto de retorno do pé da descida (o Lullu no fundo) | No último patamar, em silêncio; agachada, passe por ele |
| 5. Depois do Lullu: rumo ao flash | Na antecâmara, sem o Lullu; ande para o norte |
| 6. Flash com o Lullu ainda vivo (ele congela) | Um passo antes do flash, com o Lullu no fundo |
| 7. Teste automático (resumo no Console) | Roda os testes sozinho e escreve OK ou FALHOU no Console (`Scripts/Editor/Step11Tests.cs`) |

Para ver o Lullu perseguir, corra ou atire perto dele; para ele não ouvir, agache. Do andaime ele não alcança a Elena.

### Como testar o passo 10

Atalhos no menu **Nibiru > Testes > Passo 10** (`Scripts/Editor/Step10Shortcuts.cs`), com o mundo como se a Elena tivesse feito o plano de fuga e entrado na base, e com pistola, 24 balas e faca:

| Atalho | Onde começa |
| --- | --- |
| 1. Entrando na base | No ponto de retorno da entrada. Atravesse o pátio até a tenda de comando, a leste |
| 2. Atrás dos contêineres, perto do laser | Agachada atrás dos contêineres do sul; a avenida à frente fica à vista do atirador |
| 3. Na avenida, à vista do atirador | Parada no aberto: o laser trava, o bipe toca e, depois do aviso, ele atira (a patrulha vem) |
| 4. Na rota alta, atrás do atirador | No 2º terraço, agachada: chegue perto e use a faca ("Golpe pelas costas") |
| 5. Atrás do líder, na porta da tenda | Agachada sob o toldo, atrás do líder: o aviso do golpe aparece na hora |
| 6. Na tenda de comando, diante da mesa | Examine a mesa: e-mail, foto e a fala da Elena |
| 7. Depois dos documentos: o painel do portão | Use o painel e olhe pela porta: o portão do muro oeste abre |
| 8. Portão aberto: saindo para o Poço | Ande para o norte, entre o zigurate e a cerca leste |
| 9. Teste automático (resumo no Console) | Roda os testes sozinho e escreve OK ou FALHOU para cada um no Console (`Scripts/Editor/Step10Tests.cs`) |

Para ver a chegada ao vivo (a fala do laser), use o atalho 5 do passo 9 e ande para o leste.

**Elena invencível nos testes:** **Nibiru > Testes > Elena invencível nos atalhos** (ligado por padrão, com a marca de visto) faz os atalhos de todos os passos começarem com a vida cheia e sem perder vida (`Health.Invincible`, só ligado pelos atalhos). Desligue para testar dano e morte (por exemplo, no atalho 3).

### Como testar o passo 9

Atalhos no menu **Nibiru > Testes > Passo 9** (`Scripts/Editor/Step9Shortcuts.cs`), com o mundo como se a Elena tivesse lido a tabuleta e respondido ao Samir:

| Atalho | Onde começa |
| --- | --- |
| 1. Logo depois da conversa da tabuleta | De frente para o nicho; o Samir sai andando para a beira da aldeia |
| 2. Na beira da aldeia, antes do cerco (o Samir revelou que é Guardião) | Alguns passos ao sul do Samir: ande para o norte até ele. Na conversa aparecem as duas perguntas |
| 3. Na beira da aldeia, antes do cerco (o Samir não revelou) | O mesmo, mas a Elena contou a verdade no passo 8: só aparece "Você não vem comigo?" |
| 4. Depois do plano: passar pela patrulha | No ponto de retorno da beira, virada para a estrada, com a patrulha andando |
| 5. Na ponta sul do muro, entrando na base | A poucos passos da entrada da base: ande para o leste |

### Como testar o passo 8

Os atalhos de todos os passos usam a mesma base, `Scripts/Editor/TestShortcuts.cs` (abre a região, liga o Play, monta o mundo e um checkpoint no lugar pedido e espera a cena recarregar). Atalhos no menu **Nibiru > Testes > Passo 8** (`Scripts/Editor/Step8Shortcuts.cs`): cada um abre a região, liga o Play e põe a Elena no momento certo, com o mundo como se ela tivesse jogado até ali (e um checkpoint de frente para o nicho). Funcionam com o Play desligado ou ligado.

| Atalho | Onde começa |
| --- | --- |
| 1. No nicho, pronto para ler a tabuleta | De frente para o nicho, com "Ler a tabuleta" na tela |
| 2. Começo da memória de Shuruppak | Entrando na memória, no começo da rua |
| 3. Na porta da cabana de junco | Na memória, de frente para a porta da cabana |
| 4. Volta da memória: tabuleta, marca e Samir | De volta à casa: tela da tabuleta, a marca e a conversa com o Samir |
| 5. Jogo novo no Acampamento | Começo do jogo, para conferir que a tabuleta não se lê pelo inventário |

Jogando do jeito normal:

1. Abra `Regiao1_SitioDeUr` e jogue até descansar no nicho (ou volte do checkpoint do nicho).
2. Aperte E (ou A) no nicho: "Ler a tabuleta". A pedra acende em azul e a lamparina encolhe.
3. Em Shuruppak, ande pela rua até a cabana de junco à beira do rio (dá para falar com a menina e o pescador). Dentro da cabana, a voz fala e a parede do fundo brilha em azul.
4. De volta à casa: a tela da tabuleta, a marca azul no pulso esquerdo e a conversa com o Samir. Escolha uma resposta.
5. Aperte Q (ou LB): o Eco do Abzu acende a escrita escondida na parede do fundo.
6. Para conferir a trava: num jogo novo, no Acampamento, usar a tabuleta pelo inventário só deve mostrar "Não aqui."
