# Lista de objetos 3D da região 1 (Sítio de Ur)

Levantamento de 25 de outubro de 2026, feito a partir de `Docs/ESTADO.md`, `Docs/Regiao-1-Sitio-de-Ur.md`, `Docs/Arte/Regiao1_SitioDeUr_Arte.md` e dos scripts de blockout e de conteúdo (`SitioDeUrBlockout.cs`, `CampContent.cs`, `TrenchContent.cs`, `VillageContent.cs`, `ZigguratContent.cs`, `WellContent.cs`, `FirstServantContent.cs`, `ShuruppakMemoryContent.cs`). Nada no projeto foi alterado.

**Atualizado em 3/nov/2026:** colunas **Fonte** (quem faz: Blender por script, CC0, Unity, Tripo ou Meshy, conforme `Docs/Arte/Fontes-da-Arte.md`) e **Situação**, conferida nas pastas `ArteFonte` e na cena: **no jogo** (a arte já substitui o blockout), **gerado** (o modelo existe na `ArteFonte` ou em `Prefabs`, mas ainda não está na cena), **pendente** (ainda não existe) e **a avaliar** (falta decidir o caminho). A coluna "Forma hoje" ficou como era em 25/out: onde a Situação diz "no jogo", a forma simples já foi trocada.

**Como ler:** "Demo" é a fatia vertical (os passos 1 a 15 da missão "Sobrevivente" mais as duas secundárias).
- **Sim** = o jogador precisa ver ou usar o objeto para a demo funcionar e ser entendida.
- **Não essencial** = a demo roda com a forma simples; fica para o polimento.
- **Opcional** = só se sobrar tempo.

O que é "provisório" aqui: tudo que ainda é cubo, cilindro, esfera ou cápsula com material de blockout (`BO_...`). Os personagens (Elena, Samir, 4 soldados, Lullu e Primeiro Servo) já têm modelo definitivo e **não** entram nas tabelas de provisórios. O fuzil e a faca que eles carregam já são os modelos do Tripo (3/nov/2026).

---

## 1. Resumo para quem planeja

- **Modelos que a demo exige (sem eles o jogo não se explica):** Tabuleta do Junco, faca de campo, pistola Makarov, caixa de munição, curativo, rádio do acampamento, rolos de filme (3), bolsa da câmera do Tom, antibióticos, documentos/foto de 1979, fita de Irena, Selo de Kur, helicóptero, caminhão da Fundação, caminhonete do Samir, holofote do Primeiro Servo, fuzil dos soldados.
- **Arquitetura que dá a cara de cada área** (essencial pela leitura, mas pode ser feita em kit simples): zigurate, casas da Aldeia, tendas, trincheiras, andaimes do Poço, antecâmara.
- **Já no jogo com arte (3/nov/2026):** Tabuleta do Junco, faca, pistola, caixa de munição, curativo, rádio, rolos de filme, antibióticos, Selo de Kur, helicóptero e fuzil. Ainda faltam: bolsa da câmera do Tom, documentos e foto de 1979, fita de Irena (o gravador existe, só como prefab), caminhão, caminhonete do Samir e holofote.
- Os passos 13 a 15 já têm conteúdo em blockout na cena (o retrato de 25/out dizia que não).

---

## 2. Objetos provisórios (blockout) por área

Todos são gerados pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (cena `Regiao1_SitioDeUr`, grupo `Blockout_SitioDeUr`) ou pelos menus de **Nibiru > Conteúdo** (grupos `Conteudo_...`). Substituir peça por peça, mantendo posições (regra de ouro do guia de arte).

### 2.1 Chão e terreno

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| `Chao_Sul`, `Chao_OestePoco`, `Chao_LestePoco`, `Chao_Meio`, `Chao_NorteOeste`, `Chao_NorteLeste`, `Chao_FimDaRampa` | Lajes planas de areia | Região inteira | Sim (vira Terrain) | Unity: Terrain + texturas CC0 | pendente |

