# ARNet Discovery

**Substation LAN Discovery & Protocol Fingerprint Tool**

ARNet Discovery is a lightweight Windows engineering tool for quickly finding devices connected to a LAN, then visually inspecting likely relay, gateway, switch, RTU, PLC, serial-server, and web-managed devices.

License: **Apache-2.0**

## Why this exists

Field engineers should not need to manually ping relay IP addresses one by one. ARNet Discovery scans the selected laptop adapter/subnet, builds a snapshot of discovered hosts, probes industrial protocol ports, classifies the device type, and exposes diagnostics without crashing the app.

## Main features

- Auto-detect active IPv4 network adapters
- Fast async subnet scan with guarded concurrency
- ICMP ping + TCP port probing
- Industrial protocol fingerprint chips:
  - IEC 61850 MMS / ISO-on-TCP: 102
  - IEC 60870-5-104: 2404
  - Modbus TCP: 502
  - DNP3 TCP: 20000
  - OPC UA: 4840
  - SNMP: 161
  - Web/HTTPS/SSH/Telnet
- ARP table enrichment for MAC address
- Optional local OUI CSV lookup
- Visual discovery canvas with selected-device inspector
- Buffer snapshot model: current visible result is stable while scan updates in the background
- Diagnostic sink: handled exceptions are routed to Diagnostics instead of killing the app
- CSV export
- No heavy third-party UI dependency

## Project structure

```text
ARNetDiscovery
├─ src
│  ├─ ARNetDiscovery.Core   # scan engine, subnet math, adapter detection, classification
│  ├─ ARNetDiscovery.Wpf    # premium WPF desktop UI
│  └─ ARNetDiscovery.Cli    # lightweight CLI smoke-test scanner
├─ docs
│  └─ ARCHITECTURE.md
├─ tools
│  └─ publish-win-x64.bat
├─ LICENSE
├─ NOTICE
└─ .gitignore
```

## Build with Visual Studio 2026

1. Install **.NET Desktop Development** workload.
2. Open `ARNetDiscovery.sln`.
3. Set `ARNetDiscovery.Wpf` as startup project.
4. Build and run.

## Publish portable Windows build

From repository root:

```bat
tools\publish-win-x64.bat
```

Or manually:

```bat
dotnet publish src\ARNetDiscovery.Wpf\ARNetDiscovery.Wpf.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be under:

```text
artifacts\publish\win-x64
```

## OUI / vendor catalog

The app includes an **optional lightweight CSV loader** for MAC vendor lookup. To keep this Apache-2.0 project clean, the repository does **not** embed a full third-party OUI database with unclear redistribution constraints.

You can place your own file here:

```text
src\ARNetDiscovery.Wpf\Data\oui-custom.csv
```

Format:

```csv
prefix,vendor
001122,Example Vendor
AABBCC,Another Vendor
```

The app normalizes separators, so `00:11:22`, `00-11-22`, and `001122` are accepted.

## Safety note

Use this tool only on networks you are authorized to test. Default timeouts and concurrency are intentionally conservative for industrial LANs.

## v1.4 field-safety update

- Uses a smart local scan window when an adapter reports an overly broad subnet such as `/8`.
- Default scan is capped to 254 hosts for a responsive field workflow.
- TCP probing is globally throttled to avoid thousands of simultaneous socket attempts.
- Ping-failed hosts are probed only on critical industrial ports by default.
- TCP timeout tasks are cancelled/observed correctly so background socket failures do not surface as unobserved task exceptions.
- Empty-state and session cards now explain likely causes when no real device is discovered: wrong NIC, VLAN/cable issue, bad IP/mask, powered-off target, or blocked ports.

## v1.6 Performance / Smart UX Notes

ARNet Discovery intentionally separates the full scan snapshot from the topology canvas.

- `Devices` keeps the full buffered scan evidence for export and inspection.
- `VisibleDevices` renders only the highest-value devices on the canvas to keep WPF interaction responsive.
- Device updates are batched every 350 ms instead of refreshing the visual tree for every discovered host.
- Ping-blocked hosts are probed on industrial TCP ports by default; web-only discovery is suppressed to avoid false positives on public/routed networks.
- If the adapter uses a non-private IP address or a very wide mask, ARNet warns the user and scans conservatively.

This prevents a noisy network from producing hundreds of heavy visual cards while still preserving the underlying scan evidence.

## v2.0 Progressive Discovery update

ARNet now uses a progressive evidence pipeline instead of waiting for the whole scan to finish:

1. **Fast host evidence first** — ICMP ping uses a short timeout and publishes reachable hosts immediately.
2. **Protocol enrichment second** — IEC 61850, IEC 104, Modbus TCP, DNP3, OPC UA, and web ports are probed in the background.
3. **UI batching** — scan workers write to an in-memory snapshot buffer; the WPF UI refreshes in small batches so the table remains responsive.
4. **Own laptop IP excluded** — the active NIC IP is not listed as a discovered device.
5. **Direct Probe / Custom Range** — use `1.110.5.1`, `1.110.5.0/24`, or `1.110.5.1-1.110.5.20` to inspect routed segments outside the safe local scan window.
6. **Clean table-first UX** — the app now prioritizes a virtualized device table with a right-side evidence inspector instead of rendering a heavy topology canvas.

Default local scan profile:

```text
Ping timeout        : 200 ms
TCP timeout         : 350 ms
Ping concurrency    : 96
TCP concurrency     : 96
UI flush interval   : 250 ms
Local scan cap      : 254 hosts
```

Direct probe uses slightly longer timeouts because routed targets can legitimately respond slower than local panel devices.

## v2.1 Target List Scan update

ARNet now supports **Import Target List Scan** for commissioning/FAT inventory verification.

Supported import formats:

```text
.xlsx  Excel workbook with IP ADDRESS column
.csv   CSV table with IP ADDRESS or IP column
.txt   Plain text list containing IPv4 addresses
```

For Excel workbooks such as IED name/IP address sheets, ARNet auto-detects common columns:

```text
NO, NAMA BAY, JENIS BAY, PANEL, JENIS PERALATAN,
NO DEVICE, NAMA IED, IP ADDRESS, REMARK
```

Workflow:

```text
Import List
  -> all expected devices appear immediately as Pending
  -> Scan List probes exact IPs only, even across routed segments
  -> ping evidence updates rows first
  -> protocol ports enrich rows in the background
  -> unresolved targets become No response with diagnostic guidance
```

This is intentionally different from subnet sweep scanning. If an engineering sheet already contains the official target inventory, ARNet scans the exact IP list instead of sweeping a noisy `/16` or unsafe `/8` range.
