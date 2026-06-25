namespace Networker.Models;

public enum BannerStatus
{
    Banner, 
    Empty,
    Closed,
    Filtered,
    Error
}

public sealed class BannerResult
{
    public int Port { get; init; }
    public bool IsTls { get; init; }
    public string Banner { get; init; } = "";
    public int BytesRead { get; init; }
    public long ElapsedMs { get; init; }
    public BannerStatus Status { get; init; }

    public string Service => WellKnownPorts.GetServiceName(Port);
    public string IsTlsText => IsTls ? "TLS" : "";
    public bool HasBanner => Status == BannerStatus.Banner && Banner.Length > 0;
    public string StatusText => Status switch
    {
        BannerStatus.Banner => "OK",
        BannerStatus.Empty => "No banner",
        BannerStatus.Closed => "Closed",
        BannerStatus.Filtered => "Timeout",
        BannerStatus.Error => "Error",
        _ => Status.ToString()
    };
}
