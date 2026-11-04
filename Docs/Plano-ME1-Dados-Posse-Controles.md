# Plano: ME1, dados dos poderes, posse e mapa de controles final

Projeto no papel (4/nov/2026), **aprovado pelo dono em 4/nov/2026 com ajustes** (veja a seção 6, "Decisões da aprovação", que vale acima do texto original quando os dois divergem). **Tarefa de sistema [Opus].**

## 1. O que existe hoje (levantamento, sem alterar nada)

### 1.1 Onde os ME estão escritos

| Documento | O que diz |
| --- | --- |
| `Docs/ESTADO.md`, "Sistema dos ME" | Tabela ME1 a ME8 (dependências e estado), decisões de 3/nov, proposta de botões e mapa completo de controles, pendências do zoom no controle e da barra "Transformação %" |
| `Docs/Biblia.md`, "Regras da demo" | Sem XP nem nível; despertares; três poderes; Tabuleta do Junco sem puzzle; Sopro e Égide com puzzle de anéis; o que o save guarda; proposta de botões "ainda não aprovada" |
| `Docs/GDD.md`, "Sistemas principais" | Tabuletas dão poder, memória e história; Transformação é "barra de humano a divino" (mais divino = poderes mais fortes, visual e reações); finais dependem do grau de transformação |
| `Docs/Regiao-1-Sitio-de-Ur.md` | Itens da região, a Tabuleta do Junco (Eco: pulso que revela e atordoa o Lullu; custo: transformação 0 → 5), escolha do wadi (o poder fica em qualquer opção; "ficar" dá +3 escondido), tabela de controles |

**O que já está decidido por ME:**

| ME | Decidido | Em aberto (não está no repositório) |
| --- | --- | --- |
| ME1 | Dados em ScriptableObject e posse; botões propostos (Q/Z/X; LB, LB+RB, LB+Y; Eco ao soltar o LB se nada mais foi apertado) | Tudo o que este plano propõe |
| ME2 | Anéis giratórios; o jogo não pausa; lugar seguro; falhar sem penalidade; puzzle 1 com 3 anéis, puzzle 2 com 4 | Controles do puzzle, como a pista aparece, o que "resolver" significa por anel |
| ME3 Sopro | Na antecâmara, entre os passos 11 e 12; muito eficaz contra Anunnaki, médio contra híbridos, fraco contra humanos; recarga 90 s; dá dano no Servo | **O que o Sopro faz no espaço** (cone? projétil? empurrão?), dano, alcance, se mira |
| ME4 Égide | Na passagem (passo 13); dura 8 s; recarga 60 s; dispara o primeiro despertar | **O que a Égide faz** (escudo? invulnerabilidade? menos dano? reflete?) |
| ME5 | Recarga e HUD dos ME | Desenho do HUD dos 3 poderes |
| ME6 | Save dos ME, da recarga restante e dos despertares; depende da regra de versão (início da fase B) | — |
| ME7 | A Égide dispara o primeiro despertar; árvore bloqueada com glifos não traduzidos | Quantos despertares existem, o que dispara os seguintes, o que a árvore contém, a ligação entre transformação e despertares, a barra "Transformação %" |
| ME8 | Regressão de tudo o que os ME tocam | — |

**Inimigo por tipo:** a Bíblia diz que o Lullu e o Primeiro Servo são **híbridos**, mas nenhum inimigo tem hoje um campo "tipo" (humano, híbrido, Anunnaki). O ME3 vai precisar dele.

### 1.2 Como o jogo guarda hoje o que a Elena possui

| Coisa | Onde vive | Como entra no save de slot |
| --- | --- | --- |
| Itens comuns (curativo, kit médico, munição, antibióticos, rolos de filme) | `PlayerInventory` (componente na Elena): lista de espaços (8) com pilhas | `checkpoint.inventario` (código do item + quantidade) |
| Itens-chave (Tabuleta do Junco, faca, pistola, espingarda, rádio, fotos do Tom) | `PlayerInventory`, lista separada sem limite (`category = Chave`) | `checkpoint.itensChave` |
| Armas de fogo | **Possuir = ter o item** (`FirearmDefinition.weaponItem` no inventário). `PlayerWeapons` guarda só a arma equipada e as balas no pente | `checkpoint.armaEquipada`, `checkpoint.balasNaArma` (código `arma.*`) |
| Faca | Item `faca_de_campo`; `PlayerMeleeAttack.requiredItem` confere | Junto dos itens-chave |
| Kits médicos | Item de cura (`healAmount`), usado pelo inventário (`PlayerItemUser`) | `checkpoint.inventario` |
| Tabuleta do Junco (objeto) | Item-chave `tabuleta_do_junco`; sai na escolha do wadi (entregar ou devolver) | `checkpoint.itensChave` |
| Tabuleta **decifrada** | Evento `"Tabuleta:" + nome do arquivo` no `WorldState` | `mundo.eventos` |
| **Poder (Eco do Abzu)** | Evento `"Poder:" + nome do arquivo` (`"Poder:EcoDoAbzu"`), marcado por `PowerDefinition.Unlock()` quando o `TabletReader` decifra | `mundo.eventos` |
| Recarga do Eco | Campo privado `readyAt` dentro do `EchoOfAbzu` | **Não grava**; ao morrer ou carregar, o Eco volta pronto |
| Transformação | `WorldState.Transformation` (0 a 100) | `mundo.transformacao` |
| Selo de Kur | **Não é posse**: é a porta selada da antecâmara (cenário e luzes do `FirstServantFight`) | — |

**Dados de hoje:** `PowerDefinition` (abstrata: `displayName` e `description` em texto solto, **sem chave de idioma**; `cooldown`; `Id` = nome do arquivo) e `EchoPowerDefinition` (alcance 18 m, revela 6 s, recarga 12 s no asset). `TabletDefinition` aponta para o poder (`grantsPower`) e soma a transformação (+5). O rodapé "Novo poder: ..." do `TabletReader` é texto fixo em português.

