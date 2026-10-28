using System.Text.Json;
using System.Text.Json.Serialization;
using Nabra.Core.Models;

namespace Nabra.Core.Services;

/// <summary>
/// Loads and saves settings as a local JSON file (%AppData%\Nabra\settings.json by default).
/// Writes are atomic (temp file + move) and a corrupt file is set aside instead of crashing.
/// </summary>
public sealed class SettingsService
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string Directory { get; }
    public string FilePath { get; }
    public AppSettings Current { get; private set; } = new();

    public SettingsService(string? directory = null)
    {
        Directory = directory ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nabra");
        FilePath = System.IO.Path.Combine(Directory, "settings.json");
    }

    public AppSettings Load()
    {
        AppSettings? loaded = null;
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded == null) throw new JsonException("empty settings");
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            TryQuarantineCorruptFile();
            loaded = null;
        }

        Current = loaded ?? new AppSettings();
        Current.Normalize();
        if (loaded == null) TrySave();
        return Current;
    }

    /// <summary>Applies a change, normalises, and persists immediately.</summary>
    public void Update(Action<AppSettings> mutate)
    {
        mutate(Current);
        Current.Normalize();
        TrySave();
    }

    public void ResetToDefaults()
    {
        Current = new AppSettings();
        Current.Normalize();
        TrySave();
    }

    /// <summary>Returns false (instead of throwing) if the disk is read-only, full, etc.</summary>
    public bool TrySave()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, JsonOptions));
            File.Move(tmp, FilePath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void TryQuarantineCorruptFile()
    {
        try
        {
            var target = System.IO.Path.Combine(Directory, $"settings.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Move(FilePath, target, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
