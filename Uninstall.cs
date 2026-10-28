using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Nabra Uninstaller")]

class Program
{
    [DllImport("shell32.dll")]
    static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    static void Main()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string targetDir = Path.Combine(localAppData, "Nabra");

        try {
            var procs = Process.GetProcessesByName("Nabra");
            foreach (var p in procs) { try { p.Kill(); p.WaitForExit(); } catch { } }
        } catch {}

        try {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktopPath, "Nabra.lnk");
            if (File.Exists(shortcutPath)) File.Delete(shortcutPath);

            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string startShortcutPath = Path.Combine(startMenuPath, "Nabra", "Nabra.lnk");
            if (File.Exists(startShortcutPath)) File.Delete(startShortcutPath);
            if (Directory.Exists(Path.Combine(startMenuPath, "Nabra"))) Directory.Delete(Path.Combine(startMenuPath, "Nabra"), true);
        } catch {}

        try {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
            {
                if (key != null) key.DeleteSubKeyTree("Nabra", false);
            }
        } catch {}

        try { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); } catch {}

        // Schedule self-deletion via cmd
        Process.Start(new ProcessStartInfo()
        {
            Arguments = "/C choice /C Y /N /D Y /T 2 & rmdir /s /q \"" + targetDir + "\"",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = "cmd.exe"
        });
    }
}