**O que já está bem resolvido e não muda:** itens, armas, kits e tabuleta (objeto) já são dados com código estável, já entram no save e já têm uma dona única (`PlayerInventory`). O ME1 **não mexe em itens**; só documenta como eles se encaixam.

### 1.3 Mapa de controles atual (`Data/Input/NibiruControles`) e quem lê cada botão

Dois mapas: **Jogador** (desliga em menus, diálogos e leitura) e **Interface** (sempre ligado).

| Ação | Teclado e mouse | Controle | Quem lê |
| --- | --- | --- | --- |
| Mover | WASD, setas | Analógico esquerdo | `PlayerMotor` |
| Olhar | Mouse | Analógico direito | `PlayerCameraController` |
| Correr | Shift esquerdo | L3 (trava até parar) | `PlayerMotor` |
| Esquivar | Espaço | B | `PlayerMotor` |
| Interagir | E | A | `PlayerInteractor` |
| Atacar (faca) | Clique esquerdo, F | RB | `PlayerMeleeAttack` (não ataca mirando) |
| Mirar | Botão direito | LT | `PlayerWeapons` (só com arma equipada) |
| Atirar | Clique esquerdo | RT | `PlayerWeapons` (só mirando) |
| Recarregar | R | X | `PlayerWeapons` |
| Agachar | C, Ctrl esquerdo | R3 | `PlayerMotor` |
| Poder | Q | LB | `EchoOfAbzu` (dispara ao **apertar**) |
| Inventário | Tab, I | Select | `InventoryScreenUI` |
| Navegar | WASD, setas | D-pad, analógico esquerdo | `DialogueUI`, `InventoryScreenUI`, `PauseMenuUI` |
| Confirmar | Enter, E | A | diálogo, inventário, pausa, leitura, morte, fim da demo |
| Voltar | Esc, Backspace | B | inventário, pausa, leitura |
| Pausar | Esc | Start | `PauseMenuUI` |
| Roda do mouse | — (lida direto) | — | zoom (`PlayerCameraController`), rolagem do Arquivo (`MenuPointer`) |
| Delete / X (controle) | lidos direto | | apagar slot (`MainMenuUI`) |

**Teclas iguais em ações diferentes (hoje sem conflito, porque o contexto separa):**
- Clique esquerdo = faca e tiro: só é tiro mirando (`PlayerMeleeAttack.CanStart` recusa mirando).
- E = Interagir e Confirmar; A = Interagir e Confirmar; B = Esquivar e Voltar; X = Recarregar e Apagar slot: o mapa Jogador está desligado em todo menu e diálogo, e ninguém lê Voltar ou Navegar no jogo.
- Esc = Voltar e Pausar: a pausa trata os dois juntos.
- **Livres no jogo:** G, Z, X, 1 a 9, botões laterais do mouse; Y e o D-pad no controle.
- **Observação (não é do ME1):** não dá para pausar durante uma conversa (a pausa exige os controles da Elena ligados).

### 1.4 A barra "Transformação %" hoje

`TransformationHUD` mostra "TRANSFORMAÇÃO N%" com preenchimento, **some no zero** e aparece depois da primeira tabuleta. Sobe +5 ao decifrar a Tabuleta do Junco e +3 **sem aviso** ao ficar com ela no wadi (`SilentTransformationGain`; a barra mostra o total, sem anunciar). A marca azul no pulso (`TransformationMark`) aparece com transformação acima de zero. **Nada mais lê o valor**: não muda força de poder nem reação de personagem. No GDD, ela decide os finais.

### 1.5 Pendências do ME1 já registradas

- Confirmar os botões dos poderes contra o mapa atual (ESTADO, Bíblia e Região 1 dizem "a confirmar no ME1").
- **Atalho de zoom no controle** (hoje só roda do mouse e a opção "Distância da câmera").
- A barra "Transformação %" (decidir no papel; implementar no ME7).

## 2. Plano

### a) Dados: o que descreve cada poder e cada item

**Itens: ficam como estão.** O `ItemDefinition` já tem tudo o que o pedido lista: código estável (`id`), nome, descrição e dica com chave (`item.<Nome>.*`), categoria, pilha, cura, ligação com arma, tabuleta ou documento. Um item novo continua sendo só um arquivo novo.

**Poderes: o `PowerDefinition` ganha o que falta.** Campos da classe base (todo poder tem):

| Campo | Tipo | Exemplo (Eco) | Para que serve |
| --- | --- | --- | --- |
| `id` | texto, **código permanente** | `poder.eco_do_abzu` | Save e registro. Nunca muda, mesmo se o arquivo for renomeado |
| `nameKey`, `descriptionKey`, `hintKey` | chave | `power.EcoDoAbzu.name`, `.desc`, `.hint` | Textos no `pt-BR.json`. A dica leva `{0}` para o botão ("Aperte {0} para soltar o Eco"), preenchido pelo aparelho em uso |
| `icon` | Sprite (opcional) | — | HUD do ME5 |
| `slot` | `Poder1`, `Poder2`, `Poder3` | `Poder1` | Qual botão dispara. É uma **vaga**, não uma tecla (veja (d)) |
| `cooldown` | segundos | 12 | Recarga (já existe) |
| `duration` | segundos, 0 = instantâneo | 0 (Égide: 8) | Quanto tempo o poder fica ativo |
| `usableWhileAiming`, `usableWhileCrouched`, `usableWhileReloading` | sim/não | sim, sim, sim | Requisitos de uso próprios do poder |

**Requisitos comuns a todos (no código, não nos dados):** a Elena tem o poder, não está morta, nem esquivando, nem no meio de um golpe de faca, os controles e o combate estão ligados (isso já exclui menus, diálogos, leitura, memória jogável e viagem) e a recarga terminou.

