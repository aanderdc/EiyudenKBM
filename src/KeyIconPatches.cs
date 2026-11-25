using System;
using System.Collections.Generic;
using FieldStage.UI;
using HarmonyLib;
using UnityEngine;

namespace EiyudenKBM
{
    /// <summary>
    /// Renders crisp, dynamically generated Keycap and Mouse-button prompt sprites (LMB/RMB/MMB/M4/M5 + bound keys)
    /// in UIKeyIcon and KeyHelpUnit whenever InputManager.DeviceType == Keyboard.
    /// Automatically restores native Xbox / PS4 / PS5 sprites when a controller is used.
    /// </summary>
    internal static class KeyIconPatches
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);
        private static int _configRevision = 0;

        public static void NotifyConfigChanged()
        {
            _configRevision++;
        }

        [HarmonyPatch(typeof(UIKeyIcon), "UpdateKeyIcon")]
        private static class Patch_UIKeyIcon_UpdateKeyIcon
        {
            [HarmonyPostfix]
            private static void Postfix(UIKeyIcon __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || __instance == null)
                        return;

                    if (InputManager.DeviceType != InputManager.InputDeviceType.Keyboard)
                        return;

                    if (__instance._keyIcon == null)
                        return;

                    string token = ResolvePrimaryToken(__instance._inputActionMode, __instance._keyIconData?.InputButtonType);
                    if (string.IsNullOrEmpty(token)) return;

                    var sprite = GetOrCreatePromptSprite(token);
                    if (sprite != null)
                        __instance._keyIcon.sprite = sprite;
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(KeyHelpUnit), "UpdateIconImage")]
        private static class Patch_KeyHelpUnit_UpdateIconImage
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHelpUnit __instance)
            {
                try
                {
                    if (Plugin.Settings == null || !Plugin.Settings.Enabled.Value || __instance == null)
                        return;

                    if (InputManager.DeviceType != InputManager.InputDeviceType.Keyboard)
                        return;

                    if (__instance._keyIcon == null)
                        return;

                    string token = ResolvePrimaryToken(__instance._inputActionMode, __instance._iconData?.InputButtonType);
                    if (string.IsNullOrEmpty(token)) return;

                    var sprite = GetOrCreatePromptSprite(token);
                    if (sprite != null)
                        __instance._keyIcon.sprite = sprite;
                }
                catch { }
            }
        }

        // Force live refresh of KeyHelpUnit & UIKeyIcon if config changes while on screen
        [HarmonyPatch(typeof(UIKeyIcon), "CheckChangeIcon")]
        private static class Patch_UIKeyIcon_CheckChangeIcon
        {
            private static readonly Dictionary<int, int> SeenRev = new();

            [HarmonyPostfix]
            private static void Postfix(UIKeyIcon __instance, ref bool __result)
            {
                if (__instance == null) return;
                int id = __instance.GetInstanceID();
                if (!SeenRev.TryGetValue(id, out int rev) || rev != _configRevision)
                {
                    SeenRev[id] = _configRevision;
                    __result = true;
                }
            }
        }

        private static string ResolvePrimaryToken(InputActionMode mode, InputButtonType? fallbackButton)
        {
            var s = Plugin.Settings;
            if (s == null) return null;

            List<string> tokens = mode switch
            {
                InputActionMode.PlayerCheck => s.InteractTokens,
                InputActionMode.PlayerWalk => s.WalkToggleHoldTokens,
                InputActionMode.MenuOpen => s.MainMenuTokens,
                InputActionMode.MapOpen => s.WorldMapTokens,
                InputActionMode.UIDecide or InputActionMode.SystemUIDecide or InputActionMode.WarDecide => s.ConfirmTokens,
                InputActionMode.UICancel or InputActionMode.SystemUICancel or InputActionMode.WarCancel => s.CancelTokens,
                InputActionMode.UIAutoEquip or InputActionMode.WarCorpsDetail => s.AuxActionXTokens,
                InputActionMode.UIStatusChange or InputActionMode.UIMapSwitching or InputActionMode.WarBattleStart or InputActionMode.WarCorpsEdit or InputActionMode.WarCorpsEditComplete => s.DetailsViewYTokens,
                InputActionMode.UISelectL or InputActionMode.WarAngerGauge => s.TabPrevLBTokens,
                InputActionMode.UISelectR or InputActionMode.WarMoraleGauge => s.TabNextRBTokens,
                InputActionMode.UISelectL2 => s.SectionPrevLTTokens,
                InputActionMode.UISelectR2 => s.SectionNextRTTokens,
                InputActionMode.UISelectL3 => s.SortFilterLSTokens,
                InputActionMode.UISelectR3 => s.SecondaryActionRSTokens,
                InputActionMode.UIOption => s.PauseSystemMenuTokens,
                _ => null
            };

            if (tokens != null && tokens.Count > 0)
                return tokens[0];

            if (fallbackButton.HasValue)
            {
                return fallbackButton.Value switch
                {
                    InputButtonType.A or InputButtonType.Sys_A => FirstOrNull(s.ConfirmTokens),
                    InputButtonType.B or InputButtonType.Sys_B => FirstOrNull(s.CancelTokens),
                    InputButtonType.X => FirstOrNull(s.AuxActionXTokens),
                    InputButtonType.Y => FirstOrNull(s.DetailsViewYTokens),
                    InputButtonType.LB => FirstOrNull(s.TabPrevLBTokens),
                    InputButtonType.RB => FirstOrNull(s.TabNextRBTokens),
                    InputButtonType.LT => FirstOrNull(s.SectionPrevLTTokens),
                    InputButtonType.RT => FirstOrNull(s.SectionNextRTTokens),
                    InputButtonType.LS => FirstOrNull(s.SortFilterLSTokens),
                    InputButtonType.RS => FirstOrNull(s.SecondaryActionRSTokens),
                    InputButtonType.Menu => FirstOrNull(s.PauseSystemMenuTokens),
                    _ => null
                };
            }

            return null;
        }

        private static string FirstOrNull(List<string> list) => (list != null && list.Count > 0) ? list[0] : null;

        private static string GetCompactGlyph(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "?";
            string t = token.Trim().ToLowerInvariant();
            return t switch
            {
                "mouseleft" or "lmb" or "mouse0" => "LMB",
                "mouseright" or "rmb" or "mouse1" => "RMB",
                "mousemiddle" or "mmb" or "mouse2" => "MMB",
                "mouse4" or "mouseback" or "mb4" => "M4",
                "mouse5" or "mouseforward" or "mb5" => "M5",
                "space" => "SPC",
                "escape" or "esc" => "ESC",
                "tab" => "TAB",
                "leftshift" or "rightshift" or "shift" => "SHF",
                "leftctrl" or "rightctrl" or "ctrl" => "CTL",
                "leftalt" or "rightalt" or "alt" => "ALT",
                "enter" or "return" => "ENT",
                "backspace" => "BSP",
                "pageup" or "pgup" => "PGU",
                "pagedown" or "pgdn" => "PGD",
                _ => t.StartsWith("digit") && t.Length == 6
                    ? t.Substring(5).ToUpperInvariant()
                    : (t.Length <= 3 ? t.ToUpperInvariant() : t.Substring(0, 3).ToUpperInvariant())
            };
        }

        private static Sprite GetOrCreatePromptSprite(string token)
        {
            string glyph = GetCompactGlyph(token);
            if (SpriteCache.TryGetValue(glyph, out var cached) && cached != null)
                return cached;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;

            var pixels = new Color[size * size];
            Color clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            Color bg = new Color(0.11f, 0.13f, 0.19f, 0.96f);
            Color border = new Color(0.86f, 0.74f, 0.40f, 1.0f);
            Color highlight = new Color(0.95f, 0.72f, 0.22f, 1.0f);
            Color fg = Color.white;

            bool isMouse = glyph is "LMB" or "RMB" or "MMB" or "M4" or "M5";

            if (isMouse)
            {
                // Draw rounded badge + miniature mouse body at top + crisp label at bottom
                DrawRoundedRect(pixels, size, 3, 3, 58, 58, 10, bg, border, 3);
                // Mouse outline in upper half (y: 28..54, x: 20..44)
                DrawRoundedRect(pixels, size, 21, 29, 22, 26, 9, new Color(0.22f, 0.25f, 0.34f, 1f), fg, 2);
                // Highlight active mouse button
                if (glyph == "LMB") FillRect(pixels, size, 23, 43, 8, 10, highlight);
                else if (glyph == "RMB") FillRect(pixels, size, 33, 43, 8, 10, highlight);
                else if (glyph == "MMB") FillRect(pixels, size, 30, 44, 4, 9, highlight);
                else if (glyph == "M4") FillRect(pixels, size, 17, 34, 4, 7, highlight);
                else if (glyph == "M5") FillRect(pixels, size, 17, 43, 4, 7, highlight);
                // Crisp label below mouse icon
                DrawPixelTextCentered(pixels, size, glyph, yBottom: 11, scale: 2, color: fg);
            }
            else
            {
                // Crisp dark-gold keycap
                DrawRoundedRect(pixels, size, 3, 5, 58, 54, 10, bg, border, 3);
                int scale = glyph.Length == 1 ? 4 : (glyph.Length == 2 ? 3 : 2);
                int yBot = (size - 7 * scale) / 2;
                DrawPixelTextCentered(pixels, size, glyph, yBot, scale, fg);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SpriteCache[glyph] = sprite;
            return sprite;
        }

        private static void FillRect(Color[] buf, int w, int rx, int ry, int rw, int rh, Color c)
        {
            for (int y = ry; y < ry + rh; y++)
                for (int x = rx; x < rx + rw; x++)
                    if (x >= 0 && x < w && y >= 0 && y < w)
                        buf[y * w + x] = c;
        }

        private static void DrawRoundedRect(Color[] buf, int w, int rx, int ry, int rw, int rh, int radius, Color fill, Color border, int borderW)
        {
            int x1 = rx + rw - 1;
            int y1 = ry + rh - 1;
            for (int y = ry; y <= y1; y++)
            {
                for (int x = rx; x <= x1; x++)
                {
                    if (x < 0 || x >= w || y < 0 || y >= w) continue;
                    float dx = 0f, dy = 0f;
                    if (x < rx + radius) dx = (rx + radius) - x;
                    else if (x > x1 - radius) dx = x - (x1 - radius);
                    if (y < ry + radius) dy = (ry + radius) - y;
                    else if (y > y1 - radius) dy = y - (y1 - radius);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius) continue;

                    bool isEdge = (x < rx + borderW || x > x1 - borderW || y < ry + borderW || y > y1 - borderW || dist > radius - borderW);
                    buf[y * w + x] = isEdge ? border : fill;
                }
            }
        }

        private static void DrawPixelTextCentered(Color[] buf, int w, string text, int yBottom, int scale, Color color)
        {
            int charW = 5 * scale;
            int gap = scale;
            int totalW = text.Length * charW + (text.Length - 1) * gap;
            int startX = (w - totalW) / 2;

            for (int i = 0; i < text.Length; i++)
            {
                var rows = GetGlyph5x7(text[i]);
                int cx = startX + i * (charW + gap);
                for (int r = 0; r < 7; r++)
                {
                    byte bits = rows[r];
                    int py = yBottom + (6 - r) * scale;
                    for (int c = 0; c < 5; c++)
                    {
                        if ((bits & (1 << (4 - c))) != 0)
                            FillRect(buf, w, cx + c * scale, py, scale, scale, color);
                    }
                }
            }
        }

        private static byte[] GetGlyph5x7(char ch) => char.ToUpperInvariant(ch) switch
        {
            'A' => new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'B' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
            'C' => new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
            'D' => new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C },
            'E' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
            'F' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
            'G' => new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F },
            'H' => new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'I' => new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
            'J' => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x12, 0x12, 0x0C },
            'K' => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            'L' => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
            'M' => new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
            'N' => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            'O' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'P' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
            'Q' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
            'R' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
            'S' => new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
            'T' => new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            'U' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'V' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
            'W' => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
            'X' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
            'Y' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
            'Z' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
            '0' => new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
            '1' => new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
            '2' => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F },
            '3' => new byte[] { 0x1E, 0x01, 0x01, 0x0E, 0x01, 0x01, 0x1E },
            '4' => new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
            '5' => new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
            '6' => new byte[] { 0x0E, 0x10, 0x10, 0x1E, 0x11, 0x11, 0x0E },
            '7' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            '8' => new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
            '9' => new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x01, 0x0E },
            _ => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 }
        };
    }
}
