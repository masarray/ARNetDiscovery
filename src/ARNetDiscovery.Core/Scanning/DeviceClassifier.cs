using ARNetDiscovery.Core.Catalog;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Scanning;

public sealed class DeviceClassifier
{
    public (DeviceKind Kind, IReadOnlyList<ProtocolTag> Tags, string Evidence) Classify(
        string? hostName,
        string? vendor,
        IReadOnlyList<OpenPortResult> ports,
        bool pingSuccess,
        int? latencyMs)
    {
        var open = ports.Where(p => p.IsOpen).Select(p => p.Port).ToHashSet();
        var tags = new List<ProtocolTag>();
        var evidence = new List<string>();
        var kind = DeviceKind.Unknown;

        if (open.Contains(102))
        {
            tags.Add(new ProtocolTag("IEC61850", "IEC 61850", "#18A7C7", 85));
            kind = DeviceKind.ProtectionRelay;
            evidence.Add("Port 102 open: IEC 61850 MMS / ISO-on-TCP candidate.");
        }

        if (open.Contains(2404))
        {
            tags.Add(new ProtocolTag("IEC104", "IEC 104", "#2F80ED", 85));
            if (kind == DeviceKind.Unknown) kind = DeviceKind.Gateway;
            evidence.Add("Port 2404 open: IEC 60870-5-104 endpoint candidate.");
        }

        if (open.Contains(502))
        {
            tags.Add(new ProtocolTag("MODBUS", "Modbus", "#8A63D2", 78));
            if (kind == DeviceKind.Unknown) kind = DeviceKind.PlcOrController;
            evidence.Add("Port 502 open: Modbus TCP candidate.");
        }

        if (open.Contains(20000))
        {
            tags.Add(new ProtocolTag("DNP3", "DNP3", "#D4741F", 78));
            if (kind == DeviceKind.Unknown) kind = DeviceKind.Gateway;
            evidence.Add("Port 20000 open: DNP3 TCP candidate.");
        }

        if (open.Contains(4840))
        {
            tags.Add(new ProtocolTag("OPCUA", "OPC UA", "#5874D8", 82));
            if (kind == DeviceKind.Unknown) kind = DeviceKind.ServerOrWorkstation;
            evidence.Add("Port 4840 open: OPC UA endpoint candidate. In substation/HMI networks this is often a SCADA, WinCC, engineering workstation, gateway, or OPC server.");
        }

        if (open.Contains(161))
        {
            tags.Add(new ProtocolTag("SNMP", "SNMP", "#27A35B", 55));
            if (kind == DeviceKind.Unknown && (open.Contains(22) || open.Contains(80) || open.Contains(443)))
                kind = DeviceKind.ManagedSwitch;
            evidence.Add("TCP/161 responded. Note: normal SNMP uses UDP/161, so this is low-confidence switch evidence until UDP/SNMP probe is implemented.");
        }

        if (open.Contains(80) || open.Contains(443) || open.Contains(8080))
        {
            tags.Add(new ProtocolTag("WEB", "Web UI", "#00A1A7", 65));
            if (kind == DeviceKind.Unknown) kind = DeviceKind.WebManagedDevice;
            evidence.Add("Web interface port detected.");
        }

        if (open.Contains(22))
        {
            tags.Add(new ProtocolTag("SSH", "SSH", "#3C6E71", 55));
            evidence.Add("SSH management port detected.");
        }

        if (open.Contains(23))
        {
            tags.Add(new ProtocolTag("TELNET", "Telnet", "#BC6C25", 45));
            evidence.Add("Legacy Telnet port detected.");
        }

        var hint = IndustrialVendorCatalog.GuessFromText(hostName, vendor);
        if (hint.Kind is not null)
        {
            if (kind == DeviceKind.Unknown || kind == DeviceKind.WebManagedDevice)
                kind = hint.Kind.Value;
            evidence.Add($"Vendor/name hint: {hint.Vendor}.");
        }

        if (kind == DeviceKind.Unknown && pingSuccess)
        {
            kind = DeviceKind.ServerOrWorkstation;
            evidence.Add("Host replied to ping, but no known industrial ports were detected.");
        }

        if (latencyMs > 100)
            evidence.Add("Latency above 100 ms: slow response for a local industrial LAN.");

        return (kind, tags, string.Join(" ", evidence));
    }

    public DeviceStatus ResolveStatus(bool pingSuccess, bool hasOpenPorts, int? latencyMs)
    {
        if (pingSuccess && latencyMs > 100) return DeviceStatus.Slow;
        if (pingSuccess && hasOpenPorts) return DeviceStatus.Online;
        if (pingSuccess) return DeviceStatus.PingOnly;
        if (hasOpenPorts) return DeviceStatus.PortOpenOnly;
        return DeviceStatus.Offline;
    }
}
