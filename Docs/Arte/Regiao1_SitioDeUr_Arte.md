# Região 1 — Sítio de Ur: guia de arte e modelagem

Documento para o Claude Code transformar o blockout da cena `Regiao1_SitioDeUr` em arte final, **uma área por vez**. Complementa o `Docs/Regiao-1-Sitio-de-Ur.md` (jogabilidade) e o `Docs/GDD.md`. Se algo aqui contradizer a jogabilidade, a jogabilidade vence: pergunte antes de mudar.

---

## 0. Regras gerais (valem para todas as áreas)

### 0.1 Regra de ouro
- **Manter posições, medidas e caminhos do blockout.** A arte substitui as caixas peça por peça; o caminho do jogador, a malha de navegação, os gatilhos, os checkpoints e os discos de conteúdo continuam onde estão.
- Se uma peça de arte precisar ser maior ou menor que a caixa, ajuste a arte, não o caminho. Mudanças de layout só com aprovação do dono.
- Depois de cada área: refazer a malha de navegação, rodar os menus de conteúdo da área e testar o percurso no Play.

### 0.2 Organização
- Uma área por conversa e por commit.
- Modelos em `Assets/_Nibiru/Art/Ambientes/SitioDeUr/<Area>/`; arquivos-fonte (.blend) em `ArteFonte/Ambientes/SitioDeUr/<Area>/`.
- Objetos da cena em grupos: `Arte_<Area>` (visual), `Colisao_<Area>` (colisores simples), `Luz_<Area>`, `FX_<Area>`, `Audio_<Area>`.
- Scripts de montagem no padrão dos menus existentes (`Nibiru > Arte > Montar <Area>`), reconstruindo o grupo a cada execução.

### 0.3 Escala e orçamento
- 1 unidade = 1 metro. Personagens entre 1,70 m e 1,90 m. Portas com 2,0–2,2 m de altura e 0,9–1,2 m de largura.
- Triângulos por peça: pequenas 300–3.000; médias 3.000–10.000; grandes 10.000–30.000. Landmarks (zigurate) em módulos, nunca uma malha gigante única.
- LOD (2–3 níveis) em tudo que é grande ou repetido muitas vezes. GPU Instancing nos materiais de peças repetidas.
- Texturas: 1K em peças pequenas, 2K em grandes e heróis. Paredes, chão e madeira usam **texturas repetíveis e trim sheets**, não texturas únicas por objeto.
- Colisores: sempre **caixas e cápsulas simples** separadas da malha visual. Nunca colisor de malha em props decorativos.

### 0.4 De onde vem cada coisa
- **Kit modular de arquitetura** (paredes de barro, trincheiras, degraus do zigurate, andaimes, passarelas): feito no Blender por script, com UV para texturas repetíveis. O Meshy não serve para peças modulares que precisam encaixar.
- **Props sólidos** (caixas, geradores, rádio, cerâmicas, ferramentas, jarros): Meshy com **remesh antes de baixar**, ou packs da Asset Store/Fab.
- **Veículos** (caminhões, caminhonetes, helicóptero): packs prontos e só trocar as cores. Não gerar no Meshy.
- **Tecidos estáticos** (lonas de tenda, toldos, bandeiras): malha simples feita no Blender, com balanço leve por vento (shader ou osso), sem simulação de pano em tempo real.
- **Terreno:** Unity Terrain com camadas pintadas; detalhes com decals.
- **Vegetação:** palmeiras e arbustos de pack, com balanço por vento no shader.

