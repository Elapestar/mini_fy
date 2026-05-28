using System.Drawing;
using mini_fy.App.Helpers;
using mini_fy.App.Views;

namespace mini_fy.App.Services;

public class ScreenshotService : IScreenshotService
{
    public Bitmap? Capture()
    {
        return System.Windows.Application.Current.Dispatcher.Invoke(() =>
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

        var bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h));
        return bitmap;
    }
}
