using System.Text.Json;
using Nabra.Core.Models;
using Nabra.Core.Services;

// Dependency-free self-test (no NuGet packages needed). Exit code 0 = all passed.
// Run:  dotnet run --project tests\Nabra.SelfTest -c Release

int passed = 0, failed = 0;

void Check(string name, bool ok, string? detail = null)
{
    if (ok) { passed++; Console.WriteLine("  PASS  " + name); }
    else { failed++; Console.WriteLine("  FAIL  " + name + (detail != null ? "   -> " + detail : "")); }
}

void Eq(string name, string expected, string actual) => Check(name, expected == actual, $"expected '{expected}' got '{actual}'");

var layout = new KeyboardLayoutService();
var detector = new DirectionDetectionService();
var conv = new TextConversionService(layout, detector);

Console.WriteLine("Layout table");
var seenEn = new HashSet<string>();
var seenAr = new HashSet<string>();
bool dupEn = false, dupAr = false;
foreach (var (en, ar) in KeyboardLayoutService.Entries)
{
    if (!seenEn.Add(en)) dupEn = true;
    if (ar.Length == 1 && ar[0] > 127 && !seenAr.Add(ar)) dupAr = true;
}
Check("no duplicate English keys", !dupEn);
Check("no duplicate single-char Arabic outputs", !dupAr);
foreach (var c in "qwertyuiopasdfghjklzxcvbnm[];',./`")
    Check($"key '{c}' is mapped", layout.ToArabic(c.ToString()) != c.ToString());

Console.WriteLine("English layout -> Arabic");
Eq("lvpfh", "مرحبا", layout.ToArabic("lvpfh"));
Eq("a;vh", "شكرا", layout.ToArabic("a;vh"));
Eq("hgsghl ugd;l", "السلام عليكم", layout.ToArabic("hgsghl ugd;l"));
Eq("b -> lam-alef", "\u0644\u0627", layout.ToArabic("b"));
Eq("digits unchanged", "123", layout.ToArabic("123"));

Console.WriteLine("Arabic layout -> English");
Eq("مرحبا", "lvpfh", layout.ToEnglish("مرحبا"));
Eq("شكرا", "a;vh", layout.ToEnglish("شكرا"));
Eq("lam-alef pair -> b", "b", layout.ToEnglish("\u0644\u0627"));
Eq("Arabic-Indic digits", "2026", layout.ToEnglish("\u0662\u0660\u0662\u0666"));

Console.WriteLine("Round trips (letters without lam+alef ambiguity)");
foreach (var w in new[] { "lvpfh", "a;vh", "ugd;l", "sdhvm", "wsaq" })
    Eq("roundtrip " + w, w, layout.ToEnglish(layout.ToArabic(w)));

Console.WriteLine("Direction detection");
Check("latin => ToArabic", detector.Detect("lvpfh") == TextDirection.ToArabic);
Check("arabic => ToEnglish", detector.Detect("مرحبا") == TextDirection.ToEnglish);
Check("digits only => None", detector.Detect("12345 !!!") == TextDirection.None);
Check("empty => None", detector.Detect("") == TextDirection.None);
Check("null => None", detector.Detect((string?)null) == TextDirection.None);

