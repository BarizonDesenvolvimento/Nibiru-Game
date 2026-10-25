# Lista de objetos 3D da região 1 (Sítio de Ur)

Levantamento de 25 de outubro de 2026, feito a partir de `Docs/ESTADO.md`, `Docs/Regiao-1-Sitio-de-Ur.md`, `Docs/Arte/Regiao1_SitioDeUr_Arte.md` e dos scripts de blockout e de conteúdo (`SitioDeUrBlockout.cs`, `CampContent.cs`, `TrenchContent.cs`, `VillageContent.cs`, `ZigguratContent.cs`, `WellContent.cs`, `FirstServantContent.cs`, `ShuruppakMemoryContent.cs`). Nada no projeto foi alterado.

**Como ler:** "Demo" é a fatia vertical (os passos 1 a 15 da missão "Sobrevivente" mais as duas secundárias).
- **Sim** = o jogador precisa ver ou usar o objeto para a demo funcionar e ser entendida.
- **Não essencial** = a demo roda com a forma simples; fica para o polimento.
- **Opcional** = só se sobrar tempo.

O que é "provisório" aqui: tudo que ainda é cubo, cilindro, esfera ou cápsula com material de blockout (`BO_...`). Os personagens (Elena, Samir, 4 soldados, Lullu e Primeiro Servo) já têm modelo definitivo e **não** entram nas tabelas de provisórios, exceto o que carregam de provisório (fuzil, faca).

---

## 1. Resumo para quem planeja

- **Modelos que a demo exige (sem eles o jogo não se explica):** Tabuleta do Junco, faca de campo, pistola Makarov, caixa de munição, curativo, rádio do acampamento, rolos de filme (3), bolsa da câmera do Tom, antibióticos, documentos/foto de 1979, fita de Irena, Selo de Kur, helicóptero, caminhão da Fundação, caminhonete do Samir, holofote do Primeiro Servo, fuzil dos soldados.
- **Arquitetura que dá a cara de cada área** (essencial pela leitura, mas pode ser feita em kit simples): zigurate, casas da Aldeia, tendas, trincheiras, andaimes do Poço, antecâmara.
- **Passo 13 (passagem dos Guardiões), passo 14 (wadi) e passo 15 (fuga) ainda não têm conteúdo na cena**: só o blockout do túnel, do wadi e da caminhonete. Os itens previstos deles estão na seção 4.

---

## 2. Objetos provisórios (blockout) por área

Todos são gerados pelo menu **Nibiru > Blockout > Construir Sítio de Ur na cena aberta** (cena `Regiao1_SitioDeUr`, grupo `Blockout_SitioDeUr`) ou pelos menus de **Nibiru > Conteúdo** (grupos `Conteudo_...`). Substituir peça por peça, mantendo posições (regra de ouro do guia de arte).

### 2.1 Chão e terreno

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| `Chao_Sul`, `Chao_OestePoco`, `Chao_LestePoco`, `Chao_Meio`, `Chao_NorteOeste`, `Chao_NorteLeste`, `Chao_FimDaRampa` | Lajes planas de areia | Região inteira | Sim (vira Terrain) |

### 2.2 Área 1: Acampamento (passos 1 a 4)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| `Elevacao` | Caixa de 3 m de altura | Base de todo o acampamento | Sim |
| `Rampa_Descida` | Rampa (fechada por parede invisível até a execução) | Descida para as trincheiras | Sim |
| `Tenda_Elena` | Tenda oca laranja com porta | Início do jogo (passo 1) | Sim (interior com catre, mala, lampião é desejável) |
| `Tenda_Equipe_1`, `_2`, `_3` | Caixas | Pátio do acampamento, onde estão os corpos | Sim |
| `Tenda_Triagem`, `Mesa_Triagem_1`, `Mesa_Triagem_2` | Caixas | Acampamento | Não essencial |
| `Caminhao_1`, `Caminhao_2` | Caixas | Acampamento (o rolo nº 1 fica atrás de um) | Sim (veículo de pack) |
| `Gerador` | Caixa | Acampamento | Não essencial |
| `Antena` | Cilindro de 12 m | Acampamento (referência vertical e rádio) | Sim (marca visual) |
| `Fogueira` | Cilindro baixo | Centro do pátio | Não essencial |
| `Heliponto` | Disco branco | Abaixo da elevação (passo 4) | Sim |
| `Ponto_Inicio_Elena` | Marcador | Dentro da `Tenda_Elena` | Sim (só ponto lógico, não é modelo) |
| Corpos da equipe (7) | Cápsulas/formas de conteúdo | Pátio (passo 2) | Sim (3 modelos: veterano, jovem, trabalhador; já na ArteFonte) |
| `Trabalhador_Ferido` (Hassan) | Cápsula | Executado no passo 4 | Sim (usa o modelo do trabalhador) |
| `Catre` | Cubo | Dentro da `Tenda_Elena` | Não essencial |
| `Caixotes_Mirante_1`, `_2` | Cubos de madeira | Mirante nordeste (cobertura) | Não essencial |
| Helicóptero do acampamento (cabine, vidro, banco, hélice) | Formas simples (`ContentKit.Helicopter`), piloto já com modelo | Chega após o rádio; pousa no heliponto | Sim (herói; pack de helicóptero) |
| Caminhonetes (2) e biruta, cones | Previstas no guia de arte, ainda sem blockout | Beira da elevação, heliponto | Não essencial |

