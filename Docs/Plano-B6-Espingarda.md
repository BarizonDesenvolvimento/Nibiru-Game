# Plano B6: a espingarda

Projeto em papel, 4/nov/2026. **Tarefa de sistema. APROVADO pelo dono em 4/nov/2026, com as 5 recomendações abaixo.**

## Decisões do dono (4/nov/2026)

1. A **Umm Karim dá a chave da casa** como recompensa do *Remédio para Yusuf*.
2. **6 cartuchos na região inteira**, contando os 2 do cano; soldado não larga cartucho.
3. **Números novos:** 8×16, cheio até 5 m, caindo até 35% aos 14 m, ruído 45. Ajuste fino na fase B.
4. **Derrubada e cambaleio entram agora** (humanos: cambaleio e queda; Lullu: só cambaleio).
5. **Trocar de arma mirando é permitido**, depois da animação de 0,4 s.
6. **Esclarecimento: o Primeiro Servo LEVA DANO da espingarda normalmente** (pelo multiplicador dele); só não cambaleia nem cai, como os chefes de Resident Evil. Isso corrige a frase "inútil de propósito" do item 2: a luta continua sem *depender* de bala, mas o cartucho conta (a queima-roupa tira 32 por tiro dos 750 PV, ×0,25 do multiplicador do Servo).

Se o texto abaixo contradisser esta lista, vale a lista.
Fontes: `Docs/ESTADO.md` (B6, "Troca de arma", "Munição, curativos e kits"), `Docs/Regiao-1-Sitio-de-Ur.md` (tabela de armas), `Docs/GDD.md` (combate com munição escassa) e o código atual (`PlayerWeapons`, `FirearmDefinition`, `Espingarda.asset`).

## O que já existe

- Item `espingarda` e item `cartucho` (ids estáveis) e o dado `Data/Combat/Espingarda` (`arma.espingarda`), já no registro do save. O prefab da arma (Tripo, lote 1) está na Unity, **sem lugar na cena** ("só prefab").
- `PlayerWeapons` atira, recarrega **o pente inteiro de uma vez** e só troca de arma **usando a arma no inventário** (a primeira pega é equipada sozinha). Não tem troca por botão, recarga interrompível, queda de dano com a distância nem empurrão.
- D-pad esquerda/direita e teclas 1/2 estão **reservados e sem ligação** (ME1). Zoom: D-pad cima/baixo e roda.

## 1. Onde a Elena pega a espingarda e quanta munição existe

**Onde (Região 1, "casa trancada da Aldeia", opcional).** Uma casa da Aldeia fica trancada; dentro, a espingarda sobre uma mesa (prefab `Espingarda` em item pegável) e um lote de cartuchos. Proposta de chave: a **recompensa da secundária Remédio para Yusuf** (a Umm Karim, agradecida, entrega a chave). Isso paga uma secundária que hoje só dá antibióticos e faz a espingarda ser opcional de verdade. Pergunta 1.

**Quando.** Depois do passo 7/8, antes do passo 10: quem a pega usa nos soldados da base, no Lullu e no wadi.

**Quanto (padrão Resident Evil: escasso).** A Região diz **6 cartuchos na região inteira**. Proposta: **6 no total, contando os 2 que já estão no cano ao pegar** (hoje a arma "vem carregada"): 2 no cano + 4 de reserva na mesma casa. **Nenhum soldado larga cartucho** (só 9 mm). Sem cartucho espalhado pelo mapa. Resultado: 6 disparos de cano ≈ 6 soldados a queima-roupa, ou 3 a 4 no Lullu, ou nenhum no Primeiro Servo (veja o item 7). Referências: no RE4 Remake a espingarda é o tiro "resolve um problema" com poucos cartuchos por área; na TLOU Parte II o cartucho é raro e o jogador pensa duas vezes antes de gastar. Mesma ideia aqui: cada cartucho é uma decisão.
(Munição da pistola não muda: 30 balas, B2a.)

## 2. Comportamento

