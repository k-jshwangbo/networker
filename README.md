# Networker

A lightweight network diagnostics and testing toolkit for Windows, built with WPF on .NET 10 and styled with [WPF UI](https://github.com/lepoco/wpfui) for a modern Fluent look.

Networker bundles common network-troubleshooting tasks — connectivity checks, port scanning, local connection inspection, subnet discovery, and service identification — into a single desktop application with a responsive, tabbed UI. Most features run without administrator privileges and rely almost entirely on the .NET base class library, keeping the application small and dependency-light.

## Features

Implemented:

- **Ping** — ICMP echo with round-trip time, packet loss, and continuous monitoring for connection-quality checks.
- **Remote Port Scan** — TCP connect scan against a target host with configurable port ranges, timeout, and bounded concurrency.
- **Local Ports** — Lists the machine's local TCP connections (all states, including `LISTEN` and `ESTABLISHED`) together with the owning process ID and name, via the Windows IP Helper API.
- **Banner Grabbing** — Connects to open ports and captures service banners for identification. Sends an HTTP probe on web ports, performs a TLS handshake on secure ports, and reads service-initiated greetings (FTP, SSH, SMTP, …) elsewhere. Supports an optional custom probe.
- **Network Range Scan** — Subnet sweep to discover live hosts, combining ICMP ping with ARP resolution to catch hosts that block ICMP.
- **Traceroute** — Hop-by-hop path tracing to a destination, useful for locating latency or routing problems.

Results stream into the UI in real time as each scan progresses.

## Screenshots

The interface uses WPF UI's Fluent controls (Mica backdrop, light theme), with one tab per tool.

<!-- Add screenshots here, e.g. ![Local Ports](docs/local-ports.png) -->

## Requirements

- Windows 10 (21H2) or later (Windows 11 recommended for the full Mica backdrop)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build, or the .NET 10 Runtime for framework-dependent builds (self-contained builds bundle the runtime)

## Build & Run

```bash
git clone https://github.com/k-jshwangbo/networker.git
cd networker

# Run directly
dotnet run --project source/Networker/Networker

# or build a Release binary
dotnet build source/Networker/Networker -c Release
```

Alternatively, open the solution in **Visual Studio 2022** (17.12+ with the .NET 10 workload) and press F5. NuGet restores `WPF-UI` automatically on first build.

## Project Structure

Services are organized by feature (interface + implementation co-located per folder), so new tools slot in without touching unrelated code:

```
source/Networker/Networker/
├── Core/            # MVVM base, RelayCommand/AsyncRelayCommand, ParallelExecutor, range parsers
├── Models/          # Result/data types (PingResult, BannerResult, LocalConnectionResult, …)
├── Services/        # Feature-organized network services
│   ├── Ping/
│   ├── PortScan/
│   │   ├── Local/   # Local connection table enumeration (iphlpapi P/Invoke)
│   │   └── Remote/  # TCP connect scan
│   ├── RangeScan/   # Host discovery (ICMP + ARP)
│   ├── Traceroute/
│   └── BannerGrab/  # Banner grabbing (TCP / TLS)
├── ViewModels/      # One view-model per tool, plus MainViewModel composition root
├── Views/           # WPF UI Fluent views (one per feature tab)
├── App.xaml         # WPF UI theme dictionaries
└── Networker.csproj
```

## Tech Stack

- **Language:** C# (nullable enabled, implicit usings)
- **Runtime:** .NET 10 (`net10.0-windows`)
- **UI:** WPF with the MVVM pattern; live result updates via `ObservableCollection`. Fluent design provided by **WPF UI** (`FluentWindow`, Mica backdrop, themed controls).
- **Concurrency:** `async`/`await` with `SemaphoreSlim` for bounded parallelism, `CancellationToken` for cancellation, and `IProgress<T>` for thread-safe UI updates.
- **Platform interop:** P/Invoke into `iphlpapi.dll` (`GetExtendedTcpTable`) for owning-PID lookup; `SslStream` for TLS banner grabbing.

## Dependencies

| Package | Version | License | Purpose |
| --- | --- | --- | --- |
| [WPF-UI](https://github.com/lepoco/wpfui) | 4.3.0 | MIT | Fluent design controls and theming |

Everything else relies on the .NET base class library. `DnsClient.NET` is planned for the upcoming DNS lookup feature but is not yet a dependency.

## Roadmap

- [x] Ping with continuous latency monitoring
- [x] Remote port scan with bounded concurrency
- [x] Local connection / port listing (with owning PID)
- [x] Banner grabbing (TCP + TLS)
- [x] Network range scan (ICMP + ARP)
- [x] Traceroute
- [x] WPF UI Fluent redesign
- [ ] DNS lookup (A, AAAA, MX, TXT, NS, …)
- [ ] TLS/SSL certificate inspection (expiry, issuer, chain)
- [ ] HTTP/HTTPS health check (status, timing, redirects)
- [ ] CSV/JSON export of results
- [ ] Bandwidth/throughput measurement
- [ ] Wake-on-LAN

## Responsible Use

Networker is intended for diagnosing and testing networks that you own or have explicit permission to test. Port scanning, banner grabbing, and host discovery against systems without authorization may violate acceptable-use policies or local laws. You are responsible for ensuring you have permission before scanning any host or network.

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes before submitting a pull request.

## Third-Party Notices

Networker uses third-party open-source components. Their copyright notices and
license texts are reproduced in [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt).

- **WPF UI** (4.3.0) — Fluent design system for WPF, licensed under the MIT License.
  Copyright (c) 2021-2026 Leszek Pomianowski and WPF UI Contributors.
  <https://github.com/lepoco/wpfui>