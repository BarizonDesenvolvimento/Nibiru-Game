# Auditoria da fatia vertical (Sítio de Ur), 25 de outubro de 2026

Comparação do roteiro (`Docs/Regiao-1-Sitio-de-Ur.md`) com o `Docs/ESTADO.md`, os scripts que montam a cena (`Scripts/Editor/Content`), os arquivos de dados e o código de runtime. A cena não foi aberta visualmente; os scripts de conteúdo a reconstroem, e ela foi conferida nas etapas anteriores. Nada foi implementado nesta auditoria.

**Esforço:** P é até meio dia, M de 1 a 2 dias, G de 3 a 5 dias, GG mais de uma semana. Os dias são de trabalho com o Claude e incluem teste pela ponte, sem contar o teste do dono no Play.

## Missão e secundárias

| Item | Situação | Esforço | Observação |
| --- | --- | --- | --- |
| Missão principal, passos 1 a 15 | Pronto (blockout) | — | Todos aprovados no Play. O equilíbrio do passo 12 está pendente. Arte, sons e vozes são provisórios, e faltam as animações de dirigir finais. |
| "Remédio para Yusuf" | Pronto (blockout) | — | Aprovada no Play. Os modelos dos três personagens e o documento da Weiss estão pendentes. |
| "O que o Tom viu": 3 rolos | Pronto | — | Acampamento, Trincheiras e tenda de provas. Contam até 3. |
| "O que o Tom viu": cena final com a Umm Karim | Falta | M | Falta o fotógrafo, o quarto escuro e as fotos. O roteiro não dá o nome, o lugar nem o texto. |
| Item "Fotos do Tom" | Falta | P | O item não existe, e a recompensa depende da cena final. |
| Entrada especial no diário | Fora da demo | — | Decisão de 25/out: o diário fica fora da demo; a recompensa é o item "Fotos do Tom", aberto na tela de leitura. |

## Itens

| Item | Situação | Esforço | Observação |
| --- | --- | --- | --- |
| Faca | Pronto | — | |
| Pistola | Pronto | — | |
| Munição 9 mm | Provisório | P | 20 balas no mapa (chefe 8, líder 4, arena 8). O roteiro pede cerca de 30. Ligado ao equilíbrio do passo 12. |
| Curativos | Provisório | P | 4 no mapa (2 nas Trincheiras, 2 na arena). Nenhum no Acampamento, na Aldeia nem na base. |
| Kits médicos (3) | Falta | P | O item existe em `Data/Items`, mas nenhum está colocado. |
| Espingarda (opcional) e 6 cartuchos | Falta | M | Os itens existem. Falta a casa trancada da Aldeia (só um marcador no blockout), como abri-la, o pickup e o som forte. |
| Rádio de campanha | Provisório | M | Pode ser pego, mas não faz nada. Deveria avisar onde estão as patrulhas. |
| Pólvora, cápsulas, gaze e álcool | Fora da fatia | — | O crafting ficou de fora por decisão do roteiro. |

## Documentos, fita e inscrições

| Item | Situação | Esforço | Observação |
| --- | --- | --- | --- |
| Documentos da Fundação (5) | Falta (1 pronto, 1 provisório, 3 faltam) | M | Pronto: o e-mail do Kast. Provisório: o documento da Weiss. Faltam 3 cartas ou relatórios nas Trincheiras e na base, com o texto por escrever. |
| Foto de 1979 | Pronto | — | Mostrada na tela de leitura. Não vira item no inventário. |
| Fita de Irena nº 1 | Provisório | P | Funciona, com legenda. Falta a voz gravada, e o texto atual é provisório. |
| Inscrição da passagem | Pronto | — | |
| Inscrição da Aldeia | Provisório | P | Existe só como marca visual revelada pelo Eco, sem texto lido. |
| Inscrição do Poço | Falta | P | |

## Inimigos

