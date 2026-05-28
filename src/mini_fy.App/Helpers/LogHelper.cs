using System.Diagnostics;
using System.IO;

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

    // Open log directory in Explorer
    public static void OpenLogDir()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            Process.Start("explorer.exe", LogDir);
        }
        catch { }
    }

    // Delete logs older than 7 days
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

    // Mask API key for safe logging (shows only last 4 chars)
    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length <= 4) return "****";
        return new string('*', key.Length - 4) + key[^4..];
    }
}