### 0.5 Direção de arte da região
- **Colorida e luminosa.** O terror vem do som, do silêncio e dos inimigos, não de escuridão. Nada de tudo cinza e marrom.
- Paleta base: areia clara quente, ocre, terracota, céu azul intenso, verde-oliva seco das palmeiras.
- Cores de destaque: **turquesa** (portas da aldeia e faixa da Fundação), **branco** (tendas e veículos da Fundação), **cáqui** (uniformes), **azul de lápis-lazúli** (só onde Kur vaza: o Selo e o brilho no Poço). O azul de Kur é raro de propósito: quando aparece, o jogador sente.
- Luz: sol forte e duro, sombras bem definidas, céu limpo com poeira no horizonte. Pós-processo com leve bloom no sol e no ouro, correção de cor quente, névoa de distância cor de areia.
- **Sequência de horário proposta** (confirmar com o dono): amanhecer no Acampamento → manhã nas Trincheiras → meio-dia na Aldeia → tarde no Zigurate → interior frio e azul no Poço → fim de tarde com tempestade de areia laranja no Wadi.

### 0.6 Fatos reais para dar verdade (abril de 2003, sul do Iraque)
- Ur é uma planície aluvial plana e seca, perto do antigo leito do Eufrates. O zigurate se ergue sozinho no horizonte e é visível de quase todo o sítio.
- O **Zigurate de Ur** foi construído por Ur-Nammu (por volta de 2100 a.C.) para o deus da lua, Nanna. Núcleo de tijolo de barro cru, fachada de tijolo cozido assentado com betume, paredes levemente inclinadas com contrafortes verticais e furos de drenagem. Três grandes escadarias (uma central perpendicular e duas laterais encostadas à fachada) se encontram num portal no primeiro terraço. Base de aproximadamente 64 × 46 m. Parte da fachada foi reconstruída nos anos 1980.
- As escavações famosas de Ur (Leonard Woolley, 1922–1934) revelaram o Cemitério Real. Restos da antiga casa de escavação ainda existem perto do sítio.
- Em 2003 havia uma grande base aérea militar ao lado do sítio real. No jogo, a base da Fundação ocupa esse papel.
- Escavação arqueológica real: grade de quadrados com **paredes de terra deixadas entre eles (balks)**, estacas e barbantes, escalas listradas vermelho e branco, peneiras em cavaletes, carrinhos de mão, baldes de borracha, montes de terra descartada, mesas de triagem de cerâmica, etiquetas e sacos numerados, tripé com aparelho de medição, toldos de sombra.

---

## Área 1 — Acampamento

### Função
Primeiro lugar que o jogador vê. Tutorial de movimento, faca, exame de corpos, rádio e a cena do "resgate". Tempo de jogo: 8–12 min. Precisa ser a área mais bonita e mais cuidada da fatia vertical.

### Layout (manter o blockout)
- Acampamento sobre uma **elevação plana**, com beira alta e uma **rampa** de descida (fechada por parede invisível até a execução).
- **Tenda da Elena** (oca, com porta): ponto de início.
- Tendas da equipe ao redor de um pátio central com fogueira apagada.
- **Caminhão** com o 1º rolo de filme atrás.
- **Rádio** de campanha numa mesa sob toldo.
- **Mirante** no canto nordeste da elevação, de onde se vê o heliponto e a execução.
- **Heliponto** abaixo da elevação, atrás das caminhonetes.
- Ao norte, visíveis da elevação: as Trincheiras e, ao fundo, o Zigurate.

### Terreno e materiais
- Chão da elevação: terra compactada clara com pegadas e marcas de pneu (decals).
- Beira da elevação: talude de terra com camadas sedimentares visíveis.
- Descida: areia solta mais escura.
- Materiais repetíveis: lona de algodão bege envelhecida, lona verde-oliva militar, madeira de compensado desbotada, metal pintado descascado, plástico azul de tambor.

