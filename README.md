# ARNet Discovery

<p align="left">
  <a href="https://github.com/masarray/ARNetDiscovery/actions/workflows/build.yml"><img alt="Build" src="https://github.com/masarray/ARNetDiscovery/actions/workflows/build.yml/badge.svg?branch=main"></a>
  <a href="https://github.com/masarray/ARNetDiscovery/actions/workflows/release.yml"><img alt="Release" src="https://github.com/masarray/ARNetDiscovery/actions/workflows/release.yml/badge.svg"></a>
  <a href="https://github.com/masarray/ARNetDiscovery/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/masarray/ARNetDiscovery?label=release"></a>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%20WPF-0078D4?logo=windows&logoColor=white">
  <img alt="License" src="https://img.shields.io/badge/license-Apache--2.0-blue.svg">
</p>

**ARNet Discovery** is a portable Windows LAN discovery and relay IP scanner for substation automation work. It helps engineers discover devices on engineering LANs, verify imported target lists, and identify active protocol evidence from protection relays, bay controllers, IEDs, Ethernet switches, gateways, SCADA servers, WinCC workstations, PLCs, meters, printers, and web-managed devices.

The goal is simple: instead of manually pinging relay IP addresses one by one, you can scan a network adapter or an engineering IP list and see which devices respond, what protocol evidence is visible, and which targets need follow-up during FAT, commissioning, and troubleshooting.

ARNet Discovery is licensed under **Apache-2.0**.

Common search terms for this project include **substation LAN scanner**, **relay IP scanner**, **IEC 61850 scanner**, **IEC 104 network discovery**, **Modbus TCP scanner**, **OPC UA discovery**, **industrial automation LAN discovery**, **FAT testing tool**, and **commissioning network checker**.

---

## What ARNet Discovery does

ARNet Discovery helps during FAT, commissioning, troubleshooting, and substation LAN checks.

It can:

- scan the selected laptop network adapter using a safe local scan window;
- probe a single IP address or a custom IP range;
- import an Excel, CSV, or TXT target list and scan only the expected device IPs;
- show reachable devices immediately while protocol checks continue in the background;
- display ping, MAC, open ports, protocol candidates, and diagnostics in one clean table;
- compare expected engineering devices against actual network response;
- export scan results to CSV for documentation or punch-list follow-up;
- create portable Windows builds through GitHub Actions releases.

---

## Typical use cases

### 1. Quick LAN discovery

Use **Scan Local** when your laptop is connected to a relay, panel, or switch LAN and you want to see what is visible from the selected adapter.

ARNet checks the selected adapter, excludes the laptop's own IP address, scans a safe local window, and shows discovered devices progressively.

### 2. Direct IP probe

Use **Probe** when you already know the target IP address.

Examples:

```text
1.110.5.1
192.168.1.10
10.10.10.21
```

This is useful when a device is reachable through routing but is outside the current local scan window.

### 3. Target list verification

Use **Import Excel** when you have an engineering IP list from a project document.

Supported formats:

```text
.xlsx   Excel workbook
.csv    comma-separated table
.txt    plain text IP list
```

For Excel sheets, ARNet detects common engineering columns such as:

```text
NO, NAMA BAY, JENIS BAY, PANEL, JENIS PERALATAN,
NO DEVICE, NAMA IED, IP ADDRESS, REMARK
```

After import, every expected target appears in the table first. Then **Scan List** checks each exact IP and updates the row with ping, port, protocol, and diagnostic evidence.

This avoids noisy subnet sweeps and makes the tool useful as an inventory verifier.

---

## Protocol evidence

ARNet Discovery uses lightweight evidence checks during normal scanning. It is designed to be responsive and conservative on industrial networks.

Default protocol evidence includes:

| Evidence | Default port | Typical device meaning |
|---|---:|---|
| IEC 61850 MMS / ISO-on-TCP | 102 | protection relay, BCU, IED, gateway |
| IEC 60870-5-104 | 2404 | RTU, gateway, automation controller |
| Modbus TCP | 502 | PLC, meter, gateway, controller |
| DNP3 TCP | 20000 | outstation, RTU, gateway |
| OPC UA | 4840 | SCADA, WinCC workstation/server, OPC server |
| HTTP / HTTPS | 80 / 443 | web-managed device, server, switch, relay web UI |
| SSH / Telnet | 22 / 23 | switch, router, gateway, server management |

