# Catálogo de animações

Biblioteca bruta em `ArteFonte/Animacoes/` (fora de `Assets`, fora do Git, a Unity não a importa). Mixamo: FBX Binary, 30 fps, pose T, "sem pele" (cada pacote traz também um `X Bot.fbx`, o boneco de referência). As descrições abaixo foram deduzidas pelo nome do arquivo; nenhuma foi conferida aqui no Play.

**Como usar:** consulte este catálogo, copie para `Assets/_Nibiru/Art/Animacoes/Mixamo` só as que forem usadas e importe como Humanoid (veja "Animações do Mixamo" no `ESTADO.md`).

**Marcas das pendências atuais:**
- **[GARRA]** golpe de garra da criatura (Lullu e Primeiro Servo)
- **[URRO]** urro da criatura
- **[SAÍDA]** saída das rachaduras (levantar-se / surgir)
- **[CRIA]** outros usos de criatura (andar, correr, parado, morte, giro)
- **[LOCO]** locomoção da Elena
- **[SENTADO]** sentado na caminhonete (Elena na caçamba)
- **[DIRIGIR]** dirigir (Samir na cabine)

Lacuna importante: **não há "sitting idle" em nenhum pacote do Mixamo, nem na pasta `Avulsas`** (busca por sit, seat, ground, floor, sentad, crouch, squat, kneel, crawl e agach). O que existe:
- `UAL1_Standard.fbx` (Universal Animation Library 1, `Unity/`) tem `Sitting_Idle_Loop`, `Sitting_Talking_Loop`, `Sitting_Enter`, `Sitting_Exit`, `Driving_Loop` e `Crouch_Idle_Loop`. Importa como Humanoid sem problema, mas o sentado é de **cadeira ou banco**: medido no modelo da Elena, o quadril fica a 0,45 m e o joelho a 0,45 m do chão, com os pés no chão. **Não é sentado no chão.**
- `X Bot@Driving` e `X Bot@Entering Car` são sentados de banco de carro.
- Agachar: na `Avulsas` só há `Sneak Walk` (já usado, com um quadro parado como "agachada parada") e `X Bot@Walk Crouching Right`. Há "Crouch Idle" no Pro Magic Pack, no Pro Melee Axe Pack e no UAL1.

**Origem dos pacotes:** as pastas `Pro Magic Pack`, `Magic Locomotion Pack` e `Scary Zombie Pack` estão em `ArteFonte/Animacoes/Mixamo`, mas não constavam da lista de pacotes do projeto; a origem delas não foi confirmada, e **nada delas foi usado**.

## Clipes em uso no jogo (importados)

Tabela única: `Scripts/Editor/Animacoes/AnimationLibrary.cs`. Os FBX vão para `Assets/_Nibiru/Art/Animacoes/<Criatura|Elena|Caminhonete>/` (nome do arquivo = nome do clipe) e o `MixamoClipPostprocessor` os importa sempre igual: Humanoid com o avatar do X Bot, Loop Time pela tabela, giro e altura da raiz travados. O avanço da raiz é descartado em todos (quem anda é o jogo); "raiz anda" abaixo diz só se o arquivo original vem com deslocamento (medido pela velocidade média do clipe), que é lido para sincronizar os pés. Menus: **Nibiru > Animações > Importar clipes da biblioteca** (copia da `ArteFonte`) e **Reimportar clipes** (depois de mudar a tabela).

