using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using mini_fy.App.Helpers;

namespace mini_fy.App.Views;

public partial class ScreenshotWindow : Window
{
    private Bitmap? _fullScreenBitmap;
    private System.Windows.Point _startPoint;
    private bool _isSelecting;

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
        System.Windows.Controls.Canvas.SetLeft(SelectionRect, _startPoint.X);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        SelectionCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isSelecting) return;
        var pos = e.GetPosition(SelectionCanvas);
        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);
        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);
        System.Windows.Controls.Canvas.SetLeft(SelectionRect, x);
        System.Windows.Controls.Canvas.SetTop(SelectionRect, y);
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

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
