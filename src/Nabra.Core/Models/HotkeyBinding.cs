using System.Text.Json.Serialization;

namespace Nabra.Core.Models;

/// <summary>
/// A keyboard shortcut: one or more modifiers plus one main key.
/// Serialised to JSON as { "modifiers": ["Control","Alt"], "key": "K" }.
/// </summary>
public sealed class HotkeyBinding : IEquatable<HotkeyBinding>
{
    public List<string> Modifiers { get; set; } = new();
    public string Key { get; set; } = string.Empty;

    [JsonIgnore] public HotkeyModifiers ModifierFlags => ParseModifiers(Modifiers);
    [JsonIgnore] public int VirtualKey => KeyCatalog.TryGetVirtualKey(Key, out var vk) ? vk : 0;

    public static HotkeyBinding Create(HotkeyModifiers modifiers, string key)
        => new() { Modifiers = ToModifierNames(modifiers), Key = KeyCatalog.Canonicalize(key) };

    public HotkeyBinding Clone() => new() { Modifiers = new List<string>(Modifiers), Key = Key };

    public static HotkeyModifiers ParseModifiers(IEnumerable<string>? names)
    {
        var flags = HotkeyModifiers.None;
        if (names == null) return flags;
        foreach (var raw in names)
        {
            switch ((raw ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "control":
                case "ctrl": flags |= HotkeyModifiers.Control; break;
                case "alt": flags |= HotkeyModifiers.Alt; break;
                case "shift": flags |= HotkeyModifiers.Shift; break;
                case "windows":
                case "win":
                case "meta": flags |= HotkeyModifiers.Win; break;
            }
        }
        return flags;
    }

    public static List<string> ToModifierNames(HotkeyModifiers m)
    {
        var list = new List<string>();
        if (m.HasFlag(HotkeyModifiers.Control)) list.Add("Control");
        if (m.HasFlag(HotkeyModifiers.Alt)) list.Add("Alt");
        if (m.HasFlag(HotkeyModifiers.Shift)) list.Add("Shift");
        if (m.HasFlag(HotkeyModifiers.Win)) list.Add("Windows");
        return list;
    }

    /// <summary>Display parts in canonical order: Ctrl, Alt, Shift, Win, key.</summary>
    public IReadOnlyList<string> GetDisplayParts()
    {
        var parts = new List<string>();
        var f = ModifierFlags;
        if (f.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (f.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (f.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (f.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        if (!string.IsNullOrEmpty(Key)) parts.Add(KeyCatalog.GetDisplay(Key));
        return parts;
    }

    public string ToDisplayString() => string.Join(" + ", GetDisplayParts());

    public bool Equals(HotkeyBinding? other)
    {
        if (other is null) return false;
        if (ModifierFlags != other.ModifierFlags) return false;
        int a = VirtualKey, b = other.VirtualKey;
        if (a != 0 || b != 0) return a == b;
        return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as HotkeyBinding);
    public override int GetHashCode() => HashCode.Combine((int)ModifierFlags, VirtualKey);
    public override string ToString() => ToDisplayString();
}