| Clipe | Arquivo original (`Mixamo/…`) | Tipo | Raiz anda | Uso |
| --- | --- | --- | --- | --- |
| Lullu_Parado | Not So Scary Zombie Pack/zombie idle | loop | não | Lullu parado |
| Lullu_Andar | Not So Scary Zombie Pack/walking | loop | sim (0,5 m/s) | Lullu vagando e investigando |
| Lullu_Correr | Not So Scary Zombie Pack/zombie running | loop | sim (3 m/s) | Lullu caçando |
| Lullu_Ataque | Not So Scary Zombie Pack/zombie attack, quadros 18 a 62 | uma vez | não | golpe do Lullu (toca a 2x) |
| Lullu_Saida_Rachaduras | Not So Scary Zombie Pack/zombie stand up | uma vez | não | **importado, sem uso** (veja abaixo) |
| Servo_Parado | Creature Pack/mutant breathing idle | loop | não | Servo parado |
| Servo_Andar | Creature Pack/mutant walking | loop | sim (1,3 m/s) | Servo vagando e investigando |
| Servo_Correr | Creature Pack/mutant run | loop | sim (2,3 m/s) | Servo caçando |
| Servo_Ataque | Creature Pack/mutant swiping, quadros 14 a 56 | uma vez | não | golpe do Servo (toca a 1,7x) |
| Servo_Investida | Creature Pack/mutant jump attack, quadros 1 a 75 | uma vez | sim | preparação e salto da investida (estado `Investida` da IA) |
| Servo_Urro | Creature Pack/mutant roaring, quadros 10 a 70 | uma vez | não | urro da luta (`FirstServantFight.IsRoaring`: ao acordar e a cada troca de fase) |
| Criatura_Golpe_Recebido | Not So Scary Zombie Pack/zombie reaction hit, quadros 8 a 40 | uma vez | não | dano dos dois (camada aditiva) |
| Criatura_Morte | Creature Pack/mutant dying | uma vez | não | morte dos dois |
| Elena_Parada_Basic | Female Basic Locomotion Pack/idle | loop | não | **só vitrine**: comparar com a `Parada` atual |
| Elena_Andar_Basic | Female Basic Locomotion Pack/walking | loop | sim (1,6 m/s) | só vitrine (compara com `Andar`) |
| Elena_Correr_Basic | Female Basic Locomotion Pack/running | loop | sim (3,6 m/s) | só vitrine (compara com `Correr`; mais lenta que o pique atual de 5,4 m/s) |
| Elena_Virar_Esquerda / _Direita | Female Basic Locomotion Pack/left turn, right turn | uma vez | não | importados, sem uso (o jogo gira o corpo por código) |
| Elena_Pegar | Avulsas/X Bot@Picking Up Object | uma vez | não | importado, sem uso (veja abaixo) |
| Elena_Abrir | Avulsas/X Bot@Open Door Outwards | uma vez | sim (0,5 m/s) | importado, sem uso (veja abaixo) |
| Samir_Dirigindo | Avulsas/X Bot@Driving | loop | não | Samir ao volante na caminhonete (estado `Dirigindo` do `Samir.controller`) |

**Importados e sem ligação, porque nenhum evento ou estado do jogo serve:** `Lullu_Saida_Rachaduras` (nada no jogo faz criatura surgir das rachaduras: o Servo acorda no lugar, urrando); `Elena_Pegar` e `Elena_Abrir` (o `PlayerInteractor` não tem evento de interação a que ligar); as viradas da Elena. Estão na vitrine **Nibiru > Testes > Animações**.



---

