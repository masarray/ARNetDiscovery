using System;
using System.Globalization;
using System.Windows.Data;

namespace ARNetDiscovery.Wpf.Converters
{
public sealed class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = value is double d ? d : 0;
        var max = parameter is string s && double.TryParse(s, out var p) ? p : 100.0;
        return Math.Max(0, Math.Min(max, max * percent / 100.0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
}
