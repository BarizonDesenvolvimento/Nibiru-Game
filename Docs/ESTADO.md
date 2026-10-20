# Estado do projeto

Retrato de onde o projeto está, para retomar o trabalho numa conversa nova sem precisar reler o histórico. Atualize este arquivo ao fim de cada etapa grande.

Última atualização: 20 de outubro de 2026.

## Onde estamos

Fase 3 do plano (construir a base), indo para a fatia vertical da região 1, o Sítio de Ur.

- O protótipo do personagem está pronto.
- Os **14 sistemas essenciais** da fatia (Grupo 1 de `Docs/Regiao-1-Sitio-de-Ur.md`) estão prontos e testados.
- O **blockout** do mapa do Sítio de Ur está construído.
- Os modelos 3D da **Elena** e do **Samir** chegaram ao projeto, ainda sem esqueleto nem animação.
- O **Acampamento** (área 1) está povoado e testado pelo dono: passos 1 a 4 da missão "Sobrevivente" e o começo de "O que o Tom viu".
- As **Trincheiras** (área 2) estão povoadas e testadas pelo dono: passo 5 da missão (ordem pelo rádio, primeira furtividade, pistola), helicóptero passando baixo, Dra. Weiss (só a primeira conversa), 2º rolo de filme e rádio de campanha.

O próximo passo é **povoar a Aldeia** (área 3), começando pelo encontro com o Samir na saída das Trincheiras (passo 6).

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

O blockout é gerado pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (`Scripts/Editor/Blockout/SitioDeUrBlockout.cs`). Para mudar o mapa, mude o script e rode o menu de novo; o menu também refaz a malha de navegação. Os discos dourados `Marca_...` marcam onde entra cada conteúdo.

## Conteúdo das áreas

Cada área tem um script de editor que monta o conteúdo num grupo próprio da cena (`Conteudo_...`), apagando e reconstruindo a cada execução, como o blockout. Os arquivos de dados (missões, diálogos, eventos, itens) só são criados se ainda não existirem: textos mudados no Inspector não se perdem.

| Área | Menu | Script |
| --- | --- | --- |
| 1. Acampamento | **Nibiru > Conteúdo > Povoar Acampamento na cena aberta** | `Scripts/Editor/Content/CampContent.cs` |
| 2. Trincheiras | **Nibiru > Conteúdo > Povoar Trincheiras na cena aberta** | `Scripts/Editor/Content/TrenchContent.cs` |

As ferramentas comuns desses scripts (peças, gatilhos, itens, soldados, helicóptero, diálogos, passos de missão) ficam em `Scripts/Editor/Content/ContentKit.cs`. Rode o Acampamento antes das outras áreas: ele cria a missão "Sobrevivente", e cada área seguinte só acrescenta os passos dela no fim (se ainda não estiverem lá).

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
- Último passo da missão por enquanto: "Atravesse as trincheiras e siga para a Aldeia" (completa com `ChegouAAldeia`, cujo gatilho entra com o conteúdo da Aldeia).
- O rádio de campanha ainda não faz nada (avisar patrulhas fica para depois).

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
| Cenas roteirizadas: sequência com tempo, mover por pontos, tiro de cena, escolha por evento, área de aviso, hélice | `Sequences/` (`TimedSequence`, `MoveAlongPath`, `ScriptedShot`, `StoryEventBranch`, `PlayerZone`, `Spinner`) e `UI/NotificationPoster` | configurados no Inspector |

Prefabs: `Prefabs/Player/Elena` (Elena + câmera + HUD + diálogos; use este em toda cena), `Prefabs/AI/Soldado`, `Prefabs/AI/Lullu`.

## Personagens 3D

Feitos no Meshy, exportados do Blender para FBX:

| Personagem | FBX | Fonte original |
| --- | --- | --- |
| Elena Varga | `Art/Personagens/Elena/Elena_Varga.fbx` + texturas | `Fontes/Personagens/Elena_Varga.blend` |
| Samir Haddad | `Art/Personagens/Samir/Samir_Haddad.fbx` + texturas | `Fontes/Personagens/Samir_Haddad.blend` |

Pendências desses modelos antes de entrarem no jogo:
- São pesados demais para jogo: Elena tem 1,8 milhão de triângulos e Samir 1,5 milhão. O alvo é perto de 50 mil. O objeto `Elena_TESTE_LOW` do .blend tem o mesmo tamanho do original, ou seja, ainda não foi reduzido.
- Não têm esqueleto nem animações. O Samir está em pose A, boa para o Mixamo; a Elena está em pose natural, o que dificulta o rig automático.
- A textura `_ORM` junta oclusão, rugosidade e metal num arquivo só e precisa ser convertida para o formato do material Lit do URP.
- A pasta `Fontes/` fica fora de `Assets`, para a Unity não depender do Blender instalado. Cópias de segurança do Blender (`.blend1`) não vão para o Git.

## Git

- Repositório: `github.com/BarizonDesenvolvimento/Nibiru-Game`, branch `main`.
- **Git LFS** ativo para modelos, imagens, áudio, vídeo e tipografia (veja `.gitattributes`). O LFS guarda cerca de 450 MB (os dois .blend somam 265 MB e os dois FBX, 130 MB). Fora do LFS, nenhum arquivo passa de 1 MB. Cada nova versão de um arquivo grande ocupa espaço de novo na cota de LFS da conta.
- Tudo enviado ao GitHub até as Trincheiras (primeiro push em 20 de outubro de 2026). Commits em português, pequenos e frequentes.

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

1. Povoar a Aldeia: encontro com o Samir na saída das Trincheiras (passo 6), gatilho `ChegouAAldeia`, sobreviventes, altar e a primeira tabuleta (passos 7 a 9), "Remédio para Yusuf" com a entrega de curativos à Dra. Weiss, e a revelação dos filmes do Tom.
2. Seguir área por área: Zigurate, Poço e Wadi.
3. Pendências das Trincheiras: documentos da Fundação (falta uma tela de leitura de documentos) e o rádio de campanha avisando onde estão as patrulhas.
4. Preparar os personagens 3D: reduzir polígonos, montar o esqueleto, criar o material URP e trocar a cápsula pela Elena.
5. Versões simples do Grupo 2 (memória jogável, cenas, diário, crafting, save em disco).
