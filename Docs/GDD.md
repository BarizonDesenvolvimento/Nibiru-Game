# Nibiru — Documento de Design

19 de outubro de 2026

A história em profundidade (eras do mundo, panteão, arcos dos personagens, mistérios) está em `Biblia.md`, nesta mesma pasta.

## Visão geral

**Nibiru** é um jogo de ação e aventura em terceira pessoa, com terror e RPG, em que uma arqueóloga descobre que os deuses Anunnaki nunca foram embora, e que ela mesma é a chave para acordá-los.

| Item | Decisão |
| --- | --- |
| Gênero | Ação e aventura, com terror e RPG |
| Câmera | Terceira pessoa, sobre o ombro (estilo Resident Evil 4). Primeira pessoa entra depois, como opção que o jogador alterna |
| Visual | Realista e colorido. A superfície é poeira e ruína; o mundo Anunnaki é vivo, com lápis-lazúli, ouro, luz própria e arquitetura fiel aos relatos sumérios |
| Tom | Equilibrado: tensão e sustos, mas dá para lutar com poucos recursos |
| Duração alvo | 25 a 40 horas, com missões secundárias |
| Plataforma | PC (Steam), compatível com Steam Deck |
| Engine | Unity 6 LTS, pipeline URP |
| Nome | Nibiru (provisório) |

O terror vem do clima, do som e dos inimigos, não de deixar a tela escura. Quanto mais fundo o jogador desce, mais colorido e deslumbrante o mundo fica, e mais perigoso. A câmera e os controles são construídos desde o início para aceitar a primeira pessoa depois sem refazer o jogo.

**Os três pilares** guiam toda decisão de design. Se uma ideia não serve a pelo menos um deles, ela sai.

1. **Descer para descobrir.** Cada camada do mundo (literal e da conspiração) revela uma verdade nova. O jogador sempre quer saber o que tem mais embaixo.
2. **O poder custa a humanidade.** Os poderes Anunnaki deixam a Elena mais forte, mas a transformam por fora e por dentro.
3. **Escolhas com peso.** Quem você ajuda, em quem confia e o que você se torna decidem o final.

## História

Em abril de 2003, no sul do Iraque, a arqueóloga Elena Varga desce num sítio recém-aberto perto de Ur. Só ela volta à superfície, sem lembrar como, e capaz de ler uma língua mais antiga que a Suméria.

**Premissa.** No caos da guerra, a Fundação Enlil, uma organização privada de "preservação cultural", contrata Elena para resgatar artefatos de um sítio que não aparece em nenhum mapa. A equipe desce. Três dias depois, Elena acorda sozinha no acampamento, cercada de corpos, segurando uma tabuleta que ela não lembra de ter pego.

### Ato 1: A Escavação

- Elena descobre que consegue ler as tabuletas, e que cada uma desperta algo nela: uma memória que não é dela e um poder.
- A Fundação manda uma equipe de "resgate" que, na verdade, veio apagar todas as testemunhas.
- Ela conhece os primeiros sobreviventes e ouve falar dos Guardiões, uma ordem antiga que vigia o lugar.
- Com o guia Samir, ela foge do Sítio de Ur para Tell Amar, cidade ocupada pela Fundação, onde os Guardiões se escondem.
- Com o cerco se fechando, no fim do ato ela desce para o único lugar onde os soldados não entram: as ruínas sob o zigurate, o Templo de Enki.

### Ato 2: A Cidade Abaixo

- Sob o zigurate existe Kur, uma cidade inteira, impossível, construída antes dos humanos.
- Os Anunnaki não partiram. Estão adormecidos, e seus servos (humanos modificados ao longo de milênios) ainda guardam a cidade.
- Elena encontra os Despertos, híbridos que fugiram dos deuses e querem viver livres.
- A cada tabuleta, ela fica mais forte e menos humana. As facções começam a disputá-la.

### Ato 3: A Verdade

- A Fundação Enlil é a face moderna de um culto que serve os Anunnaki desde a Babilônia. O objetivo nunca foi esconder os deuses, e sim acordá-los.
- Elena descobre que descende dos primeiros híbridos. Ela não foi encontrada por acaso: ela é a chave viva que abre a câmara final.
- A sequência final acontece no Ekur, a câmara onde os deuses dormem, com a Fundação, os Guardiões e os Despertos chegando ao mesmo tempo.

