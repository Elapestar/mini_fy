using System.Drawing;

namespace mini_fy.App.Services;

public interface IScreenshotService
{
    /// <summary>Open screenshot overlay, return cropped bitmap or null if cancelled.</summary>
    Bitmap? Capture();
}
