# Investigação: a Elena volta fora do ponto de retorno do pé da descida (Poço)

Aberta em 3/nov/2026, na tarefa B2a/B2b (branch `b2-suprimentos-wip`). **Causa não encontrada.** Este arquivo reúne tudo o que foi medido, para continuar numa conversa só disso.

## O defeito

Com os 7 itens da B2 na cena, o teste do salvamento (**Nibiru > Testes > Salvamento > Teste automático**) falha em 2 itens, sempre no mesmo checkpoint:

- `Cada checkpoint da região`: `poco.checkpoint_pe_da_descida: a Elena deveria voltar ao ponto do checkpoint (48.40, -10.50, 71.40), está em (47.10, -9.86, 70.95) (1,37 m do ponto)`.
- `Três slots independentes`: `o slot 3 deveria pôr a Elena em poco.checkpoint_pe_da_descida`.

A tolerância do teste é **0,15 m** (`SaveTests.cs`, ~linha 142; foi posta na B1 depois de um colisor empurrar a Elena neste mesmo ponto).

O que mudou na cena em relação ao último estado verde (1º/nov, 19 OK): saiu a munição fixa da arena (`Municao_Plataforma_Oeste`, 8 balas) e entrou o grupo `Conteudo_Suprimentos` com 7 itens (4 lotes de balas, 3 kits). O ponto de retorno em si não foi tocado.

## Resultados das rodadas do teste do salvamento

| Rodada | Configuração | Resultado |
| --- | --- | --- |
| 1 | 7 itens; balas da descida em (49,9; −10,1; 72,2), na rampa `Lance_3`, a 1,7 m do ponto | 2 FALHOU, posição final (47,10; −9,86; 70,95) |
| 2 | igual à 1 (repetição) | 2 FALHOU, mesma posição |
| 3 | só `Municao_Poco_Descida` desligada e cena salva; os outros 6 itens ativos | **passou tudo (19 OK)** |
| 4 | balas da descida movidas para o `Patamar_2` (49,5; −7,0; 79,6), a uns 8 m do ponto | 2 FALHOU, mesma falha no mesmo checkpoint |

A falha é determinística e não depende da posição do item. Desligar só esse item faz o teste passar.

## Sonda de reprodução

Reproduz só o retorno do pé da descida, sem o teste inteiro. Roda com a Unity em Play na cena `Regiao1_SitioDeUr`, pela ponte (`eval`). Nomes completos (os `using` não valem no `eval`). Não grava save.

```csharp
var d = new Nibiru.Save.Checkpoints.Data {
  sceneName = "Regiao1_SitioDeUr",
  position = new UnityEngine.Vector3(48.4f, -10.5f, 71.4f),
  rotation = UnityEngine.Quaternion.Euler(0, 270, 0),
  health = 100,
  slots = new System.Collections.Generic.List<Nibiru.Inventory.ItemStack>(),
  keyItems = new System.Collections.Generic.List<Nibiru.Inventory.ItemStack>(),
  world = Nibiru.Save.WorldState.Capture() };
Nibiru.Save.Checkpoints.Save(d);
var log = new System.Text.StringBuilder();
int frames = 0;
UnityEditor.SessionState.SetString("probe", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () => {
  var m = UnityEngine.Object.FindFirstObjectByType<Nibiru.Player.PlayerMotor>();
  if (m == null) return;
  frames++;
  if (frames <= 90 && (frames <= 3 || frames % 30 == 0)) {
    var p = m.transform.position;
    log.AppendLine("q" + frames + " " + p.x.ToString("F2") + "," + p.y.ToString("F2") + "," + p.z.ToString("F2"));
    UnityEditor.SessionState.SetString("probe", log.ToString()); }
  if (frames > 90) UnityEditor.EditorApplication.update -= cb;
};
UnityEditor.EditorApplication.update += cb;
Nibiru.Save.Checkpoints.Restart("Regiao1_SitioDeUr");
return "ok";
```

Depois, ler o resultado com `return UnityEditor.SessionState.GetString("probe", "vazio");`. Para a comparação sem o item: sair do Play, desligar `Conteudo_Suprimentos/Municao_Poco_Descida`, salvar a cena e ligar o Play de novo (um `Find` não acha objeto desligado; religue pelo menu **Nibiru > Conteúdo > Posicionar munição e kits na cena aberta**, que refaz o grupo).

## Posições medidas (ponto de retorno: 48,40; −10,50; 71,40)

| Situação | Posição da Elena depois do carregamento |
| --- | --- |
| Teste do salvamento, item ativo (falha) | (47,10; −9,86; 70,95) |
| Sonda, **item ativo** | (48,23; −9,45; 71,12), estável do quadro 1 ao 90 |
| Sonda, **item desligado** | (47,10; −9,86; 71,12), estável do quadro 1 ao 90 |

Duas coisas: (a) a posição sem o item na sonda é igual, em x e y, à posição da falha do teste; (b) o item ativo muda o resultado da sonda (48,23 contra 47,10 em x; −9,45 contra −9,86 em y).

## Teletransporte no Play (deslocamento zero)

