# Lições para o próximo jogo

Documento de fechamento do GENESIS CODE (nome interno: Nibiru), escrito em 5/nov/2026, quando o projeto foi congelado. Serve para começar o próximo jogo, o primeiro para lançar na Steam, levando o que deu certo e evitando o que deu errado.

**Como ler as fontes citadas:**
- `ESTADO L123` é a linha 123 do `Docs/ESTADO.md` na versão do commit `3d94187` (4/nov/2026, a última antes do congelamento). A linha de aviso do congelamento foi posta depois, no topo, e empurra tudo duas linhas para baixo.
- Hashes de 7 letras (`fbdce10`) são commits do Git. Datas no formato dd/mm.
- **(inferência)** marca o que é conclusão minha, sem prova direta no repositório.
- **(dono)** marca o que veio do pedido do dono em 5/nov e não está registrado no repositório.
- Números de contagem de commits e linhas de código foram medidos em 5/nov com `git log` e `wc`. A classificação de commits por sistema foi feita por palavras-chave nas mensagens: é **aproximada** (inferência).

---

## 1. Resumo executivo: as 20 lições mais importantes

Em ordem de impacto no próximo jogo.

| # | Lição | De onde vem |
| --- | --- | --- |
| 1 | Rode o checklist de originalidade **no papel**, antes da primeira linha de código. A cópia de Tomb Raider só apareceu com 15 passos codados. | (dono); GDD e Bíblia entraram 7 min depois do projeto (`4f415e1`, 19/10 22:18); nenhum passo de revisão de originalidade em nenhum documento |
| 2 | Escolha uma direção de arte que **não dependa de rosto humano realista de perto**. Personagem 3D foi o maior sorvedouro de commits e o único problema que ficou sem solução. | 66 commits de personagem e animação (20% do total), 13 deles de correção; olhos, andar e IK sem solução (dono) |
| 3 | Dimensione o jogo pelo que a arte e a animação aguentam, não pelo que o código aguenta. O código foi rápido; a arte não. | 14 sistemas em ~2 h de commits (19/10 22:31 a 20/10 05:42); fase A ainda aberta 16 dias depois do primeiro commit (`ESTADO L35-38`, `L1530`) |
| 4 | Corte o escopo do jogo inteiro antes de começar: o GDD pedia 25 a 40 h, 6 regiões, 5 finais, NG+, crafting e primeira pessoa. | `GDD.md`, "Visão geral", "Regiões", "Finais" |
| 5 | Uma fonte da verdade, **dentro do repositório**. Decisões no Claude Doc da governança obrigaram o Claude Code a pedir "traga do documento de design". | `Plano-ME1` §1.1 ("não está no repositório"), §4 pergunta 3, §6 |
| 6 | ESTADO.md com limite de tamanho. Ele cresceu 44 vezes em 15 dias (6,6 KB para 295 KB) e era lido em toda tarefa. | `7cbb981` (20/10, 6.645 bytes) e `3d94187` (4/11, 295.018 bytes); 135 dos 325 commits mexem nele |
| 7 | Um teste nunca pode calcular o resultado com a mesma função que o jogo usa. Assim ele nunca acha o erro. | `ESTADO L586` (clique nas Opções) |
| 8 | Tolerância frouxa em teste esconde defeito. O teste do retorno aceitava 1,5 m de erro na altura e por isso nunca viu a Elena nascer 1 m enterrada. | `Investigacao-Respawn-Poco.md`, "Causa e correção"; `ESTADO L1096` |
| 9 | Teste automático precisa de isolamento garantido por código, não por aviso. Dois testes ao mesmo tempo apagaram os 3 slots reais do dono. | `ESTADO L1408-1411` (incidente de 26/10) |
| 10 | Defina no dia 1 as medidas do personagem (cápsula) e derive delas as medidas de porta, corredor e degrau. A porta de 1,4 m prendeu a Elena; o guia de arte mandava portas de 0,9 a 1,2 m. | `ESTADO L1475`; `Arte/Regiao1_SitioDeUr_Arte.md` §0.3 |
| 11 | Defina no dia 1 a convenção de posição (pé no chão, não centro do corpo). A troca no meio do projeto gerou uma investigação de 11 KB. | `Investigacao-Respawn-Poco.md`; `ESTADO L1027`, `L1124-1127` |
| 12 | Localização com chaves e códigos permanentes desde o primeiro texto. Funcionou muito bem, mas o retrofit extraiu 576 textos e a parte 2 nunca foi feita. | `ESTADO L1055-1063`; `6b67a9c` |
| 13 | Save com códigos permanentes e versão desde o primeiro dado. Eventos e facções ficaram com o nome do arquivo como código: renomear quebra o save. | `ESTADO L452-466` |
| 14 | Atalhos de teste por momento do jogo e um teste geral que roda todos eles foi o maior acerto de processo. Achou defeitos reais. | `ESTADO L987-989` (atalho que caía 119 m); 78 → 107 atalhos OK |
| 15 | Use o Unity Test Framework e rode testes sem a ponte MCP, em modo batch. Hoje nenhum teste roda sem o editor aberto e ninguém olhando. | 0 testes NUnit (`ESTADO L1416`); `com.unity.test-framework` instalado sem uso |
| 16 | Separe o código em módulos (asmdef) desde o começo. Sem isso, os sistemas viraram uma teia de dependências e extrair um "kit base" vai exigir cortes. | 0 arquivos `.asmdef`; Save depende de UI, Quests depende de UI, Inventory e Dialogue dependem um do outro (medido em 5/11) |
| 17 | Montar cena por script (blockout e conteúdo) é ótimo, mas a ordem dos menus virou uma dependência frágil e manual. | `ESTADO L426`, `L1098`, `L1143`, `L1424` |
| 18 | Escala dos modelos de IA nunca vem certa. Meça cada um contra uma tabela de alturas e um boneco de referência na importação. | Meshy entrega tudo com ~0,95 m (`ESTADO L662`); helicóptero menor que o piloto (`L1508`); altar do Tripo esticado 3,2× (`L1396`) |
| 19 | O papel deve ter o tamanho da tarefa. O plano do zoom da roda tem 20 KB; o do ME1 tem 41 KB e entregou dados sem efeito jogável novo. | `Plano-Zoom-da-Camera.md` (198 linhas); `Plano-ME1` §6 ("só como dados") |
| 20 | Registre o custo em dinheiro desde o começo (e as horas, se o dono quiser, só para dimensionar, nunca como prazo). Não há nenhum valor gasto em Meshy, Tripo ou ChatGPT registrado. | `Creditos-e-Licencas.md` (só "Pro, pago"); `6397a71` tirou a única linha de calibração |

---

## 2. Linha do tempo real do projeto (pelo Git)

### 2.1 Números gerais

| Medida | Valor | Fonte |
| --- | --- | --- |
| Primeiro commit | 19/10/2026 22:11 (`ce6929a`) | `git log` |
| Último commit | 04/11/2026 20:03 (`3d94187`) | `git log` |
| Commits | 325, todos do mesmo autor | `git log` |
| Dias com commit | 8 (19, 20, 21, 25 e 26/10; 1, 3 e 4/11) | `git log` |
| Dias sem commit no meio | 22 a 24/10, 27 a 31/10, 2/11 | `git log` |
| Horas ativas estimadas | ~43 h (soma dos intervalos entre commits menores que 90 min) | inferência: commits saem em lotes no mesmo minuto, então isso mede sessões, não esforço exato |
| Coautor IA | 169 commits com Opus 5.5, 152 com Sonnet 5.5, 4 sem coautor | rodapé dos commits |
| Commits que mexem só em documentação | 96 (30%) | arquivos tocados por commit |
| Commits que mexem em alguma documentação | 159 (49%) | idem |
| Commits que mexem no `ESTADO.md` | 135 (42%) | `git log -- Docs/ESTADO.md` |
| Commits que mexem no `CLAUDE.md` | 34 | `git log -- CLAUDE.md` |
| Uma branch além da `main` | `b2-suprimentos-wip` (investigação do respawn, 3/11) | `git branch -a` |

### 2.2 O que foi feito em cada período

| Período | Commits | O que aconteceu | Commits marcantes |
| --- | --- | --- | --- |
| 19/10, 22:11 a 23:49 | 28 | Projeto Unity, guia da ponte MCP, GDD e Bíblia, protótipo (andar, correr, esquivar, câmera), proposta da Região 1, e 7 dos 14 sistemas: interação, inventário, vida, checkpoints, faca, armas, furtividade | `4f415e1` (GDD e Bíblia, 22:18), `3c4295f` (câmera, 22:46), `fc13c1e` (Região 1, 22:59), `5d0f73d` (furtividade, 23:49) |
| 20/10, 04:59 a 16:33 | 32 | Os outros 7 sistemas (IA dos soldados, missões, diálogos, facções, tabuleta e Eco, Lullu e chefe, pausa) em 43 min; prefab da Elena, blockout do mapa, LFS, criação do ESTADO.md; Acampamento, Trincheiras e começo da Aldeia | `e75ff3a` (Grupo 1 pronto, 05:42), `68452db` (blockout), `7cbb981` (ESTADO criado), `8176ad8` (Acampamento) |
| 21/10 | 22 | Ninsar (manto por script, que falhou), Elena animada no lugar da cápsula, passos 7 e 8, memória de Shuruppak, regra dos atalhos por passo | `d4bdab2` (scripts do manto), `9e1549e` (Elena animada), `a41d311` (memória), `603a525` (regra dos atalhos) |
| 25/10 (o dia mais longo) | 104 | Passo 9; todos os personagens definitivos (Elena, Samir, 4 soldados, Lullu, Servo) com AccuRig e Mixamo; passos 10 a 15 em um dia; regras de eficiência no CLAUDE.md; biblioteca de animações; Remédio para Yusuf; auditoria da fatia; início da localização | `fd95f2b` (Elena definitiva), `a655bfe` (soldados), `6a206ff` (passo 10) a `b462d07` (passo 15), `b6639b4` (eficiência), `36606c8` (auditoria) |
| 26/10 | 72 | Localização parte 1 (6 commits em 24 min), final do Tom, 2º líder e helicóptero, inscrição e 2º Lullu, teste geral dos atalhos, Arquivo (A3), salvamento em 3 slots (A4a, A4b), mouse e proteção dos slots (A4c), B1 (checkpoints), B1v (6 personagens e vitrine da Aldeia, etapas 1 a 4), **incidente dos slots** | `b29199e` a `cc528dd` (localização), `0e2218c` (teste geral), `fbdce10` (save), `30a04df` (incidente) |
| 1/11 | 11 | Proteção dos slots na camada de escrita; Elena nova (segundo modelo), locomoção feminina, braços; plano oficial em fases A a E | `1f4b9c2` (trava), `37f67cf` (Elena nova), `51b2f1f` (fases) |
| 3/11 | 33 | B2a/B2b (munição e kits) com a investigação do respawn; pose parada da Elena; vitrine etapa 5 (Tripo); lotes de arte do Tripo e do Meshy no jogo; corpos da equipe (7 corpos, depois 4 lugares na mesma noite); modo livre | `4c13832` (investigação), `dbcf97e` (correção), `4613b59` (pose), `c897ad8` e `c28fb5d` (corpos), `f9f266a` (modo livre) |
| 4/11 | 23 | Zoom da câmera (plano, 2 etapas, teste, correção da roda), ME1 (plano, 3 etapas, regressão completa), B6 espingarda, câmera livre de conferência (2 correções), empunhadura com IK (2 rodadas, sem resolver) | `a523dfb` (plano do zoom), `ef10037` (roda 1 e não 120), `0375547` (ME1 fechado), `3f104d8` (B6), `704f45b` e `3d94187` (empunhadura) |

### 2.3 Commits por sistema e commits de correção

