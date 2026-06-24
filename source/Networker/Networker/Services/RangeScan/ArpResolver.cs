using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Networker.Services.RangeScan;
public static class ArpResolver
{
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int destIp, int srcIp, byte[] macAddr, ref int macAddrLen);

    public static string? Resolve(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return null;

        var destIp = BitConverter.ToInt32(ip.GetAddressBytes(), 0);
        var mac = new byte[6];
        var len = mac.Length;

        try
        {
            var result = SendARP(destIp, 0, mac, ref len);
            if (result != 0 || len == 0)
                return null;
        }
        catch (DllNotFoundException)
        {
            return null;
        }

        return string.Join("-", mac.Take(len).Select(b => b.ToString("X2")));
    }
}
