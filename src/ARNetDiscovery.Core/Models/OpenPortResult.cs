namespace ARNetDiscovery.Core.Models;

public sealed record OpenPortResult(
    int Port,
    string ProtocolKey,
    string Label,
    bool IsOpen,
    int? ProbeMs,
    string? Error = null);
