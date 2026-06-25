using Networker.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Networker.Services.BannerGrab;

public sealed class BannerGrabService : IBannerGrabService
{
    // Ports that should be wrapped in TLS before reading.
    private static readonly HashSet<int> TlsPorts =
        new() { 443, 465, 563, 636, 853, 989, 990, 993, 995, 8443 };

    // Ports where the client must send a request before the server responds.
    private static readonly HashSet<int> HttpPorts =
        new() { 80, 443, 591, 8000, 8008, 8080, 8081, 8443, 8888 };

    private static readonly TimeSpan ReadGrace = TimeSpan.FromMilliseconds(250);


    public async Task<BannerResult> GrabAsync(
        string host, int port, TimeSpan timeout, BannerGrabOptions options, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        bool useTls = options.UseTls && TlsPorts.Contains(port);

        using var client = new TcpClient();

        // --- connect ---
        try
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(timeout);
            await client.ConnectAsync(host, port, connectCts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result(port, BannerStatus.Filtered, useTls, "", 0, sw);
        }
        catch (SocketException)
        {
            return Result(port, BannerStatus.Closed, useTls, "", 0, sw);
        }

        SslStream? ssl = null;
        try
        {
            Stream stream = client.GetStream();

            if (useTls)
            {
                // accept any certificate: we're identifying services, not trusting them.
                ssl = new SslStream(stream, leaveInnerStreamOpen: false, (_, _, _, _) => true);
                using var tlsCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                tlsCts.CancelAfter(timeout);
                await ssl.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions { TargetHost = host }, tlsCts.Token);
                stream = ssl;
            }

            byte[]? probe = BuildProbe(host, port, options, useTls);
            if (probe is not null)
            {
                using var writeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                writeCts.CancelAfter(timeout);
                await stream.WriteAsync(probe, writeCts.Token);
                await stream.FlushAsync(writeCts.Token);
            }

            var buffer = new byte[options.MaxBytes];
            int total = await ReadBannerAsync(stream, buffer, timeout, ct);
            sw.Stop();

            if (total == 0)
                return Result(port, BannerStatus.Empty, useTls, "", 0, sw);

            string text = Sanitize(buffer, total, options.MaxBytes);
            return Result(port, BannerStatus.Banner, useTls, text, total, sw);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Result(port, BannerStatus.Error, useTls, ex.GetType().Name, 0, sw);
        }
        finally
        {
            ssl?.Dispose();
        }
    }

    private static async Task<int> ReadBannerAsync(
        Stream stream, byte[] buffer, TimeSpan firstTimeout, CancellationToken ct)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(total == 0 ? firstTimeout : ReadGrace);

            int n;
            try
            {
                n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), readCts.Token);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (n <= 0)
                break;

            total += n;
        }

        return total;
    }


    private static byte[]? BuildProbe(string host, int port, BannerGrabOptions options, bool useTls)
    {
        if (!string.IsNullOrEmpty(options.CustomProbe))
        {
            var raw = options.CustomProbe
                .Replace("\\r", "\r", StringComparison.Ordinal)
                .Replace("\\n", "\n", StringComparison.Ordinal);

            return Encoding.ASCII.GetBytes(raw);
        }

        bool isHttp = HttpPorts.Contains(port);
        if (options.SendHttpProbe && isHttp)
        {
            string request =
                "HEAD / HTTP/1.0\r\n" +
                $"Host: {host}\r\n" +
                "User-Agent: Networker\r\n" +
                "Connection: close\r\n\r\n";
            return Encoding.ASCII.GetBytes(request);
        }

        return null;
    }


    private static string Sanitize(byte[] buffer, int length, int maxChars)
    {
        var sb = new StringBuilder(Math.Min(length, maxChars));

        for (int i = 0; i < length && sb.Length < maxChars; i++)
        {
            char c = (char)buffer[i];

            if (c is '\r' or '\n' or '\t')
            {
                if (sb.Length > 0 && sb[^1] != ' ')
                    sb.Append(' ');
            }
            else if (c >= 0x20 && c < 0x7F)
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(',');
            }
        }

        return sb.ToString().Trim();
    }


    private static BannerResult Result(
        int port, BannerStatus status, bool tls, string banner, int bytes, Stopwatch sw)
        => new()
        {
            Port = port,
            Status = status,
            IsTls = tls,
            Banner = banner,
            BytesRead = bytes,
            ElapsedMs = sw.ElapsedMilliseconds,
        };
}