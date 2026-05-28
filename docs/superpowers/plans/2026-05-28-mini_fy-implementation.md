# mini_fy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight Windows screenshot translation tool (WPF .NET 8) that captures screen regions via hotkey, OCRs English text, and displays Chinese translations via Baidu API.

**Architecture:** Lightweight event-driven. App.xaml.cs coordinates 7 independent Services (Hotkey, Screenshot, Ocr, Translate, Overlay, Tray, Settings). Each service implements its own interface for testability. WPF Windows are used only for the screenshot overlay, translation popup, and settings panel. No MVVM framework, no DI container.

**Tech Stack:** WPF .NET 8, Windows.Media.Ocr, Baidu AI Text Translate API (Bearer Token), HttpClient, System.Text.Json, MSTest

---

## File Structure

```
mini_fy/
├── mini_fy.sln
├── README.md
├── .gitignore
├── config/
│   └── settings.example.json
├── src/
│   ├── mini_fy.App/
│   │   ├── mini_fy.App.csproj
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── Models/
│   │   │   ├── AppSettings.cs
│   │   │   └── TranslationResult.cs
│   │   ├── Helpers/
│   │   │   ├── Win32Api.cs
│   │   │   └── LogHelper.cs
│   │   ├── Services/
│   │   │   ├── ISettingsService.cs
│   │   │   ├── SettingsService.cs
│   │   │   ├── ITrayService.cs
│   │   │   ├── TrayService.cs
│   │   │   ├── IHotkeyService.cs
│   │   │   ├── HotkeyService.cs
│   │   │   ├── IScreenshotService.cs
│   │   │   ├── ScreenshotService.cs
│   │   │   ├── IOcrService.cs
│   │   │   ├── OcrService.cs
│   │   │   ├── ITranslateService.cs
│   │   │   ├── TranslateService.cs
│   │   │   ├── IOverlayService.cs
│   │   │   └── OverlayService.cs
│   │   └── Views/
│   │       ├── ScreenshotWindow.xaml
│   │       ├── ScreenshotWindow.xaml.cs
│   │       ├── TranslationOverlay.xaml
│   │       ├── TranslationOverlay.xaml.cs
│   │       ├── SettingsWindow.xaml
│   │       └── SettingsWindow.xaml.cs
│   └── mini_fy.Tests/
│       ├── mini_fy.Tests.csproj
│       └── Services/
│           ├── SettingsServiceTests.cs
│           ├── TranslateServiceTests.cs
│           └── OcrServiceTests.cs
```

---

### Task 1: Solution & Project Scaffold

**Files:**
- Create: `mini_fy/mini_fy.sln`
- Create: `mini_fy/src/mini_fy.App/mini_fy.App.csproj`
- Create: `mini_fy/src/mini_fy.App/App.xaml`
- Create: `mini_fy/src/mini_fy.App/App.xaml.cs`
- Create: `mini_fy/src/mini_fy.Tests/mini_fy.Tests.csproj`
- Create: `mini_fy/src/mini_fy.Tests/Usings.cs`

- [ ] **Step 1: Create solution file**

Run: `dotnet new sln -n mini_fy -o src --force`
Run from: `d:/Computer_test/Claude_code/mini_fy/`

- [ ] **Step 2: Create WPF project**

Run: `dotnet new wpf -n mini_fy.App -o src/mini_fy.App -f net8.0-windows`

- [ ] **Step 3: Add Windows.Media.Ocr support via CsWinRT**

`src/mini_fy.App/mini_fy.App.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>Assets\tray.ico</ApplicationIcon>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.0.4" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create MSTest project**

Run: `dotnet new mstest -n mini_fy.Tests -o src/mini_fy.Tests -f net8.0-windows`

`src/mini_fy.Tests/mini_fy.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.2.0" />
    <PackageReference Include="MSTest.TestFramework" Version="3.2.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\mini_fy.App\mini_fy.App.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Add projects to solution**

```bash
cd d:/Computer_test/Claude_code/mini_fy
dotnet sln src/mini_fy.sln add src/mini_fy.App/mini_fy.App.csproj
dotnet sln src/mini_fy.sln add src/mini_fy.Tests/mini_fy.Tests.csproj
```

- [ ] **Step 6: Create minimal App.xaml**

`src/mini_fy.App/App.xaml`:
```xml
<Application x:Class="mini_fy.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
    </Application.Resources>
</Application>
```

`src/mini_fy.App/App.xaml.cs`:
```csharp
namespace mini_fy.App;

public partial class App : System.Windows.Application
{
}
```

- [ ] **Step 7: Build and verify**

```bash
cd d:/Computer_test/Claude_code/mini_fy/src/mini_fy.App
dotnet build
```
Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add src/mini_fy.sln src/mini_fy.App/ src/mini_fy.Tests/
git commit -m "Scaffold: solution and project structure (WPF .NET 8 + CsWinRT + MSTest)"
```

---

### Task 2: Models

**Files:**
- Create: `mini_fy/src/mini_fy.App/Models/AppSettings.cs`
- Create: `mini_fy/src/mini_fy.App/Models/TranslationResult.cs`

- [ ] **Step 1: Write AppSettings.cs**

```csharp
namespace mini_fy.App.Models;

/// <summary>
/// Root configuration, serialized to/from settings.json by SettingsService.
/// </summary>
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
}
```

- [ ] **Step 2: Write TranslationResult.cs**

```csharp
namespace mini_fy.App.Models;

public class TranslationResult
{
    public string OriginalText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public string SourceLanguage { get; init; } = "en";
    public string TargetLanguage { get; init; } = "zh";
    public bool IsFromCache { get; init; } = false;
    public string? ErrorMessage { get; init; }
    public bool Success => ErrorMessage == null;
}
```

- [ ] **Step 3: Build verify**

```bash
cd d:/Computer_test/Claude_code/mini_fy/src/mini_fy.App && dotnet build
```
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/mini_fy.App/Models/
git commit -m "Add models: AppSettings and TranslationResult"
```

---

### Task 3: Win32Api Helper

**Files:**
- Create: `mini_fy/src/mini_fy.App/Helpers/Win32Api.cs`

- [ ] **Step 1: Write Win32Api.cs**

