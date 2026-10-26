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
- A **base do zigurate** (área 4) começou: o **passo 10** (atravessar a base, com a patrulha, o líder, o atirador de elite no alto do zigurate, o golpe de faca pelas costas, o e-mail do Kast e a foto de 1979, o portão para a Aldeia e a saída para o Poço) está pronto, testado pela ponte e **aprovado pelo dono no Play**. Veja "Base do zigurate, passo 10".
- A **Boca do Poço** (área 5) começou: o **passo 11** (descer o Poço ouvindo o Lullu lá embaixo, passar por ele, ponto de retorno e o flash a uns 12 m do Selo de Kur, com a Voz) está pronto, testado pela ponte e **aprovado pelo dono no Play**. Veja "Boca do Poço, passo 11".
- O **passo 12** (a luta com o Primeiro Servo na antecâmara, em três fases, com os holofotes que caem e atraem o chefe) está pronto em blockout, com o **modelo definitivo do Primeiro Servo**, testado pela ponte e **testado no Play pelo dono e aprovado no funcionamento** (o equilíbrio ainda precisa de ajuste, veja "Pendências para o teste completo da fatia"). Veja "Antecâmara do Poço, passo 12".
- O **passo 13** (a passagem dos Guardiões: ler a inscrição na parede do barco de junco, a tabuleta pulsa e a parede afunda; o túnel com lamparinas acesas; a fita de Irena nº 1 que toca enquanto a Elena anda; o salvamento no topo da rampa, na saída para o wadi) está pronto em blockout, testado pela ponte e **aprovado pelo dono no Play**. Veja "Passagem dos Guardiões, passo 13".
- O **passo 14** (o wadi: um leito de uns 60 m com curva, tempestade de areia provisória, um líder de esquadra, o rádio do Kast que espera a fita de Irena terminar e a primeira escolha de facção, com três opções) está pronto em blockout, testado pela ponte e **testado no Play pelo dono e aprovado**. Fechado. Veja "Wadi, passo 14".
- O **passo 15** (a fuga na caminhonete do Samir até o fim da demo: nicho do túnel para quem devolve a tabuleta, embarque, viagem em trilho com a conversa "A estrada" de cada escolha, diário, tela de fim da demo e menu principal provisório) está pronto em blockout, testado pela ponte e **testado no Play pelo dono e aprovado** (as três escolhas, o nicho, a pausa, o diário, o fim da demo e o menu principal). Fechado, menos as animações de sentado e de dirigir, que o dono vai trazer. Veja "Fuga, passo 15".
- A **Ninsar** tem uma prévia animada na Unity (corpo, saia e lança, sem manto), na cena `Teste_Ninsar`. O manto dela está pausado. Veja "Ninsar" em Personagens 3D.
- A **Elena definitiva** (Meshy + AccuRig, com dedos, 1,70 m) substituiu a provisória no prefab `Prefabs/Player/Elena`, com animações do Mixamo: parada, andar, correr, agachar, rolar, mirar, tiro (coice), recarregar, faca, dano e morte. Testada pelo dono. Veja "Elena" em Personagens 3D.
- O **Samir definitivo** (Meshy + AccuRig, com dedos, 1,75 m) substituiu a cápsula em todos os lugares onde ele aparece (Aldeia, passos 6 a 9, e laboratório), com animações do Mixamo: parado, andar, correr, agachado, golpe de faca e 4 gestos de conversa. Testado e aprovado pelo dono. Veja "Samir" em Personagens 3D.
- Os **4 soldados da Fundação** (Soldado, Líder de esquadra, Atirador de elite e Piloto; Meshy + AccuRig, com dedos) substituíram as cápsulas no prefab `Prefabs/AI/Soldado` e em todas as cenas, com animações de fuzil do Mixamo (parado, patrulha, corrida, mira, tiro, recarga, dano e morte), fuzil provisório e a IA e os eventos de antes. O piloto vai sentado nos helicópteros; o atirador de elite está no posto dele, no passo 10. Testados e aprovados pelo dono. Veja "Soldados da Fundação" em Personagens 3D.
- O **Lullu definitivo** (Meshy + AccuRig, com dedos, 2,10 m em pé) substituiu a cápsula no prefab `Prefabs/AI/Lullu`. Testado pela ponte; falta o teste do dono. Veja "Lullu" em Personagens 3D.
- O **Primeiro Servo definitivo** (Meshy + AccuRig, com dedos, 2,50 m em pé, máscara de ouro) está no prefab `Prefabs/AI/PrimeiroServo`, usado no passo 12 e no laboratório. Veja "Primeiro Servo" em Personagens 3D.
- A secundária **"Remédio para Yusuf"** está completa em blockout (os dois caminhos, a entrega, o prazo, o lembrete e a pergunta do Samir na viagem), testada pela ponte e **testada no Play pelo dono e aprovada**. Veja "Secundária Remédio para Yusuf".
- Os outros personagens (Yusuf, Umm Karim, Dra. Weiss e equipe morta) estão prontos na `ArteFonte`, com esqueleto e dedos, esperando a importação, um por conversa. Lista e alturas em Personagens 3D.

**Os passos 1 a 15 estão todos aprovados pelo dono no Play.** O passo 12 segue com o equilíbrio da luta pendente para o teste completo da fatia. As **animações da biblioteca** (`Docs/CATALOGO_ANIMACOES.md`) foram importadas para as criaturas (garra, investida, urro, golpe recebido e morte do Lullu e do Primeiro Servo) e para o Samir dirigindo (veja "Animações da biblioteca" abaixo), testadas pela ponte e **aprovadas pelo dono no Play** (vitrines das criaturas e da Elena, viagem do passo 15 com a Elena sentada e o Samir ao volante, passo 11 com o Lullu na caça e passo 12 com o Servo acordando e urrando). **O passo de importação de animações está aprovado.** O `zombie stand up` ficou importado e só na vitrine (nenhuma mecânica de surgimento pelas rachaduras por enquanto), e as pastas Pro Magic, Magic Locomotion e Scary Zombie seguem com **origem não confirmada, não usar até confirmação**. A Elena também já viaja sentada no chão da caçamba e tem agachada parada de verdade. Depois, importar os outros personagens (o kit comum já serve para eles). A corrida da Elena ainda é provisória (veja "Elena"): a Female Basic Locomotion está na vitrine para comparar.

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
| `Scenes/MenuPrincipal` | Menu principal provisório (Novo jogo, Sair). Primeira da lista do Build; a tela de fim da demo volta para ele. Criada por **Nibiru > Conteúdo > Criar menu principal provisório** |
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
| 6. Wadi | **Nibiru > Conteúdo > Povoar Wadi na cena aberta** | `Scripts/Editor/Content/WadiContent.cs` |
| 7. Fuga (passo 15) | **Nibiru > Conteúdo > Povoar Fuga na cena aberta** (rode depois do Poço e do Wadi). O menu principal vem de **Nibiru > Conteúdo > Criar menu principal provisório** | `Scripts/Editor/Content/EscapeContent.cs` |
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
- **Sem canto que prenda a Elena** (vale para o Lullu e para o Primeiro Servo do passo 12): o Lullu a encurralava num canto da antecâmara e golpeava sem parar. O blockout (`BuildWell`) mudou: as plataformas oeste e leste vão até a parede (acabaram as frestas de 1 m, becos sem saída); cada uma tem duas rampas (dá para dar a volta por cima); os cantos da antecâmara e do fundo do Poço são cortados em diagonal (`Ante_Canto_*`, `Poco_Canto_*`); a rampa da descida junto da parede sul ganhou uma guarda (`Lance_4_Guarda`) e o pé da 3ª rampa um pilar (`Patamar_3_Base`). **Ferramenta:** **Nibiru > Testes > Conferir cantos sem saída (Poço)** (`Scripts/Editor/RoomTrapCheck.cs`) põe um inimigo grande (raio 1,1 m, cobre o Servo) a 1,4 m da Elena em 8 direções, em cada ponto andável da malha, e vê se ela ainda chega a 5 m dele; escreve no Console um mapa de texto (número = em quantas direções fica presa). Passou de 429 pontos presos para 23: sobram só pontos de 1 direção em cantos côncavos junto às plataformas e rampas, e os cantos da própria descida de andaimes (o Lullu não sobe lá). Para outra arena, mude os limites no topo do script. Se mexer no blockout, rode **Construir Sítio de Ur na cena aberta** e salve a cena (isso refaz a malha de navegação).
- **Blockout:** o vão da passagem dos Guardiões na parede oeste da antecâmara agora está fechado pela `Parede_Barco_Junco`, com o barco de junco sobre ondas em ouro e a lamparina quente. O passo 13 a abre (a parede e o barco ficam no grupo `Parede_Movel`, que afunda no chão).
- **Pendências do Poço:** o Lullu já tem modelo e animações (veja "Lullu" em Personagens 3D); sons são sintetizados; a Voz é só legenda; o Lullu que sobrevive ao flash segue vagando para o passo 12; a inscrição dos Guardiões do Poço (colecionável) e o segundo Lullu ficam para depois.
- **Teste do flash com o Lullu vivo (corrigido):** ele falhava às vezes porque o atalho 6 punha a Elena em z = 89,4 e o colisor dela (0,5 m de raio) encostava exatamente na área do flash (z = 89,9): o flash podia começar sozinho no nascimento. O atalho agora a põe em 88,8 e dispara "passou pelo Lullu" logo depois de carregar (o Lullu continua vivo, como no jogo); o teste não teleporta mais para z = 84,6 (onde ela entrava numa rampa) e acusa se o flash começar antes da hora. Passou 3 vezes seguidas.
- Teste feito pela ponte (teste automático, tudo OK): chegada à boca, descida caminhável pelos 4 lances, retorno em silêncio, Lullu preso ao chão (barulho do andaime: caça, não sobe, não golpeia), parede do barco, flash com o Lullu vivo (congela e solta) e sem ele.

