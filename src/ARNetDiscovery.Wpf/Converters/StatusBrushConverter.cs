using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ARNetDiscovery.Core.Models;

namespace ARNetDiscovery.Wpf.Converters;

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DeviceStatus.Pending => BrushFrom("#64748B"),
            DeviceStatus.Online => BrushFrom("#18A874"),
            DeviceStatus.PingOnly => BrushFrom("#3A7BD5"),
            DeviceStatus.PortOpenOnly => BrushFrom("#D99724"),
            DeviceStatus.Slow => BrushFrom("#D9A21B"),
            DeviceStatus.Offline => BrushFrom("#98A2B3"),
            DeviceStatus.NoResponse => BrushFrom("#B94A48"),
            _ => BrushFrom("#98A2B3")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    private static SolidColorBrush BrushFrom(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
