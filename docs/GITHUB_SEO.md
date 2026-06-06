# GitHub discovery metadata

This file provides recommended public repository metadata for ARNet Discovery.

## Repository description

```text
Portable Windows LAN discovery and relay IP scanner for substation automation, FAT, commissioning, and protocol evidence checks.
```

## Repository website

```text
https://masarray.github.io/ARNetDiscovery/
```

## Recommended GitHub topics

```text
substation-automation
network-discovery
lan-scanner
relay-testing
iec61850
iec60870-5-104
modbus-tcp
dnp3
opcua
scada
industrial-automation
wpf
dotnet
windows
fat-testing
commissioning
protection-relay
engineering-tools
```

## Apply with GitHub CLI

Preview first:

```powershell
PowerShell -ExecutionPolicy Bypass -File .\tools\setup-github-seo.ps1 -WhatIf
```

Apply:

```powershell
PowerShell -ExecutionPolicy Bypass -File .\tools\setup-github-seo.ps1
```

The script supports:

```text
Owner
Repo
HomepageUrl
Description
Topics
WhatIf
```

It validates that GitHub CLI is installed and authenticated before applying changes.

## Landing page SEO

The GitHub Pages landing page in `docs/index.html` includes:

- descriptive title and meta description;
- canonical URL;
- Open Graph metadata;
- Twitter Card metadata;
- `SoftwareApplication` structured data;
- FAQ structured data;
- human-readable sections for benefits, use cases, workflow, protocol evidence, FAQ, license, and downloads;
- `robots.txt`, `sitemap.xml`, `.nojekyll`, and `site.webmanifest`.