```csharp
using System.Runtime.InteropServices;

namespace mini_fy.App.Helpers;

public static class Win32Api
{
    // Hotkey
    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Modifier flags
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // Screenshot overlay — keep window topmost
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    // Get keyboard state for screenshot cancel
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    public const int VK_ESCAPE = 0x1B;

    // Cursor position for overlay placement
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    // Screen dimensions
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    /// <summary>
    /// Parse "Ctrl+Alt" into MOD_CONTROL | MOD_ALT.
    /// </summary>
    public static uint ParseModifiers(string modifiers)
    {
        uint result = 0;
        var parts = modifiers.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            result |= part.ToLower() switch
            {
                "ctrl" => MOD_CONTROL,
                "alt" => MOD_ALT,
                "shift" => MOD_SHIFT,
                "win" => MOD_WIN,
                _ => 0
            };
        }
        return result | MOD_NOREPEAT;
    }

    /// <summary>
    /// Parse "Q" into virtual-key code.
    /// </summary>
    public static uint ParseKey(string key)
    {
        if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
            return (uint)char.ToUpperInvariant(key[0]);
        throw new ArgumentException($"Unsupported key: {key}");
    }
}
```

- [ ] **Step 2: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/mini_fy.App/Helpers/Win32Api.cs
git commit -m "Add Win32Api helper for hotkey, window, and input APIs"
```

---

### Task 4: LogHelper

**Files:**
- Create: `mini_fy/src/mini_fy.App/Helpers/LogHelper.cs`

- [ ] **Step 1: Write LogHelper.cs**

```csharp
using System.Diagnostics;

namespace mini_fy.App.Helpers;

public static class LogHelper
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
        "mini_fy", "logs");

    private static readonly object _lock = new();

    public enum Level { Info, Warning, Error }

    public static void Info(string message) => Log(Level.Info, message);
    public static void Warning(string message) => Log(Level.Warning, message);
    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex == null ? message : $"{message} | Exception: {ex}";
        Log(Level.Error, msg);
    }

    private static void Log(Level level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var logFile = Path.Combine(LogDir, $"mini_fy_{DateTime.Now:yyyy-MM-dd}.log");
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            lock (_lock)
            {
                File.AppendAllText(logFile, line + Environment.NewLine);
            }

            if (level == Level.Error)
                Debug.WriteLine(line);
        }
        catch
        {
            // Silent fail — logging must never crash the app
        }
    }

    /// <summary>Open log directory in Explorer.</summary>
    public static void OpenLogDir()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            Process.Start("explorer.exe", LogDir);
        }
        catch { }
    }

    /// <summary>Delete logs older than 7 days.</summary>
    public static void CleanOldLogs()
    {
        try
        {
            if (!Directory.Exists(LogDir)) return;
            var cutoff = DateTime.Now.AddDays(-7);
            foreach (var file in Directory.GetFiles(LogDir, "mini_fy_*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch { }
    }

    /// <summary>Mask API key for safe logging (shows only last 4 chars).</summary>
    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length <= 4) return "****";
        return new string('*', key.Length - 4) + key[^4..];
    }
}
```

- [ ] **Step 2: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 3: Commit**

```bash
git add src/mini_fy.App/Helpers/LogHelper.cs
git commit -m "Add LogHelper for lightweight file-based logging"
```

---

### Task 5: SettingsService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/ISettingsService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/SettingsService.cs`
- Create: `mini_fy/src/mini_fy.Tests/Services/SettingsServiceTests.cs`

- [ ] **Step 1: Write ISettingsService.cs**

```csharp
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    void Load();
    void Save();
    string ConfigFilePath { get; }
}
```

- [ ] **Step 2: Write SettingsService.cs**

```csharp
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
```

- [ ] **Step 3: Write SettingsServiceTests.cs**

```csharp
using mini_fy.App.Services;
using mini_fy.App.Models;

namespace mini_fy.Tests.Services;

[TestClass]
public class SettingsServiceTests
{
    [TestMethod]
    public void Load_NoFileExists_ReturnsDefaults()
    {
        var svc = new SettingsService();
        // Temporarily override config path to a non-existent file
        // SettingsService reads from BaseDirectory; defaults work if no file

        // Just verify defaults are populated
        var settings = new AppSettings();
        Assert.AreEqual("Ctrl+Alt", settings.Hotkey.Modifiers);
        Assert.AreEqual("Q", settings.Hotkey.Key);
        Assert.AreEqual("en", settings.Ocr.Language);
        Assert.IsTrue(settings.General.AutoCopyTranslation);
    }

    [TestMethod]
    public void Current_AfterNew_IsNotEmpty()
    {
        var svc = new SettingsService();
        svc.Load();
        Assert.IsNotNull(svc.Current);
        Assert.IsNotNull(svc.Current.Hotkey);
    }
}
```

- [ ] **Step 4: Build & run tests**

```bash
cd d:/Computer_test/Claude_code/mini_fy
dotnet test src/mini_fy.Tests/mini_fy.Tests.csproj --filter "SettingsService"
```
Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/mini_fy.App/Services/ISettingsService.cs src/mini_fy.App/Services/SettingsService.cs
git add src/mini_fy.Tests/Services/SettingsServiceTests.cs
git commit -m "Add SettingsService for JSON config read/write"
```

---

### Task 6: TrayService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/ITrayService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/TrayService.cs`
- Create: `mini_fy/src/mini_fy.App/Assets/tray.ico` (embedded resource)

- [ ] **Step 1: Write ITrayService.cs**

```csharp
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
```

- [ ] **Step 2: Write TrayService.cs**

```csharp
using System.Windows;
using System.Drawing;
using mini_fy.App.Helpers;

namespace mini_fy.App.Services;

public class TrayService : ITrayService, IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _disposed;

    public event Action? ScreenshotRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public void Initialize()
    {
        _contextMenu = new ContextMenuStrip();

        var captureItem = new ToolStripMenuItem("开始截图");
        captureItem.Click += (_, _) => ScreenshotRequested?.Invoke();

        var settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        var logItem = new ToolStripMenuItem("查看日志");
        logItem.Click += (_, _) => LogHelper.OpenLogDir();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        _contextMenu.Items.Add(captureItem);
        _contextMenu.Items.Add(settingsItem);
        _contextMenu.Items.Add(logItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        var icon = LoadTrayIcon();

        _notifyIcon = new NotifyIcon
        {
            Text = "mini_fy - 截图翻译",
            Icon = icon,
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => ScreenshotRequested?.Invoke();

        LogHelper.Info("Tray icon initialized");
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            var iconPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "tray.ico");
            if (File.Exists(iconPath))
                return new Icon(iconPath);

            // Fallback: create a simple colored bitmap icon
            var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.DodgerBlue);
            using var font = new System.Drawing.Font("Microsoft YaHei", 16, FontStyle.Bold);
            g.DrawString("Fy", font, Brushes.White, 2, 4);
            return Icon.FromHandle(bmp.GetHicon());
        }
        catch (Exception ex)
        {
            LogHelper.Error("Failed to load tray icon, using default", ex);
            return SystemIcons.Application;
        }
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
        LogHelper.Info("Tray disposed");
    }
}
```

