namespace ARNetDiscovery.Core.Diagnostics;

public interface IDiagnosticSink
{
    void Publish(DiagnosticEntry entry);
}
