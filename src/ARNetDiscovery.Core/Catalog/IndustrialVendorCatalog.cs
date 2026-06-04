using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Catalog;

public static class IndustrialVendorCatalog
{
    private static readonly (string Keyword, DeviceKind Kind, string Vendor)[] VendorHints =
    {
        ("siemens", DeviceKind.ProtectionRelay, "Siemens"),
        ("siprotec", DeviceKind.ProtectionRelay, "Siemens SIPROTEC"),
        ("sicam", DeviceKind.Gateway, "Siemens SICAM"),
        ("ruggedcom", DeviceKind.ManagedSwitch, "Siemens RUGGEDCOM"),
        ("moxa", DeviceKind.SerialServer, "Moxa"),
        ("schneider", DeviceKind.ProtectionRelay, "Schneider Electric"),
        ("sepam", DeviceKind.ProtectionRelay, "Schneider Electric"),
        ("easergy", DeviceKind.ProtectionRelay, "Schneider Electric"),
        ("abb", DeviceKind.ProtectionRelay, "ABB"),
        ("hitachi", DeviceKind.ProtectionRelay, "Hitachi Energy"),
        ("sel", DeviceKind.ProtectionRelay, "SEL"),
        ("ge", DeviceKind.ProtectionRelay, "GE / Grid Solutions"),
        ("alstom", DeviceKind.ProtectionRelay, "Alstom / GE Grid"),
        ("phoenix", DeviceKind.ManagedSwitch, "Phoenix Contact"),
        ("hirschmann", DeviceKind.ManagedSwitch, "Hirschmann"),
        ("cisco", DeviceKind.ManagedSwitch, "Cisco"),
        ("juniper", DeviceKind.ManagedSwitch, "Juniper"),
        ("3com", DeviceKind.ManagedSwitch, "3Com"),
        ("lenovo", DeviceKind.ServerOrWorkstation, "Lenovo"),
        ("vmware", DeviceKind.ServerOrWorkstation, "VMware")
    };

    public static (DeviceKind? Kind, string? Vendor) GuessFromText(params string?[] values)
    {
        var combined = string.Join(" ", values.Where(v => !string.IsNullOrWhiteSpace(v))).ToLowerInvariant();
        foreach (var hint in VendorHints)
        {
            if (combined.Contains(hint.Keyword, StringComparison.OrdinalIgnoreCase))
                return (hint.Kind, hint.Vendor);
        }

        return (null, null);
    }
}