**Antecâmara do Poço, passo 12 (o Primeiro Servo):**
- Montado pelo menu da Boca do Poço (`Scripts/Editor/Content/FirstServantContent.cs`, chamado pelo `WellContent`), em `Conteudo_Poco/Poco_Ativo/Primeiro_Servo`.
- Objetivos (passos 25 e 26 da missão): "Sobreviva ao Primeiro Servo" (`VenceuOPrimeiroServo`) e "Procure a passagem atrás da parede do barco de junco" (`ChegouAParedeDoBarco`, numa área diante da parede). O passo 13 acrescenta os passos 27 e 28 (veja "Passagem dos Guardiões, passo 13").
- **Despertar** (quando a Voz do flash acaba, só ao vivo): o Selo estala (`PrimeiroServoAcordou`), o Servo aparece junto à porta, a uns 9 m da Elena, e **urra parado** 1,8 s; o Selo clareia e os holofotes piscam. O Lullu, se vivo, **foge** para o fundo do poço e some (`LulluAI.FleeTo`), e não volta à luta. Fala da Elena (`Poco_OPrimeiroServo`), dica da esquiva e do Eco, e o **ponto de retorno da luta** na boca da antecâmara (`Checkpoint_Luta`, z = 83, de frente para o Selo). Aparece a **barra de vida** do Servo no alto da tela, com marcas onde começam as fases 2 e 3.
- **Voltando do checkpoint durante a luta:** o Servo já está acordado, vagando junto ao Selo, com a vida cheia; não há flash, fala nem urro, e o Lullu não está mais lá.
- **Fases** (pela vida; cada troca tem sinal claro: ele **para e urra**, a **máscara racha** soltando lascas de ouro com um estalo, os **olhos de lápis-lazúli** acendem mais, o **Selo clareia** e os **holofotes piscam**):
  - Fase 1 (100% a 60%): caça pelo som, golpe de perto e investida de 3,5 a 9 m (a esquiva atravessa). Preso ao chão, como o Lullu: das plataformas a Elena está a salvo.
  - Fase 2 (60% a 25%): **sobe as rampas** atrás da Elena e faz **duas investidas seguidas**. Na troca aparece a dica dos holofotes.
  - Fase 3 (abaixo de 25%): furioso, prepara mais rápido (x0,7), mas fica mais tempo parado depois de golpear ou investir (x1,6): é a hora de atacar.
- **Armadura e Eco:** tiro e faca tiram só 25% do dano; atordoado pelo Eco (3 s), tiram 150%. O Eco continua com a recarga de 12 s que já tinha (`Data/Powers/EcoDoAbzu`), então a luta é esquivar e esperar o Eco, não repetir o Eco sem parar. A investida só sai no mesmo piso (de baixo de uma plataforma ou de cima de uma rampa, não), e ninguém golpeia através de uma plataforma (vale para o Lullu também). Preso ao chão, um som no alto vira o ponto do chão mais perto, procurado em volta e só no nível do chão: antes, debaixo da plataforma oeste (maciça), o ponto mais perto era o topo dela e ele subia pela rampa. O Lullu do passo 11 agora espera no chão em volta do andaime, a uns 4 m da Elena, em vez de exatamente embaixo.
- **Holofotes** (`Combat/FallingFloodlight`): 4 postes com holofote branco e frio (um na plataforma oeste, dois na leste e um no canto noroeste), apontados para o meio da arena. Um tiro ou a faca derruba; a queda faz um **estrondo** (ruído de 40 m que não é da Elena): o Servo (e qualquer Lullu) vai até lá e, por 5 s, **não ouve os passos dela** e fica olhando para o holofote. Encostar nele ou feri-lo acaba com a distração. Caído, o holofote continua aceso no chão.
- **Recursos na arena** (aparecem com o despertar): 8 balas na plataforma oeste e 2 curativos (plataforma leste e boca da antecâmara).
- **Vitória:** ele tomba diante do Selo (animação de morte), a barra some, o azul do Selo enfraquece para 30% em 3 s, a Elena fala (`Poco_OServoCai`), um ponto de retorno é marcado de frente para a parede do barco (`Checkpoint_Depois_Do_Servo`) e o objetivo aponta a parede. Ao voltar de um checkpoint, ele continua caído.
- **Para ajustar a luta jogando** (mudanças com o Play ligado continuam valendo depois):
  - `Data/AI/PrimeiroServo_Luta`: **vida** (750), em que fração da vida começam as fases 2 e 3, e por fase: preparação, cansaço, velocidade, investidas seguidas e se sobe rampas; tempo do urro; as dicas na tela.
  - `Data/AI/PrimeiroServo`: **dano** do golpe (25) e da investida (30), **tempos** (preparação do golpe 0,5 s e da investida 0,6 s, recuperação 0,8 e 1,2 s, intervalo entre investidas 4 s), tempo atordoado (3 s), armadura (0,25 e 1,5), sentidos e a distração (alcance mínimo do estrondo e quantos segundos).
  - Os valores iniciais miram uma luta de 3 a 5 minutos, com um jogador mediano morrendo no máximo 2 ou 3 vezes: cada Eco bem usado tira uns 15% da vida dele.
- **Blockout** (`BuildWell`): a marca `Chefe_Primeiro_Servo` foi para junto do Selo (z = 99); a rampa oeste ficou mais curta (37°, termina em z = 94, fora da frente da parede do barco); as luzes soltas de holofote saíram (os holofotes de verdade são do conteúdo). A ferramenta de cantos sem saída continua com 23 pontos, os mesmos de antes.
- Sons provisórios novos: `Servo_Urro`, `Mascara_Racha` e `Holofote_Queda`.
- Os olhos são um foco azul saindo do rosto para a frente (uma luz em volta da cabeça pintava o corpo curvado todo de azul).
- Teste feito pela ponte (teste automático, 10 itens, tudo OK em três rodadas seguidas): despertar com urro, Lullu fugindo e retorno na boca; volta do checkpoint sem repetir nada; fase 1 presa ao chão; troca para a fase 2 com todos os sinais; subida da rampa na fase 2; distração do holofote; armadura e Eco; troca para a fase 3; vitória até a parede do barco; volta depois da vitória com o Servo caído. Os atalhos 3, 4, 5 e 8 foram conferidos à parte. O teste do passo 11 continua todo OK com a IA nova.
- **Pendências:** o golpe e a investida usam a estocada de faca do Mixamo (falta um golpe de garra e uma animação de urro); a máscara rachando são lascas soltas (o modelo não muda); o Servo sai das rachaduras aparecendo de uma vez (falta uma animação de saída); sons sintetizados; os holofotes e a arena em formas simples.

