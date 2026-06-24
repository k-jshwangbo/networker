using Networker.Services;

namespace Networker.ViewModels;

public sealed class MainViewModel
{
    public PingViewModel Ping { get; }
    public PortScanViewModel PortScan { get; }
    public LocalPortViewModel LocalPort { get; }
    public RangeScanViewModel RangeScan { get; }
    public TracerouteViewModel Traceroute { get; }

    public MainViewModel()
    {
        IPingService pingService = new PingService();
        Ping = new PingViewModel(pingService);

        IPortScanService portScanService = new PortScanService();
        PortScan = new PortScanViewModel(portScanService);

        ILocalPortService localPortService = new LocalPortService();
        LocalPort = new LocalPortViewModel(localPortService);

        IHostDiscoveryService discovery = new HostDiscoveryService(pingService);
        RangeScan = new RangeScanViewModel(discovery);

        ITracerouteService traceService = new TracerouteService();
        Traceroute = new TracerouteViewModel(traceService);
    }
}