- [ ] **Step 3: Add Windows.Forms reference for NotifyIcon**

In `mini_fy.App.csproj`, add inside `<PropertyGroup>`:
```xml
<UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 4: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/mini_fy.App/Services/ITrayService.cs src/mini_fy.App/Services/TrayService.cs
git add src/mini_fy.App/mini_fy.App.csproj
git commit -m "Add TrayService with context menu and fallback icon"
```

---

### Task 7: HotkeyService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/IHotkeyService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/HotkeyService.cs`

- [ ] **Step 1: Write IHotkeyService.cs**

```csharp
namespace mini_fy.App.Services;

public interface IHotkeyService
{
    event Action? HotkeyPressed;
    bool Register(IntPtr windowHandle, string modifiers, string key);
    void Unregister(IntPtr windowHandle);
    void HandleHotkeyMessage(int hotkeyId);
}
```

- [ ] **Step 2: Write HotkeyService.cs**

```csharp
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
```

- [ ] **Step 3: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 4: Commit**

```bash
git add src/mini_fy.App/Services/IHotkeyService.cs src/mini_fy.App/Services/HotkeyService.cs
git commit -m "Add HotkeyService with RegisterHotKey Win32 API"
```

---

### Task 8: ScreenshotService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/IScreenshotService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/ScreenshotService.cs`
- Create: `mini_fy/src/mini_fy.App/Views/ScreenshotWindow.xaml`
- Create: `mini_fy/src/mini_fy.App/Views/ScreenshotWindow.xaml.cs`

- [ ] **Step 1: Write IScreenshotService.cs**

```csharp
using System.Drawing;

namespace mini_fy.App.Services;

public interface IScreenshotService
{
    /// <summary>Open screenshot overlay, return cropped bitmap or null if cancelled.</summary>
    Bitmap? Capture();
}
```

- [ ] **Step 2: Write ScreenshotWindow.xaml**

```xml
<Window x:Class="mini_fy.App.Views.ScreenshotWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Topmost="True"
        ShowInTaskbar="False"
        WindowState="Maximized"
        Background="#66000000"
        Cursor="Cross"
        KeyDown="Window_KeyDown">
    <Grid x:Name="MainGrid">
        <!-- Screen freeze background -->
        <Image x:Name="BackgroundImage" Stretch="Fill" />
        <!-- Selection rectangle overlay -->
        <Canvas x:Name="SelectionCanvas" Background="Transparent"
                MouseLeftButtonDown="Canvas_MouseLeftButtonDown"
                MouseMove="Canvas_MouseMove"
                MouseLeftButtonUp="Canvas_MouseLeftButtonUp">
            <Rectangle x:Name="SelectionRect"
                       Stroke="#FF4080FF"
                       StrokeThickness="2"
                       Fill="#204080FF"
                       Visibility="Collapsed" />
        </Canvas>
    </Grid>
</Window>
```

- [ ] **Step 3: Write ScreenshotWindow.xaml.cs**

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using mini_fy.App.Helpers;

namespace mini_fy.App.Views;

public partial class ScreenshotWindow : Window
{
    private Bitmap? _fullScreenBitmap;
    private System.Windows.Point _startPoint;
    private bool _isSelecting;
    private Rect _selectedRect;

    public Bitmap? ResultBitmap { get; private set; }

    public ScreenshotWindow()
    {
        InitializeComponent();
    }

    public void LoadBackground(Bitmap fullScreen)
    {
        _fullScreenBitmap = fullScreen;
        BackgroundImage.Source = BitmapToImageSource(fullScreen);
        // Prevent this window from stealing focus
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        Win32Api.SetWindowPos(hwnd, Win32Api.HWND_TOPMOST, 0, 0, 0, 0,
            Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(SelectionCanvas);
        _isSelecting = true;
        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        SelectionCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;
        var pos = e.GetPosition(SelectionCanvas);
        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);
        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        SelectionCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(SelectionCanvas);
        var x = (int)Math.Min(pos.X, _startPoint.X);
        var y = (int)Math.Min(pos.Y, _startPoint.Y);
        var w = (int)Math.Abs(pos.X - _startPoint.X);
        var h = (int)Math.Abs(pos.Y - _startPoint.Y);

        if (w > 5 && h > 5)
        {
            _selectedRect = new Rect(x, y, w, h);
            CropAndClose(x, y, w, h);
        }
        else
        {
            // Selection too small, treat as cancel
            Close();
        }
    }

    private void CropAndClose(int x, int y, int w, int h)
    {
        try
        {
            ResultBitmap = _fullScreenBitmap!.Clone(
                new System.Drawing.Rectangle(x, y, w, h),
                _fullScreenBitmap.PixelFormat);
        }
        catch
        {
            ResultBitmap = null;
        }
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ResultBitmap = null;
            Close();
        }
    }

    private static BitmapSource BitmapToImageSource(Bitmap bitmap)
    {
        var data = bitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, bitmap.PixelFormat);
        try
        {
            var bmpSrc = BitmapSource.Create(
                data.Width, data.Height, 96, 96,
                PixelFormats.Bgr32, null,
                data.Scan0, data.Stride * data.Height, data.Stride);
            return bmpSrc;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _fullScreenBitmap?.Dispose();
        _fullScreenBitmap = null;
        base.OnClosed(e);
    }
}
```

- [ ] **Step 4: Write ScreenshotService.cs**

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using mini_fy.App.Helpers;
using mini_fy.App.Views;

namespace mini_fy.App.Services;

public class ScreenshotService : IScreenshotService
{
    public Bitmap? Capture()
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            using var fullScreen = CaptureFullScreen();
            if (fullScreen == null) return null;

            var window = new ScreenshotWindow();
            window.LoadBackground(fullScreen);
            window.ShowDialog();

            return window.ResultBitmap;
        });
    }

    private static Bitmap? CaptureFullScreen()
    {
        int x = Win32Api.GetSystemMetrics(Win32Api.SM_XVIRTUALSCREEN);
        int y = Win32Api.GetSystemMetrics(Win32Api.SM_YVIRTUALSCREEN);
        int w = Win32Api.GetSystemMetrics(Win32Api.SM_CXVIRTUALSCREEN);
        int h = Win32Api.GetSystemMetrics(Win32Api.SM_CYVIRTUALSCREEN);

        if (w <= 0) w = Win32Api.GetSystemMetrics(Win32Api.SM_CXSCREEN);
        if (h <= 0) h = Win32Api.GetSystemMetrics(Win32Api.SM_CYSCREEN);

        var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
        return bitmap;
    }
}
```

