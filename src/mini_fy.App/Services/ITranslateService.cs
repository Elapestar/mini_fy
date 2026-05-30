using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface ITranslateService
{
    Task<TranslationResult> TranslateAsync(string text, string from = "en", string to = "zh");
    Task<List<TranslatedBlock>> TranslateBlocksAsync(List<OcrTextBlock> blocks, string from = "en", string to = "zh");
    void ClearCache();
    void RefreshProxySettings();
}
