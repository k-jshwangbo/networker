using Networker.Models;

namespace Networker.Services.RoutingTable;

public interface IRoutingTableService
{
    Task<IReadOnlyList<RouteEntry>> GetRoutesAsync(CancellationToken cancellationToken);
}