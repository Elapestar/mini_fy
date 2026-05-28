using mini_fy.App.Models;

namespace mini_fy.App.Services;

public interface IOverlayService
{
    void Show(TranslationResult result, System.Windows.Point? nearPoint = null);
}
