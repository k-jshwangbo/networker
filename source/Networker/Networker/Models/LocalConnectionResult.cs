namespace Networker.Models;
public enum TcpConnectionState
{
    Closed = 1,
    Listen = 2,
    SynSent = 3,
    SynReceived = 4,
    Established = 5,
    FinWait1 = 6,
    FinWait2 = 7,
    CloseWait = 8,
    Closing = 9,
    LastAck = 10,
    TimeWait = 11,
    DeleteTcb = 12
}

/// <summary>
/// A single local TCP endpoint (any state) together with the owning process.
/// Produced by <see cref="Services.PortScan.Local.ILocalPortService"/>.
/// </summary>
public sealed class LocalConnectionResult
{
    /// <summary>"TCP" for IPv4, "TCP6" for IPv6.</summary>
    public string Protocol { get; init; } = "TCP";
    public string LocalAddress { get; init; } = "";
    public int LocalPort { get; init; }
    public string RemoteAddress { get; init; } = "";
    public int RemotePort { get; init; }

    public TcpConnectionState State { get; init; }
    public int Pid { get; init; }

    public string ProcessName { get; init; } = "";
    public string Service => WellKnownPorts.GetServiceName(LocalPort);

    public string LocalEndpoint => $"{LocalAddress}:{LocalPort}";
    public string RemoteEndpoint =>
        State == TcpConnectionState.Listen ? "" : $"{RemoteAddress}:{RemotePort}";
    public bool IsListening => State == TcpConnectionState.Listen;
    public string StateText => State switch
    {
        TcpConnectionState.Closed => "CLOSED",
        TcpConnectionState.Listen => "LISTEN",
        TcpConnectionState.SynSent => "SYN_SENT",
        TcpConnectionState.SynReceived => "SYN_RCVD",
        TcpConnectionState.Established => "ESTABLISHED",
        TcpConnectionState.FinWait1 => "FIN_WAIT1",
        TcpConnectionState.FinWait2 => "FIN_WAIT2",
        TcpConnectionState.CloseWait => "CLOSE_WAIT",
        TcpConnectionState.Closing => "CLOSING",
        TcpConnectionState.LastAck => "LAST_ACK",
        TcpConnectionState.TimeWait => "TIME_WAIT",
        TcpConnectionState.DeleteTcb => "DELETE_TCB",
        _ => State.ToString()
    };
}
