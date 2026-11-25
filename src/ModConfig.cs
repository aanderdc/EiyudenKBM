using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace EiyudenKBM
{
    public enum FieldCursorBehavior
    {
        AutoHideInField,
        AlwaysHiddenInField,
        AlwaysVisible
    }

    internal sealed class ModConfig
    {
        public static string ConfigFilePath => Path.Combine(Paths.ConfigPath, "EiyudenKBM.cfg");

        public event Action OnChanged;

        // 00_General
        public ConfigEntry<bool> Enabled { get; }
        public ConfigEntry<bool> ResetSafeMode { get; }
        public ConfigEntry<bool> VerboseLogging { get; }
        public ConfigEntry<bool> KeepOriginalBindings { get; }
        public ConfigEntry<string> RebindMenuKey { get; }

        // 01_Cursor
        public ConfigEntry<FieldCursorBehavior> FieldCursorMode { get; }
        public ConfigEntry<float> AutoHideDelaySeconds { get; }

        // 02_Camera
        public ConfigEntry<string> MouseDragCameraButton { get; }
        public ConfigEntry<float> MouseCameraSensitivityX { get; }
        public ConfigEntry<float> MouseCameraSensitivityY { get; }
        public ConfigEntry<bool> InvertMouseCameraX { get; }
        public ConfigEntry<bool> InvertMouseCameraY { get; }
        public ConfigEntry<string> FieldCameraLeftKey { get; }
        public ConfigEntry<string> FieldCameraRightKey { get; }

        // 03_Exploration
        public ConfigEntry<string> Interact { get; }
        public ConfigEntry<string> WalkToggleHold { get; }
        public ConfigEntry<string> MainMenu { get; }
        public ConfigEntry<string> PauseSystemMenu { get; }
        public ConfigEntry<string> WorldMap { get; }

        // 04_UI_Menus
        public ConfigEntry<string> Confirm { get; }
        public ConfigEntry<string> Cancel { get; }
        public ConfigEntry<string> TabPrevLB { get; }
        public ConfigEntry<string> TabNextRB { get; }
        public ConfigEntry<string> SectionPrevLT { get; }
        public ConfigEntry<string> SectionNextRT { get; }
        public ConfigEntry<string> AuxActionX { get; }
        public ConfigEntry<string> DetailsViewY { get; }
        public ConfigEntry<string> SortFilterLS { get; }
        public ConfigEntry<string> SecondaryActionRS { get; }

        // 05_Dialog_And_Battle
        public ConfigEntry<string> DialogAdvance { get; }
        public ConfigEntry<string> DialogSkipHold { get; }
        public ConfigEntry<bool> BattleNumberShortcuts { get; }
        public ConfigEntry<bool> MouseWheelScrollLists { get; }
        public ConfigEntry<bool> ShiftWheelSwitchTabs { get; }

        // Cached parsed token lists (avoid string.Split per frame!)
        public List<string> InteractTokens { get; private set; } = new();
        public List<string> WalkToggleHoldTokens { get; private set; } = new();
        public List<string> MainMenuTokens { get; private set; } = new();
        public List<string> PauseSystemMenuTokens { get; private set; } = new();
        public List<string> WorldMapTokens { get; private set; } = new();
        public List<string> ConfirmTokens { get; private set; } = new();
        public List<string> CancelTokens { get; private set; } = new();
        public List<string> TabPrevLBTokens { get; private set; } = new();
        public List<string> TabNextRBTokens { get; private set; } = new();
        public List<string> SectionPrevLTTokens { get; private set; } = new();
        public List<string> SectionNextRTTokens { get; private set; } = new();
        public List<string> AuxActionXTokens { get; private set; } = new();
        public List<string> DetailsViewYTokens { get; private set; } = new();
        public List<string> SortFilterLSTokens { get; private set; } = new();
        public List<string> SecondaryActionRSTokens { get; private set; } = new();
        public List<string> DialogAdvanceTokens { get; private set; } = new();
        public List<string> DialogSkipHoldTokens { get; private set; } = new();
        public List<string> RebindMenuKeyTokens { get; private set; } = new();

        public ConfigFile RawConfig { get; }
        private DateTime _lastFileWriteUtc;

        public ModConfig()
        {
            RawConfig = new ConfigFile(ConfigFilePath, true);
            var cfg = RawConfig;

            const string bindingHint =
                "Teclas separadas por coma. Soporta teclado (Space, Escape, Tab, LeftShift, F, Q, E, M, PageUp, etc.) " +
                "y botones de ratón: MouseLeft, MouseRight, MouseMiddle, Mouse4 (lateral atrás), Mouse5 (lateral adelante).";

            // 00_General
            Enabled = cfg.Bind("00_General", "Enabled", true,
                "Interruptor general del mod (true = activado, false = desactivado por completo).");
            ResetSafeMode = cfg.Bind("00_General", "ResetSafeMode", false,
                "Ponlo en true una vez si quieres salir del Modo Seguro tras un crash previo (se restablece solo a false).");
            VerboseLogging = cfg.Bind("00_General", "VerboseLogging", false,
                "Registra trazas detalladas en BepInEx/LogOutput.log para depuración.");
            KeepOriginalBindings = cfg.Bind("00_General", "KeepOriginalBindings", true,
                "Mantiene también las teclas originales del juego (Enter, Backspace, R, T, Y, I/J/K/L) como respaldo.");
            RebindMenuKey = cfg.Bind("00_General", "RebindMenuKey", "F8",
                "Tecla rápida para abrir/cerrar el panel de reasignación de controles (además de Ajustes -> Controles).");

            // 01_Cursor
            FieldCursorMode = cfg.Bind("01_Cursor", "FieldCursorMode", FieldCursorBehavior.AutoHideInField,
                "Comportamiento del cursor en exploración (en menús, diálogos, mapa y combate SIEMPRE es visible y libre). " +
                "AutoHideInField = se oculta al caminar con teclado y vuelve al mover el ratón; AlwaysHiddenInField = oculto en campo; AlwaysVisible = siempre visible.");
            AutoHideDelaySeconds = cfg.Bind("01_Cursor", "AutoHideDelaySeconds", 2.0f,
                "Segundos sin mover el ratón antes de ocultarlo en exploración (solo en modo AutoHideInField).");

            // 02_Camera
            MouseDragCameraButton = cfg.Bind("02_Camera", "MouseDragCameraButton", "MouseRight",
                "Botón de ratón a mantener pulsado para girar la cámara en exploración (MouseRight, MouseMiddle, Mouse4, Mouse5 o None).");
            MouseCameraSensitivityX = cfg.Bind("02_Camera", "MouseCameraSensitivityX", 1.0f,
                "Sensibilidad horizontal de rotación de cámara con ratón.");
            MouseCameraSensitivityY = cfg.Bind("02_Camera", "MouseCameraSensitivityY", 1.0f,
                "Sensibilidad vertical de rotación de cámara con ratón.");
            InvertMouseCameraX = cfg.Bind("02_Camera", "InvertMouseCameraX", false,
                "Invertir eje horizontal de cámara con ratón.");
            InvertMouseCameraY = cfg.Bind("02_Camera", "InvertMouseCameraY", false,
                "Invertir eje vertical de cámara con ratón.");
            FieldCameraLeftKey = cfg.Bind("02_Camera", "FieldCameraLeftKey", "Q",
                "Tecla para girar la cámara a la izquierda durante la exploración.");
            FieldCameraRightKey = cfg.Bind("02_Camera", "FieldCameraRightKey", "E",
                "Tecla para girar la cámara a la derecha durante la exploración.");

            // 03_Exploration
            Interact = cfg.Bind("03_Exploration", "Interact", "Space, F, MouseLeft",
                "Interactuar / hablar / examinar en exploración (PlayerCheck). " + bindingHint);
            WalkToggleHold = cfg.Bind("03_Exploration", "WalkToggleHold", "LeftShift",
                "Mantener para alternar caminar/correr en exploración (PlayerWalk). " + bindingHint);
            MainMenu = cfg.Bind("03_Exploration", "MainMenu", "Tab, MouseMiddle",
                "Abrir menú principal (Inventario, Equipo, Grupo) en exploración (MenuOpen). " + bindingHint);
            PauseSystemMenu = cfg.Bind("03_Exploration", "PauseSystemMenu", "Escape, P",
                "Abrir menú de pausa / sistema / opciones en exploración (UIOption). " + bindingHint);
            WorldMap = cfg.Bind("03_Exploration", "WorldMap", "M",
                "Abrir mapa en exploración (MapOpen). " + bindingHint);

            // 04_UI_Menus
            Confirm = cfg.Bind("04_UI_Menus", "Confirm", "Space, MouseLeft",
                "Confirmar en menús, listas y combate (UIDecide / SystemUIDecide). " + bindingHint);
            Cancel = cfg.Bind("04_UI_Menus", "Cancel", "Escape, MouseRight",
                "Cancelar / volver atrás en menús y combate (UICancel / SystemUICancel). " + bindingHint);
            TabPrevLB = cfg.Bind("04_UI_Menus", "TabPrev_LB", "Q",
                "Pestaña o personaje anterior (UISelectL / LB). " + bindingHint);
            TabNextRB = cfg.Bind("04_UI_Menus", "TabNext_RB", "E",
                "Pestaña o personaje siguiente (UISelectR / RB). " + bindingHint);
            SectionPrevLT = cfg.Bind("04_UI_Menus", "SectionPrev_LT", "Z, PageUp",
                "Sección principal anterior en carrusel / mapa (UISelectL2 / LT). " + bindingHint);
            SectionNextRT = cfg.Bind("04_UI_Menus", "SectionNext_RT", "C, PageDown",
                "Sección principal siguiente en carrusel / mapa (UISelectR2 / RT). " + bindingHint);
            AuxActionX = cfg.Bind("04_UI_Menus", "AuxAction_X", "R",
                "Auto-equipar / acción auxiliar X en menús (UIAutoEquip). " + bindingHint);
            DetailsViewY = cfg.Bind("04_UI_Menus", "DetailsView_Y", "T, V",
                "Ver detalles / cambiar vista Y en menús (UIStatusChange). " + bindingHint);
            SortFilterLS = cfg.Bind("04_UI_Menus", "SortFilter_LS", "G",
                "Ordenar / filtrar L3 en listas (UISelectL3). " + bindingHint);
            SecondaryActionRS = cfg.Bind("04_UI_Menus", "SecondaryAction_RS", "H",
                "Acción auxiliar R3 / cantidad máx. (UISelectR3). " + bindingHint);

            // 05_Dialog_And_Battle
            DialogAdvance = cfg.Bind("05_Dialog_And_Battle", "DialogAdvance", "Space, MouseLeft",
                "Avanzar o completar texto en diálogos. " + bindingHint);
            DialogSkipHold = cfg.Bind("05_Dialog_And_Battle", "DialogSkipHold", "Escape, MouseRight",
                "Mantener para saltar cinemáticas/escenas (UIScenarioSkip). " + bindingHint);
            BattleNumberShortcuts = cfg.Bind("05_Dialog_And_Battle", "BattleNumberShortcuts", true,
                "Habilita atajos numéricos 1-6 en combate por turnos y opciones de diálogo.");
            MouseWheelScrollLists = cfg.Bind("05_Dialog_And_Battle", "MouseWheelScrollLists", true,
                "Permite usar la rueda del ratón para desplazar listas y menús.");
            ShiftWheelSwitchTabs = cfg.Bind("05_Dialog_And_Battle", "ShiftWheelSwitchTabs", true,
                "Permite usar Shift + Rueda del ratón para cambiar de pestaña o personaje.");

            RebuildTokenCache();
            RecordFileWriteTime();
        }

        public void RebuildTokenCache()
        {
            InteractTokens = ParseTokens(Interact.Value);
            WalkToggleHoldTokens = ParseTokens(WalkToggleHold.Value);
            MainMenuTokens = ParseTokens(MainMenu.Value);
            PauseSystemMenuTokens = ParseTokens(PauseSystemMenu.Value);
            WorldMapTokens = ParseTokens(WorldMap.Value);
            ConfirmTokens = ParseTokens(Confirm.Value);
            CancelTokens = ParseTokens(Cancel.Value);
            TabPrevLBTokens = ParseTokens(TabPrevLB.Value);
            TabNextRBTokens = ParseTokens(TabNextRB.Value);
            SectionPrevLTTokens = ParseTokens(SectionPrevLT.Value);
            SectionNextRTTokens = ParseTokens(SectionNextRT.Value);
            AuxActionXTokens = ParseTokens(AuxActionX.Value);
            DetailsViewYTokens = ParseTokens(DetailsViewY.Value);
            SortFilterLSTokens = ParseTokens(SortFilterLS.Value);
            SecondaryActionRSTokens = ParseTokens(SecondaryActionRS.Value);
            DialogAdvanceTokens = ParseTokens(DialogAdvance.Value);
            DialogSkipHoldTokens = ParseTokens(DialogSkipHold.Value);
            RebindMenuKeyTokens = ParseTokens(RebindMenuKey.Value);
        }

        public void SaveAndNotify()
        {
            try
            {
                RawConfig.Save();
                RebuildTokenCache();
                RecordFileWriteTime();
                OnChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError("[ModConfig] Error guardando configuración: " + ex);
            }
        }

        public void ResetAllToDefaults()
        {
            foreach (var entry in RawConfig.Values)
            {
                entry.BoxedValue = entry.DefaultValue;
            }
            SaveAndNotify();
        }

        /// <summary>
        /// Lightweight hot-reload check (called every ~2 seconds from Update, never every frame).
        /// </summary>
        public void CheckHotReload(ManualLogSource log)
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return;
                var writeTime = File.GetLastWriteTimeUtc(ConfigFilePath);
                if (writeTime > _lastFileWriteUtc)
                {
                    _lastFileWriteUtc = writeTime;
                    RawConfig.Reload();
                    RebuildTokenCache();
                    log?.LogInfo("[ModConfig] Configuración recargada en caliente desde EiyudenKBM.cfg.");
                    LogActiveBindings(log);
                    OnChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                log?.LogWarning("[ModConfig] Error en recarga en caliente: " + ex.Message);
            }
        }

        private void RecordFileWriteTime()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                    _lastFileWriteUtc = File.GetLastWriteTimeUtc(ConfigFilePath);
            }
            catch { }
        }

        public void LogActiveBindings(ManualLogSource log)
        {
            if (log == null) return;
            log.LogInfo("=== EiyudenKBM: Configuración y bindings activos (EiyudenKBM.cfg) ===");
            log.LogInfo($"  Enabled={Enabled.Value}, KeepOriginal={KeepOriginalBindings.Value}, Cursor={FieldCursorMode.Value}, RebindKey={RebindMenuKey.Value}");
            log.LogInfo($"  [Cámara] Drag={MouseDragCameraButton.Value}, Sens=({MouseCameraSensitivityX.Value:0.00},{MouseCameraSensitivityY.Value:0.00}), Keys={FieldCameraLeftKey.Value}/{FieldCameraRightKey.Value}");
            log.LogInfo($"  [Campo]  Interact={Interact.Value} | Walk={WalkToggleHold.Value} | Menu={MainMenu.Value} | Pause={PauseSystemMenu.Value} | Map={WorldMap.Value}");
            log.LogInfo($"  [Menús]  Confirm={Confirm.Value} | Cancel={Cancel.Value} | LB/RB={TabPrevLB.Value}/{TabNextRB.Value} | LT/RT={SectionPrevLT.Value}/{SectionNextRT.Value}");
            log.LogInfo($"  [Extra]  X={AuxActionX.Value} | Y={DetailsViewY.Value} | L3={SortFilterLS.Value} | R3={SecondaryActionRS.Value}");
            log.LogInfo($"  [Diálogo/Combate] Advance={DialogAdvance.Value} | SkipHold={DialogSkipHold.Value} | NumKeys={BattleNumberShortcuts.Value} | Wheel={MouseWheelScrollLists.Value}");
            log.LogInfo("===================================================================");
        }

        public static List<string> ParseTokens(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return result;
            foreach (var part in raw.Split(','))
            {
                var t = part.Trim();
                if (t.Length > 0 && !t.Equals("None", StringComparison.OrdinalIgnoreCase))
                    result.Add(t);
            }
            return result;
        }
    }
}
