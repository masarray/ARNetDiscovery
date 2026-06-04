using System.Net;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Targets;

public sealed record TargetDeviceRecord
{
    public required IPAddress IpAddress { get; init; }
    public string Ip => IpAddress.ToString();
    public string DeviceName { get; init; } = string.Empty;
    public string BayName { get; init; } = string.Empty;
    public string BayType { get; init; } = string.Empty;
    public string Panel { get; init; } = string.Empty;
    public string ExpectedType { get; init; } = string.Empty;
    public string DeviceNo { get; init; } = string.Empty;
    public string Remark { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public int SourceRow { get; init; }

    public DeviceKind ExpectedKind => ExpectedTypeClassifier.Guess(ExpectedType, DeviceName, Panel);

    public DiscoveredDeviceSnapshot ToPendingSnapshot()
    {
        var now = DateTimeOffset.Now;
        var title = string.IsNullOrWhiteSpace(DeviceName) ? Ip : DeviceName.Trim();
        return new DiscoveredDeviceSnapshot
        {
            IpAddress = IpAddress,
            HostName = title,
            Status = DeviceStatus.Pending,
            Kind = ExpectedKind,
            IsExpectedTarget = true,
            ExpectedDeviceName = title,
            ExpectedType = ExpectedType,
            BayName = BayName,
            BayType = BayType,
            Panel = Panel,
            DeviceNo = DeviceNo,
            Remark = Remark,
            TargetSource = Source,
            SourceRow = SourceRow,
            FirstSeen = now,
            LastSeen = now,
            IsProbeInProgress = false,
            ProbeStage = "Expected target loaded",
            Evidence = "Imported target. Waiting for scan evidence."
        };
    }
}

public sealed record TargetImportResult
{
    public required string Title { get; init; }
    public required string SourcePath { get; init; }
    public IReadOnlyList<TargetDeviceRecord> Targets { get; init; } = Array.Empty<TargetDeviceRecord>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public int TargetCount => Targets.Count;
    public int UniqueTargetCount => Targets.Select(t => t.Ip).Distinct(StringComparer.Ordinal).Count();
    public IReadOnlyList<string> SubnetGroups => Targets
        .Select(t => ToSlash24Group(t.IpAddress))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(s => s, StringComparer.Ordinal)
        .ToArray();

    private static string ToSlash24Group(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 ? $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24" : address.ToString();
    }
}

public static class ExpectedTypeClassifier
{
    public static DeviceKind Guess(string? expectedType, string? name, string? panel)
    {
        var text = $"{expectedType} {name} {panel}".ToUpperInvariant();

        if (ContainsAny(text, "SWITCH", "SWTC")) return DeviceKind.ManagedSwitch;
        if (ContainsAny(text, "SERVER", "HMI", "CONFIGURATOR", "WORKSTATION", "WINCC", "OPC", "SCADA", "PRINTER")) return DeviceKind.ServerOrWorkstation;
        if (ContainsAny(text, "GATEWAY", "GWAY", "ROUTER", "RTU")) return DeviceKind.Gateway;
        if (ContainsAny(text, "KWH", "METER")) return DeviceKind.Meter;
        if (ContainsAny(text, "BCU", "BAY CONTROL")) return DeviceKind.BayController;
        if (ContainsAny(text, "RELAY", "PROTECTION", "DIFFERENTIAL", "OVERCURRENT", "CURRENT", "PTOC", "PTDF", "PLDF", "PBDF", "PSEF")) return DeviceKind.ProtectionRelay;
        if (ContainsAny(text, "IED", "IO", "I/O", "AVR", "GPS", "NTP")) return DeviceKind.PlcOrController;

        return DeviceKind.Unknown;
    }

    private static bool ContainsAny(string text, params string[] tokens) => tokens.Any(text.Contains);
}
