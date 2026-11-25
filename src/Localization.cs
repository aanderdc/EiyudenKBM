using System;
using System.Globalization;

namespace EiyudenKBM
{
    public enum ModLanguage
    {
        Auto,
        English,
        Spanish
    }

    /// <summary>
    /// Lightweight bilingual (English / Spanish) localization for the mod UI and Options menu entry.
    /// Defaults to Auto (Spanish if system locale is Spanish, English otherwise for international Nexus Mods users).
    /// </summary>
    internal static class Loc
    {
        public static bool IsSpanish
        {
            get
            {
                var setting = Plugin.Settings?.Language?.Value ?? ModLanguage.Auto;
                if (setting == ModLanguage.Spanish) return true;
                if (setting == ModLanguage.English) return false;
                try
                {
                    return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                        .Equals("es", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string T(string en, string es) => IsSpanish ? es : en;

        public static string LanguageDisplayName(ModLanguage lang) => lang switch
        {
            ModLanguage.English => "English",
            ModLanguage.Spanish => "Español",
            _ => IsSpanish ? "Auto (Español)" : "Auto (English)"
        };

        public static string OptionsMenuButtonText =>
            T("Keyboard & Mouse Controls", "Controles (Teclado y Ratón)");
    }
}