## Avulsas (já usadas, em maioria)

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| Idle | parado | LOCO (em uso) |
| WalkForward | andar para frente | LOCO (em uso) |
| Walking | andar segurando fuzil | soldados (em uso) |
| Female Walk | andar de passarela, queixo erguido | não serviu à Elena |
| Fast Run | correr rápido | LOCO (em uso) |
| Run | corrida de pernas abertas | não usar |
| Run Forward | correr para frente | LOCO (a conferir) |
| Run Look Back | correr olhando para trás | — |
| Unarmed Run Forward / Unarmed Run Back | correr sem arma, para frente / de costas | LOCO |
| Sneak Walk | andar furtivo agachado | LOCO (em uso) |
| X Bot@Walk Crouching Right | andar agachado para a direita | LOCO |
| Stand To Roll | levantar e rolar | LOCO (em uso) |
| jump forward / jump backward | pulo para frente / para trás | LOCO |
| start walking / stop walking | começar / parar de andar | LOCO |
| start walking backwards / walking backwards / walk backwards stop | andar de costas (início, loop, parada) | LOCO |
| run backwards | correr de costas | LOCO |
| strafe, strafe (2) | passo lateral | LOCO |
| Pistol Aim | mirar pistola | em uso |
| rifle aiming idle / rifle run / firing rifle | fuzil: mirar, correr, atirar | soldados (em uso) |
| Reloading | recarregar | em uso |
| Stabbing | estocada de faca | em uso; provisório para GARRA |
| Stable Sword Inward Slash / Outward Slash | golpe de espada para dentro / para fora | melee |
| Hit Reaction | reação a dano | em uso |
| Standing React Death Backward | morte caindo para trás | em uso |
| walking to dying | andar e morrer | soldados (em uso) |
| Laying Idle / Laying Shaking Head | deitado parado / balançando a cabeça | NPC (Yusuf) |
| Look Around | olhar em volta | NPC |
| Talking, Talking2, Talking3, Talking4 | gestos de conversa | Samir (em uso) |
| X Bot@Driving | dirigindo | **DIRIGIR** (Samir); serve de base para SENTADO |
| X Bot@Entering Car | entrar no carro | **SENTADO / DIRIGIR** (embarque) |
| X Bot@Exiting Car | sair do carro | SENTADO / DIRIGIR (desembarque) |
| X Bot@Dropping | largar/deixar cair algo | interação |
| X Bot@Open Door Outwards / Opening Door Inwards | abrir porta para fora / para dentro | interação |
| X Bot@Picking Up Object / Picking Up | pegar objeto do chão | interação |
| X Bot | boneco de referência | — |

## Action Adventure Pack

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| idle, idle (2), (3), (4), (5) | parado, variações | LOCO |
| walking | andar | LOCO |
| running | correr | LOCO |
| run to stop | correr e parar | LOCO |
| left turn / right turn | giro para a esquerda / direita | LOCO |
| jumping up | pulo | LOCO |
| falling idle | caindo | LOCO |
| falling to roll | cair e rolar | LOCO |
| hard landing | queda dura | LOCO |
| crouched sneaking left / right | andar agachado para os lados | LOCO |
| left cover sneak / right cover sneak | esgueirar-se encostada na cobertura | LOCO (furtivo) |
| stand to cover, stand to cover (2) | ir para a cobertura | LOCO |
| cover to stand, cover to stand (2) | sair da cobertura | LOCO |

## Creature Pack

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| mutant swiping | patada/arranhão com a garra | **GARRA** |
| mutant punch | soco | **GARRA** (alternativa) |
| mutant jump attack, jump attack | ataque com salto | **GARRA** (investida do Servo) |
| mutant roaring | urro | **URRO** |
| mutant flexing muscles | exibir os músculos | URRO / SAÍDA (alternativa) |
| mutant jumping, mutant jumping (2) | pulo | CRIA; SAÍDA (surgir saltando) |
| mutant idle, idle (2) | parado | CRIA |
| mutant breathing idle | parado respirando | CRIA |
| mutant walking | andar | CRIA |
| mutant run | correr | CRIA |
| mutant dying | morrer | CRIA |
| left turn 45, mutant left turn 45 | giro 45° esquerda | CRIA |
| mutant right turn 45, (2) | giro 45° direita | CRIA |
| mutant right turn 90 | giro 90° direita | CRIA |

## Creature NPC Pack

Versão enxuta do Creature Pack, mesmos movimentos.

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| mutant swiping | arranhão com a garra | **GARRA** |
| mutant flexing muscles | exibir os músculos | URRO / SAÍDA (alternativa) |
| mutant jumping | pulo | CRIA; SAÍDA |
| mutant idle, mutant breathing idle | parado, parado respirando | CRIA |
| mutant walking, mutant run | andar, correr | CRIA |
| mutant dying | morrer | CRIA |
| left turn 45, mutant left turn 45, mutant right turn 45, (2) | giros de 45° | CRIA |

## Not So Scary Zombie Pack