### Kit e props
| Item | Qtd. | Notas |
| --- | --- | --- |
| Tenda grande de expedição (lona bege, mastros, cordas e estacas) | 5–6 | Uma delas é a da Elena, oca, com interior |
| Interior da tenda da Elena | 1 | Catre de campanha, mala aberta, lampião a gás, mesinha dobrável, mapas presos com alfinete, caneca, **caixa com a faca de campo** |
| Tenda-refeitório com toldo aberto | 1 | Mesas compridas, bancos, galões de água, panelas |
| Mesas de triagem de cerâmica | 3–4 | Bandejas com cacos, etiquetas, sacos numerados, pincéis |
| Gerador a diesel com cabos no chão | 2 | Um ainda ligado (som e fumaça leve) |
| Antena de rádio em mastro com estais | 1 | Bom ponto de referência vertical |
| Rádio de campanha sobre mesa | 1 | Herói: textura 2K, luz vermelha piscando quando chia |
| Caminhão de expedição | 2 | Pack de veículo; um com a lona traseira rasgada |
| Caminhonetes | 2 | Na beira, perto do heliponto |
| Galões, tambores azuis, caixas de madeira e de plástico | 20–30 | Espalhados para dar vida e cobertura |
| Peneiras em cavaletes, carrinhos de mão, pás, picaretas, baldes de borracha | 10–15 | Material de escavação |
| Escalas vermelho e branco, estacas, barbantes | vários | Detalhe arqueológico |
| Corpos da equipe | 7 | Cobertos ou caídos; queimaduras em forma de escrita (decal com brilho fraco) |
| Bolsa vazia da câmera do Tom | 1 | Na frente da barraca dele; herói |
| Fogueira apagada com pedras e cinzas | 1 | Centro do pátio |
| Varal com roupas e bandeirinhas | 2 | Balanço leve por vento |
| Heliponto: disco pintado, cones e biruta | 1 | Biruta balançando indica o vento |

### Pontos de interação (não mover)
Tenda da Elena e caixa da faca, corpos, bolsa do Tom, 1º rolo de filme atrás do caminhão, rádio, mirante, checkpoints na porta da tenda e no mirante.

### Luz e atmosfera
- **Amanhecer:** sol baixo vindo do leste, luz dourada e sombras longas, céu indo de laranja a azul.
- Dentro da tenda: luz filtrada pela lona, tom bege quente, poeira suspensa no feixe da porta.
- Partículas: poeira leve no vento, fumaça fina do gerador, moscas perto dos corpos (pequenas, sem exagero).
- Sensação: silêncio errado. Bonito e calmo demais para o que aconteceu.

### Som ambiente
Vento constante, lona batendo, gerador ao longe, estática de rádio, cachorro latindo distante, sem pássaros (o silêncio é o susto).

### Ordem de produção
1. Tenda da Elena com interior. 2. Tendas da equipe e pátio. 3. Rádio, mesas e props de escavação. 4. Veículos e heliponto. 5. Terreno, talude e decals. 6. Luz, partículas e som.

---

## Área 2 — Trincheiras

### Função
Primeira furtividade. Labirinto de escavação onde o jogador se esconde de patrulhas, acha a pistola no corpo do chefe de segurança, encontra a Dra. Weiss escondida, pega o 2º rolo de filme e vê o helicóptero passar baixo. Tempo: 10–15 min.

### Layout (manter o blockout)
- **Grade de trincheiras** retangulares de escavação, com corredores de terra (balks) entre elas. O jogador anda **dentro** das trincheiras (protegido) ou **em cima** dos balks e passarelas (exposto).
- Profundidades variadas: 1,2 m (agachada se esconde), 2,5 m e trechos de 4 m nas sondagens fundas.
- Escadas de madeira e rampas de terra para entrar e sair.
- **Passarelas de tábuas** sobre trincheiras.
- **Passagem baixa** que só dá para atravessar agachada (já existe no blockout).
- Saída a oeste, em direção à Aldeia.

### Terreno e materiais
- Paredes das trincheiras: **terra em camadas sedimentares** bem visíveis (faixas de ocre, marrom, cinza, uma camada escura de cinza de incêndio antigo, fragmentos de cerâmica e tijolo cravados). Esse é o material mais importante da área: textura repetível 2K com normal map forte.
- Fundo das trincheiras: terra solta, pegadas, baldes caídos.
- Topo dos balks: terra compactada com grama seca rala.
- Em algumas trincheiras: **muros antigos de tijolo cru aparecendo** (fundações sumérias), com tijolos plano-convexos.

