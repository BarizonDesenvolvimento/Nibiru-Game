# Estado do projeto

Retrato de onde o projeto está, para retomar o trabalho numa conversa nova sem precisar reler o histórico. Atualize este arquivo ao fim de cada etapa grande.

Última atualização: 21 de outubro de 2026.

## Onde estamos

Fase 3 do plano (construir a base), indo para a fatia vertical da região 1, o Sítio de Ur.

- O protótipo do personagem está pronto.
- Os **14 sistemas essenciais** da fatia (Grupo 1 de `Docs/Regiao-1-Sitio-de-Ur.md`) estão prontos e testados.
- O **blockout** do mapa do Sítio de Ur está construído.
- Os modelos 3D da **Elena** e do **Samir** chegaram ao projeto, ainda sem esqueleto nem animação.
- O **Acampamento** (área 1) está povoado e testado pelo dono: passos 1 a 4 da missão "Sobrevivente" e o começo de "O que o Tom viu".
- As **Trincheiras** (área 2) estão povoadas e testadas pelo dono: passo 5 da missão (ordem pelo rádio, primeira furtividade, pistola), helicóptero passando baixo, Dra. Weiss (só a primeira conversa), 2º rolo de filme e rádio de campanha.
- A **Aldeia** (área 3) começou: o encontro com o Samir (passo 6) está pronto e testado pelo dono.
- A **Ninsar** tem uma prévia animada na Unity (corpo, saia e lança, sem manto), na cena `Teste_Ninsar`. O manto dela está pausado. Veja "Ninsar" em Personagens 3D.
- A **Elena animada** substituiu a cápsula no prefab `Prefabs/Player/Elena` (parada, andar, correr, agachar, esquivar e mirar), testada pela ponte em `Teste_Personagem` e `Regiao1_SitioDeUr`. Falta o dono testar. Veja "Elena nova" em Personagens 3D.

A próxima prioridade é o dono testar a Elena animada e, se aprovada, continuar a Aldeia: sobreviventes, altar e a primeira tabuleta (passos 7 a 9). Animações que faltam da Elena estão em "Elena nova".

## Documentos

| Arquivo | O que tem |
| --- | --- |
| `Docs/GDD.md` | Design do jogo inteiro. Vale acima de tudo |
| `Docs/Biblia.md` | História canônica: eras, panteão, personagens, mistérios |
| `Docs/Regiao-1-Sitio-de-Ur.md` | Região 1 aprovada: mapa, missões, inimigos, itens, diálogos, sistemas (Grupos 1 e 2), controles, blockout e perguntas abertas |
| `Docs/Guia-MCP-Unity.md` | Como a Unity conversa com o Claude Code |
| `Docs/ESTADO.md` | Este arquivo |

## Cenas

| Cena | Para que serve |
| --- | --- |
| `Scenes/Teste_Personagem` | Laboratório de sistemas: tem conteúdo de teste de tudo (missão Treinamento, Samir de cápsula, soldados, Lullu, chefe, bonecos, brasas, altar). Os arquivos de teste têm prefixo `Teste_` |
| `Scenes/Regiao1_SitioDeUr` | A região de verdade: blockout, Elena e o conteúdo do Acampamento |
| `Scenes/Teste_Ninsar` | Prévia da Ninsar animada: troca de animação por teclas (setas, 1 a 0, espaço recomeça) ou pelo menu na tela; câmera gira com o botão direito do mouse |

O blockout é gerado pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (`Scripts/Editor/Blockout/SitioDeUrBlockout.cs`). Para mudar o mapa, mude o script e rode o menu de novo; o menu também refaz a malha de navegação. Os discos dourados `Marca_...` marcam onde entra cada conteúdo.

## Conteúdo das áreas

Cada área tem um script de editor que monta o conteúdo num grupo próprio da cena (`Conteudo_...`), apagando e reconstruindo a cada execução, como o blockout. Os arquivos de dados (missões, diálogos, eventos, itens) só são criados se ainda não existirem: textos mudados no Inspector não se perdem.