Classificação pelas palavras da mensagem de cada commit (aproximada; inferência). "Correção" é o commit cuja mensagem diz que algo estava errado (corrige, restaura, volta ao de antes, ficava presa, afundava, não 120...). O número real de correções é maior: muitas correções foram feitas dentro do commit da própria tarefa e só aparecem no ESTADO.

| Sistema | Commits | Correções na mensagem | Correções registradas no ESTADO (exemplos) |
| --- | --- | --- | --- |
| Documentação pura (ESTADO, Docs, CLAUDE.md, créditos) | 81 pela mensagem, 96 pelos arquivos | — | Índice desatualizado (`ESTADO L147` diz que o ME1 "aguarda aprovação"); tabela quebrada (`Regiao-1` L310-311); padrão do "Forearm Stretch" escrito errado (`ESTADO L773`) |
| **Personagens 3D e animação** | **66** | **13** | Pés afundando no Lullu (`94261b7`, `3114d0d`); cortes do Samir apagados pelo ElenaSetup (`edfcab6`, `ESTADO L773`); braços cruzados atravessando o busto (`e4a5e0e`); 5 limites de braço testados (`ESTADO L773`); prefab da Elena revertido (`7c3eb60`); idade da Elena 27 → 26 (`74c5525`, `958c96e`) |
| Conteúdo dos passos e secundárias | 39 | 1 | Gatilhos de área (`8662b52`); três zonas com o mesmo nome quebraram testes (`a2202fb`) |
| Sistemas base do primeiro dia | 24 | 1 | Controles ao recomeçar a cena (`0d8c672`) |
| Arte de objetos e cenário | 23 | 2 | Relevos dentro da parede (`9b86706`); relevos sem emissão (`d9b3b41`); Selo atrás da parede, Weiss dentro do monte, pistola 12 cm fora da mão (`7ecf6a8`) |
| Salvamento, slots e ponto de retorno | 18 | 2 | Retorno fora do canto (`e8e1fe4`); pé no ponto, não o centro (`dbcf97e`), com a investigação de 11 KB e uma branch própria |
| Inimigos e IA | 18 | 1 | Teste do flash que falhava às vezes (`74343e2`, `ESTADO L275`); cantos que prendiam a Elena: 429 pontos presos viraram 23 (`ESTADO L267`) |
| Mouse, menus e interface | 16 | 6 | Cursor que não sumia e clique das Opções sempre diminuindo, achados pelo dono (`536b0e9`, `9a6b807`, `ESTADO L586`); erro ao fechar a cena (`b07f314`); método ambíguo para testes por reflexão (`a892959`) |
| Câmera e zoom | 11 | 4 | Câmera atravessando o canto da porta (`4b9ee26`); roda que mandava 1 e o código esperava 120 (`ef10037`); câmera livre com duas correções depois do teste do dono (`3b04835`, `ff58c15`) |
| Localização | 9 | 0 | Nenhuma correção. Foi o sistema mais limpo |
| Narrativa (GDD e Bíblia) | 8 | 3 | Ato 1 corrigido (`6b89db2`); idade da Elena duas vezes; conflito das três tabuletas (`ESTADO L62`) |
| Armas e empunhadura (B6) | 7 | 2 | Porta que prendia a Elena (`024b999`); empunhadura em duas rodadas, ainda "a olho" (`3d94187`, `ESTADO L1501-1504`) |
| Poderes e controles (ME1) | 5 | 0 | Nenhuma no código; o atraso do LB fica para revalidar (`ESTADO L1183`) |

**Quem consumiu mais tempo e mais correções:**
1. **Personagens e animação**: o maior número de commits e de correções, e o único problema sem solução no fim (olhos, andar e correr, braço esticando no IK; dono). Quatro versões do modelo da Elena em 12 dias (`e9cdd92` e `57f5e8a` em 20/10, `fd95f2b` em 25/10, `37f67cf` em 1/11).
2. **Documentação**: quase um terço dos commits.
3. **Menus e mouse**: o menu feito de um texto só (sem botões) obrigou a criar um hit-test pelo gerador de texto e conferi-lo em 3 resoluções (`ESTADO L564`, `7735f60`). Isso gerou 6 correções (inferência: o custo veio da escolha de não usar botões de interface).
4. **Salvamento e retorno**: poucos commits, mas o maior incidente (slots apagados) e a investigação mais longa.

---

## 3. Processo e governança

### 3.1 O que funcionou

| Prática | Por que funcionou | Evidência |
| --- | --- | --- |
| **Fases sem data**, cada uma com um marco | O dono decidia pelo marco, não pelo calendário. Nenhuma tarefa foi apressada para "caber" | `ESTADO L7-17`; `CLAUDE.md`, "Sem metas de data"; `6397a71` |
| **Projeto em papel antes de codar** (tarefas de sistema) | O dono aprovava as decisões antes; as discordâncias do Claude ficavam explícitas (seção "Discordâncias") | `Plano-ME1` §3 e §6; `Plano-B6-Espingarda`, "Decisões do dono"; `Plano-Zoom` §3 |
| **Um sistema por vez, testável sozinho** | Os 14 sistemas do Grupo 1 ficaram prontos e testados no laboratório antes de qualquer área | `Regiao-1` "Andamento" (20/10); `e75ff3a` |
| **Atalhos de teste por passo** (Nibiru > Testes > Passo N) | Qualquer momento do jogo em um clique, com o mundo como se a Elena tivesse jogado até ali. O dono testava sem jogar 40 min | `603a525` (regra); `ESTADO L1645` (`TestShortcuts.cs`) |
| **Teste geral dos atalhos** | Roda todos os atalhos sozinho (os novos entram sem mexer no teste) e confere se a Elena está viva e no chão. Achou um atalho que nascia dentro de um talude e caía 119 m | `ESTADO L987-989` |
| **Níveis de teste** (1, 2, 3) e "tarefa leve x tarefa de sistema" | A regressão completa (~40 min, inferência pela tabela) deixou de rodar a cada tarefa | `ESTADO L1426-1463`; `CLAUDE.md`, "Tamanho da tarefa" |
| **Modo livre** e **Elena invencível nos atalhos** | O dono conferia o mundo todo sem morrer nem esbarrar em barreira | `ESTADO L1226-1234`, `L1629` |
| **Cenas montadas por script** | Refazer o mapa era rodar um menu; nada editado à mão em `.unity` | `ESTADO L160-177` |
| **Créditos e licenças no mesmo commit do asset** | A declaração de IA para a Steam já está pronta | `Creditos-e-Licencas.md` |
| **Lições técnicas registradas na hora** (Instantiate recursivo, timeScale 0, aparelho virtual que sobra) | Viraram regra antes de se repetirem | `ESTADO L342`, `L1130`, `L1502` |

### 3.2 O que pesou

| Problema | Evidência | Efeito |
| --- | --- | --- |
| **ESTADO.md enorme, lido em toda tarefa** | 6.645 bytes em 20/10 (`7cbb981`), 99.420 em 25/10 (`1288cfa`), 193.388 em 26/10 (`cf63a9b`), 295.018 em 4/11. 1.662 linhas. +2.184 e -518 linhas no histórico | Cada conversa começava lendo ~75 mil palavras (inferência: ~70 a 80 mil tokens por leitura completa). O próprio CLAUDE.md tinha que avisar para "ler em partes" |
| **ESTADO misturava tudo** | Estado atual, histórico de cada etapa, roteiros de teste do dono, tabelas de atalhos, incidentes, decisões e lições, no mesmo arquivo | Partes antigas ficaram desatualizadas: `L147` (ME1 "aguarda aprovação" quando já estava fechado), `L356` (menu sem tela de slots, já feita), `L369` (Yusuf "ainda cápsula", já trocado em `L1366`) |
| **Decisões fora do repositório** | O Claude Code pediu "traga do documento de design" (`Plano-ME1` §4, pergunta 3); "Respostas do dono, trazidas do documento de design" (§6); a auditoria de originalidade não está no repositório | O Claude Code planejava sem ver as decisões; o dono fazia de mensageiro entre as duas conversas |
| **Decisão registrada mas não implementada** | A regra de versão do save foi decidida em 26/10 e "só está registrada: o código continua com a regra antiga" (`ESTADO L1025`) | Documento e código divergiam, e o leitor precisava saber qual valia |
| **Nomes de etapa que mudaram** | Etapas A1 a A4c, B1 a B7, B1v, ME1 a ME8, fases A a E, "etapas internas antigas mapeadas para as fases" (`ESTADO L19-33`) | Três sistemas de nomes ao mesmo tempo |
| **Planos grandes para tarefas pequenas** | Zoom da roda: 198 linhas (`Plano-Zoom`). ME1: 413 linhas e 41 KB; entregou dados do Sopro e da Égide "sem efeito e sem fonte que os dê" (`Plano-ME1` §6) | Tempo de leitura e de aprovação maior que o da implementação (inferência) |
| **Roteiros de teste do dono longos** | A4c: 19 passos (`ESTADO L590-612`); A4b: 13 (`L544-558`) | O dono virou testador de regressão manual |

### 3.3 Modelo leve para o próximo jogo

**Regra central: uma única fonte da verdade, no repositório.** A conversa de governança no Claude pode existir, mas não guarda decisão. Toda decisão tomada lá vira uma linha em `Docs/DECISOES.md` no mesmo dia, colada pelo dono ou escrita pelo Claude Code. O que não está no repositório não foi decidido.

**Arquivos e limites:**

| Arquivo | O que entra | O que não entra | Limite |
| --- | --- | --- | --- |
| `CLAUDE.md` | Regras de trabalho e decisões técnicas fixas | Estado, histórico, fase atual em parágrafo longo | ~8 KB |
| `Docs/ESTADO.md` | Fase atual e marco; o que está pronto (uma linha por sistema, com o teste que o cobre); o que está aberto; próximo passo; últimos 5 relatórios (só links); última regressão completa (uma linha) | Histórico de etapas, roteiros de teste, tabelas de atalhos, incidentes, detalhes de implementação | **8 KB (~150 linhas)**. Um teste de editor falha se passar disso |
| `Docs/DECISOES.md` | Uma linha por decisão: data, decisão, motivo curto, quem decidiu, onde está no código. Decisão trocada fica riscada com o link da nova | Discussão | Sem limite, mas uma linha cada |
| `Docs/Sistemas/<Sistema>.md` | Como o sistema funciona, onde fica, como ajustar, como testar | Histórico de como chegou lá | ~5 KB cada |
| `Docs/Relatorios/AAAA-MM-DD-<tarefa>.md` | Relatório de passagem de cada tarefa (modelo abaixo) | — | 1 página |
| `Docs/Historico/` | O que sai do ESTADO quando uma etapa fecha (roteiros, incidentes, medições) | — | Ninguém lê por padrão |
| `Docs/Planos/<Tarefa>.md` | Projeto em papel das tarefas de sistema | — | Tarefa pequena: 1 página; grande: 3 páginas |

**Modelo do relatório de passagem** (`Docs/Relatorios/`):

```
# <tarefa> (<data>)
Pedido: <uma frase>
Feito: <3 a 6 linhas>
Arquivos principais: <lista>
Testes: <quais rodaram e o resultado em números>
Decisões tomadas (já copiadas para DECISOES.md): <lista ou "nenhuma">
Ficou pendente: <lista>
O dono precisa: <testar X no Play / decidir Y / nada>
```

**O que a conversa de governança precisa ler (e o que não precisa):**

| Lê sempre | Lê quando o assunto pede | Não lê |
| --- | --- | --- |
| `ESTADO.md` (8 KB) | O `Docs/Sistemas/` do sistema em discussão | `Docs/Historico/` |
| Os relatórios novos desde a última conversa | O plano em papel em aprovação | O código |
| `DECISOES.md` (as últimas linhas) | O GDD, só ao mudar escopo ou história | Roteiros de teste antigos |

