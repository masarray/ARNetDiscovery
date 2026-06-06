# ARNet Discovery User Manual

ARNet Discovery is a Windows desktop app for engineers who need a fast, readable view of device reachability and lightweight protocol evidence on substation and industrial automation networks.

## Normal workflow

```text
Select adapter -> choose scan mode -> watch results update -> inspect evidence -> export if needed
```

## Select the correct network adapter

Use the adapter selector at the top of the app. Choose the NIC connected to the panel LAN, relay switch, commissioning network, or test bench. Wired Ethernet is recommended for panel and relay checks.

## Scan Local

Use **Scan Local** when you want to discover nearby devices from the selected adapter. ARNet Discovery uses a safe local scan window so broad subnet masks do not trigger huge scans.

Good use cases:

- quick panel LAN visibility checks;
- relay bay network checks;
- laptop-to-switch discovery;
- checking whether the correct adapter is selected.

## Probe

Use **Probe** when you already know the IP address or range. Supported examples:

```text
1.110.5.1
1.110.5.0/24
1.110.5.1-1.110.5.20
```

Use this mode for routed segments or devices outside the current local scan window.

## Import Excel + Scan List

Use **Import Excel** when you already have a project IP list. Expected devices appear immediately after import. **Scan List** checks the exact IP addresses from the file.

Supported file types:

```text
.xlsx
.csv
.txt
```

Common column names are recognized, including `IP ADDRESS`, `IP`, `ADDRESS`, `NAMA IED`, `PANEL`, `NAMA BAY`, `JENIS PERALATAN`, and `REMARK`. For TXT files, ARNet Discovery extracts IPv4 addresses from the text.

## Understanding the table

| Column | Meaning |
|---|---|
| Device | Detected or expected device name. |
| IP Address | Target or discovered IP address. |
| Expected | Indicates whether the row comes from an imported target list. |
| Protocols | Visible protocol or management-port evidence. |
| Ping | Ping latency when ICMP replies are available. |
| Status | Summary of reachability and evidence. |

Rows update progressively. A device may appear as ping-only first, then later show IEC 61850, OPC UA, web UI, or other evidence when background probing completes.

## Understanding statuses

| Status | Meaning | Suggested action |
|---|---|---|
| Online | Device responded or useful evidence was found. | Inspect ports/protocols and export the result when needed. |
| Ping only | ICMP responded, but no default protocol evidence was found. | Check expected service or firewall configuration. |
| Port open | ICMP did not reply, but a checked port responded. | Treat as reachable evidence; ping may be blocked. |
| No response | No quick ping or default port response. | Check cable, VLAN, route, IP, power, and firewall. |
| Expected | Imported target that is part of the project list. | Compare expected versus actual evidence. |

## Inspector panel

The inspector shows details for the selected row: imported target information, IP address, MAC/vendor when available, ping evidence, open ports, protocol summary, and practical diagnosis.

## Diagnostics panel

Diagnostics explains what happened during scanning: completed scans, ping failures, port timeouts, adapter selection, and target-list import messages. Warnings are common in field networks and should be interpreted with the result table.

## Exporting results

Use **Export** to save the current result table as CSV. The export is useful for FAT evidence, punch-list follow-up, no-response device documentation, and handover to project or network teams.

## Limitations

- Open ports and protocol labels are evidence, not final device identity proof.
- Firewalls, VLAN rules, routing, and blocked ICMP can affect results.
- Formal commissioning, cyber-security review, and protection testing remain the final authority for project acceptance.
- Use the app only on networks where testing is authorized.
