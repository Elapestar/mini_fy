namespace mini_fy.App.Services;

public interface IHotkeyService
{
    event Action? HotkeyPressed;
    event Action? CopyHotkeyPressed;
    bool Register(IntPtr windowHandle,
        string screenshotModifiers, string screenshotKey,
        string copyModifiers, string copyKey);
    void Unregister(IntPtr windowHandle);
    void HandleHotkeyMessage(int hotkeyId);
}
