using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Nabra.Services
{
    /// <summary>"Start with Windows" via the per-user Run key (no admin rights needed).</summary>
    internal static class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Nabra";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    return key?.GetValue(ValueName) is string;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Set(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                                 ?? Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null) return;
                    if (enabled) key.SetValue(ValueName, BuildCommand());
                    else key.DeleteValue(ValueName, false);
                }
            }
            catch (Exception)
            {
                // Group policy or a locked-down profile can block this; the app keeps working.
            }
        }

        private static string BuildCommand()
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return string.Empty;

            // If launched through "dotnet Nabra.dll", the host is dotnet.exe, so include the dll.
            if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                string dll = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
                return "\"" + exe + "\" \"" + dll + "\" --minimized";
            }
            return "\"" + exe + "\" --minimized";
        }
    }
}
