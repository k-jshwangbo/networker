namespace Networker.Models;
public static class WellKnownPorts
{
    private static readonly Dictionary<int, string> Map = new()
    {
        [20] = "FTP-Data",
        [21] = "FTP",
        [22] = "SSH",
        [23] = "Telnet",
        [25] = "SMTP",
        [53] = "DNS",
        [80] = "HTTP",
        [110] = "POP3",
        [111] = "RPC",
        [123] = "NTP",
        [135] = "MSRPC",
        [139] = "NetBIOS",
        [143] = "IMAP",
        [161] = "SNMP",
        [389] = "LDAP",
        [443] = "HTTPS",
        [445] = "SMB",
        [465] = "SMTPS",
        [587] = "SMTP",
        [636] = "LDAPS",
        [993] = "IMAPS",
        [995] = "POP3S",
        [1433] = "MSSQL",
        [1521] = "Oracle",
        [3306] = "MySQL",
        [3389] = "RDP",
        [5432] = "PostgreSQL",
        [5900] = "VNC",
        [6379] = "Redis",
        [8080] = "HTTP-Alt",
        [8443] = "HTTPS-Alt",
        [27017] = "MongoDB"
    };

    public static string GetServiceName(int port)
        => Map.TryGetValue(port, out var name) ? name : "";
}
