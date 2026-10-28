using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Nabra.Core.Models;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    public partial class SettingsPage : UserControl
    {
        private static readonly string[] AccentColors =
        {
            "#0E9F8E", "#2F6FED", "#6A5AE0", "#A44BDB", "#E0457B", "#E5484D", "#E8801C", "#3FA34D",
        };

        private readonly AppController _app;
        private bool _loading;

        internal SettingsPage(AppController app)
        {
            _app = app;
            InitializeComponent();
            BuildAccentSwatches();

            _app.SettingsApplied += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            var s = _app.Settings.Current;
            _loading = true;
            try
            {
                StartupToggle.IsChecked = s.StartWithWindows;
                TrayToggle.IsChecked = s.MinimizeToTrayOnClose;
                NotifyToggle.IsChecked = s.ShowNotifications;
                SoundToggle.IsChecked = s.PlaySound;
                RestoreToggle.IsChecked = s.RestoreClipboard;

                ModeAuto.IsChecked = s.ConversionMode == ConversionMode.Auto;
                ModeToArabic.IsChecked = s.ConversionMode == ConversionMode.ToArabic;
                ModeToEnglish.IsChecked = s.ConversionMode == ConversionMode.ToEnglish;

                SpeedFast.IsChecked = s.Speed == SpeedProfile.Fast;
                SpeedNormal.IsChecked = s.Speed == SpeedProfile.Normal;
                SpeedRelaxed.IsChecked = s.Speed == SpeedProfile.Relaxed;

                RealTimeToggle.IsChecked = s.RealTimeCorrection;
                RealTimeSmartToggle.IsChecked = s.RealTimeSmartFilter;
                RealTimeEnterToggle.IsChecked = s.RealTimeOnEnter;

                Timeout10.IsChecked = Math.Abs(s.RealTimeIdleSeconds - 1.0) < 0.15;
                Timeout15.IsChecked = Math.Abs(s.RealTimeIdleSeconds - 1.5) < 0.15;
                Timeout20.IsChecked = Math.Abs(s.RealTimeIdleSeconds - 2.0) < 0.15;
                Timeout30.IsChecked = Math.Abs(s.RealTimeIdleSeconds - 3.0) < 0.15;
                Timeout50.IsChecked = Math.Abs(s.RealTimeIdleSeconds - 5.0) < 0.15;

                ThemeSystem.IsChecked = s.Theme == ThemeMode.System;
                ThemeLight.IsChecked = s.Theme == ThemeMode.Light;
                ThemeDark.IsChecked = s.Theme == ThemeMode.Dark;

                LangAr.IsChecked = s.Language == "ar";
                LangEn.IsChecked = s.Language == "en";

                Scale090.IsChecked = Math.Abs(s.UiScale - 0.9) < 0.04;
                Scale100.IsChecked = Math.Abs(s.UiScale - 1.0) < 0.04;
                Scale115.IsChecked = Math.Abs(s.UiScale - 1.15) < 0.04;
                Scale130.IsChecked = Math.Abs(s.UiScale - 1.3) < 0.04;

                AccentHex.Text = s.AccentColor.ToUpperInvariant();
                MarkSelectedSwatch(s.AccentColor);

                bool isElevated = ElevationService.IsElevated();
                ElevateStatusText.Text = Loc.T(isElevated ? "elevate_status_admin" : "elevate_status_normal");
                ElevateStatusText.SetResourceReference(TextBlock.ForegroundProperty, isElevated ? "SuccessBrush" : "WarnBrush");
                ElevateBtn.IsEnabled = !isElevated;
                ElevateBtn.Visibility = isElevated ? Visibility.Collapsed : Visibility.Visible;
            }
            finally
            {
                _loading = false;
            }
        }

        private void Toggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            var box = (CheckBox)sender;
            bool on = box.IsChecked == true;

            if (ReferenceEquals(box, StartupToggle)) _app.Update(s => s.StartWithWindows = on);
            else if (ReferenceEquals(box, TrayToggle)) _app.Update(s => s.MinimizeToTrayOnClose = on);
            else if (ReferenceEquals(box, NotifyToggle)) _app.Update(s => s.ShowNotifications = on);
            else if (ReferenceEquals(box, SoundToggle)) _app.Update(s => s.PlaySound = on);
            else if (ReferenceEquals(box, RestoreToggle)) _app.Update(s => s.RestoreClipboard = on);
            else if (ReferenceEquals(box, RealTimeToggle)) _app.SetRealTime(on);
            else if (ReferenceEquals(box, RealTimeSmartToggle)) _app.SetRealTimeSmartFilter(on);
            else if (ReferenceEquals(box, RealTimeEnterToggle)) _app.Update(s => s.RealTimeOnEnter = on);
        }

        private void Seg_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            var radio = (RadioButton)sender;
            string tag = (string)radio.Tag;

            switch (radio.GroupName)
            {
                case "setmode":
                    _app.Update(s => s.ConversionMode = (ConversionMode)Enum.Parse(typeof(ConversionMode), tag));
                    break;
                case "speed":
                    _app.Update(s => s.Speed = (SpeedProfile)Enum.Parse(typeof(SpeedProfile), tag));
                    break;
                case "rttimeout":
                    double sec = double.Parse(tag, CultureInfo.InvariantCulture);
                    _app.SetRealTimeIdleSeconds(sec);
                    break;
                case "theme":
                    _app.Update(s => s.Theme = (ThemeMode)Enum.Parse(typeof(ThemeMode), tag));
                    break;
                case "lang":
                    _app.Update(s => s.Language = tag);
                    break;
                case "scale":
                    double scale = double.Parse(tag, CultureInfo.InvariantCulture);
                    _app.Update(s => s.UiScale = scale);
                    break;
            }
        }

        // ------------------------------------------------------------ accent colour

        private void BuildAccentSwatches()
        {
            foreach (var hex in AccentColors)
            {
                var swatch = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Margin = new Thickness(0, 0, 10, 8),
                    BorderThickness = new Thickness(3),
                    BorderBrush = Brushes.Transparent,
                    Background = new SolidColorBrush(ThemeService.ParseColor(hex)),
                    Cursor = Cursors.Hand,
                    Tag = hex,
                    ToolTip = hex,
                };
                swatch.MouseLeftButtonUp += Swatch_Click;
                AccentPanel.Children.Add(swatch);
            }
        }

        private void MarkSelectedSwatch(string current)
        {
            foreach (var child in AccentPanel.Children)
            {
                if (!(child is Border swatch)) continue;
                bool selected = string.Equals((string)swatch.Tag, current, StringComparison.OrdinalIgnoreCase);
                if (selected) swatch.SetResourceReference(Border.BorderBrushProperty, "TextPrimary");
                else swatch.BorderBrush = Brushes.Transparent;
            }
        }

        private void Swatch_Click(object sender, MouseButtonEventArgs e)
        {
            string hex = (string)((Border)sender).Tag;
            _app.Update(s => s.AccentColor = hex);
        }

        private void AccentHex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyHex();
                Keyboard.ClearFocus();
            }
        }

        private void AccentHex_LostFocus(object sender, RoutedEventArgs e) => ApplyHex();

        private void ApplyHex()
        {
            if (_loading) return;

            string text = AccentHex.Text.Trim();
            if (!text.StartsWith("#")) text = "#" + text;

            if (AppSettings.IsValidColor(text))
            {
                if (!string.Equals(text, _app.Settings.Current.AccentColor, StringComparison.OrdinalIgnoreCase))
                    _app.Update(s => s.AccentColor = text.ToUpperInvariant());
            }
            else
            {
                AccentHex.Text = _app.Settings.Current.AccentColor.ToUpperInvariant();
            }
        }

        // ------------------------------------------------------------ data

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + _app.Settings.Directory + "\"") { UseShellExecute = true });
            }
            catch (Exception)
            {
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var options = Loc.Instance.IsArabic
                ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign
                : MessageBoxOptions.None;

            var answer = MessageBox.Show(
                Window.GetWindow(this),
                Loc.T("reset_confirm"),
                Loc.T("reset_title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No,
                options);

            if (answer == MessageBoxResult.Yes) _app.ResetSettings();
        }

        private void ElevateBtn_Click(object sender, RoutedEventArgs e)
        {
            ElevationService.PromptAndRestartElevated(Window.GetWindow(this));
        }
    }
}
