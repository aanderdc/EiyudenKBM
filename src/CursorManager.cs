using System;
using UnityEngine;

namespace EiyudenKBM
{
    /// <summary>
    /// Ensures the mouse cursor is ALWAYS visible and unlocked in menus, title, dialogs, battle, and rebind UI,
    /// and follows FieldCursorMode during exploration without ever getting trapped or lost on alt-tab/scene load.
    /// </summary>
    internal static class CursorManager
    {
        private static float _lastMouseMoveTime;
        private static Vector2 _lastMousePos;
        private static int _lastUiActiveFrame = -100;

        /// <summary>
        /// Called by active UI panels (Title, DialogSelector, ScrollList, OptionMenu, Battle, Dialog, etc.)
        /// to notify that an interactive UI is currently on screen.
        /// </summary>
        public static void MarkUIActiveThisFrame()
        {
            _lastUiActiveFrame = Time.frameCount;
        }

        public static bool IsUIActiveRecently => (Time.frameCount - _lastUiActiveFrame) <= 3 || SettingsRebindUI.IsOpen;

        public static void Update()
        {
            try
            {
                if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                    return;

                // Never trap cursor
                if (Cursor.lockState != CursorLockMode.None)
                    Cursor.lockState = CursorLockMode.None;

                Vector2 pos = KeyBindingSystem.GetMousePosition();
                if ((pos - _lastMousePos).sqrMagnitude > 1.0f)
                {
                    _lastMousePos = pos;
                    _lastMouseMoveTime = Time.unscaledTime;
                }

                // Always visible in UI, title, menus, battle, dialogs, or when our Settings Rebind UI is open
                if (IsUIActiveRecently)
                {
                    if (!Cursor.visible) Cursor.visible = true;
                    return;
                }

                // In exploration (field): respect configured cursor behavior
                switch (Plugin.Settings.FieldCursorMode.Value)
                {
                    case FieldCursorBehavior.AlwaysVisible:
                        if (!Cursor.visible) Cursor.visible = true;
                        break;

                    case FieldCursorBehavior.AlwaysHiddenInField:
                        if (Cursor.visible) Cursor.visible = false;
                        break;

                    case FieldCursorBehavior.AutoHideInField:
                    default:
                        float elapsed = Time.unscaledTime - _lastMouseMoveTime;
                        bool shouldShow = elapsed < Math.Max(0.3f, Plugin.Settings.AutoHideDelaySeconds.Value);
                        if (Cursor.visible != shouldShow)
                            Cursor.visible = shouldShow;
                        break;
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Settings?.VerboseLogging.Value == true)
                    Plugin.Logger?.LogWarning("[CursorManager] " + ex.Message);
            }
        }

        public static void OnFocusRestored()
        {
            try
            {
                Cursor.lockState = CursorLockMode.None;
                _lastMouseMoveTime = Time.unscaledTime;
                if (IsUIActiveRecently) Cursor.visible = true;
            }
            catch { }
        }
    }
}
