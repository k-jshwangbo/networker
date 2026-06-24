using Networker.Models;

namespace Networker.Services;
public interface ITracerouteService
{
    Task TraceAsync(
        string host, int maxHops, int probePerHop, TimeSpan timeout, bool resolveHostNames,
        IProgress<TracerouteHop> progress, CancellationToken cancellationToken);
}
