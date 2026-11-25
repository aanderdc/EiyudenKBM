# Checklist de Pruebas Manuales — EiyudenKBM

- [ ] **Slice 1 — Arranque y BepInEx**: El juego arranca hasta la pantalla de título, genera `BepInEx\LogOutput.log`, carga `EiyudenKBM`, crea `BepInEx\config\EiyudenKBM.cfg` y limpia el lock de Modo Seguro.
- [ ] **Slice 2 — Menú principal, Título, Pausa y Ajustes de Rebind**: Navegación con ratón (hover + clic izq. + clic der. + rueda) y teclado en Título, Pausa y Opciones; probar el nuevo apartado **Ajustes → Controles (Teclado y Ratón)** (incluyendo `Mouse4`/`Mouse5`) y verificar recarga en caliente.
- [ ] **Slice 3 — Exploración y Cámara**: Movimiento `WASD` + Flechas, `Shift` caminar/correr, `Espacio`/`F`/`Clic Izq.` interactuar, `Tab`/`Clic Central` menú, `M` mapa, `Escape` pausa, arrastrar con `Clic Der.` y `Q`/`E` para rotar cámara, y auto-ocultado del cursor en campo.
- [ ] **Slice 4 — Diálogos y Cinemáticas**: Clic o `Espacio` para avanzar texto, hover + clic o `1`–`4` para opciones de diálogo, mantener `Escape`/`Clic Der.` para saltar escena.
- [ ] **Slice 5 — Combate por turnos**: Navegación con ratón y atajos `1`–`6` en comandos principales (Luchar/Auto/Huir/Apoyo), comandos de personaje (Atacar/Defender/Runa/Objeto/Combo), listas de habilidades/objetos (con rueda) y selección de objetivos/enemigos.
- [ ] **Slice 6 — Inventario, Equipo, Tienda, Castillo/Campamento y Mapa**: Hover + clic + rueda + cambio de pestañas (`Q`/`E`, `Shift+Rueda`, `Z`/`C`) en inventario, equipo, runas, tienda, formación, castillo y mapa (arrastrar + zoom).
- [ ] **Slice 7 — Cambio Teclado/Ratón ↔ Mando e Iconos en pantalla**: Con el mando conectado, tocar teclado/ratón actualiza los iconos a las teclas/botones configurados; tocar el mando devuelve instantáneamente los iconos de Xbox/PS sin romper la interfaz.
- [ ] **Slice 8 — Guardar, Cargar y Desinstalación limpia**: Guardar y cargar partida con ratón/teclado sin errores; comprobar que `desinstalar_mod.bat` deja el juego en su estado original sin tocar las partidas.
