using Networker.Models;

namespace Networker.Services;
public interface ILocalPortService
{
    Task<IReadOnlyList<LocalConnectionResult>> GetTcpConnectionsAsync(
        CancellationToken cancellationToken);
}
