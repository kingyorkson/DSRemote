using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
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
    }

    private void StopEmulation_Click(object sender, RoutedEventArgs e)
    {
        _vm.StopEmulation();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var wizard = new SetupWizard();
        wizard.ShowDialog();
        if (wizard.Result != null)
        {
            _vm.CompleteSetup(wizard.Result);
        }
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

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _slideTimer?.Stop();
        _captureTimer?.Stop();
    }
}