### Kit e props
| Item | Qtd. | Notas |
| --- | --- | --- |
| Kit modular de trincheira (paredes retas, cantos, rampas, degraus escavados) | kit | Encaixe em grade de 1 m; UV contínua para as camadas casarem entre módulos |
| Muros sumérios expostos (tijolo cru, plano-convexo) | 6–10 trechos | Dentro das trincheiras |
| Passarelas de tábuas e pranchas soltas | 8–12 | Rangem quando pisadas (som) |
| Escadas de madeira de mão | 6–8 | |
| Estacas, barbantes da grade, etiquetas numeradas nas paredes | muitas | Reforçam "escavação real" |
| Montes de terra descartada (spoil heaps) | 4–6 | Boa cobertura e silhueta |
| Toldos de sombra sobre algumas trincheiras | 3–4 | Criam manchas de sombra para furtividade |
| Tripé com aparelho de medição, prancheta, mira listrada | 2 | |
| Holofotes portáteis da Fundação (cáqui e turquesa) | 3–4 | Desligados de dia; ajudam a ler a presença inimiga |
| Corpo do chefe de segurança com a pistola | 1 | Herói |
| Esconderijo da Dra. Weiss (sondagem funda coberta por lona) | 1 | |
| 2º rolo de filme | 1 | Num nicho da parede |

### Pontos de interação (não mover)
Pistola, Dra. Weiss, 2º rolo de filme, passagem baixa, rotas de patrulha, ponto da passagem do helicóptero.

### Luz e atmosfera
- **Manhã:** sol mais alto, sombras curtas e fortes dentro das trincheiras. As trincheiras são a parte **mais escura e fresca** da região, mas ainda bem legíveis.
- Contraste claro entre "dentro" (sombra, seguro) e "em cima" (sol, exposto). Isso ensina a furtividade visualmente.
- Partículas: terra caindo das paredes quando o helicóptero passa, poeira levantada pelo vento do rotor.

### Som ambiente
Vento abafado dentro das trincheiras, rádio dos soldados ao longe, passos em terra, tábuas rangendo, helicóptero crescendo e sumindo.

### Ordem de produção
1. Kit modular de trincheira com o material de camadas. 2. Passarelas, escadas e montes de terra. 3. Muros sumérios expostos. 4. Props de escavação. 5. Pontos de história. 6. Luz e som.

---

## Área 3 — Aldeia

### Função
Primeiro respiro e base da região: encontro com o Samir, sobreviventes, altar e checkpoint, a primeira tabuleta, a missão "Remédio para Yusuf" com a Dra. Weiss, Umm Karim e a revelação dos filmes do Tom. Tempo: 12–18 min. É a área mais **calorosa e humana** do Sítio de Ur.

### Layout (manter o blockout)
- Aldeia de casas de tijolo de barro em volta de um **pátio central com poço**.
- **Quatro entradas** (leste, sudeste, sul e norte) onde o encontro com o Samir pode acontecer. Precisam ser legíveis como "entrada": um vão entre casas, um portão ou um muro baixo quebrado.
- **Casa do altar** de frente para o pátio.
- Mercado pequeno, mesquita pequena, palmeiras, canal de irrigação seco.

### Arquitetura (kit modular)
- **Casas de tijolo de barro** com reboco de barro alisado à mão, cantos arredondados, partes descascadas mostrando o tijolo.
- Telhados **planos**, com mureta (parapeito), vigas de tronco de palmeira saindo das paredes e calhas de madeira para a água da chuva.
- Pátios internos murados em algumas casas.
- **Portas e janelas de metal pintadas de turquesa** (cor de destaque da área), algumas desbotadas, outras azul-claro ou verde.
- Variações: térreas e de dois andares, com escada externa para o telhado.
- **Mesquita pequena**: volume branco caiado, cúpula baixa, minarete simples e curto.
- Kit: paredes retas (2 e 4 m), cantos, portas, janelas, muretas de telhado, escadas externas, muros de quintal, arco de entrada.

