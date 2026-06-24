using Networker.Models;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetPing = System.Net.NetworkInformation.Ping;

namespace Networker.Services.Traceroute;
public sealed class TracerouteService : ITracerouteService
{
    private static readonly byte[] Payload = new byte[32];

    public async Task TraceAsync(
        string host, int maxHops, int probePerHop, TimeSpan timeout, bool resolveHostNames,
        IProgress<TracerouteHop> progress, CancellationToken cancellationToken)
    {
        var target = await ResolveAsync(host, cancellationToken);

        for (var hop = 1; hop <= maxHops; hop++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IPAddress? hopAddress = null;
            var rtts = new List<long?>(probePerHop);
            var reached = false;

            for (var probe = 0; probe < probePerHop; probe++)
            {
                var (address, rtt, isDestination) = await SendProbeAsync(target, hop, timeout, cancellationToken);
                hopAddress ??= address;
                rtts.Add(rtt);
                if (isDestination)
                    reached = true;
            }

            string? hostName = null;
            if (resolveHostNames && hopAddress is not null)
                hostName = await TryResolveHostNameAsync(hopAddress, cancellationToken);

            progress.Report(new TracerouteHop
            {
                Hop = hop,
                Address = hopAddress,
                HostName = hostName,
                RoundtripMs = rtts,
                DestinationReached = reached,
            });

            if (reached)
                break;
        }
    }

    private static async Task<(IPAddress? Address, long? Rtt, bool IsDestination)> SendProbeAsync(
        IPAddress target, int ttl, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var ping = new NetPing();
        var options = new PingOptions(ttl, dontFragment: true);

        try
        {
            var reply = await ping.SendPingAsync(target, timeout, Payload, options, cancellationToken);
            return reply.Status switch
            {
                IPStatus.Success => (reply.Address, reply.RoundtripTime, true),
                IPStatus.TtlExpired or IPStatus.TimeExceeded => (reply.Address, reply.RoundtripTime, false),
                _ => (null, null, false),   // timed out / unreachable -> shown as '*'
            };
        }
        catch (PingException)
        {
            return (null, null, false);
        }
    }


    private static async Task<IPAddress> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            var address = await Dns.GetHostAddressesAsync(host, cancellationToken);
            var ipv4 = address.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return ipv4 ?? address.FirstOrDefault()
                ?? throw new FormatException($"Could not resolve host '{host}'.");
        }
        catch (SocketException)
        {
            throw new FormatException($"Could not resolve host '{host}'.");
        }
    }


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
            // no PTR record / resolver error
        }
        return null;
    }
}
