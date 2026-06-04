namespace ARNetDiscovery.Core.Models;

public sealed record ScanPortDefinition(
    int Port,
    string ProtocolKey,
    string Label,
    string Description,
    int Priority = 100)
{
    public override string ToString() => $"{Port} {Label}";
}
