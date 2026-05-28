namespace mini_fy.App.Services;

public interface IHotkeyService
{
    event Action? HotkeyPressed;
    bool Register(IntPtr windowHandle, string modifiers, string key);
    void Unregister(IntPtr windowHandle);
    void HandleHotkeyMessage(int hotkeyId);
}