**Requisito para ganhar:** fica em **quem dá o poder**, não no poder: a tabuleta (`TabletDefinition.grantsPower`, já existe) e, no ME2, o puzzle do selo (campo igual). Assim o mesmo poder pode vir de fontes diferentes sem mudar o arquivo dele.

**Custo por uso:** **não há campo**, porque o repositório não define nenhum (o custo de hoje é a transformação ao decifrar, que mora na tabuleta). Se for decidido, entra como campo novo, sem refazer nada (pergunta 1).

**Estado (calculado, não guardado nos dados):** `NaoPossui`, `Pronto`, `Ativo` (durante `duration`), `Recarregando`. É o que o HUD do ME5 vai ler.

**Valores próprios de cada poder** ficam numa classe filha, como já é hoje: `EchoPowerDefinition` (alcance, tempo revelado). O ME3 cria a do Sopro (dano por tipo de inimigo) e o ME4 a da Égide.

**O efeito** fica num componente na Elena que herda de uma base comum, `PowerBehaviour` (o poder que ele executa, `PodeUsarAgora`, `Ativar()`, `EstaAtivo`). O `EchoOfAbzu` passa a herdar dela e deixa de ler o botão e de contar a própria recarga.

**Quem dispara:** um componente novo na Elena, `PlayerPowers`: lê as 3 vagas no `PlayerInputReader`, acha o poder possuído daquela vaga, confere os requisitos e a recarga, chama `Ativar()` e começa a recarga. A **recarga passa a ser contada num lugar só** (aqui), porque o ME5 (HUD) e o ME6 (save da recarga restante) precisam dela; o ME5 continua dono do HUD e das regras finas da recarga.

**Como um ME futuro adiciona um poder (exemplo: ME3, Sopro de Enlil):**
1. Escreve os textos no `pt-BR.json` (`power.SoproDeEnlil.name`, `.desc`, `.hint`).
2. Cria `BreathPowerDefinition : PowerDefinition` (valores) e `BreathOfEnlil : PowerBehaviour` (efeito).
3. Cria o arquivo `Data/Powers/SoproDeEnlil` (`id` `poder.sopro_de_enlil`, vaga `Poder2`, recarga 90).
4. Roda o menu novo **Nibiru > Poderes > Montar poderes na Elena** (põe um componente de efeito por poder no prefab, sem trabalho à mão).
5. Liga a fonte: o puzzle do selo do ME2 aponta para o arquivo.
6. Roda **Nibiru > Salvamento > Criar registro e códigos** (o código entra no registro e na lista).

**Sem mexer** no mapa de controles, na posse, no save, no `PlayerPowers` nem no teste de dados.

### b) Posse: uma fonte de verdade por tipo de coisa

| O quê | Dono único | Quem pode mudar | Quem pode perguntar |
| --- | --- | --- | --- |
| Itens, armas, faca, kits, tabuleta (objeto) | `PlayerInventory` (como hoje) | pegar, trocar, usar, o wadi, os atalhos de teste, o carregamento | qualquer sistema, pelo componente da Elena |
| **Poderes** | **`PowerBook`** (novo, estático, como `Transformation`), guardado num conjunto próprio do `WorldState` | só quem **dá** o poder (tabuleta, puzzle do ME2), os atalhos de teste e o carregamento | qualquer sistema: `PowerBook.Has(poder)`, `PowerBook.Owned`, evento `Changed` |
| Tabuleta decifrada | evento `Tabuleta:*` (como hoje) | `TabletReader` | `TabletDefinition.IsDeciphered` |
| Transformação | `WorldState.Transformation` (como hoje) | tabuletas, escolhas | `Transformation.Value` |
| Despertares (ME7) | entra no `WorldState` no ME7 | ME7 | ME7 |

- **Ganhar:** `PowerBook.Grant(poder)`. Sem efeito se já tem.
- **Perder:** `PowerBook.Revoke(poder)`. **Na demo ninguém perde poder** (no wadi o poder fica em qualquer opção); existe para os testes e para o jogo inteiro.
- **Por que sair do evento `Poder:EcoDoAbzu`:** o código do evento é o nome do arquivo (frágil a renomear), evento não pode ser desmarcado, e o ME6 precisa de um campo próprio. O `PowerDefinition.IsUnlocked` continua existindo, agora perguntando ao `PowerBook`.
- **Checkpoint e morte:** o conjunto de poderes entra no `WorldState.Snapshot`, então morrer e voltar já restaura do jeito certo, como o resto do mundo.

### c) Save: o que o ME1 grava

**O ME1 grava um campo só:**

| Campo | Onde | Formato | Exemplo |
| --- | --- | --- | --- |
| `poderes` | `mundo` | lista de códigos, em ordem | `"poderes": ["poder.eco_do_abzu"]` |

- Entra na **mesma versão 1**, como campo novo, do mesmo jeito que o `valoresDaCena` entrou na B1. **Não cria a regra de versão.**
- É **sempre gravado**, mesmo vazio (`[]`): assim, a falta dele indica um arquivo antigo.
- **Arquivo antigo sem `poderes`:** o jogo procura nos eventos `Poder:<nome do arquivo>` e converte pelo registro (`Poder:EcoDoAbzu` → `poder.eco_do_abzu`); o evento antigo sai do conjunto. Sem nenhum dos dois, a Elena não tem poder (correto para quem salvou antes do passo 8). **Resultado: todo save de hoje carrega igual, com o Eco onde ele já estava.**
- **Código de poder desconhecido** no arquivo: slot **ilegível**, a mesma regra dos itens, armas e missões.
- Os atalhos de teste que hoje marcam o evento `Poder:EcoDoAbzu` (`Step9Shortcuts` e quem herda a lista dele) passam a dar o poder pelo `PowerBook`.

**Campos já nomeados para os próximos ME (o ME1 não grava):**