**O que fica no Claude Doc (se o dono quiser manter):** só o que não é decisão (rascunhos de ideia, conversa). Antes de virar trabalho, vira linha no `DECISOES.md` e, se for design, vira texto no GDD do repositório.

---

## 4. Narrativa e originalidade

### 4.1 Como a cópia aconteceu

- A auditoria que mediu "cerca de 70% de Tomb Raider" **não está no repositório** (procurei "Tomb Raider" e "Lara" em todos os `.md` e `.txt`; só aparece a pendência da roupa da Elena). O número é do dono e não pude conferi-lo.
- O GDD e a Bíblia entraram no Git **7 minutos depois do projeto** (`ce6929a` 22:11, `4f415e1` 22:18 de 19/10). A Região 1 com os 15 passos veio 41 minutos depois (`fc13c1e`, 22:59) e foi "aprovada em 19 de outubro" (`Regiao-1`, primeira linha). Inferência: o texto chegou pronto de uma conversa anterior e foi aprovado sem revisão crítica.
- Nenhum documento tem uma etapa de "comparar com jogos parecidos". O GDD cita só Resident Evil 4 (câmera) e o Cemitério Real de Ur (inspiração). O plano da espingarda cita RE4 Remake e TLOU II, como referência de mecânica.
- A semelhança da Elena com a Lara Croft foi notada como pendência ("rabo de cavalo, regata, coldre na coxa") e empurrada para "antes da página Em breve" (`Creditos-e-Licencas.md`, `Biblia.md` "Perguntas abertas", `ESTADO L777`). O sinal existia; não havia regra para parar.
- **Inferência sobre a causa:** quando se pede a uma IA "arqueóloga, ruínas, deuses antigos, ação em terceira pessoa", ela tende a convergir para o exemplo mais famoso do gênero. Sem uma checagem contra esse exemplo, cada escolha "natural" puxa para ele.
- O custo de descobrir tarde: a decisão foi congelar o projeto inteiro (dono), depois de 15 passos, 2 secundárias e 14 personagens no jogo.

### 4.2 Batidas parecidas com a série Tomb Raider (lista para evitar)

A comparação abaixo é minha (inferência), feita entre a Bíblia, o GDD e a Região 1 e o que conheço de Tomb Raider (2013), Rise of the Tomb Raider e Shadow of the Tomb Raider. A auditoria do dono pode ter uma lista diferente.

| Batida no GENESIS CODE | Parecida com | Fonte no projeto |
| --- | --- | --- |
| Arqueóloga jovem, no começo da carreira, cética | Lara Croft jovem (2013) | `Biblia.md`, "Elena Varga" |
| Visual: regata, calça cargo, coldre na coxa, luvas sem dedos, rabo de cavalo | Visual clássico da Lara | `Biblia.md` L76; `Creditos` "Pendência" |
| A expedição é dizimada e ela acorda sozinha, sem saber como sobreviveu | Abertura de 2013 (naufrágio, Lara acorda sozinha) | `GDD.md`, "Premissa"; passo 1 |
| Pai ou mãe arqueólogo(a) desaparecido(a), cuja pesquisa ela segue; gravações deixadas por ele(a) | Richard Croft e os registros dele (Rise); a mãe em Shadow | `Biblia.md`, "Irena Varga"; fitas de Irena |
| Organização paramilitar milenar, disfarçada, que quer o poder antigo e quer a protagonista | Trinity (Rise e Shadow) | `GDD.md`, "Fundação Enlil" |
| Ordem nativa de guardiões que protege o segredo, com um guia local que sabe mais do que diz | Os Remanescentes e Jacob (Rise) | `Biblia.md`, "Os Guardiões"; Samir |
| Uma entidade antiga quer tomar o corpo de uma mulher | Himiko e Sam (2013) | `Biblia.md`, "A grande virada" (Enki quer o corpo da Elena) |
| Ela toca um artefato e desencadeia a catástrofe | A adaga e o cataclismo (Shadow) | `Biblia.md`, "Mistérios" (ela tocou o ME e a equipe morreu) |
| Cidade escondida sob a terra, construída por quem veio antes | Kitezh (Rise), Paititi (Shadow) | `GDD.md`, "Kur, a Cidade Abaixo" |
| Membro da equipe que filma ou fotografa tudo | Sam filmando o documentário (2013) | Tom Becker, fotógrafo |
| Soldados executam um sobrevivente; ela vê de longe; primeira arma tirada de um morto | Sequências de 2013 | Passos 4 e 5 |
| Documentos colecionáveis que "humanizam os inimigos" | Documentos de Tomb Raider | `Biblia.md`, "Como a história é contada" |
| Tumbas opcionais com mecanismos; altares como ponto de salvamento e de melhoria; árvore de 3 ramos; crafting de munição | Tumbas de desafio, acampamentos, árvore de habilidades e crafting da trilogia | `GDD.md`, "Exploração e segredos", "Sistemas principais" |

### 4.3 Batidas proibidas no próximo jogo

Valem para qualquer gênero. Se o jogo novo precisar de uma delas, ela entra só com uma torção registrada no `DECISOES.md`.
- Protagonista arqueólogo(a) jovem em expedição.
- Pai ou mãe desaparecido(a) como motivação principal.
- "Acordei sozinho(a), a equipe morreu, não lembro de nada."
- Culto ou organização secreta milenar que quer acordar ou usar um deus antigo.
- Entidade que fala com o protagonista e quer o corpo dele.
- Ordem de guardiões nativos que protege o segredo.
- Cidade perdida embaixo da terra.
- Roupa de aventureira: regata, cargo, coldre na coxa, rabo de cavalo.
- Documentos espalhados como principal forma de contar a história.
- Execução vista de longe como cena de "o resgate era falso".

### 4.4 Checklist de originalidade (rodar no papel, antes de qualquer código)

Rode na premissa, de novo nos personagens e de novo no roteiro da primeira demo. Cada "sim" na parte A exige uma mudança ou uma justificativa escrita.

**A. Comparação com o que já existe**
1. Liste os 5 jogos (e 3 filmes ou séries) mais parecidos com a premissa. Peça à IA a lista e acrescente os seus.
2. Para cada um, faça uma tabela batida por batida (como a da seção 4.2). Mais de 3 batidas iguais a um mesmo jogo é sinal vermelho.
3. Descreva a protagonista em 3 palavras de visual. Uma busca de imagem com essas palavras mostra um personagem famoso? Se sim, troque.
4. Desenhe a silhueta da protagonista em preto. Alguém confunde com um personagem conhecido?
5. A frase de venda ("um jogo em que...") lembra a frase de venda de um jogo existente?
6. Peça a uma IA, numa conversa nova e sem contexto: "Este roteiro copia algum jogo? Seja duro. Liste as semelhanças." Peça o mesmo a uma segunda IA.

**B. De onde vem a ideia**
7. Qual parte da história só você poderia contar (seu lugar, sua profissão, sua família, sua cultura)? Se a resposta for "nenhuma", a premissa ainda é genérica.
8. Peça à IA 20 premissas, descarte as 5 primeiras (as mais óbvias) e trabalhe a partir das outras.
9. A virada principal surpreende alguém que conhece o gênero? Teste contando para 2 pessoas e pergunte o que elas acham que acontece depois.

**C. Nomes e marcas**
10. Busque o nome do jogo, dos personagens e das facções na Steam, no Google e no registro de marcas (INPI e USPTO). Nome já usado em jogo é troca obrigatória.

**D. Registro**
11. O resultado do checklist fica em `Docs/Originalidade.md`, com a data, e o GDD só é aprovado depois dele.

---

## 5. Escopo

### 5.1 O que foi planejado e o que ficou pronto

| Item | Planejado | Pronto em 4/11 | Fonte |
| --- | --- | --- | --- |
| Jogo inteiro | 25 a 40 h, 6 regiões, refúgio, 3 facções, 5 finais e 1 secreto, NG+, crafting, primeira pessoa como opção, árvore de habilidades, loja | Só a região 1, em blockout | `GDD.md` |
| Duração da fatia | ~60 min (35 a 40 só na principal) | Não medida (a "passada completa cronometrada" é tarefa da fase B, `ESTADO L1021`) | `Regiao-1`, topo |
| Áreas da região 1 | 6 | 6, em blockout; só a vitrine da Aldeia com arte final | `ESTADO L100-131`, `L1370-1404` |
| Passos da missão principal | 15 | 15, aprovados pelo dono no Play | `ESTADO L131` |
| Missões secundárias | 2 | 2, aprovadas | `ESTADO L127-128` |
| Tipos de inimigo | 6 (soldado, líder, atirador, helicóptero, Lullu, chefe) | 6, com equilíbrio pendente | `ESTADO L1065-1068` |
| Armas | 3 (faca, pistola, espingarda) | 3; a espingarda sem animação própria | `ESTADO L1480` |
| Poderes | 3 (Eco, Sopro, Égide) | 1 com efeito (Eco); Sopro e Égide só como dados | `ESTADO L1182` |
| Puzzles dos selos | 2 | 0 (ME2 não começou) | `ESTADO L53` |
| Despertar e árvore | 1 despertar, árvore bloqueada | Só o lugar onde moram no save | `ESTADO L1192` |
| Chefe | 1, em 3 fases | Pronto, com o modelo definitivo; equilíbrio pendente | `ESTADO L278-299`, `L1068` |
| Facções | 3 barras (2 usadas na região 1) | Prontas | `ESTADO L634` |
| Documentos, inscrições e fita | 5 + 3 + 1 | 5 + 3 + 1; a fita sem voz e com texto provisório | `ESTADO L386-400` |
| Memória jogável | 1 curta | 1, em formas simples | `ESTADO L217-226` |
| Personagens com modelo | (não listado) | 14 no jogo, mais a Ninsar em prévia; faltam Tom e Abu Faris | `ESTADO L664-681` |
| Objetos 3D da região | (lista própria) | 49 menções "no jogo" contra 76 "pendente" na lista | `LISTA_OBJETOS_3D.md` |
| Som e voz | Ambiente, música, vozes | Sons sintetizados por script; nenhuma voz; nenhuma música | `Creditos`, "Som" |
| Localização | pt-BR e en | Parte 1 (dados) pronta; textos do código e da cena ainda fixos; en com 27 chaves | `ESTADO L1055` |
| Save | Na fatia, "não precisa" (só checkpoints) | 3 slots com tela, mouse, proteção e trava | `Regiao-1`, Grupo 2; `ESTADO L430` |

**Para fechar só a fase A (blockout completo) ainda faltavam:** ME2 a ME8, o rádio de campanha (B4) e a localização parte 2 (`ESTADO L1530`). Depois viriam as fases B (teste completo), C (arte), D (som e polimento) e E (build e trailer), nenhuma começada (`ESTADO L7-17`).

### 5.2 O custo de mudar conteúdo depois de construído

| Mudança | O que ela custou | Fonte |
| --- | --- | --- |
| Save: "não precisa" virou 3 slots | Etapas A4a, A4b e A4c, um dia de commits (26/10, `fbdce10` a `0b2d95a`), mais a proteção dos slots (`1f4b9c2`) | `ESTADO L430` |
| Localização depois de 52 diálogos prontos | Extração de 576 campos, 74 códigos novos, scripts de conteúdo reescritos sem texto; a parte 2 nunca foi feita | `ESTADO L1056-1059`; `Auditoria` "Localização" |
| Corpos da equipe: 7 corpos, depois 4 lugares | Dois commits de montagem na mesma noite (`c897ad8`, `c28fb5d`), prefabs e materiais apagados, falas trocadas | `ESTADO L1266-1298` |
| Três tabuletas no lugar de uma | Conflito de regras entre documentos, resolvido na governança | `ESTADO L62` |
| Nicho da Aldeia trocado pelo nicho do túnel | "Falta refletir isso no Regiao-1" (o documento ficou divergente) | `ESTADO L350` |
| Idade da Elena (26 → 27 → 26) | Dois commits na Bíblia e no GDD | `74c5525`, `958c96e` |
| Elena trocada por um segundo modelo | Novo modelo, novos limites de braço, locomoção feminina, comparação de 2.395 valores do prefab | `ESTADO L756-783` |
| Qualquer mudança no blockout | Rodar de novo, na ordem certa, até 6 menus de conteúdo e 3 de arte | `ESTADO L426`, `L1098`, `L1143` |
| A narrativa inteira | O projeto foi congelado | (dono) |

