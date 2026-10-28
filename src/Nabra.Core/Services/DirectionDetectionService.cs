namespace Nabra.Core.Services;

public enum TextDirection { None, ToArabic, ToEnglish }

/// <summary>
/// Decides which way to convert by script: mostly Latin letters means the text was typed on the
/// English layout (convert to Arabic); mostly Arabic letters means the opposite.
/// </summary>
public sealed class DirectionDetectionService
{
    public TextDirection Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return TextDirection.None;
        return Detect(text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public TextDirection Detect(IEnumerable<string> tokens)
    {
        int latin = 0, arabic = 0;
        foreach (var token in tokens)
        {
            foreach (var c in token)
            {
                if (KeyboardLayoutService.IsLatinLetter(c)) latin++;
                else if (KeyboardLayoutService.IsArabicLetter(c)) arabic++;
            }
        }

        if (latin == 0 && arabic == 0) return TextDirection.None;
        return arabic > latin ? TextDirection.ToEnglish : TextDirection.ToArabic;
    }
}
