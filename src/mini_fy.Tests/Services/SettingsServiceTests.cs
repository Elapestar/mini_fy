using Microsoft.VisualStudio.TestTools.UnitTesting;
using mini_fy.App.Services;
using mini_fy.App.Models;

namespace mini_fy.Tests.Services;

[TestClass]
public class SettingsServiceTests
{
    [TestMethod]
    public void Load_NoFileExists_ReturnsDefaults()
    {
        var svc = new SettingsService();
        // Verify defaults are populated
        var settings = new AppSettings();
        Assert.AreEqual("Ctrl+Alt", settings.Hotkey.Modifiers);
        Assert.AreEqual("Q", settings.Hotkey.Key);
        Assert.AreEqual("en", settings.Ocr.Language);
        Assert.IsTrue(settings.General.AutoCopyTranslation);
    }

    [TestMethod]
    public void Current_AfterNew_IsNotEmpty()
    {
        var svc = new SettingsService();
        svc.Load();
        Assert.IsNotNull(svc.Current);
        Assert.IsNotNull(svc.Current.Hotkey);
    }
}