**Lição:** cada sistema e cada personagem que entra cedo encarece toda mudança de conteúdo depois. Conteúdo deve ser decidido (e checado contra originalidade) antes de ser construído; sistemas devem ser genéricos o bastante para mudar conteúdo sem mexer neles (isso funcionou: os ScriptableObjects e os scripts de conteúdo absorveram bem as mudanças de texto).

### 5.3 Como dimensionar a primeira demo e o primeiro jogo

Sem datas (regra do dono). O tamanho é medido em unidades de produção, não em calendário.

**O que os números deste projeto mostram:**
- O código de sistema saiu muito rápido: 14 sistemas em ~2 h de commits; 15 passos em blockout em 3 dias com commit (passos 1 a 6 em 20/10, 7 e 8 em 21/10, 9 a 15 em 25/10).
- O gargalo foi tudo o que é visual e humano: modelos, animação, pose, olhos, mãos, escala.
- A estimativa da auditoria para a localização era "cerca de 2 semanas (GG)" (`Auditoria`, "Localização"); a parte 1 saiu em 6 commits num intervalo de 24 minutos (`b29199e` 23:56 a `cc528dd` 00:20). Estimativas de esforço com IA erram muito para os dois lados (inferência: os commits saem em lote, então o trabalho real foi maior que 24 min, mas muito menor que 2 semanas).

**Regras para dimensionar (recomendação):**

| Unidade | Primeira demo | Primeiro jogo lançado |
| --- | --- | --- |
| Personagens humanos animados de perto | 0 ou 1 | 1 a 3 |
| Criaturas ou inimigos não humanos | 1 a 2 tipos | 3 a 6 tipos |
| Áreas | 1 | 3 a 5, reaproveitando kits |
| Sistemas | só os do laço principal | os da demo + 2 ou 3 |
| Escolhas com consequência | 0 | poucas, sem finais múltiplos |
| Duração | 10 a 20 min | 2 a 4 h |
| Falas | texto curto; sem voz ou só uma voz | o que o dono conseguir gravar ou pagar |
| Idiomas | pt-BR e en desde o primeiro texto | os mesmos |

- **Teste do "é jogável e divertido?" antes de crescer:** a demo só ganha conteúdo novo depois que 3 pessoas de fora jogarem o laço principal e voltarem a jogar sem ser pedidas.
- **Corte primeiro o que precisa de animação nova.** Uma mecânica que pede 10 clipes novos custa mais que 10 mecânicas que reaproveitam os clipes existentes (inferência pelos números da seção 2.3).
- **Guarde 30% do esforço para o que ninguém planeja** (equilíbrio, bugs, página da Steam, build). Este projeto não chegou a nenhuma dessas etapas.

---

## 6. Pipeline de arte 3D com IA, problema por problema

**O caminho usado:** imagem de referência no ChatGPT → modelo no Meshy (personagens) ou no Tripo (objetos) → esqueleto no AccuRig (101 ossos, 30 de dedo) → importação Humanoid na Unity por script → animações do Mixamo com o avatar do X Bot (`ESTADO L660-699`, `Creditos-e-Licencas.md`). As "soluções corretas" abaixo são prática conhecida da indústria (conhecimento geral); nenhuma foi testada neste projeto, a não ser quando a linha diz.

### 6.1 Olhos estranhos na Elena

| | |
| --- | --- |
| **O que aconteceu** | O dono achou os olhos da Elena estranhos (dono). **O problema não está registrado no repositório.** O único dado é que o modelo entrou "sem blendshapes" (`ESTADO L786`) e com "só cor" de textura, sem normal nem rugosidade (`ESTADO L764`) |
| **Causa provável** (inferência) | O Meshy gera uma malha única, com os olhos pintados na textura da cabeça. Não há globo ocular separado, nem pálpebra que fecha, nem osso de olho. O olho não pisca, não gira, não brilha (sem reflexo na córnea) e parece "pintado". De perto, numa câmera sobre o ombro, isso cai no vale da estranheza |
| **O que foi tentado** | Nada registrado |
| **O que deveria ter sido feito desde o início** | (1) **Olhos como malhas separadas**: duas esferas com íris e córnea (material com brilho próprio), presas a ossos `LeftEye` e `RightEye`, que o Humanoid da Unity já aceita no mapeamento; o olhar se controla com `Animator.SetLookAtPosition`. (2) **Pálpebras e expressões com blend shapes** (pelo menos piscar e alguns visemas). Isso não sai do Meshy: precisa de um modelo-base com topologia de rosto (por exemplo, o MPFB/MakeHuman, cujos modelos exportados têm licença livre, a conferir antes de usar; ou um artista), ou de esculpir as formas no Blender. (3) **Script de piscar** (intervalo aleatório). (4) Se nada disso couber, **evite o rosto**: câmera mais longe, personagem de capacete, máscara, capuz ou estilo não realista |

### 6.2 Rig e AccuRig

| | |
| --- | --- |
| **O que aconteceu** | Todos os personagens vieram com 101 ossos e boa pele. Problemas: falanges sem peso em 4 modelos (Weiss 4, Soldado 1, Jovem 2, Servo 4); poses A diferentes (Weiss 51°, Lullu 41°, Elena 61°); no Blender, o esqueleto novo da Elena aparecia girado 90° e a malha torta, mas na Unity estava certo; aviso "Rig Error / Avatar Rig Configuration mis-match" no Servo; dois ossos do polegar mapeados à mão (fonte: `ESTADO L662`, `L671-680`, `L687`, `L766`, `L771`, `L879`, `L769`) |
| **Causa provável** | Rig automático: acerta o grosso e erra os detalhes (dedos). A pose de repouso varia por modelo porque vem da pose em que o Meshy gerou o corpo |
| **O que foi tentado** | `FingerWeightFix` (corrige os pesos das falanges na importação), mapeamento manual dos ossos, regra de conferir tudo na Unity e não no Blender. **Funcionou** |
| **O que deveria ter sido feito desde o início** | O que foi feito, mais: gerar o modelo **já em pose T ou A padronizada** (pedir isso na geração), e uma conferência automática na importação (todos os ossos com peso, todos os ossos humanoides mapeados, pose de repouso dentro de uma faixa) |

### 6.3 Mixamo e retarget

| | |
| --- | --- |
| **O que aconteceu** | A corrida ficava girada 40° porque o avatar foi feito da própria animação sem pele (que começa no primeiro quadro, não na pose T). Animações que andam na diagonal. Fuzil atravessado 70° para a esquerda. Nomes que enganam ("Walking" é com fuzil). Animator parado em pose T no editor sem câmera olhando. Recarga de pistola é um gesto de fuzil; não há tiro nem recarga de pistola (fonte: `ESTADO L719-724`, `L808`, `L813`) |
| **Causa provável** | Configuração de importação do Humanoid. O Mixamo entrega clipes feitos para o X Bot, com proporções diferentes |
| **O que foi tentado** | "Copy From Other Avatar" do X Bot; giro da raiz por "Body Orientation" na locomoção e "Original" em golpes e no fuzil; "Always Animate" para medir no editor; `AnimationLibrary` (um `AssetPostprocessor` que importa sempre igual) e um teste com 377 verificações. **Funcionou** |
| **O que deveria ter sido feito desde o início** | Exatamente isso, desde o primeiro clipe, mais: **ver cada clipe no X Bot antes de importar** e, para cada ação do jogo, escolher um pacote coerente (mesma "família" de animação para andar, correr, mirar e recarregar com a mesma arma) |

### 6.4 Locomoção: andar, correr e pés deslizando

| | |
| --- | --- |
| **O que aconteceu** | O dono achou o andar e a corrida estranhos (dono). Medido: a 6 m/s o pé de apoio deveria recuar a 6 m/s e recua 5,7 (escorregão médio de 0,7 m/s), com a corrida feminina tocada 1,67 vez mais rápido que o natural (3,6 m/s). Andar de costas mirando é o andar tocado ao contrário. A pose parada ficou "masculinizada" e "de manequim" até o 5º ajuste de braço; braços cruzados atravessavam o busto. Agachar do UAL fundo demais (pés afundando 5 a 19 cm). O Lullu de pernas longas virava "rampa" no andar agachado (fonte: `ESTADO L772`, `L803`, `L813`, `L773-775`, `L872`) |
| **Causa provável** | (1) **Velocidade do jogo diferente da velocidade do clipe**: a corrida de 6 m/s foi escolhida antes das animações (inferência: a corrida entrou no protótipo, `96b6c12`, antes de qualquer animação), e o clipe natural é de 3,6 m/s; acelerar 1,67× deixa o passo rápido demais e ainda escorrega. (2) **Movimento sem root motion** ("avanço do clipe descartado, quem anda é o `PlayerMotor`", `ESTADO L788`): isso é normal, mas exige casar velocidade e clipe. (3) Proporções da Elena (braços longos, coldre fundido na malha) diferentes do X Bot. (4) Uma só animação parada feminina na biblioteca |
| **O que foi tentado** | Comparar o pé de apoio de lado num chão marcado; testar 1,51× e 1,67×; limites "Arm Down-Up" do Avatar em 5 combinações; camada de variação da parada (vazia); correção de altura pelos pés no Lullu (ergue o modelo até 25 cm) |
| **O que deveria ter sido feito desde o início** | (1) **Escolher as velocidades do jogo a partir das animações**, não o contrário: medir a velocidade natural de cada clipe e usar esses valores nos limiares da blend tree e no `PlayerMotor` (variar ±10%, nunca 67%). (2) **Blend tree 2D (direcional)** com clipes de andar e correr para frente, lados e trás (o Rifle 8-Way tinha isso, `ESTADO L1504`). (3) **Foot IK**: ligar "Foot IK" nos estados do Humanoid e, para terreno irregular, IK de pé por raycast (com o pacote Animation Rigging ou um script). (4) Avaliar **root motion** só para ações curtas (rolar, golpe). (5) Personagem com proporções próximas do X Bot ou animações feitas para o personagem |

### 6.5 IK das armas: braço esticando

| | |
| --- | --- |
| **O que aconteceu** | A espingarda usava a pose de mira da pistola; o cano atravessava o antebraço e a mão esquerda ficava solta. O plano previa `OnAnimatorIK` com folga de 2 cm. Na implementação, o IK do Animator foi descartado (rodava antes do ajuste do tronco e a mão ficava a 25 cm) e entrou um cálculo de dois ossos no `LateUpdate`. Mesmo assim a mão esquerda fica a ~20 cm do ponto, "porque o apoio está no limite do alcance do braço (o braço estica para ele)". O teste aceita 25 cm na espingarda e 12 cm na pistola. A mão esquerda fecha em punho. O Animation Rigging estava previsto para "decidir na B3a" e nunca foi usado (fonte: `Plano-B6-Empunhadura`, "Como está hoje" e item 3; `ESTADO L1497`, `L1501`, `L1504`; `ESTADO L1018`) |
| **Causa provável** (inferência) | (1) O ponto de apoio estava fora do alcance do braço: o IK estica o braço até o limite e ainda não chega. (2) A pose de base não era de arma longa; o IK tentava corrigir uma pose muito errada (o próprio plano dizia "o IK não conserta uma pose muito errada"). (3) A ordem de execução: o tronco era girado no `LateUpdate` depois do IK do Animator. (4) A tolerância do teste foi afrouxada até passar (2 cm no plano, 25 cm no teste) |
| **O que foi tentado** | Clipes de arma longa do Basic Shooter Pack, pontos `Grip_Direita` e `Grip_Esquerda` por arma, giro do pulso, câmera livre para conferir |
| **O que deveria ter sido feito desde o início** | (1) **A animação de base tem de ser da mesma família de arma** (mira, andar mirando, recarga e tiro de espingarda), antes de qualquer IK. (2) **Posicionar a arma pela mão direita e mover o ponto de apoio para dentro do alcance** do braço esquerdo naquela pose (ou encurtar o modelo da arma), não o contrário. (3) Usar o **Animation Rigging** (`TwoBoneIKConstraint` com dica de cotovelo, `MultiAimConstraint` para o tronco), que roda na ordem certa junto com a animação. (4) **Pose de mão** (dedos fechados no guarda-mão) como camada de máscara de mão. (5) **Não afrouxar o teste para passar**: se a mão não chega a 2 cm, o defeito é da pose ou do ponto, e o teste tem de continuar falhando |

