# Plano: câmera livre de conferência (só para teste)

Decisão do dono (4/nov/2026): **não** entra câmera de inspeção, look-ahead nem troca de ombro no jogo (nenhuma referência orbita a personagem durante o jogo; isso é modo foto, fase D). Entra só uma **ferramenta de conferência** para olhar a Elena, as armas e o cenário de qualquer ângulo.

**O que é.** Menu **Nibiru > Testes > Câmera livre (conferência)** (liga e desliga, com marca de ligado). Só funciona no Play. Ligada, o jogo fica pausado (`timeScale` 0) e uma câmera própria, que nasce na pose da câmera do jogo, voa com **WASD** (frente/trás/lados), **E** sobe, **Q** desce, **Shift** acelera (4×) e o **mouse** gira. O cursor fica preso enquanto ela está ligada.

**Isolamento.** Não mexe no CameraRig, no PlayerMotor, nas preferências nem em nenhum arquivo. Para a Elena e o CameraRig não reagirem ao mouse e às teclas, só desliga temporariamente o mapa de controles do jogador (`SetGameplayEnabled(false)`), e guarda e devolve: `timeScale`, estado do cursor e o estado anterior dos controles. Ao desligar (ou ao sair do Play), a câmera livre é destruída e tudo volta como estava.

**Fora da build.** Mora em `Scripts/Editor/Camera/FreeCameraCheck.cs`, na montagem do editor: não entra em nenhuma build.

**Testes.** Nível 1: **Nibiru > Testes > Câmera livre (teste automático)** (liga e desliga e confere que tudo volta igual; voo em todas as direções, Shift mais rápido, giro e limite de inclinação; jogo parado e a Elena e a câmera do jogo intocadas; ligar duas vezes não duplica; a ferramenta está na montagem do editor). Nível 2: **Zoom** e **Poderes** (controles e câmera), um por vez, com os slots reais conferidos.
