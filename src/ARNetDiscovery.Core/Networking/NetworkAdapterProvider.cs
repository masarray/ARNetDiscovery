using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ARNetDiscovery.Core.Diagnostics;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Networking;

public sealed class NetworkAdapterProvider
{
    private readonly IDiagnosticSink _diagnostics;

    public NetworkAdapterProvider(IDiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? NullDiagnosticSink.Instance;
    }

    public IReadOnlyList<NetworkAdapterInfo> GetActiveIpv4Adapters()
    {
        try
        {
            var adapters = new List<NetworkAdapterInfo>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var props = nic.GetIPProperties();
                var unicast = props.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.IPv4Mask is not null);

                if (unicast is null)
                    continue;

                if (IPAddress.IsLoopback(unicast.Address))
                    continue;

                var gateway = props.GatewayAddresses
                    .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

                var prefix = SubnetCalculator.PrefixLengthFromMask(unicast.IPv4Mask!);

                adapters.Add(new NetworkAdapterInfo(
                    nic.Id,
                    nic.Name,
                    nic.Description,
                    unicast.Address,
                    unicast.IPv4Mask!,
                    prefix,
                    gateway,
                    nic.NetworkInterfaceType.ToString(),
                    nic.Speed));
            }

            return adapters
                .OrderByDescending(a => a.GatewayAddress is not null)
                .ThenByDescending(a => a.SpeedBitsPerSecond)
                .ToArray();
        }
        catch (Exception ex)
        {
            _diagnostics.Publish(DiagnosticEntry.Error(nameof(NetworkAdapterProvider), "Failed to enumerate network adapters.", ex));
            return Array.Empty<NetworkAdapterInfo>();
        }
    }
}
