using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace DSRemote;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private System.Drawing.Bitmap? _trayBitmap;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _trayBitmap = CreateAppIcon();
        var iconHandle = _trayBitmap.GetHicon();
        _trayIcon = new TaskbarIcon
        {
            Icon = System.Drawing.Icon.FromHandle(iconHandle),
            ToolTipText = "DSRemote",
            Visibility = Visibility.Visible
        };

        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        var exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitMenuItem.Click += (_, _) => Shutdown();

        var showMenuItem = new System.Windows.Controls.MenuItem { Header = "Show DSRemote" };
        showMenuItem.Click += (_, _) => ShowMainWindow();

        _trayIcon.ContextMenu = new System.Windows.Controls.ContextMenu();
        _trayIcon.ContextMenu.Items.Add(showMenuItem);
        _trayIcon.ContextMenu.Items.Add(exitMenuItem);

        _mainWindow = new MainWindow();
        _mainWindow.Show();
    }

    private static System.Drawing.Bitmap CreateAppIcon()
    {
        var bmp = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.Transparent);
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(50, 205, 50));
            g.FillRectangle(brush, 4, 4, 24, 24);
            using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
            g.DrawRectangle(pen, 4, 4, 24, 24);
            using var font = new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold);
            g.DrawString("3DS", font, System.Drawing.Brushes.White, 4, 6);
        }
        return bmp;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _trayBitmap?.Dispose();
    }
}
