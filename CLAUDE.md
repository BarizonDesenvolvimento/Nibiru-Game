# Nibiru — jogo de ação, aventura, terror e RPG

Jogo 3D indie para PC (Steam), sobre a conspiração dos Anunnaki. Uma arqueóloga, Elena Varga, descobre em 2003, no sul do Iraque, que os deuses Anunnaki estão adormecidos sob a terra e que ela é a chave para acordá-los.

O design completo (história, personagens, facções, regiões, sistemas, finais) está em `Docs/GDD.md`. Leia antes de criar qualquer sistema ou conteúdo. Se algo no código contradizer o GDD, pergunte antes de mudar.

## Decisões técnicas

- Engine: Unity 6 LTS, pipeline URP (não usar HDRP nem Built-in).
- Linguagem: C#.
- Câmera: terceira pessoa sobre o ombro. A primeira pessoa entra depois como opção, então câmera e controles devem ser desacoplados desde o início (uma interface de câmera com duas implementações).
- Input: novo Input System da Unity.
- Plataforma alvo: PC Windows, compatível com Steam Deck.
- Visual: realista e colorido. Superfície com poeira e ruína; mundo Anunnaki vivo, com lápis-lazúli, ouro e luz própria. O terror vem de clima, som e inimigos, não de deixar tudo escuro.

## Como trabalhar neste projeto

- O dono do projeto não é programador profissional. Explique em português simples o que foi feito e o que ele precisa fazer no editor da Unity.
- Sistemas genéricos e orientados a dados: missões, diálogos, itens, facções e tabuletas são definidos em ScriptableObjects, para que adicionar conteúdo não exija mudar código.
- Um sistema por vez, testável sozinho, antes de passar para o próximo.
- Prefira scripts de editor e a ponte MCP para montar cenas e prefabs, em vez de pedir trabalho manual repetitivo.
- Não edite arquivos .unity, .prefab ou .meta à mão.
- Faça commits pequenos e frequentes, com mensagens em português.

## Estrutura de pastas

```
Assets/
  _Nibiru/
    Scripts/        (por sistema: Player, Camera, Combat, AI, Quests, Dialogue, Factions, Inventory, Save, UI)
    Data/           (ScriptableObjects: missões, itens, diálogos, tabuletas)
    Prefabs/
    Scenes/
    Art/            (assets próprios)
  ThirdParty/       (assets comprados ou baixados, sem modificar)
Docs/
  GDD.md
```

## Fase atual

Fase 3 do plano: construir a base. Primeira meta é o protótipo do personagem (andar, correr, esquivar, câmera em terceira pessoa) numa cena de teste. Depois disso, a fatia vertical: a região 1, Sítio de Ur.
