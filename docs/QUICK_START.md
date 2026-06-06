# ARNet Discovery Quick Start

This guide helps you run ARNet Discovery quickly from the Windows portable package.

## 1. Download

Open the latest release page:

```text
https://github.com/masarray/ARNetDiscovery/releases/latest
```

Download:

```text
ARNetDiscovery-v<version>-win-x64-portable.zip
SHA256SUMS.txt
```

## 2. Extract and run

1. Extract the ZIP to a local folder.
2. Run `ARNetDiscovery.exe`.
3. Select the Ethernet adapter connected to the engineering LAN, panel switch, relay LAN, or test bench.

No installer, subscription, license key, or Visual Studio installation is required.

## 3. Choose a scan mode

| Mode | Use it when |
|---|---|
| Scan Local | You want to discover nearby devices from the selected adapter. |
| Probe | You know one IP address, CIDR block, or bounded IP range. |
| Import Excel + Scan List | You want to verify an official project target list. |

## 4. Read the result

| Result | Meaning |
|---|---|
| Online | Ping and/or useful protocol evidence was found. |
| Ping only | ICMP replied, but default protocol evidence was not found. |
| Port open | ICMP may be blocked, but one or more checked ports responded. |
| No response | No quick ping or default protocol evidence was found. |
| Expected | The row came from an imported target list. |

## 5. Export

Use **Export** to save CSV results for FAT, SAT, commissioning, troubleshooting, or follow-up records.

## Safety note

Use ARNet Discovery only on networks where you are authorized to test. The app provides engineering evidence and does not replace formal commissioning, protection testing, or cyber-security procedures.