### 2.3 Área 2: Trincheiras (passo 5)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| Paredes e fundo das trincheiras (grade com balks) | Caixas | Área 2 inteira | Sim (kit modular, camadas de terra) |
| `Passarela_1`, `Passarela_2` | Caixas de tábua | Sobre as trincheiras | Sim |
| `Escada_Passarela_1`, `Escada_Passarela_2` | Rampas | Pé das passarelas | Sim |
| `Monte_Terra_1`, `Monte_Terra_2` | Cilindros | Cobertura | Não essencial |
| `Tenda_Medica_Escondida` | Caixa branca (4 x 2,5 x 3 m) | Oeste, esconderijo da Dra. Weiss | Sim |
| Lonas de sombra e postes (`Lona`, `Poste_1..n`) | Cubo fino e cilindros | Sobre algumas trincheiras (escondem do helicóptero) | Sim (mecânica de furtividade) |
| `Dra_Weiss` | Cápsula | Atrás da tenda médica | Sim (modelo já pronto na ArteFonte) |
| `Mochila_Medica` | Cubo turquesa | Perto da Dra. Weiss | Não essencial |
| Helicóptero que passa baixo (2º) | Mesmo kit do acampamento | Passa depois da pistola | Sim |
| `Corpo_ChefeSeguranca_Pistola` | Marcador | Centro-leste | Sim (corpo = modelo de arqueólogo/segurança) |

### 2.4 Área 3: Aldeia (passos 6 a 9)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| Casas de tijolo de barro, portas turquesa, muros | Caixas (adobe) | Toda a Aldeia | Sim (kit de casas) |
| `CasaAltar_Fundo`, `_Oeste`, `_Leste`, `_Teto` | Caixas | Casa do altar (passos 7 a 9) | Sim |
| `Nicho_Guardioes` | Caixa dourada | Fundo da casa do altar; altar de salvamento e leitura da tabuleta | Sim (herói) |
| Entalhe do barco de junco sobre ondas (`Casco`, `Proa`, `Popa`, `Feixe_Junco`, `Onda_*`) | Cubos finos | Parede do nicho | Sim (símbolo dos Guardiões; decal ou relevo) |
| Lamparina de barro (`Pires`, `Bico`, `Fogo`) | Esfera e cilindro | Nicho, com luz quente | Sim |
| `Inscricao_Escondida` (`Cunha_*`) | Cubos pequenos | Parede do fundo, só aparece com o Eco | Sim (mecânica do Eco) |
| `Poco_Agua` | Cilindro | Pátio central | Não essencial (herói do pátio) |
| `Barraca_Mercado_1`, `_2` | Caixas | Mercado | Não essencial |
| `Mesquita`, `Mesquita_Cupula`, `Minarete` | Caixa, esfera e cilindro | Fundo oeste | Não essencial (silhueta ajuda) |
| `Esteira_Yusuf` | Cubo achatado laranja | Dentro da casa do altar | Sim |
| `Umm_Karim`, Yusuf | Cápsula (Yusuf já em esteira) | Casa do altar | Sim (modelos prontos na ArteFonte) |
| `Casa_Trancada_Espingarda` | Marcador | Oeste (item opcional) | Opcional |
| Palmeiras, jarros, tapetes, varais, bicicletas | Previstos no guia de arte, sem blockout | Aldeia | Não essencial |
| Caminhão branco do cerco (`Cabine`, `Carroceria`, `Faixa`, `Roda`) | Cubos e cilindros | Estrada norte, passo 9 | Sim (pack de veículo) |
| Barreira de cavaletes (`Perna_E`, `Perna_D`, `Barra_Alta`, `Barra_Baixa`) | Cubos | Z = 41,5, passo 9 | Sim |
| Helicóptero que pousa na base | Mesmo kit | Passo 9 | Sim |
| Soldado do encontro, corpo caído | Já com modelo de soldado | Entradas da Aldeia | Sim |

