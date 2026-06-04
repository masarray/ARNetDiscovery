using System.Net;

namespace ARNetDiscovery.Core.Models;

public sealed record NetworkAdapterInfo(
    string Id,
    string Name,
    string Description,
    IPAddress Address,
    IPAddress SubnetMask,
    int PrefixLength,
    IPAddress? GatewayAddress,
    string InterfaceType,
    long SpeedBitsPerSecond)
{
    public string DisplayName => $"{Name}  •  {Address}/{PrefixLength}";
    public string SpeedLabel => SpeedBitsPerSecond <= 0 ? "Unknown" : $"{SpeedBitsPerSecond / 1_000_000:n0} Mbps";
}
