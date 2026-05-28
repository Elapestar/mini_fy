using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Load();
    void Save();
    string ConfigFilePath { get; }
}
