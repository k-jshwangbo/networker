using Networker.Models;

namespace Networker.Services;
public interface IPortScanService
{
    Task<PortScanResult> ScanPortAsync(
        string host, int port, TimeSpan timeout, CancellationToken cancellationToken);
}