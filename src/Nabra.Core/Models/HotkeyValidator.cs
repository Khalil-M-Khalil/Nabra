namespace Nabra.Core.Models;

public enum HotkeySeverity { Ok, Warning, Error }

/// <param name="MessageKey">Localisation key for the message to show the user (null when Ok).</param>
public readonly record struct HotkeyValidation(HotkeySeverity Severity, string? MessageKey);

/// <summary>
/// Static rules for shortcuts. This does NOT know whether another application already owns
/// the shortcut; that is detected by actually calling RegisterHotKey (HotkeyService).
/// </summary>
public static class HotkeyValidator
{
    // Combos that would break the system or basic editing everywhere. Never allowed.
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        "Control+C", "Control+V", "Control+X", "Control+Z", "Control+Y", "Control+A", "Control+S",
        "Alt+F4", "Alt+Space",
        "Control+Alt+Delete",
        "Win+L", "Win+D", "Win+E", "Win+R", "Win+V", "Win+Space",
        "Win+Shift+S",
    };

    public static HotkeyValidation Validate(HotkeyBinding? binding)
    {
        if (binding == null || !KeyCatalog.IsSupported(binding.Key))
            return new(HotkeySeverity.Error, "hk_err_key");

        var flags = binding.ModifierFlags;
        if (flags == HotkeyModifiers.None)
            return new(HotkeySeverity.Error, "hk_err_nomod");
        if (flags == HotkeyModifiers.Shift)
            return new(HotkeySeverity.Error, "hk_err_shiftonly");

        if (Blocked.Contains(Signature(flags, binding.Key)))
            return new(HotkeySeverity.Error, "hk_err_reserved");

        // Warnings: allowed, but the user should know what they are overriding.
        if (flags == HotkeyModifiers.Control && string.Equals(binding.Key, "K", StringComparison.OrdinalIgnoreCase))
            return new(HotkeySeverity.Warning, "hk_warn_word");

        if ((flags == HotkeyModifiers.Control || flags == HotkeyModifiers.Alt) && KeyCatalog.IsLetterOrDigit(binding.Key))
            return new(HotkeySeverity.Warning, "hk_warn_common");

        if (flags.HasFlag(HotkeyModifiers.Win))
            return new(HotkeySeverity.Warning, "hk_warn_win");

        return new(HotkeySeverity.Ok, null);
    }

    private static string Signature(HotkeyModifiers flags, string key)
    {
        var parts = new List<string>();
        if (flags.HasFlag(HotkeyModifiers.Control)) parts.Add("Control");
        if (flags.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (flags.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        if (flags.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(KeyCatalog.Canonicalize(key));
        return string.Join("+", parts);
    }
}
