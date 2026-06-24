using Networker.Models;
using System.Diagnostics;
using System.Net.Sockets;

namespace Networker.Services;
public sealed class PortScanService : IPortScanService
{
    public async Task<PortScanResult> ScanPortAsync(
        string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var client = new TcpClient();
        var sw = Stopwatch.StartNew();

        try
        {
            await client.ConnectAsync(host, port, timeoutCts.Token);
            sw.Stop();
            return new PortScanResult { Port = port, State = PortState.Open, ResponseMs = sw.ElapsedMilliseconds };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the user stopped the whole scan -> propagate so the batch unwinds.
            throw;
        }
        catch (OperationCanceledException)
        {
            // our own timeout elapsed: no response in time -> filtered.
            sw.Stop();
            return new PortScanResult { Port = port, State = PortState.Filtered, ResponseMs = sw.ElapsedMilliseconds };
        }
        catch (SocketException)
        {
            // connection actively refused / host unreachable -> closed.
            sw.Stop();
            return new PortScanResult { Port = port, State = PortState.Closed, ResponseMs = sw.ElapsedMilliseconds };
        }
    }
}