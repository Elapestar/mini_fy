using System.Drawing;

namespace mini_fy.App.Services;

public interface IOcrService
{
    /// <summary>Recognize English text from bitmap. Returns empty string if nothing found.</summary>
    Task<string> RecognizeAsync(Bitmap bitmap);
}
