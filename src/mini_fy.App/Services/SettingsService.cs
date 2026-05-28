using System.IO;
using System.Text.Json;
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Current { get; private set; } = new();

    public string ConfigFilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public void Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            else
            {
                Current = new AppSettings();
                Save(); // Write defaults
            }
        }
        catch (Exception ex)
        {
            Current = new AppSettings();
            Helpers.LogHelper.Error("Failed to load settings", ex);
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, _jsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Helpers.LogHelper.Error("Failed to save settings", ex);
        }
    }
}
