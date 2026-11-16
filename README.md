# RifleONeilFPS (Unity 6 URP)

Jogo de tiro em primeira pessoa (FPS) no **Unity 6 (6000.6.8f1)** com **URP**, usando o rifle futurista `O'NEIL` modelado no Blender (`#1B365D`, `#005A9C`, `#60C1E5`, `#A0E0EF`) para atirar e destruir ondas de cubos-alvo.

## Recursos Implementados
- **Modo Duplo de Disparo (`RifleShooter.cs`)**:
  - **Cliques rápidos (Botão Esquerdo)**: tiros de pulso energéticos individuais com recuo.
  - **Segurar Botão Esquerdo**: **Raio Feixe contínuo** (beam pulsante com dano contínuo).
- **Mira ADS (`Botão Direito`)**: alinha a mira holográfica, aproxima o FOV (`62°` → `42°`) e reduz a sensibilidade.
- **Animação de Recarga com Desmontagem e Laser (`RifleReloadAnimator.cs`)**: ao pressionar `R` (ou zerar o carregador), a parte frontal do rifle se desmonta no ar, acende um núcleo laser ciano que ilumina as peças abertas e depois remonta automaticamente.
- **Cubos Desafiadores (`TargetCube.cs` + `FPSGameManager.cs`)**: múltiplos pontos de vida, padrões de movimento (`Strafe`, `Orbit`, `ZigZag`), estilhaços com física (`Rigidbody`) e ondas progressivas.
- **Áudio Sci-Fi**: efeitos sonoros do pacote Kenney Sci-Fi Sounds em `Assets/Audio/KenneySciFi/`.

## Controles
- `WASD`: Mover · `Shift`: Correr · `Espaço`: Pular
- `Clique Esquerdo`: Tiro de pulso · `Segurar Esquerdo`: Raio Feixe contínuo
- `Botão Direito`: Mira ADS
- `R`: Recarregar · `T`: Reiniciar Onda 1 · `ESC`: Liberar cursor