| Área | Menu | Script |
| --- | --- | --- |
| 1. Acampamento | **Nibiru > Conteúdo > Povoar Acampamento na cena aberta** | `Scripts/Editor/Content/CampContent.cs` |
| 2. Trincheiras | **Nibiru > Conteúdo > Povoar Trincheiras na cena aberta** | `Scripts/Editor/Content/TrenchContent.cs` |
| 3. Aldeia | **Nibiru > Conteúdo > Povoar Aldeia na cena aberta** | `Scripts/Editor/Content/VillageContent.cs` |

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

**Aldeia, como funciona (até agora, passo 6):**
- Uma área grande cobre a Aldeia com folga (evento `AproximouSeDaAldeia`): de onde a Elena vier, o encontro começa ao entrar nela.
- Há 4 pontos fixos de encontro nas entradas (leste, sudeste, sul e norte). O componente `Sequences/PlaceAtPointAhead` escolhe o mais perto entre os que estão à frente da Elena e leva a cena (soldado, corpo e Samir) para lá, virada para ela. A cada execução, o menu confere se cada ponto está na malha de navegação e com 2,5 m livres em volta (avisos no Console).
- Um soldado (figurante, sem IA) a encurrala (`Aldeia_Encurralada`). Depois da fala dele, o Samir aparece atrás e o derruba com a faca. Em seguida vem a conversa "O guia" (`Aldeia_OGuia`): o Samir nota a luz da tabuleta na bolsa ("Esconda isso"), diz que ninguém veio resgatar ninguém e desconversa se a Elena perguntar quem ele é. No fim, dispara `ChegouAAldeia`.
- Objetivo "Siga o Samir até a Aldeia": ele anda pela malha de navegação até o pátio, na frente da casa do altar (`Sequences/WalkToPoint`). Ele espera quando a Elena fica mais de 10 m para trás, mas não quando ela está à frente.
- No pátio: fala curta do Samir (`Aldeia_ChegadaPatio`), evento `ChegouAoPatioComSamir` e ponto de retorno. Ao voltar dele, o Samir já está no pátio e o soldado, no chão.
- Se a Elena morrer antes do pátio, o encontro se repete.

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
| IA dos soldados | `AI/SoldierAI`, `SoldierPerception` | `Data/AI/SoldadoContencao`, `LiderEsquadrao` |
| Missões e eventos | `Quests/` | `Data/Quests`, `Data/Events` |
| Diálogos | `Dialogue/` | `Data/Dialogues` |
| Facções e reputação | `Factions/` | `Data/Factions` |
| Tabuletas, transformação, Eco do Abzu | `Tablets/`, `Powers/` | `Data/Tablets`, `Data/Powers` |
| Lullu e o chefe Primeiro Servo | `AI/LulluAI` | `Data/AI/Lullu`, `PrimeiroServo` |
| HUD, telas e pausa | `UI/` | opções do jogador em `GameOptions` (salvas no computador) |
| Helicóptero que avista a Elena, item largado ao morrer | `AI/HelicopterSpotter`, `Combat/DropOnDeath` | configurados no Inspector |
| Cenas roteirizadas: sequência com tempo, mover por pontos, tiro de cena, escolha por evento, área de aviso, hélice, ponto à frente da Elena, personagem que anda até um ponto | `Sequences/` (`TimedSequence`, `MoveAlongPath`, `ScriptedShot`, `StoryEventBranch`, `PlayerZone`, `Spinner`, `PlaceAtPointAhead`, `WalkToPoint`) e `UI/NotificationPoster` | configurados no Inspector |

Prefabs: `Prefabs/Player/Elena` (Elena + câmera + HUD + diálogos; use este em toda cena), `Prefabs/AI/Soldado`, `Prefabs/AI/Lullu`.

## Personagens 3D

Feitos no Meshy, exportados do Blender para FBX:

| Personagem | FBX | Fonte original |
| --- | --- | --- |
| Elena Varga (nova, para usar) | `Art/Personagens/Elena/Elena_Personagem.fbx` (15 mil triângulos, esqueleto), `Elena_Animacoes.fbx` (16 animações), `Elena_Andando.fbx` e texturas `Elena_BaseColor/Metallic/Normal/Roughness.png` | Meshy (sem .blend) |
| Elena Varga (antiga, pesada) | `Art/Personagens/Elena/Elena_Varga.fbx` + texturas | `ArteFonte/Elena/Elena_Varga.blend` |
| Samir Haddad | `Art/Personagens/Samir/Samir_Haddad.fbx` + texturas | `ArteFonte/Samir/Samir_Haddad.blend` |

