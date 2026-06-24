namespace Networker.Models;
public enum PortState
{
    Open,
    Closed,
    Filtered    // no response within the timeout (likely firewalld)
}

public sealed class PortScanResult
{
    public int Port { get; init; }
    public PortState State { get; init; }
    public long ResponseMs { get; init; }

    public bool IsOpen => State == PortState.Open;
    public string Service => WellKnownPorts.GetServiceName(Port);
    public string StateText => State.ToString();
}