| Ponto | Proposta |
| --- | --- |
| Projéteis | 8 por tiro (como hoje), dispersão de **5°** em disco (hoje 6°) |
| Dano | **16 por projétil** (hoje 14; veja item 7), **sem queda** até **5 m**; dali cai linear até **35%** em **14 m** (alcance máximo 14 m, hoje 20 sem queda nenhuma) |
| Cadência | Dois canos: **0,4 s** entre o primeiro e o segundo tiro (como hoje). Pente de **2** |
| Recarga | **Cartucho a cartucho**: 0,5 s para abrir + **0,8 s por cartucho** (1 ou 2). Pode ser **interrompida** por tiro (se houver ao menos 1 no cano), esquiva, facada ou troca de arma; os cartuchos já colocados ficam. Hoje a recarga da pistola tira a munição só no fim; a espingarda tira **um por vez, no momento em que entra**, para a interrupção não perder nada |
| Ruído | 45 (pistola 25; hoje a espingarda tem 35). "Muito barulhenta" na Região. **Conferir no teste** quantos soldados ouvem a 45 m |
| Empurrão da Elena | O coice (`recoil`) sobe para ~7 e a câmera dá o tranco. A Elena **não** é empurrada para trás (evita problemas com a malha e o `CharacterController`) |
| Empurrão/derrubada no alvo | Humanos (soldados): **cambaleiam** até 6 m (cancelam o ataque e perdem a mira por ~0,8 s) e **caem** se 6 ou mais projéteis acertarem a até 3 m (levantam em ~1,5 s: janela para a faca por cima ou um segundo tiro). Lullu: cambaleia até 4 m, não cai. Primeiro Servo: não reage (chefe) |

**Dano contra cada tipo** (o multiplicador mora no alvo, como já é: `IDamageModifier`; a arma não muda):
- **Humanos (Soldado 100 PV, Atirador de elite 100 PV, Líder de esquadra):** a queima-roupa (≤ 3 m) mata de um tiro (8×16 = 128; precisa de 7 projétil acertando). A 8 m, cerca de metade acerta e o dano já caiu: dois tiros.
- **Híbridos (Lullu, 120 PV):** 128 a queima-roupa, quase de um tiro só, **mas ele corre rápido**, então o tiro costuma ser o último antes do golpe. Atordoado (Eco) leva ×1,5.
- **Anunnaki (Primeiro Servo, multiplicador 0,25):** 32 de dano por tiro a queima-roupa contra 750 PV na luta: **inútil de propósito**. A luta continua sem depender de bala (decisão da B2a).

## 3. Troca de arma

- **Teclado:** `1` pistola, `2` espingarda. **Controle:** D-pad **esquerda** = arma anterior, **direita** = próxima (com duas armas, alternam). Sem a segunda arma, o botão não faz nada e mostra um aviso curto ("Sem outra arma").
- A troca leva **0,4 s** (animação), pode ser feita **mirando**: a mira não cai, só troca a arma e a mira (a câmera de mira fica). Cancela a recarga em curso. Em esquiva, golpe de faca, leitura, diálogo ou inventário aberto, **não responde**. Usar a arma pelo inventário continua trocando, para ninguém ficar sem alternativa.
- **Conflitos:**
  - *Zoom:* no D-pad, cima/baixo (zoom) e esquerda/direita (troca) são eixos diferentes do mesmo botão: nenhum conflito, mas **diagonais** podem disparar os dois. Mitigação: só vale o eixo dominante.
  - *Mira:* LT/botão direito continuam mirando; `1`/`2` não colidem com nada (G e 3 a 9 seguem livres).
  - *Poderes:* os poderes usam Q/Z/X e as combinações do LB; **nada** usa D-pad nem 1/2. Sem conflito.
  - *Facada:* **RB sozinho (e F) continua faca**, com qualquer arma. Nada usa RB com o LB seguro (Sopro é LB+RB, e só vale com o LB segurado). Atirar com a espingarda não bloqueia a faca.

## 4. Save

