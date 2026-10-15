using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Calendar = System.Windows.Controls.Calendar;

namespace ConferenciaNFs.Infrastructure;

public static class CalendarRange
{
    public static readonly DependencyProperty StartProperty = DependencyProperty.RegisterAttached(
        "Start",
        typeof(DateTime?),
        typeof(CalendarRange),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnRangeChanged));

    public static readonly DependencyProperty EndProperty = DependencyProperty.RegisterAttached(
        "End",
        typeof(DateTime?),
        typeof(CalendarRange),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnRangeChanged));

    public static DateTime? GetStart(DependencyObject obj) => (DateTime?)obj.GetValue(StartProperty);

    public static void SetStart(DependencyObject obj, DateTime? value) => obj.SetValue(StartProperty, value);

    public static DateTime? GetEnd(DependencyObject obj) => (DateTime?)obj.GetValue(EndProperty);

    public static void SetEnd(DependencyObject obj, DateTime? value) => obj.SetValue(EndProperty, value);

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker picker)
            return;

        picker.CalendarOpened -= PickerOnCalendarOpened;
        picker.CalendarOpened += PickerOnCalendarOpened;
        CopiarParaCalendario(picker);
    }

    private static void PickerOnCalendarOpened(object sender, RoutedEventArgs e)
    {
        if (sender is DatePicker picker)
            CopiarParaCalendario(picker);
    }

    private static void CopiarParaCalendario(DatePicker picker)
    {
        picker.Dispatcher.BeginInvoke(() =>
        {
            Calendar? calendar = null;
            if (picker.Template?.FindName("PART_Popup", picker) is Popup popup)
                calendar = popup.Child as Calendar ?? FindCalendar(popup.Child);

            if (calendar is null)
                return;

            SetStart(calendar, GetStart(picker));
            SetEnd(calendar, GetEnd(picker));
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private static Calendar? FindCalendar(DependencyObject? root)
    {
        switch (root)
        {
            case null:
                return null;
            case Calendar calendar:
                return calendar;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var found = FindCalendar(VisualTreeHelper.GetChild(root, i));
            if (found is not null)
                return found;
        }

        return null;
    }
}

public sealed class CalendarDayKindConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[0] is not DateTime dayRaw)
            return "None";

        var day = dayRaw.Date;
        var start = ToDate(values[1]);
        var end = ToDate(values[2]);

        if (start is null && end is null)
            return day == DateTime.Today ? "Today" : "None";

        start ??= end;
        end ??= start;
        if (start > end)
            (start, end) = (end, start);

        if (start == end && day == start)
            return "Single";
        if (day == start)
            return "Start";
        if (day == end)
            return "End";
        if (day > start && day < end)
            return "Middle";
        return day == DateTime.Today ? "Today" : "None";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static DateTime? ToDate(object? value)
    {
        if (value is DateTime dt)
            return dt.Date;
        return null;
    }
}
