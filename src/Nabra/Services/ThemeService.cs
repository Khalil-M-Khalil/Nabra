using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Nabra.Core.Models;

namespace Nabra.Services
{
    /// <summary>Light / dark / system theme and the user's accent colour.</summary>
    internal sealed class ThemeService
    {
        public bool IsDark { get; private set; }

        public event Action<bool>? ThemeChanged;

        public void Apply(AppSettings settings)
        {
            bool dark = settings.Theme == ThemeMode.Dark ||
                        (settings.Theme == ThemeMode.System && SystemUsesDarkApps());

            var resources = Application.Current.Resources;
            var uri = new Uri(
                dark ? "/Nabra;component/Themes/Dark.xaml" : "/Nabra;component/Themes/Light.xaml",
                UriKind.Relative);
            resources.MergedDictionaries[0] = new ResourceDictionary { Source = uri };

            Color accent = ParseColor(settings.AccentColor);
            resources["AccentBrush"] = Frozen(new SolidColorBrush(accent));
            resources["AccentSoftBrush"] = Frozen(new SolidColorBrush(Color.FromArgb(0x2A, accent.R, accent.G, accent.B)));

            IsDark = dark;
            ThemeChanged?.Invoke(dark);
        }

        public static Color ParseColor(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch (Exception)
            {
                return (Color)ColorConverter.ConvertFromString(AppSettings.DefaultAccent);
            }
        }

        private static SolidColorBrush Frozen(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }

        private static bool SystemUsesDarkApps()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false))
                {
                    return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
