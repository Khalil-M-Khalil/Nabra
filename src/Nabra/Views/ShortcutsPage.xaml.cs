using System.Windows;
using System.Windows.Controls;
using Nabra.Core.Models;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    public partial class ShortcutsPage : UserControl
    {
        private readonly AppController _app;

        internal ShortcutsPage(AppController app)
        {
            _app = app;
            InitializeComponent();
            BuildSuggestions();

            _app.SettingsApplied += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            var s = _app.Settings.Current;
            UiHelpers.FillChips(ConvertChips, s.Hotkey);
            UiHelpers.FillChips(ToggleChips, s.ToggleHotkey, Loc.T("sc_not_set"));
            RemoveToggleButton.IsEnabled = s.ToggleHotkey != null;

            if (s.IsEnabled && !_app.ConvertRegistration.Success)
            {
                string key = _app.ConvertRegistration.InUse ? "hk_reg_failed_inuse" : "hk_reg_failed";
                UiHelpers.ShowBanner(Banner, BannerText, BannerKind.Error, Loc.T(key));
            }

            if (s.ToggleHotkey != null && !_app.ToggleRegistration.Success)
            {
                string key = _app.ToggleRegistration.InUse ? "hk_reg_failed_inuse" : "hk_reg_failed";
                UiHelpers.ShowBanner(ToggleBanner, ToggleBannerText, BannerKind.Error, Loc.T(key));
            }
        }

        private void BuildSuggestions()
        {
            var suggestions = new[]
            {
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "K"),
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Shift, "K"),
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "Space"),
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "N"),
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, "N"),
                HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "F9"),
            };

            foreach (var binding in suggestions)
            {
                var button = new Button
                {
                    Content = binding.ToDisplayString(),
                    Tag = binding,
                    Margin = new Thickness(0, 0, 8, 8),
                    FlowDirection = FlowDirection.LeftToRight,
                };
                button.Click += Suggestion_Click;
                SuggestionPanel.Children.Add(button);
            }
        }

        // Called from the tray menu ("Change shortcut").
        internal void BeginChange(HotkeyAction action) => RunCapture(action);

        private void Change_Click(object sender, RoutedEventArgs e) => RunCapture(HotkeyAction.Convert);

        private void SetToggle_Click(object sender, RoutedEventArgs e) => RunCapture(HotkeyAction.Toggle);

        private void RemoveToggle_Click(object sender, RoutedEventArgs e)
        {
            _app.ClearToggleHotkey();
            UiHelpers.HideBanner(ToggleBanner);
        }

        private void RestoreDefault_Click(object sender, RoutedEventArgs e)
            => Commit(HotkeyAction.Convert, AppSettings.DefaultHotkey());

        private void Suggestion_Click(object sender, RoutedEventArgs e)
            => Commit(HotkeyAction.Convert, (HotkeyBinding)((Button)sender).Tag);

        private void RunCapture(HotkeyAction action)
        {
            var result = UiHelpers.RunCapture(this, _app, action);
            if (result != null) Show(action, result);
        }

        private void Commit(HotkeyAction action, HotkeyBinding binding)
            => Show(action, _app.CommitHotkey(action, binding));

        private void Show(HotkeyAction action, HotkeyCheckResult result)
        {
            var s = _app.Settings.Current;
            var saved = action == HotkeyAction.Convert ? s.Hotkey : s.ToggleHotkey;
            string message = UiHelpers.DescribeCommit(result, saved, out BannerKind kind);

            if (action == HotkeyAction.Convert)
                UiHelpers.ShowBanner(Banner, BannerText, kind, message);
            else
                UiHelpers.ShowBanner(ToggleBanner, ToggleBannerText, kind, message);
        }
    }
}
