using ARNetDiscovery.Core.Diagnostics;

namespace ARNetDiscovery.Core.Catalog;

public sealed class OuiVendorLookup
{
    private readonly Dictionary<string, string> _vendors = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDiagnosticSink _diagnostics;

    public OuiVendorLookup(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public int Count => _vendors.Count;

    public void LoadCsvIfExists(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _diagnostics.Publish(DiagnosticEntry.Info(nameof(OuiVendorLookup), $"OUI CSV not found: {path}"));
                return;
            }

            var loaded = 0;
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.StartsWith("prefix", StringComparison.OrdinalIgnoreCase)) continue;

                var parts = line.Split(',', 2);
                if (parts.Length < 2) continue;

                var prefix = NormalizePrefix(parts[0]);
                var vendor = parts[1].Trim().Trim('"');
                if (prefix.Length != 6 || string.IsNullOrWhiteSpace(vendor)) continue;

                _vendors[prefix] = vendor;
                loaded++;
            }

            _diagnostics.Publish(DiagnosticEntry.Info(nameof(OuiVendorLookup), $"Loaded {loaded:n0} OUI vendor entries."));
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(OuiVendorLookup), "Failed to load optional OUI CSV.", ex));
        }
    }

    public string? FindVendor(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress)) return null;
        var prefix = NormalizePrefix(macAddress);
        if (prefix.Length < 6) return null;
        return _vendors.TryGetValue(prefix[..6], out var vendor) ? vendor : null;
    }

    public static string NormalizePrefix(string raw)
    {
        return new string(raw.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
    }
}