**Passagem dos Guardiões, passo 13 (atrás da parede do barco de junco):**
- Montado pelo menu da Boca do Poço (`Scripts/Editor/Content/GuardianPassageContent.cs`, chamado pelo `WellContent`), em `Conteudo_Poco/Poco_Ativo/Passagem_Dos_Guardioes`. Rode antes o menu do blockout (a parede e os glifos agora estão no grupo `Parede_Movel`).
- Objetivos (passos 27 e 28 da missão): "Leia a inscrição na parede do barco de junco e abra a passagem" (`AbriuAPassagem`) e "Siga a passagem dos Guardiões até o wadi" (`SaiuDaPassagem`). Depois do 28 a missão aparece como concluída, até o passo 14 existir.
- **Abrir a parede:** de frente para a parede, "Ler a inscrição" (`DialogueStarter` na área `Ler_Inscricao`; a faixa de glifos dourados fica sobre o barco). A Elena lê em voz alta (conversa `Passagem_Inscricao`, legenda), a tabuleta pulsa numa **luz azul** sobre a inscrição (2 pulsos), a parede **afunda no chão** em 4 s, com som de pedra e poeira, e some (colisão e desenho). Só então vale `AbriuAPassagem`. Fala `Passagem_Aberta` ("Alguém ainda cuida deste caminho") e, logo depois, o **único ponto de retorno** da passagem (`Checkpoint_Passagem_Aberta`, de frente para o túnel). Voltando dele, a parede já está aberta, sem pulso nem conversa (`GuardianPassage.SnapOpen`).
- **Túnel** (blockout que já existia): 4 lamparinas de barro **acesas** (também na arte final: alguém cuida da passagem; luz quente, a chama treme), som ambiente (vento e pingos). Sem inimigos nem enigma: os Guardiões aparecem só como marca, lamparinas e a fita.
- **Fita de Irena nº 1** (`Documents/TapeDefinition`, `TapePickup`, `TapePlayer`; legenda no pé da tela por `UI/SubtitleUI`): "Pegar a fita" num degrau de pedra. Toca **enquanto a Elena anda**, sem pausar o jogo nem tomar o controle (legenda mais o chiado de fita; sem voz gravada ainda). Pegar fica registrado no evento `ColetouAFitaDeIrena1` e terminar no `FitaDeIrena1Terminou`: a futura tela de documentos e fitas (a mesma da foto de 1979) lê daí (`TapeDefinition.IsCollected` e `IsFinished`). Sem menu. O `TapePlayer` fica num objeto que nunca desliga, então **sair do túnel não corta a fita**. Se a Elena voltar de um salvamento guardado com a fita pega e não terminada, ela toca de novo do começo.
- **Passo 14 (feito):** o rádio do Kast no wadi espera a fita terminar (`Dialogue/RadioAfterTape`, veja "Wadi, passo 14").
- **Saída:** no topo da rampa (z = 139), `SaiuDaPassagem` e um **salvamento automático** (`Autosave_Topo_Da_Rampa`). A caminhonete do Samir (z = 143,5 em diante) fecha o fim da rampa; o passo 14 parte daí.
- **Voltar à antecâmara:** com a passagem aberta nada trava: a parede não fecha mais, a inscrição some, o Servo continua caído e nenhum evento ou conversa se repete.
- **Câmera no túnel estreito:** o teste varre 1536 posições de câmera (16 pontos do túnel e da rampa, 12 direções, 4 inclinações, mirando ou não). Achou um defeito: na porta da parede (dentro da espessura dela) a câmera passava pelo canto. Corrigi em `ThirdPersonCameraMode` com uma conferência final do ponto seguido até a câmera, que vale para o jogo inteiro (a câmera só chega um pouco mais perto quando a linha bate em algo).
- **Para ajustar jogando** (mudanças com o Play ligado continuam valendo depois):
  - `Data/Passagem/PassagemDosGuardioes`: tempo e profundidade da descida da parede (4 s, 4,4 m), cor, intensidade, tempo e quantidade dos pulsos da tabuleta (azul, 5, 1,2 s, 2 pulsos), cor, intensidade, alcance e tremor das lamparinas (1,6, 6 m, 0,12) e volume do som do túnel.
  - `Data/Tapes/Fita_Irena_1`: o texto (8 falas), o nome, a legenda "Irena, 1979", os tempos da legenda (letra a letra, mínimo por fala, pausa) e, quando houver, a voz (`voice`). O texto atual é **provisório**.
- Sons provisórios novos: `Parede_Abrindo` e `Passagem_Ambiente_Loop`.
- **Provisório / para arte e polimento:** túnel, parede, glifos, lamparinas e a fita em formas simples; poeira de quadradinhos; sons sintetizados; fita só com legenda (falta a voz de Irena); a tabuleta é uma luz, sem animação; kit de túnel, entalhes em relevo e decals.
- **Pendências:** nenhuma nova além das acima. As outras duas inscrições dos Guardiões (incluindo a do Poço) continuam pendentes, como antes.
- Teste feito pela ponte (teste automático, 7 itens, tudo OK): leitura, pulso e parede afundando com o ponto de retorno; volta do ponto de retorno com a parede já aberta; túnel caminhável da parede ao topo da rampa, lamparinas acesas, só dois pontos de retorno, salvamento e fim do passo; fita pega, tocando com o controle livre, não cortada ao sair e terminando; fita retomada ao voltar do salvamento; câmera; e a volta à antecâmara. O teste do passo 11 (que usa a parede e a câmera) continua todo OK.