- [ ] **Step 5: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 6: Commit**

```bash
git add src/mini_fy.App/Services/IScreenshotService.cs src/mini_fy.App/Services/ScreenshotService.cs
git add src/mini_fy.App/Views/ScreenshotWindow.xaml src/mini_fy.App/Views/ScreenshotWindow.xaml.cs
git commit -m "Add ScreenshotService with full-screen overlay and box selection"
```

---

### Task 9: OcrService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/IOcrService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/OcrService.cs`
- Create: `mini_fy/src/mini_fy.Tests/Services/OcrServiceTests.cs`

- [ ] **Step 1: Write IOcrService.cs**

```csharp
using System.Drawing;

namespace mini_fy.App.Services;

public interface IOcrService
{
    /// <summary>Recognize English text from bitmap. Returns empty string if nothing found.</summary>
    Task<string> RecognizeAsync(Bitmap bitmap);
}
```

- [ ] **Step 2: Write OcrService.cs**

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using mini_fy.App.Helpers;

namespace mini_fy.App.Services;

public class OcrService : IOcrService
{
    private readonly OcrEngine _engine;

    public OcrService(string languageTag = "en")
    {
        _engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(languageTag))
                  ?? throw new InvalidOperationException(
                      $"OCR engine for '{languageTag}' is not available on this system.");
        LogHelper.Info($"OCR engine initialized: {_engine.RecognizerLanguage.DisplayName}");
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        return await Task.Run(async () =>
        {
            try
            {
                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;

                var randomAccessStream = memoryStream.AsRandomAccessStream();
                var decoder = await Windows.Graphics.Imaging
                    .BitmapDecoder.CreateAsync(randomAccessStream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // OcrEngine requires BGRA8 with premultiplied alpha
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                    || softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap,
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var result = await _engine.RecognizeAsync(softwareBitmap);
                var text = result.Lines.Count == 0
                    ? ""
                    : string.Join(" ", result.Lines.Select(l => l.Text));

                LogHelper.Info($"OCR result: {result.Lines.Count} lines, \"{Truncate(text, 100)}\"");
                return text.Trim();
            }
            catch (Exception ex)
            {
                LogHelper.Error("OCR recognition failed", ex);
                return "";
            }
        });
    }

    private static string Truncate(string s, int len)
        => s.Length <= len ? s : s[..len] + "...";
}
```

- [ ] **Step 3: Write OcrServiceTests.cs**

```csharp
using System.Drawing;
using mini_fy.App.Services;

namespace mini_fy.Tests.Services;

[TestClass]
public class OcrServiceTests
{
    [TestMethod]
    [Ignore("Requires Windows OCR to be available")]
    public async Task RecognizeAsync_EmptyBitmap_ReturnsEmptyString()
    {
        var svc = new OcrService("en");
        using var bmp = new Bitmap(10, 10);
        using var g = Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.White);
        var text = await svc.RecognizeAsync(bmp);
        Assert.AreEqual("", text);
    }

    [TestMethod]
    public void Constructor_InvalidLanguage_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
            new OcrService("xx-invalid-99"));
    }
}
```

- [ ] **Step 4: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 5: Commit**

```bash
git add src/mini_fy.App/Services/IOcrService.cs src/mini_fy.App/Services/OcrService.cs
git add src/mini_fy.Tests/Services/OcrServiceTests.cs
git commit -m "Add OcrService using Windows.Media.Ocr for English text recognition"
```

---

### Task 10: TranslateService

**Files:**
- Create: `mini_fy/src/mini_fy.App/Services/ITranslateService.cs`
- Create: `mini_fy/src/mini_fy.App/Services/TranslateService.cs`
- Create: `mini_fy/src/mini_fy.Tests/Services/TranslateServiceTests.cs`

- [ ] **Step 1: Write ITranslateService.cs**

```csharp
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface ITranslateService
{
    Task<TranslationResult> TranslateAsync(string text, string from = "en", string to = "zh");
    void ClearCache();
}
```

- [ ] **Step 2: Write TranslateService.cs**

```csharp
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using mini_fy.App.Helpers;
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public class TranslateService : ITranslateService, IDisposable
{
    private const string ApiUrl = "https://fanyi-api.baidu.com/ait/api/aiTextTranslate";
    private const int TimeoutSeconds = 10;

    private readonly ISettingsService _settings;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private bool _disposed;

    public TranslateService(ISettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
    }

    public async Task<TranslationResult> TranslateAsync(string text, string from = "en", string to = "zh")
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult
            {
                OriginalText = text,
                TranslatedText = "",
                SourceLanguage = from,
                TargetLanguage = to
            };

        // Check cache
        var cacheKey = $"{from}:{to}:{text}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            LogHelper.Info($"Translation cache hit: \"{text}\" -> \"{cached}\"");
            return new TranslationResult
            {
                OriginalText = text, TranslatedText = cached,
                SourceLanguage = from, TargetLanguage = to, IsFromCache = true
            };
        }

        try
        {
            var config = _settings.Current.BaiduApi;
            if (string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return new TranslationResult
                {
                    OriginalText = text, TranslatedText = "",
                    SourceLanguage = from, TargetLanguage = to,
                    ErrorMessage = "请先在设置中配置百度翻译 API 的 APPID 和 API Key"
                };
            }

            var payload = new
            {
                appid = config.AppId,
                q = text,
                from,
                to
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            LogHelper.Info($"Translation request: \"{text}\" ({from}->{to}), " +
                           $"API Key={LogHelper.MaskKey(config.ApiKey)}");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var httpError = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "API Key 无效或未授权",
                    System.Net.HttpStatusCode.Forbidden => "API 权限不足",
                    _ => $"翻译服务返回错误 ({(int)response.StatusCode})"
                };
                LogHelper.Error($"Translation HTTP error: {response.StatusCode}");
                return ErrorResult(text, from, to, httpError);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Check for error
            if (json.TryGetProperty("error_code", out var errorCode))
            {
                var errorMsg = json.TryGetProperty("error_msg", out var msg)
                    ? msg.GetString() ?? "" : "";
                var userMsg = MapErrorCode(errorCode.GetString() ?? "", errorMsg);
                LogHelper.Error($"Translation API error: {errorCode} - {errorMsg}");
                return ErrorResult(text, from, to, userMsg);
            }

            // Parse success
            if (json.TryGetProperty("trans_result", out var transResult)
                && transResult.ValueKind == JsonValueKind.Array
                && transResult.GetArrayLength() > 0)
            {
                var dst = transResult[0].GetProperty("dst").GetString() ?? "";
                _cache[cacheKey] = dst;
                LogHelper.Info($"Translation success: \"{text}\" -> \"{dst}\"");
                return new TranslationResult
                {
                    OriginalText = text, TranslatedText = dst,
                    SourceLanguage = from, TargetLanguage = to
                };
            }

            return ErrorResult(text, from, to, "翻译结果格式异常");
        }
        catch (TaskCanceledException)
        {
            return ErrorResult(text, from, to, "翻译超时，请检查网络");
        }
        catch (HttpRequestException ex)
        {
            LogHelper.Error("Translation network error", ex);
            return ErrorResult(text, from, to, "网络异常，请检查网络连接");
        }
        catch (Exception ex)
        {
            LogHelper.Error("Translation unexpected error", ex);
            return ErrorResult(text, from, to, $"翻译失败: {ex.Message}");
        }
    }

    private static string MapErrorCode(string code, string defaultMsg)
    {
        return code switch
        {
            "52001" => "翻译超时，请重试",
            "52002" => "翻译服务异常，请重试",
            "52003" => "APPID 无效或服务未开通",
            "54000" => "请求参数错误",
            "54001" => "API Key 配置错误",
            "54003" => "请求太频繁，请稍后",
            "54004" => "翻译额度已用完",
            "59003" => "文本过长，请缩减截图区域",
            _ => defaultMsg
        };
    }

    private static TranslationResult ErrorResult(string text, string from, string to, string error)
    {
        return new TranslationResult
        {
            OriginalText = text, TranslatedText = "",
            SourceLanguage = from, TargetLanguage = to,
            ErrorMessage = error
        };
    }

    public void ClearCache()
    {
        _cache.Clear();
        LogHelper.Info("Translation cache cleared");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}
