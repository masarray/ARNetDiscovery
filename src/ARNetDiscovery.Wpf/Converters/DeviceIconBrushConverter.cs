using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Wpf.Converters;

public sealed class DeviceIconBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var mode = parameter as string ?? "Background";
        var category = ResolveCategory(value as DiscoveredDeviceSnapshot);

        return mode.Equals("Foreground", StringComparison.OrdinalIgnoreCase)
            ? Foreground(category)
            : Background(category);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    private static string ResolveCategory(DiscoveredDeviceSnapshot? device)
    {
        if (device is null)
            return "default";

        var hint = string.Join(" ", new[]
            {
                device.ExpectedType,
                device.ExpectedDeviceName,
                device.HostName,
                device.HostTitle,
                device.KindLabel,
                device.ProtocolSummary,
                device.Evidence
            }
            .Where(v => !string.IsNullOrWhiteSpace(v)))
            .ToLowerInvariant();

        if (hint.Contains("hmi") || hint.Contains("human machine interface") || hint.Contains("operator station") || hint.Contains("operator panel"))
            return "hmi";

        if (device.Kind is DeviceKind.ProtectionRelay or DeviceKind.BayController || hint.Contains("relay") || hint.Contains("protection"))
            return "relay";

        if (device.Kind is DeviceKind.ServerOrWorkstation or DeviceKind.SerialServer || hint.Contains("server") || hint.Contains("wincc") || hint.Contains("historian") || hint.Contains("nas"))
            return "server";

        return "default";
    }

    private static Brush Background(string category)
    {
        var (a, b, c) = category switch
        {
            "relay" => ("#F5FBFC", "#E7F7FB", "#DDF1F6"),
            "hmi" => ("#F7FBFF", "#EAF3FF", "#DFEAF8"),
            "server" => ("#FBFAFF", "#F0EDFA", "#E6E1F4"),
            _ => ("#F8FAFD", "#F2F6FA", "#E8EEF6")
        };

        return new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1),
            GradientStops =
            {
                new GradientStop(ColorFrom(a), 0),
                new GradientStop(ColorFrom(b), 0.58),
                new GradientStop(ColorFrom(c), 1)
            }
        };
    }

    private static Brush Foreground(string category)
        => new SolidColorBrush(ColorFrom(category switch
        {
            "relay" => "#087C92",
            "hmi" => "#2F5E9E",
            "server" => "#6657A8",
            _ => "#172033"
        }));

    private static Color ColorFrom(string value)
        => (Color)ColorConverter.ConvertFromString(value);
}
