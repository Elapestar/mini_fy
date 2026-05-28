namespace mini_fy.App.Models;

public class TranslationResult
{
    public string OriginalText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public string SourceLanguage { get; init; } = "en";
    public string TargetLanguage { get; init; } = "zh";
    public bool IsFromCache { get; init; } = false;
    public string? ErrorMessage { get; init; }
    public bool Success => ErrorMessage == null;
}