**Elena nova:** os três FBX estão como Humanoid, cada um com o próprio avatar, porque os ossos têm nomes diferentes (`smartrig` no personagem, `mixamorig` nas animações); a Unity traduz as animações de um para o outro. Não use "Copy From Other Avatar" entre eles: dá erro de ossos não encontrados.

Ela já está no jogo, no lugar da cápsula. O menu **Nibiru > Personagens > Montar Elena animada** (`Scripts/Editor/Personagens/ElenaSetup.cs`) refaz tudo (saia do Play antes):
- Material URP Lit `Art/Personagens/Elena/Materiais/Elena.mat`. Metal e lisura vão juntos em `Elena_MetalSmooth.png` (R = metal, A = 1 - rugosidade), gerado só se ainda não existir; para refazer, apague o arquivo e rode o menu.
- Importação das animações: avanço do clipe sai para a raiz e é descartado (quem anda é o `PlayerMotor`), altura e giro ficam na pose, "Based Upon: Body Orientation". Os clipes originais do Meshy continuam no FBX, e o script acrescenta os trechos usados (nomes em português).
- Controlador `Elena.controller`. Camada base: estado "Locomocao" (mistura em pé e agachada pelo parâmetro `Agachar`, e cada uma pela `Velocidade` em m/s, com os limites lidos de `Data/Player/MovimentoJogador`), "Esquiva" (gatilho `Esquivar`, no tempo exato da esquiva). Camada "Mira" com máscara do tronco (`Elena_Tronco.mask`), acesa ao mirar.
- No prefab, o filho "Modelo" (o FBX com o Animator) fica sob o "Jogador", com os pés no fundo do colisor. O colisor, a câmera e os sistemas não mudaram.
- `Player/PlayerAnimator` passa o estado do `PlayerMotor` e da mira para o Animator. Andando de costas (mirando), o andar toca ao contrário (parâmetro `Sentido`). Ao mirar, endireita o tronco (agachada, a pose dobra o corpo para a frente) e faz a arma provisória seguir a mão direita.
- `PlayerCrouchVisual` não achata mais o corpo (a animação agacha); só desce câmera e faca. `PlayerDeathHandler` deita a Elena de bruços no chão (falta animação de morte).

Trechos usados (constantes no topo do `ElenaSetup.cs`; para trocar uma animação, mude arquivo, take e quadros ali e rode o menu):

| Trecho | Vem de | Observação |
| --- | --- | --- |
| Parada | `Idle_Turn_Right`, quadro 0 | Pose parada, sem respirar |
| Andar | `Elena_Andando.fbx` | Natural a 1,56 m/s; a 3 m/s toca 1,4 vez mais rápido |
| Correr | `run_fast_8` | Natural a 2,77 m/s; a 6 m/s toca 1,5 vez mais rápido (os pés escorregam um pouco) |
| Agachada_Parada | `Sneaky_Walk_inplace`, quadro 48 | Altura pelos pés |
| Agachada_Andando | `Sneaky_Walk_inplace` | Altura pelos pés |
| Esquiva | `sliding_rool`, quadros 0 a 10 | Só o começo do mergulho; volta a andar em 0,2 s |
| Mira | `Rifle_Aim_Turn_Right`, quadro 12 | Pose de fuzil (no quadro 0 os braços apontam 48° à esquerda) |

Animações que faltam (o pacote do Meshy não tem; baixar no Meshy ou no Mixamo e pôr no lugar do trecho provisório): parada respirando, agachada parada, mira de pistola (parada e andando para os lados), esquiva rolando de verdade, golpe de faca, levar dano, morte, recarregar e interagir (pegar item). As animações do Meshy não têm dedos. A arma e a faca continuam sendo os blocos provisórios.

