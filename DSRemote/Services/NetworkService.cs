using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DSRemote.Models;

namespace DSRemote.Services;

public class NetworkService : IDisposable
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly int _port;

    public event Action<ConnectionInfo>? DeviceConnected;
    public event Action? DeviceDisconnected;
    public event Action<string>? MessageReceived;
    public event Action<byte[]>? ScreenshotReceived;

    public bool IsConnected => _client?.Connected ?? false;
    public ConnectionInfo? CurrentConnection { get; private set; }
    public string EmulatorName { get; set; } = "Citra";

    public NetworkService(int port = 9876)
    {
        _port = port;
    }

    public Task StartServer()
    {
        _listener?.Stop();
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        _ = Task.Run(() => AcceptLoop(_cts.Token));
        return Task.CompletedTask;
    }

    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                _client = client;
                _stream = client.GetStream();

                var remoteEp = (IPEndPoint)client.Client.RemoteEndPoint!;
                var connType = IsUsbAddress(remoteEp.Address) ? ConnectionType.USB : ConnectionType.WiFi;

                var info = new ConnectionInfo
                {
                    DeviceName = $"Device_{remoteEp.Address}",
                    RemoteAddress = remoteEp.Address,
                    Port = _port,
                    IsConnected = true,
                    ConnectedAt = DateTime.Now,
                    Type = connType
                };
                CurrentConnection = info;
                DeviceConnected?.Invoke(info);

                // Send welcome with connection type and emulator name
                var welcome = JsonSerializer.Serialize(new
                {
                    action = "welcome",
                    connection = connType == ConnectionType.USB ? "usb" : "wifi",
                    emulator = EmulatorName
                });
                _ = SendMessage(welcome);

                await ReadLoop(ct);
            }
            catch (OperationCanceledException) { break; }
            catch { continue; }
        }
    }

    private static bool IsUsbAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        if (bytes[0] == 192 && bytes[1] == 168 && bytes[2] == 42) return true;
        return false;
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && _stream != null)
            {
                var len = await _stream.ReadAsync(buffer, ct);
                if (len == 0) break;

                var data = buffer[..len];
                ProcessData(data);
            }
        }
        catch { }
        finally
        {
            _ = Disconnect();
        }
    }

    private void ProcessData(byte[] data)
    {
        var raw = Encoding.UTF8.GetString(data);
        if (raw.StartsWith("{"))
        {
            MessageReceived?.Invoke(raw);
            return;
        }

        var imgMarker = "IMAGE:";
        if (raw.StartsWith(imgMarker))
        {
            var base64 = raw[imgMarker.Length..].TrimEnd('\0');
            try
            {
                var imgBytes = Convert.FromBase64String(base64);
                ScreenshotReceived?.Invoke(imgBytes);
            }
            catch { }
        }
    }

    public async Task SendMessage(string message)
    {
        if (_stream == null) return;
        try
        {
            var data = Encoding.UTF8.GetBytes(message + "\n");
            await _stream.WriteAsync(data);
        }
        catch { }
    }

    public async Task SendInput(InputType type, params float[] args)
    {
        var msg = JsonSerializer.Serialize(new
        {
            type = type.ToString(),
            args
        });
        await SendMessage(msg);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task SendGameList(IEnumerable<GameRom> games)
    {
        try
        {
            var msg = JsonSerializer.Serialize(new { games }, _jsonOptions);
            await SendMessage(msg);
        }
        catch { }
    }

    public async Task Disconnect()
    {
        try
        {
            await SendMessage("{\"action\":\"disconnected\"}");
        }
        catch { }
        _stream?.Close();
        _client?.Close();
        CurrentConnection = null;
        DeviceDisconnected?.Invoke();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _stream?.Dispose();
        _client?.Dispose();
        _listener?.Stop();
    }
}

public enum InputType
{
    ButtonDown,
    ButtonUp,
    TouchDown,
    TouchMove,
    TouchUp,
    JoystickMove,
    DPadPress
}
