using System.Text;
using System.Text.RegularExpressions;
using Nabra.Core.Models;

namespace Nabra.Core.Services;

public sealed record ConversionResult(string Text, TextDirection Direction, bool Changed, int ConvertedCharacters);

/// <summary>
/// Converts whole pieces of text. Works token by token (split on whitespace) so that spaces and
/// line breaks are preserved exactly, URLs / e-mail addresses / file paths are left alone, and
/// mixed-language text keeps the parts that do not need converting.
/// Pure local string processing: nothing is logged, stored or sent anywhere.
/// </summary>
public sealed class TextConversionService
{
    private static readonly Regex Splitter = new(@"(\s+)", RegexOptions.Compiled);

    private static readonly Regex ProtectedPattern = new(
        @"^(?:(?:https?|ftp|file)://\S+|www\.\S+|[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(?:\.[A-Za-z0-9\-]+)+|[A-Za-z]:\\\S*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly char[] WrapperChars =
        { '(', '[', '{', '<', '"', '\'', ')', ']', '}', '>', '.', ',', ';', ':', '!', '?' };

    private readonly KeyboardLayoutService _layout;
    private readonly DirectionDetectionService _detector;

    public TextConversionService(KeyboardLayoutService layout, DirectionDetectionService detector)
    {
        _layout = layout;
        _detector = detector;
    }

    public ConversionResult Convert(string? input, ConversionMode mode)
    {
        if (string.IsNullOrEmpty(input))
            return new ConversionResult(input ?? string.Empty, TextDirection.None, false, 0);

        var parts = Splitter.Split(input);

        var candidates = new List<string>();
        foreach (var p in parts)
            if (IsConvertibleToken(p)) candidates.Add(p);

        var direction = mode switch
        {
            ConversionMode.ToArabic => TextDirection.ToArabic,
            ConversionMode.ToEnglish => TextDirection.ToEnglish,
            _ => _detector.Detect(candidates),
        };

        if (direction == TextDirection.None)
            return new ConversionResult(input, TextDirection.None, false, 0);

        var sb = new StringBuilder(input.Length + 8);
        int converted = 0;
        foreach (var p in parts)
        {
            if (!IsConvertibleToken(p)) { sb.Append(p); continue; }

            var result = direction == TextDirection.ToArabic ? ConvertTokenToArabic(p) : ConvertTokenToEnglish(p);
            if (!string.Equals(result, p, StringComparison.Ordinal)) converted += p.Length;
            sb.Append(result);
        }

        var text = sb.ToString();
        bool changed = !string.Equals(text, input, StringComparison.Ordinal);
        return new ConversionResult(text, direction, changed, changed ? converted : 0);
    }

    private string ConvertTokenToArabic(string token)
        => _layout.ToArabic(token);

    private string ConvertTokenToEnglish(string token)
        => _layout.ToEnglish(token);

    private static bool IsConvertibleToken(string token)
        => token.Length > 0 && !char.IsWhiteSpace(token[0]) && !IsProtectedToken(token);

    /// <summary>URLs, e-mail addresses and Windows paths (ASCII only) are never converted.</summary>
    public static bool IsProtectedToken(string token)
    {
        var core = token.Trim(WrapperChars);
        return core.Length > 0 && ProtectedPattern.IsMatch(core);
    }
}
