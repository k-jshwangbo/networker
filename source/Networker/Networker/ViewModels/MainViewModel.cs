using Networker.Services.BannerGrab;
using Networker.Services.Ping;
using Networker.Services.PortScan.Local;
using Networker.Services.PortScan.Remote;
using Networker.Services.RangeScan;
using Networker.Services.Traceroute;

namespace Networker.ViewModels;

public sealed class MainViewModel
{
    public PingViewModel Ping { get; }
    public PortScanViewModel PortScan { get; }
    public LocalPortViewModel LocalPort { get; }
    public BannerGrabViewModel BannerGrab { get; }
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

        IBannerGrabService bannerService = new BannerGrabService();
        BannerGrab = new BannerGrabViewModel(bannerService);

        IHostDiscoveryService discovery = new HostDiscoveryService(pingService);
        RangeScan = new RangeScanViewModel(discovery);

        ITracerouteService traceService = new TracerouteService();
        Traceroute = new TracerouteViewModel(traceService);
    }
}
