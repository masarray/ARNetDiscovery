# Architecture

ARNet Discovery separates scanning logic, device evidence, target-list import, and WPF presentation so the app can stay responsive during network checks.

## Solution layout

```text
src/ARNetDiscovery.Core   scanning, adapter discovery, target import, models, diagnostics
src/ARNetDiscovery.Wpf    Windows desktop UI
src/ARNetDiscovery.Cli    auxiliary project for development and automation scenarios
```

The Windows desktop app is the primary application.

## Main flow

```text
Adapter / target source
        ↓
Scan engine
        ↓
Ping + TCP evidence checks
        ↓
Device classifier
        ↓
Snapshot buffer
        ↓
WPF table, inspector, diagnostics, export
```

## Evidence model

ARNet Discovery treats scan results as evidence:

- ping reply means ICMP reachability;
- open TCP port means service evidence;
- protocol label means the evidence matches a known industrial or management port;
- imported target metadata means the row came from an engineering target list.

This approach keeps the app honest: it helps users see what is visible from the laptop, without overstating that every device identity has been formally confirmed.

## UI model

The UI is table-first:

- the device grid is the primary work area;
- inspector is collapsible for selected-device details;
- diagnostics is collapsible for scan messages;
- left navigation provides quick access to target-list, evidence, diagnostics, and settings actions.

## Packaging model

Release packaging is handled by PowerShell and GitHub Actions:

```text
scripts/publish-windows-portable.ps1
scripts/verify-release-package.ps1
.github/workflows/release-package.yml
```

The portable ZIP contains the app, quick-start notes, license, notice files, and checksum information.
