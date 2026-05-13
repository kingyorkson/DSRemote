using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DSRemote.Services;

public class DiscoveryService : IDisposable
{
    private CancellationTokenSource? _cts;
    private readonly int _port;

    public DiscoveryService(int port = 9876)
    {
        _port = port;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = BeaconLoop(_cts.Token);
    }

    private async Task BeaconLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var udp = new UdpClient { EnableBroadcast = true };
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var ni in interfaces)
                {
                    var ipProps = ni.GetIPProperties();
                    var addr = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                    if (addr == null) continue;

                    var ip = addr.Address.ToString();
                    var isUsb = IsUsbInterface(ni, addr.Address);
                    var tag = isUsb ? "USB" : "WIFI";
                    var beacon = Encoding.UTF8.GetBytes($"DSREMOTE:{ip}:{_port}:{tag}");

                    await udp.SendAsync(beacon, beacon.Length, new IPEndPoint(IPAddress.Broadcast, 9877));
                }

                await Task.Delay(2000, ct);
            }
            catch { break; }
        }
    }

    private static bool IsUsbInterface(NetworkInterface ni, IPAddress address)
    {
        var desc = ni.Description.ToLowerInvariant();
        if (desc.Contains("usb") || desc.Contains("mobile") || desc.Contains("virtual")) return true;

        var bytes = address.GetAddressBytes();
        // iTunes USB link-local
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        // Common USB tethering
        if (bytes[0] == 192 && bytes[1] == 168 && bytes[2] == 42) return true;

        return false;
    }

    public void Dispose()
    {
        _cts?.Cancel();
    }
}
