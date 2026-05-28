using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using mini_fy.App.Services;

namespace mini_fy.Tests.Services;

[TestClass]
public class OcrServiceTests
{
    [TestMethod]
    [Ignore("Requires Windows OCR English language pack to be installed")]
    public async Task RecognizeAsync_EmptyBitmap_ReturnsEmptyString()
    {
        var svc = new OcrService("en");
        using var bmp = new Bitmap(10, 10);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        var text = await svc.RecognizeAsync(bmp);
        Assert.AreEqual("", text);
    }

    [TestMethod]
    public void Constructor_InvalidLanguage_Throws()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new OcrService("xx-invalid-99"));
    }
}
