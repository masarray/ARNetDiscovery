namespace ARNetDiscovery.Core.Diagnostics;

public sealed class NullDiagnosticSink : IDiagnosticSink
{
    public static readonly NullDiagnosticSink Instance = new();
    private NullDiagnosticSink() { }
    public void Publish(DiagnosticEntry entry) { }
}
