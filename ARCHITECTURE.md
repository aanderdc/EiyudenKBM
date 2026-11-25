# Eiyuden Chronicle: Hundred Heroes — Análisis de Arquitectura (Paso 1)

> Todos los datos, clases, métodos, campos y bindings de este documento han sido verificados directamente contra los binarios instalados en este PC (`GameAssembly.dll`, `global-metadata.dat`, `ScriptingAssemblies.json`, `Player.log` y el volcado de `Il2CppDumper`).

---

## 1. Motor, Variante, Versión y Compatibilidad con BepInEx

| Propiedad | Valor verificado |
|---|---|
| **Ruta del juego (Steam)** | `C:\Program Files (x86)\Steam\steamapps\common\Eiyuden Chronicle` |
| **Steam AppID / BuildID** | `1658280` / BuildID `14818073` (Manifest `931861435176872698`) |
| **Ejecutable** | `EiyudenChronicle.exe` (64 bits, **x64 / AMD64**) |
| **Versión de Unity** | **2021.3.32f1** (`3b9dae9532f5`) |
| **Backend de scripting** | **IL2CPP** (`GameAssembly.dll` de 87 MB + `EiyudenChronicle_Data\il2cpp_data\Metadata\global-metadata.dat`, Metadata v29; no existe carpeta `Managed/`) |
| **Render Pipeline** | Universal Render Pipeline (URP) + Direct3D 11 |
| **Ruta de partidas guardadas** | `%USERPROFILE%\AppData\LocalLow\505 Games S_p_A\EiyudenChronicle\<SteamID>\SaveData\` (`SystemData.dat`, `UserData0.dat`, `UserDataInfo.dat`, `steam_autocloud.vdf`) |
| **Log de Unity** | `%USERPROFILE%\AppData\LocalLow\505 Games S_p_A\EiyudenChronicle\Player.log` |
| **BepInEx compatible** | **BepInEx 6.0.0 Bleeding Edge (BE) — `Unity.IL2CPP-win-x64`** (e.g. `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.793+dbc82f0.zip` desde `builds.bepinex.dev/projects/bepinex_be`), con `Il2CppInterop` y `HarmonyX` incluidos. |
| **Instalación en Steam (Windows)** | Extraer `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` y `BepInEx/` en la raíz del juego junto a `EiyudenChronicle.exe`. **En Windows NO hace falta opción de lanzamiento en Steam** (`WINEDLLOVERRIDES` solo aplica en Linux/Steam Deck). El primer arranque tarda 1–3 minutos mientras `Il2CppInterop` genera las librerías proxy en `BepInEx/interop/`. |
| **Target .NET del plugin** | `net6.0` (C#, ensamblado para `BepInEx.Unity.IL2CPP`). |

---

## 2. Sistema de Entrada (Input)

### Librería y arquitectura
El juego utiliza exclusivamente **Unity Input System (nuevo)** (`Unity.InputSystem.dll` y `Unity.InputSystem.ForUI.dll`), confirmado en `Player.log` (`New input system (experimental) initialized`, `Using XInput`) y en `Assembly-CSharp.dll`.

La arquitectura de entrada se compone de tres capas limpias:

1. **Capa de dispositivo / bindings (`MyInput` / `InputActionAsset`)**
   - Clase generada `MyInput : IInputActionCollection2` (en `Assembly-CSharp.dll`, TypeDefIndex 118) y su `InputActionAsset` serializado dentro de `InputRuleTable._actionAsset`.
   - Contiene 4 `InputActionMap`:
     - `Generic`: botones estilo mando (`A`, `B`, `X`, `Y`, `LB`, `LT`, `LS`, `RB`, `RT`, `RS`, `Menu`, `Guide`, `Ignore`, `AnyButton`) y ejes (`LeftMove`, `LeftMoveDigitalOnly`, `LeftMoveAnalogOnly`, `RightMove`, `RightMoveDigitalOnly`, `RightMoveAnalogOnly`).
     - `SystemUI`: `A`, `B`, `LeftMove`, `LeftMoveDigitalOnly`, `LeftMoveAnalogOnly`.
     - `DebugUI`: `A`, `B`, `LeftMove`, `VisibleChange`.
     - `MapUI`: `Scroll`, `Swiching` (sic), `Zoom`.
   - Esquemas de control declarados: `Keyboard` (`<Keyboard>`), `PS4 Controller` (`<DualShockGamepad>`), `Xbox Controller` (`<XInputController>`). **No incluye `<Mouse>` en ningún esquema de control.**
   - **Ningún script del juego llama a `UnityEngine.InputSystem.Mouse.current`** (0 referencias en `GameAssembly.dll`). El único rastro de ratón en todo el juego es `<Mouse>/scroll` en `MapUI.Zoom`.

2. **Capa de mapeo acción → botón (`InputRuleTable` y `InputRule`)**
   - `InputRuleTable : ScriptableObject` (TypeDefIndex 230) contiene:
     - `_actionAsset` (`InputActionAsset`)
     - `_rules` (`InputRule[]`), donde cada `InputRule` asocia un `InputActionMode` (enum de intención de juego: `PlayerCheck=0`, `PlayerWalk=1`, `MenuOpen=2`, `MapOpen=3`, `UIDecide=1000`, `UICancel=1001`, `UIAutoEquip=1002`, `UIStatusChange=1003`, `UISelectL=1004`, `UISelectR=1005`, `UISelectL2=1006`, `UISelectR2=1007`, `UISelectL3=1008`, `UISelectR3=1009`, `UILeftStick=1010`, `UIRightStick=1011`, `UIOption=1012`, `UIMapSwitching=1013`, `UIAnyButton=1014`, `War*`, `SystemUIDecide=8000`, `SystemUICancel=8001`, etc.) con un `InputActionReference`, un `InputButtonType` (`A..Guide`, `Sys_A`, `Sys_B`) y un nombre de efecto de sonido (`_seName`).
     - `_overrideButtonMap` (`Dictionary<InputActionMode, InputButtonType>`): usado por `GameManager.RemapDecideAndCancelButton()` (RVA `0x603EA0`) para intercambiar A/B según región (remapea `0↔1`, `1000↔1001`, `2000↔2001`, `8000↔8001`).
   - En el menú de Opciones (`OptionSecondaryMenu.SecondaryMenuType`), **no existe reasignación de teclas** en la versión actual: `OperationManual` (valor 2) es solo una pantalla informativa de solo lectura (`OptionSecondaryMenuItem.SetupOperation`). `GameData.GamePlay` tampoco guarda rebinds (solo idioma, auto-texto, vibración, invertir cámara H/V, velocidad de combate y auto-combate).
   - **Integración de ajustes de reasignación (`OptionMenu`)**: `FieldStage.UI.OptionMenu` (TypeDefIndex 9949) construye la lista principal de categorías en un `DialogSelector` (`_rootSelector` / `RootSelector`) y despacha el índice seleccionado en `UpdateRoot()` (RVA `0xEB25D0`). Esto permite añadir una entrada propia **"Controles (Teclado y Ratón)"** directamente en el menú de Ajustes del juego (tanto desde la pantalla de título como desde el menú de pausa), además del atajo `F8`, con captura interactiva de cualquier tecla del teclado y botones del ratón (`MouseLeft`, `MouseRight`, `MouseMiddle`, `Mouse4`, `Mouse5`).

3. **Capa central de consulta (`InputManager : SingletonBehaviour<InputManager>`, TypeDefIndex 226)**
   - En `Awake()` (RVA `0x749220`) obtiene los mapas `"Generic"`, `"SystemUI"` y `"MapUI"` desde `_ruleTable.ActionAsset` y crea objetos `InputCrossStatus` (con repetición de tecla `InputKeyReppet`) para `LeftMove*`, `RightMove*`, `SystemUILeftMove*` y `MapUI*`.
   - En `Update()` (RVA `0x74C400`) actualiza todos los `InputCrossStatus` y llama a `CheckDevice()`.
   - Todo el juego consulta la entrada a través de sus métodos estáticos:
     - `InputManager.CheckAction(InputActionMode mode, InputCheckMode checkMode, ...)` (RVA `0x7498F0`) → consulta `InputRuleTable.TryGetAction` y verifica `action.triggered` (`Trigger=0`), `action.IsPressed()` (`Hold=1`) o `action.WasReleasedThisFrame()` (`Release=2`).
     - Atajos: `CheckUIDecide()`, `CheckUICancel()`, `CheckSystemUIDecide()`, `CheckSystemUICancel()`, `CheckLeftMove(...)`, `CheckRightMove(...)`, `CheckLeftAxis(out Vector3)`, `CheckMapZoom`, `CheckMapScroll`, `CheckMapSwiching`, `LeftMoveValue`, `RightMoveValue`.
   - **Capa óptima para enganchar nuestro mod**:
     - Añadir bindings de teclado y ratón directamente al `InputActionAsset` vivo (`InputManager.ActionAsset`) y/o interceptar `InputManager.CheckAction`, `CheckLeftMove`, `CheckRightMove`, `RightMoveValue` y `InputCrossStatus` con Harmony. Así los bindings originales se preservan íntegros, el mando sigue intacto y no hace falta simular pulsaciones del SO.

### Detección de dispositivo e iconos en pantalla
- `InputManager.DeviceType` (`InputManager.InputDeviceType`: `Keyboard=0`, `DualShock=1`, `DualSense=2`, `XInput=3`, `GamePad=4`) es leído en **31 sitios** (`KeyHelpUnit`, `UIKeyIcon`, `TextTagDefineTable.InputIconTag`, `OptionSecondaryMenuItem`, minijuegos, batallas de guerra).
- **Hallazgo crítico en `InputManager.CheckDevice()` (RVA `0x749B80`)**:
  - Solo inspecciona `Gamepad.all` (y si alguno está actuado vía `InputControlExtensions.IsActuated`).
  - **Nunca comprueba `Keyboard.current` ni `Mouse.current` cuando hay un mando conectado.** Si tienes un mando enchufado al PC, `DeviceType` se queda fijado al mando y jamás vuelve a `Keyboard` aunque uses teclado o ratón.
  - Al parchear `CheckDevice()`, podemos detectar actividad real de teclado/ratón (cambiando a `Keyboard`) y de mando (volviendo a `DualShock`/`DualSense`/`XInput`/`GamePad` al tocar cualquier botón o stick del mando).
- **Renderizado de iconos de ayuda**:
  - `UIKeyIcon.UpdateKeyIcon()` (RVA `0x75BD50`) y `FieldStage.UI.KeyHelpUnit.UpdateIconImage()` (RVA `0xE6E770`) llaman a `KeyIconExtensions.GetKeyIconSpriteName(IKeyIcon, InputDeviceType)` (RVA `0x677A20`) → `AtlasManager.GetCommonSprite(spriteName)` → `Image.sprite`.
  - `KeyIcon` (MasterData) guarda 4 nombres de sprite fijos (`ImageNamePs4`, `ImageNamePs5`, `ImageNameXbox`, `ImageNameKeyBoard`).
  - Para mostrar en pantalla las **teclas reales del nuevo esquema (incluyendo iconos de ratón como LMB/RMB/MMB/Rueda)** cuando `DeviceType == Keyboard`, podemos interceptar `UIKeyIcon.UpdateKeyIcon`, `KeyHelpUnit.UpdateIconImage` y `TextTagDefineTable.InputIconTag.GetReplaceText` generando sprites nítidos en caché según el binding activo en el archivo de configuración, sin tocar los sprites del mando cuando `DeviceType != Keyboard`.

---

## 3. Arquitectura de Interfaz (UI)

### Tecnología base
- **uGUI (`UnityEngine.UI`, `Canvas`, `RectTransform`, `ScrollRect`, `Image`, `TextMeshProUGUI` / `KaeruText`)**. No usa UI Toolkit para el juego (UI Toolkit solo está presente en el módulo del engine y debugger de URP).
- `GameManager.InitializeEventSystem` (RVA `0x613680`) instancia un prefab `"DebugUI/EventSystem"`, pero **`EventSystem.SetSelectedGameObject` no es llamado por ninguna clase del juego** (0 llamadas desde `Assembly-CSharp`), y `EventSystem.current` solo aparece una vez (`FieldMenu.Update`).

### Navegación real en todas las pantallas (hallazgo clave)
Casi toda la UI de Eiyuden Chronicle **NO usa `UnityEngine.UI.Selectable` ni `EventSystem` para el foco**. En su lugar, cada panel mantiene su propio índice entero y sondea `InputManager` en `Update()`/`LateUpdate()`:

| Pantalla / Sistema | Clases principales (verificadas) | Cómo navega / recibe entrada |
|---|---|---|
| **Listas con scroll (Inventario, Tienda, Almacén, Guardar/Cargar, Mapa del mundo, Estrategia, Magia, Gremio, Ayuda)** | `FieldStage.UI.ScrollList` (`ScrollListBase`), `ListItemBase`, `CursorBase` | Mantiene `_selectIndex` + `List<ListItemBase> _itemList` + `_scrollRect` + `_cursor`. `ScrollList.OnUpdate()` (RVA `0xF98F30`) consulta `CheckUIDecide`, `CheckUICancel`, `CheckLeftMove`, `CheckRightMove` y llama a `SelectItem(int)` / `OnDecide()` / `OnCancel()`. |
| **Diálogos de confirmación, Opciones y Menú de sistema (Pausa)** | `FieldStage.UI.DialogSelector`, `SelectorButton`, `SystemMenu`, `OptionMenu`, `OptionSecondaryMenu` | `DialogSelector` mantiene `_cursor` y `List<SelectorButton> _buttonList`. `DialogSelector.Update()` (RVA `0xF8FD60`) consulta `CheckUIDecide`/`CheckSystemUIDecide`, `CheckUICancel`/`CheckSystemUICancel`, `CheckLeftMove` y llama a `SetCursor(int)` / `SelectorButton.Select()` / `Submit()`. |
| **Menú principal de campo (Carrusel de pestañas)** | `FieldStage.UI.MainMenu`, `HorizontalCarousel`, `MenuWindowBase` (`InventoryItemWindow`, `EquipmentWindow`, `CharaStatusWindow`, `PartyFormationWindow`, `UseMagic`, `MenuAutoAi`, `MenuOption`, `MenuHelp`, `UIMap`) | `MainMenu.Update()` (RVA `0xE9A4D0`) cambia de sección principal con `UISelectL2`/`UISelectR2` (LT/RT), y cada sub-ventana cambia pestañas/personajes con `UISelectL`/`UISelectR` (LB/RB). |
| **Pantalla de título** | `UI.Title.TitleCanvas`, `UI.Title.SelectState`, `Title.TitleMenuWindow`, `Title.TitleContentSelectList` | `SelectState.PhaseUpdate` espera `UIAnyButton`, luego `TitleContentSelectList` mantiene `_index` + `List<ContentInfo>` (`SelectorButton`) y consulta `CheckLeftMove`/`CheckUIDecide`/`CheckUICancel`. |
| **Combate por turnos** | `Battle.UI.UICommandSelect` (`SelectableHandlerBase<ICommandSelectOperation>`), `Battle.UI.InputUtility`, `UIMainCommandSelect` (Luchar/Auto/Huir/Apoyo), `UIUnitCommandSelect` (Atacar/Defender/Runa/Objeto/Combo), `UISkillSelectPanel`, `UILinkedSelectPanel`, `UIItemSelectPanel`, `UITargetList`, `UIUnitList`, `UIConfirmWindow`, `SelectableItem` | Pila de handlers (`_selectableContext`). `PollInput()` llama a `Battle.UI.InputUtility.Poll()` (RVA `0xD72100`), que sondea `CheckUIDecide`, `CheckUICancel`, `CheckLeftMove` y delega `OnMove`/`OnSubmit`/`OnCancel` al handler superior. Cada panel contiene `SelectableItem[]` o `IconSelectableItemPoolList` con `RectTransform` e índices vía `IndexSequencer`. |
| **Diálogos y elecciones de historia** | `Scenario.UI.UIScenerioCanvas`, `UIConversationGroup`, `UIConversation`, `UIScenarioChoices`, `UIScenarioChoiceOption`, `UIScenarioSkip` | `UIScenerioCanvas.Update()` (RVA `0x85C050`) sondea `CheckUIDecide`, `CheckUICancel`, `CheckLeftMoveAction` y enruta al `ISelectable` activo (`UIConversationGroup.OnSubmit` completa el texto o avanza; `UIScenarioChoices` cambia `_selected` entre `_options` y confirma/cancela). `UIScenarioSkip.Update()` (RVA `0x85B1C0`) comprueba `CheckAction(UICancel, Hold)` para saltar cinemáticas/escenas. |
| **Mapa** | `FieldStage.UI.Map.UIMap`, `MapWindow` | `UIMap` usa `MapUI.Scroll`, `MapUI.Swiching`, `MapUI.Zoom` (este último ya acepta rueda del ratón), `UISelectL2`/`R2` y `UIMapSwitching`. |
| **Castillo / Campamento / Tiendas / Selección de grupo** | `FortressTown.*`, `ShopCanvasBase`, `ItemShopPurchaseWindow`, `ShopSaleWindow`, `PartyOrganization`, `PartyFormationWindow`, `SaveLoadCanvas` | Casi todas heredan de `ScrollList`, `DialogSelector` o usan listas de `ListItemBase`/`SelectorButton` con `InputManager`. |
| **Exploración y cámara** | `FieldStage.PlayerControlDefault`, `FieldStage.FieldCamera` | `PlayerControlDefault.Update()` (RVA `0xDF0070`) lee `InputManager.CheckLeftAxis` (movimiento) y `CheckAction(PlayerWalk [1], Hold)` (caminar/correr). `CheckAction()` (RVA `0xDEE4A0`) lee `MenuOpen [2]`, `MapOpen [3]`, `UIOption [1012]`, `PlayerCheck [0]`. `UpdateCamera()` (RVA `0xDEFDD0`) pasa `InputManager.RightMoveValue` directo a `FieldCamera.UpdatePosition(Vector2)` (controla un `CinemachineFreeLook`). |

**Implicación de diseño para el ratón en UI**: Como las pantallas usan índices propios (`ScrollList._selectIndex`, `DialogSelector._cursor`, `UIScenarioChoices._selected`, `Battle.UI` `IndexSequencer`, `TitleContentSelectList._index`, etc.) en lugar de `EventSystem`, nuestro módulo de ratón en UI hará hit-test/raycast directo sobre los `RectTransform` visibles del componente activo de mayor prioridad (sin bucles costosos: solo cuando el puntero se mueve o se hace clic), moviendo el índice real del componente (el mismo método que usa el teclado) en *hover* y disparando exactamente el mismo evento `Confirmar`/`Cancelar` en clic izquierdo/derecho.

---

## 4. Control del Cursor

- **Búsqueda exhaustiva en `GameAssembly.dll`**:
  - `UnityEngine.Cursor.set_visible` (RVA `0x37018C0`) y `UnityEngine.Cursor.set_lockState` (RVA `0x3701880`) tienen **0 llamadas desde el código real de Eiyuden Chronicle**. Las únicas 8 referencias pertenecen a scripts de demostración sin usar incluidos en plugins de terceros (`Yagiuchi.SimpleCameraController`, `GPUInstancer.FPController/MouseLook/TerrainGenerator`, `StylizedGrassDemo.OrbitCamera`).
  - No existe ningún P/Invoke a `ShowCursor` ni `ClipCursor` en `Assembly-CSharp`.
- **Conclusión**: El juego no oculta ni bloquea activamente el cursor; simplemente nunca configura ni escucha el ratón. Nuestro mod puede controlar limpiamente `Cursor.visible` y `Cursor.lockState` (`CursorLockMode.None`) según el contexto (siempre visible y libre en menús, diálogos y combate; configurable en exploración — visible, oculto al mover con teclado, o discreto — y siempre liberado al perder foco o cambiar de escena).

---

## 5. Controles Reales Actuales (Extraídos de `MyInput` y `GameAssembly.dll`)

> Todos los 33 registros de `InputRuleTable._rules` y todos los bindings de `MyInput` han sido verificados en vivo (Slice 1 `LogOutput.log`) y en el JSON embebido de `GameAssembly.dll`.

| Mapa | Acción `MyInput` | Tecla actual (Keyboard) | Botón Xbox / PS4 | Uso principal verificado en `InputRuleTable._rules` |
|---|---|---|---|---|
| `Generic` | `LeftMove` / `LeftMoveDigitalOnly` | **WASD** y **Flechas** (ambas ya activas) | Stick Izq. / Cruceta | Mover personaje, mover cursor en menús y combate |
| `Generic` | `RightMove` / `RightMoveDigitalOnly` | **I / J / K / L** | Stick Der. | Rotar cámara de campo (`FieldCamera.UpdatePosition`), scroll secundario |
| `Generic` | `A` | **Enter** | A / Cruz | `PlayerCheck` (interactuar/hablar), `UIDecide` (confirmar), `WarDecide`, avanzar diálogo |
| `Generic` | `B` | **Retroceso (Backspace)** | B / Círculo | `UICancel` (cancelar/atrás), `PlayerWalk` (mantener para caminar/correr), `WarCancel`, mantener para saltar escena (`UIScenarioSkip`) |
| `Generic` | `X` | **R** | X / Cuadrado | `MapOpen` (abrir mapa en campo — verificado en vivo), `UIAutoEquip` (auto-equipar / acción auxiliar en menús), `WarCorpsDetail` |
| `Generic` | `Y` | **T** | Y / Triángulo | `MenuOpen` (abrir menú principal en campo — verificado en vivo), `UIStatusChange` (cambiar vista/estado/detalles en menús), `UIMapSwitching`, `WarBattleStart`/`WarCorpsEdit` |
| `Generic` | `LB` | **Q** | LB / L1 | `UISelectL` (pestaña / personaje / página anterior) |
| `Generic` | `RB` | **E** | RB / R1 | `UISelectR` (pestaña / personaje / página siguiente) |
| `Generic` | `LT` | **U** | LT / L2 | `UISelectL2` (sección principal anterior en carrusel / mapa) |
| `Generic` | `RT` | **O** | RT / R2 | `UISelectR2` (sección principal siguiente en carrusel / mapa) |
| `Generic` | `LS` | **G** | L3 (clic stick izq.) | `UISelectL3` (ordenar / filtrar / cambiar todos) |
| `Generic` | `RS` | **H** | R3 (clic stick der.) | `UISelectR3` (acción auxiliar / reset cámara en objetos / cantidad) |
| `Generic` | `Menu` | **Y** | Start / Options | `UIOption` (abrir menú de sistema/pausa en campo; acción secundaria en tiendas) |
| `Generic` | `Guide` | *(sin tecla en teclado)* | Select / Touchpad | Guía / vista auxiliar (solo mando en vanilla) |
| `Generic` | `AnyButton` | **Cualquier tecla** | Cualquier botón | Pantalla de título / saltar vídeo de intro |
| `SystemUI` | `A` / `B` / `LeftMove` | **Enter** / **Backspace** / **WASD + Flechas** | A / B / Stick+Cruceta | Diálogos de sistema (guardar, errores, confirmaciones críticas) |
| `MapUI` | `Scroll` / `Swiching` / `Zoom` | **Flechas** / **WASD** / **I, K + Rueda ratón** | Stick Izq. / Cruceta / Stick Der. | Desplazar mapa, cambiar planta/zona, zoom del mapa |
