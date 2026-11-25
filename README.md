# EiyudenKBM — Keyboard & Mouse Mod for Eiyuden Chronicle: Hundred Heroes

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-Download-orange)](https://www.nexusmods.com/eiyudenchronicle/mods/31)
[![GitHub Release](https://img.shields.io/github/v/release/aanderdc/EiyudenKBM)](https://github.com/aanderdc/EiyudenKBM/releases/latest)

A **BepInEx 6 (Unity IL2CPP)** mod that adds native, ergonomic **Keyboard & Mouse** support, full in-game key/mouse rebinding (including **Mouse4 / Mouse5** side buttons), dynamic keycap & mouse button prompts, and seamless KBM ↔ Gamepad switching for **Eiyuden Chronicle: Hundred Heroes** (Steam / PC).

> **Download:** [Nexus Mods](https://www.nexusmods.com/eiyudenchronicle/mods/31) · [GitHub Releases](https://github.com/aanderdc/EiyudenKBM/releases)
> **Zero game files modified or redistributed.** Everything lives cleanly inside `BepInEx/plugins` and `BepInEx/config`, with an automatic **Safe Mode** watchdog.

---

## Features / Características

- **Full Mouse UI Support** — Hover & Left-click to select/confirm, Right-click / `Esc` to cancel/back, Mouse Wheel to scroll lists, `Shift + Wheel` to switch tabs across:
  - Title Screen, Pause/System Menu, Options (toggles, selectors, sliders)
  - Inventory, Equipment, Runes, Shops, Party Formation, Castle/HQ, Save/Load
  - Story Dialogues & Choices (click or `Space` to advance, hover/click or `1`–`4` for choices, hold `Esc`/RMB to skip)
  - Turn-Based Battle (mouse hover/click & `1`–`6` shortcuts for main commands, unit commands, skills, Hero Combos, items, confirmations, and target cycling with scroll wheel)
  - Map (Left-click drag to pan, `WASD` + Arrows, Mouse Wheel or `+`/`-` to zoom)
- **Exploration & Camera** — `WASD` + Arrows movement, `Left Shift` walk/run hold, `Space`/`F`/LMB interact, `Tab`/Middle-click Main Menu, `M` Map, `Esc`/`P` Pause Menu, **Mouse Drag camera rotation** (hold Right-click by default, configurable to Middle/Mouse4/Mouse5 with sensitivity & invert X/Y) plus `Q`/`E` camera rotation.
- **In-Game Key & Mouse Rebinding Menu** — Accessible directly inside **Options → Controles (Teclado y Ratón)** or anytime with **`F8`**. Supports all keyboard keys and all 5 mouse buttons (**LMB, RMB, MMB, Mouse4, Mouse5**), plus hot-reload from `BepInEx/config/EiyudenKBM.cfg`.
- **Smart Cursor Management** — Always visible & unlocked in menus, dialogues, map, and battle; configurable in exploration (`AutoHideInField`, `AlwaysHiddenInField`, `AlwaysVisible`).
- **Dynamic Prompt Icons & Seamless Gamepad Switching** — Fixes the vanilla bug where having a controller plugged in locks on-screen icons to the controller. Displays crisp keyboard & mouse prompts (`LMB`, `RMB`, `MMB`, `M4`, `M5`, `SPC`, `ESC`, etc.) when using KBM and instantly switches back to Xbox/PlayStation prompts when touching a controller.
- **Safety First** — Automatic **Safe Mode** disables the mod on the next launch if a previous startup did not complete cleanly. Single master `Enabled` toggle in config.

---

## Requirements / Requisitos

- **Eiyuden Chronicle: Hundred Heroes** (PC / Steam, Unity 2021.3.32f1 IL2CPP x64)
- **BepInEx 6.0.0 Bleeding Edge (`Unity.IL2CPP-win-x64`)** — download from [builds.bepinex.dev/projects/bepinex_be](https://builds.bepinex.dev/projects/bepinex_be) *(tested with `6.0.0-be.793`)*.

---

## Installation / Instalación

1. Install **BepInEx 6 (`Unity.IL2CPP-win-x64`)** into your game root folder (`steamapps\common\Eiyuden Chronicle\`).
2. Download the latest `EiyudenKBM_vX.X.X.zip` from [Releases](../../releases) or Nexus Mods and extract `BepInEx\plugins\EiyudenKBM.dll` into your game folder.
3. Launch the game normally from Steam. *(The very first boot with BepInEx takes 1–3 minutes while `Il2CppInterop` builds its assembly cache.)*
4. Configure controls anytime via **Options → Controles (Teclado y Ratón)**, by pressing **`F8`**, or by editing `BepInEx\config\EiyudenKBM.cfg`.

### Uninstall / Desinstalar
Delete `BepInEx\plugins\EiyudenKBM.dll` (and optionally `BepInEx\config\EiyudenKBM.cfg`), or run `desinstalar_mod.bat` to remove BepInEx completely without touching save files.

---

## Documentation
- [CONTROLS.md](CONTROLS.md) — Default control table & mouse button reference
- [ARCHITECTURE.md](ARCHITECTURE.md) — Technical IL2CPP reverse-engineering notes
- [CHANGELOG.md](CHANGELOG.md) — Release history
- [CHECKLIST.md](CHECKLIST.md) — Manual verification checklist

## Building from Source
1. Run the game once with BepInEx 6 IL2CPP so `BepInEx\interop\*.dll` is generated.
2. Adjust `BepInExCoreDir` and `GameInteropDir` in `src/EiyudenKBM.csproj` if your Steam install path differs.
3. Run `build.bat` (requires .NET 6/8 SDK).

## License
MIT License — see [LICENSE](LICENSE). This repository contains **only original plugin code** and includes no game assets or copyrighted files.