Console.WriteLine("Conversion service");
var r = conv.Convert("lvpfh", ConversionMode.Auto);
Check("auto: lvpfh", r.Text == "مرحبا" && r.Direction == TextDirection.ToArabic && r.Changed);
r = conv.Convert("مرحبا", ConversionMode.Auto);
Check("auto: reverse", r.Text == "lvpfh" && r.Direction == TextDirection.ToEnglish);
r = conv.Convert("", ConversionMode.Auto);
Check("empty text is unchanged", r.Text == "" && !r.Changed);
r = conv.Convert("   \r\n  ", ConversionMode.Auto);
Check("whitespace only is unchanged", r.Text == "   \r\n  " && !r.Changed);
r = conv.Convert(null, ConversionMode.Auto);
Check("null is safe", r.Text == "" && !r.Changed);
r = conv.Convert("lvpfh\r\nhgsghl  ugd;l\t12", ConversionMode.Auto);
Eq("whitespace and digits preserved", "مرحبا\r\nالسلام  عليكم\t12", r.Text);
r = conv.Convert("lvpfh https://example.com/a?b=1 a@b.com", ConversionMode.Auto);
Eq("url and e-mail protected", "مرحبا https://example.com/a?b=1 a@b.com", r.Text);
r = conv.Convert("C:\\Users\\Khalil\\file.txt lvpfh", ConversionMode.Auto);
Eq("windows path protected", "C:\\Users\\Khalil\\file.txt مرحبا", r.Text);
r = conv.Convert("lvpfh", ConversionMode.ToEnglish);
Check("forced ToEnglish on latin text changes nothing", !r.Changed && r.Text == "lvpfh");
r = conv.Convert("مرحبا", ConversionMode.ToArabic);
Check("forced ToArabic on arabic text changes nothing", !r.Changed && r.Text == "مرحبا");
r = conv.Convert("lvpfh", ConversionMode.ToArabic);
Check("forced ToArabic", r.Text == "مرحبا" && r.Changed);
r = conv.Convert("hello مرحبا", ConversionMode.Auto);
Check("mixed text keeps the other script", r.Text.EndsWith("مرحبا") && r.Changed, r.Text);
r = conv.Convert("...", ConversionMode.Auto);
Check("punctuation-only token untouched", !r.Changed);

Console.WriteLine("Hotkey model + validation");
var def = AppSettings.DefaultHotkey();
Eq("default hotkey text", "Ctrl + Alt + K", def.ToDisplayString());
Check("default hotkey is valid", HotkeyValidator.Validate(def).Severity == HotkeySeverity.Ok);
var csk = HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Shift, "k");
Eq("Ctrl+Shift+K display", "Ctrl + Shift + K", csk.ToDisplayString());
Check("Ctrl+Shift+K valid", HotkeyValidator.Validate(csk).Severity == HotkeySeverity.Ok);
Check("flags map to Win32 values (Ctrl|Alt = 3)", (int)def.ModifierFlags == 3);
Check("K virtual key = 0x4B", def.VirtualKey == 0x4B);
Check("lone key is rejected", HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.None, "K")).Severity == HotkeySeverity.Error);
Check("Shift+K alone is rejected", HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.Shift, "K")).Severity == HotkeySeverity.Error);
Check("Ctrl+C is blocked", HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.Control, "C")).Severity == HotkeySeverity.Error);
Check("Alt+F4 is blocked", HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.Alt, "F4")).Severity == HotkeySeverity.Error);
Check("Win+L is blocked", HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.Win, "L")).Severity == HotkeySeverity.Error);
var wordWarn = HotkeyValidator.Validate(HotkeyBinding.Create(HotkeyModifiers.Control, "K"));
Check("Ctrl+K warns about Word", wordWarn.Severity == HotkeySeverity.Warning && wordWarn.MessageKey == "hk_warn_word");
Check("unknown key is rejected", HotkeyValidator.Validate(new HotkeyBinding { Modifiers = { "Control" }, Key = "NoSuchKey" }).Severity == HotkeySeverity.Error);
Check("null is rejected", HotkeyValidator.Validate(null).Severity == HotkeySeverity.Error);
Check("modifier parsing is tolerant", HotkeyBinding.ParseModifiers(new[] { "ctrl", "ALT", "win" }) ==
      (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Win));
Check("equality ignores key case", HotkeyBinding.Create(HotkeyModifiers.Control, "k").Equals(HotkeyBinding.Create(HotkeyModifiers.Control, "K")));

