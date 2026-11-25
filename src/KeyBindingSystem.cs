using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace EiyudenKBM
{
    /// <summary>
    /// Unified Keyboard + Mouse button reader using Unity's new InputSystem directly.
    /// Supports all Keyboard keys and all 5 standard Mouse buttons:
    /// MouseLeft, MouseRight, MouseMiddle, Mouse4 (Back / XButton1), Mouse5 (Forward / XButton2).
    /// Zero OS key simulation.
    /// </summary>
    internal static class KeyBindingSystem
    {
        public static readonly string[] MouseTokens =
        {
            "MouseLeft",
            "MouseRight",
            "MouseMiddle",
            "Mouse4",
            "Mouse5"
        };

        private static readonly Dictionary<string, Key> KeyLookup = BuildKeyLookup();

        private static Dictionary<string, Key> BuildKeyLookup()
        {
            var dict = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                if (k == Key.None) continue;
                dict[k.ToString()] = k;
            }
            // Common friendly aliases
            dict["Esc"] = Key.Escape;
            dict["Return"] = Key.Enter;
            dict["LShift"] = Key.LeftShift;
            dict["RShift"] = Key.RightShift;
            dict["Shift"] = Key.LeftShift;
            dict["LCtrl"] = Key.LeftCtrl;
            dict["RCtrl"] = Key.RightCtrl;
            dict["Ctrl"] = Key.LeftCtrl;
            dict["LAlt"] = Key.LeftAlt;
            dict["RAlt"] = Key.RightAlt;
            dict["Alt"] = Key.LeftAlt;
            dict["PgUp"] = Key.PageUp;
            dict["PgDn"] = Key.PageDown;
            dict["Del"] = Key.Delete;
            dict["Ins"] = Key.Insert;
            dict["0"] = Key.Digit0;
            dict["1"] = Key.Digit1;
            dict["2"] = Key.Digit2;
            dict["3"] = Key.Digit3;
            dict["4"] = Key.Digit4;
            dict["5"] = Key.Digit5;
            dict["6"] = Key.Digit6;
            dict["7"] = Key.Digit7;
            dict["8"] = Key.Digit8;
            dict["9"] = Key.Digit9;
            return dict;
        }

        public static bool IsMouseToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            string t = token.Trim();
            return t.Equals("MouseLeft", StringComparison.OrdinalIgnoreCase)
                || t.Equals("LMB", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Mouse0", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MouseRight", StringComparison.OrdinalIgnoreCase)
                || t.Equals("RMB", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Mouse1", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MouseMiddle", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MMB", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Mouse2", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Mouse4", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MouseBack", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MB4", StringComparison.OrdinalIgnoreCase)
                || t.Equals("Mouse5", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MouseForward", StringComparison.OrdinalIgnoreCase)
                || t.Equals("MB5", StringComparison.OrdinalIgnoreCase);
        }

        private static ButtonControl GetMouseButton(Mouse mouse, string token)
        {
            if (mouse == null || string.IsNullOrWhiteSpace(token)) return null;
            string t = token.Trim();
            if (t.Equals("MouseLeft", StringComparison.OrdinalIgnoreCase) || t.Equals("LMB", StringComparison.OrdinalIgnoreCase) || t.Equals("Mouse0", StringComparison.OrdinalIgnoreCase))
                return mouse.leftButton;
            if (t.Equals("MouseRight", StringComparison.OrdinalIgnoreCase) || t.Equals("RMB", StringComparison.OrdinalIgnoreCase) || t.Equals("Mouse1", StringComparison.OrdinalIgnoreCase))
                return mouse.rightButton;
            if (t.Equals("MouseMiddle", StringComparison.OrdinalIgnoreCase) || t.Equals("MMB", StringComparison.OrdinalIgnoreCase) || t.Equals("Mouse2", StringComparison.OrdinalIgnoreCase))
                return mouse.middleButton;
            if (t.Equals("Mouse4", StringComparison.OrdinalIgnoreCase) || t.Equals("MouseBack", StringComparison.OrdinalIgnoreCase) || t.Equals("MB4", StringComparison.OrdinalIgnoreCase))
                return mouse.backButton;
            if (t.Equals("Mouse5", StringComparison.OrdinalIgnoreCase) || t.Equals("MouseForward", StringComparison.OrdinalIgnoreCase) || t.Equals("MB5", StringComparison.OrdinalIgnoreCase))
                return mouse.forwardButton;
            return null;
        }

        public static bool TryGetKeyControl(Keyboard kb, string token, out KeyControl control)
        {
            control = null;
            if (kb == null || string.IsNullOrWhiteSpace(token)) return false;
            if (KeyLookup.TryGetValue(token.Trim(), out Key k))
            {
                control = kb[k];
                return control != null;
            }
            return false;
        }

        public static bool CheckToken(string token, InputCheckMode checkMode, bool allowMouseLeft = true)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            string t = token.Trim();
            if (t.Equals("None", StringComparison.OrdinalIgnoreCase)) return false;

            if (IsMouseToken(t))
            {
                if (!allowMouseLeft && (t.Equals("MouseLeft", StringComparison.OrdinalIgnoreCase) || t.Equals("LMB", StringComparison.OrdinalIgnoreCase) || t.Equals("Mouse0", StringComparison.OrdinalIgnoreCase)))
                    return false;

                var mouse = Mouse.current;
                var btn = GetMouseButton(mouse, t);
                if (btn == null) return false;
                return EvalButton(btn, checkMode);
            }

            var kb = Keyboard.current;
            if (TryGetKeyControl(kb, t, out var keyCtrl))
            {
                return EvalButton(keyCtrl, checkMode);
            }
            return false;
        }

        public static bool CheckTokenList(IReadOnlyList<string> tokens, InputCheckMode checkMode, bool allowMouseLeft = true)
        {
            if (tokens == null) return false;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (CheckToken(tokens[i], checkMode, allowMouseLeft))
                    return true;
            }
            return false;
        }

        private static bool EvalButton(ButtonControl btn, InputCheckMode checkMode)
        {
            return checkMode switch
            {
                InputCheckMode.Trigger => btn.wasPressedThisFrame,
                InputCheckMode.Hold => btn.isPressed,
                InputCheckMode.Release => btn.wasReleasedThisFrame,
                _ => btn.wasPressedThisFrame
            };
        }

        public static bool WasPressedThisFrame(string token) => CheckToken(token, InputCheckMode.Trigger, true);
        public static bool IsPressed(string token) => CheckToken(token, InputCheckMode.Hold, true);

        public static bool IsAnyShiftPressed()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
        }

        public static float GetMouseScrollY()
        {
            var mouse = Mouse.current;
            if (mouse == null) return 0f;
            return mouse.scroll.ReadValue().y;
        }

        public static Vector2 GetMousePosition()
        {
            var mouse = Mouse.current;
            if (mouse != null) return mouse.position.ReadValue();
            return Vector2.zero;
        }

        public static Vector2 GetMouseDelta()
        {
            var mouse = Mouse.current;
            if (mouse != null) return mouse.delta.ReadValue();
            return Vector2.zero;
        }

        public static bool AnyKeyboardOrMouseActivity()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame)
                return true;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame
                    || mouse.backButton.wasPressedThisFrame
                    || mouse.forwardButton.wasPressedThisFrame)
                    return true;

                if (mouse.delta.ReadValue().sqrMagnitude > 4.0f)
                    return true;

                if (Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Captures the first keyboard key or mouse button (including Mouse4 / Mouse5) pressed this frame.
        /// Used by the interactive Settings Rebind UI.
        /// </summary>
        public static bool TryCapturePressedInput(out string token, bool allowMouseButtons = true, bool allowMouseLeft = true)
        {
            token = null;

            if (allowMouseButtons)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    if (mouse.backButton.wasPressedThisFrame) { token = "Mouse4"; return true; }
                    if (mouse.forwardButton.wasPressedThisFrame) { token = "Mouse5"; return true; }
                    if (mouse.middleButton.wasPressedThisFrame) { token = "MouseMiddle"; return true; }
                    if (mouse.rightButton.wasPressedThisFrame) { token = "MouseRight"; return true; }
                    if (allowMouseLeft && mouse.leftButton.wasPressedThisFrame) { token = "MouseLeft"; return true; }
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                foreach (Key k in Enum.GetValues(typeof(Key)))
                {
                    if (k == Key.None || k == Key.IMESelected) continue;
                    try
                    {
                        var ctrl = kb[k];
                        if (ctrl != null && ctrl.wasPressedThisFrame)
                        {
                            token = k.ToString();
                            return true;
                        }
                    }
                    catch { }
                }
            }

            return false;
        }

        public static string GetDisplayLabel(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "-";
            string t = token.Trim();
            return t.ToLowerInvariant() switch
            {
                "mouseleft" or "lmb" or "mouse0" => "Clic Izq (LMB)",
                "mouseright" or "rmb" or "mouse1" => "Clic Der (RMB)",
                "mousemiddle" or "mmb" or "mouse2" => "Clic Central (MMB)",
                "mouse4" or "mouseback" or "mb4" => "Ratón 4 (Atrás)",
                "mouse5" or "mouseforward" or "mb5" => "Ratón 5 (Adelante)",
                "space" => "Espacio",
                "escape" or "esc" => "Esc",
                "leftshift" or "lshift" => "Shift Izq",
                "rightshift" or "rshift" => "Shift Der",
                "leftctrl" or "lctrl" => "Ctrl Izq",
                "rightctrl" or "rctrl" => "Ctrl Der",
                "leftalt" or "lalt" => "Alt Izq",
                "rightalt" or "ralt" => "Alt Der",
                "backspace" => "Retroceso",
                "pageup" => "RePág",
                "pagedown" => "AvPág",
                _ => t.StartsWith("Digit", StringComparison.OrdinalIgnoreCase) && t.Length == 6 ? t.Substring(5) : t
            };
        }

        public static string FormatBindingList(string raw)
        {
            var tokens = ModConfig.ParseTokens(raw);
            if (tokens.Count == 0) return "(Sin asignar)";
            var labels = new List<string>(tokens.Count);
            for (int i = 0; i < tokens.Count; i++)
                labels.Add(GetDisplayLabel(tokens[i]));
            return string.Join(" / ", labels);
        }
    }
}
