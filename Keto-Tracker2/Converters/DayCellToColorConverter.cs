using System.Globalization;
using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Converters;

public class DayCellToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DayCell cell)
        {
            if (cell.KetoPass == true)  return Color.FromArgb("#4CAF50");
            if (cell.KetoPass == false) return Color.FromArgb("#F44336");
            if (cell.HasData)           return Color.FromArgb("#5C6BC0"); // indigo – data but no keto verdict
        }
        return Color.FromArgb("#BDBDBD");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