### 2.2 Área 1: Acampamento (passos 1 a 4)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| `Elevacao` | Caixa de 3 m de altura | Base de todo o acampamento | Sim | Unity: Terrain | pendente |
| `Rampa_Descida` | Rampa (fechada por parede invisível até a execução) | Descida para as trincheiras | Sim | Unity: Terrain | pendente |
| `Tenda_Elena` | Tenda oca laranja com porta | Início do jogo (passo 1) | Sim (interior com catre, mala, lampião é desejável) | Blender por script | pendente |
| `Tenda_Equipe_1`, `_2`, `_3` | Caixas | Pátio do acampamento, onde estão os corpos | Sim | Blender por script | pendente |
| `Tenda_Triagem`, `Mesa_Triagem_1`, `Mesa_Triagem_2` | Caixas | Acampamento | Não essencial | Blender por script | pendente |
| `Caminhao_1`, `Caminhao_2` | Caixas | Acampamento (o rolo nº 1 fica atrás de um) | Sim | Tripo (dono), lote 3 | pendente |
| `Gerador` | Caixa | Acampamento | Não essencial | Blender por script | pendente |
| `Antena` | Cilindro de 12 m | Acampamento (referência vertical e rádio) | Sim (marca visual) | Blender por script | pendente |
| `Fogueira` | Cilindro baixo | Centro do pátio | Não essencial | Blender por script + efeito da Unity | pendente |
| `Heliponto` | Disco branco | Abaixo da elevação (passo 4) | Sim | Blender por script (pintura e cones) | pendente |
| `Ponto_Inicio_Elena` | Marcador | Dentro da `Tenda_Elena` | Sim (só ponto lógico, não é modelo) | Lógico, sem modelo | — |
| Corpos da equipe (7) | Cápsulas/formas de conteúdo | Pátio (passo 2) | Sim (3 modelos: veterano, jovem, trabalhador; já na ArteFonte) | Meshy (dono) (arqueólogo veterano, arqueóloga jovem, trabalhador) | gerado |
| `Trabalhador_Ferido` (Hassan) | Cápsula | Executado no passo 4 | Sim (usa o modelo do trabalhador) | Meshy (dono) | gerado |
| `Catre` | Cubo | Dentro da `Tenda_Elena` | Não essencial | Blender por script | pendente |
| `Caixotes_Mirante_1`, `_2` | Cubos de madeira | Mirante nordeste (cobertura) | Não essencial | Blender por script | pendente |
| Helicóptero do acampamento (cabine, vidro, banco, hélice) | Formas simples (`ContentKit.Helicopter`), piloto já com modelo | Chega após o rádio; pousa no heliponto | Sim (herói) | Tripo (dono), lote 3 | no jogo |
| Caminhonetes (2) e biruta, cones | Previstas no guia de arte, ainda sem blockout | Beira da elevação, heliponto | Não essencial | Tripo (dono) (caminhonetes, lote 3); Blender por script (biruta, cones) | pendente |

### 2.3 Área 2: Trincheiras (passo 5)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| Paredes e fundo das trincheiras (grade com balks) | Caixas | Área 2 inteira | Sim (kit modular, camadas de terra) | Blender por script | pendente |
| `Passarela_1`, `Passarela_2` | Caixas de tábua | Sobre as trincheiras | Sim | Blender por script | pendente |
| `Escada_Passarela_1`, `Escada_Passarela_2` | Rampas | Pé das passarelas | Sim | Blender por script | pendente |
| `Monte_Terra_1`, `Monte_Terra_2` | Cilindros | Cobertura | Não essencial | Unity: Terrain ou Blender por script | pendente |
| `Tenda_Medica_Escondida` | Caixa branca (4 x 2,5 x 3 m) | Oeste, esconderijo da Dra. Weiss | Sim | Blender por script | pendente |
| Lonas de sombra e postes (`Lona`, `Poste_1..n`) | Cubo fino e cilindros | Sobre algumas trincheiras (escondem do helicóptero) | Sim (mecânica de furtividade) | Blender por script | pendente |
| `Dra_Weiss` | Cápsula | Atrás da tenda médica | Sim (modelo já pronto na ArteFonte) | Meshy (dono) | no jogo |
| `Mochila_Medica` | Cubo turquesa | Perto da Dra. Weiss | Não essencial | Blender por script | pendente |
| Helicóptero que passa baixo (2º) | Mesmo kit do acampamento | Passa depois da pistola | Sim | Tripo (dono), lote 3 | no jogo |
| `Corpo_ChefeSeguranca_Pistola` | Marcador | Centro-leste | Sim (corpo = modelo de arqueólogo/segurança) | Meshy (dono) (reusar um modelo de personagem) | pendente |

