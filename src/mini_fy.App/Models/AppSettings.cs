namespace mini_fy.App.Models;

public class AppSettings
{
    public HotkeyConfig Hotkey { get; set; } = new();
    public BaiduApiConfig BaiduApi { get; set; } = new();
    public OcrConfig Ocr { get; set; } = new();
    public ScreenshotConfig Screenshot { get; set; } = new();
    public GeneralConfig General { get; set; } = new();
}

public class HotkeyConfig
{
    public string Modifiers { get; set; } = "Ctrl+Alt";
    public string Key { get; set; } = "Q";
}

public class BaiduApiConfig
{
    public string AppId { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

public class OcrConfig
{
    public string Language { get; set; } = "en";
}

public class ScreenshotConfig
{
    public bool AutoSave { get; set; } = false;
    public string SavePath { get; set; } = "";
}

public class GeneralConfig
{
    public bool AutoCopyTranslation { get; set; } = true;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool BypassProxy { get; set; } = false;
    public TranslateMode TranslateMode { get; set; } = TranslateMode.Manual;
    public int AutoCloseSeconds { get; set; } = 10;
    public string CopyHotkeyModifiers { get; set; } = "Ctrl+Alt";
    public string CopyHotkeyKey { get; set; } = "S";
}
