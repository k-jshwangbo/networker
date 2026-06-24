using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Networker.Core;
public static class LocalNetwork
{
    public static string GetDefaultRange()
    {
        var ip = GetPrimaryIPv4();
        if (ip is null)
            return "192.168.0.1-254";

        var b = ip.GetAddressBytes();
        return $"{b[0]}.{b[1]}.{b[2]}.1-254";
    }

    public static IPAddress? GetPrimaryIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(ua.Address))
                {
                    return ua.Address;
                }
            }
        }
        return null;
    }
}