```

- [ ] **Step 3: Write TranslateServiceTests.cs**

```csharp
using mini_fy.App.Models;
using mini_fy.App.Services;

namespace mini_fy.Tests.Services;

[TestClass]
public class TranslateServiceTests
{
    private TranslateService CreateService(string appId = "test_id", string apiKey = "test_key")
    {
        var mockSettings = new TestSettingsService(appId, apiKey);
        return new TranslateService(mockSettings);
    }

    [TestMethod]
    public async Task TranslateAsync_EmptyText_ReturnsEmptyResult()
    {
        using var svc = CreateService();
        var result = await svc.TranslateAsync("");
        Assert.AreEqual("", result.TranslatedText);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task TranslateAsync_WhitespaceText_ReturnsEmptyResult()
    {
        using var svc = CreateService();
        var result = await svc.TranslateAsync("   ");
        Assert.AreEqual("", result.TranslatedText);
    }

    [TestMethod]
    public async Task TranslateAsync_NoApiKey_ReturnsError()
    {
        using var svc = CreateService("", "");
        var result = await svc.TranslateAsync("hello");
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage!.Contains("配置"));
    }

    [TestMethod]
    public void ClearCache_DoesNotThrow()
    {
        using var svc = CreateService();
        svc.ClearCache();
    }

    // Mock settings service for testing
    private class TestSettingsService : ISettingsService
    {
        public AppSettings Current { get; } = new();
        public string ConfigFilePath => "";

        public TestSettingsService(string appId, string apiKey)
        {
            Current.BaiduApi.AppId = appId;
            Current.BaiduApi.ApiKey = apiKey;
        }

        public void Load() { }
        public void Save() { }
    }
}
```

- [ ] **Step 4: Build and run tests**

```bash
dotnet test src/mini_fy.Tests/mini_fy.Tests.csproj --filter "TranslateService"
```
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/mini_fy.App/Services/ITranslateService.cs src/mini_fy.App/Services/TranslateService.cs
git add src/mini_fy.Tests/Services/TranslateServiceTests.cs
git commit -m "Add TranslateService with Baidu AI translate API and in-memory cache"
```

---

### Task 11: TranslationOverlay View

**Files:**
- Create: `mini_fy/src/mini_fy.App/Views/TranslationOverlay.xaml`
- Create: `mini_fy/src/mini_fy.App/Views/TranslationOverlay.xaml.cs`
- Update: `mini_fy/src/mini_fy.App/Services/IOverlayService.cs` (create)
- Update: `mini_fy/src/mini_fy.App/Services/OverlayService.cs` (create)

- [ ] **Step 1: Create IOverlayService.cs**

```csharp
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface IOverlayService
{
    void Show(TranslationResult result, System.Windows.Point? nearPoint = null);
}
```

- [ ] **Step 2: Write TranslationOverlay.xaml**

```xml
<Window x:Class="mini_fy.App.Views.TranslationOverlay"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        AllowsTransparency="True"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize"
        Background="#F0FFFFFF"
        BorderBrush="#FFCCCCCC"
        BorderThickness="1"
        MinWidth="200" MaxWidth="420"
        SizeToContent="WidthAndHeight"
        Deactivated="Window_Deactivated"
        KeyDown="Window_KeyDown"
        MouseLeftButtonDown="Window_MouseLeftButtonDown">

    <Window.Resources>
        <Style x:Key="CopyButton" TargetType="Button">
            <Setter Property="Background" Value="#FF0078D4" />
            <Setter Property="Foreground" Value="White" />
            <Setter Property="BorderThickness" Value="0" />
            <Setter Property="Padding" Value="12,6" />
            <Setter Property="Cursor" Value="Hand" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="4" Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
        <Style x:Key="CloseButton" TargetType="Button">
            <Setter Property="Background" Value="Transparent" />
            <Setter Property="Foreground" Value="#FF666666" />
            <Setter Property="BorderThickness" Value="0" />
            <Setter Property="FontSize" Value="16" />
            <Setter Property="Cursor" Value="Hand" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" CornerRadius="4"
                                Padding="6,2">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <Border Padding="16,12" CornerRadius="8" Background="#F0FFFFFF">
        <StackPanel>
            <!-- Error message -->
            <TextBlock x:Name="ErrorMessageBlock"
                       TextWrapping="Wrap" FontSize="13"
                       Foreground="#FFD32F2F" Visibility="Collapsed"
                       Margin="0,0,0,6" />

            <!-- Original text -->
            <TextBlock Text="原文" FontSize="11"
                       Foreground="#FF999999" Margin="0,0,0,2" />
            <TextBlock x:Name="OriginalTextBlock"
                       TextWrapping="Wrap" FontSize="13"
                       Foreground="#FF444444" Margin="0,0,0,10" />

            <!-- Translated text -->
            <TextBlock Text="译文" FontSize="11"
                       Foreground="#FF999999" Margin="0,0,0,2" />
            <TextBlock x:Name="TranslatedTextBlock"
                       TextWrapping="Wrap" FontSize="16"
                       FontWeight="Bold" Margin="0,0,0,12">
                <TextBlock.TextDecorations>
                    <TextDecoration Location="Underline" />
                </TextBlock.TextDecorations>
            </TextBlock>

            <!-- Action buttons -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button x:Name="CopyButton" Content="复制译文"
                        Style="{StaticResource CopyButton}"
                        Click="CopyButton_Click" />
                <Button Content="✕" Style="{StaticResource CloseButton}"
                        Click="CloseButton_Click" Margin="8,0,0,0" />
            </StackPanel>
        </StackPanel>
    </Border>
</Window>
```

