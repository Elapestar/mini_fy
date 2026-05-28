using mini_fy.App.Helpers;

namespace mini_fy.App.Services;

public class HotkeyService : IHotkeyService
{
    private const int HOTKEY_ID = 9001;
    private bool _registered;

    public event Action? HotkeyPressed;

    public bool Register(IntPtr windowHandle, string modifiers, string key)
    {
        try
        {
            uint mod = Win32Api.ParseModifiers(modifiers);
            uint vk = Win32Api.ParseKey(key);

            bool ok = Win32Api.RegisterHotKey(windowHandle, HOTKEY_ID, mod, vk);
            if (ok)
            {
                _registered = true;
                LogHelper.Info($"Hotkey registered: {modifiers}+{key} (ID={HOTKEY_ID})");
            }
            else
            {
                int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                LogHelper.Warning($"RegisterHotKey failed, error={err}. May be occupied.");
            }
            return ok;
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
        Win32Api.UnregisterHotKey(windowHandle, HOTKEY_ID);
        _registered = false;
        LogHelper.Info("Hotkey unregistered");
    }

    public void HandleHotkeyMessage(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_ID)
            HotkeyPressed?.Invoke();
    }
}
