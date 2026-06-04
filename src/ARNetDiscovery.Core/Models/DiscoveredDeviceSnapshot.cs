using System.Net;

namespace ARNetDiscovery.Core.Models;

public sealed record DiscoveredDeviceSnapshot
{
    public required IPAddress IpAddress { get; init; }
    public string Ip => IpAddress.ToString();
    public string? HostName { get; init; }
    public string? MacAddress { get; init; }
    public string? Vendor { get; init; }
    public DeviceStatus Status { get; init; } = DeviceStatus.Unknown;
    public DeviceKind Kind { get; init; } = DeviceKind.Unknown;
    public string KindLabel => Kind switch
    {
        DeviceKind.ProtectionRelay => "Protection Relay",
        DeviceKind.BayController => "Bay Controller",
        DeviceKind.Gateway => "Gateway / RTU",
        DeviceKind.ManagedSwitch => "Managed Switch",
        DeviceKind.PlcOrController => "PLC / Controller",
        DeviceKind.Meter => "Meter",
        DeviceKind.SerialServer => "Serial Server",
        DeviceKind.EngineeringLaptop => "Engineering Laptop",
        DeviceKind.ServerOrWorkstation => "Server / Workstation",
        DeviceKind.WebManagedDevice => "Web Managed Device",
        _ => "Unknown Device"
    };

    public int? LatencyMs { get; init; }
    public int? TcpProbeMs { get; init; }
    public bool IsProbeInProgress { get; init; }
    public string ProbeStage { get; init; } = string.Empty;
    public IReadOnlyList<OpenPortResult> OpenPorts { get; init; } = Array.Empty<OpenPortResult>();
    public IReadOnlyList<ProtocolTag> ProtocolTags { get; init; } = Array.Empty<ProtocolTag>();
    public DateTimeOffset FirstSeen { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset LastSeen { get; init; } = DateTimeOffset.Now;
    public string Evidence { get; init; } = string.Empty;

    // Expected-target metadata from imported engineering inventory.
    public bool IsExpectedTarget { get; init; }
    public string? ExpectedDeviceName { get; init; }
    public string? ExpectedType { get; init; }
    public string? BayName { get; init; }
    public string? BayType { get; init; }
    public string? Panel { get; init; }
    public string? DeviceNo { get; init; }
    public string? Remark { get; init; }
    public string? TargetSource { get; init; }
    public int? SourceRow { get; init; }

    public bool HasOpenPorts => OpenPorts.Any(p => p.IsOpen);
    public string OpenPortsLabel => HasOpenPorts ? string.Join(", ", OpenPorts.Where(p => p.IsOpen).Select(p => p.Port)) : "No known port";
    public string ProtocolSummary => ProtocolTags.Count > 0 ? string.Join(" / ", ProtocolTags.Select(t => t.Label)) : IsProbeInProgress ? "Checking protocols..." : "No protocol detected";
    public string LatencyLabel => LatencyMs is int ms ? $"{ms} ms" : TcpProbeMs is int tcp ? $"TCP {tcp} ms" : "—";
    public string HostTitle => !string.IsNullOrWhiteSpace(HostName) ? HostName! : !string.IsNullOrWhiteSpace(ExpectedDeviceName) ? ExpectedDeviceName! : "Unknown host";
    public string ExpectedContextLabel
    {
        get
        {
            var parts = new[] { Panel, ExpectedType, BayName }.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
            return parts.Length == 0 ? KindLabel : string.Join(" · ", parts);
        }
    }
    public string VendorDisplay => !string.IsNullOrWhiteSpace(Vendor) ? Vendor! : IsExpectedTarget ? "Expected" : "—";
    public string DiscoveryStateLabel => IsProbeInProgress ? "Checking" : Status.ToString();
}