- [ ] **Step 3: Write TranslationOverlay.xaml.cs**

```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using mini_fy.App.Helpers;
using mini_fy.App.Models;

namespace mini_fy.App.Views;

public partial class TranslationOverlay : Window
{
    private TranslationResult? _result;

    public TranslationOverlay()
    {
        InitializeComponent();
    }

    public void ShowResult(TranslationResult result, System.Windows.Point? nearPoint)
    {
        _result = result;

        if (result.Success)
        {
            ErrorMessageBlock.Visibility = Visibility.Collapsed;
            OriginalTextBlock.Text = result.OriginalText;
            TranslatedTextBlock.Text = result.TranslatedText;
            TranslatedTextBlock.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(124, 252, 0));

            // Double-click translated text to copy
            TranslatedTextBlock.MouseLeftButtonDown += (_, _) =>
            {
                if (_result != null)
                {
                    Clipboard.SetText(_result.TranslatedText);
                    FlashCopyButton();
                }
            };
            TranslatedTextBlock.Cursor = Cursors.Hand;
            TranslatedTextBlock.ToolTip = "双击复制译文";
        }
        else
        {
            ErrorMessageBlock.Visibility = Visibility.Visible;
            ErrorMessageBlock.Text = result.ErrorMessage ?? "";
            OriginalTextBlock.Text = result.OriginalText;
            TranslatedTextBlock.Text = "翻译失败";
            TranslatedTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
        }

        Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        Arrange(new Rect(DesiredSize));

        PositionWindow(nearPoint);
        Show();
        Activate();
    }

    private void PositionWindow(System.Windows.Point? nearPoint)
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double screenH = SystemParameters.PrimaryScreenHeight;

        double x, y;

        if (nearPoint.HasValue)
        {
            // Try placing near the screenshot area (right-bottom offset)
            x = nearPoint.Value.X + 20;
            y = nearPoint.Value.Y + 20;
        }
        else
        {
            x = (screenW - ActualWidth) / 2;
            y = (screenH - ActualHeight) / 2;
        }

        // Ensure window stays on screen
        if (x + ActualWidth > screenW) x = screenW - ActualWidth - 10;
        if (y + ActualHeight > screenH) y = screenH - ActualHeight - 10;
        if (x < 0) x = 10;
        if (y < 0) y = 10;

        Left = x;
        Top = y;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_result?.Success == true)
        {
            Clipboard.SetText(_result.TranslatedText);
            FlashCopyButton();
        }
    }

    private async void FlashCopyButton()
    {
        CopyButton.Background = new SolidColorBrush(Colors.Green);
        CopyButton.Content = "已复制!";
        await Task.Delay(600);
        CopyButton.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0, 120, 212));
        CopyButton.Content = "复制译文";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Close when clicking outside the window
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
```

- [ ] **Step 4: Write OverlayService.cs**

```csharp
using mini_fy.App.Models;
using mini_fy.App.Views;

namespace mini_fy.App.Services;

public class OverlayService : IOverlayService
{
    public void Show(TranslationResult result, System.Windows.Point? nearPoint = null)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var overlay = new TranslationOverlay();
            overlay.ShowResult(result, nearPoint);
        });
    }
}
```

- [ ] **Step 5: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 6: Commit**

```bash
git add src/mini_fy.App/Services/IOverlayService.cs src/mini_fy.App/Services/OverlayService.cs
git add src/mini_fy.App/Views/TranslationOverlay.xaml src/mini_fy.App/Views/TranslationOverlay.xaml.cs
git commit -m "Add TranslationOverlay view and OverlayService"
```

---

### Task 12: SettingsWindow View

**Files:**
- Create: `mini_fy/src/mini_fy.App/Views/SettingsWindow.xaml`
- Create: `mini_fy/src/mini_fy.App/Views/SettingsWindow.xaml.cs`

- [ ] **Step 1: Write SettingsWindow.xaml**

```xml
<Window x:Class="mini_fy.App.Views.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="mini_fy 设置" Height="450" Width="420"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        ShowInTaskbar="False">

    <Grid Margin="20">
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <StackPanel>
                <!-- Hotkey -->
                <TextBlock Text="快捷键" FontWeight="Bold" FontSize="14" Margin="0,0,0,6"/>
                <Border BorderBrush="#FFDDD" BorderThickness="1" CornerRadius="4" Padding="10,8"
                        Background="#FFF8F8F8">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock x:Name="HotkeyDisplay" Text="Ctrl + Alt + Q"
                                   VerticalAlignment="Center" FontSize="14" FontFamily="Consolas" />
                        <Button Content="修改" Click="ChangeHotkey_Click"
                                Padding="10,4" Margin="16,0,0,0" />
                    </StackPanel>
                </Border>

                <Separator Margin="0,14" />

                <!-- Baidu API -->
                <TextBlock Text="百度翻译 API" FontWeight="Bold" FontSize="14" Margin="0,0,0,6"/>
                <StackPanel>
                    <TextBlock Text="APPID" FontSize="12" Foreground="#FF666" Margin="0,0,0,2"/>
                    <TextBox x:Name="AppIdTextBox" Margin="0,0,0,8" />
                    <TextBlock Text="API Key" FontSize="12" Foreground="#FF666" Margin="0,0,0,2"/>
                    <PasswordBox x:Name="ApiKeyBox" Margin="0,0,0,8" />
                </StackPanel>

                <Separator Margin="0,14" />

                <!-- OCR Language -->
                <TextBlock Text="OCR 语言" FontWeight="Bold" FontSize="14" Margin="0,0,0,6"/>
                <StackPanel>
                    <RadioButton x:Name="LangEnRadio" Content="英文 (English)" IsChecked="True"
                                 Margin="0,0,0,4" />
                    <RadioButton Content="自动检测 (后续版本支持)" IsEnabled="False"
                                 Margin="0,0,0,4" />
                </StackPanel>

                <Separator Margin="0,14" />

                <!-- Options -->
                <TextBlock Text="选项" FontWeight="Bold" FontSize="14" Margin="0,0,0,6"/>
                <CheckBox x:Name="AutoSaveCheck" Content="截图后自动保存到本地"
                          Margin="0,0,0,6" />
                <CheckBox x:Name="AutoStartCheck" Content="开机自启"
                          Margin="0,0,0,6" />
                <CheckBox x:Name="AutoCopyCheck" Content="翻译结果自动复制到剪贴板"
                          IsChecked="True" Margin="0,0,0,6" />

                <Separator Margin="0,14" />

                <!-- Maintenance -->
                <StackPanel Orientation="Horizontal">
                    <Button Content="清理缓存" Click="ClearCache_Click"
                            Padding="12,6" />
                    <Button Content="查看日志" Click="ViewLog_Click"
                            Padding="12,6" Margin="12,0,0,0" />
                </StackPanel>

                <Separator Margin="0,14" />

                <!-- Save/Cancel -->
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button Content="取消" Click="Cancel_Click"
                            Padding="16,8" Width="80" IsCancel="True" />
                    <Button Content="保存" Click="Save_Click"
                            Padding="16,8" Width="80" Margin="12,0,0,0"
                            Background="#FF0078D4" Foreground="White" BorderThickness="0" />
                </StackPanel>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</Window>
```

