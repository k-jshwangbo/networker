using System.Net;
using System.Net.NetworkInformation;

namespace Networker.Models;

public sealed class PingResult
{
    public int          Sequence    { get; init; }
    public DateTime     Timestamp   { get; init; } = DateTime.Now;
    public IPStatus     Status      { get; init; }
    public IPAddress?   Address     { get; init; }
    public long         RoundtripMs { get; init; }
    public int          Ttl         { get; init; }

    public bool   IsSuccess  => Status == IPStatus.Success;
    public string StatusText => IsSuccess ? "Releay" : Status.ToString();

} // end class