using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DSRemote.Views;

public partial class TopScreenWindow : Window
{
    private bool _isFullscreen;

    public TopScreenWindow()
    {
        InitializeComponent();
    }

    public void UpdateImage(byte[] jpegData)
    {
        if (!Dispatcher.HasShutdownStarted)
        {
            Dispatcher.Invoke(() =>
            {
                using var ms = new MemoryStream(jpegData);
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                TopScreenImage.Source = img;
            });
        }
    }

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
    {
        _isFullscreen = !_isFullscreen;

        if (_isFullscreen)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            Topmost = true;
            FullscreenBtn.Content = "⛶";
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Normal;
            Width = 800;
            Height = 480;
            Topmost = true;
            FullscreenBtn.Content = "⛶";
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _isFullscreen)
            ToggleFullscreen_Click(sender, e);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
