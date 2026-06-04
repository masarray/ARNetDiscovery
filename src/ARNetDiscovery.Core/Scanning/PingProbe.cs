using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using ARNetDiscovery.Core.Diagnostics;

namespace ARNetDiscovery.Core.Scanning;

public sealed class PingProbe
{
    private readonly IDiagnosticSink _diagnostics;

    public PingProbe(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public async Task<(bool Success, int? LatencyMs)> ProbeAsync(IPAddress address, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var sw = Stopwatch.StartNew();
            var reply = await ping.SendPingAsync(address, timeoutMs).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + 100), cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (reply.Status == IPStatus.Success)
                return (true, (int)Math.Max(0, reply.RoundtripTime));

            return (false, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(PingProbe), $"Ping failed for {address}.", ex));
            return (false, null);
        }
    }
}
