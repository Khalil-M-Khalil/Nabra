using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Nabra Setup")]
[assembly: AssemblyCompany("Khalil Mohammad Khalil & Yazed Mlhem")]
[assembly: AssemblyCopyright("Copyright © 2026 Khalil Mohammad Khalil")]
[assembly: AssemblyProduct("Nabra")]

namespace NabraInstaller
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WizardForm());
        }
    }

    public class WizardForm : Form
    {
        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private int step = 0;
        private Panel panelWelcome;
        private Panel panelPath;
        private Panel panelProgress;
        private Panel panelFinish;
        
        private Button btnNext;
        private Button btnBack;
        private Button btnCancel;
        
        private TextBox txtPath;
        private ProgressBar progressBar;
        private Label lblStatus;
        
        public WizardForm()
        {
            this.Text = "Nabra Setup";
            this.Size = new Size(500, 360);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // Bottom bar
            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            bottom.BackColor = SystemColors.Control;
            this.Controls.Add(bottom);
            
            Label line = new Label { Dock = DockStyle.Top, Height = 2, BorderStyle = BorderStyle.Fixed3D };
            bottom.Controls.Add(line);
            
            btnCancel = new Button { Text = "Cancel", Size = new Size(75, 23), Location = new Point(395, 12) };
            btnNext = new Button { Text = "Next >", Size = new Size(75, 23), Location = new Point(310, 12) };
            btnBack = new Button { Text = "< Back", Size = new Size(75, 23), Location = new Point(235, 12) };
            
            btnCancel.Click += (s, e) => this.Close();
            btnNext.Click += BtnNext_Click;
            btnBack.Click += BtnBack_Click;
            
            bottom.Controls.Add(btnCancel);
            bottom.Controls.Add(btnNext);
            bottom.Controls.Add(btnBack);

            // Container
            Panel container = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            this.Controls.Add(container);
            
            // Welcome
            panelWelcome = new Panel { Dock = DockStyle.Fill };
            Label lblWelcomeTitle = new Label { Text = "Welcome to Nabra Setup", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblWelcomeDesc = new Label { Text = "This wizard will guide you through the installation of Nabra.\n\nNabra is a smart keyboard layout corrector.\n\nClick Next to continue.", Font = new Font("Segoe UI", 10), Location = new Point(20, 70), Size = new Size(400, 200) };
            panelWelcome.Controls.Add(lblWelcomeTitle);
            panelWelcome.Controls.Add(lblWelcomeDesc);
            
            // Path
            panelPath = new Panel { Dock = DockStyle.Fill, Visible = false };
            Label lblPathTitle = new Label { Text = "Select Destination Location", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblPathDesc = new Label { Text = "Where should Nabra be installed?", Font = new Font("Segoe UI", 10), Location = new Point(20, 50), AutoSize = true };
            txtPath = new TextBox { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nabra"), Location = new Point(20, 80), Width = 350, Font = new Font("Segoe UI", 10) };
            Button btnBrowse = new Button { Text = "Browse...", Location = new Point(380, 79), Size = new Size(80, 25) };
            btnBrowse.Click += (s, e) => {
                using (var fbd = new FolderBrowserDialog { SelectedPath = txtPath.Text }) {
                    if (fbd.ShowDialog() == DialogResult.OK) txtPath.Text = fbd.SelectedPath;
                }
            };
            panelPath.Controls.Add(lblPathTitle);
            panelPath.Controls.Add(lblPathDesc);
            panelPath.Controls.Add(txtPath);
            panelPath.Controls.Add(btnBrowse);
            
            // Progress
            panelProgress = new Panel { Dock = DockStyle.Fill, Visible = false };
            Label lblProgTitle = new Label { Text = "Installing", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            lblStatus = new Label { Text = "Extracting files...", Font = new Font("Segoe UI", 9), Location = new Point(20, 50), AutoSize = true };
            progressBar = new ProgressBar { Style = ProgressBarStyle.Marquee, Location = new Point(20, 75), Size = new Size(440, 20) };
            panelProgress.Controls.Add(lblProgTitle);
            panelProgress.Controls.Add(lblStatus);
            panelProgress.Controls.Add(progressBar);
            
            // Finish
            panelFinish = new Panel { Dock = DockStyle.Fill, Visible = false };
            Label lblFinTitle = new Label { Text = "Completing Nabra Setup", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblFinDesc = new Label { Text = "Setup has finished installing Nabra on your computer.\n\nClick Finish to launch the application.", Font = new Font("Segoe UI", 10), Location = new Point(20, 70), Size = new Size(400, 200) };
            panelFinish.Controls.Add(lblFinTitle);
            panelFinish.Controls.Add(lblFinDesc);
            
            container.Controls.Add(panelWelcome);
            container.Controls.Add(panelPath);
            container.Controls.Add(panelProgress);
            container.Controls.Add(panelFinish);
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            panelWelcome.Visible = step == 0;
            panelPath.Visible = step == 1;
            panelProgress.Visible = step == 2;
            panelFinish.Visible = step == 3;
            
            btnBack.Enabled = step > 0 && step < 2;
            
            if (step == 0 || step == 1) {
                btnNext.Text = "Next >";
                btnNext.Enabled = true;
            } else if (step == 2) {
                btnNext.Enabled = false;
                btnBack.Enabled = false;
                btnCancel.Enabled = false;
            } else if (step == 3) {
                btnNext.Text = "Finish";
                btnNext.Enabled = true;
                btnCancel.Enabled = false;
            }
        }
        
        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (step > 0) step--;
            UpdateUI();
        }
        
        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (step == 3) {
                this.Close();
                return;
            }
            
            step++;
            UpdateUI();
            
            if (step == 2) {
                Thread t = new Thread(DoInstall);
                t.IsBackground = true;
                t.Start();
            }
        }
        
        private void DoInstall()
        {
            try {
                string targetDir = txtPath.Text;
                var procs = Process.GetProcessesByName("Nabra");
                foreach (var p in procs) { try { p.Kill(); p.WaitForExit(); } catch { } }

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries) {
                        string dest = Path.Combine(targetDir, entry.FullName);
                        string dir = Path.GetDirectoryName(dest);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        entry.ExtractToFile(dest, true);
                    }
                }

                string iconPath = Path.Combine(targetDir, "app.ico");
                string exePath = Path.Combine(targetDir, "Nabra.exe");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "Nabra.lnk");
                
                try { if (File.Exists(shortcutPath)) File.Delete(shortcutPath); } catch { }

                string psScript = "$wshell = New-Object -ComObject WScript.Shell; $shortcut = $wshell.CreateShortcut('" + shortcutPath + "'); $shortcut.TargetPath = '" + exePath + "'; $shortcut.IconLocation = '" + iconPath + ",0'; $shortcut.WorkingDirectory = '" + targetDir + "'; $shortcut.Description = 'Nabra'; $shortcut.Save();";
                var ps = new ProcessStartInfo {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript + "\"",
                    CreateNoWindow = true, UseShellExecute = false
                };
                var p1 = Process.Start(ps);
                if (p1 != null) p1.WaitForExit();

                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string startMenuDir = Path.Combine(startMenuPath, "Nabra");
                if (!Directory.Exists(startMenuDir)) Directory.CreateDirectory(startMenuDir);
                string startShortcutPath = Path.Combine(startMenuDir, "Nabra.lnk");
                try { if (File.Exists(startShortcutPath)) File.Delete(startShortcutPath); } catch { }
                
                psScript = "$wshell = New-Object -ComObject WScript.Shell; $shortcut = $wshell.CreateShortcut('" + startShortcutPath + "'); $shortcut.TargetPath = '" + exePath + "'; $shortcut.IconLocation = '" + iconPath + ",0'; $shortcut.WorkingDirectory = '" + targetDir + "'; $shortcut.Description = 'Nabra'; $shortcut.Save();";
                ps.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript + "\"";
                var p2 = Process.Start(ps);
                if (p2 != null) p2.WaitForExit();

                // Refresh Windows shell icon cache immediately
                try { SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); } catch { }

                var ps3 = new ProcessStartInfo { FileName = exePath, WorkingDirectory = targetDir, UseShellExecute = true };
                Process.Start(ps3);

                try {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Nabra"))
                    {
                        key.SetValue("DisplayName", "Nabra");
                        key.SetValue("DisplayIcon", iconPath);
                        key.SetValue("UninstallString", Path.Combine(targetDir, "Uninstall.exe"));
                        key.SetValue("Publisher", "Khalil Mohammad Khalil & Yazed Mlhem");
                        key.SetValue("NoModify", 1);
                        key.SetValue("NoRepair", 1);
                    }
                } catch { }

                this.Invoke((MethodInvoker)delegate {
                    step = 3;
                    UpdateUI();
                });
            } catch (Exception ex) {
                this.Invoke((MethodInvoker)delegate {
                    MessageBox.Show("Error during installation: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                });
            }
        }
    }
}
