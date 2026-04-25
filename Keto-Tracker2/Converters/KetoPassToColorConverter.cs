using System.Globalization;

namespace Keto_Tracker2.Converters;

public class KetoPassToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Color.FromArgb("#4CAF50") : Color.FromArgb("#F44336");
        return Color.FromArgb("#BDBDBD");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
