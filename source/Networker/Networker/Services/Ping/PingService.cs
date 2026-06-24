using Networker.Models;
using System.Net.NetworkInformation;
using NetPing = System.Net.NetworkInformation.Ping;

namespace Networker.Services.Ping;

public sealed class PingService : IPingService
{
    public async Task<PingResult> PingOnceAsync(
        string host, int sequence, TimeSpan timeout, int bufferSize, CancellationToken cancellationToken)
    {
        using var ping = new NetPing();
        var buffer = new byte[bufferSize];
        var options = new PingOptions(ttl:128, dontFragment: true);

        try
        {
            var reply = await ping.SendPingAsync(host, timeout, buffer, options, cancellationToken);

            return new PingResult
            {
                Sequence = sequence,
                Status = reply.Status,
                Address = reply.Address,
                RoundtripMs = reply.RoundtripTime,
                Ttl = reply.Options?.Ttl ?? 0
            };
        }
        catch (PingException)
        {
            return new PingResult { Sequence = sequence, Status = IPStatus.Unknown };
        }
    }


    public async Task PingContinuousAsync(
        string host, TimeSpan interval, TimeSpan timeout, int bufferSize, IProgress<PingResult> progress, CancellationToken cancellationToken)
    {
        var sequence = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                sequence++;
                var result = await PingOnceAsync(host, sequence, timeout, bufferSize, cancellationToken);
                progress.Report(result);
                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // stop requested - exit cleanly.
        }
    }
}