### 2.4 Área 3: Aldeia (passos 6 a 9)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| Casas de tijolo de barro, portas turquesa, muros | Caixas (adobe) | Toda a Aldeia | Sim (kit de casas) | Blender por script: kit das casas | no jogo (só a vitrine) |
| `CasaAltar_Fundo`, `_Oeste`, `_Leste`, `_Teto` | Caixas | Casa do altar (passos 7 a 9) | Sim | Blender por script: kit das casas | no jogo |
| `Nicho_Guardioes` | Caixa dourada | Fundo da casa do altar; altar de salvamento e leitura da tabuleta | Sim (herói) | Tripo (dono), lote 1 (M1 altar) | no jogo |
| Entalhe do barco de junco sobre ondas (`Casco`, `Proa`, `Popa`, `Feixe_Junco`, `Onda_*`) | Cubos finos | Parede do nicho | Sim (símbolo dos Guardiões; decal ou relevo) | Tripo (dono), lote 3 (painel do barco) | pendente |
| Lamparina de barro (`Pires`, `Bico`, `Fogo`) | Esfera e cilindro | Nicho, com luz quente | Sim | Tripo (dono), lote 1 | no jogo |
| `Inscricao_Escondida` (`Cunha_*`) | Cubos pequenos | Parede do fundo, só aparece com o Eco | Sim (mecânica do Eco) | Claude Code: decalque ou relevo | pendente |
| `Poco_Agua` | Cilindro | Pátio central | Não essencial (herói do pátio) | Tripo (dono), lote 1 (M9 poço) | no jogo |
| `Barraca_Mercado_1`, `_2` | Caixas | Mercado | Não essencial | Blender por script | pendente |
| `Mesquita`, `Mesquita_Cupula`, `Minarete` | Caixa, esfera e cilindro | Fundo oeste | Não essencial (silhueta ajuda) | Blender por script | pendente |
| `Esteira_Yusuf` | Cubo achatado laranja | Dentro da casa do altar | Sim | Claude Code: textura própria | no jogo |
| `Umm_Karim`, Yusuf | Cápsula (Yusuf já em esteira) | Casa do altar | Sim (modelos prontos na ArteFonte) | Meshy (dono) | no jogo |
| `Casa_Trancada_Espingarda` | Marcador | Oeste (item opcional) | Opcional | Blender por script: kit das casas | pendente |
| Palmeiras, jarros, tapetes, varais, bicicletas | Previstos no guia de arte, sem blockout | Aldeia | Não essencial | Blender por script (jarros, tapetes, varais, bicicleta); palmeiras por script ou Tripo (dono) | a avaliar |
| Caminhão branco do cerco (`Cabine`, `Carroceria`, `Faixa`, `Roda`) | Cubos e cilindros | Estrada norte, passo 9 | Sim | Tripo (dono), lote 3 | pendente |
| Barreira de cavaletes (`Perna_E`, `Perna_D`, `Barra_Alta`, `Barra_Baixa`) | Cubos | Z = 41,5, passo 9 | Sim | Blender por script | pendente |
| Helicóptero que pousa na base | Mesmo kit | Passo 9 | Sim | Tripo (dono), lote 3 | no jogo |
| Soldado do encontro, corpo caído | Já com modelo de soldado | Entradas da Aldeia | Sim | Meshy (dono) | no jogo |

