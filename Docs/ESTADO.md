# Estado do projeto

Retrato de onde o projeto está, para retomar o trabalho numa conversa nova sem precisar reler o histórico. Atualize este arquivo ao fim de cada etapa grande.

Última atualização: 20 de outubro de 2026.

## Onde estamos

Fase 3 do plano (construir a base), indo para a fatia vertical da região 1, o Sítio de Ur.

- O protótipo do personagem está pronto.
- Os **14 sistemas essenciais** da fatia (Grupo 1 de `Docs/Regiao-1-Sitio-de-Ur.md`) estão prontos e testados.
- O **blockout** do mapa do Sítio de Ur está construído.
- Os modelos 3D da **Elena** e do **Samir** chegaram ao projeto, ainda sem esqueleto nem animação.

O próximo passo combinado é **povoar o Acampamento** (área 1) com o conteúdo da missão principal; foi pedido e ainda não foi começado.

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
| `Scenes/Regiao1_SitioDeUr` | A região de verdade. Por enquanto, só o blockout e a Elena no acampamento |

O blockout é gerado pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (`Scripts/Editor/Blockout/SitioDeUrBlockout.cs`). Para mudar o mapa, mude o script e rode o menu de novo. Os discos dourados `Marca_...` marcam onde entra cada conteúdo.

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
- **Git LFS** ativo para modelos, imagens, áudio, vídeo e tipografia (veja `.gitattributes`). O LFS guarda cerca de 450 MB (os dois .blend somam 265 MB e os dois FBX, 130 MB). Fora do LFS, nenhum arquivo passa de 1 MB. Antes do primeiro push, confira a cota de LFS da conta no GitHub.
- Há commits locais ainda não enviados ao GitHub (sem push). Commits em português, pequenos e frequentes.

## Como testar sozinho pela ponte MCP

- A ponte é o `unity-editor-mcp` (porta 7800). Se cair, a Unity provavelmente está reabrindo o projeto; espere ela terminar.
- Para testar comandos, crie um controle virtual (`InputSystem.AddDevice<Gamepad>`) e aplique o estado a cada quadro em `InputSystem.onBeforeUpdate`. O script `Scripts/Editor/BackgroundInputInEditor.cs` (menu **Nibiru > Testes**) faz o jogo aceitar comandos com a Unity em segundo plano, só no editor.
- Nunca recompile com o Play ligado: isso recarrega os scripts no meio da partida e quebra estados. Saia do Play antes.
- O dono do projeto às vezes joga ao mesmo tempo que os testes: posição, vida e munição podem mudar entre um comando e outro.

## Perguntas ainda abertas

- O Kast aparece em pessoa ou só pelo rádio na região 1?
- A execução no passo 4 aparece de longe ou só pelo som?
- Qual o peso da consequência na Aldeia ao devolver a tabuleta ao Poço?
- Nome definitivo do jogo, dublagem da Elena e idiomas de lançamento (do GDD).

## Próximos passos

1. Povoar o Acampamento: tenda da Elena, corpos, rádio com o Kast, chegada do helicóptero e passos 1 a 4 da missão principal.
2. Seguir área por área: Trincheiras, Aldeia, Zigurate, Poço e Wadi.
3. Preparar os personagens 3D: reduzir polígonos, montar o esqueleto, criar o material URP e trocar a cápsula pela Elena.
4. Versões simples do Grupo 2 (memória jogável, cenas, diário, crafting, save em disco).
