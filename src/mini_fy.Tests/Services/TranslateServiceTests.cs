using Microsoft.VisualStudio.TestTools.UnitTesting;
using mini_fy.App.Models;
using mini_fy.App.Services;

namespace mini_fy.Tests.Services;

[TestClass]
public class TranslateServiceTests
{
    private TranslateService CreateService(string appId = "test_id", string apiKey = "test_key")
    {
        var mockSettings = new TestSettingsService(appId, apiKey);
        return new TranslateService(mockSettings);
    }

    [TestMethod]
    public async Task TranslateAsync_EmptyText_ReturnsEmptyResult()
    {
        using var svc = CreateService();
        var result = await svc.TranslateAsync("");
        Assert.AreEqual("", result.TranslatedText);
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task TranslateAsync_WhitespaceText_ReturnsEmptyResult()
    {
        using var svc = CreateService();
        var result = await svc.TranslateAsync("   ");
        Assert.AreEqual("", result.TranslatedText);
    }

    [TestMethod]
    public async Task TranslateAsync_NoApiKey_ReturnsError()
    {
        using var svc = CreateService("", "");
        var result = await svc.TranslateAsync("hello");
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage!.Contains("配置"));
    }

    [TestMethod]
    public void ClearCache_DoesNotThrow()
    {
        using var svc = CreateService();
        svc.ClearCache();
    }

    // Mock settings service for testing
    private class TestSettingsService : ISettingsService
    {
        public AppSettings Current { get; } = new();
        public string ConfigFilePath => "";

        public TestSettingsService(string appId, string apiKey)
        {
            Current.BaiduApi.AppId = appId;
            Current.BaiduApi.ApiKey = apiKey;
        }

        public void Load() { }
        public void Save() { }
    }
}