### O fim

No Ekur, Elena decide o destino dos deuses e o dela. O que ela pode escolher depende das alianças, das tabuletas decifradas e de quanto ela se transformou. Os finais estão detalhados mais abaixo.

## Personagens

Cinco personagens carregam a história; cada facção tem um rosto, e cada um pode virar aliado ou inimigo conforme as escolhas.

| Personagem | Quem é | O que quer | Papel no jogo |
| --- | --- | --- | --- |
| **Elena Varga** | Arqueóloga, 38 anos, protagonista. Cética e teimosa | Descobrir o que aconteceu com a equipe e com ela mesma | Personagem jogável. Ganha poderes ao decifrar tabuletas |
| **Dr. Aurelian Kast** | Diretor da Fundação Enlil. Elegante, paciente, convicto | Acordar os Anunnaki e ser recompensado por eles | Antagonista principal. Quer a Elena viva |
| **Samir Haddad** | Guia local e último dos Guardiões da região | Manter os deuses dormindo, a qualquer custo | Primeiro aliado. Pode trair a Elena se ela se transformar demais |
| **Ninsar** | Híbrida, líder dos Despertos. Aparência quase humana | Liberdade para os híbridos, longe dos deuses e da Fundação | Aliada possível no Ato 2. Espelho do que a Elena pode virar |
| **Tom Becker** | Fotógrafo da equipe original, dado como morto | Sobreviver e expor tudo ao mundo | Aparece no Ato 2 com memórias da descida. Chave do final secreto |

**Os Anunnaki** não são personagens que falam no começo. Eles aparecem em visões, estátuas e ecos nas tabuletas, e só ganham voz no Ato 3.

## Facções e reputação

Três facções disputam a Elena, e a reputação com cada uma decide quais missões, aliados, equipamentos e finais ficam disponíveis.

| Facção | O que é | Objetivo | O que oferece |
| --- | --- | --- | --- |
| **Fundação Enlil** | Culto milenar disfarçado de organização de preservação cultural. Soldados, cientistas, dinheiro | Acordar os Anunnaki | Armas modernas, tecnologia, acesso aos laboratórios |
| **Guardiões** | Ordem antiga que vigia os sítios desde a Suméria. Poucos, discretos, sábios | Manter os deuses dormindo para sempre | Conhecimento das tabuletas, rotas secretas, armas rituais |
| **Despertos** | Híbridos que fugiram dos deuses. Vivem escondidos em Kur | Liberdade para os híbridos | Poderes híbridos, abrigo, aliados dentro da cidade |

**Como a reputação funciona**

- Cada facção tem uma barra de reputação, de hostil a aliada.
- Missões, diálogos e decisões sobem uma e costumam baixar outra. Agradar todas ao mesmo tempo é difícil, mas possível para quem explora tudo.
- Facção hostil manda caçadores atrás da Elena e fecha suas missões. Facção aliada abre missões exclusivas, lojas e um aliado no confronto final.
- A transformação da Elena também pesa: os Guardiões desconfiam dela quanto mais divina ela fica; os Despertos se aproximam.

## Regiões

O mundo tem seis regiões grandes empilhadas em profundidade, ligadas a um refúgio central, sem mundo aberto.

| # | Região | Ato | Descrição |
| --- | --- | --- | --- |
| 1 | Sítio de Ur | Ato 1 (fatia vertical) | Deserto e escavação. Fuga da Fundação. Primeira tabuleta |
| 2 | Tell Amar | Ato 1 | Cidade ocupada pela Fundação. Guardiões escondidos |
| 3 | Templo de Enki | Ato 1–2 | Ruínas sob o zigurate. Primeiras cores e luz própria |
| 4 | Kur, a Cidade Abaixo | Ato 2 | Cidade Anunnaki viva, em lápis-lazúli e ouro. Lar dos Despertos |
| 5 | Laboratórios Enlil | Ato 2–3 | Onde a Fundação estuda e cria híbridos |
| 6 | Ekur | Ato 3 | A câmara onde os deuses dormem. Confronto final |