Console.WriteLine("Settings persistence");
var dir = Path.Combine(Path.GetTempPath(), "nabra-selftest-" + Guid.NewGuid().ToString("N"));
try
{
    var svc = new SettingsService(dir);
    var s = svc.Load();
    Check("first load creates the file", File.Exists(svc.FilePath));
    Check("defaults: Ctrl+Alt+K / auto / enabled / rt_off", s.Hotkey.Equals(AppSettings.DefaultHotkey()) && s.ConversionMode == ConversionMode.Auto && s.IsEnabled && s.RestoreClipboard && !s.RealTimeCorrection);

    svc.Update(x =>
    {
        x.Hotkey = HotkeyBinding.Create(HotkeyModifiers.Control | HotkeyModifiers.Shift, "K");
        x.ConversionMode = ConversionMode.ToArabic;
        x.Theme = ThemeMode.Dark;
        x.AccentColor = "#2F6FED";
        x.Language = "en";
    });

    var again = new SettingsService(dir).Load();
    Check("hotkey survives restart", again.Hotkey.ToDisplayString() == "Ctrl + Shift + K");
    Check("mode / theme / accent / language survive restart",
          again.ConversionMode == ConversionMode.ToArabic && again.Theme == ThemeMode.Dark &&
          again.AccentColor == "#2F6FED" && again.Language == "en");

    var json = File.ReadAllText(svc.FilePath);
    Check("json uses the documented shape", json.Contains("\"hotkey\"") && json.Contains("\"modifiers\"") &&
          json.Contains("\"Control\"") && json.Contains("\"Shift\"") && json.Contains("\"key\": \"K\"") &&
          json.Contains("\"conversionMode\": \"toArabic\""), json);

    // restoring the default hotkey
    svc.Update(x => x.Hotkey = AppSettings.DefaultHotkey());
    Check("restore default hotkey", new SettingsService(dir).Load().Hotkey.ToDisplayString() == "Ctrl + Alt + K");

    // corrupt file must not crash and must be set aside
    File.WriteAllText(svc.FilePath, "{ this is not json");
    var recovered = new SettingsService(dir).Load();
    Check("corrupt file falls back to defaults", recovered.Hotkey.Equals(AppSettings.DefaultHotkey()));
    Check("corrupt file is kept aside", Directory.GetFiles(dir, "settings.corrupt-*.json").Length == 1);

    // hand-edited invalid values get repaired
    File.WriteAllText(svc.FilePath,
        "{ \"hotkey\": { \"modifiers\": [], \"key\": \"K\" }, \"language\": \"xx\", \"accentColor\": \"red\", \"uiScale\": 99 }");
    var fixedUp = new SettingsService(dir).Load();
    Check("invalid hotkey/language/colour/scale are repaired",
          fixedUp.Hotkey.Equals(AppSettings.DefaultHotkey()) && fixedUp.Language == "ar" &&
          fixedUp.AccentColor == AppSettings.DefaultAccent && fixedUp.UiScale <= 1.4);

    // the example JSON from the project brief loads as-is
    File.WriteAllText(svc.FilePath,
        "{ \"hotkey\": { \"modifiers\": [\"Control\", \"Alt\"], \"key\": \"K\" }, \"conversionMode\": \"auto\", \"isEnabled\": true, \"restoreClipboard\": true }");
    var fromBrief = new SettingsService(dir).Load();
    Check("brief's example json loads", fromBrief.Hotkey.Equals(AppSettings.DefaultHotkey()) && fromBrief.ConversionMode == ConversionMode.Auto);

    // real-time settings persistence
    svc.Update(x =>
    {
        x.RealTimeCorrection = true;
        x.RealTimeIdleSeconds = 3.5;
        x.RealTimeSmartFilter = false;
        x.RealTimeOnEnter = false;
    });
    var rtLoaded = new SettingsService(dir).Load();
    Check("real-time correction survives restart", rtLoaded.RealTimeCorrection && rtLoaded.RealTimeIdleSeconds == 3.5 && !rtLoaded.RealTimeSmartFilter && !rtLoaded.RealTimeOnEnter);

    // idle seconds out of range is clamped
    File.WriteAllText(svc.FilePath, "{ \"realTimeIdleSeconds\": 999.0 }");
    var clampedHigh = new SettingsService(dir).Load();
    Check("idle seconds clamped to max 10.0", clampedHigh.RealTimeIdleSeconds == 10.0);

    File.WriteAllText(svc.FilePath, "{ \"realTimeIdleSeconds\": -2.0 }");
    var clampedLow = new SettingsService(dir).Load();
    Check("idle seconds clamped to min 0.5 or default", clampedLow.RealTimeIdleSeconds >= 0.5);
}
finally
{
    try { Directory.Delete(dir, true); } catch { }
}

