using Networker.Models;
using System.Net;

namespace Networker.Services.RangeScan;
public interface IHostDiscoveryService
{
    Task<HostScanResult> ProbeAsync(
        IPAddress address, TimeSpan pingTimeout, bool resolveHostName, CancellationToken cancellationToken);
}
