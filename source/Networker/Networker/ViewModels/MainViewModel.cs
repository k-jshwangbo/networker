using Networker.Services;

namespace Networker.ViewModels;

public sealed class MainViewModel
{
    public PingViewModel Ping { get; }

    public MainViewModel()
    {
        IPingService pingService = new PingService();
        Ping = new PingViewModel(pingService);
    }
}