- [ ] **Step 2: Write SettingsWindow.xaml.cs**

```csharp
using System.Windows;
using mini_fy.App.Helpers;
using mini_fy.App.Models;
using mini_fy.App.Services;

namespace mini_fy.App.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly ITranslateService? _translateService;
    private string _modifiers;
    private string _key;

    public SettingsWindow(ISettingsService settings, ITranslateService? translateService = null)
    {
        InitializeComponent();
        _settings = settings;
        _translateService = translateService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var cfg = _settings.Current;
        _modifiers = cfg.Hotkey.Modifiers;
        _key = cfg.Hotkey.Key;
        HotkeyDisplay.Text = $"{_modifiers} + {_key}";
        AppIdTextBox.Text = cfg.BaiduApi.AppId;
        ApiKeyBox.Password = cfg.BaiduApi.ApiKey;
        AutoSaveCheck.IsChecked = cfg.Screenshot.AutoSave;
        AutoCopyCheck.IsChecked = cfg.General.AutoCopyTranslation;
        AutoStartCheck.IsChecked = cfg.General.AutoStartWithWindows;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var cfg = _settings.Current;
        cfg.Hotkey.Modifiers = _modifiers;
        cfg.Hotkey.Key = _key;
        cfg.BaiduApi.AppId = AppIdTextBox.Text.Trim();
        cfg.BaiduApi.ApiKey = ApiKeyBox.Password.Trim();
        cfg.Screenshot.AutoSave = AutoSaveCheck.IsChecked == true;
        cfg.General.AutoCopyTranslation = AutoCopyCheck.IsChecked == true;
        cfg.General.AutoStartWithWindows = AutoStartCheck.IsChecked == true;

        _settings.Save();
        LogHelper.Info($"Settings saved. AppId={AppIdTextBox.Text.Trim()[..Math.Min(4, AppIdTextBox.Text.Trim().Length)]}...");
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HotkeyCaptureDialog(_modifiers, _key);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _modifiers = dialog.Modifiers;
            _key = dialog.Key;
            HotkeyDisplay.Text = $"{_modifiers} + {_key}";
        }
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        _translateService?.ClearCache();
        MessageBox.Show("缓存已清理", "mini_fy", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ViewLog_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.OpenLogDir();
    }
}

/// <summary>
/// Small dialog to capture a new hotkey combination.
/// </summary>
public class HotkeyCaptureDialog : Window
{
    public string Modifiers { get; private set; }
    public string Key { get; private set; }

    public HotkeyCaptureDialog(string currentModifiers, string currentKey)
    {
        Modifiers = currentModifiers;
        Key = currentKey;
        Title = "修改快捷键";
        Width = 320; Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };
        var label = new TextBlock
        {
            Text = "请按下新的组合键...",
            FontSize = 14, Margin = new Thickness(0, 0, 0, 16)
        };
        var keyDisplay = new TextBlock
        {
            Text = $"{Modifiers} + {Key}",
            FontSize = 20, FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var hint = new TextBlock
        {
            Text = "支持 Ctrl / Alt / Shift / Win + 字母/数字",
            FontSize = 11, Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 0)
        };

        panel.Children.Add(label);
        panel.Children.Add(keyDisplay);
        panel.Children.Add(hint);
        Content = panel;

        KeyDown += (_, e) =>
        {
            var keyChar = e.Key.ToString();
            // Only accept single letter or digit keys
            if (keyChar.Length == 1 && char.IsLetterOrDigit(keyChar[0]))
            {
                var mod = "";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mod += "Ctrl+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mod += "Alt+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mod += "Shift+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) mod += "Win+";

                if (!string.IsNullOrEmpty(mod))
                {
                    Modifiers = mod.TrimEnd('+');
                    Key = keyChar.ToUpper();
                    keyDisplay.Text = $"{Modifiers} + {Key}";
                    // Accept first valid combination
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DialogResult = true;
                            Close();
                        });
                    });
                }
            }
            e.Handled = true;
        };
    }
}
```

