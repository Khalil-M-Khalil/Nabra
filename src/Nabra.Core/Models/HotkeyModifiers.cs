namespace Nabra.Core.Models;

/// <summary>
/// Modifier flags. The numeric values intentionally match the Win32 MOD_* constants
/// (and WPF's ModifierKeys), so they can be passed straight to RegisterHotKey.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
