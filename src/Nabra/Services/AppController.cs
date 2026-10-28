using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using Nabra.Core.Models;
using Nabra.Core.Services;
using Nabra.Localization;

namespace Nabra.Services
{
    internal sealed class HotkeyCheckResult
    {
        public HotkeyCheckResult(bool canSave, HotkeySeverity severity, string messageKey)
        {
            CanSave = canSave;
            Severity = severity;
            MessageKey = messageKey;
        }

        public bool CanSave { get; }
        public HotkeySeverity Severity { get; }
        public string MessageKey { get; }
    }

    /// <summary>
    /// The single place that owns all services and applies settings to the running app
    /// (hotkeys, tray, theme, language, start-with-Windows). Views talk only to this class.
    /// </summary>
    internal sealed class AppController : IDisposable
    {
        private readonly App _app;
        private readonly SelectionConverter _selection;
        private bool _started;
        private bool _firstHideShown;

        public AppController(App app)
        {
            _app = app;
            Settings = new SettingsService();
            Layout = new KeyboardLayoutService();
            Text = new TextConversionService(Layout, new DirectionDetectionService());
            Hotkeys = new HotkeyService();
            Theme = new ThemeService();
            Tray = new TrayService();
            RealTime = new RealTimeCorrectionService(Text, Layout);
            RealTime.WordConverted += OnRealTimeWordConverted;
            _selection = new SelectionConverter(new ClipboardService(), Text);
        }

        public SettingsService Settings { get; }
        public KeyboardLayoutService Layout { get; }
        public TextConversionService Text { get; }
        public HotkeyService Hotkeys { get; }
        public ThemeService Theme { get; }
        public TrayService Tray { get; }
        public RealTimeCorrectionService RealTime { get; }

        public HotkeyRegistrationResult ConvertRegistration { get; private set; } = HotkeyRegistrationResult.Ok;
        public HotkeyRegistrationResult ToggleRegistration { get; private set; } = HotkeyRegistrationResult.Ok;

        public ConversionOutcome? LastOutcome { get; private set; }
        public DateTime? LastOutcomeTime { get; private set; }

        /// <summary>Raised after settings were applied (UI should refresh itself).</summary>
        public event Action? SettingsApplied;

        /// <summary>Raised after every conversion attempt.</summary>
        public event Action<ConversionOutcome>? ConversionFinished;

        public void Start()
        {
            var s = Settings.Load();
            Loc.Instance.SetLanguage(s.Language);
            Theme.Apply(s);
            SyncStartup(s);

            Hotkeys.HotkeyPressed += OnHotkeyPressed;
            ApplyHotkeys();

            Tray.Initialize(this);
            Tray.Refresh(s);
            RealTime.Refresh(s);

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
            _started = true;
        }

        // ---------------------------------------------------------------- settings

        public void Update(Action<AppSettings> mutate)
        {
            Settings.Update(mutate);
            ApplyAll();
        }

        public void ResetSettings()
        {
            Settings.ResetToDefaults();
            ApplyAll();
        }

        public void SetEnabled(bool enabled)
        {
            if (Settings.Current.IsEnabled == enabled) return;
            Update(s => s.IsEnabled = enabled);
            Tray.ShowBalloon(Loc.T(enabled ? "toast_enabled" : "toast_disabled"), false);
        }

        public void SetMode(ConversionMode mode) => Update(s => s.ConversionMode = mode);
        public void SetRealTime(bool enabled) => Update(s => s.RealTimeCorrection = enabled);
        public void SetRealTimeIdleSeconds(double seconds) => Update(s => s.RealTimeIdleSeconds = seconds);
        public void SetRealTimeSmartFilter(bool enabled) => Update(s => s.RealTimeSmartFilter = enabled);

        private void ApplyAll()
        {
            var s = Settings.Current;
            Loc.Instance.SetLanguage(s.Language);
            Theme.Apply(s);
            SyncStartup(s);
            ApplyHotkeys();
            RealTime.Refresh(s);
            Tray.Refresh(s);
            SettingsApplied?.Invoke();
        }

        private static void SyncStartup(AppSettings s)
        {
            if (StartupService.IsEnabled() != s.StartWithWindows)
                StartupService.Set(s.StartWithWindows);
        }

