namespace Networker.Models;

public sealed class RouteEntry
{
    public string Destination { get; init; } = "";
    public int PrefixLength { get; init; }
    public string Netmask { get; init; } = "";
    public string Gateway { get; init; } = "";
    public int InterfaceIndex { get; init; }
    public string InterfaceName { get; init; } = "";
    public long Metric { get; init; }
    public string RouteType { get; init; } = "";
    public string Protocol { get; init; } = "";

    public bool IsDefault => Destination == "0.0.0.0" && PrefixLength == 0;

    public string DestinationCidr => $"{Destination}/{PrefixLength}";

    public string GatewayText => Gateway == "0.0.0.0" ? "On-link" : Gateway;

    public string InterfaceText => string.IsNullOrEmpty(InterfaceName)
        ? InterfaceIndex.ToString()
        : $"{InterfaceName} ({InterfaceIndex})";
}
