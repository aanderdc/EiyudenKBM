using System;
using Battle;
using Battle.Command;
using Battle.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// Full Turn-Based Battle Mouse + Number Shortcuts (1-6):
    /// - Tracks the active UICommandSelect instance via OnEnable/OnDisable (non-generic, 100% IL2CPP-safe).
    /// - Hooks Battle.UI.InputUtility.Poll() (non-generic static method!) to translate mouse hover/click
    ///   and 1-6 shortcuts into native Battle.UI.InputPollResult objects, preserving 100% of game animations,
    ///   sound effects, disabled-command checks, and target marker logic.
    /// </summary>
    internal static class BattlePatches
    {
        private static UICommandSelect _activeCommandSelect;
        private static int _queuedSubmitFrame = -1;

        [HarmonyPatch(typeof(UICommandSelect), "OnEnable")]
        private static class Patch_UICommandSelect_OnEnable
        {
            [HarmonyPostfix]
            private static void Postfix(UICommandSelect __instance)
            {
                _activeCommandSelect = __instance;
            }
        }

        [HarmonyPatch(typeof(UICommandSelect), "OnDisable")]
        private static class Patch_UICommandSelect_OnDisable
        {
            [HarmonyPostfix]
            private static void Postfix(UICommandSelect __instance)
            {
                if (_activeCommandSelect == __instance)
                    _activeCommandSelect = null;
            }
        }

        [HarmonyPatch(typeof(InputUtility), nameof(InputUtility.Poll))]
        private static class Patch_BattleInputUtility_Poll
        {
            [HarmonyPrefix]
            private static bool Prefix(ref InputPollResult __result)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value)
                        return true;

                    if (SettingsRebindUI.IsOpen || SettingsRebindUI.ClosedThisFrame)
                    {
                        __result = default;
                        return false;
                    }

                    CursorManager.MarkUIActiveThisFrame();
                    MouseUIController.UpdateMouseMovementCache();

                    if (_activeCommandSelect == null || !_activeCommandSelect.IsOpen || !_activeCommandSelect.Interactable)
                        return true;

                    // Queued submit after a click or number-key moved focus on the previous frame
                    if (_queuedSubmitFrame == Time.frameCount)
                    {
                        _queuedSubmitFrame = -1;
                        __result = new InputPollResult(InputPollResultType.Submit, InputManager.GetUIDecideDelayedSE());
                        return false;
                    }

                    var topHandler = _activeCommandSelect.TopHandler;
                    if (topHandler == null) return true;

                    var mouse = Mouse.current;
                    bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
                    Vector2 mousePos = KeyBindingSystem.GetMousePosition();

                    // 1) Main Command Select (Fight / Auto / Escape / Support)
                    var mainCmd = topHandler.TryCast<UIMainCommandSelect>();
                    if (mainCmd != null)
                    {
                        if (TryHandleSelectableArray(mainCmd._items, mainCmd._indexSequencer.Current, mousePos, clicked, out __result))
                            return false;
                        return true;
                    }

                    // 2) Unit Command Select / Skill / Linked (Hero Combo) / Item / Situation Panels (all inherit UICommandPanaelBase!)
                    var panel = topHandler.TryCast<UICommandPanaelBase>();
                    if (panel != null)
                    {
                        if (TryHandlePoolList(panel._items, panel._indexSequencer.Current, mousePos, clicked, out __result))
                            return false;
                        return true;
                    }

                    // 3) Battle Confirm Window (Yes / No)
                    var confirmWin = topHandler.TryCast<UIConfirmWindow>();
                    if (confirmWin != null && confirmWin._items != null)
                    {
                        if (TryHandleConfirmItems(confirmWin._items, confirmWin._indexSequencer.Current, mousePos, clicked, out __result))
                            return false;
                        return true;
                    }

                    // 4) Target Select Presenter (Enemies / Allies)
                    var targetPresenter = topHandler.TryCast<UITargetSelectPresenter>();
                    if (targetPresenter != null)
                    {
                        if (TryHandleTargetSelect(targetPresenter, mousePos, clicked, out __result))
                            return false;
                        return true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (Plugin.Settings?.VerboseLogging.Value == true)
                        Plugin.Logger?.LogWarning("[BattlePatches.Poll] " + ex.Message);
                    return true;
                }
            }
        }

        private static bool TryHandleSelectableArray(
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SelectableItem> items,
            int currentIdx,
            Vector2 mousePos,
            bool clicked,
            out InputPollResult result)
        {
            result = default;
            if (items == null || items.Length == 0) return false;

            // Number shortcuts 1-6
            if (Plugin.Settings.BattleNumberShortcuts.Value)
            {
                int num = DialogPatches.CheckNumberKeyPressed(Math.Min(items.Length, 6));
                if (num >= 0 && num < items.Length && items[num] != null && items[num].gameObject.activeInHierarchy)
                {
                    return ProduceMoveOrSubmit(currentIdx, num, submitAfterMove: true, out result);
                }
            }

            // Mouse hover / click
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || !item.gameObject.activeInHierarchy) continue;

                if (MouseUIController.HitTestComponent(item, mousePos, out _))
                {
                    if (clicked)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: true, out result);

                    if (i != currentIdx)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: false, out result);

                    break;
                }
            }

            return false;
        }

        private static bool TryHandlePoolList(
            IconSelectableItemPoolList pool,
            int currentIdx,
            Vector2 mousePos,
            bool clicked,
            out InputPollResult result)
        {
            result = default;
            if (pool == null) return false;
            int count = pool.Count;
            if (count <= 0) return false;

            // Number shortcuts 1-6
            if (Plugin.Settings.BattleNumberShortcuts.Value)
            {
                int num = DialogPatches.CheckNumberKeyPressed(Math.Min(count, 6));
                if (num >= 0 && num < count)
                {
                    var item = pool[num];
                    if (item != null && item.gameObject.activeInHierarchy)
                        return ProduceMoveOrSubmit(currentIdx, num, submitAfterMove: true, out result);
                }
            }

            // Mouse hover / click
            for (int i = 0; i < count; i++)
            {
                var item = pool[i];
                if (item == null || !item.gameObject.activeInHierarchy) continue;

                if (MouseUIController.HitTestComponent(item, mousePos, out _))
                {
                    if (clicked)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: true, out result);

                    if (i != currentIdx)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: false, out result);

                    break;
                }
            }

            return false;
        }

        private static bool TryHandleConfirmItems(
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UIConfirmWindow.ConfirmItem> items,
            int currentIdx,
            Vector2 mousePos,
            bool clicked,
            out InputPollResult result)
        {
            result = default;
            if (items == null || items.Length == 0) return false;

            for (int i = 0; i < items.Length; i++)
            {
                var ci = items[i];
                if (ci == null || ci.Item == null || !ci.Item.gameObject.activeInHierarchy) continue;

                if (MouseUIController.HitTestComponent(ci.Item, mousePos, out _))
                {
                    if (clicked)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: true, out result);

                    if (i != currentIdx)
                        return ProduceMoveOrSubmit(currentIdx, i, submitAfterMove: false, out result);

                    break;
                }
            }

            return false;
        }

        private static bool TryHandleTargetSelect(
            UITargetSelectPresenter presenter,
            Vector2 mousePos,
            bool clicked,
            out InputPollResult result)
        {
            result = default;

            // Mouse wheel cycles targets smoothly
            float scrollY = KeyBindingSystem.GetMouseScrollY();
            if (Plugin.Settings.MouseWheelScrollLists.Value && Mathf.Abs(scrollY) > 0.01f)
            {
                var order = scrollY > 0f ? InputMoveOrder.Previous : InputMoveOrder.Next;
                var move = new InputMoveResult(order, 1, false);
                result = new InputPollResult(InputPollResultType.Move, InputManager.GetUILeftMoveDelayedSE(), ref move);
                return true;
            }

            // Clicking LeftMouse on any enemy/ally UI item or screen confirms target selection cleanly
            // (and hovering over enemy/ally list items steps through targets when clickable)
            if (clicked)
            {
                result = new InputPollResult(InputPollResultType.Submit, InputManager.GetUIDecideDelayedSE());
                return true;
            }

            return false;
        }

        private static bool ProduceMoveOrSubmit(int currentIdx, int targetIdx, bool submitAfterMove, out InputPollResult result)
        {
            int diff = targetIdx - currentIdx;
            if (diff == 0)
            {
                if (submitAfterMove)
                {
                    result = new InputPollResult(InputPollResultType.Submit, InputManager.GetUIDecideDelayedSE());
                    return true;
                }
                result = default;
                return false;
            }

            if (submitAfterMove)
                _queuedSubmitFrame = Time.frameCount + 1;

            var order = diff > 0 ? InputMoveOrder.Next : InputMoveOrder.Previous;
            var move = new InputMoveResult(order, Math.Abs(diff), false);
            result = new InputPollResult(InputPollResultType.Move, InputManager.GetUILeftMoveDelayedSE(), ref move);
            return true;
        }
    }
}