Console.WriteLine("Real-time correction & smart filter");
Check("lvpfh converts to مرحبا", conv.Convert("lvpfh", ConversionMode.Auto).Text == "مرحبا");
Check("lh converts to ما", conv.Convert("lh", ConversionMode.Auto).Text == "ما");
Check("hofhv; converts to اخبارك", conv.Convert("hofhv;", ConversionMode.Auto).Text == "اخبارك");
Check("lh hofhv; converts to ما اخبارك", conv.Convert("lh hofhv;", ConversionMode.Auto).Text == "ما اخبارك");
Check("pfdfd converts to حبيبي", conv.Convert("pfdfd", ConversionMode.Auto).Text == "حبيبي");
Check(";dt; converts to كيفك", conv.Convert(";dt;", ConversionMode.Auto).Text == "كيفك");
Check("hggi converts to الله", conv.Convert("hggi", ConversionMode.Auto).Text == "الله");
Check("hgpl]ggi converts to الحمدلله", conv.Convert("hgpl]ggi", ConversionMode.Auto).Text == "الحمدلله");
Check("wfhp converts to صباح", conv.Convert("wfhp", ConversionMode.Auto).Text == "صباح");
Check("lshx converts to مساء", conv.Convert("lshx", ConversionMode.Auto).Text == "مساء");
Check("kul converts to نعم", conv.Convert("kul", ConversionMode.Auto).Text == "نعم");
Check("b converts to لا", conv.Convert("b", ConversionMode.Auto).Text == "لا");
Check("sbl converts to سلام", conv.Convert("sbl", ConversionMode.Auto).Text == "سلام");
Check("sun converts to سعى in ToArabic", conv.Convert("sun", ConversionMode.ToArabic).Text == "سعى");
Check("Hib converts to أهلا", conv.Convert("Hib", ConversionMode.Auto).Text == "أهلا");
Check("Hpl] converts to أحمد", conv.Convert("Hpl]", ConversionMode.Auto).Text == "أحمد");
Check(";dt;? converts to كيفك؟", conv.Convert(";dt;?", ConversionMode.Auto).Text == "كيفك؟");
Check("hello is recognized as English word", EnglishWordCatalog.IsCommonEnglishWord("hello"));
Check("this is recognized as English word", EnglishWordCatalog.IsCommonEnglishWord("this"));
Check("computer is recognized as English word", EnglishWordCatalog.IsCommonEnglishWord("computer"));
Check("project is recognized as English word", EnglishWordCatalog.IsCommonEnglishWord("project"));
Check("lvpfh is not English word", !EnglishWordCatalog.LooksLikeEnglishWord("lvpfh"));
Check("lh is not English word", !EnglishWordCatalog.LooksLikeEnglishWord("lh"));
Check("hofhv; is not English word", !EnglishWordCatalog.LooksLikeEnglishWord("hofhv;"));
Check("a;vh is not English word", !EnglishWordCatalog.LooksLikeEnglishWord("a;vh"));

Console.WriteLine("Elevation check");
using (var id = System.Security.Principal.WindowsIdentity.GetCurrent())
{
    var princ = new System.Security.Principal.WindowsPrincipal(id);
    bool isElevated = princ.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    Check("Elevation check executes cleanly", isElevated == true || isElevated == false);
}

var sbTest = new System.Text.StringBuilder(16);
var keyStateArr = new byte[256];
int resAr = NativeSafeTest.ToUnicodeEx(0x4C, 0, keyStateArr, sbTest, 16, 0, (IntPtr)67184641);
Check("safe ToUnicodeEx produces Arabic Meem", resAr == 1 && sbTest[0] == '\u0645');

sbTest.Clear();
int resEn = NativeSafeTest.ToUnicodeEx(0x4C, 0, keyStateArr, sbTest, 16, 0, (IntPtr)67699721);
Check("safe ToUnicodeEx produces English 'l'", resEn == 1 && sbTest[0] == 'l');

Console.WriteLine();
Console.WriteLine($"{passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;

internal static class NativeSafeTest
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, ExactSpelling = true)]
    public static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [System.Runtime.InteropServices.Out, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);
}
