using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace EiyudenKBM
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.personal.eiyuden.kbm";
        public const string PLUGIN_NAME = "EiyudenKBM";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public sealed class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger { get; private set; }
        internal static ModConfig Settings { get; private set; }
        internal static Harmony HarmonyInstance { get; private set; }

        public override void Load()
        {
            Logger = Log;
            try
            {
                Logger.LogInfo($"[{PluginInfo.PLUGIN_NAME}] Iniciando v{PluginInfo.PLUGIN_VERSION} (Soporte completo Teclado + Ratón)...");

                // Clean up legacy GUID-named config file from Slice 1 if present
                try
                {
                    string legacyCfg = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID + ".cfg");
                    if (File.Exists(legacyCfg)) File.Delete(legacyCfg);
                }
                catch { }

                Settings = new ModConfig();

                bool resetRequested = Settings.ResetSafeMode.Value;
                if (resetRequested)
                {
                    Settings.ResetSafeMode.Value = false;
                    Settings.SaveAndNotify();
                }

                if (!SafeModeGuard.TryBeginBoot(Logger, resetRequested))
                {
                    return;
                }

                Settings.LogActiveBindings(Logger);

                if (!Settings.Enabled.Value)
                {
                    Logger.LogWarning("[EiyudenKBM] Desactivado por configuración (Enabled = false).");
                    SafeModeGuard.MarkBootSucceeded(Logger);
                    return;
                }

                // Apply Harmony patches individually so every hook is verified and isolated
                HarmonyInstance = new Harmony(PluginInfo.PLUGIN_GUID);
                ApplyPatchGroup(typeof(InputPatches));
                ApplyPatchGroup(typeof(MouseUIController));
                ApplyPatchGroup(typeof(CameraAndMapPatches));
                ApplyPatchGroup(typeof(DialogPatches));
                ApplyPatchGroup(typeof(BattlePatches));
                ApplyPatchGroup(typeof(KeyIconPatches));

                Settings.OnChanged += KeyIconPatches.NotifyConfigChanged;

                // Register lightweight runtime driver for CursorManager, Wheel, Hot-Reload, and SettingsRebindUI
                ClassInjector.RegisterTypeInIl2Cpp<KBMRuntimeHost>();
                AddComponent<KBMRuntimeHost>();

                Logger.LogInfo("[EiyudenKBM] v1.0.0 cargado correctamente. Pulsa F8 o entra en Opciones -> Controles (Teclado y Ratón).");
            }
            catch (Exception ex)
            {
                Logger.LogError("[EiyudenKBM] Error fatal durante Load(): " + ex);
            }
        }

        private static void ApplyPatchGroup(Type containerType)
        {
            var nested = containerType.GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            int ok = 0;
            foreach (var t in nested)
            {
                if (t.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    continue;
                try
                {
                    HarmonyInstance.CreateClassProcessor(t).Patch();
                    ok++;
                    Logger.LogInfo($"[Harmony] Parche aplicado: {containerType.Name}.{t.Name}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[Harmony] ERROR aplicando {containerType.Name}.{t.Name}: {ex}");
                }
            }
            Logger.LogInfo($"[Harmony] Grupo {containerType.Name}: {ok} parches activos.");
        }

        public override bool Unload()
        {
            try
            {
                HarmonyInstance?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Logger?.LogWarning("[EiyudenKBM] Error en Unload(): " + ex.Message);
            }
            return base.Unload();
        }
    }

    internal sealed class KBMRuntimeHost : MonoBehaviour
    {
        private int _frames;
        private float _nextHotReloadCheck;

        public KBMRuntimeHost(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            try
            {
                _frames++;
                if (!SafeModeGuard.BootConfirmed && _frames >= 60)
                {
                    SafeModeGuard.MarkBootSucceeded(Plugin.Logger);
                }

                SettingsRebindUI.Update();
                InputPatches.UpdateWheelState();
                CursorManager.Update();

                if (Time.unscaledTime >= _nextHotReloadCheck)
                {
                    _nextHotReloadCheck = Time.unscaledTime + 2.0f;
                    Plugin.Settings?.CheckHotReload(Plugin.Logger);
                }
            }
            catch (Exception ex)
            {
                if (Plugin.Settings?.VerboseLogging.Value == true)
                    Plugin.Logger?.LogWarning("[KBMRuntimeHost.Update] " + ex.Message);
            }
        }

        private void OnGUI()
        {
            SettingsRebindUI.OnGUI();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                CursorManager.OnFocusRestored();
        }
    }
}
