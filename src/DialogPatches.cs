using System;
using Battle.UI;
using Common.UI;
using HarmonyLib;
using Scenario.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// Full mouse & keyboard support for story dialogues, dialogue choices, tutorials, alerts, and battle results:
    /// - Left-click or Space advances / completes dialogue text.
    /// - Hover + Left-click (or number keys 1-4) selects and confirms dialogue choices.
    /// - Hold Escape / MouseRight skips cutscenes (handled cleanly via InputPatches DialogSkipHold).
    /// </summary>
    internal static class DialogPatches
    {
        private static int _pendingChoiceSubmitFrame = -1;

        [HarmonyPatch(typeof(UIScenerioCanvas), "Update")]
        private static class Patch_ScenarioCanvas_Update
        {
            [HarmonyPrefix]
            private static void Prefix(UIScenerioCanvas __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || !__instance.IsActive || __instance._active == null)
                        return;

                    CursorManager.MarkUIActiveThisFrame();
                    MouseUIController.UpdateMouseMovementCache();

                    // 1) Dialogue choices active?
                    var choices = __instance._choices;
                    if (choices != null && choices.IsActive && choices.Interactable)
                    {
                        HandleScenarioChoices(choices);
                        return;
                    }

                    // 2) Normal conversation box active: Left-click anywhere or configured DialogAdvance advances text
                    var conv = __instance._conversationGroup;
                    if (conv != null && conv.IsActive && conv.Interactable)
                    {
                        var mouse = Mouse.current;
                        if (mouse != null && mouse.leftButton.wasPressedThisFrame
                            && KeyBindingSystem.CheckTokenList(Plugin.Settings.DialogAdvanceTokens, InputCheckMode.Trigger, allowMouseLeft: true))
                        {
                            MouseUIController.AllowMouseConfirmThisFrame();
                        }
                        else if (KeyBindingSystem.CheckTokenList(Plugin.Settings.DialogAdvanceTokens, InputCheckMode.Trigger, allowMouseLeft: false))
                        {
                            MouseUIController.AllowMouseConfirmThisFrame();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[DialogPatches.ScenarioCanvas] " + ex.Message);
                }
            }
        }

        private static void HandleScenarioChoices(UIScenarioChoices choices)
        {
            var options = choices._options;
            if (options == null || options.Length == 0) return;

            int max = choices._max > 0 ? Math.Min(choices._max, options.Length) : options.Length;
            int current = choices._selected;

            // Delayed confirm from number shortcut
            if (_pendingChoiceSubmitFrame == Time.frameCount)
            {
                _pendingChoiceSubmitFrame = -1;
                choices.OnSubmit();
                return;
            }

            // Number keys 1..4 shortcut
            if (Plugin.Settings.BattleNumberShortcuts.Value)
            {
                int numPressed = CheckNumberKeyPressed(Math.Min(max, 6));
                if (numPressed >= 0 && numPressed < max)
                {
                    if (numPressed != current)
                    {
                        choices._selected = numPressed;
                        choices.UpdateVisual(numPressed, current);
                        InputManager.PlayInputSE("system", "SE_sys_0002");
                    }
                    _pendingChoiceSubmitFrame = Time.frameCount + 1;
                    return;
                }
            }

            // Mouse hover & click on UIScenarioChoiceOption
            var mouse = Mouse.current;
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            Vector2 mousePos = KeyBindingSystem.GetMousePosition();

            for (int i = 0; i < max; i++)
            {
                var opt = options[i];
                if (opt == null || !opt.gameObject.activeInHierarchy) continue;

                Component hitTarget = opt._button != null ? (Component)opt._button : opt;
                if (MouseUIController.HitTestComponent(hitTarget, mousePos, out _))
                {
                    if (i != current)
                    {
                        choices._selected = i;
                        choices.UpdateVisual(i, current);
                        InputManager.PlayInputSE("system", "SE_sys_0002");
                    }

                    if (clicked)
                    {
                        MouseUIController.AllowMouseConfirmThisFrame();
                    }
                    break;
                }
            }
        }

        // Allow left-click anywhere to dismiss Tutorials, Battle Alerts, and Battle Results screens
        [HarmonyPatch(typeof(Tutorial), "UpdateIdle")]
        private static class Patch_Tutorial_UpdateIdle
        {
            [HarmonyPrefix]
            private static void Prefix()
            {
                if (Plugin.Settings?.Enabled.Value != true || SettingsRebindUI.IsOpen) return;
                CursorManager.MarkUIActiveThisFrame();
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    MouseUIController.AllowMouseConfirmThisFrame();
            }
        }

        [HarmonyPatch(typeof(UIAlert), "Update")]
        private static class Patch_BattleAlert_Update
        {
            [HarmonyPrefix]
            private static void Prefix(UIAlert __instance)
            {
                if (Plugin.Settings?.Enabled.Value != true || SettingsRebindUI.IsOpen || __instance == null) return;
                if (!__instance.gameObject.activeInHierarchy) return;
                CursorManager.MarkUIActiveThisFrame();
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    MouseUIController.AllowMouseConfirmThisFrame();
            }
        }

        [HarmonyPatch(typeof(UIResult), "Update")]
        private static class Patch_BattleResult_Update
        {
            [HarmonyPrefix]
            private static void Prefix(UIResult __instance)
            {
                if (Plugin.Settings?.Enabled.Value != true || SettingsRebindUI.IsOpen || __instance == null) return;
                if (!__instance.gameObject.activeInHierarchy) return;
                CursorManager.MarkUIActiveThisFrame();
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    MouseUIController.AllowMouseConfirmThisFrame();
            }
        }

        internal static int CheckNumberKeyPressed(int maxCount)
        {
            var kb = Keyboard.current;
            if (kb == null || maxCount <= 0) return -1;
            if (maxCount >= 1 && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)) return 0;
            if (maxCount >= 2 && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)) return 1;
            if (maxCount >= 3 && (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)) return 2;
            if (maxCount >= 4 && (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)) return 3;
            if (maxCount >= 5 && (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)) return 4;
            if (maxCount >= 6 && (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)) return 5;
            return -1;
        }
    }
}
