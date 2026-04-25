using System.Globalization;

namespace Keto_Tracker2.Converters;

public class KetoPassToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool)
            return Colors.White;
        return Color.FromArgb("#757575");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
