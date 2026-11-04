# Plano B6: empunhadura das armas (IK e animações de espingarda)

Projeto em papel, 4/nov/2026. **Tarefa de sistema (animação e controles da Elena). Nada de código foi escrito; espera a aprovação do dono.**
É o último item para fechar o B6 (`Docs/Plano-B6-Espingarda.md`). Ferramenta de conferência: a câmera livre (**Nibiru > Testes > Câmera livre (conferência)**).

## Como está hoje (visto no código e no Play)

- A pose de mira da Elena é um recorte de **um quadro só** do clipe `Pistol Aim` (camada `Mira`, só tronco, braços, dedos e cabeça), e a recarga é o `Reloading` de pistola (camada `Recarregar`). Os pesos das camadas vêm do `PlayerAnimator`.
- Todas as armas usam a mesma pose. A arma é só um objeto colado na mão direita com um deslocamento fixo (`heldGunOffset`); **não há IK**. Na espingarda o cano atravessa o antebraço e a mão esquerda fica solta (visto no Play pelo dono).
- Chegaram em `ArteFonte/Animacoes/Mixamo` (fora do Git): **Rifle 8-Way Locomotion Pack** (49), **Basic Shooter Pack** (16) e **Slim Shooter Pack** (7), todos já com o X Bot.

## O que entra

1. **Estilo de empunhadura por arma.** `FirearmDefinition` ganha `holdStyle`: **UmaMao** (pistola, como hoje) e **DuasMaos** (espingarda). O `PlayerAnimator` escolhe a camada de mira e a de recarga pelo estilo da arma equipada.
2. **Clipes novos** (importados só os usados, Humanoid, avatar do X Bot, nomes `Elena_*`, na tabela `AnimationLibrary`):
   - `Elena_MiraLonga`: recorte da pose de `rifle aiming idle` (Basic Shooter Pack), de duas mãos.
   - `Elena_RecarregarLonga`: o `reloading` do Shooter Pack. O Mixamo não tem recarga cartucho a cartucho; a camada toca a abertura e depois **repete o trecho de colocar o cartucho** a cada cartucho que entra (cortado no código, sem clipe novo).
   - A troca de arma de 0,4 s continua sem clipe: a arma abaixa e sobe com a camada de mira. O `Drawing Gun` do Mixamo é de pistola e fica opcional para depois.
   - O andar e correr continuam os da camada de base (`Female Basic Locomotion`); o Rifle 8-Way fica de reserva para uma etapa futura de andar mirando nas 8 direções (fase D).
3. **IK das mãos** (`OnAnimatorIK`, com a camada de mira marcada com "IK Pass" e as máscaras `LeftHandIK`/`RightHandIK`, que a máscara de tronco já tem): peso do IK = peso da mira. A **mão esquerda** vai ao guarda-mão da espingarda; a **direita** ao cabo, e o IK só ajusta o que a pose já aproxima.
4. **Pontos de encaixe por arma.** Dentro de cada `Modelo_*` da mão da Elena entram dois objetos, `Grip_Direita` e `Grip_Esquerda` (posição e giro editáveis no Inspector, a olho com a câmera livre). `PlayerWeapons.WeaponModel` guarda as referências, e o `PlayerAnimator` passa a prender o **modelo** ao ponto de cabo da mão direita (em vez do deslocamento fixo `heldGunOffset`) e leva a mão esquerda ao `Grip_Esquerda`. Pistola: só `Grip_Direita` (como hoje); a mão esquerda segue a animação.
5. **Coice e recarga.** O coice do tiro (já procedural) continua; durante a recarga cartucho a cartucho a mão esquerda segue a animação (o IK da esquerda desliga na camada de recarga).
6. **Conferência.** Atalho **Nibiru > Testes > Armas (B6) > Conferir empunhadura**: põe a Elena mirando com a espingarda, o jogo parado e a câmera livre ligada com os controles dela ligados (o `KeepControls` que já existe na ferramenta), para ver e ajustar os pontos de encaixe.

## O que muda no save e nos dados

Nada no save. Dados: `holdStyle` na arma (as duas armas existentes ganham valor) e os pontos de encaixe nos prefabs (`Elena.prefab`, montados pelo mesmo menu **Povoar a espingarda (B6)**, que passa a criá-los).

## Sistemas compartilhados tocados e testes

Compartilhados: **prefab da Elena**, **tabela de animações** (`AnimationLibrary`) e o **controlador da Elena** (camadas novas).
- **Nível 1:** teste automático novo **Armas (B6) > Empunhadura**: para cada arma, mirando, a mão direita chega ao `Grip_Direita` e (espingarda) a esquerda ao `Grip_Esquerda` com folga de até 2 cm; sem mirar o IK tem peso 0 e a pose volta à normal; a troca 1/2 muda a camada e o modelo; a recarga usa a camada longa e a pistola a curta; a arma não atravessa o antebraço (distância mínima cano–antebraço); nada muda no checkpoint e no slot.
- **Nível 2:** **Animações > 4. Teste automático** (clipes e controladores da Elena), **Teste geral dos atalhos**, **Armas (B6) > 6. Teste automático** (a troca e a recarga continuam), **Poderes > Teste automático** e **Câmera > Zoom** (o prefab e a câmera de mira), um por vez, com os slots reais conferidos. Sem regressão completa.

## Riscos

- Os clipes do Mixamo foram feitos no X Bot e a Elena tem braços e proporções próprias (já houve problema de mão na coxa, resolvido no Avatar). Pode ser preciso ajustar o recorte do quadro da mira e o IK absorve o resto.
- O IK não conserta uma pose muito errada: por isso a pose de duas mãos vem do clipe.
- O ajuste fino dos pontos de encaixe é a olho; o dono confere pela câmera livre e eu corrijo os valores.

## Perguntas em aberto para o dono

1. **Pistola com as duas mãos?** Hoje só a direita tem arma. Recomendação: deixar a pistola como está (uma mão) nesta tarefa e decidir a pistola de duas mãos junto com o pacote Pistol da fase D.
2. **Recarga da espingarda:** repetir o trecho de colocar cartucho (recomendado) ou tocar o `reloading` inteiro a cada cartucho (mais fiel ao clipe, mas mais lento e estranho com 2 cartuchos)?
3. **Andar mirando:** deixar a locomoção de base como está (recomendado) ou já usar o Rifle 8-Way nas 8 direções? Recomendação: deixar para a fase D, depois do teste completo.
