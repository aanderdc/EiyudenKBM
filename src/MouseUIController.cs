using System;
using System.Collections.Generic;
using FieldStage.UI;
using HarmonyLib;
using UI.Title;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// Mouse UI raycast/hit-testing controller for Eiyuden Chronicle's custom uGUI menus:
    /// - Title Screen (UI.Title.SelectState / TitleCanvas)
    /// - DialogSelector (Pause/SystemMenu, OptionMenu root, Confirmations, Yes/No)
    /// - OptionSecondaryMenu (Options toggles, selectors, sliders)
    /// - OptionMenu integration: injects "Controles (Teclado y Ratón)" directly into Settings
    /// - ScrollList (Save/Load, Inventory, Shops, WorldMap, Help, etc.)
    /// - MainMenu carousel arrows
    /// </summary>
    internal static class MouseUIController
    {
        private static int _allowMouseConfirmFrame = -1;
        private static Vector2 _lastHoverMousePos;
        private static int _lastHoverCheckFrame = -1;
        private static bool _mouseMovedThisFrame;

        // Track OptionMenu instances where we injected the KBM Controls button
        private static readonly HashSet<int> InjectedOptionMenus = new();

        public static bool ShouldAllowMouseClickConfirmThisFrame()
        {
            return _allowMouseConfirmFrame == Time.frameCount;
        }

        public static void AllowMouseConfirmThisFrame()
        {
            _allowMouseConfirmFrame = Time.frameCount;
        }

        public static void UpdateMouseMovementCache()
        {
            if (_lastHoverCheckFrame == Time.frameCount) return;
            _lastHoverCheckFrame = Time.frameCount;

            Vector2 pos = KeyBindingSystem.GetMousePosition();
            _mouseMovedThisFrame = (pos - _lastHoverMousePos).sqrMagnitude > 0.5f;
            if (_mouseMovedThisFrame)
                _lastHoverMousePos = pos;
        }

        private static bool IsLeftClickPressed()
        {
            var mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        public static bool HitTestComponent(Component comp, Vector2 screenPos, out RectTransform hitRect)
        {
            hitRect = null;
            if (comp == null) return false;
            var go = comp.gameObject;
            if (go == null || !go.activeInHierarchy) return false;

            var rt = comp.transform.TryCast<RectTransform>();
            if (rt == null) return false;

            Camera cam = ResolveCanvasCamera(rt);
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam))
            {
                hitRect = rt;
                return true;
            }
            return false;
        }

        public static Camera ResolveCanvasCamera(Transform t)
        {
            try
            {
                var canvas = t.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                    if (root.renderMode == RenderMode.ScreenSpaceOverlay)
                        return null;
                    if (root.worldCamera != null)
                        return root.worldCamera;
                }
                if (UIManager.Instance != null && UIManager.UICamera != null)
                    return UIManager.UICamera;
            }
            catch { }
            return null;
        }

        // =========================================================================
        // 1. TITLE SCREEN (UI.Title.SelectState)
        // =========================================================================
        [HarmonyPatch(typeof(SelectState), "PhaseUpdate")]
        private static class Patch_TitleSelectState
        {
            [HarmonyPrefix]
            private static void Prefix(SelectState __instance, IContext context)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    CursorManager.MarkUIActiveThisFrame();
                    UpdateMouseMovementCache();

                    if (__instance == null || context == null) return;

                    var phase = __instance._phase.Active;

                    // "Press Any Button" phase: clicking anywhere triggers UIAnyButton
                    if (phase == SelectState.PhaseType.FirstMenu)
                    {
                        if (IsLeftClickPressed())
                            AllowMouseConfirmThisFrame();
                        return;
                    }

                    if (phase != SelectState.PhaseType.Select)
                        return;

                    var titleCanvas = context.UITitle?.TryCast<TitleCanvas>();
                    if (titleCanvas == null) return;

                    var infos = titleCanvas.ContentInfos;
                    if (infos == null || infos.Length == 0) return;

                    bool clicked = IsLeftClickPressed();
                    if (!_mouseMovedThisFrame && !clicked) return;

                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();
                    int current = __instance._indexSequencer.Current;

                    for (int i = 0; i < infos.Length; i++)
                    {
                        var info = infos[i];
                        if (info == null || !info.IsActive || !info.IsEnable) continue;
                        var btn = info._contentButton;
                        if (btn == null) continue;

                        if (HitTestComponent(btn, mousePos, out _))
                        {
                            if (i != current)
                            {
                                if (current >= 0 && current < infos.Length && infos[current] != null)
                                    infos[current].Select(false);

                                var seq = __instance._indexSequencer;
                                seq.Reset(i, current);
                                __instance._indexSequencer = seq;

                                info.Select(true);
                                InputManager.PlayInputSE("system", "SE_sys_0002");
                            }

                            if (clicked)
                                AllowMouseConfirmThisFrame();

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[MouseUI.Title] " + ex.Message);
                }
            }
        }

        // =========================================================================
        // 2. DIALOG SELECTOR (SystemMenu / Pause, OptionMenu root, Confirmations)
        // =========================================================================
        [HarmonyPatch(typeof(DialogSelector), "Update")]
        private static class Patch_DialogSelector
        {
            [HarmonyPrefix]
            private static void Prefix(DialogSelector __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || !__instance.IsActive)
                        return;

                    CursorManager.MarkUIActiveThisFrame();
                    UpdateMouseMovementCache();

                    bool clicked = IsLeftClickPressed();
                    if (!_mouseMovedThisFrame && !clicked) return;

                    var buttons = __instance._buttonList;
                    if (buttons == null || buttons.Count == 0) return;

                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();
                    int current = __instance._cursor;

                    for (int i = 0; i < buttons.Count; i++)
                    {
                        var btn = buttons[i];
                        if (btn == null || !btn.gameObject.activeInHierarchy) continue;

                        if (HitTestComponent(btn, mousePos, out _))
                        {
                            if (i != current)
                            {
                                __instance.SetCursor(i);
                                InputManager.PlayInputSE("system", "SE_sys_0002");
                            }

                            if (clicked)
                                AllowMouseConfirmThisFrame();

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[MouseUI.DialogSelector] " + ex.Message);
                }
            }
        }

        // =========================================================================
        // 3. OPTION MENU — INJECT "Controles (Teclado y Ratón)" IN SETTINGS!
        // =========================================================================
        [HarmonyPatch(typeof(OptionMenu), "BeginRoot")]
        private static class Patch_OptionMenu_BeginRoot
        {
            [HarmonyPostfix]
            private static void Postfix(OptionMenu __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || __instance == null)
                        return;

                    CursorManager.MarkUIActiveThisFrame();

                    int id = __instance.GetInstanceID();
                    var selector = __instance._rootSelector;
                    var list = __instance._activeMenuItemTypeList;
                    if (selector == null || list == null) return;

                    // Inject button once per OptionMenu instance (at index == list.Count)
                    if (!InjectedOptionMenus.Contains(id))
                    {
                        var buttons = selector._buttonList;
                        if (buttons != null && buttons.Count == list.Count)
                        {
                            selector.AddButton(Loc.OptionsMenuButtonText, null, "system", "SE_sys_0000");
                            InjectedOptionMenus.Add(id);
                            Plugin.Logger?.LogInfo("[OptionMenu] Added KBM settings button: '" + Loc.OptionsMenuButtonText + "' (index=" + list.Count + ").");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning("[OptionMenu.BeginRoot] " + ex.Message);
                }
            }
        }

        [HarmonyPatch(typeof(OptionMenu), "OnDisable")]
        private static class Patch_OptionMenu_OnDisable
        {
            [HarmonyPostfix]
            private static void Postfix(OptionMenu __instance)
            {
                if (__instance != null)
                    InjectedOptionMenus.Remove(__instance.GetInstanceID());
            }
        }

        [HarmonyPatch(typeof(OptionMenu), "OnMovedRoot")]
        private static class Patch_OptionMenu_OnMovedRoot
        {
            [HarmonyPrefix]
            private static bool Prefix(OptionMenu __instance, int index)
            {
                try
                {
                    if (__instance == null || __instance._activeMenuItemTypeList == null)
                        return true;

                    // Prevent ArgumentOutOfRangeException when hovering/selecting our injected button
                    if (index >= __instance._activeMenuItemTypeList.Count)
                    {
                        __instance._rootLastIndex = index;
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

        [HarmonyPatch(typeof(OptionMenu), "UpdateRoot")]
        private static class Patch_OptionMenu_UpdateRoot
        {
            [HarmonyPrefix]
            private static bool Prefix(OptionMenu __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || __instance == null)
                        return true;

                    CursorManager.MarkUIActiveThisFrame();

                    if (SettingsRebindUI.IsOpen)
                        return false;

                    var selector = __instance._rootSelector;
                    var list = __instance._activeMenuItemTypeList;
                    if (selector == null || list == null) return true;

                    int customIndex = list.Count;
                    if (selector.IsDecided && selector.Index == customIndex)
                    {
                        selector.CancelDecided();
                        SettingsRebindUI.Open(() =>
                        {
                            try
                            {
                                if (selector != null && selector.gameObject != null && selector.gameObject.activeInHierarchy)
                                    selector.SetCursor(customIndex);
                            }
                            catch { }
                        });
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning("[OptionMenu.UpdateRoot] " + ex.Message);
                    return true;
                }
            }
        }

        // =========================================================================
        // 4. OPTION SECONDARY MENU (Toggles, Selectors, Sliders)
        // =========================================================================
        [HarmonyPatch(typeof(OptionSecondaryMenu), nameof(OptionSecondaryMenu.OnUpdate))]
        private static class Patch_OptionSecondaryMenu
        {
            [HarmonyPrefix]
            private static void Prefix(OptionSecondaryMenu __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || __instance.IsLock)
                        return;

                    CursorManager.MarkUIActiveThisFrame();
                    UpdateMouseMovementCache();

                    bool clicked = IsLeftClickPressed();
                    if (!_mouseMovedThisFrame && !clicked) return;

                    var items = __instance._itemList;
                    if (items == null || items.Count == 0) return;

                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();
                    int current = __instance._selectedIndex;

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item == null || !item.gameObject.activeInHierarchy) continue;

                        if (HitTestComponent(item, mousePos, out var rect))
                        {
                            if (i != current)
                            {
                                if (current >= 0 && current < items.Count && items[current] != null)
                                    items[current].SetActive(false);

                                __instance._selectedIndex = i;
                                item.SetActive(true);
                                InputManager.PlayInputSE("system", "SE_sys_0002");
                            }

                            if (clicked)
                            {
                                // Determine left vs right side of the option row to decrement/increment values cleanly
                                Camera cam = ResolveCanvasCamera(rect);
                                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mousePos, cam, out Vector2 localPt))
                                {
                                    float normalizedX = rect.rect.width > 1f
                                        ? (localPt.x - rect.rect.xMin) / rect.rect.width
                                        : 0.5f;

                                    if (item.Type == OptionSecondaryMenuItem.SecondaryMenuItemType.Toggle)
                                    {
                                        __instance.ChangeValue(1, false, false);
                                    }
                                    else if (item.Type == OptionSecondaryMenuItem.SecondaryMenuItemType.Select
                                             || item.Type == OptionSecondaryMenuItem.SecondaryMenuItemType.Slider)
                                    {
                                        // Right half = next/increase, left-center = previous/decrease
                                        int delta = normalizedX >= 0.65f ? 1 : -1;
                                        __instance.ChangeValue(delta, false, false);
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[MouseUI.OptionSecondary] " + ex.Message);
                }
            }
        }

        // =========================================================================
        // 5. SCROLL LIST (Save/Load, Inventory, Shops, WorldMap, Help, etc.)
        // =========================================================================
        [HarmonyPatch(typeof(ScrollList), "OnUpdate")]
        private static class Patch_ScrollList
        {
            [HarmonyPrefix]
            private static void Prefix(ScrollList __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || !__instance.HasItem || __instance._isCursorMoving || UIManager.HasDialog())
                        return;

                    CursorManager.MarkUIActiveThisFrame();
                    UpdateMouseMovementCache();

                    bool clicked = IsLeftClickPressed();
                    if (!_mouseMovedThisFrame && !clicked) return;

                    var items = __instance._itemList;
                    if (items == null || items.Count == 0) return;

                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();

                    // Ensure mouse is inside ScrollRect viewport (if ScrollRect exists) so off-screen masked rows aren't hit
                    var sr = __instance.ScrollRect;
                    if (sr != null && sr.viewport != null)
                    {
                        Camera vpCam = ResolveCanvasCamera(sr.viewport);
                        if (!RectTransformUtility.RectangleContainsScreenPoint(sr.viewport, mousePos, vpCam))
                            return;
                    }

                    int current = __instance._selectIndex;

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item == null || !item.gameObject.activeInHierarchy) continue;

                        if (HitTestComponent(item, mousePos, out _))
                        {
                            if (i != current)
                            {
                                __instance.SelectItem(i);
                                InputManager.PlayInputSE("system", "SE_sys_0002");
                            }

                            if (clicked)
                                AllowMouseConfirmThisFrame();

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[MouseUI.ScrollList] " + ex.Message);
                }
            }
        }

        // =========================================================================
        // 6. MAIN MENU CAROUSEL (Click left/right header to switch sections)
        // =========================================================================
        [HarmonyPatch(typeof(MainMenu), "Update")]
        private static class Patch_MainMenu
        {
            [HarmonyPrefix]
            private static void Prefix(MainMenu __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || SettingsRebindUI.IsOpen)
                        return;

                    if (__instance == null || !__instance._isOpen)
                        return;

                    CursorManager.MarkUIActiveThisFrame();

                    if (!IsLeftClickPressed()) return;

                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();
                    if (__instance._leftMenu?._text != null && HitTestComponent(__instance._leftMenu._text, mousePos, out _))
                    {
                        InputPatches.RequestSyntheticAction(InputActionMode.UISelectL2);
                    }
                    else if (__instance._rightMenu?._text != null && HitTestComponent(__instance._rightMenu._text, mousePos, out _))
                    {
                        InputPatches.RequestSyntheticAction(InputActionMode.UISelectR2);
                    }
                }
                catch { }
            }
        }
    }
}
