using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// Minimal, well-scoped Harmony patches on InputManager:
    /// - Adds rebindable Keyboard + Mouse (LMB/RMB/MMB/Mouse4/Mouse5) actions per InputActionMode without removing originals.
    /// - Fixes InputManager.CheckDevice() so Keyboard/Mouse <-> Gamepad switching works seamlessly even with a controller plugged in.
    /// - Adds Mouse Wheel navigation in UI menus/lists (and Shift+Wheel for tabs).
    /// </summary>
    internal static class InputPatches
    {
        private static int _lastWheelMoveFrame = -1;
        private static InputCrossStatus.MoveDir _pendingWheelDir = InputCrossStatus.MoveDir.None;
        private static int _lastSyntheticTabFrame = -1;
        private static InputActionMode _pendingSyntheticTabMode = InputActionMode.Ignore;

        private static bool _preferKeyboardIcons = true;
        private static int _lastDeviceCheckFrame = -1;

        public static void RequestSyntheticAction(InputActionMode mode)
        {
            _pendingSyntheticTabMode = mode;
            _lastSyntheticTabFrame = Time.frameCount;
        }

        public static void UpdateWheelState()
        {
            if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                return;

            float scrollY = KeyBindingSystem.GetMouseScrollY();
            if (Mathf.Abs(scrollY) < 0.01f)
                return;

            if (Plugin.Settings.ShiftWheelSwitchTabs.Value && KeyBindingSystem.IsAnyShiftPressed())
            {
                _pendingSyntheticTabMode = scrollY > 0f ? InputActionMode.UISelectL : InputActionMode.UISelectR;
                _lastSyntheticTabFrame = Time.frameCount;
                return;
            }

            if (Plugin.Settings.MouseWheelScrollLists.Value && CursorManager.IsUIActiveRecently)
            {
                _pendingWheelDir = scrollY > 0f ? InputCrossStatus.MoveDir.Up : InputCrossStatus.MoveDir.Down;
                _lastWheelMoveFrame = Time.frameCount;
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckAction))]
        private static class Patch_CheckAction
        {
            [HarmonyPrefix]
            private static bool Prefix(
                InputActionMode mode,
                InputCheckMode checkMode,
                string seCueSheetName,
                string overrideSEName,
                ref bool __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                        return true;

                    // Completely block game input while our Settings/Rebind overlay is open
                    if (SettingsRebindUI.IsOpen || SettingsRebindUI.ClosedThisFrame)
                    {
                        __result = false;
                        return false;
                    }

                    // 1) Synthetic tab/section switch from clicking carousel arrows or Shift+Wheel
                    if (checkMode == InputCheckMode.Trigger && _lastSyntheticTabFrame == Time.frameCount && _pendingSyntheticTabMode == mode)
                    {
                        _lastSyntheticTabFrame = -1;
                        PlayActionSE(mode, seCueSheetName, overrideSEName);
                        __result = true;
                        return false;
                    }

                    // 2) Evaluate configured custom bindings for this InputActionMode
                    if (EvaluateCustomBinding(mode, checkMode))
                    {
                        PlayActionSE(mode, seCueSheetName, overrideSEName);
                        __result = true;
                        return false;
                    }

                    // 3) If KeepOriginalBindings == false, block vanilla keyboard while keeping gamepad alive
                    if (!Plugin.Settings.KeepOriginalBindings.Value && InputManager.DeviceType == InputManager.InputDeviceType.Keyboard)
                    {
                        // Keep AnyButton working on title screen
                        if (mode != InputActionMode.UIAnyButton)
                        {
                            __result = false;
                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[InputPatches.CheckAction] " + ex.Message);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckLeftMoveAction))]
        private static class Patch_CheckLeftMoveAction
        {
            [HarmonyPrefix]
            private static bool Prefix(
                ref InputCrossStatus.MoveDir dir,
                ref int count,
                ref bool repeated,
                string seCueSheetName,
                string overrideSEName,
                ref bool __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                        return true;

                    if (SettingsRebindUI.IsOpen || SettingsRebindUI.ClosedThisFrame)
                    {
                        dir = InputCrossStatus.MoveDir.None;
                        count = 0;
                        repeated = false;
                        __result = false;
                        return false;
                    }

                    if (_lastWheelMoveFrame == Time.frameCount && _pendingWheelDir != InputCrossStatus.MoveDir.None)
                    {
                        dir = _pendingWheelDir;
                        count = 1;
                        repeated = false;
                        _lastWheelMoveFrame = -1;
                        _pendingWheelDir = InputCrossStatus.MoveDir.None;

                        string se = string.IsNullOrEmpty(overrideSEName) ? "SE_sys_0002" : overrideSEName;
                        InputManager.PlayInputSE(seCueSheetName, se);
                        __result = true;
                        return false;
                    }

                    return true;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckSystemUILeftMove),
            new Type[] { typeof(InputCrossStatus.MoveDir), typeof(int), typeof(bool) },
            new ArgumentType[] { ArgumentType.Out, ArgumentType.Out, ArgumentType.Out })]
        private static class Patch_CheckSystemUILeftMove
        {
            [HarmonyPrefix]
            private static bool Prefix(
                ref InputCrossStatus.MoveDir dir,
                ref int count,
                ref bool repeated,
                ref bool __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                        return true;

                    if (SettingsRebindUI.IsOpen || SettingsRebindUI.ClosedThisFrame)
                    {
                        dir = InputCrossStatus.MoveDir.None;
                        count = 0;
                        repeated = false;
                        __result = false;
                        return false;
                    }

                    if (_lastWheelMoveFrame == Time.frameCount && _pendingWheelDir != InputCrossStatus.MoveDir.None)
                    {
                        dir = _pendingWheelDir;
                        count = 1;
                        repeated = false;
                        _lastWheelMoveFrame = -1;
                        _pendingWheelDir = InputCrossStatus.MoveDir.None;
                        __result = true;
                        return false;
                    }

                    return true;
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Fixes the vanilla bug in InputManager.CheckDevice() where having any controller plugged in
        /// locks DeviceType to the controller forever. Now switches cleanly between Keyboard/Mouse and Gamepad.
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "CheckDevice")]
        private static class Patch_CheckDevice
        {
            [HarmonyPostfix]
            private static void Postfix(InputManager __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || __instance == null)
                        return;

                    if (_lastDeviceCheckFrame != Time.frameCount)
                    {
                        _lastDeviceCheckFrame = Time.frameCount;

                        // Check if any gamepad control was meaningfully actuated
                        bool gamepadUsed = false;
                        var pads = Gamepad.all;
                        if (pads != null)
                        {
                            for (int i = 0; i < pads.Count; i++)
                            {
                                var pad = pads[i];
                                if (pad != null && InputControlExtensions.IsActuated(pad, 0.25f))
                                {
                                    gamepadUsed = true;
                                    break;
                                }
                            }
                        }

                        if (gamepadUsed)
                        {
                            _preferKeyboardIcons = false;
                        }
                        else if (KeyBindingSystem.AnyKeyboardOrMouseActivity())
                        {
                            _preferKeyboardIcons = true;
                        }
                    }

                    if (_preferKeyboardIcons)
                    {
                        __instance._deviceType = InputManager.InputDeviceType.Keyboard;
                    }
                }
                catch { }
            }
        }

        private static bool EvaluateCustomBinding(InputActionMode mode, InputCheckMode checkMode)
        {
            var s = Plugin.Settings;
            switch (mode)
            {
                case InputActionMode.UIDecide:
                case InputActionMode.SystemUIDecide:
                case InputActionMode.WarDecide:
                {
                    // Non-MouseLeft tokens (e.g., Space, Mouse4, etc.) work anywhere in UI
                    if (KeyBindingSystem.CheckTokenList(s.ConfirmTokens, checkMode, allowMouseLeft: false))
                        return true;

                    // MouseLeft confirms ONLY when clicking a valid UI item (or "click anywhere" screen)
                    if (checkMode == InputCheckMode.Trigger
                        && MouseUIController.ShouldAllowMouseClickConfirmThisFrame()
                        && HasToken(s.ConfirmTokens, "MouseLeft"))
                    {
                        return KeyBindingSystem.CheckToken("MouseLeft", InputCheckMode.Trigger, allowMouseLeft: true);
                    }
                    return false;
                }

                case InputActionMode.UICancel:
                case InputActionMode.SystemUICancel:
                case InputActionMode.WarCancel:
                {
                    if (checkMode == InputCheckMode.Hold)
                    {
                        // Used by UIScenarioSkip (hold to skip cutscene)
                        return KeyBindingSystem.CheckTokenList(s.DialogSkipHoldTokens, InputCheckMode.Hold, allowMouseLeft: false);
                    }
                    return KeyBindingSystem.CheckTokenList(s.CancelTokens, checkMode, allowMouseLeft: false);
                }

                case InputActionMode.UIAnyButton:
                {
                    if (checkMode == InputCheckMode.Trigger)
                    {
                        if (KeyBindingSystem.CheckToken("MouseLeft", InputCheckMode.Trigger, true))
                            return true;
                        if (KeyBindingSystem.CheckTokenList(s.ConfirmTokens, InputCheckMode.Trigger, true))
                            return true;
                    }
                    return false;
                }

                case InputActionMode.PlayerCheck:
                    // In field exploration: Space, F, or MouseLeft (when not over UI)
                    return KeyBindingSystem.CheckTokenList(s.InteractTokens, checkMode, allowMouseLeft: !CursorManager.IsUIActiveRecently);

                case InputActionMode.PlayerWalk:
                    return KeyBindingSystem.CheckTokenList(s.WalkToggleHoldTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.MenuOpen:
                    return KeyBindingSystem.CheckTokenList(s.MainMenuTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.MapOpen:
                    return KeyBindingSystem.CheckTokenList(s.WorldMapTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UIOption:
                    // In exploration, Escape/P opens Pause/System menu; if a UI is already active, avoid firing Escape as UIOption
                    return KeyBindingSystem.CheckTokenList(s.PauseSystemMenuTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectL:
                case InputActionMode.WarAngerGauge:
                    return KeyBindingSystem.CheckTokenList(s.TabPrevLBTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectR:
                case InputActionMode.WarMoraleGauge:
                    return KeyBindingSystem.CheckTokenList(s.TabNextRBTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectL2:
                    return KeyBindingSystem.CheckTokenList(s.SectionPrevLTTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectR2:
                    return KeyBindingSystem.CheckTokenList(s.SectionNextRTTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectL3:
                    return KeyBindingSystem.CheckTokenList(s.SortFilterLSTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UISelectR3:
                    return KeyBindingSystem.CheckTokenList(s.SecondaryActionRSTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UIAutoEquip:
                case InputActionMode.WarCorpsDetail:
                    return KeyBindingSystem.CheckTokenList(s.AuxActionXTokens, checkMode, allowMouseLeft: false);

                case InputActionMode.UIStatusChange:
                case InputActionMode.UIMapSwitching:
                case InputActionMode.WarBattleStart:
                case InputActionMode.WarCorpsEdit:
                case InputActionMode.WarCorpsEditComplete:
                    return KeyBindingSystem.CheckTokenList(s.DetailsViewYTokens, checkMode, allowMouseLeft: false);

                default:
                    return false;
            }
        }

        private static bool HasToken(List<string> tokens, string needle)
        {
            if (tokens == null) return false;
            for (int i = 0; i < tokens.Count; i++)
                if (tokens[i].Equals(needle, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void PlayActionSE(InputActionMode mode, string seCueSheetName, string overrideSEName)
        {
            try
            {
                if (!string.IsNullOrEmpty(overrideSEName))
                {
                    InputManager.PlayInputSE(seCueSheetName, overrideSEName);
                    return;
                }
                var delayed = InputManager.GetDelayedSE(mode);
                if (delayed.IsValid)
                    delayed.Play();
            }
            catch { }
        }
    }
}
