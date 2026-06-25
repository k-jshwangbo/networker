using System.Windows.Media.Imaging;

namespace Networker.Models;
public sealed class NetworkAdapterInfo
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string InterfaceType { get; init; } = "";
    public string Status { get; init; } = "";
    public string MacAddress { get; init; } = "";

    public long SpeedBps { get; init; }
    public bool IsDhcpEnabled { get; init; }

    public IReadOnlyList<string> IPv4Addresses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> IPv6Addresses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Gateways { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DnsServers { get; init; } = Array.Empty<string>();

    public long BytesSent { get; init; }
    public long BytesReceived { get; init; }

    public bool IsUp => Status.Equals("Up", StringComparison.OrdinalIgnoreCase);

    public string IPv4Text => IPv4Addresses.Count > 0 ? string.Join(", ", IPv4Addresses) : "-";
    public string IPv6Text => IPv6Addresses.Count > 0 ? string.Join(", ", IPv6Addresses) : "-";
    public string GatewayText => Gateways.Count > 0 ? string.Join(", ", Gateways) : "-";
    public string DnsText => DnsServers.Count > 0 ? string.Join(", ", DnsServers) : "_";
    public string MacText => string.IsNullOrEmpty(MacAddress) ? "-" : MacAddress;
    public string DhcpText => IsDhcpEnabled ? "Yes" : "No";

    public string SpeedText => SpeedBps <= 0
        ? "-"
        : SpeedBps >= 1_000_000_000
            ? $"{SpeedBps / 1_000_000_000d:0.#} Gbps"
            : SpeedBps >= 1_000_000
                ? $"{SpeedBps / 1_000_000d:0.#} Mbps"
                : $"{SpeedBps / 1_000d:0.#} Kbps";

    public string TrafficText => $"\u2191 {FormatBytes(BytesSent)}  \u2193 {FormatBytes(BytesReceived)}";

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value > 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }
}