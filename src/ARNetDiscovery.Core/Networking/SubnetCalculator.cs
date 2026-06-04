using System.Net;
using System.Net.Sockets;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Core.Networking;

public static class SubnetCalculator
{
    public static int PrefixLengthFromMask(IPAddress mask)
    {
        if (mask.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Only IPv4 masks are supported.", nameof(mask));

        var bytes = mask.GetAddressBytes();
        var count = 0;
        foreach (var b in bytes)
        {
            var value = b;
            for (var bit = 7; bit >= 0; bit--)
            {
                if ((value & (1 << bit)) != 0) count++;
            }
        }

        return count;
    }

    public static IReadOnlyList<IPAddress> GetSmartUsableHosts(IPAddress address, IPAddress subnetMask, ScanSettings settings)
    {
        var prefix = PrefixLengthFromMask(subnetMask);

        if (prefix < settings.UseLocalWindowWhenPrefixIsShorterThan)
        {
            var anchoredMask = MaskFromPrefix(settings.SmartLocalWindowPrefix);
            return GetUsableHosts(address, anchoredMask, settings.MaxHosts, centerOnAddress: false);
        }

        return GetUsableHosts(address, subnetMask, settings.MaxHosts, centerOnAddress: true);
    }

    public static IReadOnlyList<IPAddress> GetUsableHosts(IPAddress address, IPAddress subnetMask, int maxHosts)
        => GetUsableHosts(address, subnetMask, maxHosts, centerOnAddress: true);

    public static IReadOnlyList<IPAddress> GetUsableHosts(IPAddress address, IPAddress subnetMask, int maxHosts, bool centerOnAddress)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || subnetMask.AddressFamily != AddressFamily.InterNetwork)
            return Array.Empty<IPAddress>();

        var ip = ToUInt32(address);
        var mask = ToUInt32(subnetMask);
        var network = ip & mask;
        var broadcast = network | ~mask;

        if (broadcast <= network)
            return Array.Empty<IPAddress>();

        var first = network + 1;
        var last = broadcast - 1;
        if (last < first)
            return Array.Empty<IPAddress>();

        var count = last - first + 1;
        if (count > maxHosts)
        {
            if (centerOnAddress)
            {
                var halfWindow = (uint)Math.Max(1, maxHosts / 2);
                var focusedFirst = ip > halfWindow ? ip - halfWindow : first;
                focusedFirst = Math.Max(first, Math.Min(focusedFirst, last));
                var focusedLast = Math.Min(last, focusedFirst + (uint)maxHosts - 1);
                if (focusedLast - focusedFirst + 1 < maxHosts && focusedLast == last)
                    focusedFirst = focusedLast > (uint)maxHosts ? focusedLast - (uint)maxHosts + 1 : first;
                first = focusedFirst;
                last = focusedLast;
            }
            else
            {
                last = Math.Min(last, first + (uint)maxHosts - 1);
            }
        }

        var result = new List<IPAddress>((int)Math.Min(maxHosts, last - first + 1));
        for (var current = first; current <= last && result.Count < maxHosts; current++)
        {
            result.Add(FromUInt32(current));
        }

        return result;
    }

    public static string GetRangeLabel(IPAddress address, IPAddress subnetMask, int maxHosts)
    {
        var hosts = GetUsableHosts(address, subnetMask, maxHosts);
        if (hosts.Count == 0) return "No scan range";
        return $"{hosts[0]} - {hosts[^1]}";
    }

    public static string GetSmartRangeLabel(IPAddress address, IPAddress subnetMask, ScanSettings settings)
    {
        var prefix = PrefixLengthFromMask(subnetMask);
        var hosts = GetSmartUsableHosts(address, subnetMask, settings);
        if (hosts.Count == 0) return "No scan range";

        var smartNote = prefix < settings.UseLocalWindowWhenPrefixIsShorterThan
            ? $" · smart /{settings.SmartLocalWindowPrefix} window because adapter is /{prefix}"
            : string.Empty;

        return $"{hosts[0]} - {hosts[^1]} ({hosts.Count:n0} hosts{smartNote})";
    }

    public static string GetAdapterRiskNote(IPAddress address, IPAddress subnetMask, IPAddress? gateway)
    {
        var prefix = PrefixLengthFromMask(subnetMask);
        var privateRange = IsPrivateIpv4(address);

        if (!privateRange)
            return $"Adapter IP {address}/{prefix} is not an RFC1918 private LAN address. For panel discovery this often means wrong NIC, public/WAN routing, VPN, or unusual plant addressing. ARNet will suppress web-only noise and scan conservatively.";

        if (prefix < 16)
            return $"Adapter subnet /{prefix} is very broad. ARNet will not scan the full network; it uses a safe local window around {address}.";

        if (prefix < 24)
            return $"Adapter subnet /{prefix} is wider than common panel LAN /24. Scan is capped for safety; use the correct engineering adapter/VLAN if expected devices are missing.";

        if (gateway is null)
            return "No IPv4 gateway detected. This is normal for isolated panel LAN, but unreachable replies usually mean wrong cable/VLAN/IP range.";

        return "Adapter looks usable for local discovery.";
    }

    public static bool IsPrivateIpv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var b = address.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254);
    }

    public static IPAddress MaskFromPrefix(int prefixLength)
    {
        if (prefixLength is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "IPv4 prefix must be between 0 and 32.");

        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return FromUInt32(mask);
    }

    private static uint ToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress FromUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return new IPAddress(bytes);
    }
}