A port result is treated as evidence, not as a final device guarantee. For example, `Port 102 open` means the device is an IEC 61850 candidate and should be verified further if needed.

---

## Reading the results

ARNet separates reachability and protocol evidence so the result is easier to trust.

| Status | Meaning |
|---|---|
| Online | Device replied to quick ping and/or strong evidence was found |
| Ping only | Device replied to ping but no known industrial protocol port was detected |
| Port open | Device did not reply to ping, but one or more checked ports responded |
| No response | Expected target did not respond to quick ping or default protocol checks |
| Expected | Target came from an imported engineering list |

Useful interpretation examples:

```text
Ping OK + IEC 61850 evidence
-> target is reachable and likely has an IEC 61850 service active.

No ping + Port 102 open
-> ICMP may be blocked, but IEC 61850 evidence is visible.

Ping OK + no protocol detected
-> host is reachable, but default industrial protocol evidence was not found.

Expected + No response
-> check VLAN, route, cable, panel power, wrong IP, firewall, or device setting.
```

---

## Download and run

The easiest way to use ARNet Discovery is to download the latest portable Windows release:

```text
https://github.com/masarray/ARNetDiscovery/releases/latest
```

Then:

1. download the `ARNetDiscovery-*-win-x64-portable.zip` file;
2. extract the ZIP;
3. run `ARNetDiscovery.exe`;
4. select the correct network adapter;
5. use **Scan Local**, **Probe**, or **Import Excel** + **Scan List**.

No installer is required for the portable build.

---

## Build from source

### Requirements

- Windows
- Visual Studio 2026 or compatible Visual Studio version
- .NET Desktop Development workload
- .NET 8 SDK

### Build in Visual Studio

1. Open `ARNetDiscovery.sln`.
2. Set `ARNetDiscovery.Wpf` as startup project.
3. Build and run.

### Publish portable build locally

From the repository root:

```bat
tools\publish-win-x64.bat
```

Or manually:

```bat
dotnet publish src\ARNetDiscovery.Wpf\ARNetDiscovery.Wpf.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Output path:

```text
artifacts\publish\win-x64
```

---

## GitHub Actions release build

This repository includes CI and release workflows:

```text
.github/workflows/build.yml     build and publish CI artifact
.github/workflows/release.yml   create portable Windows GitHub Release from a tag
```

To create a public portable release:

```bat
git tag v2.12.0
git push origin v2.12.0
```

The release workflow creates:

```text
ARNetDiscovery-v2.12.0-win-x64-portable.zip
ARNetDiscovery-v2.12.0-win-x64-portable.zip.sha256
```

---

## Optional MAC vendor lookup

ARNet can read a small local vendor lookup CSV. This is optional.

Place a file here:

```text
src\ARNetDiscovery.Wpf\Data\oui-custom.csv
```

Format:

```csv
prefix,vendor
001122,Example Vendor
AABBCC,Another Vendor
```

Accepted prefix formats:

```text
00:11:22
00-11-22
001122
```

If no vendor file is provided, ARNet still works; the vendor column may simply remain blank or unresolved.

---

## Safety and responsible use

Use ARNet Discovery only on networks where you have permission to test. The default scan profile is intentionally conservative for engineering LANs:

- local scan is capped for broad subnet masks;
- host discovery runs with short timeouts;
- protocol checks are lightweight by default;
- diagnostics are shown in the app instead of crashing the UI.

ARNet is a discovery and verification aid. It does not replace project network drawings, relay settings, cyber-security requirements, or formal commissioning procedures.

---

## Documentation

- [Product landing page](https://masarray.github.io/ARNetDiscovery/)
- [Architecture](docs/ARCHITECTURE.md)
- [User guide](docs/USER_GUIDE.md)
- [Product roadmap](docs/ROADMAP.md)
- [GitHub discovery metadata](docs/GITHUB_SEO.md)
- [Contributing](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)

---

## Contributing

Contributions are welcome when they improve reliability, evidence quality, performance, documentation, or usability.

Good contribution areas:

- safer protocol evidence validators;
- target-list import improvements;
- scan result export/reporting;
- UI accessibility and keyboard navigation;
- deterministic tests for subnet parsing, importer logic, and classification;
- documentation examples from realistic lab/commissioning workflows.

Please keep the app lightweight, conservative on industrial networks, and clear about what is evidence versus confirmed protocol behavior.
