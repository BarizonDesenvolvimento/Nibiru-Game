# Plano: zoom da câmera com a roda do mouse

Projeto no papel (4/nov/2026), **aprovado e implementado em 4/nov/2026** (veja "O que mudou na implementação" no fim). **Tarefa de sistema.**

## 1. O que existe hoje (levantamento, sem alterar nada)

**Quem controla a câmera.** Sem Cinemachine. São dois scripts na câmera do prefab `Prefabs/Player/Elena`:
- `Scripts/Camera/PlayerCameraController.cs`: lê o "Olhar" (mouse ou analógico), guarda `yaw` e `pitch`, prende o cursor e chama o modo ativo no `LateUpdate`.
- `Scripts/Camera/ThirdPersonCameraMode.cs` (implementa `ICameraMode`): calcula a posição. A primeira pessoa futura será outra implementação da mesma interface.
- Valores em `Data/Camera/CameraJogador` (`CameraSettings`).

**Valores atuais (os do arquivo `CameraJogador.asset`, que mandam; os padrões do código são outros):**

| Campo | No asset | Padrão no código |
| --- | --- | --- |
| `distance` (recuo atrás do ombro) | **2,5 m** | 2,5 m |
| `shoulderOffset` (ombro direito) | **0,6 m** | 0,6 m |
| `pivotHeight` (acima do ponto seguido) | **0,6 m** | 0,6 m |
| `aimDistance` | **1,6 m** | 1,2 m (**diverge**: vale o do asset) |
| `aimShoulderOffset` | **0,9 m** | 0,75 m (**diverge**) |
| `aimFieldOfView` / `aimBlendSpeed` | 48° / 6 | igual |
| `collisionRadius` / `returnSpeed` | 0,2 m / 4 m/s | igual |
| `thirdPersonPitchLimits` / `startPitch` | −35° a 60° / 10° | igual |

**Onde a câmera fica em relação à Elena.** O ponto seguido é a raiz do prefab, que fica no **centro da cápsula** (cápsula de 2 m, pé 1 m abaixo; a Elena tem 1,70 m). Com `pivotHeight` 0,6, o pivô fica a ~1,6 m do chão, na altura do topo da cabeça. A câmera sai dali 0,6 m para o lado direito e depois recua 2,5 m. O plano de near clip da câmera é 0,1 m.

**Colisão (`ThirdPersonCameraMode.UpdateCamera`).** Ordem de hoje:
1. `aimBlend` suaviza a mira; `distance = Lerp(settings.distance, settings.aimDistance, aimBlend)` e o mesmo para o ombro.
2. `SphereCast` (raio 0,2) do pivô até o lado do ombro (`FreeDistance`).
3. `SphereCast` do ponto do ombro para trás, até `distance`. Se bater, a câmera **encosta na hora** (`currentDistance = allowed`); se liberar, **volta devagar** a 4 m/s (`MoveTowards`).
4. Conferência final em linha reta do pivô até a câmera; se bater em algo, a câmera para antes (correção do passo 13, túnel estreito).
5. O personagem é ignorado na colisão (`ignoreCollisionsOf`). `PlayerAnimator` esconde o corpo (só a sombra) quando a câmera encosta nele.

**O que muda em cada situação:**
- **Mirar (rifle, pistola):** `PlayerWeapons.SetAiming` → `PlayerCameraController.SetAiming` → `mode.SetAiming`. Só aproxima (`aimDistance` 1,6 m), puxa o ombro para 0,9 m, fecha o FOV para 48° e reduz a sensibilidade (60%).
- **Diálogos, leitura, inventário:** **não há troca de câmera.** `DialogueRunner.Play` chama `input.SetGameplayEnabled(false)` e põe `Time.timeScale = 0`; o "Olhar" é desligado junto, então a câmera fica parada onde estava. `InventoryScreenUI` faz o mesmo. A pausa e a leitura pedem o cursor livre (`CursorPolicy.RequestFree`).
- **Viagem de caminhonete (passo 15):** `TruckRide` usa `input.SetLookWhileLocked` (a câmera gira livre com a Elena sentada) e `AlignTo`. Mesma câmera, mesmos números.
- **Memória jogável (Ziusudra):** `PlayableMemory` só chama `AlignTo` e liga/desliga controles.
- **Corpos do círculo (`CampCircle`):** a câmera não sabe que eles existem. São só colisores no cenário (a câmera usa `collisionMask = DefaultRaycastLayers`, então os colisores dos corpos também a empurram; veja riscos).
- **Modo livre:** são só atalhos de editor que põem a Elena em 5 pontos. A câmera é a mesma.
- **Não existe** câmera de cena, travelling nem Cinemachine em nenhum lugar do projeto.

