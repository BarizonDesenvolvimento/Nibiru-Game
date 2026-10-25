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
- Todo passo novo da missão ganha um menu de atalhos de teste em **Nibiru > Testes > Passo N**, como o do passo 8 (`Scripts/Editor/Step8Shortcuts.cs`): cada atalho liga o Play e põe a Elena num momento do passo, com o mundo como se ela tivesse jogado até ali. Teste os atalhos pela ponte e liste-os no `Docs/ESTADO.md` ao pedir o teste do dono.

## Estrutura de pastas

```
Assets/
  _Nibiru/
    Scripts/        (por sistema: Player, Camera, Combat, AI, Stealth, Interaction, Quests, Dialogue,
                     Factions, Inventory, Tablets, Powers, Save, UI; Editor/ para ferramentas do editor)
    Data/           (ScriptableObjects: missões, eventos, itens, diálogos, tabuletas, poderes, facções, IA)
    Prefabs/        (Player/Elena é a Elena com câmera e HUD; use em toda cena)
    Scenes/         (Teste_Personagem = laboratório; Regiao1_SitioDeUr = região 1)
    Art/            (assets próprios; Personagens/ com os modelos 3D em FBX, Animacoes/Mixamo/ com
                     as animações usadas e o X Bot de referência)
  ThirdParty/       (assets comprados ou baixados, sem modificar)
ArteFonte/          (arte-fonte fora da Unity: uma pasta por personagem com Referencias, 1_Meshy e 2_Rig
                     (rig do AccuRig com dedos); a Ninsar também tem 3_Blender; Animacoes/Mixamo/)
Docs/
  GDD.md, Biblia.md, Regiao-1-Sitio-de-Ur.md, ESTADO.md
```

## Fase atual

Fase 3 do plano: construir a base, agora na fatia vertical da região 1, o Sítio de Ur. O protótipo do personagem, os 14 sistemas essenciais, o blockout do mapa, o Acampamento (área 1), as Trincheiras (área 2), a Aldeia até o passo 7 (área 3: encontro com o Samir, sobreviventes e o nicho dos Guardiões como altar) e a Elena definitiva (AccuRig com dedos, animações do Mixamo) no lugar da cápsula estão prontos. O Samir definitivo, os 4 soldados da Fundação (Soldado, Líder de esquadra, Atirador de elite e Piloto), o Lullu e o Primeiro Servo também já estão no jogo. Os outros personagens estão prontos na ArteFonte e entram na Unity um por conversa. Os passos 8 (a primeira tabuleta, a memória de Ziusudra e o Eco do Abzu) e 9 (o plano de fuga) estão prontos e testados pelo dono. O passo 10 (atravessar a base do zigurate, com o atirador de elite, o golpe de faca pelas costas, os documentos do Kast e o portão para a Aldeia), o passo 11 (descer à Boca do Poço, passar pelo Lullu e o flash diante do Selo de Kur) e o passo 12 (a luta com o Primeiro Servo na antecâmara, em três fases, com os holofotes que caem, já com o modelo definitivo dele) estão prontos e testados pela ponte, esperando o teste do dono (o 12 já foi aprovado no Play, com o equilíbrio da luta pendente). O passo 13 (a passagem dos Guardiões atrás da parede do barco: inscrição, parede que afunda, lamparinas, a fita de Irena e a saída para o wadi) também está pronto e testado pela ponte. O passo 14 (o wadi: tempestade de areia provisória, um líder de esquadra, o rádio do Kast que espera a fita de Irena terminar e a primeira escolha de facção, com três opções) também está pronto, testado pela ponte e aprovado pelo dono no Play. Depois, o passo 15 (a fuga na caminhonete, que precisa considerar as três escolhas) e seguir área por área. A Ninsar tem uma prévia animada na Unity, e o manto dela está pausado. Detalhes em `Docs/ESTADO.md`.

## Eficiência (vale para toda tarefa)
- Investigue só o que a tarefa toca; não audite o projeto. Não releia arquivos nem reconsulte o editor se nada mudou.
- Unity MCP só quando o trabalho exigir o editor. Prefira filtros, consultas pontuais e logs só com erros. Para checagens repetidas, crie um item de menu que imprima um resumo curto.
- Antes de uma tentativa, tenha uma hipótese. Depois de 2 tentativas sem resolver o mesmo problema, pare e me resuma o que descobriu.
- Teste na medida do risco: primeiro a verificação mais específica, depois a suíte ampla se a mudança tocar vários sistemas.
- Não refatore nem melhore o que não foi pedido. Ao terminar, atualize o ESTADO.md, faça os commits e encerre.
- Relatório final curto: o que mudou, como foi testado, o que ficou pendente.
- Se a tarefa estiver crescendo além do combinado, proponha dividir em outra conversa em vez de continuar.