### 6.6 Escala dos modelos

| | |
| --- | --- |
| **O que aconteceu** | O Meshy entregou todos os personagens com ~0,95 m. Altar do Tripo baixo (1,00 × 0,57 × 0,78) para uma caixa de 1 m de altura: o corpo foi esticado 3,2 vezes. Lamparina de 15 cm, pequena demais no Play (passou a 22). Helicóptero pequeno: a cabeça do piloto sai pelo teto. Curativo e caixa de munição pequenos demais para ver de longe. Rolo de filme em escala real (6 cm) mais difícil de achar que o cilindro de 12 cm do blockout. Mixamo guarda sentado e deitado na altura do quadril em pé (Yusuf flutuando a 0,78 m); a Elena sentada nascia com os pés 1 m enterrados (fonte: `ESTADO L662`, `L1396`, `L1508`, `L1510`, `L1154`, `L1356`, `L742`) |
| **Causa provável** | A IA não sabe a escala. A escala "real" nem sempre é a escala de jogo (itens precisam ser lidos de longe) |
| **O que foi tentado** | `CharacterHeights.FitImportScale` (tabela de alturas e escala de importação por personagem), script do Blender com escala real e pivô no pé, colisor refeito sobre a silhueta. **Funcionou para personagens**; objetos e veículos ficaram por conferência a olho |
| **O que deveria ter sido feito desde o início** | Uma **tabela de medidas de jogo** para tudo (personagens, portas, veículos, itens) e uma **cena-régua** com um boneco de 1,70 m, uma porta-padrão e um item-padrão, onde todo modelo novo é posto ao lado antes de entrar na cena. Itens coletáveis com escala de jogo (maiores que o real) e brilho de destaque |

### 6.7 Texturas

| | |
| --- | --- |
| **O que aconteceu** | Elena nova só com textura de cor (8192 no arquivo, 4096 no jogo), sem normal nem rugosidade. Tripo com textura de cor 4096 e sem normal. FBX do AccuRig com texturas 4K embutidas (25 a 30 MB cada): 6 personagens somaram ~220 MB em `Assets`, bem acima dos 80 MB previstos; o LFS passou de 1,5 GB de histórico (fonte: `ESTADO L764`, `L1394`, `L1356`, `L981`) |
| **O que foi tentado** | Cópias em 2K em `Assets` (originais 4K fora), máscaras do URP montadas por script, calibração de tom pela cor média (fonte: `ESTADO L1360`, `L1383`) |
| **O que deveria ter sido feito desde o início** | Exportar do AccuRig **sem texturas embutidas**; padrão de 2K para personagens e 1K para itens desde o primeiro modelo; gerar ou pintar normal e rugosidade quando a IA não entrega; conferir o tamanho no LFS antes de subir |

### 6.8 Malhas com defeito, roupa e tecido

| | |
| --- | --- |
| **O que aconteceu** | Poço do Tripo com dois buracos grandes (dava para ver o céu pelo fundo); altar com 32 furinhos; lamparina com 6; tabuleta e relevo do deus com falhas na malha. Manto da Ninsar: 3 tentativas por script falharam (casca de 2 mm; "saco reto"; poncho). Saia rasgando nas costas na animação. Coldre fundido na malha da Elena (não dá para tirar). Samir com a adaga do cinto fundida (aparecem duas facas no golpe). Túnica do Servo rígida (fonte: `ESTADO L1394`, `L1070`, `L964-968`, `L975`, `L776`, `L849`, `L1519`) |
| **Causa provável** | A IA gera uma casca única, sem separar roupa, acessório e corpo, e sem espessura nem topologia para simular tecido |
| **O que deveria ter sido feito desde o início** | Pedir à IA o **corpo sem acessórios** e gerar acessórios (arma, coldre, adaga, bolsa) como **objetos separados**, presos a ossos. Evitar roupas longas e soltas no personagem principal. Para tecido: Marvelous Designer, um artista ou roupa justa. Fechar buracos por script na importação (já foi feito) e checar a malha por script (bordas abertas, faces invertidas) |

### 6.9 Meshy x Tripo

| | Meshy | Tripo |
| --- | --- | --- |
| Uso decidido | Personagens (`ESTADO L40`) | Objetos e cenário (`ESTADO L40`, decisão de 1/11) |
| O que entregou | 14 personagens no jogo, Ninsar em prévia, 6 objetos (gravador, rolo de filme, curativo, munição, antibióticos, holofote), 19 animações da Ninsar (sem dedos) | 17 peças (altar, poço, lamparina, espingarda, fuzil, pistola, faca, tabuleta, rádio, Selo, helicóptero, 3 relevos, caminhonete, caminhão, guincho) |
| Problemas | Escala ~0,95 m; olhos pintados; acessórios fundidos; texturas embutidas pesadas; manto impossível de refazer | Buracos na malha; escala errada (altar); textura sem normal; vidros escuros nos veículos (não se vê o Samir dirigindo) |
| Plano | Pro, pago, "créditos limitados até 30/11" | Pro, pago |
| Custo em dinheiro | **Não registrado** | **Não registrado** |

Fontes: `Arte/Fontes-da-Arte.md`, `Creditos-e-Licencas.md`, `ESTADO L1242`, `L1378`.

### 6.10 Scripts do Blender

| | |
| --- | --- |
| **Funcionou** | Kit de casas de barro modular (`kit_casas_barro.py`), tratamento de objetos do Tripo (`objetos_jogo.py`, `objetos_vitrine.py`: frente +z, escala real, pivô no pé, orçamento de triângulos, texturas reduzidas). Blender 5.2 sem janela (`blender -b --factory-startup -P`) |
| **Armadilhas registradas** | FBX chega na Unity com os eixos (x, z, y) do Blender; FBX de peça única chega com o nome do arquivo e o material sai do nome da malha; esqueleto parece errado no Blender e está certo na Unity (fonte: `ESTADO L1383-1384`, `L771`; memória `blender-fbx-to-unity`) |
| **Não funcionou** | Manto da Ninsar (simulação de tecido por script) (fonte: `ESTADO L964`) |
| **Lição** | Script do Blender é ótimo para geometria regular (arquitetura, kits) e para tratar modelos; é ruim para formas orgânicas e tecido |

### 6.11 Pastas e `.gitignore`

| Problema | Fonte |
| --- | --- |
| Pasta `1_Meshy` usada também para os brutos do Tripo (nome errado herdado) | `ESTADO L42` |
| 166 renomeações para mover a arte-fonte para `ArteFonte/Personagens/<Nome>` | `ESTADO L1138` |
| Nomes com acento, ç, espaço e maiúscula errada (`PoçoM9`, `Dra. Weiss`, `Umm Karim`, `selodekur`, `TabeluteDeJunco`, `2_RIG`) | `ESTADO L1138`, `L1352`, `L1393` |
| No Windows (`core.ignorecase=true`), trocar só maiúsculas não aparece no Git: foi preciso `git rm --cached` | `ESTADO L1352` |
| Pacote de animação duplicado (Gestures Pack Basic 2) | `ESTADO L730` |
| Biblioteca de animações entrou no LFS e depois saiu (fica no histórico) | `ESTADO L982` |
| Origem das imagens de referência e das texturas do manto ainda "a confirmar" | `Creditos-e-Licencas.md` |

**Recomendação:** no dia 1, criar a árvore `ArteFonte/<Tipo>/<Nome>/{Referencias, Bruto_<Ferramenta>, Rig, Blender}` com nomes em ASCII sem espaço, o `.gitignore` dos brutos e uma conferência (menu ou hook) que recusa nome com acento ou espaço.

### 6.12 Custo gasto em modelos

**Não está registrado.** O repositório diz só que Meshy e Tripo foram usados no plano Pro, pago (`Creditos-e-Licencas.md`), que o ChatGPT foi usado em plano pago (`Creditos`, "Elena nova: imagens de referência") e que os créditos do Meshy e do Tripo vencem em 30/11 (`Fontes-da-Arte.md`). A única tentativa de medir esforço ("1 dia de trabalho com o Claude ≈ 3 a 4 h do dono") foi retirada do ESTADO em 3/11 (`6397a71`). **Para o próximo jogo:** uma planilha simples com cada assinatura, crédito comprado e o que ele gerou.

---

## 7. Arquitetura Unity e código

### 7.1 O que existe (medido em 5/11)

| Medida | Valor |
| --- | --- |
| Scripts | 261 arquivos `.cs`, 44.815 linhas |
| Código do jogo (runtime) | ~16.470 linhas em 20 pastas |
| Ferramentas de editor | 97 arquivos, 28.347 linhas (63% do total) |
| Dentro do editor: testes | ~11.080 linhas (`*Tests.cs`, `*Check.cs`, `TestFlow.cs`) |
| Dentro do editor: atalhos de teste | ~3.450 linhas |
| Dentro do editor: montagem de cenas (blockout e conteúdo) | ~6.970 linhas |
| Dentro do editor: personagens, animações, arte e som | ~6.380 linhas |
| Itens de menu (`MenuItem`) | 258 |
| Assembly definitions (`.asmdef`) | 0 |
| Testes NUnit ou Unity Test Framework | 0 (o pacote `com.unity.test-framework` 1.6.0 está instalado) |
| Cinemachine | Não usado (câmera própria) |
| Pacotes do modelo de projeto sem uso aparente | `visualscripting`, `multiplayer.center`, `collab-proxy` (inferência) |

**Dependências entre pastas (pelos `using Nibiru.*`):** Save usa Combat, Environment, Interaction, Inventory, Localization, Memories, Powers, Quests e UI. Quests usa UI. Inventory usa Dialogue, e Dialogue usa Inventory. UI usa quase tudo. Só a Localização é isolada. **Inferência:** tudo compila num assembly só, então os ciclos passaram despercebidos; para extrair um kit, será preciso cortar esses laços com interfaces ou eventos.

### 7.2 Módulos reaproveitáveis

Nota de reaproveitamento: 5 = copiar quase como está; 1 = só a ideia serve. **Nada foi extraído agora.**