**Configurações do jogador fora do save.** **Já existe:** `Scripts/UI/GameOptions.cs` (`PlayerPrefs` com chaves `Nibiru.Opcoes.*`: sensibilidade do mouse e do controle, inverter Y), mais o idioma em `Loc.cs`. As opções aparecem na página "Opções" do menu de pausa e do menu principal. O save de slot não as toca.

**Quem lê a roda do mouse hoje:**
- `UI/MenuPointer` (lê `Mouse.scroll` só com um menu aberto; `PauseMenuUI` usa para rolar o Arquivo).
- `Testes/OrbitPreviewCamera` (só laboratório de arte).
- **Ninguém no jogo:** o mapa `NibiruControles` não tem ação de roda; a troca de arma e o inventário não usam a roda. Sem conflito hoje.

## 2. Plano

### a) Faixa de distância

Recomendação: **mínimo 1,8 m, padrão 2,5 m (como hoje), máximo 4,0 m.**

- **Os números herdados conferem?** O padrão 2,5 m sim (é o do asset). Os valores do código (aim 1,2 / 0,75) são diferentes dos do asset (1,6 / 0,9) e **não devem ser usados como referência**; a base é sempre o asset.
- **Mínimo.** A câmera nunca está dentro da cabeça: ela está 0,6 m ao lado e na altura do topo da cabeça, então mesmo com recuo 0 ela fica fora do cabelo. O que limita o mínimo é outra coisa: **a mira fica a 1,6 m**. Um zoom normal mais perto que a mira inverteria o sentido de mirar (mirar afastaria a câmera). Por isso o mínimo é **1,8 m**, um pouco acima da mira, para a mira ainda "puxar" para perto. Abaixo de ~1,2 m o corpo já começa a ser escondido pela regra do `PlayerAnimator`, o que também seria estranho.
- **Máximo 4,0 m.** Em campo aberto (Aldeia, wadi) enquadra a Elena de corpo inteiro com o ambiente. Nos lugares apertados (antecâmara, Poço, túnel dos Guardiões, quarto escuro do Abu Faris) **a colisão já encurta a câmera** (passos 1 a 4 acima), então com o zoom no máximo a câmera ficará "empurrada" quase o tempo todo e o jogador terá, na prática, o enquadramento de ombro de sempre nesses lugares. Isso é aceitável (o zoom não pode quebrar nada, só perde efeito), mas há dois efeitos a medir, não a supor:
  1. mais **trancos** quando a câmera encosta e solta (volta a 4 m/s);
  2. teto baixo com `pitch` alto joga a câmera para dentro da Elena.
  Medição prevista: reaproveitar a varredura do `Step13Tests` (1536 posições no túnel e na rampa) com o zoom no máximo, e repetir na antecâmara, no Poço e no quarto escuro. **Contingência, só se a medição mostrar problema:** um volume de dados que limita o zoom máximo na área (nos moldes do `AreaVolume`). Não entra agora.
- Os três valores ficam em `CameraSettings` (editáveis no Inspector).

### b) Contínuo ou em degraus

Recomendação: **contínuo, com amortecimento** (como pedido). Para a fatia de terror, 3 degraus dariam enquadramentos "autorais" previsíveis, mas multiplicariam o que testar (3 distâncias × mira × áreas apertadas) sem ganho claro, porque a colisão já faz o papel de "enquadramento fechado" nos lugares tensos. O contínuo permite ao dono achar o ponto de que gosta no Play. **Alternativa guardada:** se o playtest mostrar que o jogador "se perde" no meio, trocar o valor contínuo por 3 marcas com ímã (snap) é uma mudança pequena, só na leitura da roda. (Sobre os jogos citados: usei as referências que você deu; não as verifiquei de novo.)

### c) Mira

Ao mirar, **o zoom é ignorado**: a distância vira `aimDistance` (1,6 m), como hoje. Ao soltar, volta à distância escolhida. Não precisa guardar nem restaurar nada: o valor escolhido simplesmente não é tocado enquanto se mira, e o `aimBlend` já faz a transição suave de/para a distância escolhida. A roda **não** muda o zoom enquanto se mira (ignorada, não adiada).

### d) Diálogos, cenas, inspeção, menus

A roda só vale quando **todas** estas condições são verdadeiras: `input.GameplayEnabled`, `!CursorPolicy.AnyFree`, cursor preso (`Cursor.lockState == Locked`) e `!aiming`. Isso cobre diálogo, leitura (Tabuleta do Junco, relevos, documentos), inventário, pausa, Arquivo, morte e fim da demo. **Não há "restaurar"**, porque nada muda a distância escolhida: a câmera já fica parada nesses estados (tempo parado, olhar desligado).