### 2.5 Área 4: Zigurate e base da Fundação (passo 10)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| `Zigurate_Terraco_1`, `_2`, `_3` | Três caixas escalonadas | Centro; domina a região | Sim (landmark; feito em módulos) |
| `Escadaria_1`, `Escadaria_2` | Rampas | Face sul do zigurate | Sim |
| `Posto_Atirador_Frente`, `_Lado_Oeste`, `_Lado_Leste`, `_Caixa` | Caixas oliva (sacos de areia) | 2º terraço | Sim (o atirador de elite é central ao passo 10) |
| `Tenda_Comando` | Tenda oca branca com porta | Avenida central, documentos do Kast | Sim |
| `Toldo_Tenda_Comando`, `Toldo_Poste_Sul`, `Toldo_Poste_Norte` | Caixa e cilindros | Porta da tenda de comando (esconde do atirador) | Sim |
| `Tenda_Medica` | Tenda oca branca | Oeste da base (antibióticos) | Sim |
| `Tenda_Provas` | Tenda oca branca | Leste (3º rolo do Tom) | Sim |
| `Tenda_Alojamento` | Caixa branca | Oeste | Não essencial |
| Seis contêineres (cobertura de furtividade) | Caixas | Sul da avenida | Sim |
| `Heliponto_Base` | Disco branco | Leste da base | Sim |
| `Cerca_Sul`, `Cerca_Leste`, `Muralha_Sul`, `Muralha_Norte`, `Muralha_Canto` | Caixas | Perímetro da base | Sim (limites) |
| `Portao_Aldeia` (`Folha`, `Faixa`, `Travessa`) | Cubos brancos e turquesa | Muro oeste, abre pelo painel | Sim |
| Rota alta: `Rota_Alta_Escada_Muro`, `_Passarela_Portao`, `_Rampa_Terraco_1`, `_Patamar`, `_Rampa_Terraco_2` | Rampas e caixas de madeira | Muro e face oeste do zigurate | Sim (rota alternativa; andaime de kit) |
| `Mesa_Kast`, `Papeis`, `Foto_1979`, `Radio_Mesa` | Cubos | Dentro da tenda de comando | Sim (documentos e foto são centrais) |
| `Painel_Portao`, `Botao_Portao` | Cubos | Parede norte da tenda de comando | Sim |
| `Mesa_Tenda_Medica`, `Antibioticos` | Cubos | Tenda médica | Sim (secundária "Remédio para Yusuf") |
| `Mesa_Tenda_Provas`, `Rolo_De_Filme_3` | Cubo e cilindro | Tenda de provas | Sim (secundária "O que o Tom viu") |
| `Ponto_Laser` | Esfera pequena | Mira do atirador | Sim (efeito, não modelo) |
| Gaiolas de artefatos, laboratório de campo, gabiões, torres de holofote, utilitários | Previstos no guia de arte, sem blockout | Base | Não essencial |

### 2.6 Área 5: Boca do Poço e antecâmara (passos 11 e 12)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| `Poco_Parede_Sul/Oeste/Leste/Norte_Alta`, `Poco_Fundo`, `Poco_Canto_*` | Caixas de rocha | Poço de 14 m | Sim (parede em camadas) |
| `Andaime_Viga_Sul`, `Andaime_Viga_Norte`, `Guincho` | Caixas metálicas | Boca do Poço, superfície | Sim (guincho com cabine é herói da superfície) |
| `Lance_1` a `Lance_4`, `Lance_4_Guarda`, `Patamar_3_Base` | Rampas de madeira | Descida de andaimes | Sim (kit de andaime) |
| `Holofote_Fundacao_Superficie` | Caixa branca | Superfície do Poço | Não essencial |
| `Ante_Chao`, `Ante_Teto`, `Ante_Parede_*`, `Ante_Canto_*` | Caixas de rocha | Antecâmara (arena do Primeiro Servo) | Sim (pedra lisa antiga com frisos) |
| `Plataforma_Oeste`, `Plataforma_Leste` e rampas (4) | Caixas e rampas de madeira | Arena do chefe (passo 12) | Sim (andaime) |
| `Porta_Selo_de_Kur`, `Selo_Ouro` | Caixas lápis-lazúli e ouro | Fundo da antecâmara | Sim (herói máximo da região) |
| `Parede_Barco_Junco`, `Barco_Casco`, `Barco_Mastro` | Caixas de rocha e ouro | Parede oeste da antecâmara (passo 13 abre) | Sim |
| Holofotes que caem (`Poste`, `Pe`, `Holofote`) | Cilindro e cubos | 4 na arena do chefe | Sim (mecânica do chefe) |
| Caixas, mochila e equipamento abandonado da Fundação | Previstos, sem blockout | Descida e antecâmara | Não essencial |
| Relevos da antecâmara (deuses e criaturas aladas, 3 a 4 painéis) | Previstos, sem blockout | Antecâmara | Não essencial |

