using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DSRemote.Services;

public class DiscoveryService : IDisposable
{
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private readonly int _port;

    public DiscoveryService(int port = 9876)
    {
        _port = port;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _udp = new UdpClient { EnableBroadcast = true };
        _ = BeaconLoop(_cts.Token);
    }

    private async Task BeaconLoop(CancellationToken ct)
    {
        var localIP = GetLocalIP();
        var beacon = Encoding.UTF8.GetBytes($"DSREMOTE:{localIP}:{_port}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _udp!.SendAsync(beacon, beacon.Length, new IPEndPoint(IPAddress.Broadcast, 9877));
                await Task.Delay(2000, ct);
            }
            catch { break; }
        }
    }

    private static string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return ip?.ToString() ?? "Unknown";
        }
        catch { return "Unknown"; }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _udp?.Close();
    }
}