### 2.5 Área 4: Zigurate e base da Fundação (passo 10)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| `Zigurate_Terraco_1`, `_2`, `_3` | Três caixas escalonadas | Centro; domina a região | Sim (landmark; feito em módulos) | Blender por script: módulos | pendente |
| `Escadaria_1`, `Escadaria_2` | Rampas | Face sul do zigurate | Sim | Blender por script: módulos | pendente |
| `Posto_Atirador_Frente`, `_Lado_Oeste`, `_Lado_Leste`, `_Caixa` | Caixas oliva (sacos de areia) | 2º terraço | Sim (o atirador de elite é central ao passo 10) | Blender por script (sacos de areia) | pendente |
| `Tenda_Comando` | Tenda oca branca com porta | Avenida central, documentos do Kast | Sim | Blender por script | pendente |
| `Toldo_Tenda_Comando`, `Toldo_Poste_Sul`, `Toldo_Poste_Norte` | Caixa e cilindros | Porta da tenda de comando (esconde do atirador) | Sim | Blender por script | pendente |
| `Tenda_Medica` | Tenda oca branca | Oeste da base (antibióticos) | Sim | Blender por script | pendente |
| `Tenda_Provas` | Tenda oca branca | Leste (3º rolo do Tom) | Sim | Blender por script | pendente |
| `Tenda_Alojamento` | Caixa branca | Oeste | Não essencial | Blender por script | pendente |
| Seis contêineres (cobertura de furtividade) | Caixas | Sul da avenida | Sim | Blender por script | pendente |
| `Heliponto_Base` | Disco branco | Leste da base | Sim | Blender por script (pintura) | pendente |
| `Cerca_Sul`, `Cerca_Leste`, `Muralha_Sul`, `Muralha_Norte`, `Muralha_Canto` | Caixas | Perímetro da base | Sim (limites) | Blender por script (cercas, gabiões, muros) | pendente |
| `Portao_Aldeia` (`Folha`, `Faixa`, `Travessa`) | Cubos brancos e turquesa | Muro oeste, abre pelo painel | Sim | Blender por script | pendente |
| Rota alta: `Rota_Alta_Escada_Muro`, `_Passarela_Portao`, `_Rampa_Terraco_1`, `_Patamar`, `_Rampa_Terraco_2` | Rampas e caixas de madeira | Muro e face oeste do zigurate | Sim (rota alternativa; andaime de kit) | Blender por script (andaime) | pendente |
| `Mesa_Kast`, `Papeis`, `Foto_1979`, `Radio_Mesa` | Cubos | Dentro da tenda de comando | Sim (documentos e foto são centrais) | Blender por script (mesa); papéis e foto: planos com textura; rádio: Tripo (dono) | pendente (o rádio já está no jogo) |
| `Painel_Portao`, `Botao_Portao` | Cubos | Parede norte da tenda de comando | Sim | Blender por script | pendente |
| `Mesa_Tenda_Medica`, `Antibioticos` | Cubos | Tenda médica | Sim (secundária "Remédio para Yusuf") | Blender por script (mesa); antibióticos: Meshy (dono) | pendente (os antibióticos já estão no jogo) |
| `Mesa_Tenda_Provas`, `Rolo_De_Filme_3` | Cubo e cilindro | Tenda de provas | Sim (secundária "O que o Tom viu") | Blender por script (mesa); rolo de filme: Meshy (dono) | pendente (o rolo já está no jogo) |
| `Ponto_Laser` | Esfera pequena | Mira do atirador | Sim (efeito, não modelo) | Efeito da Unity | pendente |
| Gaiolas de artefatos, laboratório de campo, gabiões, torres de holofote, utilitários | Previstos no guia de arte, sem blockout | Base | Não essencial | Blender por script | pendente |

### 2.6 Área 5: Boca do Poço e antecâmara (passos 11 e 12)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| `Poco_Parede_Sul/Oeste/Leste/Norte_Alta`, `Poco_Fundo`, `Poco_Canto_*` | Caixas de rocha | Poço de 14 m | Sim (parede em camadas) | Blender por script | pendente |
| `Andaime_Viga_Sul`, `Andaime_Viga_Norte`, `Guincho` | Caixas metálicas | Boca do Poço, superfície | Sim (guincho com cabine é herói da superfície) | Blender por script (vigas); guincho com cabine: Tripo (dono), lote 3 | pendente |
| `Lance_1` a `Lance_4`, `Lance_4_Guarda`, `Patamar_3_Base` | Rampas de madeira | Descida de andaimes | Sim (kit de andaime) | Blender por script (andaime) | pendente |
| `Holofote_Fundacao_Superficie` | Caixa branca | Superfície do Poço | Não essencial | Blender por script (holofote com tripé) | pendente |
| `Ante_Chao`, `Ante_Teto`, `Ante_Parede_*`, `Ante_Canto_*` | Caixas de rocha | Antecâmara (arena do Primeiro Servo) | Sim (pedra lisa antiga com frisos) | Blender por script | pendente |
| `Plataforma_Oeste`, `Plataforma_Leste` e rampas (4) | Caixas e rampas de madeira | Arena do chefe (passo 12) | Sim (andaime) | Blender por script (andaime) | pendente |
| `Porta_Selo_de_Kur`, `Selo_Ouro` | Caixas lápis-lazúli e ouro | Fundo da antecâmara | Sim (herói máximo da região) | Tripo (dono), lote 3; brilho azul: efeito da Unity | no jogo (o brilho pendente) |
| `Parede_Barco_Junco`, `Barco_Casco`, `Barco_Mastro` | Caixas de rocha e ouro | Parede oeste da antecâmara (passo 13 abre) | Sim | Blender por script (parede); barco: Tripo (dono), lote 3 | pendente |
| Holofotes que caem (`Poste`, `Pe`, `Holofote`) | Cilindro e cubos | 4 na arena do chefe | Sim (mecânica do chefe) | Blender por script (holofote com tripé) | pendente |
| Caixas, mochila e equipamento abandonado da Fundação | Previstos, sem blockout | Descida e antecâmara | Não essencial | Blender por script e CC0 | pendente |
| Relevos da antecâmara (deuses e criaturas aladas, 3 a 4 painéis) | Previstos, sem blockout | Antecâmara | Não essencial | Tripo (dono), lote 3 (deus); criaturas aladas: opcional | pendente |