**Refúgio:** base central com aliados, loja, melhorias e quadro de missões. Dá acesso às regiões 1 a 5.

O jogador desce em ordem, mas pode voltar às regiões anteriores pelo refúgio com poderes novos, abrindo atalhos e segredos que antes estavam fechados.

## Loop de jogo e sistemas

O jogador explora uma região, enfrenta o que mora nela, encontra tabuletas e volta ao refúgio para evoluir e escolher a próxima missão.

**Minuto a minuto:** explorar, encontrar perigo, lutar ou fugir, achar recursos e segredos.

**Hora a hora:** completar uma missão, decifrar uma tabuleta, ganhar um poder, tomar uma decisão que mexe com as facções.

**Jogo inteiro:** descer camada por camada, transformar-se e decidir o destino dos Anunnaki.

### Sistemas principais

| Sistema | Como funciona |
| --- | --- |
| Exploração | Áreas interligadas, atalhos que se abrem, caminhos que só um poder novo libera |
| Combate | Armas de fogo com munição escassa, ataque corpo a corpo, esquiva. Poderes Anunnaki como alternativa |
| Tabuletas | Item principal de progressão. Decifrar uma tabuleta dá um poder, uma memória e um pedaço da história |
| Transformação | Barra de humano a divino. Mais divino = poderes mais fortes, mudança no visual e nas reações dos personagens |
| RPG | Níveis, árvore de habilidades em três ramos (combate, furtividade, poderes), equipamentos com atributos |
| Missões | Principais, secundárias e de facção, com consequências que aparecem mais tarde |
| Diálogos | Escolhas de resposta que afetam reputação, aliados e finais |
| Refúgio | Base central: aliados, loja, bancada de melhorias, quadro de missões |
| Inventário e crafting | Espaço limitado, combinar recursos em munição, cura e itens rituais |
| Câmera | Terceira pessoa no lançamento. Primeira pessoa como opção numa atualização |
| Save | Pontos de salvamento no refúgio e em altares; salvamento automático entre áreas |

## Finais

O jogo tem cinco finais e um secreto, definidos pela facção aliada, pelo nível de transformação e pelos segredos descobertos.

| Final | O que acontece | Como chegar |
| --- | --- | --- |
| **O Selo** | Elena se sacrifica para lacrar o Ekur. Os deuses dormem para sempre | Aliada dos Guardiões, transformação baixa |
| **A Ascensão** | Elena acorda os deuses e se torna uma deles. O mundo muda de dono | Aliada da Fundação, transformação máxima |
| **A Traição** | Elena acorda os deuses, mas toma o poder deles para si | Hostil à Fundação, transformação máxima |
| **O Êxodo** | Elena lidera os Despertos para fora de Kur e esconde a cidade do mundo | Aliada dos Despertos, transformação média |
| **A Queda** | Ninguém vence. O Ekur desaba com todos dentro | Hostil às três facções |
| **A Verdade** (secreto) | Elena e Tom revelam tudo ao mundo, com provas. A conspiração cai | Todas as tabuletas decifradas, Tom vivo, nenhuma facção hostil |

O **New Game+** começa com a Elena guardando memórias da jornada anterior, o que abre diálogos novos e uma tabuleta extra que só aparece na segunda vez.

## Próximos passos

Com a espinha aprovada, o próximo passo é detalhar e construir a fatia vertical: o Sítio de Ur, cerca de uma hora de jogo completo.

- [ ] Revisar este documento e marcar o que muda
- [ ] Escolher 10 a 20 imagens de referência visual (superfície e mundo Anunnaki)
- [x] Detalhar a região 1: mapa, missões, diálogos, inimigos, itens (em `Regiao-1-Sitio-de-Ur.md`)
- [x] Criar o projeto Unity 6 (3D URP) e conectar o Claude Code
- [x] Protótipo do personagem: andar, correr, esquivar, câmera em terceira pessoa

**Perguntas em aberto**

- Nome definitivo do jogo: Nibiru ou outro?
- A Elena fala (com dublagem) ou só por texto?
- Idiomas do lançamento: português e inglês?
