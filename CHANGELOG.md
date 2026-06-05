# Changelog

All notable product changes are documented here in user-facing language.

## Current release

### Added

- Portable Windows release packaging through GitHub Actions.
- GitHub Actions badges for build, release, latest release, platform, .NET, and license.
- GitHub Pages landing page under `docs/index.html`.
- User guide, architecture notes, product roadmap, and contribution guide.

### Product capabilities

- Local adapter scan for quick LAN discovery.
- Direct IP/range probe for routed targets.
- Excel/CSV/TXT target-list import and exact target scanning.
- Progressive table updates: hosts appear as evidence is found, while protocol checks continue in the background.
- Collapsible inspector panel for selected-device evidence.
- Collapsible diagnostics panel for scan messages and handled network failures.
- CSV export for FAT, commissioning, and troubleshooting records.

### Notes for users

- `No response` means no quick ping or default protocol evidence was found from the selected adapter. Check route, VLAN, cable, panel power, IP address, firewall, and device settings.
- `Ping only` means the host replied to ICMP but no default industrial protocol evidence was detected.
- `Port open` means ping may be blocked but one or more checked ports responded.
- Protocol evidence is a strong clue, not a replacement for formal project verification.

