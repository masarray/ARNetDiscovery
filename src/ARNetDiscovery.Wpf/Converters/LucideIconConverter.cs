using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Wpf.Converters;

public sealed class LucideIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = parameter as string
            ?? value switch
            {
                DeviceKind kind => KindToKey(kind),
                string text => text,
                _ => "device"
            };

        return Geometry.Parse(DataFor(key));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    private static string KindToKey(DeviceKind kind) => kind switch
    {
        DeviceKind.ProtectionRelay => "shield",
        DeviceKind.BayController => "sliders",
        DeviceKind.Gateway => "router",
        DeviceKind.ManagedSwitch => "network",
        DeviceKind.PlcOrController => "cpu",
        DeviceKind.Meter => "gauge",
        DeviceKind.SerialServer => "server",
        DeviceKind.EngineeringLaptop => "laptop",
        DeviceKind.ServerOrWorkstation => "server",
        DeviceKind.WebManagedDevice => "globe",
        _ => "device"
    };

    private static string DataFor(string key) => key.Trim().ToLowerInvariant() switch
    {
        "activity" => "M22,12 L18,12 L15,21 L9,3 L6,12 L2,12",
        "bolt" => "M13,2 L3,14 L11,14 L9,22 L21,10 L13,10 Z",
        "check" => "M20,6 L9,17 L4,12",
        "copy" => "M8,8 L19,8 L19,19 L8,19 Z M5,16 L5,5 L16,5",
        "cpu" => "M8,8 L16,8 L16,16 L8,16 Z M4,9 L2,9 M4,15 L2,15 M9,4 L9,2 M15,4 L15,2 M20,9 L22,9 M20,15 L22,15 M9,20 L9,22 M15,20 L15,22",
        "database" => "M4,6 C4,3.8 7.6,2 12,2 C16.4,2 20,3.8 20,6 C20,8.2 16.4,10 12,10 C7.6,10 4,8.2 4,6 Z M4,6 L4,18 C4,20.2 7.6,22 12,22 C16.4,22 20,20.2 20,18 L20,6 M4,12 C4,14.2 7.6,16 12,16 C16.4,16 20,14.2 20,12",
        "download" => "M12,3 L12,15 M7,10 L12,15 L17,10 M5,21 L19,21",
        "gauge" => "M4,14 A8,8 0 0 1 20,14 M12,14 L16,10 M6,20 L18,20",
        "globe" => "M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 M2,12 L22,12 M12,2 C15,5 16,9 16,12 C16,15 15,19 12,22 M12,2 C9,5 8,9 8,12 C8,15 9,19 12,22",
        "import" => "M12,3 L12,15 M7,10 L12,15 L17,10 M4,21 L20,21 M4,17 L4,21 M20,17 L20,21",
        "laptop" => "M4,5 L20,5 L20,15 L4,15 Z M2,19 L22,19 L20,15 L4,15 Z",
        "network" => "M12,3 L12,9 M6,15 L6,21 M18,15 L18,21 M12,9 L6,15 M12,9 L18,15 M4,21 L8,21 L8,17 L4,17 Z M16,21 L20,21 L20,17 L16,17 Z M10,7 L14,7 L14,3 L10,3 Z",
        "play" => "M8,5 L19,12 L8,19 Z",
        "router" => "M5,12 L19,12 A2,2 0 0 1 21,14 L21,18 A2,2 0 0 1 19,20 L5,20 A2,2 0 0 1 3,18 L3,14 A2,2 0 0 1 5,12 Z M7,16 L7,16.1 M11,16 L11,16.1 M8,9 C10.2,7.7 13.8,7.7 16,9 M5,6 C9,3.7 15,3.7 19,6",
        "search" => "M21,21 L16.65,16.65 M11,19 A8,8 0 1 1 11,3 A8,8 0 1 1 11,19",
        "server" => "M4,4 L20,4 L20,10 L4,10 Z M4,14 L20,14 L20,20 L4,20 Z M8,7 L8,7.1 M8,17 L8,17.1",
        "shield" => "M12,22 C17,19.5 20,15.5 20,9 L20,5 L12,2 L4,5 L4,9 C4,15.5 7,19.5 12,22 Z",
        "sliders" => "M4,21 L4,14 M4,10 L4,3 M12,21 L12,12 M12,8 L12,3 M20,21 L20,16 M20,12 L20,3 M2,14 L6,14 M10,8 L14,8 M18,16 L22,16",
        "square" => "M5,5 L19,5 L19,19 L5,19 Z",
        "upload" => "M12,21 L12,9 M7,14 L12,9 L17,14 M5,3 L19,3",
        "wifi" => "M5,13 C9,9.5 15,9.5 19,13 M8.5,16.5 C10.5,14.8 13.5,14.8 15.5,16.5 M12,20 L12,20.1",
        _ => "M5,4 L19,4 L19,20 L5,20 Z M9,8 L15,8 M9,12 L15,12 M9,16 L13,16"
    };
}
