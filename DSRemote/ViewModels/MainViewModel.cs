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
        set { _accentColor = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        _config = new ConfigService();
        _emulator = new EmulatorService();
        _library = new GameLibraryService();
        _network = new NetworkService();
        _capture = new ScreenCaptureService();

        AccentColor = (Color)ColorConverter.ConvertFromString(_config.Current.AccentColor);

        IsSetupComplete = _config.Current.SetupComplete;

        if (IsSetupComplete)
        {
            RefreshGames();
            _ = _network.StartServer();
        }

        _network.DeviceConnected += OnDeviceConnected;
        _network.DeviceDisconnected += OnDeviceDisconnected;
        _network.ScreenshotReceived += OnScreenshotReceived;
        _emulator.StateChanged += OnEmulatorStateChanged;
    }

    public void CompleteSetup(AppConfig config)
    {
        _config.Save(config);
        IsSetupComplete = true;
        AccentColor = (Color)ColorConverter.ConvertFromString(config.AccentColor);
        RefreshGames();
        _ = _network.StartServer();
    }

    public void RefreshGames()
    {
        Games.Clear();
        var found = _library.ScanFolders(_config.Current.GameFolders);
        foreach (var game in found)
            Games.Add(game);
    }

    public async Task LaunchGame(GameRom game)
    {
        _network.Disconnect();
        await _emulator.LaunchGame(_config.Current, game);
    }

    public void StopEmulation()
    {
        _emulator.StopEmulation();
    }

    public void DisconnectDevice()
    {
        _network.Disconnect();
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

    private void OnDeviceConnected(ConnectionInfo info)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = true;
            DeviceName = info.DeviceName;
            StatusText = $"Connected to {info.DeviceName}";
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