| Campo | Onde | Quem grava | Falta no arquivo = |
| --- | --- | --- | --- |
| `recargaDosPoderes` | `checkpoint` | ME6 | todos prontos (o que acontece hoje) |
| `despertares` | `mundo` | ME7 / ME6 | 0 |

**Quando a regra de versão entrar (início da fase B):** os três campos são "só acrescentam", então pela regra decidida em 26/out ficam opcionais na mesma versão; a conversão dos eventos `Poder:*` vira o primeiro caso de "migrar e carregar".

**Registro e códigos:** o `SaveRegistry` ganha a lista de poderes; **Criar registro e códigos** dá código a poder novo; o teste de códigos passa a conferir os poderes; `Docs/Codigos-Salvamento.txt` ganha `poder.eco_do_abzu`.

### d) Mapa de controles final

**Validação contra jogos reais:**
- **God of War (2018):** segura-se L1 e aperta R1 (ou R2) para o ataque rúnico. A diferença importante: **no God of War o L1 sozinho é o escudo, uma ação de segurar**, então ele age ao ser apertado e não há atraso. No nosso caso o LB sozinho é o Eco, uma ação de **toque**, e por isso só pode disparar **ao soltar** (é o único jeito de saber que não veio um RB ou um Y depois).
- **Control (2019):** no PlayStation, Lançar no R1, Escudo no L1 (segurar), Esquiva no Círculo e Possuir no Quadrado. Os poderes ficam **cada um no seu botão**, sem combinação, e o poder "de segurar" fica num ombro. Um guia de Xbox que encontrei contradiz esse mapa; considerei a versão do PlayStation, que tem fonte melhor.
- **Conclusão:** a proposta do dono é do tipo God of War (modificador), o que libera botões, mas herda um atraso que o God of War não tem. Ele é aceitável (veja os riscos), e há um plano B pronto.

**Regras do LB (controle):**
1. **LB sozinho, tocado e solto:** Eco **ao soltar**, se nenhum outro botão foi apertado enquanto o LB estava seguro.
2. **Com o LB seguro:** RB = **Sopro** (dispara na hora, ao apertar o RB); Y = **Égide** (na hora). Depois disso, soltar o LB não dispara o Eco.
3. **Com o LB seguro, o RB nunca é faca.** Se a Elena ainda não tem o Sopro, LB+RB não faz nada (previsível: não sai uma facada sem querer).
4. **Qualquer outro botão** apertado com o LB seguro (B, A, X, LT, RT, Start, Select, L3, R3, D-pad) **faz a ação normal** dele e cancela o Eco. Mexer os analógicos não cancela.
5. **Proposta nova (pergunta 2):** se o LB ficar seguro **mais de 0,5 s** sem combinação, soltar **não** dispara o Eco. Serve para "desistir" e evita o Eco sem querer de quem segurou o LB para pensar. Valor ajustável.
6. Com o LB seguro por mais de 0,25 s, uma dica pequena mostra "RB Sopro · Y Égide" (só os poderes que ela tem). Fica para o ME5 desenhar; o ME1 só expõe o estado.
7. **O botão que segue o RB é uma "ação"**, não o RB fixo: a regra usa as ações do mapa (`ModificadorPoderes`, `Atacar`, `Poder3Combo`), então remapear depois continua funcionando.

**Teclado:** sem combinação. Q, Z e X disparam **ao apertar** (sem atraso). Segunda tecla opcional: botões laterais do mouse (Mouse 4 = Sopro, Mouse 5 = Égide), para quem não quer tirar os dedos do WASD.

**Zoom no controle (pendência do ME1): D-pad para cima aproxima, para baixo afasta**, 0,25 m por toque (o mesmo passo da roda); segurar repete a cada 0,15 s. Vale nas mesmas condições da roda (`ZoomAllowed`: não mirando, sem menu, sem diálogo, fora da memória e da viagem). Grava no mesmo `GameOptions.CameraDistance`. No teclado continua só a roda (e a opção nas Opções).

**D-pad esquerda e direita:** **reservados para a troca de arma do B6** (espingarda), como nos jogos do gênero; no teclado, 1 e 2 reservados para o mesmo. Não entram no ME1.

**Mudanças no mapa `NibiruControles`:**
- `Poder` vira `Poder1` (Q, LB).
- Novas: `Poder2` (Z, Mouse 4; no controle é a combinação), `Poder3` (X, Mouse 5; combinação), `ModificadorPoderes` (LB), `Poder3Combo` (Y) e `ZoomCamera` (D-pad cima e baixo, eixo).
- A combinação é resolvida **no código** do `PlayerInputReader`, não pela opção global do Input System que faz o atalho "consumir" o botão: aquela opção vale para o jogo todo e não sabe fazer "dispara ao soltar".

**Tabela final por contexto:**

*Exploração e combate (é o mesmo contexto: o jogo não tem "modo combate")*

| Ação | Teclado e mouse | Controle |
| --- | --- | --- |
| Mover / olhar | WASD ou setas / mouse | Analógico esquerdo / direito |
| Correr | Shift (segurar) | L3 (trava até parar) |
| Agachar | C ou Ctrl | R3 |
| Esquivar | Espaço | B |
| Interagir | E | A |
| Faca | Clique esquerdo ou F | RB (sem o LB seguro) |
| Mirar | Botão direito (segurar) | LT (segurar) |
| Recarregar | R | X |
| Eco do Abzu | Q | LB (toque; dispara ao soltar) |
| Sopro de Enlil | Z (ou Mouse 4) | LB + RB |
| Égide de Enki | X (ou Mouse 5) | LB + Y |
| Zoom da câmera | Roda | D-pad cima / baixo |
| Troca de arma | 1, 2 (reservado, B6) | D-pad esquerda / direita (reservado, B6) |
| Inventário | Tab ou I | Select |
| Pausa | Esc | Start |

*Mira (botão de mirar seguro, com arma)*

