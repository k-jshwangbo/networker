using System.Net;
using System.Net.NetworkInformation;

namespace Networker.Models;

public sealed class PingResult
{
    public int          Sequence    { get; set; }
    public DateTime     Timestamp   { get; set; } = DateTime.Now;
    public IPStatus     Status      { get; set; }
    public IPAddress?   Address     { get; set; }
    public long         RoundtripMs { get; set; }
    public int          Ttl         { get; set; }

    public bool   IsSuccess  => Status == IPStatus.Success;
    public string StatusText => IsSuccess ? "Releay" : Status.ToString();

} // end class