- **Arma obtida:** já é o item `espingarda` na bolsa (salvo com os itens).
- **Arma equipada** e **munição no pente** (`armaEquipada` e o mapa de pentes por código `arma.espingarda`) **já existem** no formato do save v1 e no `Snapshot` do checkpoint, e a arma já está no `SaveRegistry`. A **reserva** é o item `cartucho` na bolsa (já salvo).
- **Novo:** nenhum campo obrigatório. A recarga em curso **não** é salva (checkpoint e carga cancelam); a troca em curso também não.
- **Conversão de save antigo: não precisa.** Slots antigos não têm a espingarda; carregam como estão. O formato não muda, então a regra de versão (`Version = 1`) fica como está. Se na implementação surgir um campo a mais (ex.: "arma selecionada para o D-pad"), entra como **campo opcional** com valor inicial, pela regra já decidida na governança.

## 5. HUD, animações e som

- **HUD (texto, como os poderes):** o `AmmoUI` já mostra `no pente / reserva`; ganha o nome da arma e, na troca, o aviso "Espingarda equipada". Durante a recarga: "RECARREGANDO" (já existe). Sem ícones e sem barra de dano por enquanto.
- **Animações do Mixamo** (a Elena hoje tem *Pistol Aim* e *Reloading*):
  - **Rifle 8-Way Locomotion Pack** (está na lista do dono): idle de mira, andar nas 8 direções mirando. Serve de base para a mira e o andar mirando com a espingarda (ela é arma de duas mãos, como o fuzil).
  - Falta pedir: **tiro** (*Firing Rifle* dos soldados serve como coice), **recarga de espingarda** (*Shotgun Reload*/*Reloading* de arma longa, de preferência o loop de colocar cartucho, para a recarga cartucho a cartucho), **trocar de arma** (guardar/sacar, ~0,4 s). Se faltar o loop, provisório: a recarga da pistola por cima.
  - Segue o fluxo de `Docs/CATALOGO_ANIMACOES.md` e `CharacterSetupKit`: importar só o que for usado, Humanoid, e atualizar a tabela de animações (`AnimationLibrary`).
- **Som provisório:** sintetizado, como os do Poço: tiro grave e longo (maior que o da pistola), "clique" de abrir o cano, cartucho entrando, fechar, troca. Nenhum asset de som novo.
- **Arte:** a espingarda do Tripo (lote 1) já está na Unity; falta só o ponto de encaixe na mão (`heldGun`) e o cano (`muzzle`) para os dois tamanhos de arma.

## 6. Sistemas compartilhados tocados e testes

Compartilhados tocados: **salvamento** (só leitura do que já existe), **prefab da Elena** (arma na mão, animações, entrada), **controles** (D-pad, 1/2), **tabela de animações**, **localização** (avisos novos, textos com chave `ui.weapons...`), **menus que remontam cenas** (a casa da Aldeia e a espingarda entram pelo menu da Aldeia e de suprimentos). A malha de navegação não muda.

**Testes de nível 1:** o teste novo e o `Suprimentos > Teste de contagem` (passa a contar 6 cartuchos e nenhum fora da casa).
**Testes de nível 2** (pela tabela "sistema → testes"):
- Salvamento: *Teste automático* (~3 min), *Teste de códigos* e *Proteção dos slots*.
- Prefab da Elena/controles: *Teste geral dos atalhos* (~8,5 min), *Mouse > Teste automático*, *Poderes > Teste automático* (as combinações de LB não podem mudar), *Câmera > Zoom* (D-pad).
- Animações: *Animações > 4. Teste automático*.
- Localização: *Teste de chaves e códigos*.
- Aldeia: o teste do *Remédio para Yusuf > 15. Teste automático* (a chave entra aqui) e o *Tom* (a casa é nova, não pode quebrar a Umm Karim).

**Teste automático novo:** **Nibiru > Testes > Armas (B6)**: com a espingarda na bolsa a 1 m, 4 m, 8 m e 15 m de um manequim, confere o dano de cada distância; dispersão (cobertura de 8 projéteis dentro do cone); cadência; recarga cartucho a cartucho (1 e 2 cartuchos) e a interrupção por tiro, esquiva e troca sem perder munição; troca por tecla e pelo D-pad (via `PlayerInputReader` com teclado/controle virtual) com 1 e 2 armas; troca mirando; RB faca ainda funciona com a espingarda na mão; derrubada e cambaleio de um soldado; Servo sem reação; save e carga mantendo arma equipada e pente. Atalhos de passo (regra do CLAUDE.md) não precisam de menu novo: o B6 é sistema, não passo da missão, mas ganham **Nibiru > Testes > Armas (B6)** com "Elena na casa da espingarda", "Elena diante de 3 soldados com a espingarda" e "Elena diante do Lullu com a espingarda".

## 7. Os números herdados do blockout: o que está estranho

| Valor atual | Problema | Proposta |
| --- | --- | --- |
| 8 × 14 = 112 de dano total | Mata o Soldado (100 PV) **só se os 8 projéteis acertarem**; com 7 sobra com 98. "Muito forte" na Região não se cumpre | 8 × 16 = 128 (7 de 8 já matam) |
| `spread = 6°`, `range = 20`, **sem queda de dano** | A 20 m o cone tem ~2 m de raio: quase tudo erra, mas o que acerta faz dano cheio. O alcance de 20 m é de espingarda de caça, não de cano duplo | alcance 14 m, queda de dano a partir de 5 m, dispersão 5° |
| `reloadTime = 2.2` para o pente todo | Não permite recarregar 1 só nem interromper | Cartucho a cartucho (item 2) |
| `noiseRadius = 35` | Só 10 acima da pistola (25), para uma arma "muito barulhenta" | 45; conferir no teste quantos soldados atrai |
| Pente carregado ao pegar | Já é o comportamento; ótimo, mas os 2 do cano entram na conta dos 6 | 6 totais (2 + 4) |
| Munição na fatia: "20 balas" no ESTADO antigo vs 30 hoje | Já corrigido na B2a (30) | nada |
| Dano da faca 25, Servo ×0,25 | A espingarda não resolve o chefe | deixar assim |

Nada disso é perigoso para o resto do jogo: soldados têm 100 PV, a Elena 100, o Lullu 120 e o Servo 120 (750 na luta). Eu **não** mexo nesses valores.

## 8. Perguntas em aberto para o dono

1. **Como abre a casa trancada?** Recomendação: a Umm Karim dá a chave como recompensa de *Remédio para Yusuf*. Alternativa: a porta abre com a faca, sem chave (mais simples, perde a recompensa).
2. **Quantos cartuchos?** Recomendação: **6 no total** (2 no cano + 4), como diz a Região. Alternativa: 8, se o dono achar a espingarda fraca demais para o Lullu.
3. **Os números do item 7** (8 × 16, alcance 14 m, queda a partir de 5 m, ruído 45)? Recomendação: aprovar e acertar no teste de equilíbrio da fase B.
4. **Derrubada e cambaleio** em soldados (e só cambaleio no Lullu) entram agora ou ficam para a fase B? Recomendação: entram agora, porque o "empurrão" é o que faz a espingarda parecer espingarda; exige uma pequena interface de reação a golpe nos soldados e no Lullu.
5. **Trocar de arma mirando** é permitido? Recomendação: sim (como no RE4 Remake e TLOU II), mas só depois da animação de 0,4 s.

## Como ficou (4/nov/2026)

Implementado como no plano, com estes detalhes de execução:
- A casa trancada é a **`Casa_8`** da Aldeia (o bloco e a placa `Porta_8` do blockout saem; a casa nova tem porta no lado leste). O marcador `Casa_Trancada_Espingarda` do blockout, em `Casa_5`, não foi usado porque `Casa_5` tem a arte da casa por cima.
- A chave sai do evento `EntregouAntibioticos` (`ItemGiver` na raiz `Conteudo_CasaTrancada`); quem deixa o Yusuf morrer não a recebe.
- O Primeiro Servo é reconhecido pela luta (`FirstServantFight`) por cima da `LulluAI`, e por isso não reage; ele leva o dano pelo multiplicador dele (0,25).
- **A queda de soldado só aparece com 6 acertos exatos:** 6 × 16 = 96 deixa o soldado vivo, e 7 ou mais o matam (100 PV). Fica para o equilíbrio da fase B decidir se a queda precisa de ajuste.
- Falta a animação de espingarda do Mixamo; a reação dos inimigos é provisória (modelo inclinado e empurrado).
- Detalhes, menus, testes e roteiro do dono: `Docs/ESTADO.md`, seção "B6: a espingarda".