| Ação | Teclado e mouse | Controle |
| --- | --- | --- |
| Atirar | Clique esquerdo | RT |
| Faca | **não** (o clique é tiro; F e RB não fazem nada, como hoje) | — |
| Recarregar, esquivar, agachar, mover | iguais | iguais |
| Poderes | Q, Z, X | LB, LB+RB, LB+Y (padrão: valem mirando; cada ME pode recusar pelo campo `usableWhileAiming`) |
| Zoom | **não** (a mira manda) | **não** |

*Menus (pausa, Opções, Arquivo, inventário, leitura, slots, morte, fim da demo)*

| Ação | Teclado e mouse | Controle |
| --- | --- | --- |
| Navegar | WASD ou setas; mouse | D-pad ou analógico esquerdo |
| Mudar valor (Opções) | A/D ou setas; clique na metade | D-pad ou analógico, esquerda e direita |
| Confirmar | Enter ou E (Espaço no menu principal); clique esquerdo | A |
| Voltar | Esc ou Backspace; clique direito | B |
| Apagar slot | Delete | X |
| Rolar o Arquivo | Roda | Navegar |
| Fechar inventário | Tab, I ou Voltar | Select ou B |

*Diálogo (conversa e escolhas)*

| Ação | Teclado e mouse | Controle |
| --- | --- | --- |
| Escolher resposta | W/S ou setas | D-pad ou analógico esquerdo |
| Avançar / confirmar | Enter ou E | A |
| Todo o resto | desligado | desligado |

*Contextos especiais (sem botão novo)*
- **Memória jogável:** mover, olhar, correr, agachar e interagir; sem esquiva, faca, armas, poderes, inventário e zoom (como hoje).
- **Viagem de caminhonete:** só olhar (como hoje).
- **Puzzle do selo (ME2, só sugestão para o projeto dele):** D-pad ou analógico cima e baixo escolhem o anel, esquerda e direita giram, A confere, B sai. Os controles da Elena ficam desligados, então o zoom e os poderes não disparam.

**Conferência "um botão, duas funções no mesmo contexto":** nenhuma. Os casos de tecla repetida (clique esquerdo, E, A, B, X, Esc, Y) estão sempre em contextos diferentes ou resolvidos por uma regra (mirando ou não; LB seguro ou não). O X do teclado (Égide) e o X do controle (Recarregar) são aparelhos diferentes; a dica na tela usa o nome do aparelho em uso.

**Riscos:**
1. **Atraso do Eco no controle:** o Eco sai quando o dedo solta, ou seja, a duração do toque (tipicamente 80 a 150 ms) mais um quadro. Para revelar e acender marcas não se sente; **contra a investida do Primeiro Servo pode pesar**. O teste mede o atraso, e há um atalho para o dono sentir com um controle de verdade. **Plano B, se o atraso incomodar no Play:** Eco no **Y sozinho** (dispara ao apertar), LB vira modificador puro (sozinho não faz nada), LB+RB Sopro e LB+Y Égide. Muda só a regra do leitor, não os dados.
2. **Ordem trocada:** apertar o RB um instante antes do LB sai faca; o Y antes do LB, no plano B, sai Eco em vez de Égide. É do esquema de modificador (o God of War tem o mesmo); o teste confere que a ordem certa nunca falha.
3. **Mira e recarga:** LB+RB mirando dispara o Sopro (o RB mirando não faz nada hoje, então não há disputa). LB+X recarrega e cancela o Eco. LB+Y (Égide) fica ao lado do X (recarregar): apertar errado recarrega; aceitável, mas vale observar no teste do dono.
4. **Teclado:** Z e X tiram por um instante o dedo de A/S (o strafe para). Mouse 4 e 5 são a saída. Em teclado AZERTY, o Input System liga a tecla pela **posição física**, então as posições ficam iguais e a dica mostra a letra local.
5. **Acessibilidade e remapeamento:** o Input System já sabe remapear e guardar as trocas (em texto, nas preferências). **No ME1 não há tela de remapear** (a tela de Opções maior é da fase D), mas tudo fica pronto para ela: cada poder é uma ação, e a regra do LB usa ações e não botões fixos. Dois cuidados para a fase D: cada Elena usa uma **cópia** do arquivo de controles (`Instantiate` no `PlayerInputReader`), então as trocas precisam ser aplicadas no `Awake`; e uma opção "Eco ao apertar" (o plano B como escolha do jogador). No Steam e no Steam Deck, o Steam Input já deixa remapear o controle por fora, inclusive nos botões de trás do Deck.
6. **Controle real ligado durante o teste:** como a roda real no zoom, um controle de verdade mexido durante o teste pode atrapalhar; o teste usa um controle virtual e confere o aparelho de cada evento.

### e) "Transformação %": o que a barra vira com os despertares (decidir no papel; implementar no ME7)

Premissa do repositório: **transformação** (0 a 100) e **despertares** são coisas diferentes. A transformação já existe e decide os finais; o primeiro despertar vem de um **evento** (a Égide), não de um valor da barra.

| Opção | Como funciona | Jogo de referência | A favor | Contra |
| --- | --- | --- | --- | --- |
| **1. Barra com marcos** | A barra fica no HUD; os despertares viram marcas nela | **inFAMOUS** (o medidor de karma tem degraus que liberam poderes) | Clara; o jogador vê o custo | Vira meta: o jogador quer **encher** a barra, o contrário de "o poder custa a humanidade"; casa mal com despertar por evento |
| **2. Oculta, no corpo** | A barra sai do HUD; o número continua lá dentro (finais, reações); a marca azul no pulso cresce por faixas; a árvore, no menu, mostra os despertares como glifos | **Hellblade** (a podridão sobe pelo braço da Senua, sem número) | Diegético, combina com o terror e com o "+3 escondido" do wadi; a marca já existe | O jogador sente, mas não mede; precisa de faixas bem visíveis na marca |
| **3. Oculta, com resumo** | Como a 2, e no fim de cada região (na demo, a tela de fim) aparece o estado em palavras ("Marcada") | **Dishonored** (o Caos fica escondido e aparece no fim da missão) | Mostra o peso das escolhas sem virar meta | Um texto a mais na tela de fim |