Criatura mais cambaleante e arrastada; boa para o Lullu (mais fraco) e para a saída do chão.

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| zombie stand up, (2), (3) | levantar-se do chão | **SAÍDA** (surgir das rachaduras) |
| zombie transition, (2) | transição (de deitado/arrastando para de pé) | SAÍDA |
| zombie attack | ataque | **GARRA** (Lullu) |
| zombie punching, (2) | socos | GARRA |
| zombie headbutt | cabeçada | GARRA (alternativa) |
| zombie kicking, (2) | chutes | GARRA (alternativa) |
| zombie agonizing | agonizando | CRIA (atordoado pelo Eco) |
| zombie reaction hit, (2) | reação a dano | CRIA |
| zombie idle, (2), (3), (4) | parado | CRIA |
| zombie scratch idle | parado se coçando | CRIA |
| walking, walking (2) | andar arrastado | CRIA (andar do Lullu) |
| zombie stumbling | tropeçando | CRIA |
| zombie running | correr | CRIA |
| zombie turn | girar | CRIA |

## Female Basic Locomotion Pack

Base natural para a Elena (corpo feminino).

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| idle | parada | LOCO |
| walking | andar | LOCO |
| running | correr | LOCO (candidata à corrida definitiva) |
| jump | pulo | LOCO |
| left strafe walk / right strafe walk | andar de lado | LOCO |
| left strafe / right strafe | correr de lado | LOCO |
| left turn, (2) / right turn, (2) | giros | LOCO |

## Male Locomotion Pack

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| idle, walking, standard run, jump | parado, andar, correr, pulo | NPCs masculinos (Samir) |
| left/right strafe, left/right strafe walking | passo lateral | NPCs |
| left/right turn 90 | giro 90° | NPCs |

## Gestures Pack Basic

Gestos para conversas (Samir, NPCs, Elena).

| Arquivo | Movimento |
| --- | --- |
| acknowledging | assentir reconhecendo |
| angry gesture | gesto de raiva |
| annoyed head shake | balançar a cabeça irritado |
| being cocky | ar de convencido |
| dismissing gesture | gesto de dispensar |
| happy hand gesture | gesto de mão alegre |
| hard head nod / head nod yes / lengthy head nod | acenos de "sim" |
| sarcastic head nod | aceno sarcástico |
| shaking head no | "não" com a cabeça |
| thoughtful head shake | cabeça pensativa |
| look away gesture | desviar o olhar |
| relieved sigh | suspiro de alívio |
| weight shift | trocar o peso entre as pernas |

## Pistol_Handgun Locomotion Pack

| Arquivo | Movimento | Marca |
| --- | --- | --- |
| pistol idle | parada com pistola | LOCO (armada) |
| pistol walk, walk arc, (2) | andar | LOCO |
| pistol walk backward, backward arc, (2) | andar de costas | LOCO |
| pistol run, run arc, (2) | correr | LOCO |
| pistol run backward, backward arc, (2) | correr de costas | LOCO |
| pistol strafe, (2) | passo lateral | LOCO |
| pistol jump, (2) | pulo | LOCO |
| pistol stand to kneel / kneeling idle / kneel to stand | ajoelhar, ajoelhada parada, levantar | LOCO |

## Pro Rifle Pack

Locomoção completa com fuzil (soldados e eventual Elena armada).

| Arquivo | Movimento |
| --- | --- |
| idle, idle aiming, idle crouching, idle crouching aiming | parado (em pé, mirando, agachado) |
| walk forward / backward / left / right (+ forward left/right, backward left/right) | andar nas 8 direções |
| run forward / backward / left / right (+ diagonais) | correr nas 8 direções |
| sprint forward / backward / left / right (+ diagonais) | disparada nas 8 direções |
| walk crouching forward / backward / left / right (+ diagonais) | andar agachado nas 8 direções |
| turn 90 left / right, crouching turn 90 left / right | giros |
| jump up / jump loop / jump down | pulo em três partes |
| death from the front, from the back, from right | mortes |
| death from front headshot, from back headshot, crouching headshot front | mortes por tiro na cabeça |

Marca: LOCO (armada) e soldados. Mortes servem a soldados.

## Pro Melee Axe Pack

Combate corpo a corpo com machado; os "unarmed" servem sem arma.