### 2.7 Área 6: Passagem dos Guardiões e Wadi (passos 13 a 15)

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| `Tunel_Chao_Entrada`, `Tunel_Chao`, `Tunel_Teto`, `Tunel_Parede_*` | Caixas de rocha | Passagem dos Guardiões (passo 13) | Sim |
| `Tunel_Rampa_Saida`, `Rampa_Parede_Oeste/Leste` | Rampa e caixas de terra | Saída para o wadi | Sim |
| `Wadi_Margem_Oeste`, `Wadi_Margem_Leste` | Caixas de terra | Wadi (passo 14) | Sim |
| `Caminhonete_Samir` | Caixa de areia (2,2 x 2 x 5 m) | Fim do wadi (passo 15) | Sim (herói final; pack de veículo) |
| `Marcador Escolha_de_Faccao` | Marcador | Wadi | Sim (ponto lógico) |
| Pedras, arbustos, carcaça de carro, tocha/lamparinas, entalhes | Previstos, sem blockout | Passagem e wadi | Não essencial |

### 2.8 Memória de Shuruppak (passo 8, cena `Memoria_Shuruppak`)

Tudo em formas simples (`ShuruppakMemoryContent.cs`). A memória dura 2 a 3 minutos e é um ponto alto da demo.

| Nome | Forma hoje | Onde aparece | Demo |
| --- | --- | --- | --- |
| Casas de barro com toldos e panos coloridos | Caixas | Rua de Shuruppak | Sim (reuso do kit de casas) |
| Mercado, palmeiras, juncos, rio | Caixas, cilindros, planos | Rua e margem | Sim (versão simples) |
| Zigurate de topo dourado ao fundo | Caixas | Do outro lado do rio | Sim (reuso do módulo do zigurate) |
| Cabana de junco com parede do fundo | Caixas | Fim da rua | Sim |
| Menina, pescador, Ziusudra (avatar) | Cápsulas | Rua e cabana | Sim (versão simples; cápsula com túnica aceitável na demo) |

---

## 3. Itens coletáveis e interativos

### 3.1 Já implementados (aparecem como forma simples)

| Item / objeto | Onde aparece | Passo | Demo |
| --- | --- | --- | --- |
| **Tabuleta do Junco** (`Pedra`, `Linha_Ouro_*`) | Mão da Elena; lida no nicho (passo 8) | 1, 8 | Sim (herói) |
| **Faca de campo** (`Faca_De_Campo`, `Caixa_Faca`) | Caixa na barraca da Elena | 1 | Sim |
| **Bolsa vazia da câmera do Tom** (`Bolsa_Camera_Tom`) | Frente da barraca do Tom | 2 | Sim (inicia "O que o Tom viu") |
| **Corpos da equipe** | Pátio do acampamento | 2 | Sim |
| **Rádio do acampamento** (`Caixa_Radio`, `Aparelho_Radio`) | Mesa sob toldo | 3 | Sim (herói, luz vermelha) |
| **Rolo de filme 1** (`Rolo_De_Filme_1`) | Atrás do caminhão | 2 | Sim |
| **Rolo de filme 2** (`Rolo_De_Filme_2`) | Canto nordeste das Trincheiras | 5 | Sim |
| **Rolo de filme 3** (`Rolo_De_Filme_3`) | Tenda de provas da base | 10 | Sim |
| **Pistola Makarov** (`Pistola_Makarov`) | Corpo do chefe de segurança | 5 | Sim |
| **Munição 9 mm** (`Municao_ChefeSeguranca`, `Municao_Lider`, `Municao_Plataforma_Oeste`) | Chefe de segurança, líder das Trincheiras, arena do Servo | 5, 12 | Sim |
| **Rádio de campanha** (`Radio_De_Campanha`) | Largado pelo líder das Trincheiras | 5 | Não essencial (ainda não faz nada) |
| **Curativos** (`Curativo_1`, `_2`, `Curativo_Plataforma_Leste`, `Curativo_Boca_Antecamara`) | Trincheiras e arena do Servo | 5, 12 | Sim |
| **Mochila médica** (`Mochila_Medica`) | Perto da Dra. Weiss | 5 | Não essencial |
| **Antibióticos** (`Antibioticos`) | Tenda médica da base | 10 | Sim |
| **Documentos da Fundação**: e-mail do Kast e foto de 1979 (`Mesa_Kast`, `Papeis`, `Foto_1979`) | Tenda de comando | 10 | Sim |
| **Painel do portão** (`Painel_Portao`, `Botao_Portao`) | Tenda de comando | 10 | Sim |
| **Altar/nicho dos Guardiões** | Casa do altar | 7, 8 | Sim |
| **Pontos de retorno (checkpoints)** | Vários | todos | Sim (lógicos, sem modelo) |
| **Lamparina de barro** | Nicho | 7, 8 | Sim |
| **Holofotes que caem** | Arena do Primeiro Servo | 12 | Sim |
| **Escrita escondida (Eco do Abzu)** | Parede do fundo da casa do altar | 8 | Sim |
| **Selo de Kur** | Fundo da antecâmara | 11, 12 | Sim |
| **Fuzil dos soldados** | Mão dos 4 soldados | todos | Sim (hoje formas simples, a pendência mais visível dos inimigos) |
| **Faca na mão do Samir e da Elena** | Golpes | 6, 10 | Não essencial (lâmina curta provisória) |
| **Esteira do Yusuf** | Casa do altar | 7 | Não essencial |

