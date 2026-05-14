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
    private readonly InputMapper _input = new();

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

    private bool _isKidModeActive;
    public bool IsKidModeActive
    {
        get => _isKidModeActive;
        set
        {
            _isKidModeActive = value;
            OnPropertyChanged();
        }
    }

    private bool _usePcAsTopScreen;
    public bool UsePcAsTopScreen
    {
        get => _usePcAsTopScreen;
        set
        {
            _usePcAsTopScreen = value;
            _config.Current.UsePcAsTopScreen = value;
            _config.Save();
            OnPropertyChanged();
        }
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
        _input.LoadConfig(_config.Current);
        _input.SetCaptureService(_capture);

        AccentColor = (Color)ColorConverter.ConvertFromString(_config.Current.AccentColor);
        _network.EmulatorName = "3DS";
        _usePcAsTopScreen = _config.Current.UsePcAsTopScreen;

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
        _input.LoadConfig(config);
        IsSetupComplete = true;
        AccentColor = (Color)ColorConverter.ConvertFromString(config.AccentColor);
        _network.EmulatorName = "3DS";
        _usePcAsTopScreen = config.UsePcAsTopScreen;
        _ = Task.Run(RefreshGames);
        _ = _network.StartServer();
        _discovery.Start();
    }

    public async void RefreshGames()
    {
        var found = await Task.Run(() => _library.ScanFolders(_config.Current.GameFolders));
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            Games.Clear();
            foreach (var game in found)
                Games.Add(game);
        });
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
        var (top, bottom) = _capture.CaptureScreensAsBase64(emulatorHwnd);
        if (!string.IsNullOrEmpty(top))
            await _network.SendMessage(top);
        if (!string.IsNullOrEmpty(bottom))
            await _network.SendMessage(bottom);
    }

    public (string Top, string Bottom) CaptureScreens()
    {
        var hWnd = GetEmulatorWindowHandle();
        if (hWnd == null || hWnd.Value == IntPtr.Zero) return ("", "");
        return _capture.CaptureScreensAsBase64(hWnd.Value);
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

            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();
                var hWnd = _emulator.GetEmulatorWindowHandle();
                if (hWnd == null || hWnd.Value == IntPtr.Zero) return;

                var args = new List<float>();
                if (root.TryGetProperty("args", out var argsProp))
                {
                    foreach (var item in argsProp.EnumerateArray())
                        args.Add(item.GetSingle());
                }

                switch (type)
                {
                    case "ButtonDown":
                        if (args.Count > 0) _input.SendButtonDown(hWnd.Value, (int)args[0]);
                        break;
                    case "ButtonUp":
                        if (args.Count > 0) _input.SendButtonUp(hWnd.Value, (int)args[0]);
                        break;
                    case "DPadPress":
                        if (args.Count > 0) _input.SendDPad(hWnd.Value, (int)args[0]);
                        break;
                    case "JoystickMove":
                        if (args.Count >= 2) _input.SendJoystickMove(hWnd.Value, args[0], args[1]);
                        break;
                    case "TouchDown":
                        if (args.Count >= 2) _input.SendTouchDown(hWnd.Value, args[0], args[1]);
                        break;
                    case "TouchMove":
                        if (args.Count >= 2) _input.SendTouchMove(hWnd.Value, args[0], args[1]);
                        break;
                    case "TouchUp":
                        _input.SendTouchUp(hWnd.Value);
                        break;
                }
                return;
            }

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
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = "Launching emulator...";
                            });
                            _ = _emulator.LaunchGame(_config.Current, game);
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                StatusText = $"Streaming to {DeviceName}";
                            });
                        }
                    }
                    break;
                case "usePcAsTopScreen":
                    if (root.TryGetProperty("value", out var val))
                        UsePcAsTopScreen = val.GetBoolean();
                    break;
                case "stop":
                    App.Current.Dispatcher.Invoke(StopEmulation);
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