### 2.7 Área 6: Passagem dos Guardiões e Wadi (passos 13 a 15)

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| `Tunel_Chao_Entrada`, `Tunel_Chao`, `Tunel_Teto`, `Tunel_Parede_*` | Caixas de rocha | Passagem dos Guardiões (passo 13) | Sim | Blender por script | pendente |
| `Tunel_Rampa_Saida`, `Rampa_Parede_Oeste/Leste` | Rampa e caixas de terra | Saída para o wadi | Sim | Blender por script | pendente |
| `Wadi_Margem_Oeste`, `Wadi_Margem_Leste` | Caixas de terra | Wadi (passo 14) | Sim | Unity: Terrain | pendente |
| `Caminhonete_Samir` | Caixa de areia (2,2 x 2 x 5 m) | Fim do wadi (passo 15) | Sim (herói final) | Tripo (dono), lote 3 | pendente |
| `Marcador Escolha_de_Faccao` | Marcador | Wadi | Sim (ponto lógico) | Lógico, sem modelo | — |
| Pedras, arbustos, carcaça de carro, tocha/lamparinas, entalhes | Previstos, sem blockout | Passagem e wadi | Não essencial | CC0 (pedras) e cartões de vegetação; carcaça e lamparinas: Blender por script e Tripo (dono) (reuso) | pendente |

### 2.8 Memória de Shuruppak (passo 8, cena `Memoria_Shuruppak`)

Tudo em formas simples (`ShuruppakMemoryContent.cs`). A memória dura 2 a 3 minutos e é um ponto alto da demo.

| Nome | Forma hoje | Onde aparece | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| Casas de barro com toldos e panos coloridos | Caixas | Rua de Shuruppak | Sim (reuso do kit de casas) | Blender por script: kit das casas | pendente |
| Mercado, palmeiras, juncos, rio | Caixas, cilindros, planos | Rua e margem | Sim (versão simples) | Blender por script, cartões CC0 e água da Unity; palmeiras por script ou Tripo (dono) | a avaliar |
| Zigurate de topo dourado ao fundo | Caixas | Do outro lado do rio | Sim (reuso do módulo do zigurate) | Blender por script: módulos | pendente |
| Cabana de junco com parede do fundo | Caixas | Fim da rua | Sim | Blender por script | pendente |
| Menina, pescador, Ziusudra (avatar) | Cápsulas | Rua e cabana | Sim (versão simples; cápsula com túnica aceitável na demo) | Meshy (dono) | pendente |

---

## 3. Itens coletáveis e interativos

### 3.1 Já implementados (aparecem como forma simples)

