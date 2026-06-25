using Networker.Models;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Networker.Services.RoutingTable;

public sealed class RoutingTableService : IRoutingTableService
{
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;

    public Task<IReadOnlyList<RouteEntry>> GetRoutesAsync(CancellationToken ct)
        => Task.Run(() => Collect(ct), ct);

    private static IReadOnlyList<RouteEntry> Collect(CancellationToken ct)
    {
        var ifNames = BuildInterfaceNameMap();
        var buffer = QueryTable();

        try
        {
            int count = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_IPFORWARDROW>();

            var routes = new List<RouteEntry>(count);

            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var row = Marshal.PtrToStructure<MIB_IPFORWARDROW>(rowPtr);
                rowPtr = IntPtr.Add(rowPtr, rowSize);

                int ifIndex = (int)row.dwForwardIfIndex;
                ifNames.TryGetValue(ifIndex, out var name);

                routes.Add(new RouteEntry
                {
                    Destination = new IPAddress(row.dwForwardDest).ToString(),
                    PrefixLength = BitOperations.PopCount(row.dwForwardMask),
                    Netmask = new IPAddress(row.dwForwardMask).ToString(),
                    Gateway = new IPAddress(row.dwForwardNextHop).ToString(),
                    InterfaceIndex = ifIndex,
                    InterfaceName = name ?? "",
                    Metric = row.dwForwardMetric1,
                    RouteType = TypeText(row.dwForwardType),
                    Protocol = ProtoText(row.dwForwardProto)
                });
            }

            return routes.OrderByDescending(r => r.IsDefault).ToList();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }



    private static Dictionary<int, string> BuildInterfaceNameMap()
    {
        var map = new Dictionary<int, string>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            try
            {
                var props = ni.GetIPProperties().GetIPv4Properties();
                if (props != null)
                    map[props.Index] = ni.Name;
            }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
        }

        return map;
    }


    private static IntPtr QueryTable()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int size = 0;
            uint ret = GetIpForwardTable(IntPtr.Zero, ref size, true);
            if (ret != ERROR_INSUFFICIENT_BUFFER && ret != 0)
                throw new Win32Exception((int)ret);

            IntPtr buffer = Marshal.AllocHGlobal(size);
            ret = GetIpForwardTable(buffer, ref size, true);
            if (ret == 0)
                return buffer;

            Marshal.FreeHGlobal(buffer);
            if (ret != ERROR_INSUFFICIENT_BUFFER)
                throw new Win32Exception((int)ret);
        }

        throw new InvalidOperationException(
            "Unable to read the routing table; its size kept changing between calls.");
    }


    private static string TypeText(uint type) => type switch
    {
        1 => "Other",
        2 => "Invalid",
        3 => "Direct",
        4 => "Indirect",
        _ => type.ToString()
    };


    private static string ProtoText(uint proto) => proto switch
    {
        1 => "Other",
        2 => "Local",
        3 => "Network management",
        4 => "ICMP",
        10002 => "NT auto-static",
        10006 => "NT static",
        10007 => "NT static (non-DOD)",
        _ => proto.ToString()
    };


    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetIpForwardTable(IntPtr pIpForwardTable, ref int pdwSize, bool bOrder);


    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_IPFORWARDROW
    {
        public uint dwForwardDest;
        public uint dwForwardMask;
        public uint dwForwardPolicy;
        public uint dwForwardNextHop;
        public uint dwForwardIfIndex;
        public uint dwForwardType;
        public uint dwForwardProto;
        public uint dwForwardAge;
        public uint dwForwardNextHopAS;
        public uint dwForwardMetric1;
        public uint dwForwardMetric2;
        public uint dwForwardMetric3;
        public uint dwForwardMetric4;
        public uint dwForwardMetric5;
    }
}