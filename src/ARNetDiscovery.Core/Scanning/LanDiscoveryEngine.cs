using System.Collections.Concurrent;
using System.Net;
using ARNetDiscovery.Core.Catalog;
using ARNetDiscovery.Core.Diagnostics;
using ARNetDiscovery.Core.Models;
using ARNetDiscovery.Core.Networking;

namespace ARNetDiscovery.Core.Scanning;

public sealed class LanDiscoveryEngine
{
    private readonly PingProbe _pingProbe;
    private readonly TcpPortProbe _tcpPortProbe;
    private readonly ArpTableReader _arpTableReader;
    private readonly DnsReverseResolver _dnsResolver;
    private readonly DeviceClassifier _classifier = new();
    private readonly OuiVendorLookup _vendorLookup;
    private readonly IDiagnosticSink _diagnostics;
    private readonly SnapshotBuffer<string, DiscoveredDeviceSnapshot> _snapshotBuffer = new();

    public LanDiscoveryEngine(IDiagnosticSink? diagnostics = null, OuiVendorLookup? vendorLookup = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
        _vendorLookup = vendorLookup ?? new OuiVendorLookup(_diagnostics);
        _pingProbe = new PingProbe(_diagnostics);
        _tcpPortProbe = new TcpPortProbe(_diagnostics);
        _arpTableReader = new ArpTableReader(_diagnostics);
        _dnsResolver = new DnsReverseResolver(_diagnostics);
    }

    public IReadOnlyList<DiscoveredDeviceSnapshot> CurrentSnapshot =>
        _snapshotBuffer.Snapshot.OrderBy(d => ToSortKey(d.IpAddress)).ToArray();

