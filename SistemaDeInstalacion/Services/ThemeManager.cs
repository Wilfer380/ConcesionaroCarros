using ConcesionaroCarros.Properties;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace ConcesionaroCarros.Services
{
    public static class ThemeManager
    {
        public const string SystemMode = "System";
        public const string LightMode = "Light";
        public const string DarkMode = "Dark";

        private const string LightDictionaryPath = "Themes/Light.xaml";
        private const string DarkDictionaryPath = "Themes/Dark.xaml";
        private const string ThemePreferenceSettingName = "ThemePreference";

        public static string CurrentPreference => NormalizePreference(GetStoredPreference());

        public static void Initialize(Application application)
        {
            ApplyThemePreference(CurrentPreference, application, false);
        }

        public static void ApplyThemePreference(string preference)
        {
            ApplyThemePreference(preference, Application.Current, true);
        }

        private static void ApplyThemePreference(string preference, Application application, bool persistPreference)
        {
            if (application?.Resources == null)
                return;

            var normalizedPreference = NormalizePreference(preference);
            var resolvedTheme = ResolveTheme(normalizedPreference);
            var mergedDictionaries = application.Resources.MergedDictionaries;
            var targetSource = new Uri(GetDictionaryPath(resolvedTheme), UriKind.Relative);

            foreach (var themeDictionary in mergedDictionaries.Where(IsThemeDictionary).ToList())
            {
                mergedDictionaries.Remove(themeDictionary);
            }

            try
            {
                mergedDictionaries.Add(new ResourceDictionary { Source = targetSource });
            }
            catch (Exception ex)
            {
                LogService.Error("ThemeManager", "No se pudo aplicar el diccionario de tema", ex, targetSource.OriginalString);
            }

            if (persistPreference && !string.Equals(GetStoredPreference(), normalizedPreference, StringComparison.Ordinal))
            {
                SavePreference(normalizedPreference);
            }
        }

        public static string ResolveTheme(string preference)
        {
            var normalizedPreference = NormalizePreference(preference);
            if (string.Equals(normalizedPreference, DarkMode, StringComparison.Ordinal))
                return DarkMode;

            if (string.Equals(normalizedPreference, LightMode, StringComparison.Ordinal))
                return LightMode;

            return IsSystemUsingDarkTheme() ? DarkMode : LightMode;
        }

        public static bool IsSystemUsingDarkTheme()
        {
            const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(personalizeKey))
                {
                    var rawValue = key?.GetValue("AppsUseLightTheme");
                    if (rawValue is int intValue)
                        return intValue == 0;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool IsThemeDictionary(ResourceDictionary dictionary)
        {
            if (dictionary?.Source == null)
                return false;

            var source = dictionary.Source.OriginalString;
            return EndsWithThemePath(source, LightDictionaryPath)
                || EndsWithThemePath(source, DarkDictionaryPath);
        }

        private static string NormalizePreference(string preference)
        {
            if (string.Equals(preference, LightMode, StringComparison.OrdinalIgnoreCase))
                return LightMode;

            if (string.Equals(preference, DarkMode, StringComparison.OrdinalIgnoreCase))
                return DarkMode;

            return SystemMode;
        }

        private static string GetDictionaryPath(string resolvedTheme)
        {
            return string.Equals(resolvedTheme, DarkMode, StringComparison.Ordinal)
                ? DarkDictionaryPath
                : LightDictionaryPath;
        }

        private static bool EndsWithThemePath(string source, string themePath)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            var normalizedSource = source.Replace('\\', '/');
            var normalizedThemePath = themePath.Replace('\\', '/');
            return normalizedSource.EndsWith(normalizedThemePath, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetStoredPreference()
        {
            try
            {
                return Settings.Default.ThemePreference;
            }
            catch (ConfigurationErrorsException ex)
            {
                LogService.Error("ThemeManager", "No se pudo leer la preferencia de tema; se usara modo sistema", ex);
            }
            catch (SettingsPropertyNotFoundException ex)
            {
                LogService.Error("ThemeManager", "No existe la propiedad de configuracion del tema; se usara modo sistema", ex, ThemePreferenceSettingName);
            }

            return SystemMode;
        }

        private static void SavePreference(string preference)
        {
            try
            {
                Settings.Default.ThemePreference = preference;
                Settings.Default.Save();
            }
            catch (ConfigurationErrorsException ex)
            {
                LogService.Error("ThemeManager", "No se pudo guardar la preferencia de tema", ex, preference);
            }
            catch (SettingsPropertyNotFoundException ex)
            {
                LogService.Error("ThemeManager", "No existe la propiedad de configuracion del tema; no se guardo la preferencia", ex, ThemePreferenceSettingName);
            }
        }
    }
}
