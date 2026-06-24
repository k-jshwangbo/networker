using Networker.Models;

namespace Networker.Services;
public interface IPingService
{
    Task<PingResult> PingOnceAsync(
        string host, int sequence, TimeSpan timeout, int bufferSize, CancellationToken cancellationToken);

    Task PingContinuousAsync(
        string host, TimeSpan interval, TimeSpan timeout, int bufferSize,
        IProgress<PingResult> progress, CancellationToken cancellationToken);
}