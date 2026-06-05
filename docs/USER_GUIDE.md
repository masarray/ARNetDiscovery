# ARNet Discovery user guide

ARNet Discovery is designed for field engineers who need to check which devices are visible from a laptop network adapter and whether common substation protocol evidence is active.

The normal workflow is:

```text
Select adapter -> choose scan mode -> watch results update -> inspect evidence -> export if needed
```

---

## 1. Select the correct network adapter

Use the adapter selector at the top of the app.

Choose the NIC connected to the panel LAN, relay switch, commissioning network, or test bench.

Preferred adapter examples:

```text
Ethernet / LAN / RJ45
USB Ethernet adapter
Docking station Ethernet
```

Wi-Fi can be used for office/lab testing, but for panel and relay checks, wired Ethernet is usually more reliable.

---

## 2. Scan modes

### Scan Local

Use **Scan Local** when you want to discover devices near the selected adapter.

ARNet uses a safe local scan window. If an adapter reports a broad subnet mask such as `/8`, ARNet does not scan millions of addresses. It scans a controlled local window so the app stays responsive.

Use this for:

- quick panel LAN checks;
- laptop-to-switch discovery;
- relay bay network checks;
- troubleshooting wrong adapter selection.

### Probe

Use **Probe** when you already know the IP address or range.

Supported examples:

```text
1.110.5.1
1.110.5.0/24
1.110.5.1-1.110.5.20
```

Use this for routed segments or devices outside the current local scan window.

### Import Excel + Scan List

Use **Import Excel** when you already have a project IP list.

After import, expected devices appear immediately. **Scan List** then checks the exact IP addresses from the file.

This mode is recommended for FAT and commissioning because it verifies the engineering target list directly instead of scanning unnecessary addresses.

---

## 3. Supported target-list formats

ARNet supports:

```text
.xlsx
.csv
.txt
```

For Excel and CSV, the most important column is the target IP address. ARNet recognizes common names such as:

```text
IP ADDRESS
IP
ADDRESS
NAMA IED
PANEL
NAMA BAY
JENIS PERALATAN
REMARK
```

For TXT files, ARNet extracts IPv4 addresses from the text.

---

## 4. Understanding the result table

| Column | Meaning |
|---|---|
| Device | Detected or expected device name |
| IP Address | Target/discovered IP |
| Expected | Shows whether the row comes from an imported target list |
| Protocols | Visible protocol or management-port evidence |
| Ping | Ping latency when ICMP replies are available |
| Status | Summary of current reachability/evidence |

Rows update progressively. A device can appear as ping-only first, then later show IEC 61850, OPC UA, web UI, or other evidence when background probing completes.

---

## 5. Understanding statuses

| Status | Meaning | Suggested action |
|---|---|---|
| Online | Device responded or useful evidence was found | Inspect ports/protocols and export result if needed |
| Ping only | ICMP responded, but no default protocol evidence was found | Check expected service/port configuration |
| Port open | ICMP did not reply, but a checked port responded | Treat as reachable; ping may be blocked |
| No response | No quick ping or default port response | Check cable, VLAN, route, IP, power, and firewall |
| Expected | Imported target that is part of the project list | Compare expected vs actual evidence |

---

## 6. Inspector panel

The inspector panel shows details for the selected row:

- expected target information from Excel/CSV;
- IP address, MAC address, and vendor when available;
- ping/latency evidence;
- open port evidence;
- protocol fingerprint summary;
- practical diagnosis.

The panel is collapsible so the device table can remain the main work area.

---

## 7. Diagnostics panel

Diagnostics explains what the scanner did and what went wrong, without crashing the app.

Typical diagnostics:

```text
Scan completed
Ping failed for 1.110.5.17
Port probe timeout
Adapter selected
Imported target list loaded
```

Warnings are not always failures. In field networks, ping timeouts and blocked ports are normal evidence to interpret.

---

## 8. Exporting results

Use **Export** to save the current result table as CSV.

The export is useful for:

- FAT evidence;
- punch-list follow-up;
- documenting no-response devices;
- sharing device status with project/network teams.

---

## 9. Practical field tips

- Always confirm the selected NIC before scanning.
- Prefer wired Ethernet for relay and panel checks.
- Use target-list scan when you have an engineering IP list.
- Use direct probe for a device outside the current local scan window.
- Do not assume `No ping` means offline; protocol ports may still respond.
- Do not assume a single open port fully confirms device identity; treat it as evidence.
- Export results after each meaningful scan session.

