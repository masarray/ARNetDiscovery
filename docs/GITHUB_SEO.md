# GitHub discovery metadata

This file describes the public repository metadata recommended for ARNet Discovery.
It is intended for maintainers who publish the repository and want the project to be easier to find on GitHub, GitHub Topics, and search engines.

## Repository description

Use this as the GitHub repository description:

```text
Portable Windows LAN discovery and relay IP scanner for substation automation, FAT, commissioning, and protocol evidence checks.
```

## Repository website

Use this as the GitHub repository website URL:

```text
https://masarray.github.io/ARNetDiscovery/
```

## Recommended GitHub topics

GitHub allows repository topics to help people find and contribute to projects. Use lowercase topic names with hyphens.

Recommended topics:

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

## Optional GitHub CLI setup

From the repository root, after authenticating with GitHub CLI:

```powershell
gh repo edit masarray/ARNetDiscovery `
  --description "Portable Windows LAN discovery and relay IP scanner for substation automation, FAT, commissioning, and protocol evidence checks." `
  --homepage "https://masarray.github.io/ARNetDiscovery/" `
  --add-topic substation-automation `
  --add-topic network-discovery `
  --add-topic lan-scanner `
  --add-topic relay-testing `
  --add-topic iec61850 `
  --add-topic iec60870-5-104 `
  --add-topic modbus-tcp `
  --add-topic dnp3 `
  --add-topic opcua `
  --add-topic scada `
  --add-topic industrial-automation `
  --add-topic wpf `
  --add-topic dotnet `
  --add-topic windows `
  --add-topic fat-testing `
  --add-topic commissioning `
  --add-topic protection-relay `
  --add-topic engineering-tools
```

If a topic already exists, GitHub CLI may report that no change was needed.

## Landing page SEO checklist

The GitHub Pages landing page in `docs/index.html` includes:

- descriptive page title;
- meta description;
- canonical URL;
- Open Graph metadata;
- Twitter card metadata;
- structured data using `SoftwareApplication` JSON-LD;
- human-readable sections for use cases, supported protocol evidence, and FAQ;
- `robots.txt` and `sitemap.xml` under `docs/`.

Keep the landing page user-facing. Do not write internal implementation notes, private roadmap assumptions, or maintainer-only commentary on the public landing page.
