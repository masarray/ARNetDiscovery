using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ARNetDiscovery.Core.Catalog;
using ARNetDiscovery.Core.Diagnostics;
using ARNetDiscovery.Core.Models;
using ARNetDiscovery.Core.Networking;
using ARNetDiscovery.Core.Scanning;
using ARNetDiscovery.Core.Targets;
using ARNetDiscovery.Wpf.Commands;

namespace ARNetDiscovery.Wpf.ViewModels;

public enum DeviceSortColumn
{
    Device,
    Ip,
    Expected,
    Protocols,
    Ping,
    Status
}

public sealed class MainViewModel : ViewModelBase
{
    private const int MaxVisibleRows = 5000;
    private static readonly HashSet<string> IndustrialProtocolKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "IEC61850", "IEC104", "MODBUS", "DNP3", "OPCUA", "SNMP", "SSH", "TELNET"
    };

    private readonly BufferedDiagnosticSink _diagnostics = new(700);
    private readonly OuiVendorLookup _ouiLookup;
    private readonly LanDiscoveryEngine _engine;
    private readonly NetworkAdapterProvider _adapterProvider;
    private readonly TargetListImporter _targetListImporter = new();
    private readonly ScanSettings _defaultScanSettings = new();
    private readonly DispatcherTimer _deviceBatchTimer;
    private readonly object _pendingLock = new();
    private readonly Dictionary<string, DiscoveredDeviceSnapshot> _pendingDevices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DiscoveredDeviceSnapshot> _deviceIndex = new(StringComparer.Ordinal);
    private IReadOnlyList<TargetDeviceRecord> _importedTargets = Array.Empty<TargetDeviceRecord>();

    private CancellationTokenSource? _scanCancellation;
    private NetworkAdapterInfo? _selectedAdapter;
    private DiscoveredDeviceSnapshot? _selectedDevice;
    private bool _isScanning;
    private bool _isFinalizingScan;
    private int _totalHosts;
    private int _checkedHosts;
    private int _discoveredHosts;
    private int _filteredDeviceCount;
    private string _currentAddress = "Ready";
    private string _filterText = string.Empty;
    private string _directTargetText = string.Empty;
    private string _selectedSection = "Discovery";
    private string _scanInsightTitle = "Ready for LAN discovery";
    private string _scanInsightMessage = "Choose the correct engineering adapter, then scan. ARNet will use a safe local scan window and push all exceptions to diagnostics instead of crashing.";
    private string _scanAdvice = "Tip: for panel LAN, use the NIC directly connected to relay/gateway/switch. Avoid Wi-Fi/VPN adapters unless that is the real route.";
    private string _scanStateLabel = "Idle";
    private string _scanStateMessage = "No scan running.";
    private string _resultBufferMessage = "Table is ready.";
    private string _importedListTitle = "No target list imported";
    private string _importedListSummary = "Import Excel/CSV/TXT to scan exact relay/server IP targets across segments.";
    private DateTime _lastProgressUi = DateTime.MinValue;
    private bool _isDiagnosticsExpanded = true;
    private bool _isInspectorExpanded = true;
    private bool _isAlwaysOnTop = true;
    private DeviceSortColumn _sortColumn = DeviceSortColumn.Status;
    private bool _sortDescending = true;
    private GridLength _deviceColumnWidth = new(330);
    private GridLength _ipColumnWidth = new(180);
    private GridLength _expectedColumnWidth = new(190);
    private GridLength _protocolColumnWidth = new(260);
    private GridLength _pingColumnWidth = new(110);
    private GridLength _statusColumnWidth = new(132);

    public MainViewModel()
    {
        _ouiLookup = new OuiVendorLookup(_diagnostics);
        _ouiLookup.LoadCsvIfExists(Path.Combine(AppContext.BaseDirectory, "Data", "oui-custom.csv"));
        _ouiLookup.LoadCsvIfExists(Path.Combine(AppContext.BaseDirectory, "oui-custom.csv"));

        _engine = new LanDiscoveryEngine(_diagnostics, _ouiLookup);
        _adapterProvider = new NetworkAdapterProvider(_diagnostics);

        _deviceBatchTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _deviceBatchTimer.Tick += (_, _) => FlushPendingDevices();

        ScanCommand = new AsyncRelayCommand(_ => ScanAsync(), _ => SelectedAdapter is not null && !IsScanning);
        ProbeTargetCommand = new AsyncRelayCommand(_ => ProbeTargetAsync(), _ => !IsScanning && !string.IsNullOrWhiteSpace(DirectTargetText));
        StopCommand = new RelayCommand(_ => StopScan(), _ => IsScanning);
        RefreshAdaptersCommand = new RelayCommand(_ => LoadAdapters());
        SelectDeviceCommand = new RelayCommand(p => SelectDevice(p as DiscoveredDeviceSnapshot));
        ExportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => Devices.Count > 0);
        CopyIpCommand = new RelayCommand(_ => CopySelectedIp(), _ => SelectedDevice is not null);
        LoadDemoCommand = new RelayCommand(_ => LoadDemoSnapshot(), _ => !IsScanning);
        ImportTargetListCommand = new RelayCommand(_ => ImportTargetList(), _ => !IsScanning);
        ScanImportedTargetsCommand = new AsyncRelayCommand(_ => ScanImportedTargetsAsync(), _ => !IsScanning && HasImportedTargets);
        ClearDiagnosticsCommand = new RelayCommand(_ => ClearDiagnostics());
        ToggleDiagnosticsCommand = new RelayCommand(_ => IsDiagnosticsExpanded = !IsDiagnosticsExpanded);
        ToggleInspectorCommand = new RelayCommand(_ => IsInspectorExpanded = !IsInspectorExpanded);
        ToggleAlwaysOnTopCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        SortDevicesCommand = new RelayCommand(SortDevices);

        _diagnostics.EntryPublished += (_, entry) => RunOnUi(() =>
        {
            Diagnostics.Insert(0, entry);
            while (Diagnostics.Count > 200) Diagnostics.RemoveAt(Diagnostics.Count - 1);
            OnPropertyChanged(nameof(HasDiagnostics));
        });

        LoadAdapters();
    }

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = new();

    // Full buffered snapshot. Used for counters, export, and selected-device evidence.
    public ObservableCollection<DiscoveredDeviceSnapshot> Devices { get; } = new();

    // Filtered render snapshot for the virtualized device table.
    public ObservableCollection<DiscoveredDeviceSnapshot> VisibleDevices { get; } = new();
    public ObservableCollection<DiagnosticEntry> Diagnostics { get; } = new();

    public ICommand ScanCommand { get; }
    public ICommand ProbeTargetCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RefreshAdaptersCommand { get; }
    public ICommand SelectDeviceCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand CopyIpCommand { get; }
    public ICommand LoadDemoCommand { get; }
    public ICommand ImportTargetListCommand { get; }
    public ICommand ScanImportedTargetsCommand { get; }
    public ICommand ClearDiagnosticsCommand { get; }
    public ICommand ToggleDiagnosticsCommand { get; }
    public ICommand ToggleInspectorCommand { get; }
    public ICommand ToggleAlwaysOnTopCommand { get; }
    public ICommand SortDevicesCommand { get; }

    public NetworkAdapterInfo? SelectedAdapter
    {
        get => _selectedAdapter;
        set
        {
            if (SetProperty(ref _selectedAdapter, value))
            {
                OnPropertyChanged(nameof(AdapterSummary));
                OnPropertyChanged(nameof(ScanRangeLabel));
                UpdateScanInsightForAdapter();
                RaiseCommandStates();
            }
        }
    }

    public DiscoveredDeviceSnapshot? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                OnPropertyChanged(nameof(HasSelectedDevice));
                RaiseCommandStates();
            }
        }
    }

    public bool HasSelectedDevice => SelectedDevice is not null;
    public bool HasDevices => Devices.Count > 0;
    public bool HasVisibleDevices => VisibleDevices.Count > 0;
    public bool HasDiagnostics => Diagnostics.Count > 0;
    public bool HasImportedTargets => _importedTargets.Count > 0;
    public int ImportedTargetCount => _importedTargets.Count;
    public string ImportedTargetCountLabel => $"{ImportedTargetCount:n0} targets";

    public string ImportedListTitle
    {
        get => _importedListTitle;
        set => SetProperty(ref _importedListTitle, value);
    }

    public string ImportedListSummary
    {
        get => _importedListSummary;
        set => SetProperty(ref _importedListSummary, value);
    }

    public bool IsDiagnosticsExpanded
    {
        get => _isDiagnosticsExpanded;
        set
        {
            if (SetProperty(ref _isDiagnosticsExpanded, value))
            {
                OnPropertyChanged(nameof(DiagnosticsPanelHeight));
                OnPropertyChanged(nameof(DiagnosticsToggleText));
            }
        }
    }

    public double DiagnosticsPanelHeight => IsDiagnosticsExpanded ? 168 : 38;
    public string DiagnosticsToggleText => IsDiagnosticsExpanded ? "Collapse" : "Expand";

    public bool IsInspectorExpanded
    {
        get => _isInspectorExpanded;
        set
        {
            if (SetProperty(ref _isInspectorExpanded, value))
            {
                OnPropertyChanged(nameof(InspectorPanelWidth));
                OnPropertyChanged(nameof(InspectorToggleToolTip));
            }
        }
    }

    public double InspectorPanelWidth => IsInspectorExpanded ? 400 : 56;
    public string InspectorToggleToolTip => IsInspectorExpanded ? "Collapse inspector" : "Expand inspector";

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            if (SetProperty(ref _isAlwaysOnTop, value))
            {
                OnPropertyChanged(nameof(AlwaysOnTopToolTip));
            }
        }
    }

    public string AlwaysOnTopToolTip => IsAlwaysOnTop ? "Always on top is ON" : "Always on top is OFF";

    public GridLength DeviceColumnWidth
    {
        get => _deviceColumnWidth;
        set => SetGridLength(ref _deviceColumnWidth, value);
    }

    public GridLength IpColumnWidth
    {
        get => _ipColumnWidth;
        set => SetGridLength(ref _ipColumnWidth, value);
    }

    public GridLength ExpectedColumnWidth
    {
        get => _expectedColumnWidth;
        set => SetGridLength(ref _expectedColumnWidth, value);
    }

    public GridLength ProtocolColumnWidth
    {
        get => _protocolColumnWidth;
        set => SetGridLength(ref _protocolColumnWidth, value);
    }

    public GridLength PingColumnWidth
    {
        get => _pingColumnWidth;
        set => SetGridLength(ref _pingColumnWidth, value);
    }

    public GridLength StatusColumnWidth
    {
        get => _statusColumnWidth;
        set => SetGridLength(ref _statusColumnWidth, value);
    }

    public string DeviceSortGlyph => SortGlyph(DeviceSortColumn.Device);
    public string IpSortGlyph => SortGlyph(DeviceSortColumn.Ip);
    public string ExpectedSortGlyph => SortGlyph(DeviceSortColumn.Expected);
    public string ProtocolSortGlyph => SortGlyph(DeviceSortColumn.Protocols);
    public string PingSortGlyph => SortGlyph(DeviceSortColumn.Ping);
    public string StatusSortGlyph => SortGlyph(DeviceSortColumn.Status);


    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                OnPropertyChanged(nameof(ScanButtonText));
                OnPropertyChanged(nameof(BusyStateText));
                RaiseCommandStates();
            }
        }
    }

    public bool IsFinalizingScan
    {
        get => _isFinalizingScan;
        set
        {
            if (SetProperty(ref _isFinalizingScan, value))
            {
                OnPropertyChanged(nameof(BusyStateText));
            }
        }
    }

    public int TotalHosts
    {
        get => _totalHosts;
        set
        {
            if (SetProperty(ref _totalHosts, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ScanProgressText));
            }
        }
    }

    public int CheckedHosts
    {
        get => _checkedHosts;
        set
        {
            if (SetProperty(ref _checkedHosts, value))
            {
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ScanProgressText));
            }
        }
    }

    public int DiscoveredHosts
    {
        get => _discoveredHosts;
        set => SetProperty(ref _discoveredHosts, value);
    }

    public int FilteredDeviceCount
    {
        get => _filteredDeviceCount;
        private set
        {
            if (SetProperty(ref _filteredDeviceCount, value))
            {
                OnPropertyChanged(nameof(HiddenDeviceCount));
                OnPropertyChanged(nameof(HasHiddenBufferedDevices));
            }
        }
    }

    public string CurrentAddress
    {
        get => _currentAddress;
        set => SetProperty(ref _currentAddress, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                RebuildVisibleDevices();
        }
    }

    public string DirectTargetText
    {
        get => _directTargetText;
        set
        {
            if (SetProperty(ref _directTargetText, value))
                RaiseCommandStates();
        }
    }

    public string SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public string ScanInsightTitle
    {
        get => _scanInsightTitle;
        set => SetProperty(ref _scanInsightTitle, value);
    }

    public string ScanInsightMessage
    {
        get => _scanInsightMessage;
        set => SetProperty(ref _scanInsightMessage, value);
    }

    public string ScanAdvice
    {
        get => _scanAdvice;
        set => SetProperty(ref _scanAdvice, value);
    }

    public string ScanStateLabel
    {
        get => _scanStateLabel;
        set => SetProperty(ref _scanStateLabel, value);
    }

    public string ScanStateMessage
    {
        get => _scanStateMessage;
        set => SetProperty(ref _scanStateMessage, value);
    }

    public string ResultBufferMessage
    {
        get => _resultBufferMessage;
        set => SetProperty(ref _resultBufferMessage, value);
    }

    public double ProgressPercent => TotalHosts <= 0 ? 0 : Math.Clamp((double)CheckedHosts / TotalHosts * 100.0, 0, 100);
    public string ScanButtonText => IsScanning ? "Scanning..." : "Scan Local";
    public string BusyStateText => IsFinalizingScan ? "Finalizing evidence" : IsScanning ? "Progressive scan running" : "Ready";
    public string ScanProgressText => TotalHosts <= 0 ? "No active scan" : $"{CheckedHosts:n0} / {TotalHosts:n0} hosts checked";
    public string AdapterSummary => SelectedAdapter is null ? "No active adapter" : $"{SelectedAdapter.Name} · {SelectedAdapter.Address}/{SelectedAdapter.PrefixLength} · {SelectedAdapter.SpeedLabel}";
    public string ScanRangeLabel => SelectedAdapter is null ? "No scan range" : SubnetCalculator.GetSmartRangeLabel(SelectedAdapter.Address, SelectedAdapter.SubnetMask, _defaultScanSettings);
    public int HiddenDeviceCount => Math.Max(0, FilteredDeviceCount - VisibleDevices.Count);
    public bool HasHiddenBufferedDevices => HiddenDeviceCount > 0;

    public int ExpectedTargetCount => Devices.Count(d => d.IsExpectedTarget);
    public int NoResponseCount => Devices.Count(d => d.Status is DeviceStatus.NoResponse or DeviceStatus.Offline or DeviceStatus.Pending);

    public int OnlineCount => Devices.Count(d => d.Status is DeviceStatus.Online or DeviceStatus.PingOnly or DeviceStatus.Slow or DeviceStatus.PortOpenOnly);
    public int RelayCandidateCount => Devices.Count(d => d.Kind is DeviceKind.ProtectionRelay or DeviceKind.BayController);
    public int GatewayCount => Devices.Count(d => d.Kind is DeviceKind.Gateway or DeviceKind.SerialServer or DeviceKind.PlcOrController);
    public int SwitchCount => Devices.Count(d => d.Kind == DeviceKind.ManagedSwitch);
    public int IndustrialCandidateCount => Devices.Count(IsIndustrialCandidate);
    public int LowConfidenceHostCount => Devices.Count(IsLikelyLowConfidenceHost);

    public IDiagnosticSink DiagnosticsSink => _diagnostics;

    public void ReportUnhandledException(string source, Exception exception)
        => _diagnostics.Publish(DiagnosticEntry.Error(source, "Unhandled exception captured by application guard.", exception));

    private void ClearDiagnostics()
    {
        Diagnostics.Clear();
        OnPropertyChanged(nameof(HasDiagnostics));
    }

    private void LoadAdapters()
    {
        Adapters.Clear();
        foreach (var adapter in _adapterProvider.GetActiveIpv4Adapters())
            Adapters.Add(adapter);

        SelectedAdapter = Adapters.FirstOrDefault();
        UpdateScanInsightForAdapter();
        _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), $"Detected {Adapters.Count:n0} active IPv4 adapters."));
    }

    private async Task ScanAsync()
    {
        if (SelectedAdapter is null) return;

        var settings = _defaultScanSettings;
        ScanInsightTitle = "Scanning safe local window";
        ScanInsightMessage = $"Checking {SubnetCalculator.GetSmartRangeLabel(SelectedAdapter.Address, SelectedAdapter.SubnetMask, settings)}. Ping evidence appears first; protocol evidence is enriched progressively in background.";
        ScanAdvice = "If the relay/server IP is outside this safe local window, use Direct Probe or a custom range such as 1.110.5.1 or 1.110.5.0/24.";
        ScanStateLabel = "Scanning";
        ScanStateMessage = "Progressive discovery running. Hosts that answer ping appear immediately; protocol columns update as TCP probes finish.";

        ClearDeviceState();
        TotalHosts = 0;
        CheckedHosts = 0;
        DiscoveredHosts = 0;
        CurrentAddress = "Starting scan";
        IsFinalizingScan = false;
        IsScanning = true;
        _lastProgressUi = DateTime.MinValue;
        _scanCancellation = new CancellationTokenSource();
        _deviceBatchTimer.Start();

        var progress = new Progress<ScanProgressInfo>(p =>
        {
            var now = DateTime.UtcNow;
            if (!p.IsCompleted && (now - _lastProgressUi).TotalMilliseconds < 120)
                return;

            _lastProgressUi = now;
            TotalHosts = p.TotalHosts;
            CheckedHosts = p.CheckedHosts;
            DiscoveredHosts = p.DiscoveredHosts;
            CurrentAddress = p.IsCompleted ? "Scan completed" : p.CurrentAddress;
            ScanStateMessage = p.IsCompleted
                ? "Discovery sweep completed. ARNet is enriching the snapshot with ARP/MAC/vendor information."
                : $"Checking {p.CurrentAddress} · {p.CheckedHosts:n0}/{p.TotalHosts:n0} hosts · {p.DiscoveredHosts:n0} candidates";
        });

        try
        {
            var finalSnapshot = await _engine.ScanAsync(SelectedAdapter, settings, progress, EnqueueDevice, _scanCancellation.Token);
            var wasCancelled = _scanCancellation?.IsCancellationRequested == true;

            RunOnUi(() =>
            {
                IsFinalizingScan = true;
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                ReplaceDeviceSnapshot(finalSnapshot.OrderBy(d => IpSortKey(d.Ip)).ToArray());
                DiscoveredHosts = Devices.Count;
                RefreshCounters();
                if (wasCancelled)
                {
                    ScanInsightTitle = "Scan stopped";
                    ScanInsightMessage = "User stopped the discovery session. Partial snapshot is preserved in the UI.";
                    ScanAdvice = "Run scan again after selecting the correct adapter or changing the LAN connection.";
                    ScanStateLabel = "Stopped";
                    ScanStateMessage = $"Scan stopped. {Devices.Count:n0} buffered host(s) are available for review/export.";
                }
                else
                {
                    UpdateScanOutcome();
                }
                if (SelectedDevice is null && VisibleDevices.Count > 0)
                    SelectedDevice = VisibleDevices[0];
            });
        }
        catch (OperationCanceledException)
        {
            ScanInsightTitle = "Scan stopped";
            ScanInsightMessage = "User stopped the discovery session. Existing snapshot is preserved in the UI.";
            ScanAdvice = "Run scan again after selecting the correct adapter or changing the LAN connection.";
            ScanStateLabel = "Stopped";
            ScanStateMessage = "Scan stopped by user. Current device snapshot remains available.";
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), "Scan cancellation observed by UI."));
        }
        catch (Exception ex)
        {
            ScanInsightTitle = "Scan error handled";
            ScanInsightMessage = "An exception occurred, but ARNet kept the app alive. Open diagnostics for the exact source and message.";
            ScanAdvice = "Send the diagnostic line if this repeats; the scanner engine is designed to fail-soft.";
            ScanStateLabel = "Error handled";
            ScanStateMessage = "An engine exception was handled and reported to diagnostics. The app is still usable.";
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(MainViewModel), "Scan failed but application remained alive.", ex));
        }
        finally
        {
            RunOnUi(() =>
            {
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                IsFinalizingScan = false;
                IsScanning = false;
                if (!ScanStateLabel.Contains("Error", StringComparison.OrdinalIgnoreCase) && ScanStateLabel != "Stopped")
                    ScanStateLabel = "Complete";
                if (ScanStateLabel == "Complete")
                    ScanStateMessage = $"Scan complete. {Devices.Count:n0} buffered host(s), {IndustrialCandidateCount:n0} protocol candidate(s).";
                _scanCancellation?.Dispose();
                _scanCancellation = null;
                RaiseCommandStates();
            });
        }
    }


    private async Task ProbeTargetAsync()
    {
        var targets = ParseTargetInput(DirectTargetText, Math.Max(1, _defaultScanSettings.MaxHosts))
            .Where(ip => !IsLocalAdapterAddress(ip))
            .ToArray();
        if (targets.Length == 0)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(MainViewModel), $"Invalid target input: '{DirectTargetText}'. Use single IP, CIDR /24, or start-end range."));
            ScanInsightTitle = "Invalid target";
            ScanInsightMessage = "Enter a valid IPv4 target such as 1.110.5.1, 1.110.5.0/24, or 1.110.5.1-1.110.5.20. The laptop's own IP is intentionally excluded.";
            ScanStateLabel = "Input error";
            ScanStateMessage = "Target probe was not started because the target field is invalid.";
            return;
        }

        var settings = _defaultScanSettings with
        {
            PingTimeoutMs = 500,
            TcpTimeoutMs = 700,
            MaxHostConcurrency = Math.Min(64, _defaultScanSettings.MaxHostConcurrency),
            MaxTcpConcurrency = Math.Min(96, _defaultScanSettings.MaxTcpConcurrency),
            ProbeWebPortsWhenPingFails = true,
            EnableReverseDns = false
        };

        ScanInsightTitle = targets.Length == 1 ? $"Direct probe {targets[0]}" : $"Custom probe {targets.Length:n0} host(s)";
        ScanInsightMessage = "Target probe runs outside the safe local adapter window. Ping evidence appears first; protocol ports enrich the row in background.";
        ScanAdvice = "Use this mode for relay/server IPs in another routed segment, for example 1.110.5.1.";
        ScanStateLabel = "Probing";
        ScanStateMessage = "Direct/custom probe running. Reachable hosts appear immediately; protocol evidence follows.";

        ClearDeviceState();
        TotalHosts = targets.Length;
        CheckedHosts = 0;
        DiscoveredHosts = 0;
        CurrentAddress = "Starting target probe";
        IsFinalizingScan = false;
        IsScanning = true;
        _lastProgressUi = DateTime.MinValue;
        _scanCancellation = new CancellationTokenSource();
        _deviceBatchTimer.Start();

        var progress = new Progress<ScanProgressInfo>(p =>
        {
            var now = DateTime.UtcNow;
            if (!p.IsCompleted && (now - _lastProgressUi).TotalMilliseconds < 100)
                return;

            _lastProgressUi = now;
            TotalHosts = p.TotalHosts;
            CheckedHosts = p.CheckedHosts;
            DiscoveredHosts = p.DiscoveredHosts;
            CurrentAddress = p.IsCompleted ? "Probe completed" : p.CurrentAddress;
            ScanStateMessage = p.IsCompleted
                ? "Target probe completed. Final evidence is available in the table and inspector."
                : $"Checking {p.CurrentAddress} · {p.CheckedHosts:n0}/{p.TotalHosts:n0} host(s) · {p.DiscoveredHosts:n0} visible evidence row(s)";
        });

        try
        {
            var finalSnapshot = await _engine.ProbeTargetsAsync(targets, settings, progress, EnqueueDevice, _scanCancellation.Token);
            var wasCancelled = _scanCancellation?.IsCancellationRequested == true;
            RunOnUi(() =>
            {
                IsFinalizingScan = true;
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                ReplaceDeviceSnapshot(finalSnapshot.OrderBy(d => IpSortKey(d.Ip)).ToArray());
                DiscoveredHosts = Devices.Count;
                RefreshCounters();

                if (wasCancelled)
                {
                    ScanInsightTitle = "Probe stopped";
                    ScanInsightMessage = "User stopped the target probe. Partial evidence is preserved.";
                    ScanStateLabel = "Stopped";
                    ScanStateMessage = $"Probe stopped. {Devices.Count:n0} buffered device(s) remain available.";
                }
                else if (Devices.Count == 0)
                {
                    ScanInsightTitle = "No target response";
                    ScanInsightMessage = "No ping reply and no expected protocol port responded from the target input.";
                    ScanAdvice = "Check routing/VLAN/cable/firewall. If Windows ping succeeds but ARNet does not, increase timeout later or report the exact target/adapter.";
                    ScanStateLabel = "Complete";
                    ScanStateMessage = "Target probe complete with no device evidence.";
                }
                else
                {
                    ScanInsightTitle = $"{Devices.Count:n0} target device(s) found";
                    ScanInsightMessage = "Ping evidence was shown early; protocol fingerprints were enriched progressively in background.";
                    ScanAdvice = "Select a row to inspect port evidence and classification.";
                    ScanStateLabel = "Complete";
                    ScanStateMessage = $"Target probe complete. {Devices.Count:n0} device(s), {IndustrialCandidateCount:n0} protocol candidate(s).";
                }

                if (SelectedDevice is null && VisibleDevices.Count > 0)
                    SelectedDevice = VisibleDevices[0];
            });
        }
        catch (OperationCanceledException)
        {
            ScanStateLabel = "Stopped";
            ScanStateMessage = "Target probe stopped by user.";
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), "Target probe cancellation observed by UI."));
        }
        catch (Exception ex)
        {
            ScanInsightTitle = "Probe error handled";
            ScanInsightMessage = "An exception occurred during target probe, but ARNet kept the app alive.";
            ScanStateLabel = "Error handled";
            ScanStateMessage = "Target probe error was pushed to diagnostics.";
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(MainViewModel), "Target probe failed but application remained alive.", ex));
        }
        finally
        {
            RunOnUi(() =>
            {
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                IsFinalizingScan = false;
                IsScanning = false;
                _scanCancellation?.Dispose();
                _scanCancellation = null;
                RaiseCommandStates();
            });
        }
    }


    private void ImportTargetList()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import ARNet target list",
                Filter = "Target lists (*.xlsx;*.csv;*.txt)|*.xlsx;*.csv;*.txt|Excel workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|Text IP list (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            var result = _targetListImporter.Import(dialog.FileName);
            _importedTargets = result.Targets
                .Where(t => !IsLocalAdapterAddress(t.IpAddress))
                .ToArray();

            ImportedListTitle = result.Title;
            ImportedListSummary = _importedTargets.Count > 0
                ? $"{_importedTargets.Count:n0} target(s) loaded · {result.SubnetGroups.Count:n0} subnet group(s) · {Path.GetFileName(dialog.FileName)}"
                : $"No valid target IP found in {Path.GetFileName(dialog.FileName)}.";

            OnPropertyChanged(nameof(HasImportedTargets));
            OnPropertyChanged(nameof(ImportedTargetCount));
            OnPropertyChanged(nameof(ImportedTargetCountLabel));
            RaiseCommandStates();

            ClearDeviceState();
            ReplaceDeviceSnapshot(_importedTargets.Select(t => t.ToPendingSnapshot()).ToArray());
            SelectedDevice = VisibleDevices.FirstOrDefault();

            ScanInsightTitle = _importedTargets.Count > 0 ? "Target list imported" : "Import completed without targets";
            ScanInsightMessage = _importedTargets.Count > 0
                ? "Imported targets are shown as expected devices. Click Scan List to probe exact IPs across routed segments without sweeping a whole /16 or /8 network."
                : "ARNet could not find valid IPv4 target rows. Check that the workbook contains an IP ADDRESS column or a plain IP list.";
            ScanAdvice = "Target List Scan is best for FAT/commissioning inventory verification: expected device first, ping evidence next, protocol evidence later.";
            ScanStateLabel = "Imported";
            ScanStateMessage = _importedTargets.Count > 0
                ? $"Ready to scan {_importedTargets.Count:n0} imported target(s)."
                : "No imported target is available.";
            TotalHosts = _importedTargets.Count;
            CheckedHosts = 0;
            DiscoveredHosts = Devices.Count;
            CurrentAddress = "Target list ready";

            foreach (var warning in result.Warnings.Take(12))
                _diagnostics.Publish(DiagnosticEntry.Warning(nameof(TargetListImporter), warning));
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(TargetListImporter), $"Imported {_importedTargets.Count:n0} target(s) from {dialog.FileName}."));
        }
        catch (Exception ex)
        {
            ScanInsightTitle = "Import error handled";
            ScanInsightMessage = "Target list import failed, but the app stayed alive. Open diagnostics for the exact file/parser error.";
            ScanStateLabel = "Import error";
            ScanStateMessage = "Target list import failed.";
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(TargetListImporter), "Failed to import target list.", ex));
        }
    }

    private async Task ScanImportedTargetsAsync()
    {
        var targets = _importedTargets
            .Where(t => !IsLocalAdapterAddress(t.IpAddress))
            .ToArray();

        if (targets.Length == 0)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(MainViewModel), "Scan List requested but no imported target is available."));
            ScanInsightTitle = "No imported targets";
            ScanInsightMessage = "Import an Excel/CSV/TXT target list first, then click Scan List.";
            ScanStateLabel = "Input error";
            ScanStateMessage = "Target list scan was not started because no targets are loaded.";
            return;
        }

        var settings = _defaultScanSettings with
        {
            MaxHosts = Math.Max(targets.Length, _defaultScanSettings.MaxHosts),
            PingTimeoutMs = 220,
            TcpTimeoutMs = 450,
            MaxHostConcurrency = Math.Min(96, Math.Max(32, targets.Length)),
            MaxTcpConcurrency = 96,
            ProbeWebPortsWhenPingFails = true,
            EnableReverseDns = false
        };

        ScanInsightTitle = $"Scanning imported target list";
        ScanInsightMessage = $"Checking {targets.Length:n0} exact IP target(s) from {ImportedListTitle}. Expected devices stay visible even if they do not respond.";
        ScanAdvice = "This mode is intentionally exact-IP scanning. It avoids noisy subnet sweeps and is suitable for relay/server inventory verification.";
        ScanStateLabel = "Scanning list";
        ScanStateMessage = "Imported devices are visible first. Ping/protocol evidence will enrich each row progressively.";

        ClearDeviceState();
        ReplaceDeviceSnapshot(targets.Select(t => t.ToPendingSnapshot()).ToArray());
        SelectedDevice = VisibleDevices.FirstOrDefault();
        TotalHosts = targets.Length;
        CheckedHosts = 0;
        DiscoveredHosts = Devices.Count;
        CurrentAddress = "Starting target list scan";
        IsFinalizingScan = false;
        IsScanning = true;
        _lastProgressUi = DateTime.MinValue;
        _scanCancellation = new CancellationTokenSource();
        _deviceBatchTimer.Start();

        var targetMap = targets.ToDictionary(t => t.Ip, StringComparer.Ordinal);
        var progress = new Progress<ScanProgressInfo>(p =>
        {
            var now = DateTime.UtcNow;
            if (!p.IsCompleted && (now - _lastProgressUi).TotalMilliseconds < 100)
                return;

            _lastProgressUi = now;
            TotalHosts = p.TotalHosts;
            CheckedHosts = p.CheckedHosts;
            CurrentAddress = p.IsCompleted ? "Target list scan completed" : p.CurrentAddress;
            ScanStateMessage = p.IsCompleted
                ? "Target list scan completed. Marking unresolved expected devices."
                : $"Checking {p.CurrentAddress} · {p.CheckedHosts:n0}/{p.TotalHosts:n0} imported target(s)";
        });

        try
        {
            await _engine.ProbeTargetsAsync(targets.Select(t => t.IpAddress), settings, progress, EnqueueDevice, _scanCancellation.Token);
            var wasCancelled = _scanCancellation?.IsCancellationRequested == true;

            RunOnUi(() =>
            {
                IsFinalizingScan = true;
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                MarkUnresolvedExpectedTargetsNoResponse(targetMap);
                DiscoveredHosts = Devices.Count;
                RefreshCounters();

                var responsive = Devices.Count(d => d.Status is DeviceStatus.Online or DeviceStatus.PingOnly or DeviceStatus.PortOpenOnly or DeviceStatus.Slow);
                var missing = Devices.Count(d => d.Status is DeviceStatus.NoResponse or DeviceStatus.Offline or DeviceStatus.Pending);

                if (wasCancelled)
                {
                    ScanInsightTitle = "Target list scan stopped";
                    ScanInsightMessage = "User stopped the target list scan. Partial evidence is preserved; unresolved targets may remain pending.";
                    ScanStateLabel = "Stopped";
                    ScanStateMessage = $"Stopped. {responsive:n0} responsive target(s), {missing:n0} unresolved target(s).";
                }
                else
                {
                    ScanInsightTitle = $"Target list scan complete";
                    ScanInsightMessage = $"{targets.Length:n0} expected target(s) checked · {responsive:n0} responsive · {IndustrialCandidateCount:n0} protocol candidate(s) · {missing:n0} no response.";
                    ScanAdvice = "Select a no-response row to verify expected bay/panel/type, then check routing, VLAN, cable, device power, and target IP.";
                    ScanStateLabel = "Complete";
                    ScanStateMessage = $"Imported target scan complete. {responsive:n0} responsive, {missing:n0} no response.";
                }

                if (SelectedDevice is null && VisibleDevices.Count > 0)
                    SelectedDevice = VisibleDevices[0];
            });
        }
        catch (OperationCanceledException)
        {
            ScanStateLabel = "Stopped";
            ScanStateMessage = "Target list scan stopped by user.";
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), "Target list scan cancellation observed by UI."));
        }
        catch (Exception ex)
        {
            ScanInsightTitle = "Target list scan error handled";
            ScanInsightMessage = "An exception occurred during imported target scanning, but ARNet kept the app alive.";
            ScanStateLabel = "Error handled";
            ScanStateMessage = "Target list scan error was pushed to diagnostics.";
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(MainViewModel), "Target list scan failed but application remained alive.", ex));
        }
        finally
        {
            RunOnUi(() =>
            {
                _deviceBatchTimer.Stop();
                FlushPendingDevices();
                IsFinalizingScan = false;
                IsScanning = false;
                _scanCancellation?.Dispose();
                _scanCancellation = null;
                RaiseCommandStates();
            });
        }
    }

    private void MarkUnresolvedExpectedTargetsNoResponse(IReadOnlyDictionary<string, TargetDeviceRecord> targetMap)
    {
        var now = DateTimeOffset.Now;
        var changed = false;
        for (var i = 0; i < Devices.Count; i++)
        {
            var device = Devices[i];
            if (!targetMap.ContainsKey(device.Ip))
                continue;

            if (device.Status is DeviceStatus.Pending or DeviceStatus.Unknown or DeviceStatus.Offline)
            {
                var updated = device with
                {
                    Status = DeviceStatus.NoResponse,
                    IsProbeInProgress = false,
                    ProbeStage = "No response from quick ping or priority protocol ports",
                    LastSeen = now,
                    Evidence = "Expected target imported, but no ping reply and no known protocol TCP port responded. Check route/VLAN/cable/device power/IP address/firewall."
                };
                Devices[i] = updated;
                _deviceIndex[updated.Ip] = updated;
                if (SelectedDevice?.Ip == updated.Ip)
                    SelectedDevice = updated;
                changed = true;
            }
        }

        if (changed)
            RebuildVisibleDevices();
    }

    private void StopScan()
    {
        _scanCancellation?.Cancel();
        ScanStateLabel = "Stopping";
        ScanStateMessage = "Stop requested. Waiting for in-flight probes to finish or timeout.";
        _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), "Stop requested by user."));
    }

    private void EnqueueDevice(DiscoveredDeviceSnapshot device)
    {
        lock (_pendingLock)
        {
            _pendingDevices[device.Ip] = device;
        }
    }

    private void FlushPendingDevices()
    {
        List<DiscoveredDeviceSnapshot> batch;
        lock (_pendingLock)
        {
            if (_pendingDevices.Count == 0)
                return;

            batch = _pendingDevices.Values.ToList();
            _pendingDevices.Clear();
        }

        foreach (var device in batch)
            UpsertDeviceFast(device);

        DiscoveredHosts = Devices.Count;
        RebuildVisibleDevices();
        RefreshCounters();
    }

    private void ClearDeviceState()
    {
        lock (_pendingLock) _pendingDevices.Clear();
        _deviceIndex.Clear();
        Devices.Clear();
        VisibleDevices.Clear();
        SelectedDevice = null;
        FilteredDeviceCount = 0;
        ResultBufferMessage = "Table waiting for discovered devices.";
        RefreshCounters();
        OnPropertyChanged(nameof(HasVisibleDevices));
        OnPropertyChanged(nameof(HasHiddenBufferedDevices));
    }

    private void ReplaceDeviceSnapshot(IReadOnlyList<DiscoveredDeviceSnapshot> snapshot)
    {
        _deviceIndex.Clear();
        Devices.Clear();
        foreach (var device in snapshot)
        {
            _deviceIndex[device.Ip] = device;
            Devices.Add(device);
        }

        RebuildVisibleDevices();
        OnPropertyChanged(nameof(HasDevices));
        OnPropertyChanged(nameof(HasVisibleDevices));
    }

    private void UpsertDeviceFast(DiscoveredDeviceSnapshot device)
    {
        if (_deviceIndex.TryGetValue(device.Ip, out var existing))
            device = MergeExpectedMetadata(existing, device);

        if (_deviceIndex.ContainsKey(device.Ip))
        {
            _deviceIndex[device.Ip] = device;
            if (SelectedDevice?.Ip == device.Ip)
                SelectedDevice = device;

            for (var i = 0; i < Devices.Count; i++)
            {
                if (Devices[i].Ip == device.Ip)
                {
                    Devices[i] = device;
                    return;
                }
            }
        }

        _deviceIndex[device.Ip] = device;
        Devices.Add(device);
    }

    private static DiscoveredDeviceSnapshot MergeExpectedMetadata(DiscoveredDeviceSnapshot existing, DiscoveredDeviceSnapshot incoming)
    {
        if (!existing.IsExpectedTarget)
            return incoming;

        return incoming with
        {
            IsExpectedTarget = true,
            ExpectedDeviceName = existing.ExpectedDeviceName,
            ExpectedType = existing.ExpectedType,
            BayName = existing.BayName,
            BayType = existing.BayType,
            Panel = existing.Panel,
            DeviceNo = existing.DeviceNo,
            Remark = existing.Remark,
            TargetSource = existing.TargetSource,
            SourceRow = existing.SourceRow,
            HostName = !string.IsNullOrWhiteSpace(incoming.HostName) && incoming.HostName != incoming.Ip ? incoming.HostName : existing.ExpectedDeviceName ?? incoming.HostName,
            Kind = ShouldPreserveExpectedKind(existing, incoming) ? existing.Kind : incoming.Kind,
            Evidence = string.IsNullOrWhiteSpace(incoming.Evidence) ? existing.Evidence : incoming.Evidence
        };
    }


    private static bool ShouldPreserveExpectedKind(DiscoveredDeviceSnapshot existing, DiscoveredDeviceSnapshot incoming)
    {
        if (!existing.IsExpectedTarget)
            return false;

        var hasStrongProtocolEvidence = incoming.OpenPorts.Any(p => p.Port is 102 or 2404 or 502 or 20000 or 4840)
            || incoming.ProtocolTags.Any(t => t.Key is "IEC61850" or "IEC104" or "MODBUS" or "DNP3" or "OPCUA");

        if (hasStrongProtocolEvidence)
            return false;

        return incoming.Status is DeviceStatus.PingOnly or DeviceStatus.Online or DeviceStatus.Slow
            || incoming.Kind is DeviceKind.ServerOrWorkstation or DeviceKind.WebManagedDevice or DeviceKind.Unknown;
    }

    private void RebuildVisibleDevices()
    {
        var filtered = ApplyDeviceSort(Devices.Where(MatchesFilter))
            .Take(MaxVisibleRows)
            .ToArray();

        FilteredDeviceCount = Devices.Count(MatchesFilter);

        // Delta-update the visible table instead of Clear/Add every flush.
        // This keeps WPF virtualization smooth during progressive discovery.
        var indexByIp = new Dictionary<string, DiscoveredDeviceSnapshot>(StringComparer.Ordinal);
        foreach (var device in filtered)
            indexByIp[device.Ip] = device;

        for (var i = VisibleDevices.Count - 1; i >= 0; i--)
        {
            if (!indexByIp.ContainsKey(VisibleDevices[i].Ip))
                VisibleDevices.RemoveAt(i);
        }

        for (var targetIndex = 0; targetIndex < filtered.Length; targetIndex++)
        {
            var device = filtered[targetIndex];
            var currentIndex = -1;
            for (var i = 0; i < VisibleDevices.Count; i++)
            {
                if (VisibleDevices[i].Ip == device.Ip)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                var insertIndex = Math.Min(targetIndex, VisibleDevices.Count);
                VisibleDevices.Insert(insertIndex, device);
            }
            else
            {
                if (!ReferenceEquals(VisibleDevices[currentIndex], device) && !VisibleDevices[currentIndex].Equals(device))
                    VisibleDevices[currentIndex] = device;

                if (currentIndex != targetIndex && targetIndex < VisibleDevices.Count)
                    VisibleDevices.Move(currentIndex, targetIndex);
            }
        }

        ResultBufferMessage = HiddenDeviceCount > 0
            ? $"Showing {VisibleDevices.Count:n0}; {HiddenDeviceCount:n0} buffered for export."
            : VisibleDevices.Count > 0
                ? $"Showing all {VisibleDevices.Count:n0} filtered device(s)."
                : "No visible device matches the current filter.";

        OnPropertyChanged(nameof(HasDevices));
        OnPropertyChanged(nameof(HasVisibleDevices));
        OnPropertyChanged(nameof(HiddenDeviceCount));
        OnPropertyChanged(nameof(HasHiddenBufferedDevices));
        OnPropertyChanged(nameof(ResultBufferMessage));
    }

    private IOrderedEnumerable<DiscoveredDeviceSnapshot> ApplyDeviceSort(IEnumerable<DiscoveredDeviceSnapshot> devices)
    {
        return _sortColumn switch
        {
            DeviceSortColumn.Device => SortBy(devices, d => d.HostTitle),
            DeviceSortColumn.Ip => SortBy(devices, d => IpSortKey(d.Ip)),
            DeviceSortColumn.Expected => SortBy(devices, d => d.IsExpectedTarget ? d.ExpectedContextLabel : d.VendorDisplay),
            DeviceSortColumn.Protocols => SortBy(devices, d => d.ProtocolSummary),
            DeviceSortColumn.Ping => SortBy(devices, d => d.LatencyMs ?? d.TcpProbeMs ?? int.MaxValue),
            DeviceSortColumn.Status => SortBy(devices, d => DeviceOnlineRank(d)),
            _ => SortBy(devices, d => DeviceOnlineRank(d))
        };
    }

    private IOrderedEnumerable<DiscoveredDeviceSnapshot> SortBy<TKey>(IEnumerable<DiscoveredDeviceSnapshot> devices, Func<DiscoveredDeviceSnapshot, TKey> keySelector)
    {
        var sorted = _sortDescending
            ? devices.OrderByDescending(keySelector)
            : devices.OrderBy(keySelector);

        return sorted
            .ThenByDescending(DeviceRenderPriority)
            .ThenBy(d => IpSortKey(d.Ip));
    }

    private void SortDevices(object? parameter)
    {
        if (!Enum.TryParse<DeviceSortColumn>(parameter?.ToString(), true, out var column))
            return;

        if (_sortColumn == column)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = column;
            _sortDescending = column is DeviceSortColumn.Status or DeviceSortColumn.Protocols;
        }

        RebuildVisibleDevices();
        OnPropertyChanged(nameof(DeviceSortGlyph));
        OnPropertyChanged(nameof(IpSortGlyph));
        OnPropertyChanged(nameof(ExpectedSortGlyph));
        OnPropertyChanged(nameof(ProtocolSortGlyph));
        OnPropertyChanged(nameof(PingSortGlyph));
        OnPropertyChanged(nameof(StatusSortGlyph));
    }

    private string SortGlyph(DeviceSortColumn column)
        => _sortColumn != column ? string.Empty : _sortDescending ? "down" : "up";

    private bool SetGridLength(ref GridLength field, GridLength value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        var normalized = value.IsAbsolute
            ? new GridLength(Math.Clamp(value.Value, 72, 680))
            : value;

        if (field.Equals(normalized))
            return false;

        field = normalized;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void SelectDevice(DiscoveredDeviceSnapshot? device)
    {
        if (device is null) return;
        SelectedDevice = device;
    }

    private bool MatchesFilter(DiscoveredDeviceSnapshot d)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;

        var text = FilterText.Trim();
        return d.Ip.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (d.HostName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (d.Vendor?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (d.ExpectedDeviceName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (d.ExpectedType?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (d.Panel?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || (d.BayName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
            || d.KindLabel.Contains(text, StringComparison.OrdinalIgnoreCase)
            || d.ProtocolSummary.Contains(text, StringComparison.OrdinalIgnoreCase)
            || d.OpenPortsLabel.Contains(text, StringComparison.OrdinalIgnoreCase)
            || d.Status.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private void ExportCsv()
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ARNetDiscovery-Exports");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"arnet-discovery-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            var sb = new StringBuilder();
            sb.AppendLine("IP,Status,Kind,Hostname,ExpectedName,ExpectedType,Panel,BayName,BayType,DeviceNo,Remark,MAC,Vendor,LatencyMs,TcpMs,OpenPorts,Protocols,Evidence,Source,SourceRow");
            foreach (var d in Devices.OrderBy(d => d.Ip))
            {
                sb.AppendLine(string.Join(',',
                    Csv(d.Ip), Csv(d.Status.ToString()), Csv(d.KindLabel), Csv(d.HostName), Csv(d.ExpectedDeviceName), Csv(d.ExpectedType), Csv(d.Panel), Csv(d.BayName), Csv(d.BayType), Csv(d.DeviceNo), Csv(d.Remark),
                    Csv(d.MacAddress), Csv(d.Vendor), Csv(d.LatencyMs?.ToString()), Csv(d.TcpProbeMs?.ToString()), Csv(d.OpenPortsLabel), Csv(d.ProtocolSummary), Csv(d.Evidence), Csv(d.TargetSource), Csv(d.SourceRow?.ToString())));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), $"CSV exported: {path}"));
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(MainViewModel), "CSV export failed.", ex));
        }
    }

    private void CopySelectedIp()
    {
        if (SelectedDevice is null) return;
        try
        {
            Clipboard.SetText(SelectedDevice.Ip);
            _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), $"Copied IP {SelectedDevice.Ip} to clipboard."));
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Warning(nameof(MainViewModel), "Failed to copy IP to clipboard.", ex));
        }
    }

    private void LoadDemoSnapshot()
    {
        ClearDeviceState();
        var now = DateTimeOffset.Now;
        var samples = new[]
        {
            new DiscoveredDeviceSnapshot
            {
                IpAddress = System.Net.IPAddress.Parse("192.168.1.10"), HostName = "SIPROTEC-BAY01", MacAddress = "00:11:22:33:44:10", Vendor = "Siemens", Status = DeviceStatus.Online, Kind = DeviceKind.ProtectionRelay, LatencyMs = 2,
                OpenPorts = new[] { new OpenPortResult(102,"IEC61850","IEC 61850",true,2), new OpenPortResult(80,"WEB","Web",true,3) },
                ProtocolTags = new[] { new ProtocolTag("IEC61850","IEC 61850","#18A7C7",85), new ProtocolTag("WEB","Web UI","#00A1A7",65) },
                FirstSeen = now, LastSeen = now, Evidence = "Demo: Port 102 open; web interface detected."
            },
            new DiscoveredDeviceSnapshot
            {
                IpAddress = System.Net.IPAddress.Parse("192.168.1.20"), HostName = "GW-RTU-01", MacAddress = "00:11:22:33:44:20", Vendor = "Moxa", Status = DeviceStatus.PortOpenOnly, Kind = DeviceKind.Gateway, LatencyMs = null,
                OpenPorts = new[] { new OpenPortResult(2404,"IEC104","IEC 104",true,4), new OpenPortResult(502,"MODBUS","Modbus",true,4), new OpenPortResult(80,"WEB","Web",true,2) },
                ProtocolTags = new[] { new ProtocolTag("IEC104","IEC 104","#2F80ED",85), new ProtocolTag("MODBUS","Modbus","#8A63D2",78), new ProtocolTag("WEB","Web UI","#00A1A7",65) },
                FirstSeen = now, LastSeen = now, Evidence = "Demo: Ping blocked but protocol ports are open."
            },
            new DiscoveredDeviceSnapshot
            {
                IpAddress = System.Net.IPAddress.Parse("192.168.1.1"), HostName = "SW-CORE", MacAddress = "00:11:22:33:44:01", Vendor = "Hirschmann", Status = DeviceStatus.Online, Kind = DeviceKind.ManagedSwitch, LatencyMs = 1,
                OpenPorts = new[] { new OpenPortResult(161,"SNMP","SNMP",true,2), new OpenPortResult(22,"SSH","SSH",true,3), new OpenPortResult(443,"HTTPS","HTTPS",true,3) },
                ProtocolTags = new[] { new ProtocolTag("SNMP","SNMP","#27A35B",70), new ProtocolTag("SSH","SSH","#3C6E71",55), new ProtocolTag("WEB","Web UI","#00A1A7",65) },
                FirstSeen = now, LastSeen = now, Evidence = "Demo: SNMP + management ports; likely managed switch."
            },
            new DiscoveredDeviceSnapshot
            {
                IpAddress = System.Net.IPAddress.Parse("192.168.1.42"), HostName = "ENG-LAPTOP", MacAddress = "00:11:22:33:44:42", Vendor = "Lenovo", Status = DeviceStatus.PingOnly, Kind = DeviceKind.ServerOrWorkstation, LatencyMs = 1,
                OpenPorts = Array.Empty<OpenPortResult>(), ProtocolTags = Array.Empty<ProtocolTag>(), FirstSeen = now, LastSeen = now, Evidence = "Demo: Ping only, no known industrial ports."
            }
        };

        ReplaceDeviceSnapshot(samples);
        ScanInsightTitle = "Sample devices loaded";
        ScanInsightMessage = "Demo shows the intended clean table workflow: devices on the left, detailed protocol evidence on the right, diagnostics only for scan notes/errors.";
        ScanAdvice = "Use Scan Local for adapter subnet discovery, or Probe for a specific routed IP such as 1.110.5.1.";
        ScanStateLabel = "Demo";
        ScanStateMessage = "Demo snapshot loaded. No real network scan is running.";
        TotalHosts = 254;
        CheckedHosts = 254;
        DiscoveredHosts = Devices.Count;
        CurrentAddress = "Demo snapshot";
        SelectedDevice = VisibleDevices.FirstOrDefault();
        RefreshCounters();
        _diagnostics.Publish(DiagnosticEntry.Info(nameof(MainViewModel), "Demo snapshot loaded for visual validation."));
    }

    private void UpdateScanInsightForAdapter()
    {
        if (SelectedAdapter is null)
        {
            ScanInsightTitle = "No active adapter";
            ScanInsightMessage = "ARNet could not find an active IPv4 network adapter. Connect the engineering LAN cable or enable the NIC.";
            ScanAdvice = "Avoid VPN-only adapters for panel discovery unless the devices are reachable through that route.";
            ScanStateLabel = "No adapter";
            ScanStateMessage = "Connect or enable a network adapter.";
            return;
        }

        ScanInsightTitle = "Adapter selected";
        ScanInsightMessage = SubnetCalculator.GetAdapterRiskNote(SelectedAdapter.Address, SelectedAdapter.SubnetMask, SelectedAdapter.GatewayAddress);
        ScanAdvice = $"Scan range: {ScanRangeLabel}. If your relay IP is outside this range, use Direct Probe or Custom Range instead of local scan.";
        ScanStateLabel = "Ready";
        ScanStateMessage = "Ready to scan. Check that this is the NIC connected to the panel LAN.";
    }

    private void UpdateScanOutcome()
    {
        if (SelectedAdapter is null)
            return;

        if (Devices.Count > 0)
        {
            var low = LowConfidenceHostCount;
            var industrial = IndustrialCandidateCount;
            ScanInsightTitle = $"{Devices.Count:n0} host(s) buffered · {industrial:n0} industrial candidate(s)";
            ScanInsightMessage = low > 0
                ? $"ARNet found live hosts, but {low:n0} look low-confidence/web-only. Filter or inspect rows to separate real devices from routed/web noise."
                : "ARNet found live hosts by ICMP and/or open TCP protocol ports. Select a row to inspect evidence, ports, MAC/vendor, and protocol fingerprint.";
            ScanAdvice = !SubnetCalculator.IsPrivateIpv4(SelectedAdapter.Address)
                ? "This adapter uses a non-private IP range. If results look strange, this may be a routed/public network rather than isolated panel LAN. Verify NIC, cable, VLAN, and laptop mask."
                : "If an expected relay is missing, check whether it blocks ping and whether the relevant industrial port is open on this subnet/VLAN.";
            return;
        }

        var prefix = SelectedAdapter.PrefixLength;
        ScanInsightTitle = "No real device response detected";
        ScanInsightMessage = prefix < 24
            ? $"The selected adapter is /{prefix}, which is unusually broad for a panel LAN. ARNet scanned a safe local window only, not the whole /{prefix} network, to avoid freezing or flooding the network."
            : "No ICMP reply and no expected industrial TCP port was detected in the selected scan range.";
        ScanAdvice = "Most likely causes: wrong NIC selected, wrong VLAN/cable, laptop IP/mask not in the device subnet, target device powered off, or firewall blocks the probed ports. Try a direct cable, confirm relay IP/subnet, then scan again.";
    }

    private void RefreshCounters()
    {
        OnPropertyChanged(nameof(OnlineCount));
        OnPropertyChanged(nameof(RelayCandidateCount));
        OnPropertyChanged(nameof(GatewayCount));
        OnPropertyChanged(nameof(SwitchCount));
        OnPropertyChanged(nameof(IndustrialCandidateCount));
        OnPropertyChanged(nameof(LowConfidenceHostCount));
        OnPropertyChanged(nameof(ExpectedTargetCount));
        OnPropertyChanged(nameof(NoResponseCount));
        OnPropertyChanged(nameof(HasDevices));
        OnPropertyChanged(nameof(HasVisibleDevices));
        OnPropertyChanged(nameof(HiddenDeviceCount));
        OnPropertyChanged(nameof(HasHiddenBufferedDevices));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        (ScanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ProbeTargetCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ScanImportedTargetsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ExportCsvCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CopyIpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (LoadDemoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ImportTargetListCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static bool IsIndustrialCandidate(DiscoveredDeviceSnapshot device)
    {
        if (device.OpenPorts.Any(p => p.Port is 102 or 2404 or 502 or 20000 or 4840 or 161))
            return true;

        return device.ProtocolTags.Any(t => IndustrialProtocolKeys.Contains(t.Key) && t.Key is not "WEB" and not "WEBALT");
    }

    private static bool IsLikelyLowConfidenceHost(DiscoveredDeviceSnapshot device)
    {
        var open = device.OpenPorts.Where(p => p.IsOpen).Select(p => p.Port).ToArray();
        return device.Status == DeviceStatus.PortOpenOnly
            && string.IsNullOrWhiteSpace(device.MacAddress)
            && open.Length > 0
            && open.All(p => p is 80 or 443 or 8080);
    }

    private bool IsLocalAdapterAddress(IPAddress address)
        => Adapters.Any(a => a.Address.Equals(address));

    private static int DeviceRenderPriority(DiscoveredDeviceSnapshot device)
    {
        var score = 0;
        if (IsIndustrialCandidate(device)) score += 1000;
        if (device.Kind is DeviceKind.ProtectionRelay or DeviceKind.BayController) score += 600;
        if (device.Kind is DeviceKind.Gateway or DeviceKind.PlcOrController or DeviceKind.SerialServer) score += 480;
        if (device.Kind == DeviceKind.ManagedSwitch) score += 420;
        if (device.Status is DeviceStatus.Online or DeviceStatus.Slow) score += 240;
        if (device.Status == DeviceStatus.PingOnly) score += 160;
        if (!string.IsNullOrWhiteSpace(device.MacAddress)) score += 120;
        if (device.ProtocolTags.Count > 0) score += device.ProtocolTags.Max(t => t.Confidence);
        if (IsLikelyLowConfidenceHost(device)) score -= 500;
        return score;
    }

    private static int DeviceOnlineRank(DiscoveredDeviceSnapshot device)
        => device.Status switch
        {
            DeviceStatus.Online => 700,
            DeviceStatus.Slow => 650,
            DeviceStatus.PingOnly => 600,
            DeviceStatus.PortOpenOnly => 560,
            DeviceStatus.Pending => 300,
            DeviceStatus.Unknown => 220,
            DeviceStatus.Offline => 120,
            DeviceStatus.NoResponse => 80,
            _ => 0
        };

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }
    }

    private static uint IpSortKey(string ip)
    {
        if (!System.Net.IPAddress.TryParse(ip, out var address))
            return 0;

        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
            return 0;

        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }


    private static IEnumerable<IPAddress> ParseTargetInput(string input, int maxHosts)
    {
        if (string.IsNullOrWhiteSpace(input))
            yield break;

        var text = input.Trim();

        if (IPAddress.TryParse(text, out var single) && single.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            yield return single;
            yield break;
        }

        if (text.Contains('/', StringComparison.Ordinal))
        {
            var parts = text.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var baseIp) && int.TryParse(parts[1], out var prefix) && prefix is >= 0 and <= 32)
            {
                foreach (var ip in ExpandCidr(baseIp, prefix, maxHosts))
                    yield return ip;
            }
            yield break;
        }

        if (text.Contains('-', StringComparison.Ordinal))
        {
            var parts = text.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var start) && IPAddress.TryParse(parts[1], out var end))
            {
                var a = IpToUInt32(start);
                var b = IpToUInt32(end);
                if (a > b) (a, b) = (b, a);
                var count = 0;
                for (var current = a; current <= b && count < maxHosts; current++, count++)
                    yield return UInt32ToIp(current);
            }
        }
    }

    private static IEnumerable<IPAddress> ExpandCidr(IPAddress baseIp, int prefix, int maxHosts)
    {
        if (baseIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            yield break;

        var ip = IpToUInt32(baseIp);
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        var network = ip & mask;
        var broadcast = network | ~mask;
        var first = prefix >= 31 ? network : network + 1;
        var last = prefix >= 31 ? broadcast : broadcast - 1;
        var count = 0;
        for (var current = first; current <= last && count < maxHosts; current++, count++)
            yield return UInt32ToIp(current);
    }

    private static uint IpToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4) return 0;
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress UInt32ToIp(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return new IPAddress(bytes);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
