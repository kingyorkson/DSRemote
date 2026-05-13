using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DSRemote.Models;
using DSRemote.Services;

namespace DSRemote.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _config;
    private readonly EmulatorService _emulator;
    private readonly GameLibraryService _library;
    private readonly NetworkService _network;
    private readonly ScreenCaptureService _capture;
    private readonly DiscoveryService _discovery;

    public AppConfig Config => _config.Current;

    public ObservableCollection<GameRom> Games { get; } = new();

    private bool _isSetupComplete;
    public bool IsSetupComplete
    {
        get => _isSetupComplete;
        set { _isSetupComplete = value; OnPropertyChanged(); }
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    private string _statusText = "Not connected";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private string _deviceName = string.Empty;
    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

    private BitmapImage? _phoneScreenImage;
    public BitmapImage? PhoneScreenImage
    {
        get => _phoneScreenImage;
        set { _phoneScreenImage = value; OnPropertyChanged(); }
    }

    public string LocalIPAddress => GetLocalIP();
    public int Port => 9876;

    private static string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip?.ToString() ?? "Unknown";
        }
        catch { return "Unknown"; }
    }

    private bool _isSlideMenuOpen;
    public bool IsSlideMenuOpen
    {
        get => _isSlideMenuOpen;
        set { _isSlideMenuOpen = value; OnPropertyChanged(); }
    }

    private Color _accentColor;
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(BackgroundBrush));
        }
    }

    public Color BackgroundColor
    {
        get
        {
            var r = (byte)(_accentColor.R * 0.15 + 0x1a * 0.85);
            var g = (byte)(_accentColor.G * 0.15 + 0x1a * 0.85);
            var b = (byte)(_accentColor.B * 0.15 + 0x2e * 0.85);
            return Color.FromRgb(r, g, b);
        }
    }

    public SolidColorBrush BackgroundBrush => new(BackgroundColor);

    public MainViewModel()
    {
        _config = new ConfigService();
        _emulator = new EmulatorService();
        _library = new GameLibraryService();
        _network = new NetworkService();
        _capture = new ScreenCaptureService();
        _discovery = new DiscoveryService();

        AccentColor = (Color)ColorConverter.ConvertFromString(_config.Current.AccentColor);

        IsSetupComplete = _config.Current.SetupComplete;

        if (IsSetupComplete)
        {
            RefreshGames();
            _ = _network.StartServer();
            _discovery.Start();
        }

        _network.DeviceConnected += OnDeviceConnected;
        _network.DeviceDisconnected += OnDeviceDisconnected;
        _network.ScreenshotReceived += OnScreenshotReceived;
        _network.MessageReceived += OnMessageReceived;
        _emulator.StateChanged += OnEmulatorStateChanged;
    }

    public void CompleteSetup(AppConfig config)
    {
        _config.Save(config);
        IsSetupComplete = true;
        AccentColor = (Color)ColorConverter.ConvertFromString(config.AccentColor);
        RefreshGames();
        _ = _network.StartServer();
        _discovery.Start();
    }

    public async void RefreshGames()
    {
        Games.Clear();
        var found = _library.ScanFolders(_config.Current.GameFolders);
        foreach (var game in found)
            Games.Add(game);
        if (_network.IsConnected)
            await _network.SendGameList(Games.ToList());
    }

    public async Task LaunchGame(GameRom game)
    {
        await _network.Disconnect();
        await _emulator.LaunchGame(_config.Current, game);
    }

    public void StopEmulation()
    {
        _emulator.StopEmulation();
    }

    public async void DisconnectDevice()
    {
        await _network.Disconnect();
        IsConnected = false;
        DeviceName = string.Empty;
        StatusText = "Not connected";
        PhoneScreenImage = null;
    }

    public IntPtr? GetEmulatorWindowHandle() => _emulator.GetEmulatorWindowHandle();

    public void QuitToGameSelect()
    {
        StopEmulation();
        _ = _network.StartServer();
    }

    public async Task SendScreenshot(IntPtr emulatorHwnd)
    {
        if (!_network.IsConnected) return;
        var b64 = _capture.CaptureWindowAsBase64(emulatorHwnd);
        if (!string.IsNullOrEmpty(b64))
            await _network.SendMessage(b64);
    }

    private async void OnDeviceConnected(ConnectionInfo info)
    {
        await App.Current.Dispatcher.Invoke(async () =>
        {
            IsConnected = true;
            DeviceName = info.DeviceName;
            StatusText = $"Connected to {info.DeviceName}";
            await _network.SendGameList(Games.ToList());
        });
    }

    private void OnDeviceDisconnected()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = false;
            DeviceName = string.Empty;
            StatusText = "Device disconnected";
            PhoneScreenImage = null;
        });
    }

    private void OnMessageReceived(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var action = root.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : null;
            switch (action)
            {
                case "deviceInfo":
                    if (root.TryGetProperty("name", out var name))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            DeviceName = name.GetString() ?? "Unknown";
                            StatusText = $"Connected to {DeviceName}";
                        });
                    }
                    break;
                case "launch":
                    if (root.TryGetProperty("path", out var pathProp))
                    {
                        var path = pathProp.GetString();
                        var game = Games.FirstOrDefault(g => g.FullPath == path);
                        if (game != null)
                            _ = LaunchGame(game);
                    }
                    break;
            }
        }
        catch { }
    }

    private void OnScreenshotReceived(byte[] jpegData)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            using var ms = new MemoryStream(jpegData);
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            PhoneScreenImage = img;
        });
    }

    private void OnEmulatorStateChanged(EmulatorState state)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            StatusText = state switch
            {
                EmulatorState.Running => "Emulator running",
                EmulatorState.Idle => IsConnected ? $"Connected to {DeviceName}" : "Not connected",
                EmulatorState.Error => "Error launching emulator",
                _ => StatusText
            };
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