    public async Task<IReadOnlyList<DiscoveredDeviceSnapshot>> ScanAsync(
        NetworkAdapterInfo adapter,
        ScanSettings settings,
        IProgress<ScanProgressInfo>? progress = null,
        Action<DiscoveredDeviceSnapshot>? onDeviceDiscovered = null,
        CancellationToken cancellationToken = default)
    {
        var hosts = SubnetCalculator.GetSmartUsableHosts(adapter.Address, adapter.SubnetMask, settings)
            .Where(h => !h.Equals(adapter.Address))
            .Distinct()
            .ToArray();

        _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), SubnetCalculator.GetAdapterRiskNote(adapter.Address, adapter.SubnetMask, adapter.GatewayAddress)));
        return await ScanTargetsInternalAsync(
            hosts,
            settings,
            scopeLabel: $"Local scan on {adapter.DisplayName}",
            progress,
            onDeviceDiscovered,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DiscoveredDeviceSnapshot>> ProbeTargetsAsync(
        IEnumerable<IPAddress> targets,
        ScanSettings settings,
        IProgress<ScanProgressInfo>? progress = null,
        Action<DiscoveredDeviceSnapshot>? onDeviceDiscovered = null,
        CancellationToken cancellationToken = default)
    {
        var targetList = targets
            .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Distinct()
            .Take(Math.Max(1, settings.MaxHosts))
            .ToArray();

        return await ScanTargetsInternalAsync(
            targetList,
            settings,
            scopeLabel: targetList.Length == 1 ? $"Direct probe {targetList[0]}" : $"Custom target probe ({targetList.Length:n0} host(s))",
            progress,
            onDeviceDiscovered,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DiscoveredDeviceSnapshot>> ScanTargetsInternalAsync(
        IReadOnlyList<IPAddress> hosts,
        ScanSettings settings,
        string scopeLabel,
        IProgress<ScanProgressInfo>? progress,
        Action<DiscoveredDeviceSnapshot>? onDeviceDiscovered,
        CancellationToken cancellationToken)
    {
        _snapshotBuffer.Clear();

        if (hosts.Count == 0)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(LanDiscoveryEngine), "No usable IPv4 targets to scan."));
            return Array.Empty<DiscoveredDeviceSnapshot>();
        }

        _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), $"{scopeLabel} started; hosts={hosts.Count:n0}; pingTimeout={settings.PingTimeoutMs}ms; tcpTimeout={settings.TcpTimeoutMs}ms; pingConcurrency={settings.MaxHostConcurrency}; tcpConcurrency={settings.MaxTcpConcurrency}."));
        _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), "Progressive pipeline active: ping evidence is published first; protocol ports are enriched in background."));
        if (!settings.ProbeWebPortsWhenPingFails)
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), "Noise guard active: ping-blocked hosts are probed on industrial TCP ports only."));

        var checkedHosts = 0;
        using var pingSemaphore = new SemaphoreSlim(Math.Clamp(settings.MaxHostConcurrency, 1, 256));
        using var tcpSemaphore = new SemaphoreSlim(Math.Clamp(settings.MaxTcpConcurrency, 8, 512));
        var protocolTasks = new ConcurrentBag<Task>();

        void Publish(DiscoveredDeviceSnapshot snapshot)
        {
            _snapshotBuffer.Upsert(snapshot.Ip, snapshot);
            onDeviceDiscovered?.Invoke(snapshot);
        }

        var pingTasks = hosts.Select(async address =>
        {
            await pingSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var (pingSuccess, latencyMs) = await _pingProbe.ProbeAsync(address, settings.PingTimeoutMs, cancellationToken).ConfigureAwait(false);

                if (pingSuccess)
                {
                    var initial = BuildSnapshot(
                        address,
                        hostName: null,
                        pingSuccess: true,
                        latencyMs,
                        openPorts: Array.Empty<OpenPortResult>(),
                        isProbeInProgress: true,
                        stage: "Checking protocols");

                    Publish(initial);
                }

                if (pingSuccess || settings.ProbeTcpEvenWhenPingFails)
                {
                    protocolTasks.Add(ProbeProtocolsAndPublishAsync(
                        address,
                        pingSuccess,
                        latencyMs,
                        settings,
                        tcpSemaphore,
                        Publish,
                        cancellationToken));
                }
            }
            finally
            {
                var current = Interlocked.Increment(ref checkedHosts);
                progress?.Report(new ScanProgressInfo(hosts.Count, current, _snapshotBuffer.Count, address.ToString(), current >= hosts.Count));
                pingSemaphore.Release();
            }
        }).ToArray();

        try
        {
            await Task.WhenAll(pingTasks).ConfigureAwait(false);
            progress?.Report(new ScanProgressInfo(hosts.Count, hosts.Count, _snapshotBuffer.Count, "Protocol enrichment running", false));
            await Task.WhenAll(protocolTasks.ToArray()).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), "Scan cancelled by user."));
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(LanDiscoveryEngine), "Scan orchestration error handled by diagnostics.", ex));
        }

        try
        {
            var arp = await _arpTableReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            EnrichSnapshotWithArp(arp, onDeviceDiscovered);
        }
        catch (OperationCanceledException)
        {
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), "ARP enrichment skipped because scan was cancelled."));
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(LanDiscoveryEngine), "ARP enrichment failed but scan result is preserved.", ex));
        }

        var finalSnapshot = CurrentSnapshot;
        progress?.Report(new ScanProgressInfo(hosts.Count, hosts.Count, finalSnapshot.Count, string.Empty, true));
        _diagnostics.Publish(DiagnosticEntry.Info(nameof(LanDiscoveryEngine), $"Scan completed. Discovered {finalSnapshot.Count:n0} device(s)."));
        return finalSnapshot;
    }

    private async Task ProbeProtocolsAndPublishAsync(
        IPAddress address,
        bool pingSuccess,
        int? latencyMs,
        ScanSettings settings,
        SemaphoreSlim tcpSemaphore,
        Action<DiscoveredDeviceSnapshot> publish,
        CancellationToken cancellationToken)
    {
        try
        {
            var selectedPorts = SelectPortsForHost(pingSuccess, settings);
            var portTasks = selectedPorts
                .Select(port => ProbeTcpWithGateAsync(address, port, settings, tcpSemaphore, cancellationToken))
                .ToArray();

            var ports = await Task.WhenAll(portTasks).ConfigureAwait(false);
            var openPorts = ports.Where(p => p.IsOpen).OrderBy(p => p.Port).ToArray();

            if (!pingSuccess && openPorts.Length == 0)
                return;

            var hostName = settings.EnableReverseDns && pingSuccess
                ? await _dnsResolver.TryResolveAsync(address, 250, cancellationToken).ConfigureAwait(false)
                : null;

            var updated = BuildSnapshot(
                address,
                hostName,
                pingSuccess,
                latencyMs,
                openPorts,
                isProbeInProgress: false,
                stage: openPorts.Length > 0 ? "Protocol evidence found" : "Host reachable, no protocol evidence");

            if (updated.LatencyMs is null)
                updated = updated with { TcpProbeMs = openPorts.Where(p => p.ProbeMs.HasValue).Select(p => p.ProbeMs!.Value).DefaultIfEmpty().Min() };

            publish(updated);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(LanDiscoveryEngine), $"Protocol probe failed for {address}; result will remain partial.", ex));
            if (pingSuccess)
            {
                var partial = BuildSnapshot(
                    address,
                    null,
                    true,
                    latencyMs,
                    Array.Empty<OpenPortResult>(),
                    isProbeInProgress: false,
                    stage: "Protocol probe failed");
                publish(partial);
            }
        }
    }

    private DiscoveredDeviceSnapshot BuildSnapshot(
        IPAddress address,
        string? hostName,
        bool pingSuccess,
        int? latencyMs,
        IReadOnlyList<OpenPortResult> openPorts,
        bool isProbeInProgress,
        string stage)
    {
        var status = _classifier.ResolveStatus(pingSuccess, openPorts.Count > 0, latencyMs);
        var classification = _classifier.Classify(hostName, null, openPorts, pingSuccess, latencyMs);
        var now = DateTimeOffset.Now;

        var firstSeen = now;
        if (_snapshotBuffer.TryGet(address.ToString(), out var existing) && existing is not null)
        {
            firstSeen = existing.FirstSeen;
            hostName ??= existing.HostName;
        }

        return new DiscoveredDeviceSnapshot
        {
            IpAddress = address,
            HostName = hostName,
            Status = status,
            Kind = classification.Kind,
            LatencyMs = latencyMs,
            OpenPorts = openPorts,
            ProtocolTags = classification.Tags,
            FirstSeen = firstSeen,
            LastSeen = now,
            Evidence = isProbeInProgress
                ? "Ping response received. Protocol probe is still running in the background."
                : classification.Evidence,
            IsProbeInProgress = isProbeInProgress,
            ProbeStage = stage
        };
    }

    private static ScanPortDefinition[] SelectPortsForHost(bool pingSuccess, ScanSettings settings)
    {
        return settings.Ports
            .Where(p =>
            {
                if (pingSuccess)
                    return true;

                if (!settings.ProbeOnlyCriticalPortsWhenPingFails)
                    return true;

                if (!ScanSettings.CriticalPingBlockedPorts.Contains(p.Port))
                    return false;

                if (!settings.ProbeWebPortsWhenPingFails && (p.ProtocolKey is "WEB" or "HTTPS" or "WEBALT"))
                    return false;

                return true;
            })
            .OrderBy(p => p.Priority)
            .ToArray();
    }

    private async Task<OpenPortResult> ProbeTcpWithGateAsync(IPAddress address, ScanPortDefinition port, ScanSettings settings, SemaphoreSlim tcpSemaphore, CancellationToken cancellationToken)
    {
        await tcpSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _tcpPortProbe.ProbeAsync(address, port, settings.TcpTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            tcpSemaphore.Release();
        }
    }

    private void EnrichSnapshotWithArp(IReadOnlyDictionary<string, string> arp, Action<DiscoveredDeviceSnapshot>? publish)
    {
        foreach (var device in CurrentSnapshot)
        {
            if (!arp.TryGetValue(device.Ip, out var mac))
                continue;

            var vendor = _vendorLookup.FindVendor(mac);
            var classification = _classifier.Classify(device.HostName, vendor, device.OpenPorts, device.Status is DeviceStatus.Online or DeviceStatus.PingOnly or DeviceStatus.Slow, device.LatencyMs);
            var enriched = device with
            {
                MacAddress = mac,
                Vendor = vendor,
                Kind = classification.Kind,
                ProtocolTags = classification.Tags,
                Evidence = string.Join(" ", new[] { device.Evidence, classification.Evidence }.Where(s => !string.IsNullOrWhiteSpace(s))),
                IsProbeInProgress = false,
                ProbeStage = string.IsNullOrWhiteSpace(device.ProbeStage) ? "ARP enriched" : device.ProbeStage
            };

            _snapshotBuffer.Upsert(enriched.Ip, enriched);
            publish?.Invoke(enriched);
        }
    }

    private static uint ToSortKey(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}