        private void OnSystemPreferenceChanged(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (e.Category != Microsoft.Win32.UserPreferenceCategory.General) return;
            if (Settings.Current.Theme != ThemeMode.System) return;
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Theme.Apply(Settings.Current);
                SettingsApplied?.Invoke();
            }));
        }

        // ---------------------------------------------------------------- hotkeys

        /// <summary>(Re)registers the shortcuts to match the current settings.</summary>
        public void ApplyHotkeys()
        {
            var s = Settings.Current;

            if (s.IsEnabled)
            {
                ConvertRegistration = Hotkeys.Register(HotkeyAction.Convert, s.Hotkey);
            }
            else
            {
                // While Nabra is off, the shortcut is released so other apps can use it again.
                Hotkeys.Unregister(HotkeyAction.Convert);
                ConvertRegistration = HotkeyRegistrationResult.Ok;
            }

            if (s.ToggleHotkey != null)
            {
                ToggleRegistration = Hotkeys.Register(HotkeyAction.Toggle, s.ToggleHotkey);
            }
            else
            {
                Hotkeys.Unregister(HotkeyAction.Toggle);
                ToggleRegistration = HotkeyRegistrationResult.Ok;
            }
        }

        /// <summary>Releases all shortcuts, e.g. while the capture dialog is open.</summary>
        public void SuspendHotkeys() => Hotkeys.UnregisterAll();

        public void ResumeHotkeys()
        {
            ApplyHotkeys();
            SettingsApplied?.Invoke();
        }

        /// <summary>
        /// Full check of a candidate shortcut: static rules, duplicate with the other Nabra shortcut,
        /// and a real test registration with Windows. Call with hotkeys suspended.
        /// </summary>
        public HotkeyCheckResult CheckCandidate(HotkeyAction action, HotkeyBinding? candidate)
        {
            var rule = HotkeyValidator.Validate(candidate);
            if (rule.Severity == HotkeySeverity.Error)
                return new HotkeyCheckResult(false, HotkeySeverity.Error, rule.MessageKey ?? "hk_err_key");

            var other = action == HotkeyAction.Convert ? Settings.Current.ToggleHotkey : Settings.Current.Hotkey;
            if (other != null && other.Equals(candidate))
                return new HotkeyCheckResult(false, HotkeySeverity.Error, "hk_err_duplicate");

            var probe = Hotkeys.Probe(candidate!);
            if (!probe.Success)
                return new HotkeyCheckResult(false, HotkeySeverity.Error, probe.InUse ? "hk_err_inuse" : "hk_err_register");

            if (rule.Severity == HotkeySeverity.Warning)
                return new HotkeyCheckResult(true, HotkeySeverity.Warning, rule.MessageKey ?? "hk_ok");

            return new HotkeyCheckResult(true, HotkeySeverity.Ok, "hk_ok");
        }

        /// <summary>Checks, saves and activates a new shortcut. Returns what happened.</summary>
        public HotkeyCheckResult CommitHotkey(HotkeyAction action, HotkeyBinding candidate)
        {
            Hotkeys.UnregisterAll();
            HotkeyCheckResult check;
            try
            {
                check = CheckCandidate(action, candidate);
                if (check.CanSave)
                {
                    var copy = candidate.Clone();
                    Settings.Update(s =>
                    {
                        if (action == HotkeyAction.Convert) s.Hotkey = copy;
                        else s.ToggleHotkey = copy;
                    });
                }
            }
            finally
            {
                ApplyAll();
            }

            // Activation can still fail in a race with another program; never report a false success.
            var reg = action == HotkeyAction.Convert ? ConvertRegistration : ToggleRegistration;
            if (check.CanSave && !reg.Success)
                return new HotkeyCheckResult(false, HotkeySeverity.Error, reg.InUse ? "hk_err_inuse" : "hk_err_register");
            return check;
        }

        public void ClearToggleHotkey() => Update(s => s.ToggleHotkey = null);

        // ---------------------------------------------------------------- conversion

        private void OnHotkeyPressed(HotkeyAction action)
        {
            if (action == HotkeyAction.Toggle)
            {
                SetEnabled(!Settings.Current.IsEnabled);
                return;
            }

            if (Settings.Current.IsEnabled) _ = RunConversionAsync();
        }

        private async Task RunConversionAsync()
        {
            var settings = Settings.Current;
            var outcome = await _selection.RunAsync(settings);
            if (outcome.Kind == OutcomeKind.Busy) return;

            LastOutcome = outcome;
            LastOutcomeTime = DateTime.Now;

            if (outcome.Kind == OutcomeKind.Success && settings.PlaySound)
            {
                try { SystemSounds.Asterisk.Play(); } catch (Exception) { }
            }

            if (settings.ShowNotifications)
            {
                string? message = outcome.Kind switch
                {
                    OutcomeKind.Success => Loc.T(outcome.Direction == TextDirection.ToArabic ? "toast_ok_ar" : "toast_ok_en"),
                    OutcomeKind.NoSelection => Loc.T("toast_no_selection"),
                    OutcomeKind.NothingToConvert => Loc.T("toast_nothing"),
                    _ => Loc.T("toast_error"),
                };
                Tray.ShowBalloon(message, outcome.Kind != OutcomeKind.Success);
            }

            ConversionFinished?.Invoke(outcome);
        }

        private void OnRealTimeWordConverted(string from, string to)
        {
            var res = Text.Convert(from, Settings.Current.ConversionMode);
            var outcome = new ConversionOutcome(OutcomeKind.Success, res.Direction, to.Length);
            LastOutcome = outcome;
            LastOutcomeTime = DateTime.Now;

            if (Settings.Current.PlaySound)
            {
                try { SystemSounds.Asterisk.Play(); } catch (Exception) { }
            }

            ConversionFinished?.Invoke(outcome);
        }

        // ---------------------------------------------------------------- windows / lifetime

        public void OpenWindow(string page, bool beginHotkeyChange) => _app.ShowMainWindow(page, beginHotkeyChange);

        public void RequestExit() => _app.ExitApplication();

        public void NotifyHiddenToTrayOnce()
        {
            if (_firstHideShown) return;
            _firstHideShown = true;
            Tray.ShowBalloon(Loc.T("toast_in_tray"), false);
        }

        public void Dispose()
        {
            if (_started)
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
            Hotkeys.HotkeyPressed -= OnHotkeyPressed;
            RealTime.WordConverted -= OnRealTimeWordConverted;
            RealTime.Dispose();
            Hotkeys.Dispose();
            Tray.Dispose();
        }
    }
}
