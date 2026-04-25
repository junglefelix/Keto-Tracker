using CommunityToolkit.Mvvm.ComponentModel;

namespace Keto_Tracker2.ViewModels;

public partial class DayCell : ObservableObject
{
    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private bool? _ketoPass;

    [ObservableProperty]
    private bool? _omadPass;

    [ObservableProperty]
    private bool _isToday;

    [ObservableProperty]
    private bool _isFuture;

    [ObservableProperty]
    private bool _isCurrentMonth;
}
