using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Nabra.Core.Models;
using Nabra.Core.Services;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    public partial class HomePage : UserControl
    {
        private readonly AppController _app;
        private bool _loading;

        internal HomePage(AppController app)
        {
            _app = app;
            InitializeComponent();

            _app.SettingsApplied += Refresh;
            _app.ConversionFinished += OnConversionFinished;
            Refresh();
        }

        private void Refresh()
        {
            var s = _app.Settings.Current;

            _loading = true;
            EnableToggle.IsChecked = s.IsEnabled;
            ModeAuto.IsChecked = s.ConversionMode == ConversionMode.Auto;
            ModeToArabic.IsChecked = s.ConversionMode == ConversionMode.ToArabic;
            ModeToEnglish.IsChecked = s.ConversionMode == ConversionMode.ToEnglish;
            _loading = false;

            StatusText.Text = Loc.T(s.IsEnabled ? "status_on" : "status_off");
            StatusDot.SetResourceReference(Shape.FillProperty, s.IsEnabled ? "SuccessBrush" : "DangerBrush");
            StatusHint.Text = Loc.T(s.IsEnabled ? "home_status_hint_on" : "home_status_hint_off");

            bool isElevated = ElevationService.IsElevated();
            ElevateBadgeText.Text = Loc.T(isElevated ? "elevate_status_admin" : "elevate_status_normal");
            ElevateBadgeText.SetResourceReference(TextBlock.ForegroundProperty, isElevated ? "SuccessBrush" : "TextSecondary");

            UiHelpers.FillChips(HotkeyChips, s.Hotkey);

            if (s.IsEnabled && !_app.ConvertRegistration.Success)
            {
                string key = _app.ConvertRegistration.InUse ? "hk_reg_failed_inuse" : "hk_reg_failed";
                UiHelpers.ShowBanner(Banner, BannerText, BannerKind.Error, Loc.T(key));
            }
            else
            {
                UiHelpers.HideBanner(Banner);
            }

            switch (s.ConversionMode)
            {
                case ConversionMode.ToArabic: ModeDesc.Text = Loc.T("mode_to_ar_desc"); break;
                case ConversionMode.ToEnglish: ModeDesc.Text = Loc.T("mode_to_en_desc"); break;
                default: ModeDesc.Text = Loc.T("mode_auto_desc"); break;
            }

            UpdateLastConversion();
            UpdateTester();
        }

        private void OnConversionFinished(ConversionOutcome outcome) => UpdateLastConversion();

        private void UpdateLastConversion()
        {
            var outcome = _app.LastOutcome;
            var time = _app.LastOutcomeTime;
            if (outcome == null || time == null)
            {
                LastText.Text = Loc.T("last_none");
                return;
            }

            string stamp = time.Value.ToString("HH:mm:ss");
            if (outcome.Kind == OutcomeKind.Success)
            {
                string direction = Loc.T(outcome.Direction == TextDirection.ToArabic ? "dir_en_to_ar" : "dir_ar_to_en");
                LastText.Text = Loc.F("last_success", direction, outcome.Characters, stamp);
            }
            else
            {
                LastText.Text = Loc.F("last_failed", stamp);
            }
        }

        private void Enable_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _app.SetEnabled(EnableToggle.IsChecked == true);
        }

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var tag = (string)((RadioButton)sender).Tag;
            var mode = (ConversionMode)Enum.Parse(typeof(ConversionMode), tag);
            _app.SetMode(mode);
        }

        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            var result = UiHelpers.RunCapture(this, _app, HotkeyAction.Convert);
            if (result == null) return;

            string message = UiHelpers.DescribeCommit(result, _app.Settings.Current.Hotkey, out BannerKind kind);
            UiHelpers.ShowBanner(Banner, BannerText, kind, message);
        }

        // ------------------------------------------------------------ local tester

        private void Input_TextChanged(object sender, TextChangedEventArgs e) => UpdateTester();

        private void UpdateTester()
        {
            if (InputBox == null || OutputBox == null || DirectionLabel == null) return;

            var result = _app.Text.Convert(InputBox.Text, _app.Settings.Current.ConversionMode);
            OutputBox.Text = result.Text;
            OutputBox.FlowDirection = result.Changed && result.Direction == TextDirection.ToArabic
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            if (result.Changed)
                DirectionLabel.Text = Loc.T(result.Direction == TextDirection.ToArabic ? "dir_en_to_ar" : "dir_ar_to_en");
            else if (string.IsNullOrWhiteSpace(InputBox.Text))
                DirectionLabel.Text = Loc.T("tester_placeholder");
            else
                DirectionLabel.Text = Loc.T("tester_nochange");
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(OutputBox.Text)) return;
            try
            {
                Clipboard.SetText(OutputBox.Text);
            }
            catch (ExternalException)
            {
                // clipboard is busy; the user can simply press the button again
            }
        }

        private void Example_Click(object sender, RoutedEventArgs e) => InputBox.Text = "lvpfh hgsghl ugd;l";

        private void Clear_Click(object sender, RoutedEventArgs e) => InputBox.Clear();
    }
}
