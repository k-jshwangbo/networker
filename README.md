# Networker

A lightweight network diagnostics and testing toolkit for Windows, built with WPF on .NET 10.

Networker bundles the most common network troubleshooting tasks — connectivity checks, port scanning, and subnet discovery — into a single desktop application with a responsive, modern UI. Most features run without administrator privileges and rely only on the .NET base class library, keeping the application small and dependency-free.

## Features

- **Ping** — ICMP echo with round-trip time, packet loss, and continuous monitoring for connection-quality checks.
- **Traceroute** — Hop-by-hop path tracing to a destination, useful for locating latency or routing problems.
- **Local Port Scan** — Lists active TCP/UDP listeners on the local machine.
- **Remote Port Scan** — TCP connect scan against a target host with configurable port ranges and timeouts.
- **Network Range Scan** — Subnet sweep to discover live hosts, combining ICMP ping with ARP resolution to catch hosts that block ICMP.
- **Banner Grabbing** — Service and version identification on open ports.
- **DNS Lookup** — Forward and reverse lookups across record types (A, AAAA, MX, TXT, NS, and more).
- **TLS/SSL Certificate Inspection** — Expiry date, issuer, and certificate-chain validation for a target host.
- **HTTP/HTTPS Health Check** — Status codes, response times, and redirect tracing.

Scan results stream into the UI in real time and can be exported to CSV/JSON for use with other tools.

## Requirements

- Windows 10 (21H2) or later
- .NET 10 Runtime (only required for framework-dependent builds; self-contained builds include the runtime)

## Getting Started

### Build from source

```bash
git clone https://github.com/<your-username>/networker.git
cd networker
dotnet build -c Release
```

### Run

```bash
dotnet run -c Release
```

### Publish a standalone executable

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The resulting single-file executable can be run on any supported Windows 10+ machine without installing the .NET runtime separately.

## Tech Stack

- **Language:** C#
- **UI:** WPF (MVVM pattern with `ObservableCollection` for live result updates)
- **Runtime:** .NET 10 (LTS)
- **Concurrency:** `async`/`await` with `SemaphoreSlim` for bounded parallelism, `CancellationToken` for cancellation, and `IProgress<T>` for thread-safe UI updates
- **Dependencies:** Primarily the .NET base class library; [DnsClient.NET](https://github.com/MichaCo/DnsClient.NET) for extended DNS record queries

## Roadmap

- [ ] Ping and continuous latency/jitter monitoring
- [ ] Remote port scan with bounded concurrency
- [ ] Network range scan (ICMP + ARP)
- [ ] Traceroute
- [ ] DNS lookup
- [ ] Banner grabbing
- [ ] TLS certificate inspection
- [ ] HTTP health check
- [ ] CSV/JSON export
- [ ] Bandwidth/throughput measurement
- [ ] Wake-on-LAN

## Responsible Use

Networker is intended for diagnosing and testing networks that you own or have explicit permission to test. Port scanning and host discovery against systems without authorization may violate acceptable-use policies or local laws. You are responsible for ensuring you have permission before scanning any host or network.

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a pull request.