using System.Net;

namespace DSRemote.Models;

public class ConnectionInfo
{
    public string DeviceName { get; set; } = "Unknown Device";
    public IPAddress? RemoteAddress { get; set; }
    public int Port { get; set; } = 9876;
    public bool IsConnected { get; set; }
    public DateTime ConnectedAt { get; set; }
    public ConnectionType Type { get; set; } = ConnectionType.WiFi;
}

public enum ConnectionType
{
    USB,
    WiFi
}
