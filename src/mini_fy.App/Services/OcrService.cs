using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using mini_fy.App.Helpers;
using mini_fy.App.Models;

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
                using var swBitmap = await BitmapToSoftwareBitmap(bitmap);
                var result = await _engine.RecognizeAsync(swBitmap);
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

    public async Task<List<OcrTextBlock>> RecognizeBlocksAsync(Bitmap bitmap)
    {
        return await Task.Run(async () =>
        {
            var blocks = new List<OcrTextBlock>();
            try
            {
                using var swBitmap = await BitmapToSoftwareBitmap(bitmap);
                var result = await _engine.RecognizeAsync(swBitmap);

                if (result.Lines.Count == 0) return blocks;

                // Group lines into blocks by vertical spacing
                var lines = result.Lines;
                double avgHeight = lines.Average(l => l.Words.Count > 0
                    ? l.Words.Average(w => w.BoundingRect.Height) : 20);

                int blockIdx = 0;
                var blockLines = new List<OcrLine> { lines[0] };
                double blockTop = lines[0].Words.Count > 0 ? lines[0].Words[0].BoundingRect.Y : 0;
                double blockLeft = double.MaxValue, blockRight = 0;

                for (int i = 1; i < lines.Count; i++)
                {
                    double prevBottom = blockLines.Last().Words.Count > 0
                        ? blockLines.Last().Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height) : 0;
                    double currTop = lines[i].Words.Count > 0
                        ? lines[i].Words.Min(w => w.BoundingRect.Y) : 0;

                    double gap = currTop - prevBottom;

                    if (gap > avgHeight * 2.0 && blockLines.Count > 0)
                    {
                        // Gap too large → finish current block, start new one
                        blocks.Add(BuildBlock(blockLines, blockIdx++, blockTop, blockLeft, blockRight));
                        blockLines.Clear();
                        blockTop = lines[i].Words.Count > 0 ? lines[i].Words[0].BoundingRect.Y : 0;
                        blockLeft = double.MaxValue;
                        blockRight = 0;
                    }
                    blockLines.Add(lines[i]);
                }

                // Last block
                if (blockLines.Count > 0)
                    blocks.Add(BuildBlock(blockLines, blockIdx, blockTop, blockLeft, blockRight));

                LogHelper.Info($"OCR blocks: {blocks.Count} blocks, {result.Lines.Count} lines");
            }
            catch (Exception ex)
            {
                LogHelper.Error("OCR block recognition failed", ex);
            }
            return blocks;
        });
    }

    private static OcrTextBlock BuildBlock(List<OcrLine> lines, int blockIdx,
        double blockTop, double blockLeft, double blockRight)
    {
        var text = string.Join(" ", lines.Select(l => l.Text)).Trim();
        double left = double.MaxValue, right = 0, bottom = 0;
        foreach (var line in lines)
        {
            foreach (var word in line.Words)
            {
                var r = word.BoundingRect;
                if (r.X < left) left = r.X;
                if (r.X + r.Width > right) right = r.X + r.Width;
                if (r.Y + r.Height > bottom) bottom = r.Y + r.Height;
            }
        }
        double w = right - left;
        double h = bottom - blockTop;
        return new OcrTextBlock
        {
            Text = text,
            BlockIndex = blockIdx,
            X = left > 10000 ? 0 : left,
            Y = blockTop > 10000 ? 0 : blockTop,
            Width = w < 0 ? 100 : w,
            Height = h < 0 ? 20 : h
        };
    }

    private static async Task<SoftwareBitmap> BitmapToSoftwareBitmap(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;

        var randomAccessStream = memoryStream.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
            || softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap,
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }
        return softwareBitmap;
    }

    private static string Truncate(string s, int len)
        => s.Length <= len ? s : s[..len] + "...";
}
