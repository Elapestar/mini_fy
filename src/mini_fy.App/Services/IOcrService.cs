using System.Drawing;
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface IOcrService
{
    /// <summary>Recognize English text from bitmap. Returns empty string if nothing found.</summary>
    Task<string> RecognizeAsync(Bitmap bitmap);

    /// <summary>Recognize text and split into non-contiguous blocks by line spacing.</summary>
    Task<List<OcrTextBlock>> RecognizeBlocksAsync(Bitmap bitmap);
}
