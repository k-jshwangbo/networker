namespace Networker.Core;
public static class PortRange
{
    public static IReadOnlyCollection<int> Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            throw new FormatException("Port range is empty.");

        var ports = new SortedSet<int>();

        foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var dash = part.IndexOf('-');

            if (dash < 0)
            {
                ports.Add(ParsePort(part));
            }
            else
            {
                var start = ParsePort(part[..dash].Trim());
                var end = ParsePort(part[(dash + 1)..].Trim());

                if (start > end)
                    (start, end) = (end, start);
                for (var p = start; p <= end; p++)
                    ports.Add(p);
            }
        }

        return new List<int>(ports);
    }

    private static int ParsePort(string s)
    {
        if (!int.TryParse(s, out var port) || port is < 1 or > 65535)
            throw new FormatException($"Invalid port: '${s}'. Ports must be between 1 and 65535.");

        return port;
    }
}