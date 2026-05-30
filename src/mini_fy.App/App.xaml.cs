using System.Windows;
using System.Windows.Interop;
using mini_fy.App.Helpers;
using mini_fy.App.Services;
using Application = System.Windows.Application;

namespace mini_fy.App;

public partial class App : Application
{
    private ISettingsService _settingsService = null!;
    private ITrayService _trayService = null!;
    private IHotkeyService _hotkeyService = null!;
    private IScreenshotService _screenshotService = null!;
    private IOcrService _ocrService = null!;
    private ITranslateService _translateService = null!;
    private IOverlayService _overlayService = null!;

    private Window? _messageWindow;
    private string _lastTranslatedText = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogHelper.Info("mini_fy starting...");
        LogHelper.CleanOldLogs();

        // 1. Init settings
        _settingsService = new SettingsService();
        _settingsService.Load();

        // 2. Init services
        _trayService = new TrayService();
        _screenshotService = new ScreenshotService();
        _overlayService = new OverlayService();
        _translateService = new TranslateService(_settingsService);

        // 3. Init OCR (may fail if language pack missing)
        try
        {
            _ocrService = new OcrService(_settingsService.Current.Ocr.Language);
        }
        catch (Exception ex)
        {
            LogHelper.Error("OCR init failed — English language pack may be missing", ex);
            _trayService.ShowNotification("mini_fy",
                "OCR 初始化失败，请检查系统是否安装了英文语言包");
            _ocrService = new OcrService("en");
        }

        // 4. Hotkeys
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.CopyHotkeyPressed += OnCopyHotkeyPressed;

        // 5. Tray
        _trayService.ScreenshotRequested += OnHotkeyPressed;
        _trayService.SettingsRequested += OpenSettings;
        _trayService.ExitRequested += ExitApplication;
        _trayService.Initialize();

        // 6. Create hidden window for hotkey messages
        _messageWindow = CreateMessageWindow();
        var hwnd = new WindowInteropHelper(_messageWindow).Handle;
        RegisterHotkeys(hwnd);

