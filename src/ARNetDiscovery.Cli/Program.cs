using ARNetDiscovery.Core.Catalog;
using ARNetDiscovery.Core.Diagnostics;
using ARNetDiscovery.Core.Models;
using ARNetDiscovery.Core.Networking;
using ARNetDiscovery.Core.Scanning;

var diagnostics = new BufferedDiagnosticSink();
diagnostics.EntryPublished += (_, entry) => Console.Error.WriteLine($"[{entry.Timestamp:HH:mm:ss}] {entry.Severity} {entry.Source}: {entry.Message}");

var adapters = new NetworkAdapterProvider(diagnostics).GetActiveIpv4Adapters();
if (adapters.Count == 0)
{
    Console.WriteLine("No active IPv4 adapters found.");
    return 2;
}

var selected = adapters[0];
Console.WriteLine($"ARNet Discovery CLI");
Console.WriteLine($"Adapter : {selected.DisplayName}");
var settings = new ScanSettings();
Console.WriteLine($"Range   : {SubnetCalculator.GetSmartRangeLabel(selected.Address, selected.SubnetMask, settings)}");
Console.WriteLine($"Health  : {SubnetCalculator.GetAdapterRiskNote(selected.Address, selected.SubnetMask, selected.GatewayAddress)}");
Console.WriteLine();

var lookup = new OuiVendorLookup(diagnostics);
lookup.LoadCsvIfExists(Path.Combine(AppContext.BaseDirectory, "oui-custom.csv"));

var engine = new LanDiscoveryEngine(diagnostics, lookup);
var progress = new Progress<ScanProgressInfo>(p =>
{
    Console.Write($"\rScanning {p.CheckedHosts}/{p.TotalHosts}  discovered={p.DiscoveredHosts}       ");
});

var results = await engine.ScanAsync(selected, settings, progress, device =>
{
    Console.WriteLine($"\nFOUND {device.Ip,-15} {device.KindLabel,-22} {device.OpenPortsLabel,-20} {device.ProtocolSummary}");
}, CancellationToken.None);

Console.WriteLine();
Console.WriteLine("Final snapshot:");
foreach (var d in results)
{
    Console.WriteLine($"{d.Ip,-15} {d.Status,-14} {d.KindLabel,-22} {d.OpenPortsLabel,-18} {d.HostName ?? "-"}");
}

return 0;
