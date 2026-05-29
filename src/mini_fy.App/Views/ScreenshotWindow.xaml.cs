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
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        Win32Api.SetWindowPos(hwnd, Win32Api.HWND_TOPMOST, 0, 0, 0, 0,
            Win32Api.SWP_NOMOVE | Win32Api.SWP_NOSIZE | Win32Api.SWP_NOACTIVATE | Win32Api.SWP_SHOWWINDOW);
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(MainGrid);
        _isSelecting = true;

        // Show dim overlay and selection border
        DimOverlay.Visibility = Visibility.Visible;
        SelectionRect.Visibility = Visibility.Visible;
        SelectionCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isSelecting) return;
        var pos = e.GetPosition(MainGrid);
        var x = Math.Min(pos.X, _startPoint.X);
        var y = Math.Min(pos.Y, _startPoint.Y);
        var w = Math.Abs(pos.X - _startPoint.X);
        var h = Math.Abs(pos.Y - _startPoint.Y);

        // Position the selection border
        SelectionRect.Width = w;
        SelectionRect.Height = h;
        SelectionRect.Margin = new Thickness(x, y, 0, 0);
        SelectionRect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        SelectionRect.VerticalAlignment = System.Windows.VerticalAlignment.Top;

        // Dark overlay with a "hole" for the selection
        UpdateDimOverlay(x, y, w, h);
    }

    private void UpdateDimOverlay(double x, double y, double w, double h)
    {
        if (w <= 0 || h <= 0) return;

        var screenRect = new RectangleGeometry(
            new Rect(0, 0, MainGrid.ActualWidth, MainGrid.ActualHeight));
        var holeRect = new RectangleGeometry(new Rect(x, y, w, h));

        DimOverlay.Data = new CombinedGeometry(
            GeometryCombineMode.Exclude, screenRect, holeRect);
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        SelectionCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(MainGrid);
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
            Close();
        }
    }

    private void CropAndClose(int x, int y, int w, int h)
    {
        try
        {
            // WPF uses 96 DPI device-independent pixels.
            // The bitmap may have the screen's physical DPI (e.g. 120 at 125% scaling).
            // Scale WPF coordinates to bitmap pixel coordinates.
            double dpiScaleX = _fullScreenBitmap!.HorizontalResolution / 96.0;
            double dpiScaleY = _fullScreenBitmap!.VerticalResolution / 96.0;
            int bx = (int)(x * dpiScaleX);
            int by = (int)(y * dpiScaleY);
            int bw = (int)(w * dpiScaleX);
            int bh = (int)(h * dpiScaleY);

            ResultBitmap = _fullScreenBitmap!.Clone(
                new System.Drawing.Rectangle(bx, by, bw, bh),
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
            // Use bitmap's own DPI so WPF maps image 1:1 to screen pixels
            var bmpSrc = BitmapSource.Create(
                data.Width, data.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
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
