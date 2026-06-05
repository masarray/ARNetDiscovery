# ARNet Discovery product roadmap

This roadmap describes planned improvements for ARNet Discovery as a public engineering tool. The priority is reliability, clear evidence, lightweight operation, and field usability.

---

## Current product baseline

ARNet Discovery currently provides:

- Windows WPF desktop application;
- local LAN discovery;
- direct IP/range probing;
- Excel/CSV/TXT target-list scan;
- progressive result updates;
- collapsible device inspector;
- collapsible diagnostics panel;
- CSV export;
- portable Windows release build through GitHub Actions.

---

## Near-term improvements

### 1. More protocol evidence validators

The default scan remains lightweight, but selected-device inspection can become deeper.

Planned evidence improvements:

| Protocol | Planned improvement |
|---|---|
| IEC 61850 | safer ISO-on-TCP/COTP evidence check |
| OPC UA | HEL/ACK endpoint confirmation |
| Modbus TCP | optional safe device identification |
| HTTP/HTTPS | title/header collection with timeout |
| IEC 104 | conservative optional validation flow |

These checks should be explicit, timeout guarded, and safe for engineering networks.

### 2. Project profile files

Planned `.arnetprofile.json` support:

```text
project name
imported target list
selected scan profile
custom protocol ports
last scan result snapshot
operator notes
```

This will allow users to reopen a project and repeat a known target-list scan later.

### 3. Evidence reports

CSV is useful, but commissioning teams often need a clearer report.

Planned report outputs:

- HTML evidence report;
- expected vs actual summary;
- no-response list grouped by bay/panel/subnet;
- protocol evidence summary;
- later PDF export after the evidence model is stable.

### 4. Core tests

The first automated tests should cover deterministic logic:

```text
subnet/range parsing
target-list import
expected-vs-actual classification
own IP exclusion
protocol evidence classification
snapshot update behavior
```

---

## Contribution priorities

The most useful contributions are:

1. safer protocol evidence checks;
2. better target-list import mapping;
3. field-friendly reporting;
4. performance improvements for large target lists;
5. documentation with realistic commissioning examples;
6. tests for scanner and importer logic.

---

## Product principles

ARNet Discovery should remain:

- lightweight;
- responsive during scans;
- clear about evidence vs confirmation;
- conservative on industrial networks;
- easy to run as a portable Windows application;
- useful without requiring a database or server.

