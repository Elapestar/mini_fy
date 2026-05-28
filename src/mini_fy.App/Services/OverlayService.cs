using mini_fy.App.Models;
using mini_fy.App.Views;

namespace mini_fy.App.Services;

public class OverlayService : IOverlayService
{
    public void Show(TranslationResult result, System.Windows.Point? nearPoint = null)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var overlay = new TranslationOverlay();
            overlay.ShowResult(result, nearPoint);
        });
    }
}
