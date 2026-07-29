using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ConferenciaNFs.Infrastructure;

public sealed class WidthThresholdToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width)
            return Visibility.Collapsed;

        if (!double.TryParse(parameter?.ToString(), NumberStyles.Any, culture, out var threshold))
            threshold = 900;

        return width >= threshold ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
