# Nibiru — jogo de ação, aventura, terror e RPG

Jogo 3D indie para PC (Steam), sobre a conspiração dos Anunnaki. Uma arqueóloga, Elena Varga, descobre em 2003, no sul do Iraque, que os deuses Anunnaki estão adormecidos sob a terra e que ela é a chave para acordá-los.

O design completo (história, personagens, facções, regiões, sistemas, finais) está em `Docs/GDD.md`. Leia antes de criar qualquer sistema ou conteúdo. Se algo no código contradizer o GDD, pergunte antes de mudar.

**No começo de cada conversa, leia `Docs/ESTADO.md`**: ele diz o que já está pronto, onde fica cada coisa, o que está pendente e qual é o próximo passo. Ao terminar uma etapa grande, atualize esse arquivo.

## Decisões técnicas

- Engine: Unity 6 LTS, pipeline URP (não usar HDRP nem Built-in).
- Linguagem: C#.
- Câmera: terceira pessoa sobre o ombro. A primeira pessoa entra depois como opção, então câmera e controles devem ser desacoplados desde o início (uma interface de câmera com duas implementações).
- Input: novo Input System da Unity.
- Plataforma alvo: PC Windows, compatível com Steam Deck.
- Visual: realista e colorido. Superfície com poeira e ruína; mundo Anunnaki vivo, com lápis-lazúli, ouro e luz própria. O terror vem de clima, som e inimigos, não de deixar tudo escuro.

## Como trabalhar neste projeto

- Escreva sempre em português, inclusive as mensagens de progresso durante o trabalho.
- O dono do projeto não é programador profissional. Explique em português simples o que foi feito e o que ele precisa fazer no editor da Unity.
- Sistemas genéricos e orientados a dados: missões, diálogos, itens, facções e tabuletas são definidos em ScriptableObjects, para que adicionar conteúdo não exija mudar código.
- Um sistema por vez, testável sozinho, antes de passar para o próximo.
- Prefira scripts de editor e a ponte MCP para montar cenas e prefabs, em vez de pedir trabalho manual repetitivo.
- Não edite arquivos .unity, .prefab ou .meta à mão.
- Faça commits pequenos e frequentes, com mensagens em português.
- Arquivos pesados (modelos, imagens, áudio, vídeo) vão pelo Git LFS, já configurado em `.gitattributes`. Nenhum arquivo fora do LFS pode passar de 100 MB (limite do GitHub).
- Modelos 3D entram na Unity como FBX em `Assets/_Nibiru/Art/Personagens`; a arte-fonte de todos os personagens (.blend, rigs, referências) fica em `ArteFonte/<Personagem>/`, fora de `Assets`. Os arquivos brutos baixados do Meshy (`ArteFonte/*/1_Meshy`) ficam fora do Git, menos os que estão nas exceções do `.gitignore`.
- Teste cada sistema no Play pela ponte MCP antes de pedir o teste do dono (o jeito está em `Docs/ESTADO.md`). Nunca recompile scripts com o Play ligado.
- Um passo por vez: ao terminar um sistema, mostre o resultado e espere o dono testar no Play antes de seguir.

## Estrutura de pastas

```
Assets/
  _Nibiru/
    Scripts/        (por sistema: Player, Camera, Combat, AI, Stealth, Interaction, Quests, Dialogue,
                     Factions, Inventory, Tablets, Powers, Save, UI; Editor/ para ferramentas do editor)
    Data/           (ScriptableObjects: missões, eventos, itens, diálogos, tabuletas, poderes, facções, IA)
    Prefabs/        (Player/Elena é a Elena com câmera e HUD; use em toda cena)
    Scenes/         (Teste_Personagem = laboratório; Regiao1_SitioDeUr = região 1)
    Art/            (assets próprios; Personagens/ com os modelos 3D em FBX)
  ThirdParty/       (assets comprados ou baixados, sem modificar)
ArteFonte/          (arte-fonte por personagem, fora da Unity: Elena/, Samir/, Ninsar/
                     com 1_Meshy, 2_Rig, 3_Blender e Imagens De Referencia)
Docs/
  GDD.md, Biblia.md, Regiao-1-Sitio-de-Ur.md, ESTADO.md
```

## Fase atual

Fase 3 do plano: construir a base, agora na fatia vertical da região 1, o Sítio de Ur. O protótipo do personagem, os 14 sistemas essenciais, o blockout do mapa, o Acampamento (área 1), as Trincheiras (área 2) e o encontro com o Samir na Aldeia (área 3, passo 6) estão prontos. O próximo passo é continuar a Aldeia (passos 7 a 9) e seguir área por área. Detalhes em `Docs/ESTADO.md`.
