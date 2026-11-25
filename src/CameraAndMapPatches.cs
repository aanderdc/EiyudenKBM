using System;
using FieldStage.UI.Map;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// Exploration camera control (Mouse Drag + Q/E keys) and Map mouse dragging/zooming.
    /// Hooks directly into InputManager's camera/map axis getters so native Cinemachine walls,
    /// fixed cameras, and game invert settings are respected automatically.
    /// </summary>
    internal static class CameraAndMapPatches
    {
        private static int _lastMapActiveFrame = -100;
        private static bool IsMapOpenRecently => (Time.frameCount - _lastMapActiveFrame) <= 3;

        [HarmonyPatch(typeof(InputManager), "get_RightMoveValue")]
        private static class Patch_RightMoveValue
        {
            [HarmonyPostfix]
            private static void Postfix(ref Vector2 __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                        return;

                    if (SettingsRebindUI.IsOpen || CursorManager.IsUIActiveRecently)
                        return;

                    var s = Plugin.Settings;
                    Vector2 extra = Vector2.zero;

                    // 1) Mouse drag camera rotation (hold configured MouseDragCameraButton, e.g. MouseRight / MouseMiddle / Mouse4 / Mouse5)
                    string dragBtn = s.MouseDragCameraButton.Value;
                    if (!string.IsNullOrWhiteSpace(dragBtn) && !dragBtn.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        if (KeyBindingSystem.CheckToken(dragBtn, InputCheckMode.Hold, allowMouseLeft: true))
                        {
                            Vector2 delta = KeyBindingSystem.GetMouseDelta();
                            if (delta.sqrMagnitude > 0.001f)
                            {
                                float sx = s.MouseCameraSensitivityX.Value * 0.10f * (s.InvertMouseCameraX.Value ? -1f : 1f);
                                float sy = s.MouseCameraSensitivityY.Value * 0.10f * (s.InvertMouseCameraY.Value ? -1f : 1f);
                                extra.x += Mathf.Clamp(delta.x * sx, -1.5f, 1.5f);
                                extra.y += Mathf.Clamp(delta.y * sy, -1.5f, 1.5f);
                            }
                        }
                    }

                    // 2) Comfortable field camera rotation keys (default Q / E)
                    if (KeyBindingSystem.CheckToken(s.FieldCameraLeftKey.Value, InputCheckMode.Hold, allowMouseLeft: false))
                        extra.x -= 1.0f;
                    if (KeyBindingSystem.CheckToken(s.FieldCameraRightKey.Value, InputCheckMode.Hold, allowMouseLeft: false))
                        extra.x += 1.0f;

                    if (extra.sqrMagnitude > 0.0001f)
                    {
                        __result = new Vector2(
                            Mathf.Clamp(__result.x + extra.x, -1.5f, 1.5f),
                            Mathf.Clamp(__result.y + extra.y, -1.5f, 1.5f));
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[CameraPatches.RightMoveValue] " + ex.Message);
                }
            }
        }

        [HarmonyPatch(typeof(MapWindow), "Update")]
        private static class Patch_MapWindow_Update
        {
            [HarmonyPrefix]
            private static void Prefix(MapWindow __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || !__instance._isOpen)
                        return;

                    _lastMapActiveFrame = Time.frameCount;
                    CursorManager.MarkUIActiveThisFrame();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckMapScroll))]
        private static class Patch_CheckMapScroll
        {
            [HarmonyPostfix]
            private static void Postfix(ref Vector3 axis, ref bool __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (!IsMapOpenRecently) return;

                    Vector3 extra = Vector3.zero;

                    // Drag map with MouseLeft
                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.isPressed)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        if (delta.sqrMagnitude > 0.25f)
                        {
                            extra.x -= Mathf.Clamp(delta.x * 0.12f, -2f, 2f);
                            extra.y -= Mathf.Clamp(delta.y * 0.12f, -2f, 2f);
                        }
                    }

                    // Also allow WASD panning in map (vanilla only used Arrows for Scroll)
                    var kb = Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.aKey.isPressed) extra.x -= 1f;
                        if (kb.dKey.isPressed) extra.x += 1f;
                        if (kb.sKey.isPressed) extra.y -= 1f;
                        if (kb.wKey.isPressed) extra.y += 1f;
                    }

                    if (extra.sqrMagnitude > 0.001f)
                    {
                        axis += extra;
                        __result = true;
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckMapZoom))]
        private static class Patch_CheckMapZoom
        {
            [HarmonyPostfix]
            private static void Postfix(ref Vector3 axis, ref bool __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (!IsMapOpenRecently) return;

                    var kb = Keyboard.current;
                    if (kb == null) return;

                    float z = 0f;
                    if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed) z += 1f;
                    if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed) z -= 1f;

                    if (Mathf.Abs(z) > 0.01f)
                    {
                        axis.y += z;
                        __result = true;
                    }
                }
                catch { }
            }
        }
    }
}
