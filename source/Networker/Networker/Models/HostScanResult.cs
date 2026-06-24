using System.Net;

namespace Networker.Models;
public sealed class HostScanResult
{
    public required IPAddress Address { get; init; }
    public bool IsAlive { get; init; }
    public long? PingMs { get; init; }
    public string? MacAddress { get; init; }
    public string? HostName { get; init; }
    public string DiscoveryMethod { get; init; } = "";
    public string AddressText => Address.ToString();
    public string PingText => PingMs?.ToString() ?? "";
    public string MacText => MacAddress ?? "";
    public string HostNameText => HostName ?? "";
}
