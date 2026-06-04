using System.Collections.Concurrent;

namespace ARNetDiscovery.Core.Diagnostics;

public sealed class BufferedDiagnosticSink : IDiagnosticSink
{
    private readonly ConcurrentQueue<DiagnosticEntry> _entries = new();
    private readonly int _capacity;

    public BufferedDiagnosticSink(int capacity = 500)
    {
        _capacity = Math.Max(50, capacity);
    }

    public event EventHandler<DiagnosticEntry>? EntryPublished;

    public IReadOnlyList<DiagnosticEntry> Snapshot => _entries.ToArray();

    public void Publish(DiagnosticEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > _capacity && _entries.TryDequeue(out _))
        {
        }

        EntryPublished?.Invoke(this, entry);
    }
}