| Item / objeto | Onde aparece | Passo | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| **Tabuleta do Junco** (`Pedra`, `Linha_Ouro_*`) | Mão da Elena; lida no nicho (passo 8) | 1, 8 | Sim (herói) | Tripo (dono), lote 1 | no jogo |
| **Faca de campo** (`Faca_De_Campo`, `Caixa_Faca`) | Caixa na barraca da Elena | 1 | Sim | Tripo (dono), lote 1 (a caixa: Blender por script) | no jogo |
| **Bolsa vazia da câmera do Tom** (`Bolsa_Camera_Tom`) | Frente da barraca do Tom | 2 | Sim (inicia "O que o Tom viu") | Blender por script ou Tripo (dono) | a avaliar |
| **Corpos da equipe** | Pátio do acampamento | 2 | Sim | Meshy (dono) | gerado |
| **Rádio do acampamento** (`Caixa_Radio`, `Aparelho_Radio`) | Mesa sob toldo | 3 | Sim (herói, luz vermelha) | Tripo (dono) | no jogo |
| **Rolo de filme 1** (`Rolo_De_Filme_1`) | Atrás do caminhão | 2 | Sim | Meshy (dono) | no jogo |
| **Rolo de filme 2** (`Rolo_De_Filme_2`) | Canto nordeste das Trincheiras | 5 | Sim | Meshy (dono) | no jogo |
| **Rolo de filme 3** (`Rolo_De_Filme_3`) | Tenda de provas da base | 10 | Sim | Meshy (dono) | no jogo |
| **Pistola Makarov** (`Pistola_Makarov`) | Corpo do chefe de segurança | 5 | Sim | Tripo (dono), lote 1 | no jogo |
| **Munição 9 mm** (`Municao_ChefeSeguranca`, `Municao_Lider`, `Municao_Plataforma_Oeste`) | Chefe de segurança, líder das Trincheiras, arena do Servo | 5, 12 | Sim | Meshy (dono) | no jogo |
| **Rádio de campanha** (`Radio_De_Campanha`) | Largado pelo líder das Trincheiras | 5 | Não essencial (ainda não faz nada) | Tripo (dono) (o mesmo modelo do rádio) | no jogo |
| **Curativos** (`Curativo_1`, `_2`, `Curativo_Plataforma_Leste`, `Curativo_Boca_Antecamara`) | Trincheiras e arena do Servo | 5, 12 | Sim | Meshy (dono) | no jogo |
| **Mochila médica** (`Mochila_Medica`) | Perto da Dra. Weiss | 5 | Não essencial | Blender por script | pendente |
| **Antibióticos** (`Antibioticos`) | Tenda médica da base | 10 | Sim | Meshy (dono) | no jogo |
| **Documentos da Fundação**: e-mail do Kast e foto de 1979 (`Mesa_Kast`, `Papeis`, `Foto_1979`) | Tenda de comando | 10 | Sim | Claude Code: planos com textura | pendente |
| **Painel do portão** (`Painel_Portao`, `Botao_Portao`) | Tenda de comando | 10 | Sim | Blender por script | pendente |
| **Altar/nicho dos Guardiões** | Casa do altar | 7, 8 | Sim | Tripo (dono), lote 1 (M1) | no jogo |
| **Pontos de retorno (checkpoints)** | Vários | todos | Sim (lógicos, sem modelo) | Lógico, sem modelo | — |
| **Lamparina de barro** | Nicho | 7, 8 | Sim | Tripo (dono), lote 1 | no jogo |
| **Holofotes que caem** | Arena do Primeiro Servo | 12 | Sim | Blender por script | pendente |
| **Escrita escondida (Eco do Abzu)** | Parede do fundo da casa do altar | 8 | Sim | Claude Code: decalque ou relevo | pendente |
| **Selo de Kur** | Fundo da antecâmara | 11, 12 | Sim | Tripo (dono), lote 3 | no jogo |
| **Fuzil dos soldados** | Mão dos 4 soldados | todos | Sim (hoje formas simples, a pendência mais visível dos inimigos) | Tripo (dono), lote 1 | no jogo |
| **Faca na mão do Samir e da Elena** | Golpes | 6, 10 | Não essencial (lâmina curta provisória) | Tripo (dono), lote 1 | no jogo |
| **Esteira do Yusuf** | Casa do altar | 7 | Não essencial | Claude Code: textura própria | no jogo |

### 3.2 Previstos nos passos 1 a 15 e nas secundárias, ainda sem implementação