**Wadi, passo 14 (tempestade, o rádio do Kast e a primeira escolha de facção):**
- Montado pelo menu **Nibiru > Conteúdo > Povoar Wadi na cena aberta** (`Scripts/Editor/Content/WadiContent.cs`), em `Conteudo_Wadi/Wadi_Ativo`, que só liga quando a passagem abre (`AbriuAPassagem`). Rode antes o menu da Boca do Poço (o Wadi usa os eventos e a fita dele) e, se refizer o blockout, rode o do Poço e depois o do Wadi.
- **Blockout novo** (`BuildPassageAndWadi`): o leito tem uns 60 m em três trechos. A: do topo da rampa (32, 141) para o norte até z = 150. B: a curva para leste (x de 24 a 66, z de 150 a 164). C: de novo para o norte (x de 54 a 66, z de 164 a 180), com a caminhonete do Samir no fim (60, 176). A margem norte do trecho B é o canto de fora da curva e corta a vista longa: do topo da rampa não se vê o trecho C. Pedras e a carcaça de um carro no trecho B dão cobertura. A malha de navegação foi refeita.
- Objetivos (passos 29 a 31 da missão): "Siga o wadi até o soldado ferido" (`ChegouAoSoldadoFerido`), "Ouça o rádio do soldado" (`RadioDoKastComecou`) e "Decida o que fazer com a tabuleta" (`EscolheuONoWadi`). Depois do 31 a missão aparece como concluída, até o passo 15 existir.
- **Tempestade** (`Environment/Storm`, `StormController`, `StormSettings`; dados em `Data/Tempestade/TempestadeDoWadi`): só o que mexe na jogabilidade, provisório. A intensidade sobe com o z da Elena, de 138 (topo da rampa) a 150, e desce se ela voltar. No máximo: névoa de areia (`RenderSettings`), a Elena anda a 90% (`PlayerMotor`), os soldados enxergam 55% do alcance normal e ouvem passos e golpes a 50% do alcance (`SoldierPerception`); **tiros não são cobertos**. Vento em loop provisório (`Vento_Tempestade_Loop`). Fora do wadi (intensidade 0) todos os multiplicadores valem 1 e o jogo é o de sempre.
- **Líder de esquadra** (`Lider_Do_Wadi`, o mesmo prefab e a mesma IA dos soldados, sem comportamento novo): patrulha rente à margem norte do trecho B (de x = 38 a 46, z = 162,2). A margem sul fica a mais de 11 m dele, fora da vista dele na tempestade (20 m x 0,55): dá para passar agachada sem ser vista. O combate é possível (ele chama reforços como os outros líderes).
- **Ambiente:** um veículo da Fundação com faróis passa de vez em quando sobre a margem norte e três vultos de soldados andam pela margem leste do trecho C (`Sequences/LoopingPasser` liga, anda e desliga; sem colisão nem inteligência). Provisório; vozes não têm áudio ainda.
- **Achar o soldado sem HUD:** o rádio caído chia (`Radio_Chiado_Loop`, audível a até 60 m) e tem um LED vermelho que pisca (`Environment/BlinkingLight`), ponto de luz provisório. Depois da escolha, o chiado se cala e o LED apaga.
- **Rádio do Kast** (`Dialogue/RadioAfterTape`): ao cruzar a faixa da entrada do trecho C (`ChegouAoSoldadoFerido`), o rádio **espera a fita de Irena nº 1 terminar** e começa sozinho, sem a Elena voltar ao gatilho. Se a fita nunca foi pega, ou já terminou (inclusive num jogo carregado de depois dela), começa na hora. Nunca espera para sempre: o teto é a duração real da fita (`TapeDefinition.PlaybackSeconds`: o clipe de voz, ou o tempo das legendas enquanto não há voz) mais 5 s, contado em tempo de jogo (com o jogo pausado o relógio não anda). Outra conversa em andamento também segura o rádio. Duas versões da fala de abertura, pelo que a Elena respondeu no rádio do acampamento (`KastDesconfiou`): `Data/Dialogues/Wadi_RadioKast` e `Wadi_RadioKast_Desconfia`. Textos provisórios.
- **A escolha** ("O que fazer com a tabuleta", no fim da conversa do rádio: o Kast promete soltar a Aldeia, o Samir entra na frequência e diz que a tabuleta tem de voltar ao Poço, a Voz sussurra "Não abra mão de nada"):

| Opção | Reputação | Evento que os próximos passos leem | Efeito escondido |
| --- | --- | --- | --- |
| Deixar a tabuleta no chão para o Kast | Fundação +15, Guardiões -15 | `EntregouATabuleta` | nenhum |
| Levar a tabuleta de volta ao Poço, com o Samir | Guardiões +15, Fundação -15 | `DevolveuATabuleta` | nenhum |
| Ficar com a tabuleta | Fundação -5, Guardiões -5 | `FicouComATabuleta` | transformação +3, **sem aviso** (`Tablets/SilentTransformationGain`); a única pista é a fala da Voz ("Sábia") |

  Qualquer opção dispara também `EscolheuONoWadi` ao fim da conversa (fecha o passo 14). A reputação já vai no `WorldState` (volta certa de um ponto de retorno). A escolha fica guardada pelos eventos, que o passo 15 e a região 2 leem com `StoryEvents.HasBeenRaised`. O poder da Elena fica com ela em qualquer opção, como na especificação. A barra de transformação do HUD lê o valor sozinha e mostra o total (3 pontos a mais); só não há aviso nem número novo.
- **Pontos de retorno:** `Autosave_Topo_Da_Rampa` (passo 13), `Checkpoint_Antes_Do_Radio` (atravessa o trecho B inteiro e devolve a Elena à margem sul, longe do líder; o rádio recomeça ao voltar) e `Autosave_Depois_Da_Escolha` (só liga depois da escolha, junto do soldado). Ao voltar do salvamento depois da escolha: escolha, reputação e transformação continuam, o rádio está calado e nada se repete.
- **Ainda não faz (por decisão):** "Devolver ao Poço" só marca a escolha e mostra a reação do Kast e do Samir; a volta pela passagem e o enterro da tabuleta no nicho não existem. **Pendência para o plano do passo 15:** a fuga na caminhonete precisa considerar as **três** escolhas (as reações do Samir mudam: furioso, de acordo ou em silêncio) e a região 2 lê `EntregouATabuleta`, `DevolveuATabuleta` e `FicouComATabuleta`.
- **Provisório / para arte e polimento:** tempestade só com névoa, lentidão e vento (faltam partículas, céu laranja, rajadas, cor e luz definitivas); soldado ferido como cápsula, rádio e veículo em caixas; sem voz do Kast, do Samir e dos soldados; sons sintetizados; falas provisórias.
- **Para ajustar jogando** (mudanças com o Play ligado continuam valendo depois): `Data/Tempestade/TempestadeDoWadi` (onde a tempestade começa e fica cheia, lentidão, alcance da audição e da vista dos soldados, névoa, vento); `Data/Tapes/Fita_Irena_1` (a margem de segurança do rádio fica no `RadioAfterTape`, 5 s).
- **Avisos de reputação (ajustados no teste do dono):** `Notifications.PostImportant` (usado por `Reputation.Apply`) mostra o aviso no **meio da tela, acima da Elena**, numa linha só, com texto só um pouco maior que o comum (fonte normal, contorno fino), por **9 s**, com fade de 0,6 s ("Fundação passa a confiar menos em você (-5)"). Os avisos comuns continuam pequenos no canto. **Durante uma conversa todos os avisos esperam** (escondidos, sem contar tempo) e aparecem quando ela acaba; antes eles sumiam enquanto a Elena lia a resposta. Ajustes no `NotificationUI` do HUD: "Important Duration", "Important Fade", "Important Height" e "Important Scale".
- **Cuidado ao criar interface por código (travamento que houve):** a primeira versão do aviso clonava o texto com `Instantiate`, mas o texto fica no mesmo objeto do `NotificationUI`, então o clone levava o componente junto e ele clonava de novo, sem parar (estouro de pilha e a Unity despejando milhões de linhas no console, que continuaram a cada Play até reiniciar a Unity). Regra: **nunca clone, por código, um objeto que tem o próprio script que está clonando**; crie o objeto novo com `new GameObject` e copie só o que precisa (fonte, tamanho). Se a Unity travar assim, pare o Play e reinicie o editor.
- Teste feito pela ponte (teste automático, 10 itens): veja "Como testar o passo 14".

