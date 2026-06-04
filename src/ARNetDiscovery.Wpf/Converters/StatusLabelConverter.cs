using System;
using System.Globalization;
using System.Windows.Data;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Wpf.Converters;

public sealed class StatusLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DeviceStatus.Pending => "Pending",
            DeviceStatus.Online => "Online",
            DeviceStatus.PingOnly => "Ping only",
            DeviceStatus.PortOpenOnly => "Port open",
            DeviceStatus.Slow => "Slow",
            DeviceStatus.Offline => "Offline",
            DeviceStatus.NoResponse => "No response",
            _ => "Unknown"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