| Item / objeto | Onde está previsto | Passo | Demo | Fonte | Situação |
| --- | --- | --- | --- | --- | --- |
| **Fita de Irena nº 1** (gravador/fita) | Passagem dos Guardiões | 13 | Sim (item central; áudio com legenda) | Gravador: Meshy (dono); fita (cassete): Blender por script | gerado (o gravador, só prefab); fita pendente |
| **Inscrição dos Guardiões da Aldeia, do Poço e da passagem** (3 colecionáveis) | Aldeia, Poço, passagem | 7, 11, 13 | Opcional (a do Poço está listada como pendência) | Claude Code: decalque ou relevo | pendente |
| **Documentos da Fundação** (cartas, relatórios; total de 5) | Base e Trincheiras | 5, 10 | Opcional (só o e-mail e a foto existem; os outros 3 humanizam os soldados) | Claude Code: planos com textura | pendente |
| **Foto de Irena como item de inventário** | Tenda de comando | 10 | Opcional (hoje só tela de leitura) | Claude Code: plano com textura | pendente |
| **Fotos do Tom** (item-chave, recompensa) | Fotógrafo da Aldeia, quarto escuro | 7, secundária | Sim, se "O que o Tom viu" entrar na demo | Claude Code: planos com textura | pendente |
| **Fotógrafo / quarto escuro improvisado** | Aldeia | secundária | Opcional | Blender por script (quarto); Abu Faris: Meshy (dono) | pendente |
| **Kit médico** (3 na região) | Espalhados | 5 a 12 | Opcional | Blender por script ou Tripo (dono) | a avaliar |
| **Espingarda de cano duplo** e **cartuchos** (6) | Casa trancada da Aldeia | 7 | Opcional | Tripo (dono), lote 1 (cartuchos: Blender por script) | gerado (a espingarda, só prefab; entra no B6) |
| **Pólvora, cápsulas, gaze, álcool** (crafting) | Região | — | Não (crafting fora da fatia) | — | — |
| **Rádio do soldado ferido no wadi** | Wadi | 14 | Sim (dispara a escolha de facção) | Tripo (dono) (o mesmo modelo do rádio) | no jogo |
| **Tabuleta no chão do wadi** (entregar/enterrar/ficar) | Wadi | 14 | Sim | Tripo (dono), lote 1 | no jogo |
| **Caminhonete do Samir (interativa)** | Wadi | 15 | Sim | Tripo (dono), lote 3 | pendente |
| **Entrada do diário** | Fim da fatia | 15 | Opcional (diário fica de fora) | Interface | — |
| **Lamparinas apagadas e entalhes dos Guardiões na passagem** | Passagem | 13 | Não essencial | Tripo (dono), lote 1 (reuso da lamparina); entalhes: decalque | pendente |
| **Cartazes e murais rasgados de 2003** | Aldeia | — | Opcional (pergunta aberta ao dono) | Claude Code: planos com textura | pendente |
| **Galinhas e cabras** | Aldeia | — | Opcional (pergunta aberta ao dono) | — | a avaliar |

---

## 4. Ordem sugerida de substituição para a demo

1. **Itens de interação** (tabuleta, faca, pistola, rolo de filme, curativo, antibióticos, rádio, fita de Irena, fuzil): pequenos, rápidos, mudam muito a percepção.
2. **Selo de Kur e antecâmara**: o momento mais forte da demo.
3. **Veículos e helicóptero** (Tripo, lote 3; nada de pack da Asset Store): caminhões, helicóptero (já no jogo), caminhonete do Samir.
4. **Kits modulares**: casas da Aldeia, trincheiras, andaimes, zigurate.
5. **Polimento**: palmeiras, tendas com interior, props de escavação, relevos, cartazes.

## 5. Observações

- Os modelos dos arqueólogos e do trabalhador já estão na Unity (prefabs em `Prefabs/Personagens`). O corpo do trabalhador executado já usa o modelo; os 7 corpos do círculo seguem em blockout, **deitados** (decisão do dono em 3/nov/2026), esperando a troca. O Tom e o Abu Faris ainda não têm modelo.
- A lista de itens-chave do `Docs/Regiao-1-Sitio-de-Ur.md` prevê 5 documentos da Fundação e 3 inscrições; hoje há 2 documentos e nenhuma inscrição implementada.
- Esta lista não confere a cena aberta na Unity; vem dos scripts e dos documentos. Se algum objeto foi mudado à mão na cena (o que o projeto evita), ele pode divergir.