**Fuga, passo 15 (a caminhonete do Samir até o fim da demo):**
- Montado pelo menu da Fuga (`EscapeContent.cs`), em `Conteudo_Fuga`. A caixa `Caminhonete_Samir` do blockout é desligada pelo menu e dá lugar à caminhonete de verdade (chassi, capô, cabine aberta, caçamba sem tampa, rodas e faróis), no mesmo lugar (60, 176). Se refizer o blockout, rode o menu da Fuga de novo.
- Objetivos (passos 32 a 34 da missão): "Leve a tabuleta ao nicho dos Guardiões, no túnel" (`TabuletaResolvida`), "Entre na caminhonete do Samir" (`EmbarcouNaCaminhonete`) e "Fuja para Tell Amar" (`FimDaViagem`). Quem **entrega** ou **fica com** a tabuleta tem o primeiro passo cumprido na hora (o evento dispara com a escolha); só quem **devolve** o vê. Depois do passo 34 a missão "Sobrevivente" se conclui.
- **Samir:** só aparece na caminhonete depois da escolha (entregou ou ficou) ou depois de a tabuleta ser enterrada (devolveu). A escolha do passo 3 (confiar ou desconfiar do Kast) **não** tem efeito na fuga (decisão do dono).
- **Devolver ao Poço:** aparece o **nicho dos Guardiões no túnel** (parede oeste, depois da lamparina 2: pedra baixa, lamparina de barro com luz quente, barco de junco sobre ondas em ouro) e o Samir ao lado dele. "Enterrar a tabuleta" (conversa `Fuga_Nicho`: "Deixa aqui. Os Guardiões levam ao Poço.", provisória) tira a tabuleta do inventário (`Inventory/ItemRemover`; o poder e a transformação ficam), mostra a tabuleta no nicho, dispara `EnterrouATabuleta` e `TabuletaResolvida` e marca um ponto de retorno ao lado do nicho. A Elena volta pelo túnel e pelo wadi até a caminhonete.
- **Decisão de história:** o nicho do túnel é um **ponto de passagem dos Guardiões rumo ao Poço**: o que se deixa ali eles levam até o Poço. Isso substitui, para esta fatia, o "enterrar no nicho da Aldeia" da especificação; falta refletir isso no `Docs/Regiao-1-Sitio-de-Ur.md`.
- **Embarque:** com a tabuleta resolvida, a traseira da caminhonete tem o salvamento automático `Autosave_Embarque` e a interação "Entrar na caminhonete". Morrer antes de entrar volta ao ponto de retorno com a escolha, a reputação, a transformação e o inventário intactos.
- **Viagem** (`Sequences/TruckRide`, sem física de veículo): a tela escurece (0,7 s), a caminhonete aparece numa estrada provisória longe do mapa (x = 400 a 440, z = 300 a 1550, sem colisão, com pedras dos lados), a tela clareia e a caminhonete anda por um trilho. A conversa "A estrada" (`Fuga_Estrada_Entregou`, `_Devolveu` ou `_Ficou`, escolhida pelo evento da escolha) começa 2 s depois. **Duração mínima de 60 s**; se a conversa for mais longa, a caminhonete chega ao fim do trilho e espera a última fala, e a tela de fim nunca corta uma fala. O trilho usa o tempo real, então anda também durante a conversa (que pausa o jogo).
- **A Elena na viagem:** presa na caçamba (movimento, interação, combate e dano desligados); a câmera gira livre. Pausar (Esc) congela o trilho e continuar o retoma; "Voltar ao último checkpoint" na pausa recomeça do embarque.
- **Fim:** ao chegar, o diário (`Data/Documents/Diario_Entrada_1`, texto provisório, na tela de leitura dos documentos) dispara `LeuODiario`; ao fechá-lo dispara `FimDaFatia`, que abre a **tela provisória de fim da demo** (`UI/DemoEndScreen`). Confirmar zera a partida e os pontos de retorno e volta ao **menu principal** (`UI/MainMenuUI`, cena `Scenes/MenuPrincipal`, primeira da lista do Build: "Novo jogo" e "Sair do jogo", setas ou direcional e Enter ou A). "Novo jogo" abre a região do começo.
- **Para ajustar jogando:** as falas estão em `Data/Dialogues/Fuga_*`; o diário em `Data/Documents/Diario_Entrada_1`; a duração mínima (60 s), o atraso da conversa (2 s) e o trilho no `TruckRide` da `Conteudo_Fuga/Caminhonete/Caminhonete_Do_Samir`.
- **Provisório e pendências:** o **Samir dirige** (sentado ao volante, num banco e volante provisórios; o fundo da cabine ganhou uma janela traseira de 1,9 a 2,75 m para ele aparecer) e a **Elena viaja sentada no chão da caçamba** (veja "Animações da biblioteca"); falta voz nas falas; a estrada, a caminhonete e a tempestade são formas simples; o diário é só uma tela de texto; a tabuleta de quem **entrega** continua no inventário (só os eventos mudam); o menu principal não tem opções nem save; o segundo líder de esquadra e a passagem do helicóptero sobre a base continuam como pendência da base do zigurate.
- Teste feito pela ponte (teste automático, 3 itens): veja "Como testar o passo 15".

**Secundária "Remédio para Yusuf" (em blockout, testada pela ponte e aprovada pelo dono no Play):**
- Montada pelo menu **Nibiru > Conteúdo > Povoar Remédio para Yusuf na cena aberta** (`Scripts/Editor/Content/YusufRemedyContent.cs`), em `Conteudo_RemedioYusuf`. **Rode depois dos menus das áreas** (Trincheiras, Aldeia, Base, Wadi e Fuga): ele troca a conversa de espera da Weiss e a do Yusuf por trocas de item, liga um evento ao pickup dos antibióticos da tenda médica e acrescenta uma conversa à viagem. Se rodar de novo o menu de uma dessas áreas, rode este depois. A cena aberta tem que ser a região (no laboratório ele só dá avisos).
- Missão (`Data/Quests/RemedioParaYusuf`, começa em `ConheceuSobreviventes`): "Consiga antibióticos para o Yusuf" (cumprido por ter o item na bolsa, mesmo se o pegou antes de a missão começar) e "Leve os antibióticos ao Yusuf" (`EntregouAntibioticos`). Dois caminhos para os antibióticos: **a Dra. Weiss** (curativos nas Trincheiras, dois no mapa) ou **a tenda médica** da base.
- **Troca com a Weiss** (`Inventory/ItemTrade`, na zona `Falar_DraWeiss_Depois`): com 1 curativo na bolsa aparece "Dar os curativos à Dra. Weiss". Ela leva o curativo, **dá os antibióticos só a quem ainda não os tem nem os entregou** (senão dá só o documento), entrega o **documento provisório** `Data/Documents/Weiss_Documento` ("a definir", mostrado na tela de leitura como a foto de 1979) e **pede silêncio** sobre onde se esconde (nos dois casos). A troca acontece uma vez; depois ela só repete o pedido de silêncio. Cada conversa está em `Data/Dialogues/Remedio_Troca_Weiss*`.
- **Entrega ao Yusuf** (`Falar_Yusuf`, também um `ItemTrade`): "Dar os antibióticos ao Yusuf" tira o item, conclui a missão, e a Umm Karim e o Yusuf passam a falar de agradecimento (`Aldeia_UmmKarimGrata`, `Aldeia_YusufMelhor`).
- **Prazo** (`Quests/DeadlineWatcher`, genérico: evento X + flag Y ausente → evento Z): quando `EscolheuONoWadi` acontece sem `EntregouAntibioticos`, dispara `YusufMorreu` (e então `UmmKarimFria`). A missão **falha**: o `QuestManager` ganhou um estado de falha genérico (`QuestDefinition.failOn`, `QuestManager.FailQuest/IsFailed`, guardado no `WorldState` e no checkpoint), a missão sai da lista de objetivos e aparece "Missão falhou". A Umm Karim fica fria (`Aldeia_UmmKarimFria`) e a entrega deixa de ser possível. A missão principal não é afetada.
- **Lembrete:** ao sair da passagem (`SaiuDaPassagem`, começo do passo 14), se ainda há antibióticos não entregues na bolsa, a Elena lembra do Yusuf (`Remedio_Lembrete_Yusuf`, `Inventory/ItemBranch`), uma vez.
- **Pergunta do Samir** (na viagem do passo 15, depois da conversa "A estrada"; `TruckRide.followups`; só com `AjudouAWeiss`, com o Yusuf vivo ou morto): "Cobrir" a Weiss (`CobriuAWeiss`, Fundação +5, Guardiões −5) ou "Contar" (`DenunciouAWeiss`, Guardiões +5, Fundação −5; o destino dela fica em aberto). Conversa `Fuga_Estrada_Weiss`.
- **Flags e eventos gravados** (`Data/Events`, para as missões seguintes lerem): `ConseguiuAntibioticosComAWeiss`, `ConseguiuAntibioticosNaTenda`, `AjudouAWeiss` (disparado na troca dos curativos, com ou sem antibióticos), `WeissEntregouODocumento`, `LeuODocumentoDaWeiss`, `EntregouAntibioticos`, `YusufSobreviveu` (ao concluir a missão), `YusufMorreu`, `UmmKarimFria`, `CobriuAWeiss`, `DenunciouAWeiss`. Na região 2, o aviso da emboscada deve ler `YusufSobreviveu`; **ainda não existe**, só a flag.
- **Equilíbrio:** a missão principal continua possível sem ela. Os antibióticos não são consumíveis de combate; os 2 curativos do mapa já existiam e um deles pode ser gasto na troca com a Weiss (fica registrado em "Pendências para o teste completo da fatia").
- **Provisório:** Yusuf, Umm Karim e Dra. Weiss ainda são cápsulas (os modelos estão prontos na `ArteFonte`); a animação de deitado do Yusuf (*Laying Idle*, no catálogo) ainda não está importada; o documento da Weiss é um texto "a definir"; falas sem voz.
- **Atalhos:** **Nibiru > Testes > Secundárias > Remédio para Yusuf** (1 a 14): do zero na casa do altar; passo 7 com antibióticos; diante da Weiss com curativo; diante da Weiss já com os antibióticos; tenda médica; saída da passagem com e sem antibióticos; passo 14 sem entrega (o prazo vence ao decidir); e a viagem com cada resultado (Yusuf vivo ou morto × cobrir ou contar, escolha manual, e sem ter ajudado a Weiss). Cada um liga o Play e põe a Elena no momento, com o mundo como se tivesse jogado até ali. Na Weiss, a Elena fica a leste dela, ao lado da mochila (a tenda escondida é sólida, e do outro lado não dá para entrar).
- **Teste automático** (**Nibiru > Testes > Secundárias > Remédio para Yusuf > 15**, 8 cenários, tudo OK): caminho da Weiss e entrega (sem nada em dobro, flags e checkpoint); caminho da tenda e curativos para a Weiss (só o documento) seguido da viagem e da escolha de contar com o Yusuf morto; Weiss depois da entrega; prazo (morte, falha, Umm Karim fria, falha guardada no checkpoint, missão principal livre); lembrete com e sem antibióticos; as três versões da viagem. Resumo no Console.

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
| Prazo genérico de missão, falha de missão (`failOn`, guardada no checkpoint) | `Quests/DeadlineWatcher`, `Quests/QuestManager` | `Data/Quests` |
| Troca de itens com um personagem e reação conforme um item na bolsa | `Inventory/ItemTrade`, `Inventory/ItemBranch` | configurados no Inspector |
| Tabuletas, transformação, Eco do Abzu | `Tablets/`, `Powers/` | `Data/Tablets`, `Data/Powers` |
| Lullu e o chefe Primeiro Servo | `AI/LulluAI` | `Data/AI/Lullu`, `PrimeiroServo` |
| Tempestade de areia (névoa, lentidão, vento que cobre passos) | `Environment/Storm`, `StormController` | `Data/Tempestade` |
| Luta do Primeiro Servo: fases pela vida, urro e sinais de cada troca, barra de vida no alto da tela, vitória | `AI/FirstServantFight` | `Data/AI/PrimeiroServo_Luta` (`FirstServantSettings`) |
| Holofote em poste que cai com um tiro e distrai com o estrondo | `Combat/FallingFloodlight` | configurado no Inspector |
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
| Primeiro Servo | `PrimeiroServo` | 2,50 m | **No jogo** (prefab `AI/PrimeiroServo`); 4 ossos de dedo sem peso, corrigidos na importação |
| Ninsar | `Ninsar` | 1,90 m | Prévia na Unity; manto pausado (Ato 2). Veja "Ninsar" abaixo |

