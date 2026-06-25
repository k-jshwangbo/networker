using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace Networker.Core;

public sealed class SubnetInfo
{
    public required IPAddress Address { get; init; }
    public int PrefixLength { get; init; }
    public required IPAddress SubnetMask { get; init; }
    public required IPAddress WildcardMask { get; init; }
    public required IPAddress NetworkAddress { get; init; }
    public required IPAddress BroadcastAddress { get; init; }
    public IPAddress? FirstHost { get; init; }
    public IPAddress? LastHost { get; init; }
    public long TotalAddresses { get; init; }
    public long UsableHosts { get; init; }
    public required string AddressClass { get; init; }
    public required string Scope { get; init; }
    public required string MaskBinary { get; init; }
}

public static class SubnetCalculator
{
    public static SubnetInfo Calculate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException("Enter an address, e.g. 192.168.1.10/24");

        var (ipPart, maskPart) = SplitInput(input.Trim());
        var address = ParseV4(ipPart);

        int prefix = maskPart.Contains('.')
            ? PrefixFromMask(ParseV4(maskPart))
            : ParsePrefix(maskPart);

        uint mask = MaskFromPrefix(prefix);
        uint addr = ToUInt(address);
        uint network = addr & mask;
        uint wildcard = ~mask;
        uint broadcast = network | wildcard;

        long total = 1L << (32 - prefix);

        IPAddress? firstHost, lastHost;
        long usable;
        if (prefix >= 31)
        {
            // /31 (RFC 3021 point-to-point) and /32 (host route): every address usable.
            firstHost = FromUInt(network);
            lastHost = FromUInt(broadcast);
            usable = total;
        }
        else
        {
            firstHost = FromUInt(network + 1);
            lastHost = FromUInt(broadcast - 1);
            usable = total - 2;
        }

        return new SubnetInfo
        {
            Address = address,
            PrefixLength = prefix,
            SubnetMask = FromUInt(mask),
            WildcardMask = FromUInt(wildcard),
            NetworkAddress = FromUInt(network),
            BroadcastAddress = FromUInt(broadcast),
            FirstHost = firstHost,
            LastHost = lastHost,
            TotalAddresses = total,
            UsableHosts = usable,
            AddressClass = ClassOf(address),
            Scope = ScopeOf(address),
            MaskBinary = ToBinary(mask)
        };
    }


    private static (string ip, string mask) SplitInput(string s)
    {
        int slash = s.IndexOf('/');
        if (slash >= 0)
            return (s[..slash].Trim(), s[(slash + 1)..].Trim());

        int sp = s.IndexOfAny(new[] { ' ', '\t', ',' });
        if (sp >= 0)
            return (s[..sp].Trim(), s[(sp + 1)..].Trim());

        throw new FormatException("Add a prefix or mask, e.g. 192.168.1.10/24");
    }


    private static int ParsePrefix(string s)
    {
        if (!int.TryParse(s, out var prefix) || prefix is < 0 or > 32)
            throw new FormatException($"Invalid prefix '/{s}'. Must be 0-32.");
        return prefix;
    }


    private static int PrefixFromMask(IPAddress mask)
    {
        uint m = ToUInt(mask);
        uint wildcard = ~m;

        if ((wildcard & (wildcard + 1)) != 0)
            throw new FormatException($"'{mask}' is not a valid (contiguous) subnet mask.");

        return BitOperations.PopCount(m);
    }


    private static uint MaskFromPrefix(int prefix)
        => prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);


    private static string ClassOf(IPAddress ip)
    {
        int first = ip.GetAddressBytes()[0];
        return first switch
        {
            <= 127 => "A",
            <= 191 => "B",
            <= 224 => "C",
            <= 239 => "D (multicast)",
            _ => "E (experimental" 
        };
    }


    private static string ScopeOf(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return (b[0], b[1]) switch
        {
            (10, _) => "Privatee (RFC 1918)",
            (172, >= 16 and <= 31) => "Private (RFC 1918)",
            (192, 168) => "Private (RFC 1918)",
            (127, _) => "Loopback",
            (169, 254) => "Link-local (APIPA)",
            (100, >= 64 and <= 127) => "Carrier-grade NAT (RFC 6598)",
            _ when b[0] >= 224 => "Reserved (multicast/experimental)",
            _ => "Public",
        };
    }


    private static string ToBinary(uint value)
    {
        var sb = new StringBuilder(35);
        for (int shift = 24; shift >= 0; shift -= 8)
        {
            if (shift != 24) sb.Append('.');
            sb.Append(Convert.ToString((byte)(value >> shift), 2).PadLeft(8, '0'));
        }
        return sb.ToString();
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