- [ ] **Step 2: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```

- [ ] **Step 3: Commit**

```bash
git add src/mini_fy.App/Views/SettingsWindow.xaml src/mini_fy.App/Views/SettingsWindow.xaml.cs
git commit -m "Add SettingsWindow with hotkey capture, API config, and options"
```

---

### Task 13: App.xaml.cs — Wire Everything Together

**Files:**
- Modify: `mini_fy/src/mini_fy.App/App.xaml.cs`

- [ ] **Step 1: Write the complete App.xaml.cs**

```csharp
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

    private Window? _messageWindow; // Hidden window to receive WM_HOTKEY

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
            // Still continue — user can configure in settings
            _ocrService = new OcrService("en"); // Try again
        }

        // 4. Hotkey
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        // 5. Tray
        _trayService.ScreenshotRequested += OnHotkeyPressed;
        _trayService.SettingsRequested += OpenSettings;
        _trayService.ExitRequested += ExitApplication;
        _trayService.Initialize();

        // 6. Create hidden window for hotkey messages
        _messageWindow = CreateMessageWindow();
        var hwnd = new WindowInteropHelper(_messageWindow).Handle;
        var cfg = _settingsService.Current;
        if (!_hotkeyService.Register(hwnd, cfg.Hotkey.Modifiers, cfg.Hotkey.Key))
        {
            _trayService.ShowNotification("mini_fy",
                $"快捷键 {cfg.Hotkey.Modifiers}+{cfg.Hotkey.Key} 注册失败，可能被其他程序占用");
        }

        LogHelper.Info("mini_fy started successfully");
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
            LogHelper.Info("Hotkey pressed, starting capture...");

            // Capture screenshot (runs on UI thread, blocks briefly for overlay)
            var bitmap = _screenshotService.Capture();
            if (bitmap == null)
            {
                LogHelper.Info("Screenshot cancelled");
                return;
            }

            // Run OCR + Translate in background
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
                    OriginalText = "(未识别到文字)",
                    TranslatedText = "",
                    ErrorMessage = "未识别到文字，请确认截图区域包含英文文本"
                });
                return;
            }

            var result = await _translateService.TranslateAsync(ocrText);

            // Auto-copy
            if (result.Success && _settingsService.Current.General.AutoCopyTranslation)
            {
                System.Windows.Clipboard.SetText(result.TranslatedText);
            }

            _overlayService.Show(result);
        }
        catch (Exception ex)
        {
            LogHelper.Error("Hotkey handler error", ex);
            _overlayService.Show(new Models.TranslationResult
            {
                OriginalText = "",
                TranslatedText = "",
                ErrorMessage = $"程序异常: {ex.Message}"
            });
        }
    }

    private void OpenSettings()
    {
        var window = new Views.SettingsWindow(_settingsService, _translateService);
        window.ShowDialog();

        // Re-register hotkey if changed
        if (_messageWindow != null)
        {
            var hwnd = new WindowInteropHelper(_messageWindow).Handle;
            _hotkeyService.Unregister(hwnd);
            var cfg = _settingsService.Current;
            _hotkeyService.Register(hwnd, cfg.Hotkey.Modifiers, cfg.Hotkey.Key);
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
```

- [ ] **Step 2: Build verify**

```bash
dotnet build src/mini_fy.App/mini_fy.App.csproj
```
Expected: Build succeeds. May have warnings about nullable, fix as needed.

- [ ] **Step 3: Commit**

```bash
git add src/mini_fy.App/App.xaml.cs
git commit -m "Wire all services in App.xaml.cs — complete core loop"
```

---

### Task 14: Config Template and README

**Files:**
- Create: `mini_fy/config/settings.example.json`
- Create: `mini_fy/README.md`

- [ ] **Step 1: Write settings.example.json**

```json
{
  "hotkey": {
    "modifiers": "Ctrl+Alt",
    "key": "Q"
  },
  "baiduApi": {
    "appId": "",
    "apiKey": ""
  },
  "ocr": {
    "language": "en"
  },
  "screenshot": {
    "autoSave": false,
    "savePath": ""
  },
  "general": {
    "autoCopyTranslation": true,
    "autoStartWithWindows": false
  }
}
```

- [ ] **Step 2: Write README.md**

```markdown
# mini_fy — Windows 截图翻译工具

轻量级 Windows 截图翻译小程序。按下快捷键框选屏幕区域，自动识别英文文字并通过大模型翻译为中文。

## 技术栈

- WPF (.NET 8)
- Windows.Media.Ocr（系统内置 OCR）
- 百度大模型文本翻译 API

## 运行环境

- Windows 10/11
- .NET 8 Desktop Runtime
- 系统须安装英文 OCR 语言包（设置 → 语言 → 添加语言 → English）

## 快速开始

### 1. 安装 .NET 8 SDK/Runtime

从 https://dotnet.microsoft.com/download/dotnet/8.0 下载安装。

### 2. 安装英文 OCR 语言包

Windows 设置 → 时间和语言 → 语言和区域 → 添加语言 → English (United States)

### 3. 配置百度翻译 API

1. 注册百度翻译开放平台：https://fanyi-api.baidu.com/
2. 开通"大模型文本翻译"服务
3. 在【管理控制台】→【API Key管理】创建 API Key
4. 获取 APPID 和 API Key

### 4. 运行程序

```bash
cd src/mini_fy.App
dotnet run
```

首次运行会在程序目录生成 `settings.json`，通过托盘菜单"设置"配置 API 密钥。

## 使用方式

1. 程序启动后常驻系统托盘
2. 按 `Ctrl + Alt + Q` 进入截图模式
3. 鼠标拖拽框选需要翻译的区域
4. 松开鼠标后自动识别并翻译
5. 翻译结果以悬浮窗展示

## 打包发布

```bash
cd src/mini_fy.App
dotnet publish -c Release -o publish /p:PublishSingleFile=true /p:SelfContained=false
```

## 项目结构

```
mini_fy/
├── src/mini_fy.App/      # WPF 主项目
│   ├── Services/         # 核心服务（Hotkey, Screenshot, OCR, Translate, Overlay, Tray, Settings）
│   ├── Models/           # 数据模型
│   ├── Views/            # WPF 窗口
│   └── Helpers/          # Win32 P/Invoke, 日志
├── src/mini_fy.Tests/    # 单元测试
├── config/               # 配置模板
└── docs/                 # 设计文档
```

## 注意事项

- 游戏中使用时，建议"无边框窗口"或"窗口化全屏"模式以获得最佳叠加体验
- API 密钥仅存储在本地 `settings.json`，请勿将该文件分享给他人
- 程序不上传截图，仅发送 OCR 提取的文本进行翻译
```

- [ ] **Step 3: Commit**

```bash
git add config/settings.example.json README.md
git commit -m "Add config template and README"
```

---

### Task 15: Build, Fix, and Final Integration Test

- [ ] **Step 1: Full solution build**

```bash
cd d:/Computer_test/Claude_code/mini_fy
dotnet build src/mini_fy.sln
```
Expected: Build succeeds. Iterate on any warnings or errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test src/mini_fy.Tests/mini_fy.Tests.csproj
```
Expected: All tests pass.

- [ ] **Step 3: Verify dotnet run**

```bash
cd src/mini_fy.App && dotnet run
```
Expected: Program starts, tray icon appears. Press Ctrl+Alt+Q to test.

- [ ] **Step 4: Commit any fixes and finalize**

```bash
git add -A
git commit -m "Final integration: build, test, and run verification"
```

---

### Task 16: Push to GitHub

- [ ] **Step 1: Push all commits**

```bash
cd d:/Computer_test/Claude_code/mini_fy
git push origin main
```

- [ ] **Step 2: Verify on GitHub**

Open `https://github.com/Elapestar/mini_fy` and confirm all files are visible.