As alturas ficam também em `Scripts/Editor/Personagens/CharacterHeights.cs`: `CharacterHeights.FitImportScale(fbx, "Nome")` ajusta o "Scale Factor" da importação até o modelo medir a altura da tabela (use na montagem de cada personagem).

A Elena e o Samir antigos (`Elena_Varga`, `Samir_Haddad`, 1,5 a 1,8 milhão de triângulos, sem esqueleto) e a Elena provisória do Meshy saíram do projeto (os arquivos do `Samir_Haddad` que ainda estavam em `Art/Personagens/Samir` foram apagados com a entrada do Samir novo).

**Dedos sem peso:** `Scripts/Editor/Personagens/FingerWeightFix.cs` corrige na importação, em todo modelo de `Art/Personagens`, as falanges do AccuRig que vieram sem peso: cada uma recebe a pele do trecho dela, tirada das falanges vizinhas (e da mão, na primeira), com transição suave nas juntas. O FBX original não muda; o Console mostra quais ossos foram corrigidos.

**Kit comum para os próximos personagens:** `Scripts/Editor/Personagens/CharacterSetupKit.cs` (texturas e material URP de dois lados, importação Humanoid na altura da tabela, cortes do Mixamo com o avatar do X Bot e árvore de velocidades). Os cortes são acrescentados ao arquivo do Mixamo sem apagar os de outros personagens. O `SamirSetup` é o exemplo de uso. A Elena ainda usa o `ElenaSetup` próprio, com as mesmas regras. O Tom não entra agora.

### Animações do Mixamo

A biblioteca inteira (pacotes do Mixamo e Universal Animation Library) fica em `ArteFonte/Animacoes`, **fora do Git** (`.gitignore`) e fora de `Assets`; o catálogo de tudo que há nela, com as marcas das pendências (garra, urro, saída das rachaduras, locomoção, sentado, dirigir), está em `Docs/CATALOGO_ANIMACOES.md`. Consulte-o antes de pedir uma animação. Em `ArteFonte/Animacoes/Mixamo` (baixadas "sem pele"; o `X Bot.fbx` é o boneco em pose T). As usadas são copiadas para `Assets/_Nibiru/Art/Animacoes/Mixamo`.

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

### Animações da biblioteca

Clipes importados da `ArteFonte/Animacoes` (fora do Git) para `Art/Animacoes/<Criatura|Elena|Caminhonete>/`, **só os usados**, com nome `Personagem_Movimento`. A tabela (de qual arquivo vem cada um, loop, cortes) e o `AssetPostprocessor` que os importa sempre igual (Humanoid, avatar do X Bot, Loop Time, raiz travada) ficam em `Scripts/Editor/Animacoes/AnimationLibrary.cs`; o mapeamento arquivo original → clipe está no `Docs/CATALOGO_ANIMACOES.md`. Menus **Nibiru > Animações**: "Importar clipes da biblioteca" e "Reimportar clipes (depois de mudar a tabela)".

- **Lullu** (pacote do zumbi): parado, andar, correr, ataque, e dano e morte compartilhados com o Servo. **Primeiro Servo** (pacote do mutante): parado, andar, correr, ataque, investida e urro. Os controladores (`Lullu.controller`, `PrimeiroServo.controller`) são montados pelos mesmos menus de antes (**Montar Lullu animado** e **Montar Primeiro Servo animado**), com blend tree de locomoção pela velocidade real; o golpe toca mais rápido que o clipe (2x o do zumbi, 1,7x o do mutante) para o gesto cair na preparação de 0,5 s da IA.
- **Ligações a coisas que já existiam (sem lógica nova):** o golpe entra pelo estado `Golpe` da `LulluAI`; a investida do Servo, pelo estado `Investida` (o Lullu, que não tem esse estado no controlador, usa o golpe); o urro, pelo `FirstServantFight.IsRoaring` (ao acordar e a cada troca de fase); dano e morte, pelos eventos do `Health`. O `LulluAnimator` só ganhou os dois gatilhos novos (`Investida` e `Urro`), que ignora se o controlador não os tiver.
- **Samir dirigindo:** estado `Dirigindo` no `Samir.controller` (parâmetro `Dirigindo`; o `NpcAnimator` tem o campo "driving" e `SetDriving`). O menu da Fuga põe o Samir sentado na cabine com banco e volante provisórios. **Rode de novo "Montar Samir animado" e "Povoar Fuga na cena aberta" se mexer nisso.**
- **Importados e sem ligação** (nenhum evento do jogo serve): saída das rachaduras (`Lullu_Saida_Rachaduras`), pegar e abrir da Elena (o `PlayerInteractor` não tem evento de interação) e as viradas. Estão na vitrine.
- **Elena:** o controlador aprovado **não mudou**. A parada, o andar e a corrida da Female Basic Locomotion estão só na vitrine, ao lado da atual, para comparar antes de trocar (a nova corrida é mais lenta: 3,6 m/s contra 5,4 do pique atual).
- **Elena sentada:** `Elena_Sentada` (`X Bot@Sitting Idle2`, sentada no chão; o `Sitting Idle` é de cadeira e não foi usado). O `Elena.controller` ganhou o parâmetro e o estado `Sentada` (`PlayerAnimator.SetSeated`), que o `TruckRide` liga ao travar a Elena. No mesmo ajuste, o `TruckRide` passou a pôr a **raiz da Elena 1,05 m acima do ponto do banco** (antes os pés ficavam 1 m enterrados no chão da caçamba).
- **Agachada parada:** `Elena_Agachada_Parada` (`X Bot@Crouching Idle`) substitui o quadro congelado do `Sneak Walk` na blend tree da agachada. É a única mudança no controlador aprovado, além da sentada; peça a vitrine 2 para conferir. As pastas Pro Magic, Magic Locomotion e Scary Zombie seguem sem origem confirmada e sem uso.

