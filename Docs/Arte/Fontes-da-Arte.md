# Fontes da arte

De onde vem cada peça de arte do jogo. Vale acima da seção 0.4 do `Docs/Arte/Regiao1_SitioDeUr_Arte.md`. A lista peça por peça da região 1, com a mesma Fonte e Situação, está em `Docs/LISTA_OBJETOS_3D.md`.

**Regra geral:** nada da Asset Store e nada pago. Há três caminhos sem IA de geração, feitos pelo Claude Code:
1. **Blender por script**, o método do kit das casas da vitrine (`ArteFonte/Ambientes/SitioDeUr/Aldeia/Blender/kit_casas_barro.py`).
2. **Bibliotecas CC0** (Poly Haven e ambientCG), para texturas e alguns modelos.
3. **Recursos da Unity**: Terrain, luz, efeitos e água.

O que precisa de IA de geração é feito pelo dono, no **Tripo** (objetos) e no **Meshy** (personagens).

**Situação** (conferida nas pastas em 3/nov/2026, não só pela lista combinada): **no jogo** (a arte já está na cena ou no prefab em uso), **gerado** (o modelo existe na `ArteFonte`, mas ainda não está no jogo), **pendente** (ainda não existe) e **a avaliar** (falta decidir o caminho).

**Convenção de pastas:** cenário e peças fixas em `ArteFonte/Ambientes/<Objeto>`; itens e armas em `ArteFonte/Objetos/<Item>`; personagens em `ArteFonte/Personagens/<Nome>`. Dentro de cada uma, `1_Meshy/` com o bruto (fora do Git) e `Referencias/` (no Git). Nomes sem acento nem ç.

## Feito pelo Claude Code, sem IA de geração

### Terreno e chão

| Peça | Caminho | Situação |
| --- | --- | --- |
| Terreno da região (dunas, desníveis, leito do wadi, margens) | Terrain da Unity | pendente |
| Texturas de areia, terra batida, barro rachado, rocha e cascalho | CC0 (Poly Haven, ambientCG) | no jogo só na vitrine da Aldeia (reboco, chão, tijolo, piso, madeira, metal); resto pendente |
| Trilhas, marcas de pneu e manchas | Decalques | pendente |

### Arquitetura (Blender por script)

| Peça | Situação |
| --- | --- |
| Casas de barro da Aldeia, muros, portas e janelas turquesa (o kit já existe) | no jogo só na vitrine (casa do altar, Casa_6 e casas 1 a 5); resto pendente |
| Mesquita, cúpula e minarete | pendente |
| Barracas do mercado, toldos e varais | pendente |
| Zigurate em módulos (terraços, escadarias, tijolos e desgaste) | pendente |
| Trincheiras de escavação (camadas de terra, balks e fundo) | pendente |
| Passarelas de tábua, escadas e rampas | pendente |
| Andaimes do Poço (lances, patamares, guarda-corpos, vigas do guincho) | pendente |
| Paredes do Poço em camadas de rocha | pendente |
| Antecâmara (pedra lisa, frisos, colunas e plataformas) | pendente |
| Túnel da Passagem dos Guardiões | pendente |
| Cabana de junco e casas da memória de Shuruppak | pendente |

### Acampamento e base da Fundação

| Peça | Situação |
| --- | --- |
| Tendas (da Elena, da equipe, de triagem, de comando, médica, de provas, alojamento), com lonas e cordas | pendente |
| Lonas de sombra sobre as trincheiras | pendente |
| Contêineres | pendente |
| Sacos de areia dos postos do atirador | pendente |
| Cercas, gabiões, barreira de cavaletes, cones e biruta | pendente |
| Portão da base e painel com botão | pendente |
| Heliponto (pintura no chão) | pendente |
| Antena alta do rádio | pendente |
| Gerador simples | pendente |
| **Holofote com tripé**, inclusive os que caem na arena | pendente |
| Mesas, catre, cadeiras dobráveis e caixotes de madeira | pendente |
| Bolsa vazia da câmera do Tom | pendente |
| Kits médicos (3), com **cruz branca em fundo verde** (nada de cruz vermelha: é emblema protegido) | pendente |
| Gaiolas de artefatos e mesas do laboratório de campo | pendente |
| Fogueira (pedras e lenha; o fogo é efeito) | pendente |

### Objetos pequenos de cena

| Peça | Caminho | Situação |
| --- | --- | --- |
| Jarros, potes e cântaros de barro | Torno no Blender | pendente |
| Tapetes e esteiras | Plano com textura | no jogo só a esteira do Yusuf e o tapete kilim da vitrine; resto pendente |
| Bicicleta simples, baldes e cordas | Blender por script | pendente |
| Papéis, e-mail do Kast, foto de 1979, documentos e cartazes | Planos com textura desenhada pelo Claude Code | pendente |
| Inscrições cuneiformes nas paredes, inclusive a escondida que só aparece com o Eco | Decalque ou relevo | pendente |

### Natureza

