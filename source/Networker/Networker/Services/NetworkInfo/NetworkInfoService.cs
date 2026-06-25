using Networker.Models;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Networker.Services.NetworkInfo;

public sealed class NetworkInfoService : INetworkInfoService
{
    public Task<IReadOnlyList<NetworkAdapterInfo>> GetAdaptersAsync(CancellationToken ct)
        => Task.Run(() => Collect(ct), ct);

    private static IReadOnlyList<NetworkAdapterInfo> Collect(CancellationToken ct)
    {
        var adapters = new List<NetworkAdapterInfo>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            ct.ThrowIfCancellationRequested();
            adapters.Add(Describe(ni));
        }

        // Active adapters first, then by name.
        return adapters
            .OrderByDescending(a => a.IsUp)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static NetworkAdapterInfo Describe(NetworkInterface ni)
    {
        var ipv4 = new List<string>();
        var ipv6 = new List<string>();
        var gateways = new List<string>();
        var dns = new List<string>();
        bool dhcp = false;

        try
        {
            var props = ni.GetIPProperties();

            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    ipv4.Add(ua.Address.ToString());
                else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    ipv6.Add(ua.Address.ToString());
            }

            foreach (var gw in props.GatewayAddresses)
                gateways.Add(gw.Address.ToString());

            foreach (var d in props.DnsAddresses)
                dns.Add(d.ToString());

            // Not all interfaces support an IPv4 configuration; guard the call.
            try { dhcp = props.GetIPv4Properties()?.IsDhcpEnabled ?? false; }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
        }
        catch (NetworkInformationException)
        {
            // Some virtual/transient interfaces refuse GetIPProperties(); skip details.
        }

        long sent = 0, received = 0;
        try
        {
            var stats = ni.GetIPStatistics();
            sent = stats.BytesSent;
            received = stats.BytesReceived;
        }
        catch (NetworkInformationException) { }
        catch (NotSupportedException) { }

        return new NetworkAdapterInfo
        {
            Name = ni.Name,
            Description = ni.Description,
            InterfaceType = Humanize(ni.NetworkInterfaceType),
            Status = ni.OperationalStatus.ToString(),
            MacAddress = FormatMac(ni),
            SpeedBps = SafeSpeed(ni),
            IsDhcpEnabled = dhcp,
            IPv4Addresses = ipv4,
            IPv6Addresses = ipv6,
            Gateways = gateways,
            DnsServers = dns,
            BytesSent = sent,
            BytesReceived = received
        };
    }

    private static long SafeSpeed(NetworkInterface ni)
    {
        try { return ni.Speed; }
        catch { return -1; }
    }

    private static string FormatMac(NetworkInterface ni)
    {
        var bytes = ni.GetPhysicalAddress().GetAddressBytes();
        return bytes.Length == 0
            ? ""
            : string.Join(":", bytes.Select(b => b.ToString("X2")));
    }

    private static string Humanize(NetworkInterfaceType type) => type switch
    {
        NetworkInterfaceType.Ethernet => "Ethernet",
        NetworkInterfaceType.Wireless80211 => "Wi-Fi",
        NetworkInterfaceType.Loopback => "Loopback",
        NetworkInterfaceType.Tunnel => "Tunnel",
        NetworkInterfaceType.Ppp => "PPP",
        _ => type.ToString()
    };
}