### Props
| Item | Qtd. | Notas |
| --- | --- | --- |
| Poço central com borda de tijolo, roldana e balde | 1 | Herói do pátio |
| Palmeiras-tamareiras | 10–15 | Balanço por vento; troncos com textura de escamas |
| Bancas de mercado com toldos coloridos | 4–6 | Tecidos estampados, sacos de grãos, tâmaras, cestos |
| Jarros de barro, potes, cestos de vime, tapetes estendidos | muitos | Dão cor e vida |
| Bicicletas velhas, motocicleta, carroça de madeira | 3–4 | |
| Galinhas e cabras (animadas simples) | opcional | Vida ambiente |
| Varais, mangueiras, tambores de água, antena de TV no telhado | vários | Toques de 2003 |
| Cartazes e murais oficiais rasgados e riscados nas paredes | 3–5 | Marca de abril de 2003, sem rostos reconhecíveis |
| Interior da casa do altar | 1 | Tapetes, almofadas, nicho na parede, lampião, altar de pedra antiga reaproveitada com sinais cuneiformes |
| Casa de Umm Karim e casa de Yusuf | 2 | Interiores simples e acolhedores |
| Canal de irrigação seco com ponte de tábuas | 1 | |

### Pontos de interação (não mover)
Quatro pontos de encontro com o Samir, pátio (checkpoint), altar, sobreviventes, Yusuf, Umm Karim, Dra. Weiss, local da revelação dos filmes.

### Luz e atmosfera
- **Meio-dia:** luz forte e branca do alto, sombras curtas embaixo dos toldos e dentro dos becos.
- Interiores com feixes de luz entrando pelas janelas e portas, tons quentes de barro e tecido.
- Partículas: poeira dourada nos feixes, roupas e toldos balançando.
- Sensação: abrigo frágil. Bonito e humano, mas vazio demais, com sinais de fuga às pressas.

### Som ambiente
Vento nas palmeiras, toldos batendo, água pingando no poço, galinha ao longe, rádio velho tocando música árabe baixinho numa casa (fonte diegética).

### Ordem de produção
1. Kit de casas de barro e turquesa. 2. Pátio, poço e casa do altar. 3. Mercado e mesquita. 4. Palmeiras e props de vida. 5. Interiores. 6. Luz e som.

---

## Área 4 — Zigurate e base da Fundação

### Função
O landmark da região e a área mais perigosa: a base da Fundação montada aos pés do zigurate, com patrulhas, atirador e heliponto. O jogador atravessa por furtividade ou combate até a Boca do Poço. Tempo: 10–15 min.

### Layout (manter o blockout)
- **Zigurate** no centro, visível de toda a região.
- Base da Fundação em volta da base do zigurate: tendas brancas, contêineres, heliponto, posto de atirador elevado.
- Rotas de infiltração: por trás dos contêineres, pelas escadarias laterais do zigurate, por valas de escavação antigas.

### O zigurate (landmark)
- Inspirado no Zigurate de Ur real, com escala ajustada ao blockout.
- Corpo em **três níveis escalonados**; o primeiro bem mais largo e alto.
- **Paredes inclinadas** com contrafortes verticais regulares e fileiras de furos de drenagem.
- Fachada de **tijolo cozido cor de mel e ocre**, juntas escuras de betume, partes restauradas (tijolo mais novo e regular) misturadas a partes antigas e erodidas.
- **Três escadarias**: uma central de frente e duas laterais encostadas à fachada, encontrando-se num portal no topo do primeiro nível.
- Topo arruinado, sem o santuário original; fragmentos e montes de tijolo.
- Detalhe: tijolos estampados com inscrição cuneiforme (decal), visíveis de perto nas escadarias.
- Construção **modular**: módulos de parede inclinada com contrafortes, cantos, degraus, patamares e portal. Material repetível de tijolo 2K com variação por vertex color (desgaste, restauro, manchas de betume).

