using System.Windows.Forms;
using System.Drawing;
using System.IO;
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
            g.Clear(Color.DodgerBlue);
            using var font = new Font("Microsoft YaHei", 16, FontStyle.Bold);
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
        if (_notifyIcon != null)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
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
