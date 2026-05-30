using mini_fy.App.Helpers;

namespace mini_fy.App.Services;

public class HotkeyService : IHotkeyService
{
    private const int HOTKEY_ID_SCREENSHOT = 9001;
    private const int HOTKEY_ID_COPY = 9002;
    private bool _registered;

    public event Action? HotkeyPressed;
    public event Action? CopyHotkeyPressed;

    public bool Register(IntPtr windowHandle,
        string screenshotModifiers, string screenshotKey,
        string copyModifiers, string copyKey)
    {
        try
        {
            uint mod1 = Win32Api.ParseModifiers(screenshotModifiers);
            uint vk1 = Win32Api.ParseKey(screenshotKey);
            uint mod2 = Win32Api.ParseModifiers(copyModifiers);
            uint vk2 = Win32Api.ParseKey(copyKey);

            bool ok1 = Win32Api.RegisterHotKey(windowHandle, HOTKEY_ID_SCREENSHOT, mod1, vk1);
            bool ok2 = Win32Api.RegisterHotKey(windowHandle, HOTKEY_ID_COPY, mod2, vk2);

            if (ok1 && ok2)
            {
                _registered = true;
                LogHelper.Info($"Hotkeys registered: {screenshotModifiers}+{screenshotKey} (ID={HOTKEY_ID_SCREENSHOT}), " +
                               $"{copyModifiers}+{copyKey} (ID={HOTKEY_ID_COPY})");
            }
            else
            {
                int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                LogHelper.Warning($"RegisterHotKey failed, error={err}. May be occupied.");
            }
            return ok1 && ok2;
        }
        catch (Exception ex)
        {
            LogHelper.Error("Hotkey registration error", ex);
            return false;
        }
    }

    public void Unregister(IntPtr windowHandle)
    {
        if (!_registered) return;
        Win32Api.UnregisterHotKey(windowHandle, HOTKEY_ID_SCREENSHOT);
        Win32Api.UnregisterHotKey(windowHandle, HOTKEY_ID_COPY);
        _registered = false;
        LogHelper.Info("Hotkeys unregistered");
    }

    public void HandleHotkeyMessage(int hotkeyId)
    {
        switch (hotkeyId)
        {
            case HOTKEY_ID_SCREENSHOT:
                HotkeyPressed?.Invoke();
                break;
            case HOTKEY_ID_COPY:
                CopyHotkeyPressed?.Invoke();
                break;
        }
    }
}