Com o jogo já em Play e a Elena parada, desliguei o `CharacterController`, pus a posição e liguei de novo, em 6 pontos do `Patamar_3` (inclusive o ponto exato do checkpoint), e li a posição 12 quadros depois. Deslocamento 0,00 m em todos:

`(48,40; −10,50; 71,40)`, `(49,00; −10,50; 71,00)`, `(49,00; −10,50; 70,60)`, `(48,80; −10,50; 70,80)`, `(49,20; −10,50; 70,70)`, `(49,00; −10,30; 70,80)`.

Conclusão: a geometria sozinha não empurra a Elena. O deslocamento acontece na colocação durante o **carregamento** da cena.

## Geometria do pé da descida

Medida com os colisores da cena (`Collider.bounds`):

- `Patamar_3`: centro (49,0; −10,7; 71,0), meia-extensão (1,0; 0,1; 1,0): plataforma de **2 × 2 m** (x de 48 a 50, z de 70 a 72), topo em y ≈ −10,6.
- O ponto de retorno (48,4; −10,5; 71,4) fica a **0,4 m da borda oeste** do patamar, onde começa a `Lance_4` (rampa para baixo, para o oeste, centro (44,8; −12,4; 71,0)), e a 0,1 m do começo da `Lance_3` (rampa que sobe de z = 72, a AABB dela começa em z ≈ 71,5).
- `OverlapCapsule` no ponto (raio 0,55): sobrepõe `Lance_3` e `Patamar_3`.
- `CharacterController` da Elena: raio 0,5, altura 2, centro (0, 0, 0), `skinWidth` 0,05, `stepOffset` 0,3, `slopeLimit` 45.
- `Patamar_3_Base` (pilar sob o patamar, centro (49,0; −12,4; 71,8)) e `Lance_4_Guarda` (centro (46,3; −12,0; 72,1)) também ficam por perto.
- Os itens da B2 não estão nessa lista de sobreposição.

A B1 deixou este ponto com 0,15 m de tolerância justamente porque um colisor em cima dele já o empurrou antes.

## A contradição

- Na **sonda**, com o item desligado, a Elena sai deslocada 1,3 m.
- No **teste do salvamento**, com o item desligado (rodada 3), o checkpoint passou.

Então a sonda não reproduz o fluxo do teste. O teste faz outra coisa antes de carregar: `Prepare(n)` (reputação, transformação, inventário, armas, vida), `Cp(code).ActivateNow()`, `Checkpoints.Restart` pela memória e depois, em outro `Reload`, `SaveSlots.Load(1)`; a checagem de posição vem depois do segundo. Também pode pesar o fato de a sonda partir de uma partida que começou no Acampamento, e o teste, de um estado reiniciado. O que está claro é que **o item ativo ou desligado muda o resultado de um carregamento**, e que o teste passa ou falha conforme isso.

## Pistas descartadas

1. **Id stale no teste (`poco.municao_plataforma_oeste`, linha 906):** só é usado em `Prepare` para marcar quanto resta de um pickup; nenhum passo procura munição por tipo, região ou ordem. A posição final também não corresponde às balas antigas (38,5; −10,95; 88,6) nem às novas.
2. **Coleta automática ao carregar:** `ItemPickup` só age em `Interact`. Nada em `Save/*.cs` trata pickups além de guardar quanto resta (`WorldState`).
3. **Colisão do item com o ponto de retorno:** o item não aparece na sobreposição do ponto; e a falha se repete com o item a 1,7 m e a uns 8 m.
4. **Dois ids de salvamento iguais:** o teste de contagem confere códigos únicos (20 códigos, sem repetição).

## Ainda não verificado (hipóteses)

- **Ordem e tempo no carregamento:** `PlayerRespawn.Awake` põe a Elena com o `CharacterController` desligado e o liga depois; o primeiro `Move` pode resolver a sobreposição com `Lance_3`/`Patamar_3` de forma diferente conforme o que existe na cena naquele quadro. Um objeto a mais com `PersistentId` e `ItemPickup` pode mudar a ordem de `Awake`/`OnEnable`.
- **`Physics.autoSyncTransforms`:** se estiver desligado, a posição escrita no `Awake` pode ainda não estar nos colisores quando o controle liga.
- **Posição no quadro 0:** ver onde a Elena está no `Awake` e no `Start` (a sonda só lê a partir do primeiro quadro do editor), por exemplo com um `Debug.Log` temporário no `PlayerRespawn`.
- **Ponto de retorno mais folgado:** só como saída de contorno, e só depois de achar a causa (a B1 já mostra que mexer nele esconde o defeito, não o explica).

## Como continuar

1. Fazer checkout da branch `b2-suprimentos-wip` (cena com os 7 itens ativos, estado que falha).
2. Colocar um log temporário na posição da Elena em `PlayerRespawn.Awake`, em `Start` e no primeiro `Update` do `PlayerMotor`, com e sem `Municao_Poco_Descida`.
3. Rodar o teste do salvamento só até o checkpoint `poco.checkpoint_pe_da_descida` (hoje só roda inteiro) para ganhar tempo.
4. Depois da correção: rodar de novo o salvamento, os passos 11, 12 e 13, o teste geral dos atalhos e a contagem (**Nibiru > Testes > Suprimentos**), atualizar o ESTADO e fazer o merge.