Pendências dos modelos antigos (Elena_Varga e Samir) antes de entrarem no jogo:
- São pesados demais para jogo: Elena tem 1,8 milhão de triângulos e Samir 1,5 milhão. O alvo é perto de 50 mil. O objeto `Elena_TESTE_LOW` do .blend tem o mesmo tamanho do original, ou seja, ainda não foi reduzido.
- Não têm esqueleto nem animações. O Samir está em pose A, boa para o Mixamo; a Elena está em pose natural, o que dificulta o rig automático.
- A textura `_ORM` junta oclusão, rugosidade e metal num arquivo só e precisa ser convertida para o formato do material Lit do URP.
- A arte-fonte de todos os personagens fica em `ArteFonte/<Personagem>/`, fora de `Assets`, para a Unity não depender do Blender instalado. Cópias de segurança do Blender (`.blend1`) não vão para o Git.

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
- **Git LFS** ativo para modelos, imagens, áudio, vídeo e tipografia, e para todo binário em `ArteFonte/` (veja `.gitattributes`). O LFS guarda cerca de 800 MB (450 MB até a Aldeia, 260 MB da arte-fonte da Ninsar e 90 MB da prévia dela na Unity e das texturas do manto). Fora do LFS, nenhum arquivo passa de 1 MB. Cada nova versão de um arquivo grande ocupa espaço de novo na cota de LFS da conta.
- Tudo enviado ao GitHub até a prévia da Ninsar (21 de outubro de 2026). Commits em português, pequenos e frequentes.

## Como testar sozinho pela ponte MCP

- A ponte é o `unity-editor-mcp` (porta 7800). Se cair, a Unity provavelmente está reabrindo o projeto; espere ela terminar.
- Para testar comandos, crie um controle virtual (`InputSystem.AddDevice<Gamepad>`) e aplique o estado a cada quadro em `InputSystem.onBeforeUpdate`. O script `Scripts/Editor/BackgroundInputInEditor.cs` (menu **Nibiru > Testes**) faz o jogo aceitar comandos com a Unity em segundo plano, só no editor.
- Nunca recompile com o Play ligado: isso recarrega os scripts no meio da partida e quebra estados. Saia do Play antes.
- O dono do projeto às vezes joga ao mesmo tempo que os testes (inclusive com um controle de verdade): posição, vida e munição podem mudar entre um comando e outro.
- No prefab Elena, a raiz ("Elena") é só o grupo; o personagem é o filho "Jogador", com a etiqueta Player. Para reconhecer a Elena numa área, procure o `PlayerMotor` no objeto que entrou, não a etiqueta da raiz.
- Diálogos pausam o tempo do jogo (as sequências também param). Para avançar pela ponte, chame `DialogueRunner.Continue()` ou `Choose(i)`, um comando por vez.

## Perguntas ainda abertas

- O Kast aparece em pessoa ou só pelo rádio na região 1?
- Qual o peso da consequência na Aldeia ao devolver a tabuleta ao Poço?
- Nome definitivo do jogo, dublagem da Elena e idiomas de lançamento (do GDD).

## Próximos passos

1. Fatia vertical: o dono testar a Elena animada no Play. Depois, trocar os trechos provisórios pelas animações que faltam (lista em "Elena nova").
2. Continuar a Aldeia: sobreviventes (Yusuf e Umm Karim), altar e a primeira tabuleta (passos 7 a 9), "Remédio para Yusuf" com a entrega de curativos à Dra. Weiss, patrulhas na estrada e a revelação dos filmes do Tom.
3. Seguir área por área: Zigurate, Poço e Wadi.
4. Pendências das Trincheiras: documentos da Fundação (falta uma tela de leitura de documentos) e o rádio de campanha avisando onde estão as patrulhas.
5. Pendência da Aldeia: ao voltar de um checkpoint depois do encontro, o corpo do soldado reaparece sempre na entrada leste, e não na entrada onde o encontro aconteceu. Corrigir quando entrarem os modelos (guardar a entrada escolhida no estado do mundo).
6. Personagens 3D: o Samir ainda precisa reduzir polígonos e ganhar esqueleto. Ninsar: decidir o caminho do manto (veja as alternativas em Personagens 3D) e terminar saia, física e poses de mão.
7. Versões simples do Grupo 2 (memória jogável, cenas, diário, crafting, save em disco).
