using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ARNetDiscovery.Core.Diagnostics;

namespace ARNetDiscovery.Wpf.Converters
{
public sealed class SeverityBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DiagnosticSeverity.Error => BrushFrom("#D92D20"),
            DiagnosticSeverity.Warning => BrushFrom("#B7791F"),
            _ => BrushFrom("#2775A3")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    private static SolidColorBrush BrushFrom(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
}
