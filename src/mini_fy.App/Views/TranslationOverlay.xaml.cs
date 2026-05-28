using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using mini_fy.App.Models;

namespace mini_fy.App.Views;

public partial class TranslationOverlay : Window
{
    private TranslationResult? _result;

    public TranslationOverlay()
    {
        InitializeComponent();
    }

    public void ShowResult(TranslationResult result, System.Windows.Point? nearPoint)
    {
        _result = result;

        if (result.Success)
        {
            ErrorMessageBlock.Visibility = Visibility.Collapsed;
            OriginalTextBlock.Text = result.OriginalText;
            TranslatedTextBlock.Text = result.TranslatedText;
            TranslatedTextBlock.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(124, 252, 0));

            // Double-click translated text to copy
            TranslatedTextBlock.MouseLeftButtonDown += (_, _) =>
            {
                if (_result != null)
                {
                    System.Windows.Clipboard.SetText(_result.TranslatedText);
                    FlashCopyButton();
                }
            };
            TranslatedTextBlock.Cursor = System.Windows.Input.Cursors.Hand;
            TranslatedTextBlock.ToolTip = "双击复制译文";
        }
        else
        {
            ErrorMessageBlock.Visibility = Visibility.Visible;
            ErrorMessageBlock.Text = result.ErrorMessage ?? "";
            OriginalTextBlock.Text = result.OriginalText;
            TranslatedTextBlock.Text = "翻译失败";
            TranslatedTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
        }

        Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        Arrange(new Rect(DesiredSize));

        PositionWindow(nearPoint);
        Show();
        Activate();
    }

    private void PositionWindow(System.Windows.Point? nearPoint)
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double screenH = SystemParameters.PrimaryScreenHeight;

        double x, y;

        if (nearPoint.HasValue)
        {
            // Try placing near the screenshot area (right-bottom offset)
            x = nearPoint.Value.X + 20;
            y = nearPoint.Value.Y + 20;
        }
        else
        {
            x = (screenW - ActualWidth) / 2;
            y = (screenH - ActualHeight) / 2;
        }

        // Ensure window stays on screen
        if (x + ActualWidth > screenW) x = screenW - ActualWidth - 10;
        if (y + ActualHeight > screenH) y = screenH - ActualHeight - 10;
        if (x < 0) x = 10;
        if (y < 0) y = 10;

        Left = x;
        Top = y;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_result?.Success == true)
        {
            System.Windows.Clipboard.SetText(_result.TranslatedText);
            FlashCopyButton();
        }
    }

    private async void FlashCopyButton()
    {
        CopyButton.Background = new SolidColorBrush(Colors.Green);
        CopyButton.Content = "已复制!";
        await Task.Delay(600);
        CopyButton.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0, 120, 212));
        CopyButton.Content = "复制译文";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Close when clicking outside the window
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
