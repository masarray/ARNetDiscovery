# ARNet Discovery Architecture

## Product direction

ARNet Discovery is a lightweight substation/network discovery tool for Windows engineering laptops.

The current product direction is **clean table-first inventory + right-side evidence inspector**:

- Top: adapter selection, direct target probe, scan actions
- Center-left: virtualized discovered-device table
- Right: selected device evidence, open ports, protocol fingerprint
- Bottom: collapsible diagnostics drawer

The app intentionally avoids a heavy topology canvas in the default workflow. A table is faster, more readable, easier to virtualize, and better for real FAT/commissioning use.

## Progressive discovery pipeline

ARNet does not wait for the full scan to complete before showing results.

```text
Target feeder
  -> Fast ICMP ping pass
  -> publish first evidence immediately
  -> background priority TCP protocol probe
  -> update same row with open ports / protocol tags
  -> ARP/MAC/vendor enrichment
  -> final immutable snapshot for export
```

Expected user experience:

```text
< 1 s    reachable hosts start appearing
1-3 s    protocol tags start filling in
3-8 s    final enrichment and diagnostics complete
```

## Scan scope modes

### Local adapter scan

Uses the selected NIC and scans a safe local window. If the adapter reports a broad mask such as `/8`, ARNet does **not** scan the full logical subnet. It scans a capped local window instead.

### Direct probe / custom target

The target field accepts:

```text
1.110.5.1
1.110.5.0/24
1.110.5.1-1.110.5.20
```

This solves the common field case where the relay/server is reachable by routing but sits outside the selected NIC's safe local scan window.

## Runtime flow

```text
Adapter detection
  -> Target list generation
  -> Exclude own laptop IP
  -> Progressive scan session
  -> Ping evidence published immediately
  -> TCP protocol enrichment in background
  -> SnapshotBuffer upsert
  -> UI batch flush every 250 ms
  -> ARP/OUI enrichment
  -> CSV/export snapshot
```

## Performance and freeze prevention

Design rules:

1. Scanner workers never update WPF visual collections directly.
2. Results go to an in-memory snapshot buffer first.
3. The UI flushes pending devices through a `DispatcherTimer`.
4. The device table uses WPF virtualization/recycling.
5. Reverse DNS is disabled in the fast scan path.
6. Direct probe uses longer timeout; local scan uses short timeout.

Default local scan profile:

```text
Ping timeout        : 200 ms
TCP timeout         : 350 ms
Ping concurrency    : 96
TCP concurrency     : 96
UI flush interval   : 250 ms
Local scan cap      : 254 hosts
```

## Protocol classification

Default evidence ports:

```text
102     IEC 61850 MMS / ISO-on-TCP
2404    IEC 60870-5-104
502     Modbus TCP
20000   DNP3 TCP
4840    OPC UA / SCADA / WinCC / OPC server candidate
80/443  Web UI evidence
22/23   management access evidence
```

SNMP is normally UDP/161, so TCP/161 is not used as default high-confidence evidence.

## Snapshot buffer principle

The scanner writes results into `SnapshotBuffer<TKey,TValue>`. The UI receives progressive snapshots, while the engine keeps a stable current snapshot. This avoids a fragile live object graph and makes future PDF/evidence reporting easier.

## Exception handling principle

All expected probe-level exceptions are handled inside the engine and converted into diagnostic entries.

The WPF app also guards:

- `DispatcherUnhandledException`
- `AppDomain.CurrentDomain.UnhandledException`
- `TaskScheduler.UnobservedTaskException`

The intended behavior is clear: diagnostic visibility first, application crash last.

## OUI strategy

A full OUI database can be useful but should not be embedded blindly. OUI datasets may have redistribution terms. ARNet therefore provides:

- `OuiVendorLookup` loader
- user-supplied `oui-custom.csv`
- internal vendor keyword hints for device-kind classification

This keeps the repository clean for Apache-2.0.

## Future roadmap

### v2.1
- Save selected device as target profile
- Scan profile presets: Fast / Balanced / Deep
- Custom port editor

### v2.2
- Optional protocol handshake validators:
  - IEC 61850 COTP/MMS presence check
  - IEC 104 STARTDT safe check option
  - Modbus device identification option
  - OPC UA hello/endpoint read option

### v2.3
- PDF evidence report
- Imported IP list scan
- Saved project profiles

### v3
- Optional Avalonia UI using the same `ARNetDiscovery.Core`

## v2.1 Target List Scan

Imported target lists are treated as **expected inventory**, not as discovered-only results.

```text
Import Excel/CSV/TXT
  -> TargetDeviceRecord list
  -> Pending DiscoveredDeviceSnapshot rows rendered immediately
  -> exact-IP progressive scan
  -> responsive rows are enriched by ping/protocol evidence
  -> unresolved expected rows are marked NoResponse
```

This allows ARNet to scan devices outside the safe local window, for example when the laptop is `1.110.200.x` but relay IEDs are in `1.110.5.x`, `1.110.13.x`, or other routed plant segments.

Key behavior:

- The laptop's own active adapter IP is excluded from imported scans.
- Excel metadata such as bay, panel, expected equipment type, IED name, and remark is preserved in the right inspector and CSV export.
- Target rows stay visible even if the device never answers ping or protocol probes.
- The scanner still uses the progressive evidence pipeline: expected row first, ping evidence second, protocol enrichment third.
