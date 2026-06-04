namespace ARNetDiscovery.Core.Models;

public sealed record ScanSettings
{
    // Field-safe defaults: fast enough for /24 panel LAN, gentle enough for old industrial switches.
    public int PingTimeoutMs { get; init; } = 200;
    public int TcpTimeoutMs { get; init; } = 350;
    public int MaxHostConcurrency { get; init; } = 96;
    public int MaxTcpConcurrency { get; init; } = 96;
    public int MaxHosts { get; init; } = 254;

    // Huge masks such as /8 often appear on misconfigured engineering laptops.
    // Do not scan the entire routed class-A space. Anchor to the laptop's local /24 window first.
    public int SmartLocalWindowPrefix { get; init; } = 24;
    public int UseLocalWindowWhenPrefixIsShorterThan { get; init; } = 24;

    public bool ProbeTcpEvenWhenPingFails { get; init; } = true;
    public bool ProbeOnlyCriticalPortsWhenPingFails { get; init; } = true;

    // Default is intentionally conservative. Web ports on public/routed networks often create
    // noisy false positives. ARNet scans web ports after a host proves it is alive by ICMP,
    // but ping-blocked hosts are probed on industrial ports only unless this is explicitly enabled.
    public bool ProbeWebPortsWhenPingFails { get; init; } = false;

    // Reverse DNS is intentionally disabled in the fast scan path. It can be slow on isolated panel LANs.
    public bool EnableReverseDns { get; init; } = false;

    public IReadOnlyList<ScanPortDefinition> Ports { get; init; } = DefaultPorts;

    public static IReadOnlyList<int> CriticalPingBlockedPorts { get; } = new[]
    {
        102,    // IEC 61850
        2404,   // IEC 104
        502,    // Modbus TCP
        20000,  // DNP3 TCP
        4840    // OPC UA
    };

    public static IReadOnlyList<ScanPortDefinition> DefaultPorts { get; } = new[]
    {
        new ScanPortDefinition(102, "IEC61850", "IEC 61850", "MMS / ISO-on-TCP. Common for substation IEDs and bay controllers.", 10),
        new ScanPortDefinition(2404, "IEC104", "IEC 104", "IEC 60870-5-104 RTU/gateway endpoint.", 20),
        new ScanPortDefinition(502, "MODBUS", "Modbus", "Modbus TCP PLC, meter, controller, or gateway.", 30),
        new ScanPortDefinition(20000, "DNP3", "DNP3", "DNP3 TCP outstation endpoint.", 40),
        new ScanPortDefinition(4840, "OPCUA", "OPC UA", "OPC UA server endpoint.", 50),
        // SNMP is UDP/161 in normal deployments. TCP/161 is not used as default evidence.
        new ScanPortDefinition(80, "WEB", "Web", "HTTP web interface.", 70),
        new ScanPortDefinition(443, "HTTPS", "HTTPS", "HTTPS web interface.", 80),
        new ScanPortDefinition(22, "SSH", "SSH", "SSH management access.", 90),
        new ScanPortDefinition(23, "TELNET", "Telnet", "Legacy Telnet management access.", 100),
        new ScanPortDefinition(8080, "WEBALT", "Web Alt", "Alternative web port.", 110),
    };
}
