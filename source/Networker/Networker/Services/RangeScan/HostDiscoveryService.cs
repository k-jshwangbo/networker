using Networker.Models;
using Networker.Services.Ping;
using System.Net;
using System.Net.NetworkInformation;

namespace Networker.Services.RangeScan;

public sealed class HostDiscoveryService : IHostDiscoveryService
{
    private readonly IPingService _pingService;

    public HostDiscoveryService(IPingService pingService) => _pingService = pingService;


    public async Task<HostScanResult> ProbeAsync(IPAddress address, TimeSpan pingTimeout, bool resolveHostName, CancellationToken cancellationToken)
    {
        // 1) ICMP ping
        var ping = await _pingService.PingOnceAsync(
            address.ToString(), sequence: 0, pingTimeout, bufferSize: 32, cancellationToken);
        var pingOk = ping.Status == IPStatus.Success;

        cancellationToken.ThrowIfCancellationRequested();

        // 2) ARP (layer 2, local subnet). SendARP blocks, so run it off the current continuation thread.
        var mac = await Task.Run(() => ArpResolver.Resolve(address), cancellationToken);
        var arpOk = mac is not null;

        var alive = pingOk || arpOk;

        string? hostName = null;
        if (alive && resolveHostName)
            hostName = await TryResolveHostNameAsync(address, cancellationToken);

        return new HostScanResult
        {
            Address = address,
            IsAlive = alive,
            PingMs = pingOk ? ping.RoundtripMs : null,
            MacAddress = mac,
            HostName = hostName,
            DiscoveryMethod = alive ? Method(pingOk, arpOk) : ""
        };
    }

    private static string Method(bool ping, bool arp)
        => ping && arp ? "Ping+ARP" : ping ? "Ping" : "ARP";

    private static async Task<string?> TryResolveHostNameAsync(IPAddress ip, CancellationToken ct)
    {
        try
        {
            var lookup = Dns.GetHostEntryAsync(ip);
            var finished = await Task.WhenAny(lookup, Task.Delay(TimeSpan.FromSeconds(1), ct));
            if (finished == lookup && lookup.Status == TaskStatus.RanToCompletion)
                return lookup.Result.HostName;
        }
        catch
        {
            // NO PTR record / resolver error - leave not name blank.
        }
        return null;
    }
}
