using System.Net;
using System.Net.Sockets;

namespace Networker.Core;

/// <summary>
/// Parses an IPv4 target specification into a list of addresses. Supports:
///     CIDR            "192.168.1.0/24"
///     full range      "192.168.1.1-192.168.1.254"
///     shorthand       "192.168.1.1-254"   (replaces the last octet)
///     single          "192.168.1.5"
/// throws <see cref="=FormatException"/> on invalid input or oversied ranges.
/// </summary>
public static class IpRange
{
    private const int MaxHosts = 65536;

    public static IReadOnlyList<IPAddress> Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            throw new FormatException("Target is empty.");

        spec = spec.Trim();

        if (spec.Contains('/'))
            return ParseCidr(spec);
        if (spec.Contains('-'))
            return ParseRange(spec);

        return new List<IPAddress> { ParseV4(spec) };
    }


    private static List<IPAddress> ParseCidr(string spec)
    {
        var parts = spec.Split('/', 2);
        var baseIp = ParseV4(parts[0].Trim());

        if (!int.TryParse(parts[1].Trim(), out var prefix) || prefix is <0 or > 32)
                throw new FormatException($"Invalid CIDR prefix: '{parts[1]}'. Must be 0-32.");

        var baseVal = ToUInt(baseIp);
        var mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);
        var network = baseVal & mask;
        var broadcast = network | ~mask;

        uint first, last;
        if (prefix >= 31)
        {
            first = network;        // /31, /32: include all
            last = broadcast;
        }
        else
        {
            first = network + 1;    // skip network address
            last = broadcast - 1;   // skip broadcast address
        }

        return Enumerate(first, last);
    }

    private static List<IPAddress> ParseRange(string spec)
    {
        var parts = spec.Split('-', 2);
        var start = ParseV4(parts[0].Trim());
        var rightRaw = parts[1].Trim();

        IPAddress end;
        if(rightRaw.Contains('.'))
        {
            end = ParseV4(rightRaw);
        }
        else
        {
            if (!int.TryParse(rightRaw, out var lastOctet) || lastOctet is < 0 or > 255)
                throw new FormatException($"Invalid range end: '{rightRaw}'.");
            var b = start.GetAddressBytes();
            b[3] = (byte)lastOctet;
            end = new IPAddress(b);
        }

        var s = ToUInt(start);
        var e = ToUInt(end);
        if (s > e) (s, e) = (e, s);
        return Enumerate(s, e);
    }

    private static List<IPAddress> Enumerate(uint first, uint last)
    {
        if (last < first)
            return new List<IPAddress>();

        var count = last - first + 1;
        if (count > MaxHosts)
            throw new FormatException($"Range too large ({count} addresses). Limit is {MaxHosts}.");

        var list = new List<IPAddress>((int)count);
        for(var v = first; ; v++)
        {
            list.Add(FromUInt(v));
            if (v == last) break;       // guard against uint overflow at 255.255.255.255
        }

        return list;
    }


    private static IPAddress ParseV4(string s)
    {
        if (!IPAddress.TryParse(s, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            throw new FormatException($"Invalid IPv4 address: '{s}'.");
        return ip;
    }

    private static uint ToUInt(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24 | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3]);
    }

    private static IPAddress FromUInt(uint v)
        => new(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v });
}
