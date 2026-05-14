using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DSRemote.Models;
using DSRemote.ViewModels;
using DSRemote.Views;

namespace DSRemote;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private DispatcherTimer? _slideTimer;
    private DispatcherTimer? _captureTimer;
    private bool _slideMenuVisible;
    private TopScreenWindow? _topScreenWindow;
    private byte[]? _lastTopScreenData;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_vm.IsSetupComplete)
            {
                var wizard = new SetupWizard();
                if (wizard.ShowDialog() == true && wizard.Result != null)
                {
                    _vm.CompleteSetup(wizard.Result);
                }
                else
                {
                    Close();
                    return;
                }
            }

            StartSlideMenuWatcher();
            StartCaptureLoop();
            UpdatePcTopScreenButton();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during setup: {ex.Message}\n\n{ex.StackTrace}",
                "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void StartSlideMenuWatcher()
    {
        _slideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _slideTimer.Tick += (_, _) =>
        {
            var pos = Mouse.GetPosition(this);
            bool shouldShow = pos.X >= 0 && pos.X <= 5 && pos.Y > 36;

            if (shouldShow && !_slideMenuVisible)
            {
                _slideMenuVisible = true;
                AnimateSlideMenu(0);
            }
            else if (!shouldShow && _slideMenuVisible &&
                     (pos.X > 230 || pos.Y < 36 || pos.Y > ActualHeight))
            {
                _slideMenuVisible = false;
                AnimateSlideMenu(-220);
            }
        };
        _slideTimer.Start();
    }

    private void AnimateSlideMenu(double target)
    {
        var duration = TimeSpan.FromMilliseconds(150);
        var start = SlideMenu.Margin.Left;
        var frameTime = TimeSpan.FromMilliseconds(16);
        var steps = (int)(duration.TotalMilliseconds / frameTime.TotalMilliseconds);
        var step = 0;

        var timer = new DispatcherTimer { Interval = frameTime };
        timer.Tick += (_, _) =>
        {
            step++;
            var progress = (double)step / steps;
            var current = start + (target - start) * progress;
            SlideMenu.Margin = new Thickness(current, 0, 0, 0);
            if (step >= steps)
            {
                timer.Stop();
                SlideMenu.Margin = new Thickness(target, 0, 0, 0);
            }
        };
        timer.Start();
    }

    private void StartCaptureLoop()
    {
        _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += async (_, _) =>
        {
            if (!_vm.IsConnected) return;
            var hWnd = _vm.GetEmulatorWindowHandle();
            if (hWnd.HasValue)
            {
                await _vm.SendScreenshot(hWnd.Value);

                // Update top screen window if open
                if (_topScreenWindow != null)
                {
                    var (top, _) = _vm.CaptureScreens();
                    if (!string.IsNullOrEmpty(top))
                    {
                        var base64 = top.Replace("TOP_IMAGE:", "");
                        _lastTopScreenData = Convert.FromBase64String(base64);
                        _topScreenWindow.UpdateImage(_lastTopScreenData);
                    }
                }
            }
        };
        _captureTimer.Start();
    }

    private void PlayGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is GameRom game)
        {
            _ = _vm.LaunchGame(game);
        }
    }

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        _vm.DisconnectDevice();
        CloseTopScreenWindow();
    }

    private void StopEmulation_Click(object sender, RoutedEventArgs e)
    {
        _vm.StopEmulation();
    }

    private void PcTopScreen_Click(object sender, RoutedEventArgs e)
    {
        _vm.UsePcAsTopScreen = !_vm.UsePcAsTopScreen;

        if (_vm.UsePcAsTopScreen)
        {
            _topScreenWindow = new TopScreenWindow();
            _topScreenWindow.Closed += (_, _) =>
            {
                _topScreenWindow = null;
                _vm.UsePcAsTopScreen = false;
                UpdatePcTopScreenButton();
            };
            _topScreenWindow.Show();

            // Send state to phone
            _ = _vm.Config.UsePcAsTopScreen
                ? _vm.SendScreenshot(IntPtr.Zero) // just to trigger network
                : Task.CompletedTask;
        }
        else
        {
            CloseTopScreenWindow();
        }

        UpdatePcTopScreenButton();
    }

    private void CloseTopScreenWindow()
    {
        if (_topScreenWindow != null)
        {
            _topScreenWindow.Close();
            _topScreenWindow = null;
        }
    }

    private void UpdatePcTopScreenButton()
    {
        PcTopScreenBtn.Content = _vm.UsePcAsTopScreen
            ? "✓ Use PC as Top Screen"
            : "Use PC as Top Screen";
        PcTopScreenBtn.Background = _vm.UsePcAsTopScreen
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#32CD32"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e"));
    }

    private void InputBindings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputMappingDialog(
            _vm.Config.ButtonMappings,
            _vm.Config.DPadMappings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.ResultConfig != null)
        {
            _vm.Config.ButtonMappings = dialog.ResultConfig.ButtonMappings;
            _vm.Config.DPadMappings = dialog.ResultConfig.DPadMappings;
            _vm.Config.Save();
            _vm.CompleteSetup(_vm.Config);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new SetupWizard();
        wizard.ShowDialog();
        if (wizard.Result != null)
            _vm.CompleteSetup(wizard.Result);
    }

    private void RefreshGames_Click(object sender, RoutedEventArgs e)
    {
        _vm.RefreshGames();
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        var search = SearchBox.Text.ToLower();
        GameList.Items.Filter = o =>
        {
            if (o is GameRom game)
                return string.IsNullOrEmpty(search) || game.Name.ToLower().Contains(search);
            return true;
        };
    }

    private void GameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GameList.SelectedItem is GameRom selected)
        {
            _ = _vm.LaunchGame(selected);
            GameList.SelectedItem = null;
        }
    }

    private void SlideMenu_MouseEnter(object sender, RoutedEventArgs e)
    {
        _slideMenuVisible = true;
    }

    private KidsModeWindow? _kidsModeWindow;

    private void KidsBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_kidsModeWindow != null)
        {
            _kidsModeWindow.Focus();
            return;
        }

        _kidsModeWindow = new KidsModeWindow();
        _kidsModeWindow.Closed += (_, _) =>
        {
            if (_kidsModeWindow?.IsKidModeActive == true)
                _vm.IsKidModeActive = true;
            _kidsModeWindow = null;
        };
        _kidsModeWindow.Show();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _slideTimer?.Stop();
        _captureTimer?.Stop();
        CloseTopScreenWindow();
        _kidsModeWindow?.Close();
    }
}
