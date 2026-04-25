using System.Globalization;

namespace Keto_Tracker2.Converters;

public class NullableBoolToToggleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool? current = value as bool?;
        bool isPassButton = parameter?.ToString() == "true";

        if (current == null)
            return Color.FromArgb("#E0E0E0");

        if (isPassButton && current == true)
            return Color.FromArgb("#4CAF50");
        if (!isPassButton && current == false)
            return Color.FromArgb("#F44336");

        return Color.FromArgb("#E0E0E0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
