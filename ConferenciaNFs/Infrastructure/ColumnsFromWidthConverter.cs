using System.Globalization;
using System.Windows.Data;

namespace ConferenciaNFs.Infrastructure;

public sealed class ColumnsFromWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width)
            return 1;

        return width switch
        {
            < 500 => 1,
            < 760 => 2,
            < 1020 => 3,
            _ => 4
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