**Recomendação: opção 3** (a 2 durante o jogo, com o resumo no fim). Dá ao terror o que ele pede (o custo se vê no corpo, não num número) e ainda mostra ao jogador, uma vez, onde ele chegou. **Muda a apresentação descrita no GDD** ("barra de humano a divino"), então precisa da sua aprovação; o número continua igual por dentro. O aviso "Transformação +5" da leitura da tabuleta vira, no ME7, uma frase curta, sem número.

### f) Testes

**Nível 1 (novos):**

| Teste | O que confere | Tempo |
| --- | --- | --- |
| **Nibiru > Testes > Poderes > Teste de dados** (sem Play) | Todo poder tem código não vazio e único, está no registro e na lista de códigos, tem as chaves no `pt-BR.json`, recarga e duração ≥ 0, uma vaga válida; na demo, uma vaga por poder; todo poder tem o seu componente de efeito no prefab da Elena | segundos |
| **Nibiru > Testes > Poderes > Teste automático** (Play, controle e teclado virtuais, `SlotGuard`) | 1. Ganhar, perguntar e perder (`Grant`, `Has`, `Revoke`, evento `Changed`). 2. Passo 8: ler a tabuleta dá o Eco uma vez só. 3. Morrer antes e depois de ganhar: o checkpoint devolve o certo. 4. Save de teste: ida e volta pelo disco com `poderes`; arquivo **sem o campo** e com o evento antigo carrega com o Eco; sem nenhum dos dois, sem poder; código desconhecido fica ilegível. 5. LB tocado: Eco **ao soltar**, nunca ao apertar; mede o atraso. 6. LB+RB: Sopro de teste, sem faca e sem Eco. 7. LB+Y: Égide de teste, sem Eco. 8. RB sozinho: faca. 9. LB seguro + B: esquiva e sem Eco. 10. LB seguro mais de 0,5 s e solto: nada. 11. Mirando: LB solto dá Eco; LB+RB dá Sopro; RB sozinho, nada. 12. Q, Z, X e Mouse 4/5 no teclado, na hora. 13. Poder que ela não tem: nada (e LB+RB não vira faca). 14. Diálogo, pausa, inventário, memória e viagem: nenhum poder dispara. 15. D-pad cima e baixo: zoom de 0,25 m; mirando, em menu e em diálogo, não; no menu o D-pad continua navegando. 16. Recarga: dentro dela o poder não dispara | ~2 min (estimado) |

Para os casos 6 e 7, como o Sopro e a Égide ainda não existem, o teste usa **dois poderes de teste** (`Data/Powers/Teste_PoderVaga2` e `Teste_PoderVaga3`, prefixo `Teste_`, só registram que dispararam). Eles nunca são dados no jogo de verdade.

**Atalhos para o teste do dono (Nibiru > Testes > Poderes):** 1. Antecâmara, com o Eco e os dois poderes de teste (cada disparo mostra na tela qual poder saiu e quantos ms depois do LB); 2. Aldeia antes do passo 8 (sem poder; ler a tabuleta). O primeiro serve para sentir o atraso do LB num controle de verdade.

**Nível 2** (o ME1 toca salvamento, prefab da Elena, controles e localização; tempos da tabela do ESTADO):

| Teste | Tempo |
| --- | --- |
| Salvamento > Teste automático | ~3 min |
| Salvamento > Teste de códigos | segundos |
| Salvamento > Teste da proteção dos slots | segundos |
| Teste geral dos atalhos | ~8,5 min |
| Mouse > Teste automático | ~1 min |
| Animações > 4. Teste automático | ~10 s |
| Câmera > Zoom: teste automático (o D-pad usa o mesmo zoom) | ~2,5 min |
| Localização > Teste de chaves e códigos | segundos |
| Passo 11 > 7 (o Lullu reage ao Eco) | ~2 min |
| Passo 12 > 7 (o Eco atordoa o Servo; o teste lê a recarga do Eco) | ~2 min |

Total do nível 2: **~20 min**, mais ~2 min do nível 1. Um por vez, conferindo "slots reais iguais" no log novo de cada um.

**Nível 3 (regressão completa), porque o ME1 fecha um sistema:** tudo o que está acima, mais Passo 10 (~2,5 min), Passo 13, 14 e 15 (~2 min cada), Remédio para Yusuf (~3 min), Arquivo (~1 min), O que o Tom viu (~30 s), Suprimentos (segundos) e Teste dos corpos da equipe (segundos). **Total estimado: ~35 min**, um por vez. O resultado vira a nova referência no ESTADO (hoje: 100 OK, 0 FALHOU, 48 pulados no teste geral). Os atalhos novos de Poderes devem subir o número de OK do teste geral; o teste automático de Poderes entra como "pulado" pela regra `.teste`.

### g) Arquivos e ordem de trabalho (para quando o plano for aprovado)

**Criados:** `Scripts/Powers/PowerBook.cs`, `PowerBehaviour.cs`, `PlayerPowers.cs`; `Scripts/Editor/Powers/PowerTests.cs`, `PowerShortcuts.cs`, `PowerSetup.cs` (menu "Montar poderes na Elena"); `Data/Powers/Teste_PoderVaga2` e `Teste_PoderVaga3`.

**Alterados:** `PowerDefinition.cs`, `EchoOfAbzu.cs`, `TabletReader.cs` (rodapé com chave: `ui.poder.novo` com `{0}`), `PlayerInputReader.cs` (vagas, regra do LB, zoom do D-pad), `PlayerCameraController.cs` (ler o zoom do D-pad), `NibiruControles.inputactions`, `WorldState.cs`, `SaveSlots.cs`, `SaveRegistry.cs`, `PowerHUD.cs` (só passa a ler o `PlayerPowers`; o HUD novo é do ME5), `Step9Shortcuts.cs`, prefab da Elena (pelo menu), `pt-BR.json` e `en.json`, `Codigos-Salvamento.txt`, `ESTADO.md`, a Bíblia e a Região 1 (botões aprovados) e a tabela de chaves do `CLAUDE.md` (tipo novo `power.<Nome>.name`, `.desc`, `.hint`).

