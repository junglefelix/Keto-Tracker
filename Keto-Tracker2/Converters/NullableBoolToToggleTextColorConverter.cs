using System.Globalization;

namespace Keto_Tracker2.Converters;

public class NullableBoolToToggleTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool? current = value as bool?;
        bool isPassButton = parameter?.ToString() == "true";

        if (current == null)
            return Color.FromArgb("#757575");

        if (isPassButton && current == true)
            return Colors.White;
        if (!isPassButton && current == false)
            return Colors.White;

        return Color.FromArgb("#757575");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
