using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace EiyudenKBM
{
    /// <summary>
    /// Automatic Safe Mode watchdog:
    /// - Creates a boot lock file when the plugin starts loading.
    /// - Clears the lock file only after the game has safely rendered frames.
    /// - If the lock file still exists on the next launch, the previous boot crashed,
    ///   so the plugin automatically enters Safe Mode and disables itself.
    /// </summary>
    internal static class SafeModeGuard
    {
        private static readonly string LockPath = Path.Combine(Paths.ConfigPath, "EiyudenKBM.booting.lock");
        private static readonly string SafeModeFlagPath = Path.Combine(Paths.ConfigPath, "EiyudenKBM.safemode");

        public static bool IsInSafeMode { get; private set; }
        public static bool BootConfirmed { get; private set; }

        public static bool TryBeginBoot(ManualLogSource log, bool resetSafeModeRequested)
        {
            try
            {
                Directory.CreateDirectory(Paths.ConfigPath);

                if (resetSafeModeRequested)
                {
                    if (File.Exists(LockPath)) File.Delete(LockPath);
                    if (File.Exists(SafeModeFlagPath)) File.Delete(SafeModeFlagPath);
                    log.LogWarning("[SafeMode] ResetSafeMode=true detectado: se borró el estado de Modo Seguro.");
                }

                if (File.Exists(SafeModeFlagPath))
                {
                    IsInSafeMode = true;
                    log.LogWarning("=================================================================");
                    log.LogWarning("[SafeMode] MODO SEGURO ACTIVO: EiyudenKBM permanece desactivado.");
                    log.LogWarning("[SafeMode] Para reactivarlo, borra: " + SafeModeFlagPath);
                    log.LogWarning("[SafeMode] o pon ResetSafeMode = true en BepInEx/config/EiyudenKBM.cfg.");
                    log.LogWarning("=================================================================");
                    return false;
                }

                if (File.Exists(LockPath))
                {
                    string prevInfo = "";
                    try { prevInfo = File.ReadAllText(LockPath).Trim(); } catch { }

                    IsInSafeMode = true;
                    File.WriteAllText(SafeModeFlagPath,
                        $"SafeMode triggered at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                        $"Reason: Previous boot did not finish cleanly.\n" +
                        $"Previous lock: {prevInfo}\n");
                    try { File.Delete(LockPath); } catch { }

                    log.LogError("=================================================================");
                    log.LogError("[SafeMode] El arranque anterior NO terminó correctamente.");
                    log.LogError("[SafeMode] EiyudenKBM se ha desactivado automáticamente (Modo Seguro).");
                    log.LogError("[SafeMode] Lock previo: " + prevInfo);
                    log.LogError("[SafeMode] Revisa BepInEx/LogOutput.log para diagnosticar la causa.");
                    log.LogError("=================================================================");
                    return false;
                }

                File.WriteAllText(LockPath,
                    $"Started={DateTime.Now:yyyy-MM-dd HH:mm:ss}; PID={Environment.ProcessId}; Version={PluginInfo.PLUGIN_VERSION}");
                return true;
            }
            catch (Exception ex)
            {
                log.LogError("[SafeMode] Error comprobando estado de arranque: " + ex);
                IsInSafeMode = true;
                return false;
            }
        }

        public static void MarkBootSucceeded(ManualLogSource log)
        {
            if (BootConfirmed) return;
            BootConfirmed = true;
            try
            {
                if (File.Exists(LockPath))
                {
                    File.Delete(LockPath);
                    log.LogInfo("[SafeMode] Arranque completado correctamente. Lock de arranque eliminado.");
                }
            }
            catch (Exception ex)
            {
                log.LogWarning("[SafeMode] No se pudo eliminar el lock de arranque: " + ex.Message);
            }
        }
    }
}
