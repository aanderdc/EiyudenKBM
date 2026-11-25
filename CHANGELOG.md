# Changelog — EiyudenKBM

## [1.0.0 — Completo: Cámara, Diálogos, Combate por Turnos, Mapa e Iconos] — 2026-11-25
- **Exploración y Cámara (`CameraAndMapPatches`)**: rotación suave de cámara arrastrando con el botón de ratón configurado (por defecto `Clic Derecho`, admite `MouseMiddle`, `Mouse4`, `Mouse5`) con sensibilidad e inversión X/Y ajustables, más giro rápido con `Q`/`E` en campo.
- **Mapa (`CameraAndMapPatches`)**: arrastrar el mapa con `Clic Izquierdo`, desplazamiento con `WASD` + Flechas y zoom con rueda del ratón o `+`/`-`.
- **Diálogos y Escenas (`DialogPatches`)**: `Clic Izquierdo` o `Espacio` para avanzar/completar texto, hover + clic o teclas `1`–`4` para elecciones de diálogo, mantener `Escape`/`Clic Derecho` para saltar cinemáticas, y clic para cerrar tutoriales, alertas y resultados de combate.
- **Combate por Turnos (`BattlePatches`)**: navegación completa con ratón (hover + clic izq. + clic der. + rueda) y atajos numéricos `1`–`6` en comandos principales (Luchar/Auto/Huir/Apoyo), comandos de personaje (Atacar/Defender/Runa/Objeto/Combo), habilidades, objetos, confirmaciones y selección de objetivos.
- **Iconos Dinámicos en Pantalla (`KeyIconPatches`)**: genera en tiempo real sprites nítidos de teclas y botones de ratón (`LMB`, `RMB`, `MMB`, `M4`, `M5`, `SPC`, `ESC`, `TAB`, etc.) en `UIKeyIcon` y `KeyHelpUnit` cuando juegas con teclado/ratón, actualizándose al instante al reasignar controles y devolviendo los iconos de Xbox/PS al tocar un mando.

## [0.2.0 — Slice 2: Menús, Pausa y Ajustes de Reasignación] — 2026-11-25
- Capa de entrada unificada (`KeyBindingSystem`) con soporte completo para teclado y 5 botones de ratón (`MouseLeft`, `MouseRight`, `MouseMiddle`, `Mouse4`, `Mouse5`), sin simular pulsaciones del SO.
- Parches Harmony aislados en `InputManager.CheckAction`, `CheckLeftMoveAction`, `CheckSystemUILeftMove` y `CheckDevice` (cambio dinámico Teclado/Ratón ↔ Mando incluso con mando conectado).
- Soporte de ratón (hover + clic izq. + clic der. + rueda) en pantalla de Título (`UI.Title.SelectState`), menús `DialogSelector` (Pausa, Opciones, confirmaciones), `OptionSecondaryMenu` (toggles/selectores/sliders), `ScrollList` (Guardar/Cargar, listas) y carrusel `MainMenu`.
- Nuevo apartado **"Controles (Teclado y Ratón)"** integrado directamente dentro de **Opciones / Ajustes** del juego (además de tecla rápida `F8`), con captura interactiva de teclado y ratón (incl. `Mouse4` y `Mouse5`), selector de modo de cursor, sensibilidad de cámara y guardado/recarga en caliente en `BepInEx\config\EiyudenKBM.cfg`.

## [0.1.0 — Slice 1: Base, Estabilidad y Diagnóstico] — 2026-11-25
- Backup completo del juego y de las partidas guardadas antes de instalar BepInEx.
- Instalación de BepInEx 6.0.0-be.793 (`Unity.IL2CPP-win-x64`).
- Proyecto compilable C# (`net6.0`) con `build.bat`, `instalar_mod.bat` y `desinstalar_mod.bat`.
- Modo Seguro automático (`SafeModeGuard`): si un arranque no llega a completarse limpiamente, el mod se desactiva automáticamente en el siguiente inicio y deja constancia en el log.
- Archivo de configuración único `BepInEx\config\EiyudenKBM.cfg` con interruptor general `Enabled`, modo de cursor y todos los bindings de teclado y ratón (incluyendo `MouseLeft`, `MouseRight`, `MouseMiddle`, `Mouse4`, `Mouse5`).
- Registro detallado de diagnóstico en arranque (`EiyudenKBM.log` + `BepInEx\LogOutput.log`).
