namespace ARNetDiscovery.Core.Scanning;

public sealed class SnapshotBuffer<TKey, TValue>
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TKey, TValue> _items = new();

    public int Count
    {
        get
        {
            lock (_gate) return _items.Count;
        }
    }

    public IReadOnlyList<TValue> Snapshot
    {
        get
        {
            lock (_gate) return _items.Values.ToArray();
        }
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_gate)
        {
            return _items.TryGetValue(key, out value);
        }
    }

    public void Upsert(TKey key, TValue value)
    {
        lock (_gate)
        {
            _items[key] = value;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _items.Clear();
        }
    }
}