| Módulo | Arquivos | Depende de | O que muda para ficar genérico | Nota |
| --- | --- | --- | --- | --- |
| **Localização com chaves e códigos permanentes** | `Localization/Loc.cs`, `LocKeyAttribute.cs`; `Editor/Localization/*` (7 arquivos: extrator, campos, chaves, desenho no Inspector, tabelas, atalhos, testes); `Resources/Localization/*.json` | Newtonsoft JSON (pacote da Unity) | Tirar a lista de campos específica do Nibiru (`LocFields`), deixar o caminho das tabelas configurável, trocar `Text` por TextMeshPro | **5** |
| **Fonte e tema central** | `UI/UiFont.cs`, `UI/UiTheme.cs` | — | Suporte a TextMeshPro e fontes de outros alfabetos | 4 |
| **Camada única de escrita do save e trava de testes** | `Save/SaveStorage.cs`, `Save/AutomatedRun.cs`, `Editor/Save/SlotGuard*.cs` | `UnityEngine` | Nada de específico: é a peça mais valiosa do salvamento | **5** |
| **Slots, versão, gravação atômica (.tmp), estados vazio/ocupado/ilegível** | `Save/SaveSlots.cs` (670 linhas), `PlayTime*.cs`, `SessionReset.cs` | Inventory, Quests, Powers, Combat e outros (monta o arquivo conhecendo cada sistema) | Trocar a montagem fixa por uma interface `ISaveSection` (cada sistema grava a sua parte); implementar a regra de versão decidida (`ESTADO L1025`) | 3 |
| **Estado do mundo, eventos da história, códigos permanentes** | `Save/WorldState.cs`, `PersistentId.cs`, `SaveRegistry.cs`; `Quests/StoryEvent*.cs`; `Editor/Save/SaveCodes.cs`; `Docs/Codigos-Salvamento.txt` | Quests | Dar código próprio a eventos e facções (hoje é o nome do arquivo, `ESTADO L462`) | 4 |
| **Checkpoints e retorno** | `Save/Checkpoint.cs`, `Checkpoints.cs`, `PlayerRespawn.cs` | Player, Inventory, Combat | Já tem a convenção do pé (`ESTADO L1124`); tirar o que é da Elena | 3 |
| **Mapa de controles e leitor de entrada** | `Data/Input/NibiruControles.inputactions`, `Player/PlayerInputReader.cs` (regra do LB como modificador, planos A e B) | Input System | Separar a regra do modificador num componente próprio; acrescentar tela de remapear (fase D) | 4 |
| **Missões, prazos e falha** | `Quests/QuestDefinition.cs`, `QuestManager.cs`, `DeadlineWatcher.cs` | Save, UI, Inventory, Combat | Tirar o aviso de tela de dentro do gerenciador (evento) | 4 |
| **Diálogos com escolhas e requisitos** | `Dialogue/DialogueDefinition.cs`, `DialogueRunner.cs`, `DialogueStarter.cs`; `UI/DialogueUI.cs` | Factions, Inventory, Quests, Save, Documents, Player | Escolhas e efeitos por interface (reputação, item, evento), em vez de referências diretas | 3 |
| **Documentos, fitas e Arquivo** | `Documents/*` (7 arquivos), `UI/ReadingScreenUI.cs`, `UI/SubtitleUI.cs` | Quests, Save, UI | Pouca coisa; a leitura que reduz a letra até caber já é genérica | 4 |
| **Avisos na tela** | `UI/Notifications.cs`, `NotificationUI.cs`, `NotificationPoster.cs` | — | Já genérico (com a lição do `Instantiate`, `ESTADO L342`) | 4 |
| **Menus (principal, pausa, slots, confirmação, opções)** | `UI/MainMenuUI.cs`, `PauseMenuUI.cs` (618 linhas), `SlotRows.cs`, `ConfirmDialog.cs`, `GameOptions.cs`, `MenuPointer.cs`, `TextLines.cs`, `CursorPolicy.cs` | Save, Localization | O menu é um `Text` só com uma linha por item, e o mouse depende de um hit-test pelo gerador de texto (`ESTADO L564`). Para o próximo jogo, refazer com botões (UI Toolkit ou uGUI) e aproveitar só as regras: foco inicial em "Cancelar", cursor por contexto, clique que não vaza para o jogo, varredura dos `static` no reset | 2 |
| **Câmera com interface** | `Camera/ICameraMode.cs`, `ThirdPersonCameraMode.cs`, `PlayerCameraController.cs`, `Data/Camera` | Player, UI | A conferência final contra paredes e o zoom já são genéricos; avaliar Cinemachine no lugar | 3 |
| **Cenas roteirizadas** | `Sequences/*` (11 componentes: sequência com tempo, mover por pontos, personagem que anda até um ponto, luz que muda, ponto à frente do jogador...) | Vários | Separar os que conhecem a Elena | 3 |
| **Blockout por script** | `Editor/Blockout/SitioDeUrBlockout.cs` (609 linhas) | ContentKit | O conteúdo é do mapa; o padrão (apagar e reconstruir um grupo, refazer a malha de navegação, marcas de conteúdo) é reaproveitável | 3 |
| **Kit de montagem de conteúdo** | `Editor/Content/ContentKit.cs` (489 linhas): peças, gatilhos, itens, soldados, diálogos, passos, `StableId` | Muitos | Separar o que é genérico (gatilho, item, código estável) do que é do Nibiru (soldado, helicóptero). Resolver a **ordem dos menus** com um menu único "Remontar tudo" que conhece a ordem | 3 |
| **Atalhos de teste (Nibiru > Testes)** | `Editor/TestShortcuts.cs` (base: abre a cena, liga o Play, monta o mundo e um checkpoint), `Step*Shortcuts.cs`, `FreeRoamShortcuts.cs` | Save, Player | O padrão inteiro é reaproveitável; o conteúdo de cada atalho não | **4** |
| **Teste geral dos atalhos** | `Editor/ShortcutsCheck.cs` | TestShortcuts | Genérico: roda todos os atalhos registrados e confere vivo, no chão e na malha | **4** |
| **Motor de roteiro dos testes** | `Editor/TestFlow.cs` | AutomatedRun | Migrar para o Unity Test Framework (seção 8) | 3 |
| **Ferramentas de conferência** | `Editor/RoomTrapCheck.cs` (cantos que prendem o jogador), `Editor/Camera/FreeCameraCheck.cs` (câmera livre só no editor), `Editor/BackgroundInputInEditor.cs` | Player | Quase nada | **4** |
| **Montagem de personagens** | `Editor/Personagens/CharacterSetupKit.cs`, `CharacterHeights.cs`, `FingerWeightFix.cs`, `PoseDiagnostic.cs` | Animação | Genérico para AccuRig + Mixamo | **4** |
| **Biblioteca de animações** | `Editor/Animacoes/AnimationLibrary.cs` (postprocessor e tabela), `AnimationTests.cs` | — | Trocar a tabela | 4 |
| **Sons provisórios sintetizados** | `Editor/Audio/ProvisionalSounds.cs` | — | Nada | 4 |
| **Poderes (dados, posse, recarga, vagas)** | `Powers/PowerDefinition.cs`, `PowerBook.cs`, `PlayerPowers.cs`, `PowerBehaviour.cs` | Save, Player, Combat | Serve para qualquer habilidade com recarga | 3 |
| **IA (soldados, atirador, Lullu, chefe)** | `AI/*` (15 arquivos, 2.842 linhas) | Muitos | Muito ligada ao jogo | 2 |
| **Memória jogável** | `Memories/*` | Câmera, Player, UI | Específica | 2 |
| **Ponte MCP e testes no Play pela ponte** | `Docs/Guia-MCP-Unity.md`; memória `unity-play-testing-via-mcp` | Unity CLI | Processo, não código: reaproveita inteiro | 5 |

### 7.3 Como extrair um "kit base" depois (não fazer agora)

1. Criar um pacote local (`Packages/com.barizon.kit`) com assemblies separados: `Kit.Localization`, `Kit.Save`, `Kit.Events`, `Kit.Input`, `Kit.UI`, `Kit.EditorTools`, `Kit.Tests`.
2. Começar pelo que não tem dependência: Localização, `SaveStorage` e `AutomatedRun`, avisos, sons provisórios, ferramentas de conferência.
3. Para cada módulo com dependência, trocar a referência direta por uma interface ou evento (exemplo: `ISaveSection` para o save; `IChoiceEffect` para os diálogos).
4. Escrever os testes do kit no Unity Test Framework, em modo de edição, antes de usar o kit no jogo novo.
5. Copiar o padrão dos atalhos e do teste geral, não o conteúdo.
6. Não levar: menus de texto único, IA, memória jogável, conteúdo das áreas.

### 7.4 Lições de código registradas (para o CLAUDE.md do próximo jogo)

| Lição | Fonte |
| --- | --- |
| Nunca clonar por código um objeto que tem o próprio script que clona (recursão infinita, Unity travada) | `ESTADO L342` |
| Nunca recompilar com o Play ligado; esperar a ponte voltar antes de ligar o Play | `ESTADO L995` |
| Teste com `Time.timeScale = 0` não mede física | `ESTADO L1130` |
| Aparelhos virtuais do Input System ficam entre um Play e outro (sem recarga de domínio): remover no fim | `ESTADO L1502` |
| O Input System entrega a roda como ±1 por clique, não 120 | `ESTADO L1170` |
| O Input System guarda o estado do jogo e o do editor separados: ler teclas no `onAfterUpdate` | `ESTADO L1492` |
| `autoSyncTransforms` desligado: chamar `Physics.SyncTransforms` depois de teleportar | `ESTADO L1125` |
| Métodos chamados por reflexão nos testes quebram com nomes ambíguos | `ESTADO L588` |
| Variantes de prefab gravam controlador vazio se o prefab não for reimportado depois de recriar o controlador | `ESTADO L897` |
| Um setup que reescreve cortes de um FBX compartilhado apaga os cortes de outro personagem | `ESTADO L773`, `edfcab6` |
| Varrer os campos `static` ao voltar ao menu | `ESTADO L522-534` |

---

## 8. Testes e qualidade

### 8.1 O que a regressão completa cobria (última: 4/11, fechamento do ME1)

Fonte: `ESTADO L1438`, `L1442-1461`. Tempos são estimativas do próprio ESTADO ("corrija na próxima regressão"), menos os marcados como medidos.

| Teste | Resultado em 4/11 | Tempo |
| --- | --- | --- |
| Teste geral dos atalhos (Elena viva, no chão e na malha em cada atalho) | 102 OK, 0 FALHOU, 49 pulados (107 e 50 depois do B6) | ~8,5 min (medido) |
| Salvamento: automático, códigos, proteção dos slots | 19 OK; 8 OK; tudo OK | ~3 min; segundos; segundos |
| Passos 10, 11, 12, 13, 14 e 15 | 14, 9, 10, 9, 10 e 3 OK | ~2 a 3 min cada |
| Remédio para Yusuf; Arquivo; O que o Tom viu | 8; 6; 4 OK | ~3 min; ~1 min; ~25 s (medido) |
| Suprimentos (contagem); corpos da equipe | 10; 27 OK | segundos |
| Animações | 377 verificações OK | ~10 s |
| Mouse; zoom; localização | 10; 14; 14 OK | ~1 min; ~2,5 min (medido); segundos |
| Poderes: automático e dados | 18 e 18 OK | ~50 s (medido); segundos |
| Armas (B6) e empunhadura (entraram depois) | 10 e 5 OK | ~8 min; ~1 min |

**Tempo total (inferência):** somando a tabela, ~45 min de testes, rodados um por vez, sem contar o tempo de ligar e desligar o Play e de conferir o log de cada um. O plano do ME1 estimou só o nível 2 em ~20 min (`Plano-ME1` §2f).

**Os 49 a 50 pulados:** testes automáticos, vitrines sem malha, a viagem na caçamba e a memória de Shuruppak (`ESTADO L987`). Listar os pulados e o motivo de cada um ficou no backlog e não foi feito (`ESTADO L1520`).

### 8.2 O que dependia da ponte MCP

- **Todos** os testes com Play eram itens de menu dentro do editor aberto. O Claude Code os disparava pela ponte e lia o resultado no Console (`ESTADO L991-999`).
- Para jogar sozinho, o teste cria um controle, teclado ou mouse virtual do Input System e um script deixa a Unity aceitar comandos em segundo plano (`BackgroundInputInEditor.cs`).
- O dono às vezes jogava ou mexia no mouse ao mesmo tempo, e isso derrubou testes: o mouse do teste de cursor (`ESTADO L1207`), a roda no zoom (`Plano-Zoom` §6), a faca e o RB no B6 (`ESTADO L1503`).
- A trava `AutomatedRun` já reconhece "a Unity sem janela (batch)" (`ESTADO L1414`), mas não há registro de nenhum teste rodado em modo batch (inferência).
- **Consequência:** sem o editor aberto e uma conversa do Claude Code ativa, nada era testado. Não havia teste noturno nem teste a cada commit.

