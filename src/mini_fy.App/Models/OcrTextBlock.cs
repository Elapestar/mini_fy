namespace mini_fy.App.Models;

/// <summary>
/// A contiguous block of text detected by OCR, with position info.
/// </summary>
public class OcrTextBlock
{
    public string Text { get; init; } = "";
    public int BlockIndex { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}
