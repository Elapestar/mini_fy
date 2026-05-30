namespace mini_fy.App.Models;

/// <summary>
/// A translated text block, paired with its original.
/// </summary>
public class TranslatedBlock
{
    public string OriginalText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public int BlockIndex { get; init; }
}