### 8.3 Erros que escaparam dos testes

| Erro | Quem achou | Por que o teste não pegou | Fonte |
| --- | --- | --- | --- |
| Cursor não sumia na pausa; clique nas Opções sempre diminuía | Dono, no Play | O teste calculava o clique com a mesma função do jogo | `ESTADO L586` |
| Zoom não respondia à roda real | Dono, no Play | O teste mandava 120 por clique; a roda real manda 1 | `ESTADO L1170` |
| Câmera livre: só o mouse funcionava; a Elena andava depois de recarregar a cena | Dono, no Play (duas vezes) | O teste usava um caminho de teclado diferente do real | `ESTADO L1492` |
| Porta da casa trancada prendia a Elena | Dono, no Play | Nenhum teste atravessava a porta | `ESTADO L1475` |
| Elena nascia 1 m enterrada em todo retorno | Teste novo de suprimentos, por acaso | Tolerância de 1,5 m na altura | `Investigacao-Respawn-Poco.md` |
| Dois testes ao mesmo tempo gravaram nos slots reais | Teste geral (4 falhas) | A proteção era um aviso; um `static` nulo valia a pasta real | `ESTADO L1408-1411` |
| Braços cruzados atravessando o busto | Dono, no Play | Teste de animação confere dados, não a imagem | `ESTADO L774` |
| Relevos com o fundo dentro da parede; relevos "pouco visíveis" | Dono, no Play | Sem conferência visual | `ESTADO L1250-1251` |
| Helicóptero pequeno para o piloto; Elena sentada atravessando a caminhonete | Dono, no Play | Sem conferência de escala | `ESTADO L1507-1508` |
| Cortes de animação do Samir apagados por um menu da Elena | Claude, depois | Testes rodaram no estado já quebrado | `ESTADO L773`, `edfcab6` |
| Contador da pistola "0 / 0" num teste | Visto, não investigado | — | `ESTADO L943` |
| Teste de corpos com 3 falhas falsas | Claude | Rodado com o Play ainda ligado | `ESTADO L1438` |
| Testes que falharam uma vez e passaram na repetição | — | Entrada concorrente do dono e aparelho virtual que sobrava | `ESTADO L1503` |

**Padrões:** (1) teste que usa a mesma fórmula do código testado; (2) valor de entrada irreal; (3) tolerância folgada; (4) nada confere a imagem; (5) testes que dependem da máquina estar parada.

### 8.4 Plano para automatizar no próximo jogo

1. **Unity Test Framework desde o dia 1.** Testes de modo de edição para lógica pura (save e leitura, códigos, chaves de idioma, regras de dano, regras de entrada); testes de modo Play para fluxo (passos da missão, retorno, menus). Entrada simulada com o `InputTestFixture` do Input System, que cria e remove os aparelhos sozinho (evita o problema de `ESTADO L1502`).
2. **Rodar sem o editor do dono:** num **segundo clone** do repositório (a Unity não abre o mesmo projeto duas vezes), em modo batch: `Unity.exe -batchmode -projectPath <clone> -runTests -testPlatform PlayMode -testResults <arquivo>.xml`. O resultado vira um resumo curto num arquivo de texto.
3. **Regressão noturna:** uma tarefa agendada do Windows que atualiza o clone, roda os testes de edição e de Play, faz uma build de Windows (pega erro que só aparece na build) e grava o resumo. De manhã, o Claude Code lê só o resumo.
4. **Bot de passada:** um teste de Play que joga o caminho crítico sozinho (anda pela malha de navegação até cada objetivo, interage, escolhe a primeira resposta de cada diálogo) e mede o tempo de cada passo. Dá a "passada completa cronometrada" que a fase B pedia (`ESTADO L1021`) e acha travamentos de progressão.
5. **Conferência de imagem:** capturas fixas de cada área (câmera e hora fixas) comparadas com as da noite anterior; só avisa diferença grande, e quem decide é o dono.
6. **Regras de teste no CLAUDE.md:** valor de entrada real (medido no aparelho), resultado calculado de forma independente do código, tolerância justificada por escrito e nunca afrouxada só para passar.
7. **Manter o que funcionou:** atalhos por momento do jogo, teste geral dos atalhos, trava de execução e camada única de escrita do save.

---

## 9. Ferramentas de IA

### 9.1 O que o Claude Code fez bem

| O quê | Evidência |
| --- | --- |
| Velocidade em sistemas: protótipo e 14 sistemas em ~2 h de commits | `6594403` (19/10 22:31) a `e75ff3a` (20/10 05:42) |
| Ferramentas de editor para quem não programa: 258 itens de menu, montagem de cena por script, atalhos de teste, vitrines, câmera livre | Seção 7.1 |
| Testes que acharam defeitos reais: atalho caindo 119 m; câmera atravessando a porta; teste do flash instável; kit em cima do ponto de retorno; documento na linha de tiro do atirador | `ESTADO L989`, `L310`, `L275`, `L1120`, `L406` |
| Investigação disciplinada: hipóteses, pistas descartadas, reprodução e causa provada | `Investigacao-Respawn-Poco.md` |
| Proteção forte depois do erro: camada única de escrita e trava entre processos | `ESTADO L1410-1419` |
| Planos com discordâncias explícitas e perguntas para o dono | `Plano-ME1` §3 e §4; `Plano-Zoom` §3 |
| Explicação em português simples e roteiro de teste para o dono | Todas as seções "Como testar" do ESTADO |

### 9.2 Onde errou ou travou

| Problema | Fonte |
| --- | --- |
| Leu no log um resumo antigo como se fosse novo e disparou um segundo teste: os slots reais do dono foram perdidos | `ESTADO L1408` |
| Escreveu testes que usavam a mesma função do jogo, ou valores irreais (roda 120) | `ESTADO L586`, `L1170` |
| Escolheu IK do Animator no plano, descartou na implementação e afrouxou a tolerância de 2 cm para 25 cm | `Plano-B6-Empunhadura` item 3; `ESTADO L1497`, `L1501` |
| Um menu de setup apagou os cortes de outro personagem | `ESTADO L773`, `edfcab6` |
| Registrou um valor padrão errado no ESTADO (Forearm Stretch 5° em vez de 80°), o que levou a uma pose errada | `ESTADO L773` |
| Criou uma recursão infinita com `Instantiate` que travou a Unity | `ESTADO L342` |
| Travou em problemas visuais: manto da Ninsar (3 tentativas), pose parada da Elena (5 combinações), empunhadura (2 rodadas); os olhos não foram atacados | `ESTADO L964-968`, `L773`, `L1504` |
| Documentação que só cresce: linhas antigas desatualizadas, tabelas quebradas | `ESTADO L147`, `L356`, `L369`; `Regiao-1` L310-311 |
| Estimativa de esforço sem base: a auditoria deu 2 semanas para a localização | `Auditoria`, "Localização" |
| Rodou teste com o Play ainda ligado e leu falhas falsas | `ESTADO L1438` |

### 9.3 Regras do CLAUDE.md que evitaram problemas

| Regra | O que ela evitou |
| --- | --- |
| Não editar `.unity`, `.prefab` ou `.meta` à mão | Cenas corrompidas; tudo passou por script de editor |
| Nunca recompilar com o Play ligado | Estados quebrados ("Map must be contained in state"; memória `unity-play-testing-via-mcp`) |
| Testes automáticos um por vez | Nova perda de slots (memória `one-automated-test-at-a-time`; depois garantido por código) |
| Depois de 2 tentativas sem resolver, parar e resumir (25/10, `b6639b4`) | Loops longos (inferência: entrou depois da Ninsar) |
| Tarefa leve x tarefa de sistema; níveis de teste (3/11) | Regressão de ~45 min em cada ajuste de arte |
| Todo texto com chave em pt-BR e en; códigos permanentes | Tradução e save sem retrabalho (para o que entrou depois de 25/10) |
| Créditos no mesmo commit do asset | Declaração de IA pronta para a Steam |
| Atalhos por passo | Teste do dono em minutos |
| Sem metas de data | Pressa (escolha do dono) |
| Arte: nada da Asset Store e nada pago | Custo e licença sob controle (mas limitou as soluções de personagem; inferência) |

**Outras ferramentas:** o Codex chegou a criar um `AGENTS.md`, que foi ignorado no Git (`1d62f02`). Os commits dividem-se entre Opus 5.5 (169) e Sonnet 5.5 (152); o ESTADO marca algumas tarefas como **[Opus]** (`ESTADO L48`).

### 9.4 CLAUDE.md modelo para o próximo projeto

Copie, troque o que está entre `<>` e apague o que não servir. Tamanho alvo: até ~8 KB.

````markdown
# <NOME INTERNO> — <gênero em uma linha>

Jogo para PC (Steam), compatível com Steam Deck. Feito por uma pessoa, com IA.
Premissa em uma frase: <frase>. Design completo em `Docs/GDD.md` (aprovado depois do `Docs/Originalidade.md`).

## Fonte da verdade
- Tudo o que vale está neste repositório. O que não está aqui não foi decidido.
- Decisões: `Docs/DECISOES.md` (uma linha por decisão). Se o código contradizer o GDD ou uma decisão, pergunte antes de mudar.
- No começo de cada conversa, leia só: este arquivo, `Docs/ESTADO.md` e as últimas 10 linhas de `Docs/DECISOES.md`. Leia `Docs/Sistemas/<X>.md` só se a tarefa tocar o sistema X.

## Decisões técnicas fixas
- Unity <versão> LTS, URP, C#, novo Input System, Unity Test Framework.
- Código em `Assets/_<Jogo>/Scripts/<Modulo>/`, cada módulo com o seu `.asmdef`. Módulo não depende de UI; UI depende dos módulos.
- Câmera: <tipo>, atrás de uma interface `ICameraMode`.
- Unidades: 1 = 1 m. Cápsula do jogador: raio <0,3> m, altura <1,8> m. Porta mínima: largura = 2 × raio + 0,6 m; altura = altura da cápsula + 0,3 m. Degrau máximo: <0,3> m.
- Posição salva de qualquer personagem = onde o pé pisa.
- Velocidades de andar e correr = velocidade natural dos clipes escolhidos (±10%).
- Todo dado que vai para o save tem código permanente (campo `id`), nunca o nome do arquivo. O save tem `versao`; campo novo é opcional na mesma versão.
- Todo texto que o jogador vê nasce com chave em pt-BR e en (`Resources/Localization/<idioma>.json`). Nenhum texto fixo no código ou na cena.
- Menus com botões de verdade (<UI Toolkit ou uGUI>), com teclado, controle e mouse.

## Como trabalhar
- Escreva em português simples; o dono não é programador. Diga o que ele precisa fazer no editor.
- Tarefa leve (arte, posição, fala): faça direto, teste a área, uma linha no ESTADO, commit.
- Tarefa de sistema: plano em papel em `Docs/Planos/` (1 página; 3 se for grande), aprovado pelo dono antes do código.
- Um sistema por vez, testável sozinho. Mostre o resultado e espere o dono testar no Play antes de seguir.
- Scripts de editor e a ponte MCP para montar cenas e prefabs. Nunca edite `.unity`, `.prefab` ou `.meta` à mão.
- Cenas montadas por script têm um menu "Remontar tudo" que conhece a ordem certa.
- Commits pequenos, em português. Arquivos pesados no Git LFS. Nada fora do LFS acima de 100 MB.
- Sem metas de data.
- Depois de 2 tentativas sem resolver o mesmo problema, pare e resuma o que descobriu.
- Se a tarefa crescer além do combinado, proponha dividir.

