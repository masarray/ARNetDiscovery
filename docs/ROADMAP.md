# Roadmap

ARNet Discovery is developed as a practical Windows engineering tool for substation LAN visibility, target-list verification, and protocol evidence collection.

## Current focus

- Keep the table-first UX fast and readable.
- Keep scans conservative for engineering networks.
- Improve evidence quality without making the app heavy.
- Keep release packages easy to download and run.

## Planned improvements

### Protocol evidence

- IEC 61850 COTP/MMS handshake evidence beyond basic port visibility.
- OPC UA HEL/ACK evidence.
- Optional Modbus device-identification request.
- HTTP/HTTPS title and header summary.
- Clearer distinction between open-port evidence and confirmed protocol behavior.

### Target-list workflow

- Column preview before import.
- Better duplicate-IP handling.
- Target-list profile save/load.
- Session export with expected-versus-actual summary.

### Reporting

- HTML evidence report.
- PDF evidence report.
- Summary counts for expected, online, ping-only, port-open, and no-response rows.
- Release-ready report template for FAT and commissioning attachments.

### Quality and maintainability

- Tests for subnet parsing, target import, device classification, and snapshot buffering.
- Smaller view models and clearer UI resource structure.
- Better accessibility and keyboard navigation.
- More deterministic diagnostics for network exceptions and timeouts.
