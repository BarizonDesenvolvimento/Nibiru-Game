# Créditos e licenças

Origem e licença de cada asset de terceiros usado no projeto. **Todo asset novo entra nesta lista no mesmo commit em que entra no projeto** (inclusive texturas CC0 e objetos gerados no Meshy). Regra da etapa de arte: nada da Asset Store e nada pago sem combinar com o dono.

Situação: **confirmado** (lido no arquivo de licença ou nos termos), **a confirmar** (falta informação do dono) ou **resumo** (o que se sabe dos termos do serviço; conferir os termos atuais antes da página da Steam e do lançamento).

Última conferência: 26 de outubro de 2026 (plano do Meshy confirmado: Pro).

## Personagens (modelos 3D)

| Asset | Onde está | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Elena, Samir, Yusuf, Umm Karim, Dra. Weiss, Soldado, Líder de esquadra, Atirador de elite, Piloto, Arqueólogo veterano, Arqueóloga jovem, Trabalhador da escavação, Lullu, Primeiro Servo e Ninsar (corpo, manto, saia e lança): malhas e texturas PBR | `ArteFonte/<Personagem>/1_Meshy` (fora do Git), cópias em `Assets/_Nibiru/Art/Personagens` | Gerados no **Meshy** (meshy.ai) pela conta do dono, no plano **Pro (pago)** em todos os modelos (resposta do dono em 26/out/2026) | Resumo dos termos do Meshy para planos pagos: o modelo é privado e pertence ao usuário, com uso comercial; atribuição não exigida. Conferir os termos atuais antes da página da Steam | Confirmado (plano); termos: resumo |
| Elena nova (malha e textura de cor; aprovada pelo dono em 1º/nov/2026, substitui a antiga no jogo) | `ArteFonte/Elena_Nova_Usar/1_Meshy` (fora do Git), cópias em `Assets/_Nibiru/Art/Personagens/ElenaNova` | Gerada no **Meshy** (meshy.ai) pela conta do dono. No plano **Pro** (confirmado pelo dono em 1º/nov/2026) | A mesma dos outros modelos, se gerada no Pro. Conferir os termos atuais antes da página da Steam | A confirmar (plano) |
| Elena nova: imagens de referência | `ArteFonte/Elena_Nova_Usar/Referencias` | Geradas no **ChatGPT**, pela conta do dono, em plano pago (confirmado pelo dono em 1º/nov/2026) | Termos da OpenAI. Só referência interna: não entram no build | Confirmado |
| Ninsar: 19 animações | `ArteFonte/Ninsar/2_Rig/Ninsar_Animacoes`, cópia em `Art/Personagens/Ninsar` | Meshy (animação automática), plano Pro | A mesma do modelo | Confirmado (plano); termos: resumo |
| Esqueletos com dedos (101 ossos) de todos os personagens acima | `ArteFonte/<Personagem>/2_Rig` | **AccuRig** (Reallusion), gratuito | Resumo: o personagem rigado pode ser usado em projetos comerciais | Resumo |
| Ninsar: montagem, encaixe e scripts do Blender | `ArteFonte/Ninsar/3_Blender` | Feitos no projeto (Blender) | Próprios | Confirmado |
| Ninsar: texturas do manto (`Ninsar_Tecido_Manto.png`, `Ninsar_Faixa_Bordado.png`) | `ArteFonte/Ninsar/Texturas` | Enviadas pelo dono | — | **A confirmar** (origem) |

## Animações

A biblioteca bruta fica em `ArteFonte/Animacoes` (fora do Git); só os clipes usados são copiados para `Assets/_Nibiru/Art/Animacoes`. O mapa arquivo → clipe está em `Docs/CATALOGO_ANIMACOES.md` e em `Scripts/Editor/Animacoes/AnimationLibrary.cs`.

| Asset | Onde está | Origem | Licença | Situação |
| --- | --- | --- | --- | --- |
| Clipes do Mixamo usados: Idle, WalkForward, Fast Run, Sneak Walk, Stand To Roll, Pistol Aim, Reloading, Stabbing, Hit Reaction, Standing React Death Backward, Talking 1 a 4, Walking, rifle aiming idle, rifle run, firing rifle, walking to dying e o boneco `X Bot` | `Art/Animacoes/Mixamo` | **Mixamo** (Adobe), pastas `Avulsas` e pacotes do Mixamo | Resumo: uso livre de royalties em projetos pessoais e comerciais, inclusive jogos; não se pode redistribuir os arquivos de animação soltos (só dentro do jogo) | Resumo |
| Clipes da biblioteca (Lullu, Primeiro Servo, criaturas, Elena básica, pegar, abrir, agachada, sentada, dirigir) | `Art/Animacoes/Criatura`, `Elena`, `Caminhonete` | Mixamo: `Not So Scary Zombie Pack`, `Creature Pack`, `Female Basic Locomotion Pack` e `Avulsas` | A mesma do Mixamo | Resumo |
| Universal Animation Library 1 e 2 (Standard) | `ArteFonte/Animacoes/Universal Animation Library[Standard]` e `... 2[Standard]` | **Quaternius** (patreon.com/quaternius) | **CC0 1.0** (domínio público; atribuição não exigida, mas fica aqui por cortesia) | Confirmado (`License.txt`). Nenhum clipe em uso (o `Idle_FoldArms_Loop` foi testado e removido: atravessava o busto da Elena) |
| Pro Magic Pack, Magic Locomotion Pack, Scary Zombie Pack | `ArteFonte/Animacoes/Mixamo/` | Não confirmada | — | **Origem não confirmada: não usar.** Conferido em 26/out/2026: **nenhum uso** em `Assets` (nenhum arquivo deles foi copiado, comparando o conteúdo byte a byte; nenhum script, cena ou prefab os cita). Os dois nomes que coincidem (`X Bot` e `Standing React Death Backward`) vieram da pasta `Avulsas`, com conteúdo diferente do desses pacotes |

