# ARNet Discovery architecture

ARNet Discovery is built as a small Windows desktop product with a separated scanning core and WPF presentation layer.

The architecture is designed around one product behavior: **show useful evidence as soon as it is available, then enrich the same rows in the background**.

---

## Solution layout

```text
ARNetDiscovery
├─ src
│  ├─ ARNetDiscovery.Core
│  ├─ ARNetDiscovery.Wpf
│  └─ ARNetDiscovery.Cli
├─ docs
├─ tools
└─ .github/workflows
```

### ARNetDiscovery.Core

The core project owns network and evidence logic:

- adapter discovery;
- subnet/range parsing;
- local scan window calculation;
- ping probing;
- TCP port probing;
- ARP/MAC enrichment;
- device/protocol classification;
- target-list import model;
- diagnostics and scan snapshots.

### ARNetDiscovery.Wpf

The WPF project owns the desktop experience:

- adapter selector;
- scan/probe/import commands;
- virtualized device table;
- selected-device inspector;
- collapsible diagnostics panel;
- CSV export workflow;
- application/window state.

### ARNetDiscovery.Cli

The CLI project is a lightweight smoke-test entry point for scanner behavior. It is useful for basic development checks without launching the WPF UI.

---

## Progressive evidence pipeline

ARNet does not wait for the entire scan to finish before showing results.

Typical flow:

```text
Target scope
  -> quick reachability evidence
  -> row appears in table
  -> background protocol probing
  -> row is enriched with ports/protocols
  -> diagnostics and export use the same evidence model
```

This keeps the app responsive and makes the scan feel alive during FAT or commissioning checks.

---

## Scan scopes

### Local adapter scan

ARNet scans a safe local window based on the selected NIC.

If a NIC reports a broad mask such as `/8`, ARNet does not scan the entire logical subnet. It uses a capped practical scan window so the application remains responsive and safe for field use.

### Direct probe

Direct probe accepts a specific IP address or a bounded target range, for example:

```text
1.110.5.1
1.110.5.0/24
1.110.5.1-1.110.5.20
```

This is useful when the target device is routed but not inside the selected local scan window.

### Target-list scan

Target-list scan reads expected devices from Excel, CSV, or TXT and probes only those exact targets.

This is the preferred workflow for engineering list verification because it avoids scanning unnecessary ranges.

---

## Evidence model

ARNet separates different evidence types so the UI does not overstate the result.

Examples:

```text
Ping evidence      -> host replied to ICMP
Port evidence      -> a TCP connection succeeded
Protocol candidate -> a known protocol/service port responded
Expected target    -> row came from imported engineering data
No response        -> no quick ping or default port response
```

This distinction matters in real networks. A device may block ping while still exposing an IEC 61850 or OPC UA port.

---

## Device classification

Classification is evidence-based and conservative.

Examples:

| Evidence | Classification direction |
|---|---|
| Port 102 | IEC 61850 / IED / relay candidate |
| Port 2404 | IEC 104 / RTU or gateway candidate |
| Port 502 | Modbus TCP device candidate |
| Port 20000 | DNP3 device candidate |
| Port 4840 | OPC UA / SCADA / workstation/server candidate |
| Web only | web-managed device candidate |
| Ping only | reachable host with no default protocol evidence |

Imported expected type is preserved unless stronger protocol evidence is found. A relay from an engineering list should not be reclassified as a workstation just because only ping evidence was detected.

---

## UI model

The UI is intentionally table-first.

```text
Central device table       main working area
Inspector panel            selected-device evidence, collapsible
Diagnostics drawer         scan messages and handled exceptions, collapsible
Left rail                  quick access to target list, evidence, diagnostics, settings
```

This keeps the application usable with large imported target lists.

---

## Performance strategy

ARNet uses:

- async scan operations;
- guarded concurrency;
- short default timeouts;
- cancellation support;
- batch UI updates;
- WPF virtualization for the device table;
- diagnostics instead of unhandled UI crashes.

The goal is not to be the most aggressive scanner. The goal is to be fast enough for field work while staying predictable on engineering LANs.

---

## Release model

The repository includes two GitHub Actions workflows:

```text
build.yml     validates main branch and uploads a Windows artifact
release.yml   packages a portable Windows ZIP when a v* tag is pushed
```

A release contains:

```text
ARNetDiscovery-<version>-win-x64-portable.zip
ARNetDiscovery-<version>-win-x64-portable.zip.sha256
```

---

## Design constraints

ARNet should remain:

- lightweight;
- install-free when using the portable build;
- responsive with large target lists;
- clear about evidence quality;
- conservative with default network probing;
- easy to contribute to under Apache-2.0.