**Como testar:** menu **Nibiru > Testes > Animações** (`Scripts/Editor/Animacoes/AnimationShortcuts.cs`):

| Atalho | O que mostra |
| --- | --- |
| 1. Criaturas: Lullu e Primeiro Servo | Laboratório, num chão vazio: cada um repete parado, vagando, investigando, caçando, golpe, dano e morte (o Servo também a investida e o urro), com o controlador de verdade; mais a saída das rachaduras solta |
| 2. Elena: controlador atual ao lado dos clipes novos | À esquerda a Elena atual (parada, andar, correr em ciclo); depois parada, andar e correr novos, viradas, pegar e abrir |
| 3. Caminhonete: meio da viagem | O atalho 5 do passo 15: a Elena na caçamba e o Samir sentado ao volante, visto pela janela traseira |
| 4. Teste automático | 253 verificações no Console: clipes (existem, Humanoid, avatar do X Bot, Loop Time, tamanho do corte, raiz travada) e controladores (estados e parâmetros da criatura, do Servo, do Samir e da Elena) |

Teste pela ponte: o teste automático passou inteiro; as vitrines foram vistas em captura de tela; o Servo acordando na luta (passo 12) tocou o urro e depois o golpe, ligado à IA.

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
- `AI/LulluAnimator` (novo, a IA não mudou): lê o estado da `LulluAI` e a vida; passa a velocidade (pelo deslocamento), o golpe (entrando em Golpe ou Investida), o dano e a morte ao Animator. **Postura de criatura:** o modelo é em pé, então o tronco, o pescoço e a cabeça são dobrados para a frente por cima da animação (22°, 22°, -18°, -12°; mais fraco andando e correndo, `movingWeight`). **Pés no chão:** a estocada baixava o corpo e a sola entrava no chão; o `LulluAnimator` mede o tornozelo mais baixo a cada quadro e ergue o modelo o que faltar até a altura de repouso (no máximo 25 cm), sem mexer na animação. O golpe tem corte próprio (`Lullu_Golpe`, altura pelos pés). Morto, ele segura o `Corpo` de pé onde estava, porque a `LulluAI` tomba a raiz e a animação de morte já o deita.
- **Colisor:** cápsula de 0,4 m de raio e 1,8 m de altura, do chão para cima (antes 0,35 x 2 m). **Audição:** a `LulluAI` mede distância e altura a partir da raiz, a 1 m do chão (cintura dele); a cabeça curvada fica a uns 1,7 m, então o "ouvido" fica 0,7 m abaixo da cabeça, sem efeito prático nos alcances (6, 14, 1,8 m). O alcance vertical da Elena nos andaimes (2,4 m acima da raiz) não mudou.
- O andar agachado (`Sneak Walk`) não serviu: nas pernas longas dele o corpo vira uma rampa quase deitada. Por isso o andar é o normal, encurvado pelo `LulluAnimator`.
- Pendências: o golpe, o dano e a morte agora vêm do pacote do zumbi (veja "Animações da biblioteca"); o atordoado do Eco ainda é só o tremor do corpo da `LulluAI`; o andar do zumbi é cambaleante, falta ver se serve ou se vale um andar de quatro apoios.
- **Como testar:** os atalhos do passo 11 (**Nibiru > Testes > Passo 11**, 4 e 6) levam ao Lullu; corra ou atire perto dele para vê-lo caçar e golpear (ou fique agachada para ele não ouvir).

### Primeiro Servo

O menu **Nibiru > Personagens > Montar Primeiro Servo animado** (`Scripts/Editor/Personagens/PrimeiroServoSetup.cs`) refaz tudo (saia do Play antes; rode antes os menus da Elena e do Lullu):
- Copia da `ArteFonte/PrimeiroServo` o FBX do AccuRig e as texturas do Meshy para `Art/Personagens/PrimeiroServo`. Humanoid com dedos, escala de importação 2,724 (2,50 m em pé); 4 ossos de dedo sem peso são corrigidos pelo `FingerWeightFix`. Na importação aparece um aviso amarelo "Rig Error / Avatar Rig Configuration mis-match": é do passo intermediário em que a escala ainda está sendo ajustada; o avatar final fica válido.
- Controlador `PrimeiroServo.controller`: os mesmos estados e cortes do Lullu (montados pelo `LulluSetup`, agora reaproveitável), nas velocidades de `Data/AI/PrimeiroServo`.
- Prefab `Prefabs/AI/PrimeiroServo`: na primeira vez, uma cópia do prefab do Lullu; depois, editado no lugar. Mesmo arranjo (`LulluAI`, `LulluAnimator`, `Corpo` > `Modelo`), com colisor de 0,55 x 2,2 m, os dados `PrimeiroServo` e a `FirstServantFight` (dados `PrimeiroServo_Luta`, sons do urro e da máscara, lascas de ouro). A postura encurvada é a mesma do Lullu.
- No laboratório, o Primeiro Servo (antes o Lullu em escala 1,25) passa a ser este prefab, acordado desde o começo (campo "Awake On Start").
- Animações novas (pacote do mutante): parado, andar, correr, golpe, investida e urro. Pendências: saída das rachaduras (clipe importado, sem evento que o use); a máscara não muda no modelo (as lascas são soltas).

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
- Tudo enviado ao GitHub até os soldados da Fundação, testados pelo dono (25 de outubro de 2026), inclusive a arte-fonte nova dos personagens (`Referencias/` e `2_Rig/`) e as primeiras animações do Mixamo (a biblioteca de animações agora está fora do Git; a cópia no LFS fica só no histórico). Os 4 soldados somaram cerca de 250 MB ao LFS (modelos e texturas em `Art/Personagens`). Commits em português, pequenos e frequentes.
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

## Pendências para o teste completo da fatia

- **Remédio para Yusuf:** a recompensa (antibióticos, gastar um dos 2 curativos do mapa na Weiss) não mexe no combate, mas conferir no teste completo se os curativos fazem falta para quem os troca.
- **Equilíbrio da luta do Primeiro Servo:** com pouca munição a luta fica impossível e na faca é difícil demais. Decidir entre munição na arena, faca mais forte contra o Servo atordoado ou munição mínima garantida antes da antecâmara.

## Próximos passos