        LogHelper.Info("mini_fy started successfully");
    }

    private void RegisterHotkeys(IntPtr hwnd)
    {
        var cfg = _settingsService.Current;
        if (!_hotkeyService.Register(hwnd,
                cfg.Hotkey.Modifiers, cfg.Hotkey.Key,
                cfg.General.CopyHotkeyModifiers, cfg.General.CopyHotkeyKey))
        {
            _trayService.ShowNotification("mini_fy",
                "快捷键注册失败，可能被其他程序占用");
        }
    }

    private Window CreateMessageWindow()
    {
        var window = new Window
        {
            Width = 0, Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Opacity = 0,
            ShowActivated = false
        };
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        };
        window.Show();
        return window;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Api.WM_HOTKEY)
        {
            _hotkeyService.HandleHotkeyMessage(wParam.ToInt32());
            handled = true;
        }
        return IntPtr.Zero;
    }

    private async void OnHotkeyPressed()
    {
        try
        {
            var mode = _settingsService.Current.General.TranslateMode;
            if (mode == Models.TranslateMode.Auto)
            {
                await RunAutoTranslate();
            }
            else
            {
                await RunManualTranslate();
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("Hotkey handler error", ex);
            _overlayService.Show(new Models.TranslationResult
            {
                OriginalText = "", TranslatedText = "",
                ErrorMessage = $"程序异常: {ex.Message}"
            });
        }
    }

    private async Task RunManualTranslate()
    {
        LogHelper.Info("Manual screenshot started...");

        var bitmap = _screenshotService.Capture();
        if (bitmap == null)
        {
            LogHelper.Info("Screenshot cancelled");
            return;
        }

        var ocrTask = Task.Run(async () =>
        {
            var text = await _ocrService.RecognizeAsync(bitmap);
            bitmap.Dispose();
            return text;
        });

        var ocrText = await ocrTask;

        if (string.IsNullOrWhiteSpace(ocrText))
        {
            _overlayService.Show(new Models.TranslationResult
            {
                OriginalText = "(未识别到文字)", TranslatedText = "",
                ErrorMessage = "未识别到文字，请确认截图区域包含英文文本"
            });
            return;
        }

        var result = await _translateService.TranslateAsync(ocrText);
        StoreTranslation(result);

        if (result.Success && _settingsService.Current.General.AutoCopyTranslation)
            SafeCopyToClipboard(result.TranslatedText);

        _overlayService.Show(result);
    }

    private async Task RunAutoTranslate()
    {
        LogHelper.Info("Auto full-screen translate started...");

        // Capture full screen directly (no user interaction)
        using var fullScreen = CaptureFullScreen();
        if (fullScreen == null)
        {
            LogHelper.Error("Full screen capture failed");
            return;
        }

        // OCR: find all text blocks
        var blocks = await _ocrService.RecognizeBlocksAsync(fullScreen);
        if (blocks.Count == 0)
        {
            _overlayService.Show(new Models.TranslationResult
            {
                OriginalText = "(未识别到文字)", TranslatedText = "",
                ErrorMessage = "全屏未识别到英文文本"
            });
            return;
        }

        LogHelper.Info($"Auto OCR: {blocks.Count} text blocks found");

        // Translate all blocks concurrently
        var translated = await _translateService.TranslateBlocksAsync(blocks);

        // Store for copy hotkey
        var allText = string.Join("\n", translated.Select(b => b.TranslatedText));
        _lastTranslatedText = allText;

        // Show display-only overlay
        int autoClose = _settingsService.Current.General.AutoCloseSeconds;
        _overlayService.ShowAutoResult(translated, autoClose);
    }

    private void StoreTranslation(Models.TranslationResult result)
    {
        if (result.Success)
            _lastTranslatedText = result.TranslatedText;
    }

    private void OnCopyHotkeyPressed()
    {
        if (!string.IsNullOrWhiteSpace(_lastTranslatedText))
        {
            SafeCopyToClipboard(_lastTranslatedText);
            LogHelper.Info("Copy hotkey: last translation copied to clipboard");
        }
    }

    private static System.Drawing.Bitmap? CaptureFullScreen()
    {
        int x = Win32Api.GetSystemMetrics(Win32Api.SM_XVIRTUALSCREEN);
        int y = Win32Api.GetSystemMetrics(Win32Api.SM_YVIRTUALSCREEN);
        int w = Win32Api.GetSystemMetrics(Win32Api.SM_CXVIRTUALSCREEN);
        int h = Win32Api.GetSystemMetrics(Win32Api.SM_CYVIRTUALSCREEN);

        if (w <= 0) w = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
        if (h <= 0) h = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);

        var bitmap = new System.Drawing.Bitmap(w, h,
            System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
        return bitmap;
    }

    private static void SafeCopyToClipboard(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                Thread.Sleep(80);
            }
        }
        LogHelper.Warning("Failed to copy to clipboard after 5 retries");
    }

    private void OpenSettings()
    {
        var window = new Views.SettingsWindow(_settingsService, _translateService);
        window.ShowDialog();

        if (_messageWindow != null)
        {
            var hwnd = new WindowInteropHelper(_messageWindow).Handle;
            _hotkeyService.Unregister(hwnd);
            RegisterHotkeys(hwnd);
        }
    }

    private void ExitApplication()
    {
        LogHelper.Info("mini_fy exiting...");

        if (_messageWindow != null)
        {
            var hwnd = new WindowInteropHelper(_messageWindow).Handle;
            _hotkeyService.Unregister(hwnd);
        }

        _trayService.Dispose();
        (_translateService as IDisposable)?.Dispose();

        LogHelper.Info("mini_fy exited");
        Shutdown();
    }
}
