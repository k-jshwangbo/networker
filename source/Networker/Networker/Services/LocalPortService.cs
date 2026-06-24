using Networker.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Networker.Services;
public sealed class LocalPortService : ILocalPortService
{
    private const int AF_INET = 2;
    private const int AF_INET6 = 23;

    // TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL
    private const int TCP_TABLE_OWNER_PID_ALL = 5;
    private const uint ERROR_INSUFFICIENT_BUFFER = 122;


    public Task<IReadOnlyList<LocalConnectionResult>> GetTcpConnectionsAsync(
        CancellationToken cancellationToken)
        => Task.Run(() => Collect(cancellationToken), cancellationToken);


    private static IReadOnlyList<LocalConnectionResult> Collect(CancellationToken ct)
    {
        var results = new List<LocalConnectionResult>();
        var nameCache = new Dictionary<int, string>();

        ReadIpv4(results, nameCache, ct);
        ct.ThrowIfCancellationRequested();
        ReadIpv6(results, nameCache, ct);

        return results;
    }


    private static void ReadIpv4(
        List<LocalConnectionResult> results,
        Dictionary<int, string> nameCache,
        CancellationToken ct)
    {
        var buffer = QueryTable(AF_INET);
        try
        {
            int count = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                rowPtr = IntPtr.Add(rowPtr, rowSize);

                int pid = (int)row.owningPid;
                results.Add(new LocalConnectionResult
                {
                    Protocol = "TCP",
                    LocalAddress = new IPAddress(row.localAddr).ToString(),
                    LocalPort = ParsePort(row.localPort),
                    RemoteAddress = new IPAddress(row.remoteAddr).ToString(),
                    RemotePort = ParsePort(row.remotePort),
                    State = (TcpConnectionState)row.state,
                    Pid = pid,
                    ProcessName = ResolveProcessName(pid, nameCache)
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }


    private static void ReadIpv6(
        List<LocalConnectionResult> results,
        Dictionary<int, string> nameCache,
        CancellationToken ct)
    {
        var buffer = QueryTable(AF_INET6);
        try
        {
            int count = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = IntPtr.Add(buffer, 4);
            int rowSize = Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();

            for (int i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(rowPtr);
                rowPtr = IntPtr.Add(rowPtr, rowSize);

                int pid = (int)row.owningPid;
                results.Add(new LocalConnectionResult
                {
                    Protocol = "TCP6",
                    LocalAddress = new IPAddress(row.localAddr, row.localScopeId).ToString(),
                    LocalPort = ParsePort(row.localPort),
                    RemoteAddress = new IPAddress(row.remoteAddr, row.remoteScopeId).ToString(),
                    RemotePort = ParsePort(row.remotePort),
                    State = (TcpConnectionState)row.state,
                    Pid = pid,
                    ProcessName = ResolveProcessName(pid, nameCache)
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }


    private static IntPtr QueryTable(int addressFamily)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int size = 0;
            uint ret = GetExtendedTcpTable(
                IntPtr.Zero, ref size, true, addressFamily, TCP_TABLE_OWNER_PID_ALL);

            if (ret != ERROR_INSUFFICIENT_BUFFER && ret != 0)
                throw new Win32Exception((int)ret);

            IntPtr buffer = Marshal.AllocHGlobal(size);
            ret = GetExtendedTcpTable(
                buffer, ref size, true, addressFamily, TCP_TABLE_OWNER_PID_ALL);

            if (ret == 0)
                return buffer;

            Marshal.FreeHGlobal(buffer);

            if (ret != ERROR_INSUFFICIENT_BUFFER)
                throw new Win32Exception((int)ret);
        }

        throw new InvalidOperationException(
            "Unable to read the TCP table; the size kept changing between calls.");
    }


    private static int ParsePort(uint raw)
        => (int)((raw & 0xFF) << 8) | (int)((raw >> 8) & 0xFF);


    private static string ResolveProcessName(int pid, Dictionary<int, string> cache)
    {
        if (cache.TryGetValue(pid, out var cached))
            return cached;

        string name = pid switch
        {
            0 => "System Idle Process",
            4 => "System",
            _ => TryGetProcessName(pid)
        };

        cache[pid] = name;
        return name;
    }


    private static string TryGetProcessName(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return p.ProcessName;
        }
        catch
        {
            return "";
        }
    }


    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        bool bOrder,
        int ulAf,
        int tableClass,
        uint reserved = 0);


    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public uint localPort;
        public uint remoteAddr;
        public uint remotePort;
        public uint owningPid;
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCP6ROW_OWNER_PID
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] localAddr;
        public uint localScopeId;
        public uint localPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] remoteAddr;
        public uint remoteScopeId;
        public uint remotePort;
        public uint state;
        public uint owningPid;
    }
}