1. Teste do dono nas vitrines de animação (veja "Animações da biblioteca"); os passos 1 a 15 já foram aprovados pelo dono. Depois, o segundo líder e a passagem do helicóptero na base, a revelação dos filmes do Tom.
2. Seguir área por área: Zigurate, Poço e Wadi.
3. Pendências das Trincheiras: documentos da Fundação (falta uma tela de leitura de documentos) e o rádio de campanha avisando onde estão as patrulhas.
4. Pendência da Aldeia: ao voltar de um checkpoint depois do encontro, o soldado caído reaparece sempre na entrada leste, e não na entrada onde o encontro aconteceu (guardar a entrada escolhida no estado do mundo).
5. Personagens 3D, um por conversa: Yusuf, Umm Karim, Dra. Weiss, equipe morta (arqueólogos e trabalhador, que também serve de Hassan) e Primeiro Servo, cada um com a altura da tabela e trocando a cápsula onde ele aparece (use o `CharacterSetupKit` e o `NpcAnimator`, como no Samir). Soldados: fuzil de verdade e as pendências em "Soldados da Fundação". Samir: pose de mão na faca e as outras pendências em "Samir". Elena: corrida mais atlética (conferir a Run Forward), agachada parada e tiro/recarga de pistola (lista em "Elena"). Ninsar: decidir o caminho do manto (veja as alternativas em Personagens 3D) e terminar saia, física e poses de mão.
6. Versões simples do Grupo 2 (cenas, diário, crafting, save em disco). A memória jogável já tem a versão simples.
7. Memória de Shuruppak: hoje em formas simples; trocar por arte de verdade, com som de cidade, rio e a voz, quando a fatia ganhar arte.

### Como testar o passo 15

Atalhos no menu **Nibiru > Testes > Passo 15** (`Scripts/Editor/Step15Shortcuts.cs`), com o mundo como se a Elena tivesse feito a escolha do wadi, e com pistola, 24 balas, faca e a tabuleta:
1. **Entregou a tabuleta**, junto da caminhonete (vá à traseira e use "Entrar na caminhonete").
2. **Devolve ao Poço**, no wadi: volte pela rampa e pelo túnel até o nicho, enterre a tabuleta e volte à caminhonete.
3. **Ficou com a tabuleta**, junto da caminhonete.
4. **Devolve ao Poço, diante do nicho** do túnel (o Samir espera ao lado).
5, 6 e 7. **Meio da viagem** (entregou, devolve, ficou): a caminhonete já na estrada, a conversa da escolha, a pausa (Esc) e o fim.
8. **Fim da viagem**: o diário e a tela de fim da demo.
9. **Teste automático** (resumo no Console, uns 3 minutos): para os três ramos confere objetivo, Samir, zona de embarque e conversa certa; morrer antes de embarcar e voltar mantém escolha, reputação, transformação e tabuleta; Elena presa na caminhonete; viagem de pelo menos 60 s (mediu 62,6 s); pausa congela o trilho e continuar o retoma; conversa mais longa que o trilho: o trilho espera e `FimDaViagem` só vem depois da última fala; diário, `FimDaFatia`, tela de fim aberta e, no ramo Devolve, volta ao menu principal (jogo zerado) e "Novo jogo" abre a região do começo; no Devolve também o nicho: enterrar tira a tabuleta, marca o retorno e a volta do ponto de retorno mantém tudo. Passou nos três ramos; os testes dos passos 13 e 14 continuam OK.

### Como testar o passo 14

Atalhos no menu **Nibiru > Testes > Passo 14** (`Scripts/Editor/Step14Shortcuts.cs`), com o mundo como se a Elena tivesse vencido o Primeiro Servo e atravessado a passagem, e com pistola, 24 balas e faca:

| Atalho | Onde começa |
| --- | --- |
| 1. Início do wadi, no topo da rampa | Ande para o norte: a tempestade sobe, a Elena fica mais lenta, o líder patrulha o trecho da curva e o chiado do rádio se ouve de longe |
| 2. Gatilho do rádio, com a fita de Irena tocando | Antes da entrada do trecho final, com a fita tocando: ao entrar, o rádio espera e começa quando a fita termina (mesmo que você já tenha se afastado) |
| 3. Gatilho do rádio, sem a fita (nunca pega) | O mesmo, mas a fita nunca foi pega: o rádio começa na hora |
| 4. Direto à escolha de facção | Junto do soldado; as falas avançam sozinhas até as três opções, que ficam na tela para você escolher |
| 5. Forçar: entregar a tabuleta à Fundação | O rádio fala sozinho e a primeira opção é escolhida |
| 6. Forçar: devolver ao Poço com o Samir | O mesmo, com a segunda opção |
| 7. Forçar: ficar com a tabuleta | O mesmo, com a terceira opção (a transformação sobe 3 sem aviso) |
| 8. Teste automático (resumo no Console) | Roda os testes sozinho e escreve OK ou FALHOU para cada um no Console (`Scripts/Editor/Step14Tests.cs`) |

O teste automático (10 itens) cobre: o percurso a pé e a tempestade; o líder de esquadra e a margem sul fora da vista dele; o vento que cobre passos mas não tiros; o rádio com a fita tocando, já terminada, nunca pega e com uma fita que não acaba (o teto); e cada uma das três escolhas, que são salvas, recarregadas do salvamento automático e disparam o evento. Para ver a espera da fita no jogo de verdade (uns 50 s), use o atalho 2 e fique perto do soldado. Ajuste a tempestade em `Data/Tempestade/TempestadeDoWadi`.

### Como testar o passo 13

Atalhos no menu **Nibiru > Testes > Passo 13** (`Scripts/Editor/Step13Shortcuts.cs`), com o mundo como se a Elena tivesse vencido o Primeiro Servo, e com pistola, 24 balas e faca:

| Atalho | Onde começa |
| --- | --- |
| 1. Diante da parede, pronta para ler a inscrição | De frente para a parede do barco de junco: "Ler a inscrição", a tabuleta pulsa, a parede afunda |
| 2. Passagem aberta, na entrada do túnel | Como no ponto de retorno depois da abertura: entre no túnel, a oeste |
| 3. Junto da fita de Irena | No túnel, de frente para a fita: "Pegar a fita" e siga andando enquanto ela toca |
| 4. Na rampa, com a fita já pega (toca de novo) | Meio da rampa: a fita recomeça sozinha. Suba até o topo |
| 5. Pé do topo da rampa: salvamento e saída para o wadi | Ande para o norte: "Ponto de retorno marcado" e fim do passo |
| 6. Voltar do túnel à antecâmara | No começo do túnel, de frente para a antecâmara: confira que nada trava |
| 7. Teste automático (resumo no Console) | Roda os testes sozinho e escreve OK ou FALHOU no Console (`Scripts/Editor/Step13Tests.cs`) |

Dica: para ouvir a fita inteira (uns 50 s), ande devagar. Com a **Elena invencível** ligada (o padrão) nada atrapalha.

### Como testar o passo 12

Atalhos no menu **Nibiru > Testes > Passo 12** (`Scripts/Editor/Step12Shortcuts.cs`), com o mundo como se a Elena tivesse descido o Poço, passado pelo Lullu e visto o flash, e com pistola, 24 balas e faca:

| Atalho | Onde começa |
| --- | --- |
| 1. Do flash ao Servo acordando (o Lullu foge) | Um passo antes do flash, com o Lullu vivo. Ande para o norte: a Voz, o Servo saindo do Selo e urrando, o Lullu fugindo |
| 2. Ponto de retorno da luta (fase 1) | Na boca da antecâmara, com o Servo vagando junto ao Selo. Faça barulho para ele vir |
| 3. Troca para a fase 2 | O Servo passa para a fase 2 logo ao carregar: olhe para ele (urro, lascas da máscara, holofotes piscando) |
| 4. Troca para a fase 3 | O mesmo, da fase 2 para a 3 |
| 5. Fim da luta: o Servo quase sem vida | Fase 3 com 6% da vida: use o Eco e ataque |
| 6. Depois da vitória: rumo à parede do barco | O Servo caído; ande até a parede do barco, a oeste |
| 7. Teste automático (resumo no Console) | Roda os testes sozinho e escreve OK ou FALHOU no Console (`Scripts/Editor/Step12Tests.cs`) |
| 8. Ferir o Servo (10% da vida) | Com o Play ligado e o Servo acordado: tira 10% da vida dele na hora (para ver as trocas de fase) |

Para jogar a luta inteira, use o atalho 2 e desligue **Nibiru > Testes > Elena invencível nos atalhos** (senão ela não morre). Dicas: a investida se atravessa com a esquiva (role na hora em que ele se encolhe); o Eco (Q ou LB) o atordoa por 3 s, a hora de atirar ou golpear; um tiro num holofote o manda para longe.

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