**Ordem, um passo testável por vez:** 1. dados e posse (com o save e o teste de dados); 2. controles (vagas, regra do LB, zoom do D-pad, teste automático); 3. atalhos do dono e documentação; 4. nível 2; 5. teste do dono no Play; 6. nível 3 para fechar.

## 3. Discordâncias e acréscimos às recomendações do dono

1. **Poderes por vaga, não por nome:** as ações do mapa são `Poder1/2/3` e cada poder diz a vaga dele. Assim um ME futuro não mexe no mapa de controles (sistema compartilhado), e o jogo inteiro, com mais poderes que botões, pode trocar o que está em cada vaga depois.
2. **Limite de 0,5 s no Eco ao soltar** (regra 5), um acréscimo à regra do dono. Pode ser desligado.
3. **Com o LB seguro, o RB nunca vira faca**, nem quando a Elena ainda não tem o Sopro.
4. **A recarga sai do `EchoOfAbzu` e vai para o `PlayerPowers` já no ME1** (o ME5 continua dono do HUD), para o ME5 e o ME6 não terem que refazer.
5. **A posse do poder sai dos eventos** (`Poder:*`) para um campo próprio, com conversão automática dos saves de hoje.
6. **Barra de transformação: oculta** (opção 3), contra o texto atual do GDD ("barra de humano a divino").

## 4. Perguntas abertas (respondidas na aprovação; veja a seção 6)

1. **Custo por uso** dos poderes: existe (transformação, vida, outro) ou o custo é só ao decifrar? O repositório não diz.
2. **Eco ao soltar:** aceita o limite de 0,5 s (segurar mais e soltar = desistir)?
3. **O que o Sopro faz** no espaço (cone, projétil, empurrão; mira ou não) e **o que a Égide faz** (escudo, invulnerabilidade, menos dano)? Não está no repositório; o ME1 não precisa, mas o ME3 e o ME4 sim. Por favor, traga do documento de design.
4. **ME7:** quantos despertares há na demo e no jogo, o que dispara os seguintes e o que a árvore contém; e se a transformação tem ligação com os despertares.
5. **Zoom no D-pad** (cima e baixo) e **D-pad esquerda e direita reservados para a troca de arma do B6**: aprova?
6. **Poderes mirando:** o padrão "valem mirando" serve?
7. **Barra de transformação:** opção 1, 2 ou 3?
8. **Remapear dentro do jogo** fica para a fase D (Opções maiores), junto com a opção "Eco ao apertar"?

## 5. Pendências registradas

- **ME2:** controles do puzzle (sugestão em (d)).
- **ME3:** campo "tipo" nos inimigos (humano, híbrido, Anunnaki) para a eficácia do Sopro.
- **ME5:** dica "RB Sopro · Y Égide" com o LB seguro; HUD dos 3 poderes.
- **B6:** troca de arma no D-pad esquerda e direita e nas teclas 1 e 2.
- **Fase D:** tela de remapear; opção "Eco ao apertar".
- **Observação, fora do ME1:** não dá para pausar durante uma conversa.

## 6. Decisões da aprovação (4/nov/2026)

Respostas do dono, trazidas do documento de design. Valem acima das seções 2 a 5.

**Respostas às perguntas abertas:**

| # | Pergunta | Resposta |
| --- | --- | --- |
| 1 | Custo por uso | **Não existe.** O único limite é a recarga, contada **só em tempo de jogo**: não anda com o jogo pausado, em menus nem no inventário (tempo do jogo, `Time.deltaTime`, que para quando a pausa, o inventário, a leitura e os diálogos param o tempo) |
| 2 | Eco ao soltar, limite de 0,5 s | **Aprovado**: LB seguro por mais de 0,5 s cancela o Eco; com o LB seguro, o RB nunca vira faca |
| 3 | O que o Sopro e a Égide fazem | **Sopro de Enlil:** ataque; muito eficaz contra Anunnaki, dano médio contra híbridos, fraco contra humanos; recarga 90 s. **Égide de Enki:** buff/defesa; dura 8 s; recarga 60 s; o efeito exato fica para o ME da Égide (ME4). **No ME1 os dois entram só como dados** (vaga, duração, recarga e textos com chave), sem efeito e sem fonte que os dê |
| 3b | Eco do Abzu | Recebido no altar da Aldeia (passo 8), sem puzzle; atordoa (usado contra o Primeiro Servo) |
| 4 | Despertares | A demo tem **um**, disparado ao ler a Égide (passo 13), antes do wadi. O total do jogo completo está em aberto. A árvore tem **três ramos** (combate, furtividade, poderes) e na demo aparece **inteira, com os nós bloqueados** (ME7) |
| 5 | Zoom no D-pad e D-pad esquerda/direita para o B6 | **Aprovado**: D-pad cima e baixo fazem zoom; D-pad esquerda/direita e as teclas 1 e 2 ficam reservados para a troca de arma do B6 |
| 6 | Poderes mirando | Padrão "valem mirando" mantido (campo `usableWhileAiming`) |
| 7 | Barra de transformação | **O valor continua por dentro e a barra sai do HUD.** A marca no pulso que cresce e o estado em palavras no fim da demo ficam para o **ME7** e a **fase C**. A barra sai do HUD no ME7, junto com a troca pela marca (no ME1 nada muda na tela) |
| 8 | Remapear | Sem tela de remapear no ME1 (fase D, Opções maiores) |

**Na demo a Elena não perde nem descarta ME.** O `PowerBook.Revoke` continua existindo, sem uso no jogo (só nos testes).

