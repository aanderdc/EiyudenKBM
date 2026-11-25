using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EiyudenKBM
{
    /// <summary>
    /// In-game Settings & Key/Mouse Rebinding Panel:
    /// - Accessible directly from Ajustes / Opciones -> "Controles (Teclado y Ratón)" AND via F8 (configurable).
    /// - Supports capturing ANY Keyboard key AND ALL 5 Mouse buttons (MouseLeft, MouseRight, MouseMiddle, Mouse4, Mouse5).
    /// - Uses explicit Rect-based IMGUI calls (zero GUILayoutOption IL2CPP marshalling quirks) with dark-gold styling.
    /// - Saves live into BepInEx/config/EiyudenKBM.cfg immediately.
    /// </summary>
    internal static class SettingsRebindUI
    {
        public static bool IsOpen { get; private set; }
        public static bool ClosedThisFrame => _closedFrame == Time.frameCount;

        private static int _closedFrame = -10;
        private static Action _onCloseCallback;
        private static Vector2 _scrollPos = Vector2.zero;

        // Interactive key/mouse capture state
        private static ConfigEntry<string> _capturingEntry;
        private static string _capturingLabel;
        private static bool _captureAppendMode;
        private static bool _captureAllowMouseLeft = true;
        private static int _captureStartFrame;
        private static string _statusMessage = "";
        private static float _statusUntilTime;

        // Cached textures & styles (created once on GUI thread)
        private static bool _stylesReady;
        private static Texture2D _texBgDim;
        private static Texture2D _texPanelBg;
        private static Texture2D _texHeaderBg;
        private static Texture2D _texRowBg;
        private static Texture2D _texRowAltBg;
        private static Texture2D _texBtnNormal;
        private static Texture2D _texBtnHover;
        private static Texture2D _texBtnAccent;
        private static Texture2D _texBorder;

        private static GUIStyle _titleStyle;
        private static GUIStyle _sectionStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _btnStyle;
        private static GUIStyle _btnAccentStyle;

        private sealed class BindingRow
        {
            public string Section;
            public string Label;
            public ConfigEntry<string> Entry;
            public bool SingleValueOnly;
        }

        private static List<BindingRow> _rows;

        public static void Open(Action onClose = null)
        {
            _onCloseCallback = onClose;
            _capturingEntry = null;
            IsOpen = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureRows();
            ShowStatus("Haz clic en [Cambiar] o [+Añadir] y pulsa cualquier tecla o botón del ratón (incl. Mouse4 / Mouse5).");
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _capturingEntry = null;
            IsOpen = false;
            _closedFrame = Time.frameCount;
            Plugin.Settings?.SaveAndNotify();
            var cb = _onCloseCallback;
            _onCloseCallback = null;
            try { cb?.Invoke(); } catch { }
        }

        public static void Update()
        {
            if (Plugin.Settings == null) return;

            // Toggle with F8 (configurable RebindMenuKey) when not actively capturing a key
            if (_capturingEntry == null && KeyBindingSystem.CheckTokenList(Plugin.Settings.RebindMenuKeyTokens, InputCheckMode.Trigger, false))
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (!IsOpen) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Handle active key/mouse capture
            if (_capturingEntry != null)
            {
                // Skip the frame we clicked the button so the button click itself isn't captured
                if (Time.frameCount <= _captureStartFrame + 1)
                    return;

                // ESC cancels capture
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    _capturingEntry = null;
                    ShowStatus("Captura cancelada.");
                    return;
                }

                if (KeyBindingSystem.TryCapturePressedInput(out string captured, allowMouseButtons: true, allowMouseLeft: _captureAllowMouseLeft))
                {
                    ApplyCapturedToken(_capturingEntry, captured, _captureAppendMode);
                    ShowStatus($"'{_capturingLabel}' actualizado -> {KeyBindingSystem.FormatBindingList(_capturingEntry.Value)}");
                    _capturingEntry = null;
                }
                return;
            }

            // ESC closes the panel when not capturing
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private static void ApplyCapturedToken(ConfigEntry<string> entry, string token, bool append)
        {
            if (entry == null || string.IsNullOrWhiteSpace(token)) return;
            if (!append)
            {
                entry.Value = token;
            }
            else
            {
                var list = ModConfig.ParseTokens(entry.Value);
                bool exists = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Equals(token, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists) list.Add(token);
                entry.Value = string.Join(", ", list);
            }
            Plugin.Settings.SaveAndNotify();
        }

        private static void ShowStatus(string msg)
        {
            _statusMessage = msg;
            _statusUntilTime = Time.unscaledTime + 6.0f;
        }

        private static void EnsureRows()
        {
            if (_rows != null || Plugin.Settings == null) return;
            var s = Plugin.Settings;
            _rows = new List<BindingRow>
            {
                // Exploración
                new() { Section = "EXPLORACIÓN", Label = "Interactuar / Hablar / Examinar", Entry = s.Interact },
                new() { Section = "EXPLORACIÓN", Label = "Caminar / Correr (mantener)", Entry = s.WalkToggleHold },
                new() { Section = "EXPLORACIÓN", Label = "Menú Principal (Inventario/Equipo)", Entry = s.MainMenu },
                new() { Section = "EXPLORACIÓN", Label = "Menú de Pausa / Sistema", Entry = s.PauseSystemMenu },
                new() { Section = "EXPLORACIÓN", Label = "Abrir Mapa", Entry = s.WorldMap },

                // Cámara
                new() { Section = "CÁMARA (EXPLORACIÓN)", Label = "Botón arrastrar cámara con ratón", Entry = s.MouseDragCameraButton, SingleValueOnly = true },
                new() { Section = "CÁMARA (EXPLORACIÓN)", Label = "Girar cámara izquierda (tecla)", Entry = s.FieldCameraLeftKey, SingleValueOnly = true },
                new() { Section = "CÁMARA (EXPLORACIÓN)", Label = "Girar cámara derecha (tecla)", Entry = s.FieldCameraRightKey, SingleValueOnly = true },

                // Menús y Combate
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Confirmar / Aceptar", Entry = s.Confirm },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Cancelar / Atrás", Entry = s.Cancel },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Pestaña / Personaje anterior (LB)", Entry = s.TabPrevLB },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Pestaña / Personaje siguiente (RB)", Entry = s.TabNextRB },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Sección carrusel anterior (LT)", Entry = s.SectionPrevLT },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Sección carrusel siguiente (RT)", Entry = s.SectionNextRT },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Auto-equipar / Acción X", Entry = s.AuxActionX },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Detalles / Cambiar vista (Y)", Entry = s.DetailsViewY },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Ordenar / Filtrar (L3)", Entry = s.SortFilterLS },
                new() { Section = "MENÚS, LISTAS Y COMBATE", Label = "Acción auxiliar / Máx. (R3)", Entry = s.SecondaryActionRS },

                // Diálogos y panel
                new() { Section = "DIÁLOGOS Y ATALAYA DEL MOD", Label = "Avanzar / Completar texto", Entry = s.DialogAdvance },
                new() { Section = "DIÁLOGOS Y ATALAYA DEL MOD", Label = "Saltar cinemática (mantener)", Entry = s.DialogSkipHold },
                new() { Section = "DIÁLOGOS Y ATALAYA DEL MOD", Label = "Abrir este panel de controles", Entry = s.RebindMenuKey, SingleValueOnly = true },
            };
        }

        public static void OnGUI()
        {
            if (!IsOpen || Plugin.Settings == null) return;

            try
            {
                EnsureStyles();
                EnsureRows();

                float sw = Screen.width;
                float sh = Screen.height;

                // Fullscreen dim background
                GUI.DrawTexture(new Rect(0, 0, sw, sh), _texBgDim);

                float panelW = Mathf.Clamp(sw * 0.78f, 760f, 1140f);
                float panelH = Mathf.Clamp(sh * 0.86f, 540f, 880f);
                float px = (sw - panelW) * 0.5f;
                float py = (sh - panelH) * 0.5f;

                // Border + main panel
                GUI.DrawTexture(new Rect(px - 2, py - 2, panelW + 4, panelH + 4), _texBorder);
                GUI.DrawTexture(new Rect(px, py, panelW, panelH), _texPanelBg);

                // Header bar
                float headerH = 54f;
                GUI.DrawTexture(new Rect(px, py, panelW, headerH), _texHeaderBg);
                GUI.Label(new Rect(px + 20, py + 10, panelW - 220, 34),
                    $"EiyudenKBM v{PluginInfo.PLUGIN_VERSION} — Ajustes de Teclado y Ratón", _titleStyle);

                if (GUI.Button(new Rect(px + panelW - 175, py + 11, 155, 32), "Guardar y Cerrar (ESC)", _btnAccentStyle))
                {
                    Close();
                    return;
                }

                // Footer bar (fixed at bottom)
                float footerH = 54f;
                float footerY = py + panelH - footerH;
                GUI.DrawTexture(new Rect(px, footerY, panelW, footerH), _texHeaderBg);

                string status = Time.unscaledTime < _statusUntilTime
                    ? _statusMessage
                    : "Soporta teclado y ratón: Clic Izq, Clic Der, Clic Central, Ratón 4 (Atrás) y Ratón 5 (Adelante).";
                GUI.Label(new Rect(px + 20, footerY + 14, panelW - 260, 26), status, _hintStyle);

                if (GUI.Button(new Rect(px + panelW - 225, footerY + 11, 205, 32), "Restablecer por defecto", _btnStyle))
                {
                    Plugin.Settings.ResetAllToDefaults();
                    ShowStatus("Todos los controles han sido restablecidos a sus valores por defecto.");
                }

                // Scrollable body
                float bodyX = px + 16f;
                float bodyY = py + headerH + 10f;
                float bodyW = panelW - 32f;
                float bodyH = panelH - headerH - footerH - 20f;

                float contentW = bodyW - 22f;
                float contentH = CalculateContentHeight();

                _scrollPos = GUI.BeginScrollView(
                    new Rect(bodyX, bodyY, bodyW, bodyH),
                    _scrollPos,
                    new Rect(0, 0, contentW, contentH));

                float y = 6f;
                y = DrawGeneralAndCursorSection(contentW, y);
                y += 12f;
                y = DrawBindingRows(contentW, y);

                GUI.EndScrollView();

                // Modal capture overlay
                if (_capturingEntry != null)
                {
                    GUI.DrawTexture(new Rect(0, 0, sw, sh), _texBgDim);
                    float mw = 560f, mh = 185f;
                    float mx = (sw - mw) * 0.5f, my = (sh - mh) * 0.5f;
                    GUI.DrawTexture(new Rect(mx - 2, my - 2, mw + 4, mh + 4), _texBorder);
                    GUI.DrawTexture(new Rect(mx, my, mw, mh), _texPanelBg);

                    GUI.Label(new Rect(mx + 20, my + 20, mw - 40, 32),
                        $"Asignando: {_capturingLabel}", _titleStyle);
                    GUI.Label(new Rect(mx + 20, my + 62, mw - 40, 54),
                        "Pulsa ahora cualquier tecla del teclado o botón del ratón\n" +
                        "(Clic Izq, Clic Der, Clic Central, Botón lateral Mouse4 o Mouse5)...", _valueStyle);
                    GUI.Label(new Rect(mx + 20, my + 132, mw - 40, 28),
                        "Pulsa ESC para cancelar.", _hintStyle);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError("[SettingsRebindUI.OnGUI] " + ex);
            }
        }

        private static float CalculateContentHeight()
        {
            float h = 240f; // General + Cursor + Camera sliders
            string lastSec = null;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Section != lastSec)
                {
                    lastSec = _rows[i].Section;
                    h += 34f;
                }
                h += 36f;
            }
            return h + 30f;
        }

        private static float DrawGeneralAndCursorSection(float w, float y)
        {
            var s = Plugin.Settings;

            GUI.Label(new Rect(8, y, w - 16, 26), "GENERAL, CURSOR Y SENSIBILIDAD", _sectionStyle);
            y += 30f;

            // Row 1: General toggles
            GUI.DrawTexture(new Rect(0, y, w, 34), _texRowBg);
            if (GUI.Button(new Rect(10, y + 4, 210, 26), $"Mod activo: {(s.Enabled.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.Enabled.Value = !s.Enabled.Value;
                s.SaveAndNotify();
            }
            if (GUI.Button(new Rect(230, y + 4, 250, 26), $"Mantener teclas originales: {(s.KeepOriginalBindings.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.KeepOriginalBindings.Value = !s.KeepOriginalBindings.Value;
                s.SaveAndNotify();
            }
            if (GUI.Button(new Rect(490, y + 4, 220, 26), $"Atajos 1-6 combate: {(s.BattleNumberShortcuts.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.BattleNumberShortcuts.Value = !s.BattleNumberShortcuts.Value;
                s.SaveAndNotify();
            }
            y += 38f;

            // Row 2: Wheel toggles
            GUI.DrawTexture(new Rect(0, y, w, 34), _texRowAltBg);
            if (GUI.Button(new Rect(10, y + 4, 260, 26), $"Rueda desplaza listas: {(s.MouseWheelScrollLists.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.MouseWheelScrollLists.Value = !s.MouseWheelScrollLists.Value;
                s.SaveAndNotify();
            }
            if (GUI.Button(new Rect(280, y + 4, 280, 26), $"Shift + Rueda cambia pestañas: {(s.ShiftWheelSwitchTabs.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.ShiftWheelSwitchTabs.Value = !s.ShiftWheelSwitchTabs.Value;
                s.SaveAndNotify();
            }
            y += 38f;

            // Row 3: Cursor mode in exploration
            GUI.DrawTexture(new Rect(0, y, w, 34), _texRowBg);
            GUI.Label(new Rect(12, y + 6, 250, 24), "Cursor en exploración:", _labelStyle);
            string cursorLabel = s.FieldCursorMode.Value switch
            {
                FieldCursorBehavior.AutoHideInField => "Auto-ocultar al caminar (Recomendado)",
                FieldCursorBehavior.AlwaysHiddenInField => "Siempre oculto en exploración",
                _ => "Siempre visible"
            };
            if (GUI.Button(new Rect(265, y + 4, 330, 26), cursorLabel, _btnStyle))
            {
                int next = ((int)s.FieldCursorMode.Value + 1) % 3;
                s.FieldCursorMode.Value = (FieldCursorBehavior)next;
                s.SaveAndNotify();
            }
            y += 38f;

            // Row 4: Mouse camera sensitivity
            GUI.DrawTexture(new Rect(0, y, w, 34), _texRowAltBg);
            GUI.Label(new Rect(12, y + 6, 175, 24), $"Sensibilidad X: {s.MouseCameraSensitivityX.Value:0.00}", _labelStyle);
            float newSx = GUI.HorizontalSlider(new Rect(190, y + 11, 160, 18), s.MouseCameraSensitivityX.Value, 0.2f, 3.0f);
            if (Mathf.Abs(newSx - s.MouseCameraSensitivityX.Value) > 0.01f)
            {
                s.MouseCameraSensitivityX.Value = Mathf.Round(newSx * 20f) / 20f;
                s.SaveAndNotify();
            }

            GUI.Label(new Rect(375, y + 6, 175, 24), $"Sensibilidad Y: {s.MouseCameraSensitivityY.Value:0.00}", _labelStyle);
            float newSy = GUI.HorizontalSlider(new Rect(550, y + 11, 160, 18), s.MouseCameraSensitivityY.Value, 0.2f, 3.0f);
            if (Mathf.Abs(newSy - s.MouseCameraSensitivityY.Value) > 0.01f)
            {
                s.MouseCameraSensitivityY.Value = Mathf.Round(newSy * 20f) / 20f;
                s.SaveAndNotify();
            }
            y += 38f;

            // Row 5: Invert camera
            GUI.DrawTexture(new Rect(0, y, w, 34), _texRowBg);
            if (GUI.Button(new Rect(10, y + 4, 210, 26), $"Invertir cámara X: {(s.InvertMouseCameraX.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.InvertMouseCameraX.Value = !s.InvertMouseCameraX.Value;
                s.SaveAndNotify();
            }
            if (GUI.Button(new Rect(230, y + 4, 210, 26), $"Invertir cámara Y: {(s.InvertMouseCameraY.Value ? "SÍ" : "NO")}", _btnStyle))
            {
                s.InvertMouseCameraY.Value = !s.InvertMouseCameraY.Value;
                s.SaveAndNotify();
            }
            y += 38f;

            return y;
        }

        private static float DrawBindingRows(float w, float y)
        {
            string currentSec = null;
            int rowIdx = 0;

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Section != currentSec)
                {
                    currentSec = row.Section;
                    y += 4f;
                    GUI.Label(new Rect(8, y, w - 16, 26), currentSec, _sectionStyle);
                    y += 30f;
                }

                GUI.DrawTexture(new Rect(0, y, w, 34), (rowIdx++ % 2 == 0) ? _texRowBg : _texRowAltBg);

                float labelW = Mathf.Min(310f, w * 0.36f);
                float btnsW = row.SingleValueOnly ? 175f : 265f;
                float valX = labelW + 14f;
                float valW = w - labelW - btnsW - 28f;

                GUI.Label(new Rect(12, y + 6, labelW, 24), row.Label, _labelStyle);
                GUI.Label(new Rect(valX, y + 6, valW, 24), KeyBindingSystem.FormatBindingList(row.Entry.Value), _valueStyle);

                float bx = w - btnsW - 8f;
                if (GUI.Button(new Rect(bx, y + 4, 86, 26), "Cambiar", _btnStyle))
                {
                    StartCapture(row.Entry, row.Label, append: false, allowMouseLeft: !row.SingleValueOnly || row.Entry == Plugin.Settings.MouseDragCameraButton);
                }
                bx += 92f;

                if (!row.SingleValueOnly)
                {
                    if (GUI.Button(new Rect(bx, y + 4, 84, 26), "+ Añadir", _btnStyle))
                    {
                        StartCapture(row.Entry, row.Label, append: true, allowMouseLeft: true);
                    }
                    bx += 90f;
                }

                if (GUI.Button(new Rect(bx, y + 4, 78, 26), "Quitar", _btnStyle))
                {
                    row.Entry.Value = "None";
                    Plugin.Settings.SaveAndNotify();
                    ShowStatus($"'{row.Label}' dejado sin asignar.");
                }

                y += 36f;
            }

            return y;
        }

        private static void StartCapture(ConfigEntry<string> entry, string label, bool append, bool allowMouseLeft)
        {
            _capturingEntry = entry;
            _capturingLabel = label;
            _captureAppendMode = append;
            _captureAllowMouseLeft = allowMouseLeft;
            _captureStartFrame = Time.frameCount;
        }

        private static void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _texBgDim = MakeTex(new Color(0.02f, 0.03f, 0.06f, 0.78f));
            _texPanelBg = MakeTex(new Color(0.08f, 0.10f, 0.16f, 0.97f));
            _texHeaderBg = MakeTex(new Color(0.12f, 0.15f, 0.24f, 1.0f));
            _texRowBg = MakeTex(new Color(0.11f, 0.13f, 0.20f, 0.90f));
            _texRowAltBg = MakeTex(new Color(0.09f, 0.11f, 0.17f, 0.90f));
            _texBtnNormal = MakeTex(new Color(0.18f, 0.22f, 0.34f, 1.0f));
            _texBtnHover = MakeTex(new Color(0.28f, 0.35f, 0.52f, 1.0f));
            _texBtnAccent = MakeTex(new Color(0.62f, 0.46f, 0.16f, 1.0f));
            _texBorder = MakeTex(new Color(0.76f, 0.62f, 0.30f, 1.0f));

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _titleStyle.normal.textColor = new Color(0.97f, 0.88f, 0.60f);

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _sectionStyle.normal.textColor = new Color(0.92f, 0.78f, 0.42f);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = new Color(0.93f, 0.95f, 0.98f);

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _valueStyle.normal.textColor = new Color(0.58f, 0.88f, 1.0f);

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            _hintStyle.normal.textColor = new Color(0.82f, 0.85f, 0.90f);

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _btnStyle.normal.background = _texBtnNormal;
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.hover.background = _texBtnHover;
            _btnStyle.hover.textColor = Color.white;
            _btnStyle.active.background = _texBtnAccent;
            _btnStyle.active.textColor = Color.white;

            _btnAccentStyle = new GUIStyle(_btnStyle);
            _btnAccentStyle.normal.background = _texBtnAccent;
            _btnAccentStyle.hover.background = _texBorder;
            _btnAccentStyle.normal.textColor = Color.white;
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }
    }
}
