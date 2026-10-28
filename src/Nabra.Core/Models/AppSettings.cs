using System.Text.RegularExpressions;

namespace Nabra.Core.Models;

public enum ConversionMode { Auto, ToArabic, ToEnglish }
public enum ThemeMode { System, Light, Dark }
public enum SpeedProfile { Fast, Normal, Relaxed }

public readonly record struct SpeedTimings(int CopyTimeoutMs, int PasteDelayMs);

public static class SpeedProfileExtensions
{
    public static SpeedTimings GetTimings(this SpeedProfile profile) => profile switch
    {
        SpeedProfile.Fast => new SpeedTimings(700, 120),
        SpeedProfile.Relaxed => new SpeedTimings(1800, 450),
        _ => new SpeedTimings(1000, 220),
    };
}

/// <summary>
/// Everything the user can customise. Stored as local JSON only (no database, no cloud).
/// </summary>
public sealed class AppSettings
{
    public const string DefaultAccent = "#0E9F8E";
    private static readonly Regex ColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public int Version { get; set; } = 1;

    /// <summary>The conversion shortcut. Default: Ctrl + Alt + K.</summary>
    public HotkeyBinding Hotkey { get; set; } = DefaultHotkey();

    /// <summary>Optional second shortcut that turns Nabra on/off. Null = not set.</summary>
    public HotkeyBinding? ToggleHotkey { get; set; }

    public ConversionMode ConversionMode { get; set; } = ConversionMode.Auto;
    public bool IsEnabled { get; set; } = true;
    public bool RestoreClipboard { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool PlaySound { get; set; }

    /// <summary>"ar" or "en".</summary>
    public string Language { get; set; } = "ar";
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public string AccentColor { get; set; } = DefaultAccent;
    public double UiScale { get; set; } = 1.0;
    public SpeedProfile Speed { get; set; } = SpeedProfile.Normal;

    /// <summary>Optional real-time typing correction upon Space/Tab or idle timeout.</summary>
    public bool RealTimeCorrection { get; set; } = false;

    /// <summary>Idle timeout in seconds before auto-correcting the unfinished word. Default: 2.0.</summary>
    public double RealTimeIdleSeconds { get; set; } = 2.0;

    /// <summary>When auto-detecting, ignore valid common English words to avoid accidental conversions.</summary>
    public bool RealTimeSmartFilter { get; set; } = true;

    /// <summary>Also convert before Enter key when finishing a line or message.</summary>
    public bool RealTimeOnEnter { get; set; } = true;

    public static HotkeyBinding DefaultHotkey()
        => HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, "K");

    public static bool IsValidColor(string? value) => value != null && ColorRegex.IsMatch(value);

    /// <summary>Repairs any invalid value (hand-edited or corrupt file) with a safe default.</summary>
    public void Normalize()
    {
        Hotkey ??= DefaultHotkey();
        if (HotkeyValidator.Validate(Hotkey).Severity == HotkeySeverity.Error)
            Hotkey = DefaultHotkey();

        if (ToggleHotkey != null &&
            (HotkeyValidator.Validate(ToggleHotkey).Severity == HotkeySeverity.Error || ToggleHotkey.Equals(Hotkey)))
            ToggleHotkey = null;

        if (Language != "ar" && Language != "en") Language = "ar";
        if (!IsValidColor(AccentColor)) AccentColor = DefaultAccent;
        if (double.IsNaN(UiScale) || double.IsInfinity(UiScale)) UiScale = 1.0;
        UiScale = Math.Clamp(UiScale, 0.85, 1.4);

        if (double.IsNaN(RealTimeIdleSeconds) || RealTimeIdleSeconds <= 0) RealTimeIdleSeconds = 2.0;
        RealTimeIdleSeconds = Math.Clamp(RealTimeIdleSeconds, 0.5, 10.0);
        
        // Force disable Real-Time Correction until it's officially released
        RealTimeCorrection = false;
        
        // Force lock ConversionMode to Auto to simplify the app and prevent confusion
        ConversionMode = ConversionMode.Auto;
    }
}