**HUD com um ícone e um anel de recarga por ME: entra no ME5** ("Recarga e HUD", que depende do ME3 e do ME4 na tabela da governança), não no ME1 nem no ME3. Motivos: o HUD precisa dos três ícones (arte que ainda não existe) e do estado "ativo" da Égide (ME4). O ME1 deixa pronto o que o HUD vai ler (`PlayerPowers.StateOf`, `PowerBook.CooldownLeft`) e mantém o HUD de texto de hoje, que passa a mostrar cada poder possuído com a recarga. Se o dono quiser o anel antes, a alternativa é o ME3 (Sopro).

**Ajuste no save (muda a seção 2c):** a Bíblia diz que o save guarda os ME obtidos, a recarga restante em segundos de jogo e os despertares, e que esses campos entram **antes** da regra de versão. Então o **ME1 grava os três já agora**, todos dentro de `mundo` (fazem parte do estado do mundo, que o checkpoint congela e a morte restaura):

| Campo | Formato | Exemplo | Falta no arquivo (save antigo) = |
| --- | --- | --- | --- |
| `poderes` | lista de códigos | `["poder.eco_do_abzu"]` | convertido dos eventos `Poder:<nome do arquivo>`; sem eles, nenhum poder |
| `recargaDosPoderes` | código → segundos de jogo restantes | `{"poder.eco_do_abzu": 4.5}` | todas zeradas (prontos) |
| `despertares` | lista de códigos | `[]` (vazia até o ME7) | vazia |

- Continua na **versão 1**. **Não** cria a regra de versão do save.
- Os três são sempre gravados (mesmo vazios).
- Código de poder desconhecido em `poderes` ou em `recargaDosPoderes` deixa o slot ilegível (a regra de sempre). Recarga negativa ou inválida vira 0; maior que a recarga do poder é limitada a ela. Os despertares ainda não têm registro (o ME7 cria); até lá, a lista é lida como está.
- **Efeito no jogo:** antes, o Eco voltava sempre pronto depois de morrer ou carregar. Agora ele volta com a recarga que tinha **no momento do checkpoint** (quase sempre zero, porque os checkpoints ficam fora das lutas).
- O campo de recarga ficou em `mundo`, e não em `checkpoint` como a seção 2c previa: assim a posse, a recarga e os despertares ficam juntos no `WorldState`, a única fonte de verdade.

**Atraso do LB:** a proposta (Eco ao soltar o LB) é a padrão. O **plano B (Eco no Y sozinho; LB só modificador)** fica como **opção de configuração no código** (campo `ecoScheme` no `PlayerInputReader` do prefab da Elena), não no menu. **Último passo antes de fechar o ME1:** o dono testa os **dois atalhos do LB** (Nibiru > Testes > Poderes) num controle de verdade e escolhe; depois vem a regressão completa (nível 3).

**Ordem de trabalho (substitui a da seção 2g):**
1. Etapa 1: dados e posse (`PowerDefinition`, `PowerBook`, Sopro e Égide como dados, save com os três campos, registro e códigos, textos com chave, teste de dados). Testes de nível 1 e 2, commit e push.
2. Etapa 2: controles (vagas, regra do LB com o plano B na configuração, `PlayerPowers` com a recarga em tempo de jogo, Eco como `PowerBehaviour`, zoom no D-pad, teste automático). Testes de nível 1 e 2, commit e push.
3. Etapa 3: os dois atalhos do LB para o dono, documentação completa. Testes, commit e push.
4. Teste do dono no Play, com a escolha do LB.
5. Regressão completa (nível 3) para fechar o ME1. **Não roda antes do passo 4.**

## 7. O que mudou na implementação (4/nov/2026)

Tudo como na seção 6, com estes ajustes:

- **Códigos dos poderes de teste:** `poder.teste_poder_vaga_2` e `poder.teste_poder_vaga_3` (o gerador de códigos separa o número: "Vaga2" vira "vaga_2"). Já estão na lista de códigos permanentes.
- **Onde fica a regra do LB:** no `PlayerInputReader` (`EvaluatePowerButtons`, uma vez por quadro), usando as ações do mapa (`ModificadorPoderes`, `Atacar` vindo do controle e `Poder3Combo`), e não a opção global do Input System. "Outro botão" é qualquer botão do mesmo controle (A, B, X, gatilhos, Start, Select, L3, R3, D-pad); mexer os analógicos não cancela.
- **Plano B como configuração:** campo `ecoScheme` do `PlayerInputReader` no prefab da Elena (padrão: plano A). Os atalhos do LB trocam o esquema só para aquele atalho (`SchemeOverride`, que todo outro atalho zera e que some ao sair do Play).
- **Rótulos dos botões:** `PlayerInputReader.PowerSlotLabel` (teclado: só a primeira tecla, "Z" e não "Z | Mouse 4"; controle: "LB", "LB + RB", "LB + Y" ou "Y" no plano B).
- **Recarga e duração:** a recarga fica no `WorldState` (por código), contada pelo `PlayerPowers` com `Time.deltaTime`; a duração (estado "ativo", para a Égide) fica só na memória do `PlayerPowers` e não vai para o save.
- **Atraso medido do Eco ao soltar o LB:** ~20 ms num toque de 6 quadros do controle virtual (o atraso real é a duração do toque do dedo; o dono mede no atalho).
- **HUD:** o `PowerHUD` de texto passou a mostrar uma linha por poder possuído (os de teste não aparecem nele), com textos com chave.
- **Atalhos do LB:** na etapa 2, junto com os controles (os testes usam a mesma montagem), e não na 3. Lugar: o nicho da Aldeia, logo depois da tabuleta (calmo), e não a antecâmara.
- **Teste automático:** 18 cenários em vez de 16 (entraram a tabuleta dando o Eco e a recarga só em tempo de jogo); ~50 s.
