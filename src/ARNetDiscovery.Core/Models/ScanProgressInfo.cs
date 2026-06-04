namespace ARNetDiscovery.Core.Models;

public sealed record ScanProgressInfo(
    int TotalHosts,
    int CheckedHosts,
    int DiscoveredHosts,
    string CurrentAddress,
    bool IsCompleted)
{
    public double Percent => TotalHosts <= 0 ? 0 : Math.Clamp((double)CheckedHosts / TotalHosts * 100.0, 0, 100);
}
