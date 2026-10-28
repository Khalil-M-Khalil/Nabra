using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Nabra.Installer
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var app = new Application();
            var window = new InstallerWindow();
            app.Run(window);
        }
    }

    class InstallerWindow : Window
    {
        private ProgressBar _progress;
        private TextBlock _status;
        private Button _btnAction;

        public InstallerWindow()
        {
            Title = "Nabra Setup";
            Width = 500;
            Height = 320;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var border = new Border
            {
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 36)), // Dark modern bg
                Padding = new Thickness(30)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var title = new TextBlock
            {
                Text = "تثبيت نبْرة - Nabra Setup",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // Center content
            var centerPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            
            _status = new TextBlock
            {
                Text = "جاهز للتثبيت...",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(151, 165, 178)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            _progress = new ProgressBar
            {
                Height = 6,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(43, 53, 65)),
                Foreground = new SolidColorBrush(Color.FromRgb(14, 159, 142)),
                Maximum = 100,
                Value = 0,
                Margin = new Thickness(20, 0, 20, 0)
            };

            centerPanel.Children.Add(_status);
            centerPanel.Children.Add(_progress);
            Grid.SetRow(centerPanel, 1);
            grid.Children.Add(centerPanel);

            // Button
            _btnAction = new Button
            {
                Content = "تثبيت (Install)",
                Height = 40,
                Width = 160,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(14, 159, 142)),
                BorderThickness = new Thickness(0),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            // Quick style for rounded button
            _btnAction.Resources.Add(typeof(Border), new Style(typeof(Border))
            {
                Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(8)) }
            });
            _btnAction.Click += Install_Click;

            Grid.SetRow(_btnAction, 2);
            grid.Children.Add(_btnAction);

            border.Child = grid;
            Content = border;

            MouseLeftButtonDown += (s, e) => DragMove();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (_btnAction.Content.ToString() == "إنهاء (Finish)")
            {
                Close();
                return;
            }

            _btnAction.IsEnabled = false;
            _status.Text = "جاري التثبيت...";
            _progress.IsIndeterminate = true;

            try
            {
                await Task.Run(() => PerformInstall());
                _progress.IsIndeterminate = false;
                _progress.Value = 100;
                _status.Text = "تم التثبيت بنجاح!";
                _status.Foreground = new SolidColorBrush(Color.FromRgb(76, 208, 138));
                _btnAction.Content = "إنهاء (Finish)";
                _btnAction.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _progress.IsIndeterminate = false;
                _status.Text = "حدث خطأ: " + ex.Message;
                _status.Foreground = Brushes.Red;
                _btnAction.Content = "إغلاق";
                _btnAction.IsEnabled = true;
            }
        }

        private void PerformInstall()
        {
            // Extract payload
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string targetDir = Path.Combine(localAppData, "Nabra");

            // Kill existing process if running
            var procs = Process.GetProcessesByName("Nabra");
            foreach (var p in procs) { try { p.Kill(); p.WaitForExit(); } catch { } }

            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }
            Directory.CreateDirectory(targetDir);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Nabra.Installer.payload.zip"))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(targetDir);
            }

            // Create Shortcut on Desktop using PowerShell (no COM interop needed)
            string exePath = Path.Combine(targetDir, "Nabra.exe");
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktopPath, "Nabra.lnk");
            
            string psScript = $@"
$wshell = New-Object -ComObject WScript.Shell;
$shortcut = $wshell.CreateShortcut('{shortcutPath}');
$shortcut.TargetPath = '{exePath}';
$shortcut.WorkingDirectory = '{targetDir}';
$shortcut.Description = 'Nabra';
$shortcut.Save();
";
            var ps = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(ps)?.WaitForExit();

            // Create Start Menu Shortcut
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string startMenuDir = Path.Combine(startMenuPath, "Nabra");
            if (!Directory.Exists(startMenuDir)) Directory.CreateDirectory(startMenuDir);
            string startShortcutPath = Path.Combine(startMenuDir, "Nabra.lnk");
            
            psScript = $@"
$wshell = New-Object -ComObject WScript.Shell;
$shortcut = $wshell.CreateShortcut('{startShortcutPath}');
$shortcut.TargetPath = '{exePath}';
$shortcut.WorkingDirectory = '{targetDir}';
$shortcut.Description = 'Nabra';
$shortcut.Save();
";
            ps.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"";
            Process.Start(ps)?.WaitForExit();

            // Launch
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = targetDir,
                UseShellExecute = true
            });
        }
    }
}
