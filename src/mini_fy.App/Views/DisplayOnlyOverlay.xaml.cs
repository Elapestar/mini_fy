using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using mini_fy.App.Models;

namespace mini_fy.App.Views;

/// <summary>
/// Read-only overlay for auto-translate mode.
/// Shows translated blocks as white-background black text.
/// Immune to keyboard — does NOT close on key press or deactivation.
/// Auto-closes after a configurable number of seconds.
/// </summary>
public partial class DisplayOnlyOverlay : Window
{
    private readonly DispatcherTimer _timer;
    private int _remainingSeconds;

    public DisplayOnlyOverlay()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
    }

    public void ShowBlocks(List<TranslatedBlock> blocks, int autoCloseSeconds)
    {
        BlocksPanel.Children.Clear();

        foreach (var block in blocks)
        {
            var container = new Border
            {
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFEEEEEE")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 6, 0, 6)
            };

            var panel = new StackPanel();

            if (!string.IsNullOrWhiteSpace(block.OriginalText))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = block.OriginalText,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF999999")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            panel.Children.Add(new TextBlock
            {
                Text = block.TranslatedText,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                TextWrapping = TextWrapping.Wrap
            });

            container.Child = panel;
            BlocksPanel.Children.Add(container);
        }

        _remainingSeconds = autoCloseSeconds;
        UpdateTimerText();
        _timer.Start();

        // Left edge at screen center, vertically centered
        Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = DesiredSize;
        Left = SystemParameters.PrimaryScreenWidth / 2;
        Top = (SystemParameters.PrimaryScreenHeight - size.Height) / 2;

        Show();
        Activate();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            Close();
            return;
        }
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        TimerBlock.Text = $"{_remainingSeconds}s 后自动关闭";
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }
}
