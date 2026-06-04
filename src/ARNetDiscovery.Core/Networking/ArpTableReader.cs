using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using ARNetDiscovery.Core.Diagnostics;

namespace ARNetDiscovery.Core.Networking;

public sealed class ArpTableReader
{
    private static readonly Regex ArpLineRegex = new(
        @"(?<ip>(?:\d{1,3}\.){3}\d{1,3})\s+(?<mac>(?:[0-9a-fA-F]{2}[-:]){5}[0-9a-fA-F]{2})\s+\w+",
        RegexOptions.Compiled);

    private readonly IDiagnosticSink _diagnostics;

    public ArpTableReader(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public async Task<IReadOnlyDictionary<string, string>> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in ArpLineRegex.Matches(output))
            {
                var ip = match.Groups["ip"].Value;
                var mac = NormalizeMac(match.Groups["mac"].Value);
                if (IPAddress.TryParse(ip, out _))
                    dict[ip] = mac;
            }

            return dict;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(ArpTableReader), "Could not read ARP table. MAC/vendor enrichment will be partial.", ex));
            return new Dictionary<string, string>();
        }
    }

    public static string NormalizeMac(string raw)
    {
        var hex = new string(raw.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        if (hex.Length != 12) return raw.ToUpperInvariant();
        return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
    }
}
