# Validation matrix

This matrix describes what ARNet Discovery checks by default and how to interpret the result.

| Area | Evidence | User interpretation |
|---|---|---|
| Adapter selection | NIC name, IP address, subnet, gateway | Confirms which laptop interface is used for discovery |
| Local scan | Safe local address window | Finds nearby devices without sweeping broad masks such as `/8` |
| Direct probe | IP, CIDR, or bounded range | Checks routed targets or known device addresses |
| Target-list scan | Imported Excel/CSV/TXT rows | Verifies exact expected engineering devices |
| Ping | ICMP reply and latency | Shows basic reachability when ICMP is allowed |
| TCP evidence | Open default ports | Shows service evidence even when ICMP is blocked |
| Protocol labels | IEC 61850, IEC 104, Modbus TCP, DNP3, OPC UA, HTTP/HTTPS, SSH/Telnet | Provides engineering evidence for follow-up validation |
| Device classification | Relay, BCU, gateway, switch, server/workstation, meter/PLC, printer | Helps prioritize review, not a final asset identity certificate |
| Export | CSV result | Supports FAT, commissioning, troubleshooting, and punch-list records |

## Evidence versus confirmation

ARNet Discovery is intentionally clear about evidence. A visible TCP port is useful evidence, but it does not replace formal device configuration checks, protocol conformance testing, cyber-security review, or commissioning procedures.

## Recommended field validation flow

1. Import the official target list.
2. Scan the exact target list.
3. Review `No response`, `Ping only`, and `Port open` rows.
4. Probe critical devices directly when needed.
5. Export CSV after each meaningful scan session.
6. Attach the result to FAT, SAT, commissioning, or troubleshooting records as supporting evidence.