**Exceções decididas:** na **viagem de caminhonete** (passo 15) e nas **memórias jogáveis** o enquadramento é autoral; o zoom fica **desligado** (basta o controlador ignorar a roda enquanto `GameplayEnabled` for falso, e a viagem usa `SetLookWhileLocked`, que não conta como gameplay ligado). A distância escolhida continua guardada.

### e) Colisão

O zoom só muda a **distância desejada**; a colisão continua mandando. Ordem de cálculo por quadro:
1. Controlador lê a roda (se permitido) → atualiza `zoomAlvo` (clampado a [min, máx]).
2. Amortecimento: `zoomAtual = SmoothDamp(zoomAtual, zoomAlvo, ...)`.
3. Modo: `distanciaNormal = zoomAtual`; `distance = Lerp(zoomAtual, aimDistance, aimBlend)`. O ombro e o FOV seguem como hoje (o zoom não mexe neles).
4. Pontos 2 a 4 da colisão de hoje, **sem alteração** (ombro, recuo, conferência final em linha reta).
5. Posiciona a câmera.

Detalhe: o `currentDistance` já volta no máximo a 4 m/s. O amortecimento do zoom fica limitado a ~3,5 m/s (parâmetro `zoomMaxSpeed`) para as duas suavizações não brigarem e o zoom em campo aberto não "atrasar".

### f) Controle (gamepad)

**Nenhum atalho agora.** Pendência do **ME1** (mapa final de controles): decidir se o zoom ganha botão no controle. Equivalente proposto: uma opção **"Distância da câmera"** (controle deslizante) na página Opções, que serve ao controle e ao mouse. Ela entra num passo próprio depois deste (veja perguntas abertas), porque mexe no `PauseMenuUI`, no menu principal e na localização.

### g) Persistência

Já existe o sistema certo: **`GameOptions`** (`PlayerPrefs`, fora do slot). Proposta: nova chave **`Nibiru.Opcoes.DistanciaCamera`**, guardada em **metros**, `float`, padrão 2,5. Na leitura, o valor é limitado a [min, máx] (assim mudar a faixa depois, ou um valor corrompido, não quebra nada). Gravação com `GameOptions.Save()` só **uma vez após 1 s sem girar a roda** e ao pausar (evita gravar em disco a cada passo da roda). **Não mexe no save de slot nem na regra de versão do save.**

### h) Sensibilidade e direção da roda

Em `CameraSettings` (Inspector, ajustável no Play, grupo "Zoom"):

| Campo | Valor inicial | Para que serve |
| --- | --- | --- |
| `zoomMinDistance` | 1,8 | mínimo |
| `zoomMaxDistance` | 4,0 | máximo |
| `zoomMetersPerNotch` | 0,25 | metros por "clique" da roda |
| `zoomInvertWheel` | falso | falso = rolar para frente aproxima |
| `zoomSmoothTime` | 0,15 s | amortecimento |
| `zoomMaxSpeed` | 3,5 m/s | velocidade máxima do zoom |

A leitura normaliza a roda: `Mouse.scroll.y / 120` = cliques (a roda do Windows dá ±120; rodas de alta resolução dão frações, e o cálculo aceita). **Lida direto de `Mouse.current.scroll`** em `PlayerCameraController`, como `MenuPointer` já faz, sem criar ação nova no `NibiruControles` (o mapa é compartilhado e do ME1).

### i) Modo livre

Igual: o zoom vive na câmera da Elena, e o Modo livre só a teleporta. Os pontos 4 (Poço) e 5 (wadi) entram nos testes por serem os mais apertados e os mais abertos.

### j) Testes