## Testes
- Testes no Unity Test Framework (modo de edição e modo Play), rodáveis em modo batch.
- Um teste nunca calcula o resultado esperado com a mesma função que está testando.
- Valores de entrada iguais aos do aparelho real (meça antes).
- Tolerância escrita com o motivo. Nunca afrouxe uma tolerância para o teste passar: se não passa, o defeito é real.
- Todo teste passa pela trava de execução (um por vez) e nunca grava fora da pasta de teste.
- Todo momento novo do jogo ganha um atalho em **<Jogo> > Testes > <Momento>** e entra no teste geral dos atalhos.
- Nunca recompile com o Play ligado. Teste com `Time.timeScale = 0` não mede física. Remova aparelhos virtuais no fim.
- Níveis: 1 (sempre: o teste da tarefa); 2 (sistema compartilhado: + os testes dele); 3 (regressão completa: ao fechar um sistema, antes de uma build, ou quando um teste falhar de forma inesperada).

## Arte
- Direção de arte: <estilo>. <Regra sobre rostos humanos de perto.>
- Fontes permitidas e quem faz cada uma: `Docs/Arte/Fontes.md`. Todo asset entra em `Docs/Creditos.md` no mesmo commit.
- Todo modelo novo passa pela cena-régua (boneco de <1,70> m, porta e item padrão) antes de entrar no jogo.
- Personagens: corpo sem acessórios; acessórios como objetos separados presos a ossos; olhos separados com osso; texturas 2K; sem texturas embutidas no FBX.
- Animações: consulte `Docs/Animacoes.md`; importe só as usadas, Humanoid, avatar de referência.
- Pastas e arquivos com nomes em ASCII, sem espaço nem acento.

## Documentação
- `Docs/ESTADO.md`: até 8 KB. Fase e marco; o que está pronto (uma linha por sistema, com o teste); o que está aberto; próximo passo; links dos 5 últimos relatórios. O resto vai para `Docs/Historico/`.
- Ao fim de cada tarefa: relatório de passagem em `Docs/Relatorios/<data>-<tarefa>.md` (modelo em `Docs/Relatorios/_modelo.md`), decisões copiadas para `DECISOES.md`, ESTADO atualizado, commit.

## Eficiência
- Investigue só o que a tarefa toca. Não releia arquivos que não mudaram.
- Unity MCP só quando o trabalho exigir o editor; prefira consultas pontuais e logs só com erros.
- Teste na medida do risco.
- Relatório final curto: o que mudou, como foi testado, o que ficou pendente.
````

---

## 10. Lançamento: o que este projeto não chegou a tocar

Este projeto parou antes da fase C, cujo marco era a página "Em breve" na Steam (`ESTADO L15`). Nada abaixo foi feito. Os valores e regras da Steam e da Unity mudam: **confira cada item na documentação oficial do Steamworks e da Unity antes de agir**. Os que cito são do meu conhecimento geral, não do repositório.

### 10.1 O que falta aprender

| Tema | O que é | Observação |
| --- | --- | --- |
| Conta Steamworks | Cadastro como desenvolvedor, dados fiscais e bancários, taxa do Steam Direct por jogo (US$ 100 na regra que conheço, devolvida depois de um faturamento mínimo) | Há um período de espera entre pagar a primeira taxa e poder lançar; pessoa física no Brasil preenche o formulário fiscal dos EUA (W-8BEN). Fale com um contador sobre impostos no Brasil |
| Página "Em breve" | Textos, capturas, trailer, cápsulas, tags, requisitos, classificação de conteúdo | A Steam revisa a página antes de publicar; a página precisa ficar pública algumas semanas antes do lançamento |
| Cápsulas | Imagens de tamanhos fixos (cabeçalho, pequena, principal, vertical, biblioteca, fundo da biblioteca, logo) | São a "capa" do jogo na loja; precisam funcionar pequenas. Tamanhos atuais no Steamworks |
| Wishlists | Lista de desejos dos jogadores | É o principal indicador antes do lançamento; começa a contar quando a página abre |
| Steam Next Fest | Festival com demo, algumas vezes por ano | Inscrição antecipada no Steamworks; precisa de demo pronta e página aberta |
| Demo | Um aplicativo separado na Steam, ligado ao jogo | Pode entrar antes do jogo, para o Next Fest |
| Build | Build de Windows x64; envio pelo SteamPipe (steamcmd), depósitos e ramos (beta) | Testar a build fora do editor; testar no Steam Deck (ou Proton); revisão de compatibilidade do Deck |
| Integração com a Steam | Overlay, conquistas, salvamento na nuvem | Opcional para vender; a nuvem pode ser a automática apontando para a pasta dos slots |
| Trailer | 30 a 90 s, jogo de verdade nos primeiros segundos | Gravado da build, não do editor |
| Declaração de IA | O questionário de conteúdo da Steam pergunta sobre conteúdo gerado por IA | O texto deste projeto já existe (`Creditos-e-Licencas.md`, "Declaração de uso de IA") |
| Licenças e créditos | Termos atuais de Meshy, Tripo, Mixamo, AccuRig, fontes, sons e músicas; licença da Unity (o plano Personal tem limite de faturamento) | O projeto deixou vários como "resumo, conferir antes da Steam" e alguns "a confirmar" (`Creditos-e-Licencas.md`) |
| Nome e marca | Busca de nome na Steam e no registro de marcas | "Nibiru" era provisório (`GDD.md`, "Perguntas em aberto") |
| Preço | Preço base e preços regionais | Política de reembolso da Steam (tempo de jogo curto) pesa em jogo curto |
| Imprensa e divulgação | Press kit, devlog, redes, criadores de conteúdo | Começa junto com a página |

### 10.2 Checklist de lançamento

- [ ] Nome escolhido e conferido (Steam, Google, registro de marcas).
- [ ] Conta Steamworks aberta, dados fiscais e bancários aprovados, taxa paga.
- [ ] Licenças conferidas nos termos atuais e registradas em `Docs/Creditos.md`; nada "a confirmar".
- [ ] Declaração de IA escrita e revisada.
- [ ] Cápsulas em todos os tamanhos, legíveis em miniatura.
- [ ] 5 ou mais capturas de tela da build, em 1920 × 1080.
- [ ] Trailer curto da build.
- [ ] Textos da página em pt-BR e en.
- [ ] Página "Em breve" enviada para revisão e publicada.
- [ ] Demo como aplicativo separado; inscrição no Next Fest.
- [ ] Build de Windows testada fora do editor, em outro computador e no Steam Deck.
- [ ] Salvamento na nuvem configurado e testado.
- [ ] Controle, teclado e mouse em todos os menus; opções de áudio, vídeo e controles.
- [ ] Créditos dentro do jogo.
- [ ] Regressão completa e bot de passada verdes na build.
- [ ] Preço e preços regionais definidos.
- [ ] Press kit e lista de contatos.

---

## 11. Checklist do dia 1 do próximo jogo

Tudo isto decidido e configurado **antes da primeira linha de código**.

**Design (no papel)**
- [ ] Premissa em uma frase e os 3 pilares.
- [ ] `Docs/Originalidade.md`: checklist da seção 4.4 rodado e aprovado; nenhuma batida proibida da seção 4.3 sem torção registrada.
- [ ] Tabela de escopo (seção 5.3) preenchida para a demo e para o jogo; o que fica de fora escrito.
- [ ] Direção de arte decidida com a regra sobre rostos humanos (estilizado, câmera distante, máscara ou personagem não humano), testada com 1 personagem de prova antes de decidir.
- [ ] Laço principal descrito (o que o jogador faz a cada minuto).
- [ ] Idiomas: pt-BR e en. Voz: sim ou não.

**Medidas e convenções**
- [ ] Cápsula do jogador (raio e altura) e, dela, portas, corredores, degraus e coberturas.
- [ ] Velocidades de andar, correr e agachar tiradas dos clipes escolhidos.
- [ ] Tabela de alturas e tamanhos (personagens, veículos, itens) e a cena-régua.
- [ ] Posição salva = pé. Códigos permanentes para tudo que vai para o save. Versão do save = 1 e a regra de versão escrita.
- [ ] Prefixos de chave de idioma (a tabela do CLAUDE.md atual serve).

**Projeto Unity**
- [ ] Projeto URP criado; pacotes do modelo que não serão usados retirados (exemplos, Visual Scripting, Multiplayer Center, Collab).
- [ ] Pastas de código por módulo, cada uma com `.asmdef`; pasta `Tests` com os assemblies de teste.
- [ ] Unity Test Framework com um teste de exemplo rodando em modo batch num segundo clone.
- [ ] Mapa de controles inicial (teclado, mouse e controle) com as ações do laço principal.
- [ ] Localização (o módulo `Loc` daqui, extraído) e fonte central.
- [ ] Camada única de escrita do save e trava de testes (as daqui).
- [ ] Base dos atalhos de teste e o teste geral dos atalhos.
- [ ] Ponte MCP configurada (`Docs/Guia-MCP-Unity.md`) e "No Throttling" no editor.

**Repositório e documentos**
- [ ] `.gitignore` e `.gitattributes` (LFS) daqui, com a pasta de brutos já ignorada.
- [ ] Árvore da `ArteFonte` com nomes em ASCII.
- [ ] `CLAUDE.md` (modelo da seção 9.4), `Docs/ESTADO.md` (vazio, com as seções fixas), `Docs/DECISOES.md`, `Docs/Relatorios/_modelo.md`, `Docs/Creditos.md`, `Docs/Arte/Fontes.md`.
- [ ] Teste de editor que falha se o `ESTADO.md` passar de 8 KB.
- [ ] Planilha de custos (assinaturas e créditos de IA) e, se o dono quiser, de horas.

**Steam (cedo, mesmo sem jogo)**
- [ ] Nome conferido.
- [ ] Conta Steamworks aberta (o período de espera e a papelada correm enquanto o jogo é feito).

---

## 12. Perguntas em aberto para o dono decidir

1. **Gênero e tamanho do próximo jogo:** é o mesmo gênero (ação, terror, terceira pessoa) em escala menor, ou outro gênero que dependa menos de personagem humano animado?
2. **Personagem humano realista:** aceita uma direção de arte que evite rosto humano de perto (estilizado, câmera mais alta, máscara, criatura), já que foi o problema sem solução aqui?
3. **Dinheiro para arte:** existe orçamento para um artista freelancer só para o personagem principal (rosto, olhos, rig, roupa), deixando o resto com IA e script?
4. **Meshy e Tripo:** continuar nos planos Pro? Os créditos vencem em 30/11 (`Fontes-da-Arte.md`).
5. **Governança no Claude Doc:** aceita que toda decisão vá para o `DECISOES.md` no repositório e que o Claude Doc deixe de ser fonte?
6. **Medir horas:** sem prazos, aceita registrar as horas gastas por tarefa (só para dimensionar, nunca como meta)? A linha de calibração foi tirada em 3/11 (`6397a71`) porque "convertia trabalho em calendário".
7. **Kit base:** extrair os módulos da seção 7 num pacote antes de começar, ou começar limpo e copiar só quando precisar?
8. **Testes sem o editor:** aceita um segundo clone do repositório rodando testes à noite na mesma máquina?
9. **Primeira demo pública:** a primeira meta é o Next Fest (precisa de demo e página) ou só a página "Em breve"?
10. **Voz:** o próximo jogo terá voz gravada? Se sim, de quem?
11. **GENESIS CODE:** fica só congelado, ou um dia volta com outra história usando os sistemas daqui? Isso decide se vale manter o repositório organizado.
12. **Repositório:** o próximo jogo nasce num repositório novo (recomendado: o LFS deste já passa de 1,5 GB, `ESTADO L981`) ou neste?
13. **Os slots reais do GENESIS CODE** continuam com dados falsos de teste desde 26/10 (`ESTADO L1422`). Apagar, ou deixar como estão?
