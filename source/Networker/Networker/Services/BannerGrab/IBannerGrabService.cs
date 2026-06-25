using Networker.Models;

namespace Networker.Services.BannerGrab;

public sealed class BannerGrabOptions
{
    public bool UseTls { get; init; } = true;
    public bool SendHttpProbe { get; init; } = true;
    public string? CustomProbe { get; init; }
    public int MaxBytes { get; init; } = 4096;
    public int MaxChars { get; init; } = 500;
}

public interface IBannerGrabService
{
    Task<BannerResult> GrabAsync(
        string host, int port, TimeSpan timeout, BannerGrabOptions options, CancellationToken cancellationToken);
}
