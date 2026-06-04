using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ARNetDiscovery.Core.Diagnostics;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Scanning;

public sealed class TcpPortProbe
{
    private readonly IDiagnosticSink _diagnostics;

    public TcpPortProbe(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public async Task<OpenPortResult> ProbeAsync(IPAddress address, ScanPortDefinition port, int timeoutMs, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient(address.AddressFamily);
            client.NoDelay = true;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(50, timeoutMs));

            await client.ConnectAsync(new IPEndPoint(address, port.Port), timeoutCts.Token).AsTask().ConfigureAwait(false);
            sw.Stop();
            return new OpenPortResult(port.Port, port.ProtocolKey, port.Label, true, (int)sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new OpenPortResult(port.Port, port.ProtocolKey, port.Label, false, null, "Timeout");
        }
        catch (Exception ex) when (ex is TimeoutException or SocketException or IOException or ObjectDisposedException)
        {
            sw.Stop();
            return new OpenPortResult(port.Port, port.ProtocolKey, port.Label, false, null, ex.GetType().Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(TcpPortProbe), $"TCP probe failed for {address}:{port.Port}.", ex));
            return new OpenPortResult(port.Port, port.ProtocolKey, port.Label, false, null, ex.GetType().Name);
        }
    }
}