| Item | Situação | Esforço | Observação |
| --- | --- | --- | --- |
| Soldados, líder nas Trincheiras, líder no wadi, atirador, helicóptero nas Trincheiras e no Acampamento | Pronto | — | |
| 2º líder de esquadra na base | Falta | M | Há um líder na tenda de comando e uma dupla de soldados. |
| Helicóptero passando sobre a base | Provisório | M | Aparece só pousado, no heliponto. Falta a passagem que chama soldados. |
| Lullu no Poço | Provisório | M | O 1º encontro funciona. |
| 2º Lullu no Poço | Falta | M | O roteiro pede 2 encontros. |
| Primeiro Servo | Pronto | — | O equilíbrio da luta está pendente. |

## Memória

| Item | Situação | Esforço | Observação |
| --- | --- | --- | --- |
| Memória de Shuruppak | Provisório | G | A mecânica está pronta e aprovada. A cena, o som de cidade e rio, e a voz de Enki são formas simples. |

## Localização com o pacote Localization

O pacote `com.unity.localization` não estava instalado na data da auditoria. Os textos estavam em três lugares:

- **Em arquivos de dados** (fácil de migrar): 52 diálogos (227 falas e 22 escolhas), 4 documentos, 1 fita, 11 itens (nome, descrição e dica), 5 missões com objetivos, a tabuleta e a memória.
- **No código** (trabalhoso): cerca de 22 chamadas de aviso na tela, cerca de 55 textos de interface (pausa, menu, inventário, diálogo, fim da demo) e cerca de 16 avisos de interação.
- **Dentro da cena** (o mais difícil): os avisos de interação ("Falar com…", "Pegar…") e os avisos disparados por eventos ficam gravados nos componentes; os scripts de `Editor/Content` guardam uma segunda cópia do texto (cerca de 208 falas) que recria o arquivo se ele for apagado.

| Etapa | Esforço |
| --- | --- |
| Instalar o pacote, tabelas pt-BR e inglês, seletor de idioma, fontes com acentos | M |
| Script que extrai os textos de diálogos, documentos, itens e missões para tabelas e liga cada campo à chave | G |
| Trocar os textos fixos do código (avisos, menus, interface) por consulta à tabela | M a G |
| Trocar os textos serializados na cena (avisos de interação e dos eventos) | G |
| Fazer os scripts de conteúdo gerarem chaves, sem duplicar texto | M |
| **Total para deixar a fatia pronta para traduzir (sem a tradução em si)** | **cerca de 2 semanas (GG)** |

## Salvamento do último checkpoint ("Continuar")

Falta, mas o terreno está preparado. Tudo que importa já vive em `WorldState` (eventos, passos de missão, falhas, reputação, transformação, itens pegos, checkpoints ativados) e em `Checkpoints.Data` (posição, vida, inventário, munição), só na memória. O resto do mundo se reconstrói por eventos ao carregar a cena.

| Etapa | Esforço |
| --- | --- |
| Gravar e ler esse conjunto em JSON (com campo de versão) | M |
| Identificar itens e armas por nome (hoje são referências de arquivo, sem registro central) | P |
| "Continuar" no menu principal e gravar ao ativar um checkpoint ou salvamento automático | P |
| Casos especiais: não salvar dentro da memória de Shuruppak; o fim da demo apaga ou arquiva o arquivo | P |
| Teste: gravar, fechar o Play, abrir e comparar o estado | P |
| **Total** | **cerca de 2 a 3 dias (M a G)** |

## Decisões tomadas depois da auditoria (25/out/2026)

- **Localização em 2 partes.** Parte 1 agora: tabelas pt-BR/en, extração dos textos dos arquivos de dados, scripts de conteúdo gerando chaves, fontes e seletor de idioma. Parte 2 (textos fixos no código e textos serializados na cena) depois do teste completo, com o layout congelado.
- **Diário fora da demo.** A recompensa de "O que o Tom viu" é o item "Fotos do Tom", aberto na tela de leitura.
- **Etapa de equilíbrio (B):** munição, curativos, kits médicos, espingarda e rádio de campanha.
