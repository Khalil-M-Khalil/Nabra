using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using Nabra.Localization;

namespace Nabra.Services
{
    /// <summary>
    /// Manages process elevation status and allows the user to grant full Administrator / root privileges
    /// with an explicit consent prompt, bypassing Windows UIPI restrictions for system-wide input tracking.
    /// </summary>
    internal static class ElevationService
    {
        public static bool IsElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool PromptAndRestartElevated(Window? owner = null)
        {
            if (IsElevated()) return true;

            var title = Loc.T("elevate_dialog_title");
            var message = Loc.T("elevate_dialog_message");

            var options = Loc.Instance.IsArabic
                ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                : MessageBoxOptions.None;

            var result = MessageBox.Show(
                owner,
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                options);

            if (result != MessageBoxResult.Yes) return false;

            return RestartElevated();
        }

        public static bool RestartElevated()
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(startInfo);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current is App app)
                    {
                        app.ExitApplication();
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                });
                return true;
            }
            catch (Exception)
            {
                // User cancelled UAC dialog
                return false;
            }
        }
    }
}
