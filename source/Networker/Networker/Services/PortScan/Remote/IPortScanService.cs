using Networker.Models;

namespace Networker.Services.PortScan.Remote;
public interface IPortScanService
{
    Task<PortScanResult> ScanPortAsync(
        string host, int port, TimeSpan timeout, CancellationToken cancellationToken);
}