### Base da Fundação
| Item | Qtd. | Notas |
| --- | --- | --- |
| Tendas militares brancas com logo da Fundação (coroa com chifres) | 4–6 | Branco limpo contrastando com a poeira |
| Contêineres brancos e turquesa, empilhados | 8–12 | Principal cobertura de furtividade |
| Laboratório de campo em contêiner aberto | 1 | Mesas brancas, caixas de amostras, artefatos embalados |
| Gaiolas de artefatos saqueados embalados em plástico e palha | 4–6 | Reforça o "resgate" que é roubo |
| Barreiras de gabião (sacos de terra em armação) e cercas de arame | muitas | Delimitam a base |
| Posto de atirador elevado (torre de andaime com lona) | 1 | Linha de visão clara para o jogador ler o perigo |
| Heliponto com helicóptero branco de faixa turquesa | 1 | Mesmo modelo da cena do Acampamento |
| Holofotes de torre, gerador grande, cabos | 4–6 | |
| Veículos utilitários com o logo | 2–3 | |
| Mesa de comando com mapas, rádio e tela | 1 | Pode ter documento da Fundação para ler |

### Luz e atmosfera
- **Tarde:** sol mais baixo vindo do oeste, o zigurate ganha um tom **dourado** e sombras longas na fachada.
- Base iluminada também por holofotes já ligados, para a leitura de áreas vigiadas.
- Partículas: poeira levantada pelos veículos, bandeiras e lonas batendo.
- Sensação: um monumento sagrado ocupado como depósito.

### Som ambiente
Geradores, rádios, ordens em inglês ao longe, vento forte no alto do zigurate, helicóptero estacionado com o rotor desacelerando.

### Ordem de produção
1. Kit do zigurate (paredes, contrafortes, escadarias, portal). 2. Montagem do zigurate. 3. Contêineres, tendas e barreiras. 4. Posto de atirador e heliponto. 5. Props da Fundação. 6. Luz, partículas e som.

---

## Área 5 — Boca do Poço

### Função
O momento em que o mundo comum acaba. Descida vertical por um poço escavado pela Fundação até a antecâmara com a porta de pedra e o **Selo de Kur**. Primeiro contato visual com o azul de Kur. Tempo: 5–10 min.

### Layout (manter o blockout)
- Boca do poço na superfície, cercada por andaimes e um **guincho** com cabine.
- Descida em lances (escadas de andaime presas à parede), com plataformas de descanso.
- No fundo, a **antecâmara**: sala retangular de pedra antiga, com a porta de pedra ao fundo.
- Cuidado já anotado no blockout: o teto da antecâmara não pode bloquear o segundo lance da descida.

### Materiais
- Parede do poço: corte em terra e rocha, com as **camadas sedimentares** ficando mais antigas e escuras conforme desce; no fundo, tijolo e pedra trabalhada.
- Andaimes metálicos galvanizados, tábuas, redes de proteção laranja, cabos de aço.
- Antecâmara: blocos de **pedra lisa e muito antiga**, juntas finíssimas, frisos com relevos de deuses e criaturas aladas meio apagados, e incrustações de lápis-lazúli e ouro quase intactas.

### Props
| Item | Qtd. | Notas |
| --- | --- | --- |
| Guincho com cabine, tambor de cabo e plataforma elevatória | 1 | Herói da superfície |
| Kit de andaime (módulos de 2 m) | kit | Escadas, plataformas, guarda-corpos |
| Holofotes presos ao andaime, cabos descendo | 8–10 | Luz branca e fria |
| Equipamento abandonado da Fundação (caixas, mochila, câmera, tripé) | vários | Sinais de que algo deu errado lá embaixo |
| **Porta de pedra com o Selo de Kur** | 1 | Herói máximo: disco central com relevo de estrela de oito pontas e anéis de cuneiforme; linhas de lápis-lazúli que **brilham em azul**, pulsando devagar |
| Relevos da antecâmara | 3–4 painéis | Deuses com coroas de chifres, criaturas aladas |

