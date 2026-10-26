# Créditos e licenças

Origem e licença de cada asset de terceiros usado no projeto. **Todo asset novo entra nesta lista no mesmo commit em que entra no projeto** (inclusive texturas CC0 e objetos gerados no Meshy). Regra da etapa de arte: nada da Asset Store e nada pago sem combinar com o dono.

Situação: **confirmado** (lido no arquivo de licença ou nos termos), **a confirmar** (falta informação do dono) ou **resumo** (o que se sabe dos termos do serviço; conferir os termos atuais antes da página da Steam e do lançamento).

Última conferência: 26 de outubro de 2026.

## Personagens (modelos 3D)

| Asset | Onde está | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Elena, Samir, Yusuf, Umm Karim, Dra. Weiss, Soldado, Líder de esquadra, Atirador de elite, Piloto, Arqueólogo veterano, Arqueóloga jovem, Trabalhador da escavação, Lullu, Primeiro Servo e Ninsar (corpo, manto, saia e lança): malhas e texturas PBR | `ArteFonte/<Personagem>/1_Meshy` (fora do Git), cópias em `Assets/_Nibiru/Art/Personagens` | Gerados no **Meshy** (meshy.ai) pela conta do dono | Depende do plano da conta quando cada modelo foi gerado. Pelo que se sabe dos termos do Meshy: no plano gratuito o modelo sai público sob **CC BY 4.0** (exige atribuição nos créditos do jogo); nos planos pagos fica privado, do usuário | **A confirmar** (plano da conta) |
| Ninsar: 19 animações | `ArteFonte/Ninsar/2_Rig/Ninsar_Animacoes`, cópia em `Art/Personagens/Ninsar` | Meshy (animação automática) | A mesma do modelo | **A confirmar** (plano da conta) |
| Esqueletos com dedos (101 ossos) de todos os personagens acima | `ArteFonte/<Personagem>/2_Rig` | **AccuRig** (Reallusion), gratuito | Resumo: o personagem rigado pode ser usado em projetos comerciais | Resumo |
| Ninsar: montagem, encaixe e scripts do Blender | `ArteFonte/Ninsar/3_Blender` | Feitos no projeto (Blender) | Próprios | Confirmado |
| Ninsar: texturas do manto (`Ninsar_Tecido_Manto.png`, `Ninsar_Faixa_Bordado.png`) | `ArteFonte/Ninsar/Texturas` | Enviadas pelo dono | — | **A confirmar** (origem) |

## Animações

A biblioteca bruta fica em `ArteFonte/Animacoes` (fora do Git); só os clipes usados são copiados para `Assets/_Nibiru/Art/Animacoes`. O mapa arquivo → clipe está em `Docs/CATALOGO_ANIMACOES.md` e em `Scripts/Editor/Animacoes/AnimationLibrary.cs`.

| Asset | Onde está | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Clipes do Mixamo usados: Idle, WalkForward, Fast Run, Sneak Walk, Stand To Roll, Pistol Aim, Reloading, Stabbing, Hit Reaction, Standing React Death Backward, Talking 1 a 4, Walking, rifle aiming idle, rifle run, firing rifle, walking to dying e o boneco `X Bot` | `Art/Animacoes/Mixamo` | **Mixamo** (Adobe), pastas `Avulsas` e pacotes do Mixamo | Resumo: uso livre de royalties em projetos pessoais e comerciais, inclusive jogos; não se pode redistribuir os arquivos de animação soltos (só dentro do jogo) | Resumo |
| Clipes da biblioteca (Lullu, Primeiro Servo, criaturas, Elena básica, pegar, abrir, agachada, sentada, dirigir) | `Art/Animacoes/Criatura`, `Elena`, `Caminhonete` | Mixamo: `Not So Scary Zombie Pack`, `Creature Pack`, `Female Basic Locomotion Pack` e `Avulsas` | A mesma do Mixamo | Resumo |
| Universal Animation Library 1 e 2 (Standard) | `ArteFonte/Animacoes/Universal Animation Library[Standard]` e `... 2[Standard]` | **Quaternius** (patreon.com/quaternius) | **CC0 1.0** (domínio público; atribuição não exigida, mas fica aqui por cortesia) | Confirmado (`License.txt`). **Nenhum clipe usado no jogo ainda** |
| Pro Magic Pack, Magic Locomotion Pack, Scary Zombie Pack | `ArteFonte/Animacoes/Mixamo/` | Não confirmada | — | **Origem não confirmada: não usar.** Conferido em 26/out/2026: **nenhum uso** em `Assets` (nenhum arquivo deles foi copiado, comparando o conteúdo byte a byte; nenhum script, cena ou prefab os cita). Os dois nomes que coincidem (`X Bot` e `Standing React Death Backward`) vieram da pasta `Avulsas`, com conteúdo diferente do desses pacotes |

## Som, fonte e interface

| Asset | Onde está | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Sons provisórios (helicóptero, tiro, rádio, vento, urro, ambientes) | `Art/Audio/Provisorio` | Sintetizados por script do projeto (**Nibiru > Áudio > Gerar sons provisórios**, `Scripts/Editor/Audio/ProvisionalSounds.cs`) | Próprios | Confirmado |
| Fonte da interface | Fonte embutida da Unity (`UiFont.Current` com o tema vazio) | Unity | Parte do Unity, distribuível nos jogos feitos com ele | Resumo |
| Shader `RaioX` | `Art/Shaders` | Feito no projeto | Próprio | Confirmado |

## Pacotes do Unity

| Pacote | Licença | Situação |
| --- | --- | --- |
| Universal Render Pipeline, Input System, AI Navigation, Timeline, uGUI, Test Framework, Visual Scripting, Multiplayer Center, integrações de IDE, Collab Proxy | Termos do Unity (Unity Companion License) | Resumo |
| `com.unity.nuget.newtonsoft-json` (Json.NET, lê as tabelas de idioma) | Json.NET: MIT; o pacote, termos do Unity | Resumo |
| `com.unity.pipeline` (ponte MCP do editor) | Termos do Unity. Ferramenta de desenvolvimento, não entra no jogo | Resumo |
| `Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/Scenes/SampleScene` | Vieram com o modelo de projeto URP do Unity | Resumo |

## Só referência interna (não entra no build)

| Asset | Onde está | Origem | Situação |
| --- | --- | --- | --- |
| Imagens de referência dos personagens e deuses (Elena em 3 estágios, Enki, Enlil, Ereshkigal, Irena 1979 e 2003, Kast, Lullu, Ninmah, Ninsar, Samir, Tom) | `Assets/_Nibiru/Art/Referencias` | **A confirmar** | Conferido em 26/out/2026: **nenhuma cena, prefab, material ou controlador usa essas imagens** e a pasta não fica em `Resources`, então elas não entram no build. Se forem de terceiros, continuam só como referência interna |
| Imagens de referência de cada personagem | `ArteFonte/<Personagem>/Referencias` (e `Imagens De Referencia` da Ninsar) | **A confirmar** | Fora de `Assets`: nunca entram no build |

## Ferramentas (não entram no jogo)

Blender (GPL; não se aplica aos arquivos criados com ele), AccuRig, Meshy, Unity CLI e a ponte MCP.

## Como registrar um asset novo

Uma linha na tabela da categoria, com: o que é, onde está no projeto, de onde veio (site e autor, com link), a licença (CC0, CC BY, termos do serviço...), o que ela exige (atribuição, proibição de revenda do arquivo solto) e a situação. Texturas CC0 (ambientCG, Poly Haven) e objetos do Meshy entram assim, na conversa em que forem usados.
