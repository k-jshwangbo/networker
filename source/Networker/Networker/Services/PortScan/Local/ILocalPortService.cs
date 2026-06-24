using Networker.Models;

namespace Networker.Services.PortScan.Local;
public interface ILocalPortService
{
    Task<IReadOnlyList<LocalConnectionResult>> GetTcpConnectionsAsync(
        CancellationToken cancellationToken);
}
