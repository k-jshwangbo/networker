using Networker.Models;

namespace Networker.Services.NetworkInfo;
public interface INetworkInfoService
{
    Task<IReadOnlyList<NetworkAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken);
}