| Peça | Caminho | Situação |
| --- | --- | --- |
| Pedras e rochas | CC0 (Poly Haven) | pendente |
| Juncos e capim seco | Cartões de vegetação com textura CC0 | pendente |
| Rio da memória de Shuruppak | Shader de água da Unity | pendente |
| Palmeiras | Blender por script (tronco segmentado e folhas em cartões); se a qualidade não convencer, candidatas ao Tripo | a avaliar (hoje em blockout, com cores discretas na vitrine) |

### Efeitos, luz e atmosfera (Unity)

| Peça | Situação |
| --- | --- |
| Chama da lamparina, fogueira e tochas | no jogo a chama provisória da lamparina; o resto pendente |
| Brilho de lápis-lazúli dos poderes e do Selo | pendente (o Selo de Kur já tem o modelo, sem o brilho) |
| Poeira, partículas no ar e areia levantada | pendente |
| Tempestade de areia do wadi | no jogo (provisória: névoa e partículas simples) |
| Laser do atirador | no jogo (provisório) |
| Céu, neblina, pós-processamento e luz de cada área | no jogo o céu da região e a luz da vitrine; resto pendente |
| Brilho de item coletável | pendente |

## Feito pelo dono no Tripo (malha Smart, FBX com textura)

| Lote | Peça | Pasta | Situação |
| --- | --- | --- | --- |
| 1 | M1 altar | `Ambientes/Altar` | no jogo (vitrine, etapa 5) |
| 1 | M9 poço | `Ambientes/PocoM9` | no jogo (vitrine, etapa 5) |
| 1 | Lamparina de barro | `Objetos/LamparinaDeBarro` | no jogo (vitrine, etapa 5) |
| 1 | Espingarda de cano duplo | `Objetos/Espingarda` | gerado (só prefab; entra no B6) |
| 1 | Fuzil dos soldados (AKM modernizado preto, red dot) | `Objetos/Fuzil` | no jogo |
| 1 | Pistola Makarov | `Objetos/Pistola` | no jogo |
| 1 | Faca de campo | `Objetos/Faca` | no jogo |
| 1 | Tabuleta do Junco (barro com borda de ouro; ao ser lida, acende em azul pelas rachaduras e inscrições, um efeito da Unity ainda pendente) | `Objetos/TabuletaDeJunco` | no jogo |
| 2 | Rádio do acampamento (também o rádio de campanha e o rádio do wadi) | `Objetos/Radio` | no jogo |
| 2 | Gravador com a fita de Irena | `Objetos/Gravador` | gerado no **Meshy**, não no Tripo (só prefab; a fita, o cassete, não tem modelo) |
| 2 | Rolo de filme (um modelo, usado 3 vezes) | `Objetos/RoloFilme` | no jogo (gerado no **Meshy**) |
| 2 | Curativo | `Objetos/Curativo` | no jogo (gerado no **Meshy**) |
| 2 | Caixa de munição 9 mm | `Objetos/CaixaMunicao` | no jogo (gerada no **Meshy**) |
| 2 | Antibióticos | `Objetos/Antibioticos` | no jogo (gerados no **Meshy**) |
| 3 | Selo de Kur | `Ambientes/SeloDeKur` | no jogo |
| 3 | Helicóptero da Fundação | `Ambientes/Helicoptero` | no jogo |
| 3 | Painel de relevo do deus Anunnaki | — | pendente |
| 3 | Painel do barco de junco | — | pendente |
| 3 | Caminhonete do Samir | — | pendente |
| 3 | Caminhão (cerco e acampamento) | — | pendente |
| 3 | Guincho do Poço com cabine | — | pendente |

**Do Tripo faltam só:** o painel de relevo do deus, o painel do barco de junco, a caminhonete do Samir, o caminhão e o guincho. O painel das criaturas aladas é opcional (só se sobrar crédito). O holofote é feito no Blender, por script.

Os créditos do Tripo e do Meshy vencem em 30/11 (data externa do serviço, não é meta do projeto).

A bolsa da câmera do Tom e os kits médicos são feitos no Blender, por script (decisão do dono, 3/nov/2026).

**Plano usado nos 13 modelos novos:** Tripo **Pro** e Meshy **Pro** (pagos; confirmado pelo dono em 3/nov/2026).

## Feito pelo dono no Meshy (personagens)

| Personagem | Situação |
| --- | --- |
| Elena, Samir, Yusuf, Umm Karim, Dra. Weiss, Soldado, Líder de esquadra, Atirador de elite, Piloto, Lullu, Primeiro Servo | no jogo |
| Arqueólogo veterano, arqueóloga jovem, trabalhador da escavação (equipe morta) | no jogo: o corpo do trabalhador executado e os 3 corpos do círculo (um de cada modelo, deitados, poses do Mixamo; prefabs `EquipeMorta_*`) |
| Ninsar | prévia na Unity (cena `Teste_Ninsar`); manto pausado |
| Tom | pendente |
| Povo de Shuruppak (menina, pescador, Ziusudra), se sobrar crédito | pendente |
| Abu Faris (variação do aldeão base) | pendente |
