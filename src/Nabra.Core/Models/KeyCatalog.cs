namespace Nabra.Core.Models;

/// <summary>
/// The set of keys Nabra allows as the "main" key of a shortcut, with their Win32
/// virtual-key codes and the display text shown in the UI.
/// </summary>
public static class KeyCatalog
{
    private static readonly Dictionary<string, int> ByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> ByVk = new();
    private static readonly Dictionary<string, string> Display = new(StringComparer.OrdinalIgnoreCase);

    static KeyCatalog()
    {
        for (char c = 'A'; c <= 'Z'; c++) Add(c.ToString(), c, c.ToString());
        for (char c = '0'; c <= '9'; c++) Add(c.ToString(), c, c.ToString());
        for (int i = 1; i <= 24; i++) Add("F" + i, 0x6F + i, "F" + i);
        for (int i = 0; i <= 9; i++) Add("Num" + i, 0x60 + i, "Num " + i);

        Add("Space", 0x20, "Space");
        Add("Enter", 0x0D, "Enter");
        Add("Insert", 0x2D, "Insert");
        Add("Delete", 0x2E, "Delete");
        Add("Home", 0x24, "Home");
        Add("End", 0x23, "End");
        Add("PageUp", 0x21, "PgUp");
        Add("PageDown", 0x22, "PgDn");
        Add("Left", 0x25, "\u2190");
        Add("Up", 0x26, "\u2191");
        Add("Right", 0x27, "\u2192");
        Add("Down", 0x28, "\u2193");

        Add("Oem1", 0xBA, ";");
        Add("OemPlus", 0xBB, "=");
        Add("OemComma", 0xBC, ",");
        Add("OemMinus", 0xBD, "-");
        Add("OemPeriod", 0xBE, ".");
        Add("Oem2", 0xBF, "/");
        Add("Oem3", 0xC0, "`");
        Add("Oem4", 0xDB, "[");
        Add("Oem5", 0xDC, "\\");
        Add("Oem6", 0xDD, "]");
        Add("Oem7", 0xDE, "'");
    }

    private static void Add(string name, int vk, string display)
    {
        ByName[name] = vk;
        ByVk[vk] = name;
        Display[name] = display;
    }

    public static bool TryGetVirtualKey(string? name, out int vk)
    {
        vk = 0;
        return !string.IsNullOrWhiteSpace(name) && ByName.TryGetValue(name.Trim(), out vk);
    }

    public static bool TryGetName(int vk, out string name)
    {
        if (ByVk.TryGetValue(vk, out var n)) { name = n; return true; }
        name = string.Empty;
        return false;
    }

    public static bool IsSupported(string? name) => TryGetVirtualKey(name, out _);

    public static string GetDisplay(string name)
        => Display.TryGetValue(name ?? string.Empty, out var d) ? d : (name ?? string.Empty);

    /// <summary>Returns the canonical spelling of a key name ("k" becomes "K").</summary>
    public static string Canonicalize(string? name)
    {
        if (TryGetVirtualKey(name, out var vk) && ByVk.TryGetValue(vk, out var canonical)) return canonical;
        return name?.Trim() ?? string.Empty;
    }

    public static bool IsLetterOrDigit(string name)
        => name.Length == 1 && char.IsAsciiLetterOrDigit(name[0]);
}