### Luz e atmosfera
- No topo: luz do dia entrando. Na descida: holofotes brancos da Fundação. No fundo: **o azul do Selo toma conta**, frio e bonito.
- Partículas: poeira caindo do alto, gotas de água, partículas azuis bem finas flutuando perto do Selo.
- Sensação: maravilha e ameaça. É a primeira vez que o jogo mostra cor "impossível".

### Som ambiente
Vento uivando na boca do poço, metal rangendo, gotejamento, e um zumbido grave e baixo vindo do Selo, que cresce conforme a Elena se aproxima.

### Ordem de produção
1. Kit de andaime. 2. Parede do poço em camadas. 3. Guincho e superfície. 4. Antecâmara e relevos. 5. Selo de Kur com shader de brilho. 6. Luz e som.

---

## Área 6 — Wadi e saída

### Função
A fuga final da região: túnel dos Guardiões sai num leito de rio seco, a Elena corre pela tempestade de areia até a caminhonete do Samir. Tempo: 5–8 min. Fecha a região com adrenalina e beleza.

### Layout (manter o blockout)
- **Túnel dos Guardiões**: passagem estreita e antiga, com marcas dos Guardiões, saindo por uma rampa até o wadi.
- **Wadi**: leito seco e sinuoso, com margens altas de terra erodida, pedras e arbustos secos.
- No alto da margem, no fim, a **caminhonete do Samir**.

### Materiais
- Túnel: rocha e tijolo antigo, com **entalhes do barco de junco sobre ondas** (símbolo dos Guardiões) e marcas de tocha.
- Wadi: chão de lama seca rachada em placas, cascalho, margens com camadas erodidas, raízes expostas.

### Props
| Item | Qtd. | Notas |
| --- | --- | --- |
| Kit de túnel (seções retas, curvas, rampa de saída) | kit | |
| Entalhes e símbolos dos Guardiões | 4–6 | Decals e relevos |
| Lamparinas antigas de barro, apagadas | 3–4 | |
| Pedras grandes, arbustos secos, troncos mortos | muitos | Coberturas e silhuetas |
| Carcaça de carro velho enterrada | 1 | |
| Caminhonete do Samir (velha, branca com ferrugem) | 1 | Herói final |

### Luz e atmosfera
- **Fim de tarde com tempestade de areia laranja e clara**: o sol vira um disco pálido, a visibilidade cai, tudo ganha tom laranja e dourado. Bonito, não escuro.
- Névoa volumétrica de areia, rajadas de partículas passando pela tela, arbustos e areia sendo carregados.
- Os faróis da caminhonete do Samir cortando a tempestade são o guia visual do jogador.

### Som ambiente
Vento rugindo, areia batendo, helicóptero procurando acima da tempestade, motor da caminhonete ligado.

### Ordem de produção
1. Kit do túnel. 2. Terreno e margens do wadi. 3. Pedras, arbustos e carcaça. 4. Caminhonete. 5. Tempestade de areia (névoa, partículas, vento). 6. Luz e som.

---

## Ordem geral sugerida para a região 1

1. **Acampamento** (primeira impressão do jogo).
2. **Trincheiras**.
3. **Aldeia**.
4. **Zigurate e base da Fundação**.
5. **Boca do Poço**.
6. **Wadi e saída**.

Cada área: plano → kit → montagem no lugar das caixas → luz e som → renders para o dono aprovar → teste no Play → ESTADO.md, commit e push.

## Perguntas em aberto para o dono
- A sequência de horários (amanhecer até fim de tarde) está aprovada?
- Os cartazes e murais oficiais rasgados de 2003 na Aldeia entram ou ficam de fora?
- Galinhas e cabras na Aldeia: entram agora ou ficam para o polimento?
