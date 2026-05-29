using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using mini_fy.App.Helpers;
using mini_fy.App.Models;

namespace mini_fy.App.Services;

public class TranslateService : ITranslateService, IDisposable
{
    private const string ApiUrl = "https://fanyi-api.baidu.com/ait/api/aiTextTranslate";
    private const int TimeoutSeconds = 10;

    private readonly ISettingsService _settings;
    private HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private bool _disposed;

    public TranslateService(ISettingsService settings)
    {
        _settings = settings;
        _httpClient = CreateHttpClient();
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        if (_settings.Current.General.BypassProxy)
        {
            handler.UseProxy = false;
            handler.Proxy = null!;
        }
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
    }

    /// <summary>Rebuild HttpClient when proxy setting changes.</summary>
    public void RefreshProxySettings()
    {
        var old = _httpClient;
        _httpClient = CreateHttpClient();
        old.Dispose();
    }

    public async Task<TranslationResult> TranslateAsync(string text, string from = "en", string to = "zh")
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult
            {
                OriginalText = text,
                TranslatedText = "",
                SourceLanguage = from,
                TargetLanguage = to
            };

        // Check cache
        var cacheKey = $"{from}:{to}:{text}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            LogHelper.Info($"Translation cache hit: \"{text}\" -> \"{cached}\"");
            return new TranslationResult
            {
                OriginalText = text, TranslatedText = cached,
                SourceLanguage = from, TargetLanguage = to, IsFromCache = true
            };
        }

        try
        {
            var config = _settings.Current.BaiduApi;
            if (string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.ApiKey))
            {
                return new TranslationResult
                {
                    OriginalText = text, TranslatedText = "",
                    SourceLanguage = from, TargetLanguage = to,
                    ErrorMessage = "请先在设置中配置百度翻译 API 的 APPID 和 API Key"
                };
            }

            var payload = new
            {
                appid = config.AppId,
                q = text,
                from,
                to
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            LogHelper.Info($"Translation request: \"{text}\" ({from}->{to}), " +
                           $"API Key={LogHelper.MaskKey(config.ApiKey)}");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var httpError = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "API Key 无效或未授权",
                    System.Net.HttpStatusCode.Forbidden => "API 权限不足",
                    _ => $"翻译服务返回错误 ({(int)response.StatusCode})"
                };
                LogHelper.Error($"Translation HTTP error: {response.StatusCode}");
                return ErrorResult(text, from, to, httpError);
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            var json = doc.RootElement;

            // Check for error
            if (json.TryGetProperty("error_code", out var errorCode))
            {
                var errorMsg = json.TryGetProperty("error_msg", out var msg)
                    ? msg.GetString() ?? "" : "";
                var codeStr = errorCode.ValueKind == JsonValueKind.String
                    ? errorCode.GetString()!
                    : errorCode.GetRawText();
                var userMsg = MapErrorCode(codeStr, errorMsg);
                LogHelper.Error($"Translation API error: {codeStr} - {errorMsg}");
                return ErrorResult(text, from, to, userMsg);
            }

            // Parse success
            if (json.TryGetProperty("trans_result", out var transResult)
                && transResult.ValueKind == JsonValueKind.Array
                && transResult.GetArrayLength() > 0)
            {
                var dst = transResult[0].GetProperty("dst").GetString() ?? "";
                _cache[cacheKey] = dst;
                LogHelper.Info($"Translation success: \"{text}\" -> \"{dst}\"");
                return new TranslationResult
                {
                    OriginalText = text, TranslatedText = dst,
                    SourceLanguage = from, TargetLanguage = to
                };
            }

            return ErrorResult(text, from, to, "翻译结果格式异常");
        }
        catch (TaskCanceledException)
        {
            return ErrorResult(text, from, to, "翻译超时，请检查网络");
        }
        catch (HttpRequestException ex)
        {
            LogHelper.Error("Translation network error", ex);
            return ErrorResult(text, from, to, "网络异常，请检查网络连接");
        }
        catch (Exception ex)
        {
            LogHelper.Error("Translation unexpected error", ex);
            return ErrorResult(text, from, to, $"翻译失败: {ex.Message}");
        }
    }

    private static string MapErrorCode(string code, string defaultMsg)
    {
        return code switch
        {
            "52001" => "翻译超时，请重试",
            "52002" => "翻译服务异常，请重试",
            "52003" => "APPID 无效或服务未开通",
            "54000" => "请求参数错误",
            "54001" => "API Key 配置错误",
            "54003" => "请求太频繁，请稍后",
            "54004" => "翻译额度已用完",
            "59003" => "文本过长，请缩减截图区域",
            _ => defaultMsg
        };
    }

    private static TranslationResult ErrorResult(string text, string from, string to, string error)
    {
        return new TranslationResult
        {
            OriginalText = text, TranslatedText = "",
            SourceLanguage = from, TargetLanguage = to,
            ErrorMessage = error
        };
    }

    public void ClearCache()
    {
        _cache.Clear();
        LogHelper.Info("Translation cache cleared");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}