**Nível 1: novo teste automático**, menu **Nibiru > Testes > Câmera > Zoom** (`Scripts/Editor/Camera/ZoomTests.cs`), com roda virtual (`QueueStateEvent` com `scroll`, como o `MouseTests` já faz) e `SlotGuard` automático (slots reais conferidos iguais antes e depois). Casos:
1. Roda para frente aproxima; para trás afasta; com `zoomInvertWheel` ligado inverte.
2. **Limites:** muitos cliques para aproximar → para em 1,8 m; muitos para afastar → para em 4,0 m (medidos em campo aberto, distância pivô → câmera; o teste também confere que o mínimo não deixa a câmera dentro do colisor da Elena).
3. **Mira ignora o zoom:** com o zoom em 4,0 m, mirar leva a 1,6 m; rolar a roda mirando não muda; soltar volta a 4,0 m.
4. **Diálogo:** começa a conversa, rola a roda: nada muda; termina: a distância é a mesma.
5. **Menus:** com pausa, inventário e leitura abertos a roda não faz zoom; no Arquivo a roda continua rolando a lista e não faz zoom.
6. **Colisão com o zoom no máximo:** varredura da câmera no túnel e na rampa do passo 13 (reaproveita a do `Step13Tests`), na antecâmara e no Poço: câmera nunca dentro de parede nem da Elena.
7. **Persistência:** valor gravado é lido de volta, valor fora da faixa ou inválido é limitado (o teste guarda e restaura o valor real do `PlayerPrefs`).
8. **Viagem de caminhonete e memória:** roda ignorada.
9. **Modo livre:** em um ponto aberto e em um apertado, a roda funciona.

**Nível 2** (a câmera, o prefab da Elena e os controles são sistemas compartilhados; tempos da tabela do ESTADO):

| Teste | Tempo |
| --- | --- |
| Teste geral dos atalhos | ~5 min |
| Mouse > Teste automático (roda e cursor) | ~1 min |
| Animações > 4. Teste automático | ~10 s |
| Passo 13 > 7. Teste automático (túnel, varredura da câmera) | ~2 min |
| Passo 12 > 7. Teste automático (antecâmara) | ~2 min |
| Passo 15 > 9. Teste automático (viagem de caminhonete) | ~2 min |
| Localização > Teste de chaves e códigos (só se entrar texto, veja perguntas) | segundos |

Total estimado: ~12 min, um por vez. O salvamento não é tocado (nenhum teste de salvamento é exigido, mas o `SlotGuard` confirma "slots reais iguais"). **Nível 3** não é necessário.

### k) Arquivos e riscos

**Criados:**
- `Docs/Plano-Zoom-da-Camera.md` (este arquivo).
- `Scripts/Editor/Camera/ZoomTests.cs` (menu de teste).

**Alterados:**
- `Scripts/Camera/CameraSettings.cs`: campos do grupo "Zoom" (os assets existentes recebem os padrões sozinhos; não precisa editar o `.asset` à mão).
- `Scripts/Camera/PlayerCameraController.cs`: ler a roda, regras de quando vale, amortecimento, persistência.
- `Scripts/Camera/ICameraMode.cs`: ganha `void SetZoom(float distance)`. O `ThirdPersonCameraMode` usa; a primeira pessoa futura implementa vazio.
- `Scripts/Camera/ThirdPersonCameraMode.cs`: usar a distância recebida no lugar de `settings.distance`.
- `Scripts/UI/GameOptions.cs`: chave nova.
- `Docs/ESTADO.md` (uma linha agora; seção completa quando o sistema estiver pronto).
- Se o controle deslizante entrar junto: `UI/PauseMenuUI.cs`, menu principal, `pt-BR.json` e `en.json`.

**Riscos:**
1. **Lugares apertados com zoom alto:** mais trancos e teto baixo (medir, contingência descrita em (a)).
2. **Colisores dos corpos do círculo** empurram a câmera (a máscara é "tudo"). Hoje a 2,5 m já acontece; a 4,0 m vê-se mais. Conferir no círculo; se incomodar, tirar corpos da máscara é decisão à parte.
3. **`ICameraMode` muda** (um método novo): o `Step13Tests` usa a interface; o teste precisa continuar compilando.
4. **Roda do Windows em teclados e mouses diferentes** (passo de 120 ou frações): o teste usa 120, e o valor em metros por clique fica ajustável.
5. **`PlayerPrefs` no teste:** guardar e restaurar o valor real do dono.
6. O editor da Unity também reage à roda; no Play, a roda precisa estar sobre a Game view (já é assim para o olhar).

## 3. Discordâncias das recomendações do dono

1. **Mínimo 1,8 m e não "o mais perto possível da cabeça":** o limite real é a mira (1,6 m), não a cabeça, porque o pivô já está ao lado dela.
2. **Persistir em metros, não em "nível":** simples e protegido pelo limite na leitura.
3. **Ler a roda direto, sem ação nova no mapa de controles:** o mapa é do ME1.
4. **Não existe "restaurar" a distância:** o valor escolhido nunca é alterado enquanto o zoom está bloqueado, então não há o que restaurar. Isso reduz o risco.
5. **Zoom desligado também na viagem de caminhonete e nas memórias**, além dos casos que você listou.

## 4. Perguntas abertas

