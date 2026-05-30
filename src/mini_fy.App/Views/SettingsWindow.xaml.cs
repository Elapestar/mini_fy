using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using mini_fy.App.Helpers;
using mini_fy.App.Services;

namespace mini_fy.App.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settings;
    private readonly ITranslateService? _translateService;
    private string _modifiers = "Ctrl+Alt";
    private string _key = "Q";
    private string _copyModifiers = "Ctrl+Alt";
    private string _copyKey = "S";

    public SettingsWindow(ISettingsService settings, ITranslateService? translateService = null)
    {
        InitializeComponent();
        _settings = settings;
        _translateService = translateService;
        LoadSettings();
        ModeManualRadio.Checked += (_, _) => UpdateModePanel();
        ModeAutoRadio.Checked += (_, _) => UpdateModePanel();
    }

    private void LoadSettings()
    {
        var cfg = _settings.Current;
        _modifiers = cfg.Hotkey.Modifiers;
        _key = cfg.Hotkey.Key;
        _copyModifiers = cfg.General.CopyHotkeyModifiers;
        _copyKey = cfg.General.CopyHotkeyKey;

        HotkeyDisplay.Text = $"{_modifiers} + {_key}";
        CopyHotkeyDisplay.Text = $"{_copyModifiers} + {_copyKey}";
        AppIdTextBox.Text = cfg.BaiduApi.AppId;
        ApiKeyBox.Password = cfg.BaiduApi.ApiKey;
        AutoSaveCheck.IsChecked = cfg.Screenshot.AutoSave;
        AutoCopyCheck.IsChecked = cfg.General.AutoCopyTranslation;
        AutoStartCheck.IsChecked = cfg.General.AutoStartWithWindows;
        BypassProxyCheck.IsChecked = cfg.General.BypassProxy;

        if (cfg.General.TranslateMode == Models.TranslateMode.Auto)
            ModeAutoRadio.IsChecked = true;
        else
            ModeManualRadio.IsChecked = true;

        AutoCloseSecondsBox.Text = cfg.General.AutoCloseSeconds.ToString();
        UpdateModePanel();
    }

    private void UpdateModePanel()
    {
        bool isAuto = ModeAutoRadio.IsChecked == true;
        AutoClosePanel.Visibility = isAuto ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var cfg = _settings.Current;
        cfg.Hotkey.Modifiers = _modifiers;
        cfg.Hotkey.Key = _key;
        cfg.General.CopyHotkeyModifiers = _copyModifiers;
        cfg.General.CopyHotkeyKey = _copyKey;
        cfg.General.TranslateMode = ModeAutoRadio.IsChecked == true
            ? Models.TranslateMode.Auto : Models.TranslateMode.Manual;
        if (int.TryParse(AutoCloseSecondsBox.Text, out int secs) && secs >= 3 && secs <= 60)
            cfg.General.AutoCloseSeconds = secs;

        cfg.BaiduApi.AppId = AppIdTextBox.Text.Trim();
        cfg.BaiduApi.ApiKey = ApiKeyBox.Password.Trim();
        cfg.Screenshot.AutoSave = AutoSaveCheck.IsChecked == true;
        cfg.General.AutoCopyTranslation = AutoCopyCheck.IsChecked == true;
        cfg.General.AutoStartWithWindows = AutoStartCheck.IsChecked == true;
        cfg.General.BypassProxy = BypassProxyCheck.IsChecked == true;

        _settings.Save();
        _translateService?.RefreshProxySettings();
        LogHelper.Info("Settings saved.");
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HotkeyCaptureDialog(_modifiers, _key);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _modifiers = dialog.Modifiers;
            _key = dialog.Key;
            HotkeyDisplay.Text = $"{_modifiers} + {_key}";
        }
    }

    private void ChangeCopyHotkey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HotkeyCaptureDialog(_copyModifiers, _copyKey);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _copyModifiers = dialog.Modifiers;
            _copyKey = dialog.Key;
            CopyHotkeyDisplay.Text = $"{_copyModifiers} + {_copyKey}";
        }
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        _translateService?.ClearCache();
        System.Windows.MessageBox.Show("缓存已清理", "mini_fy", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ViewLog_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.OpenLogDir();
    }
}

/// <summary>
/// Small dialog to capture a new hotkey combination.
/// Press any modifier+key combination to set it.
/// </summary>
public class HotkeyCaptureDialog : Window
{
    public string Modifiers { get; private set; }
    public string Key { get; private set; }

    public HotkeyCaptureDialog(string currentModifiers, string currentKey)
    {
        Modifiers = currentModifiers;
        Key = currentKey;
        Title = "修改快捷键";
        Width = 320; Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };
        var label = new TextBlock
        {
            Text = "请按下新的组合键...",
            FontSize = 14, Margin = new Thickness(0, 0, 0, 16)
        };
        var keyDisplay = new TextBlock
        {
            Text = $"{Modifiers} + {Key}",
            FontSize = 20, FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };
        var hint = new TextBlock
        {
            Text = "支持 Ctrl / Alt / Shift / Win + 字母/数字",
            FontSize = 11, Foreground = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(0, 10, 0, 0)
        };

        panel.Children.Add(label);
        panel.Children.Add(keyDisplay);
        panel.Children.Add(hint);
        Content = panel;

        KeyDown += (_, e) =>
        {
            var keyChar = e.Key.ToString();
            // Only accept single letter or digit keys
            if (keyChar.Length == 1 && char.IsLetterOrDigit(keyChar[0]))
            {
                var mod = "";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mod += "Ctrl+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) mod += "Alt+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) mod += "Shift+";
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) mod += "Win+";

                if (!string.IsNullOrEmpty(mod))
                {
                    Modifiers = mod.TrimEnd('+');
                    Key = keyChar.ToUpper();
                    keyDisplay.Text = $"{Modifiers} + {Key}";
                    // Accept first valid combination after a brief delay
                    Task.Delay(300).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DialogResult = true;
                            Close();
                        });
                    });
                }
            }
            e.Handled = true;
        };
    }
}
