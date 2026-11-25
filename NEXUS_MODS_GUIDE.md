# Guía paso a paso para subir EiyudenKBM a Nexus Mods

Tu archivo listo para subir a Nexus Mods ya está empaquetado aquí:
- **`releases\EiyudenKBM_v1.0.0.zip`** *(contiene únicamente `BepInEx\plugins\EiyudenKBM.dll` + documentación; 0 archivos del juego).*

---

## Paso 1: Crear la página del mod en Nexus Mods
1. Entra en **https://www.nexusmods.com/eiyudenchroniclehundredheroes** e inicia sesión con tu cuenta.
2. Haz clic en **Mods → Upload a Mod** (arriba a la derecha).
3. Rellena los campos básicos:
   - **Mod name**: `EiyudenKBM - Full Keyboard & Mouse Support + Rebinding`
   - **Category**: `Gameplay` o `User Interface`
   - **Version**: `1.0.0`
   - **Summary (máx. 255 caracteres)**:
     ```
     Full Keyboard & Mouse support for Eiyuden Chronicle: Hundred Heroes (BepInEx 6 IL2CPP): mouse hover/click/wheel in all menus, dialogs, map & turn-based battle, mouse camera rotation, in-game key & mouse4/mouse5 rebinding, and dynamic KBM/Gamepad prompts.
     ```
   - **GitHub / Source URL**: `https://github.com/aanderdc/EiyudenKBM`
4. En **Description**, pega directamente el bloque BBCode de abajo.

---

## Paso 2: Subir el archivo `.zip`
1. Ve a la pestaña **Files → Add a new file**.
2. Selecciona **`releases\EiyudenKBM_v1.0.0.zip`** (dentro de la carpeta del proyecto `EiyudenKBM`).
3. **File name**: `EiyudenKBM v1.0.0 (BepInEx 6 IL2CPP)`
4. **Category**: `Main Files`
5. **Version**: `1.0.0`
6. En **Requirements**, añade en *Off-site requirements*:
   - Name: `BepInEx 6.0.0 Bleeding Edge (Unity.IL2CPP-win-x64)`
   - Link: `https://builds.bepinex.dev/projects/bepinex_be`
   - Notes: `Required. Extract into the game root folder before installing EiyudenKBM.`
7. Sube 2–3 capturas de pantalla (por ejemplo: el menú **Opciones → Controles (Teclado y Ratón)** abierto con `F8`, los iconos de teclado/ratón en combate o menú) y pulsa **Publish**!

---

## Descripción lista para copiar y pegar (BBCode de Nexus Mods)

```bbcode
[size=5][b]EiyudenKBM — Full Keyboard & Mouse Support + In-Game Rebinding[/b][/size]

A lightweight [b]BepInEx 6 (Unity IL2CPP)[/b] plugin that brings comfortable, native Keyboard & Mouse controls, full in-game key & mouse button rebinding (including [b]Mouse4 / Mouse5[/b] side buttons), mouse camera control, and dynamic KBM/Gamepad prompt icons to [b]Eiyuden Chronicle: Hundred Heroes[/b].

[b]Source Code (MIT License):[/b] [url=https://github.com/aanderdc/EiyudenKBM]https://github.com/aanderdc/EiyudenKBM[/url]

[size=4][b]Features[/b][/size]
[list]
[*][b]Full Mouse UI Support[/b] — Hover & Left-click to select/confirm, Right-click or Esc to go back, Mouse Wheel to scroll lists, and Shift+Wheel to switch tabs across Title Screen, Pause/Options, Inventory, Equipment, Runes, Shops, Party Formation, Castle/HQ, Save/Load, Dialogues, Map, and Turn-Based Battle.
[*][b]In-Game Key & Mouse Rebinding Menu[/b] — Directly integrated inside [b]Options -> Controles (Teclado y Ratón)[/b] (also toggleable anytime with [b]F8[/b]). Supports all keyboard keys and all 5 mouse buttons ([b]LMB, RMB, MMB, Mouse4, Mouse5[/b]) with instant save & hot-reload in [font=Courier New]BepInEx/config/EiyudenKBM.cfg[/font].
[*][b]Exploration & Mouse Camera[/b] — WASD + Arrows movement, Left Shift walk/run hold, Space/F/LMB interact, Tab/Middle-click Main Menu, M Map, Esc/P Pause Menu, [b]hold Right-click + drag[/b] (rebindable to Middle/Mouse4/Mouse5) to rotate the field camera with configurable X/Y sensitivity & invert, plus Q/E quick turn.
[*][b]Turn-Based Battle[/b] — Mouse hover/click, Right-click back, Wheel scrolling, and [b]1–6 numeric shortcuts[/b] for commands, skills, Hero Combos, items, and target selection.
[*][b]Dialogues & Map[/b] — Left-click or Space to advance text, hover/click or 1–4 for dialogue choices, hold Esc/Right-click to skip cutscenes; Left-click drag to pan the map and Wheel/+/- to zoom.
[*][b]Smart Cursor Management[/b] — Always visible & free in menus, dialogues, map, and battle; auto-hides while walking with keyboard in exploration (configurable: AutoHide / AlwaysHidden / AlwaysVisible).
[*][b]Dynamic Prompt Icons & Seamless Gamepad Support[/b] — Fixes the vanilla bug where plugging in a controller locks prompts to controller icons. Shows crisp keyboard & mouse prompts ([b]LMB, RMB, MMB, M4, M5, SPC, ESC, TAB...[/b]) when using KBM and switches back to Xbox/PlayStation prompts automatically when touching a gamepad.
[*][b]Built-in Safe Mode[/b] — Automatically disables itself on the next boot if a previous startup did not complete cleanly. Single master [font=Courier New]Enabled[/font] toggle.
[/list]

[size=4][b]Requirements[/b][/size]
[list]
[*][b]BepInEx 6.0.0 Bleeding Edge (Unity.IL2CPP-win-x64)[/b] from [url=https://builds.bepinex.dev/projects/bepinex_be]builds.bepinex.dev/projects/bepinex_be[/url] (tested on [font=Courier New]6.0.0-be.793[/font]).
[/list]

[size=4][b]Installation[/b][/size]
[list=1]
[*]Install [b]BepInEx 6 (Unity.IL2CPP-win-x64)[/b] into your game root folder (where [font=Courier New]EiyudenChronicle.exe[/font] is located).
[*]Extract this zip directly into your game root folder so [font=Courier New]EiyudenKBM.dll[/font] lands in [font=Courier New]BepInEx\plugins\EiyudenKBM.dll[/font].
[*]Launch the game from Steam. [i](First launch with BepInEx takes 1–3 minutes to generate the IL2CPP interop cache; subsequent launches are fast.)[/i]
[*]Open [b]Options -> Controles (Teclado y Ratón)[/b] or press [b]F8[/b] anytime to customize bindings, cursor mode, and camera sensitivity.
[/list]

[size=4][b]Uninstallation[/b][/size]
Delete [font=Courier New]BepInEx\plugins\EiyudenKBM.dll[/font] (and optionally [font=Courier New]BepInEx\config\EiyudenKBM.cfg[/font]). Save files are never touched.
```