1. A faixa 1,8 a 4,0 m (padrão 2,5) serve para um primeiro teste no Play?
2. O controle deslizante "Distância da câmera" entra **neste sistema** (mexe em menus e textos) ou num passo logo depois?
3. Se a medição nas áreas apertadas for ruim, você prefere limitar o zoom máximo por área ou aceitar que ali o zoom "perde efeito"?
4. A roda deve ser ignorada durante a viagem de caminhonete (proposto) ou funcionar também ali?
5. **Pendência do ME1:** botão de zoom no controle (hoje, nenhum).

## 5. Pendência registrada

- **ME1 (mapa de controles):** decidir se o zoom terá botão no controle; até lá, o equivalente é a opção "Distância da câmera" nas Opções.

## 6. O que mudou na implementação (4/nov/2026)

Tudo como no plano, com estes ajustes:

- **Faixa:** 1,8 / 2,5 (padrão, o `distance` do asset) / 4,0 m, confirmada pelo dono. Campos novos no grupo "Zoom" do `CameraSettings`: `zoomMinDistance`, `zoomMaxDistance`, `zoomMetersPerNotch` (0,25), `zoomInvertWheel`, `zoomSmoothTime` (0,15 s) e `zoomMaxSpeed` (3,5 m/s). O asset existente recebeu os padrões sozinho.
- **Roda lida direto do mouse** (`Mouse.current.scroll`; o valor de um clique vem de `PlayerCameraController.WheelUnitsPerNotch`: 1 no padrão do Input System, 120 só se a faixa da plataforma estiver mantida; frações para rodas de alta resolução), sem ação no mapa de controles. **Correção de 4/nov:** a primeira versão dividia por 120 sempre e a roda real não fazia efeito; o teste novo agora manda 1 por clique.
- **Onde fica a lógica:** `PlayerCameraController` guarda o zoom escolhido (alvo), suaviza (`SmoothDamp`) e entrega ao modo por `ICameraMode.SetZoom` (método novo; a primeira pessoa futura implementa vazio). `ThirdPersonCameraMode` usa essa distância no lugar de `settings.distance`; a mira continua trocando pela dela (`aimDistance`). As checagens de parede não mudaram.
- **Quando a roda vale (`ZoomAllowed`):** controles da Elena ligados, sem menu aberto (`CursorPolicy`), cursor preso, sem mirar e sem bloqueio. **Bloqueio explícito** (`SetZoomBlocked`) em `PlayableMemory` (memórias) e `TruckRide` (viagem), porque nos dois os controles de olhar continuam ligados e só por isso o zoom valeria.
- **Persistência:** `GameOptions.CameraDistance` (chave `Nibiru.Opcoes.DistanciaCamera`, metros) e `CameraDistanceClamped`. O valor vai para as preferências na hora; a gravação em disco é 1 s depois da última rolagem, e `PauseMenuUI.Open` grava ao pausar. Valor ausente, NaN ou fora da faixa vira o padrão ou o limite.
- **Etapa 2, opção nas Opções:** o menu de pausa é um texto com linhas "‹ valor ›" (não há controle deslizante gráfico), então a opção segue o mesmo desenho das outras: "Distância da câmera ‹ 2,50 m ›", 0,25 m por seta, no teclado, no mouse (metade esquerda e direita) e no controle. Linha 3 das Opções; **Idioma passou a 4 e Voltar a 5.** Textos novos com chave: `ui.opcoes.distancia_camera` e `ui.opcoes.distancia_camera.valor` (`{0} m`), em pt-BR e en.
- **Teste:** 14 cenários (os 9 do plano, mais a Opção igual à roda depois de recarregar, o Modo livre no Poço e no wadi, a memória e a viagem, a mira e a limpeza); mouse virtual do Input System; ~2,5 min. As varreduras de colisão usam o zoom em 1,8 e 4,0.
- **Resultado da varredura das áreas apertadas:** nenhuma posição de câmera dentro de parede ou atravessando (5184 posições: antecâmara, Poço, quarto escuro, túnel e rampa dos Guardiões). Rolando a roda junto de paredes (20 rolagens: antecâmara, andaime do Poço e quarto escuro, 4 direções cada) a câmera nunca passou de 3,6 m/s (o zoom é limitado a 3,5 m/s): **nenhum pulo nem tremor**. Nenhum limite por área foi criado.
- **Observação dos testes:** em duas rodadas, um micro-movimento da roda do computador real (frações de clique, ~0,01 m) entrou durante o teste e derrubou uma conferência exata; as conferências de "valor padrão" agora têm folga de 0,05 m. Não é defeito do jogo.
