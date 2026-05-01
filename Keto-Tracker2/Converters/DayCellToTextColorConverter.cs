using System.Globalization;
using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Converters;

public class DayCellToTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DayCell cell && (cell.KetoPass.HasValue || cell.HasData))
            return Colors.White;
        return Color.FromArgb("#757575");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
