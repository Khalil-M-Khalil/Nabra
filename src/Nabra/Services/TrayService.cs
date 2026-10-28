using System;
using System.Windows;
using Nabra.Core.Models;
using Nabra.Localization;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Nabra.Services
{
    /// <summary>System-tray icon and its context menu.</summary>
    internal sealed class TrayService : IDisposable
    {
        private readonly Forms.NotifyIcon _icon = new Forms.NotifyIcon();
        private readonly Forms.ContextMenuStrip _menu = new Forms.ContextMenuStrip();

        private Forms.ToolStripMenuItem _miToggle = null!;
        private Forms.ToolStripMenuItem _miRealTime = null!;
        private Forms.ToolStripMenuItem _miOpen = null!;
        private Forms.ToolStripMenuItem _miHotkey = null!;
        private Forms.ToolStripMenuItem _miMode = null!;
        private Forms.ToolStripMenuItem _miAuto = null!;
        private Forms.ToolStripMenuItem _miToArabic = null!;
        private Forms.ToolStripMenuItem _miToEnglish = null!;
        private Forms.ToolStripMenuItem _miSettings = null!;
        private Forms.ToolStripMenuItem _miExit = null!;

        private AppController _app = null!;
        private Drawing.Icon? _currentIcon;
        private bool _lastEnabled;
        private string _lastAccent = string.Empty;

        public void Initialize(AppController app)
        {
            _app = app;

            _miToggle = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.SetEnabled(!_app.Settings.Current.IsEnabled));
            _miRealTime = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.SetRealTime(!_app.Settings.Current.RealTimeCorrection));
            _miOpen = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.OpenWindow("Home", false));
            _miHotkey = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.OpenWindow("Shortcuts", true));

            _miAuto = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.SetMode(ConversionMode.Auto));
            _miToArabic = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.SetMode(ConversionMode.ToArabic));
            _miToEnglish = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.SetMode(ConversionMode.ToEnglish));
            _miMode = new Forms.ToolStripMenuItem(string.Empty);
            _miMode.DropDownItems.AddRange(new Forms.ToolStripItem[] { _miAuto, _miToArabic, _miToEnglish });

            _miSettings = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.OpenWindow("Settings", false));
            _miExit = new Forms.ToolStripMenuItem(string.Empty, null, (s, e) => _app.RequestExit());

            _menu.Items.AddRange(new Forms.ToolStripItem[]
            {
                _miToggle,
                _miRealTime,
                new Forms.ToolStripSeparator(),
                _miOpen,
                _miHotkey,
                _miMode,
                _miSettings,
                new Forms.ToolStripSeparator(),
                _miExit,
            });

            _icon.ContextMenuStrip = _menu;
            _icon.Text = "Nabra";
            _icon.MouseClick += (s, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left) _app.OpenWindow("Home", false);
            };
            _icon.Visible = true;
        }

        public void Refresh(AppSettings settings)
        {
            _menu.RightToLeft = Loc.Instance.IsArabic ? Forms.RightToLeft.Yes : Forms.RightToLeft.No;

            _miToggle.Text = Loc.T(settings.IsEnabled ? "tray_disable" : "tray_enable");
            _miRealTime.Text = Loc.T("tray_realtime");
            _miRealTime.Checked = settings.RealTimeCorrection;
            _miOpen.Text = Loc.T("tray_open");
            _miHotkey.Text = Loc.T("tray_change_hotkey") + "  (" + settings.Hotkey.ToDisplayString() + ")";
            _miMode.Text = Loc.T("tray_mode");
            _miAuto.Text = Loc.T("mode_auto");
            _miToArabic.Text = Loc.T("mode_to_ar");
            _miToEnglish.Text = Loc.T("mode_to_en");
            _miAuto.Checked = settings.ConversionMode == ConversionMode.Auto;
            _miToArabic.Checked = settings.ConversionMode == ConversionMode.ToArabic;
            _miToEnglish.Checked = settings.ConversionMode == ConversionMode.ToEnglish;
            _miSettings.Text = Loc.T("tray_settings");
            _miExit.Text = Loc.T("tray_exit");

            _icon.Text = "Nabra - " + Loc.T(settings.IsEnabled ? "status_on" : "status_off");

            if (_currentIcon == null || _lastEnabled != settings.IsEnabled || _lastAccent != settings.AccentColor)
            {
                var color = ThemeService.ParseColor(settings.AccentColor);
                var old = _currentIcon;
                _currentIcon = IconFactory.Create(settings.IsEnabled, color.R, color.G, color.B);
                _icon.Icon = _currentIcon;
                old?.Dispose();
                _lastEnabled = settings.IsEnabled;
                _lastAccent = settings.AccentColor;
            }
        }

        public void ShowBalloon(string text, bool isError)
        {
            try
            {
                _icon.ShowBalloonTip(2500, "Nabra \u2014 \u0646\u0628\u0652\u0631\u0629", text,
                    isError ? Forms.ToolTipIcon.Warning : Forms.ToolTipIcon.Info);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
            _menu.Dispose();
            _currentIcon?.Dispose();
        }
    }
}
