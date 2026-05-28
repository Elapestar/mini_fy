namespace mini_fy.App.Services;

public interface ITrayService
{
    event Action? ScreenshotRequested;
    event Action? SettingsRequested;
    event Action? ExitRequested;
    void Initialize();
    void ShowNotification(string title, string message);
    void Dispose();
}