| Arquivo | Movimento |
| --- | --- |
| standing idle, idle looking ver. 1 e 2 | parado com machado |
| unarmed idle, idle looking ver. 1 e 2 | parado sem arma |
| crouch idle, crouch to standing idle | agachado, levantar |
| standing walk forward/back/left/right, run forward/back | andar e correr com machado |
| unarmed walk forward/back, run forward/back | andar e correr sem arma |
| standing/unarmed turn left 90, turn right 90 | giros |
| standing jump, unarmed jump, unarmed jump running | pulos |
| standing melee attack horizontal / downward / backhand | golpes simples |
| standing melee attack 360 high / low | giro de 360° |
| standing melee attack kick ver. 1 e 2 | chutes |
| standing melee combo attack ver. 1, 2, 3 | combos |
| standing melee run jump attack | ataque correndo e saltando |
| standing block idle, block react large | defender, reagir ao bloqueio |
| standing react large from left / right / gut | dano forte (esquerda, direita, barriga) |
| standing disarm over shoulder / underarm | desarmar |
| unarmed equip over shoulder / underarm | empunhar |
| standing taunt battlecry / chest thump | provocar: grito de guerra, bater no peito (**URRO**, alternativa humanoide) |

## Pro Magic Pack

Magia e combate desarmado (poderes dos Anunnaki, fase futura).

| Arquivo | Movimento |
| --- | --- |
| Standing 1H Magic Attack 01, 02, 03; standing 1H cast spell 01 | feitiço com uma mão |
| Standing 2H Magic Attack 01 a 05; Standing 2H Cast Spell 01 | feitiço com as duas mãos |
| Standing 2H Magic Area Attack 01, 02 | feitiço em área |
| Standing Block Start / Idle / End / React Large | defesa |
| Standing Idle 03, 04; standing idle; standing idle 02 | paradas |
| Standing Walk Forward / Back / Left / Right | andar |
| Standing Run Forward / Back / Left / Right; Sprint Forward | correr |
| Standing Turn Left 90 / Right 90 | giros |
| Standing Jump, Jump Running, Jump Running Landing, Land To Standing Idle | pulos |
| Standing Idle To Crouch; Crouch Idle; Crouch To Standing Idle | agachar e levantar |
| Crouch Walk Forward / Back / Left / Right; Crouch Turn Left 90 / Right 90 | agachada andando e girando |
| Standing React Small From Front / Back / Left / Right | dano leve |
| Standing React Large From Front / Back / Left / Right | dano forte |
| Standing React Death Backward / Forward / Left / Right | mortes |

## Universal Animation Library (UAL1 e UAL2)

Pacotes de outra fonte (Standard, fora do Mixamo), em `Universal Animation Library[Standard]/` e `Universal Animation Library 2[Standard]/`; ignorados pelo Git por uma regra à parte. Trazem as animações dentro de um único FBX (`Unity/UAL1_Standard.fbx`, `UAL2_Standard.fbx`) e um manequim feminino. **Ainda não catalogadas clipe a clipe**: abra o FBX numa conversa própria e liste os clipes (pode ter um sentado). Licença em `License.txt`.

---

## Resumo por pendência

| Pendência | Melhores candidatas |
| --- | --- |
| Golpe de garra (Lullu, Servo) | Creature Pack: `mutant swiping`, `mutant punch`, `mutant jump attack` (investida); Zombie Pack: `zombie attack` (Lullu) |
| Urro | Creature Pack: `mutant roaring`; alternativa `mutant flexing muscles` |
| Saída das rachaduras | Zombie Pack: `zombie stand up` (1, 2, 3), `zombie transition`; Creature: `mutant jumping` |
| Locomoção da Elena | Female Basic Locomotion Pack já importado só para comparação (veja a vitrine); a atual (aprovada) continua; Action Adventure (cobertura, rolar, cair), Pistol Handgun (armada) |
| Sentado (Elena na caçamba) | **pendente**: nada no Mixamo; `UAL1 Sitting_Idle_Loop` é de cadeira, não de chão. Decisão do dono (baixar "Sitting Idle" de chão no Mixamo) |
| Dirigindo (Samir) | **feito**: `Samir_Dirigindo` (`X Bot@Driving`) |
| Garra, urro, saída (criaturas) | **feitos**, menos a saída das rachaduras (importada, sem evento) |
