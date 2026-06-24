using System.Net;

namespace Networker.Models;
public sealed class TracerouteHop
{
    public int Hop { get; init; }
    public IPAddress? Address { get; init; }
    public string? HostName { get; init; }
    public IReadOnlyList<long?> RoundtripMs { get; init; } = new List<long?>();
    public bool DestinationReached { get; init; }
    public bool AnyResponse => Address is not null;
    public string AddressText => Address?.ToString() ?? "*";
    public string HostNameText => HostName ?? "";
    public string RttText =>
        AnyResponse
        ? string.Join(" ", RoundtripMs.Select(r => r.HasValue ? $"{r} ms" : "*"))
        : "*";

    public string StatusText =>
        DestinationReached ? "Destination reached"
                           : AnyResponse ? "OK"
                                         : "No response";
}
