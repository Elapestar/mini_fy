using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
                var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
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
