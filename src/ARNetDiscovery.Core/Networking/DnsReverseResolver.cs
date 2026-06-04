using System.Net;
using ARNetDiscovery.Core.Diagnostics;

namespace ARNetDiscovery.Core.Networking;

public sealed class DnsReverseResolver
{
    private readonly IDiagnosticSink _diagnostics;

    public DnsReverseResolver(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public async Task<string?> TryResolveAsync(IPAddress address, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            var task = Dns.GetHostEntryAsync(address.ToString());
            var completed = await Task.WhenAny(task, Task.Delay(Math.Max(50, timeoutMs), cancellationToken)).ConfigureAwait(false);
            if (completed != task)
            {
                _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                return null;
            }

            var entry = await task.ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(entry.HostName) ? null : entry.HostName;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(DnsReverseResolver), $"Reverse DNS unresolved for {address}.", ex));
            return null;
        }
    }
}