## Ambientes (área-vitrine da Aldeia, B1v Parte 3)

Texturas do **Poly Haven** (polyhaven.com): todo o acervo é **CC0** (domínio público; atribuição não exigida, fica aqui por cortesia). Cor e normal estão como baixadas (JPG); a máscara do URP (`*_mascara_1k.png`) é montada do `arm` de 1K, guardado em `ArteFonte/Ambientes/SitioDeUr/Aldeia/Texturas`, pelo menu **Nibiru > Arte > Vitrine da Aldeia > 1. Criar materiais** (`Scripts/Editor/Arte/VillageShowcaseMaterials.cs`).

| Asset | Onde está | Origem (autor) | Licença | Situação |
| --- | --- | --- | --- | --- |
| `patterned_clay_plaster` (reboco de barro, 2K) | `Art/Ambientes/SitioDeUr/Aldeia/Texturas/patterned_clay_plaster` | Poly Haven, polyhaven.com/a/patterned_clay_plaster (Amal Kumar) | CC0 | Confirmado |
| `dense_sand` (chão do pátio, 2K) | `.../Texturas/dense_sand` | Poly Haven, polyhaven.com/a/dense_sand (Dimitrios Savva) | CC0 | Confirmado |
| `slumped_mortar_brick` (tijolo de barro, 1K) | `.../Texturas/slumped_mortar_brick` | Poly Haven, polyhaven.com/a/slumped_mortar_brick (Dimitrios Savva) | CC0 | Confirmado |
| `clay_floor_001` (piso de barro, 1K) | `.../Texturas/clay_floor_001` | Poly Haven, polyhaven.com/a/clay_floor_001 (Dimitrios Savva, Rico Cilliers) | CC0 | Confirmado |
| `rough_wood` (madeira velha, 1K) | `.../Texturas/rough_wood` | Poly Haven, polyhaven.com/a/rough_wood (Rob Tuytel) | CC0 | Confirmado |
| `blue_metal_plate` (chapa de metal, 1K; base do metal turquesa) | `.../Texturas/blue_metal_plate` | Poly Haven, polyhaven.com/a/blue_metal_plate (Rob Tuytel) | CC0 | Confirmado |
| Tapete kilim, esteira de junco (cor e normal) | `.../Texturas/Proprias` | Gerados por script do projeto | Próprios | Confirmado |
| Metal pintado (cinza) e mancha de reboco descascado | `.../Texturas/Proprias` | Gerados por script a partir de `blue_metal_plate` e `slumped_mortar_brick` (CC0) | Próprios (derivados de CC0) | Confirmado |
| M1 (altar dos Guardiões), M9 (poço da Aldeia) e lamparina de barro: malha e textura de cor | Brutos em `ArteFonte/Ambientes/Altar/1_Meshy`, `ArteFonte/Ambientes/PoçoM9/1_Meshy` e `ArteFonte/Objetos/LamparinaDeBarro/1_Meshy` (fora do Git); cópias tratadas (buracos fechados, escala do jogo, cor em 2048 ou 1024 px) em `Art/Ambientes/SitioDeUr/Aldeia/Objetos`, pelo script `ArteFonte/Ambientes/SitioDeUr/Aldeia/Blender/objetos_vitrine.py` | Gerados no **Tripo** (tripo3d.ai, malha "Smart") pela conta do dono, em 3/nov/2026; o plano **Pro** foi o decidido em 1º/nov/2026 | Resumo dos termos do Tripo: nos planos pagos o modelo é privado e de uso comercial; no gratuito, público e sob CC BY 4.0 (pede atribuição). Conferir os termos atuais antes da página da Steam | A confirmar (plano usado nestes três) |
| Faixa de tijolo do poço (atrás do reboco descascado) e água escura do fundo | Peças `Poco_M9_Base` e `Poco_M9_Agua` dentro de `Objetos/Poco_M9.fbx` | Feitas pelo mesmo script; o tijolo usa `slumped_mortar_brick` (CC0) | Próprios | Confirmado |

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

## Declaração de uso de IA (para a página da Steam)

Conteúdo gerado por IA que entra no jogo: os **modelos 3D e texturas dos personagens feitos no Meshy** (lista em "Personagens"), incluindo a Elena nova, e os objetos do Meshy e do **Tripo** registrados neste arquivo (por enquanto o altar, o poço e a lamparina da Aldeia). A declaração da Steam precisa citar isso. O esqueleto vem do AccuRig (rig automático), e as animações, do Mixamo.

**Pendência antes da página "Em breve" da Steam:** avaliar a semelhança da Elena nova com a Lara Croft (rabo de cavalo, regata, coldre na coxa, pelo modelo e pelas referências) e, se for preciso, ajustar o visual. O dono decide.

## Ferramentas (não entram no jogo)

Blender (GPL; não se aplica aos arquivos criados com ele), AccuRig, Meshy, Tripo, ChatGPT (imagens de referência da Elena nova), Unity CLI e a ponte MCP.

## Como registrar um asset novo

Uma linha na tabela da categoria, com: o que é, onde está no projeto, de onde veio (site e autor, com link), a licença (CC0, CC BY, termos do serviço...), o que ela exige (atribuição, proibição de revenda do arquivo solto) e a situação. Texturas CC0 (ambientCG, Poly Haven) e objetos do Meshy entram assim, na conversa em que forem usados.