### 3.2 Previstos nos passos 1 a 15 e nas secundárias, ainda sem implementação

| Item / objeto | Onde está previsto | Passo | Demo |
| --- | --- | --- | --- |
| **Fita de Irena nº 1** (gravador/fita) | Passagem dos Guardiões | 13 | Sim (item central; áudio com legenda) |
| **Inscrição dos Guardiões da Aldeia, do Poço e da passagem** (3 colecionáveis) | Aldeia, Poço, passagem | 7, 11, 13 | Opcional (a do Poço está listada como pendência) |
| **Documentos da Fundação** (cartas, relatórios; total de 5) | Base e Trincheiras | 5, 10 | Opcional (só o e-mail e a foto existem; os outros 3 humanizam os soldados) |
| **Foto de Irena como item de inventário** | Tenda de comando | 10 | Opcional (hoje só tela de leitura) |
| **Fotos do Tom** (item-chave, recompensa) | Fotógrafo da Aldeia, quarto escuro | 7, secundária | Sim, se "O que o Tom viu" entrar na demo |
| **Fotógrafo / quarto escuro improvisado** | Aldeia | secundária | Opcional |
| **Kit médico** (3 na região) | Espalhados | 5 a 12 | Opcional |
| **Espingarda de cano duplo** e **cartuchos** (6) | Casa trancada da Aldeia | 7 | Opcional |
| **Pólvora, cápsulas, gaze, álcool** (crafting) | Região | — | Não (crafting fora da fatia) |
| **Rádio do soldado ferido no wadi** | Wadi | 14 | Sim (dispara a escolha de facção) |
| **Tabuleta no chão do wadi** (entregar/enterrar/ficar) | Wadi | 14 | Sim |
| **Caminhonete do Samir (interativa)** | Wadi | 15 | Sim |
| **Entrada do diário** | Fim da fatia | 15 | Opcional (diário fica de fora) |
| **Lamparinas apagadas e entalhes dos Guardiões na passagem** | Passagem | 13 | Não essencial |
| **Cartazes e murais rasgados de 2003** | Aldeia | — | Opcional (pergunta aberta ao dono) |
| **Galinhas e cabras** | Aldeia | — | Opcional (pergunta aberta ao dono) |

---

## 4. Ordem sugerida de substituição para a demo

1. **Itens de interação** (tabuleta, faca, pistola, rolo de filme, curativo, antibióticos, rádio, fita de Irena, fuzil): pequenos, rápidos, mudam muito a percepção.
2. **Selo de Kur e antecâmara**: o momento mais forte da demo.
3. **Veículos e helicóptero** (packs prontos): caminhões, helicóptero, caminhonete do Samir.
4. **Kits modulares**: casas da Aldeia, trincheiras, andaimes, zigurate.
5. **Polimento**: palmeiras, tendas com interior, props de escavação, relevos, cartazes.

## 5. Observações

- O Tom e a equipe morta usam cápsulas ou formas; os modelos dos arqueólogos e do trabalhador estão prontos na `ArteFonte`, esperando importação (um por conversa).
- A lista de itens-chave do `Docs/Regiao-1-Sitio-de-Ur.md` prevê 5 documentos da Fundação e 3 inscrições; hoje há 2 documentos e nenhuma inscrição implementada.
- Esta lista não confere a cena aberta na Unity; vem dos scripts e dos documentos. Se algum objeto foi mudado à mão na cena (o que o projeto evita), ele pode divergir.
