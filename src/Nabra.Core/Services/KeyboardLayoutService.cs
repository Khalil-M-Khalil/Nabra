using System.Text;

namespace Nabra.Core.Services;

/// <summary>
/// Maps characters between the US-English (QWERTY) layout and Windows "Arabic (101)" by
/// physical key position. Edit the <see cref="Layout"/> table to adjust the mapping; both
/// directions are derived from it.
///
/// Notes on design:
///  - Digits and shared symbols (! @ # $ % ^ &amp; * ( ) - _ = + \ | : ") are identical on both
///    layouts, so they are intentionally absent and never change.
///  - Several Arabic letters live on punctuation keys (ك on ';', ط on ''', و on ',', ز on '.',
///    ظ on '/', ج on '[', د on ']', ذ on '`'), so those punctuation keys convert too.
///  - Key B produces the two-character sequence lam+alef; the shifted lam-alef variants too.
/// </summary>
public sealed class KeyboardLayoutService
{
    /// <summary>(character typed on the English layout, text the same key gives on Arabic 101)</summary>
    private static readonly (string En, string Ar)[] Layout =
    {
        // ---- unshifted ----
        ("`", "ذ"),
        ("q", "ض"), ("w", "ص"), ("e", "ث"), ("r", "ق"), ("t", "ف"), ("y", "غ"), ("u", "ع"), ("i", "ه"), ("o", "خ"), ("p", "ح"), ("[", "ج"), ("]", "د"),
        ("a", "ش"), ("s", "س"), ("d", "ي"), ("f", "ب"), ("g", "ل"), ("h", "ا"), ("j", "ت"), ("k", "ن"), ("l", "م"), (";", "ك"), ("'", "ط"),
        ("z", "ئ"), ("x", "ء"), ("c", "ؤ"), ("v", "ر"), ("b", "\u0644\u0627"), ("n", "ى"), ("m", "ة"), (",", "و"), (".", "ز"), ("/", "ظ"),

        // ---- shifted ----
        ("~", "\u0651"),
        ("Q", "\u064E"), ("W", "\u064B"), ("E", "\u064F"), ("R", "\u064C"), ("T", "\u0644\u0625"), ("Y", "\u0625"), ("U", "\u2018"), ("I", "\u00F7"), ("O", "\u00D7"), ("P", "\u061B"), ("{", "<"), ("}", ">"),
        ("A", "\u0650"), ("S", "\u064D"), ("D", "]"), ("F", "["), ("G", "\u0644\u0623"), ("H", "\u0623"), ("J", "\u0640"), ("K", "\u060C"), ("L", "/"),
        ("Z", "~"), ("X", "\u0652"), ("C", "}"), ("V", "{"), ("B", "\u0644\u0622"), ("N", "\u0622"), ("M", "\u2019"), ("<", ","), (">", "."), ("?", "\u061F"),
    };

    private readonly Dictionary<char, string> _enToAr = new();
    private readonly Dictionary<char, char> _arToEn = new();
    private readonly Dictionary<string, char> _ligatures = new();

    public KeyboardLayoutService()
    {
        foreach (var (en, ar) in Layout)
        {
            if (en.Length != 1)
                throw new InvalidOperationException("Layout key must be a single character: " + en);

            _enToAr[en[0]] = ar;

            if (ar.Length == 1)
            {
                // Only non-ASCII Arabic output is reversible. ASCII output (e.g. "/" from Shift+L)
                // is indistinguishable from ordinary punctuation, so it is never converted back.
                if (ar[0] > 127 && !_arToEn.ContainsKey(ar[0])) _arToEn[ar[0]] = en[0];
            }
            else
            {
                _ligatures[ar] = en[0];
            }
        }

        // Presentation-form lam-alef ligatures (appear if text was pasted from a shaped source).
        _arToEn['\uFEFB'] = 'b';
        _arToEn['\uFEF7'] = 'G';
        _arToEn['\uFEF9'] = 'T';
        _arToEn['\uFEF5'] = 'B';

        // Arabic-Indic digits typed with national digit shapes enabled.
        for (int i = 0; i < 10; i++) _arToEn[(char)('\u0660' + i)] = (char)('0' + i);
    }

    public static IReadOnlyList<(string En, string Ar)> Entries => Layout;

    public static bool IsLatinLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

    public static bool IsArabicLetter(char c)
        => char.IsLetter(c) &&
           ((c >= '\u0600' && c <= '\u06FF') || (c >= '\u0750' && c <= '\u077F') ||
            (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'));

    /// <summary>True if this character is something the Arabic-to-English direction can convert.</summary>
    public bool IsArabicSource(char c) => _arToEn.ContainsKey(c);

    /// <summary>English-layout text to what the same keystrokes give on the Arabic layout.</summary>
    public string ToArabic(string text)
    {
        var sb = new StringBuilder(text.Length + 4);
        foreach (var c in text)
        {
            if (_enToAr.TryGetValue(c, out var ar)) sb.Append(ar);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Arabic-layout text to what the same keystrokes give on the English layout.</summary>
    public string ToEnglish(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && _ligatures.TryGetValue(text.Substring(i, 2), out var lig))
            {
                sb.Append(lig);
                i++;
            }
            else if (_arToEn.TryGetValue(text[i], out var en))
            {
                sb.Append(en);
            }
            else
            {
                sb.Append(text[i]);
            }
        }
        return sb.ToString();
    }
}
