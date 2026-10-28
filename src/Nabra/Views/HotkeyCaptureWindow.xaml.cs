using System.Windows;
using System.Windows.Input;
using Nabra.Core.Models;
using Nabra.Localization;
using Nabra.Services;

namespace Nabra.Views
{
    /// <summary>
    /// "Press the new key combination now." Only key presses inside this dialog are looked at;
    /// nothing is captured globally.
    /// </summary>
    public partial class HotkeyCaptureWindow : Window
    {
        private readonly AppController _app;
        private readonly HotkeyAction _action;
        private HotkeyBinding? _candidate;

        internal HotkeyCaptureWindow(AppController app, HotkeyAction action)
        {
            _app = app;
            _action = action;
            InitializeComponent();
            Loaded += (s, e) => Focus();
        }

        /// <summary>The combination the user accepted (null if cancelled).</summary>
        public HotkeyBinding? Result { get; private set; }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true; // keep Alt from opening a system menu, Space from clicking a button, etc.
            if (e.IsRepeat) return;

            Key key = e.Key;
            if (key == Key.System) key = e.SystemKey;
            if (key == Key.ImeProcessed) key = e.ImeProcessedKey;
            if (key == Key.DeadCharProcessed) key = e.DeadCharProcessedKey;

            var mods = CurrentModifiers();

            if (IsModifierKey(key))
            {
                _candidate = null;
                SaveButton.IsEnabled = false;
                UiHelpers.HideBanner(Banner);
                ShowModifiersOnly(mods);
                return;
            }

            if (key == Key.Escape && mods == HotkeyModifiers.None)
            {
                DialogResult = false;
                return;
            }

            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (!KeyCatalog.TryGetName(vk, out string name))
            {
                _candidate = null;
                SaveButton.IsEnabled = false;
                ShowModifiersOnly(mods);
                UiHelpers.ShowBanner(Banner, BannerText, BannerKind.Error, Loc.T("hk_err_key"));
                return;
            }

            var binding = HotkeyBinding.Create(mods, name);
            _candidate = binding;
            Placeholder.Visibility = Visibility.Collapsed;
            UiHelpers.FillChips(Chips, binding);

            var check = _app.CheckCandidate(_action, binding);
            string text = check.CanSave && check.Severity == HotkeySeverity.Ok
                ? Loc.T("hk_ok")
                : Loc.T(check.MessageKey);
            UiHelpers.ShowBanner(Banner, BannerText, UiHelpers.ToBanner(check.Severity), text);
            SaveButton.IsEnabled = check.CanSave;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (_candidate != null) return; // keep showing the captured combination

            var mods = CurrentModifiers();
            if (mods == HotkeyModifiers.None)
            {
                Chips.Children.Clear();
                Placeholder.Visibility = Visibility.Visible;
            }
            else
            {
                ShowModifiersOnly(mods);
            }
        }

        private void ShowModifiersOnly(HotkeyModifiers mods)
        {
            if (mods == HotkeyModifiers.None)
            {
                Chips.Children.Clear();
                Placeholder.Visibility = Visibility.Visible;
                return;
            }

            Placeholder.Visibility = Visibility.Collapsed;
            UiHelpers.FillChips(Chips, HotkeyBinding.Create(mods, string.Empty));
        }

        private static HotkeyModifiers CurrentModifiers() => (HotkeyModifiers)(int)Keyboard.Modifiers;

        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_candidate == null) return;
            Result = _candidate;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
