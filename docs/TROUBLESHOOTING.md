# Troubleshooting

This guide explains common ARNet Discovery results and what they usually mean in engineering networks.

## The expected device shows No response

Common causes:

- wrong laptop network adapter selected;
- VLAN mismatch;
- no route to the target segment;
- cable or switch port issue;
- device power is off;
- wrong IP address in the target list;
- firewall blocks ping and checked protocol ports;
- relay or gateway service is disabled.

Recommended checks:

1. Confirm the selected adapter in ARNet Discovery.
2. Confirm the laptop IP, mask, and gateway.
3. Probe the exact IP address.
4. Check the switch port, VLAN, route, and device power.
5. Compare the imported target list against the latest project network document.

## Ping only

`Ping only` means the host replies to ICMP, but ARNet did not find default protocol evidence on the checked ports.

This can happen when:

- the device is a workstation, printer, or network appliance;
- the expected service is disabled;
- a firewall blocks the checked port;
- the device uses a non-default port;
- the protocol is not part of the current evidence profile.

## Port open but no ping

Some devices block ICMP but still expose TCP services. Treat this as reachable evidence and inspect the open ports shown in the row or inspector.

## Import file is not detected correctly

Check that the file contains readable IPv4 addresses. Excel/CSV files work best when the IP column uses names such as:

```text
IP ADDRESS
IP
ADDRESS
NAMA IED
PANEL
NO DEVICE
REMARK
```

TXT files can contain plain text as long as IPv4 addresses are present.

## The scan feels slow

Large routed ranges can take time. For FAT and commissioning work, prefer **Import Excel + Scan List** so ARNet checks the exact expected device IPs instead of scanning unnecessary addresses.

## The app does not start

Try these checks:

1. Extract the ZIP before running the app.
2. Run `ARNetDiscovery.exe` from the extracted folder.
3. Confirm the release ZIP was downloaded completely.
4. If Windows blocks the file because it was downloaded from the internet, right-click the ZIP or EXE, open **Properties**, and choose **Unblock** if available.
5. Download the latest release again if the file looks incomplete.
