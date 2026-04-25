using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keto_Tracker2.Services;
using System.Collections.ObjectModel;

namespace Keto_Tracker2.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    [ObservableProperty]
    private string _monthLabel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DayCell> _days = new();

    private DateTime _currentMonth;

    public CalendarViewModel(DatabaseService db)
    {
        _db = db;
        _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    public async Task LoadAsync()
    {
        await LoadMonthAsync(_currentMonth);
    }

    [RelayCommand]
    private async Task PrevMonth()
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        await LoadMonthAsync(_currentMonth);
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        _currentMonth = _currentMonth.AddMonths(1);
        await LoadMonthAsync(_currentMonth);
    }

    private async Task LoadMonthAsync(DateTime month)
    {
        MonthLabel = month.ToString("MMMM yyyy");
        var entries = await _db.GetDayEntriesForMonthAsync(month.Year, month.Month);
        var entryDict = entries.ToDictionary(e => e.Date.Date);

        var cells = new ObservableCollection<DayCell>();
        var today = DateTime.Today;

        var firstDay = new DateTime(month.Year, month.Month, 1);
        var offset = (int)firstDay.DayOfWeek;

        for (int i = 0; i < offset; i++)
            cells.Add(new DayCell { Date = DateTime.MinValue, IsCurrentMonth = false, IsFuture = true });

        int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(month.Year, month.Month, d);
            var isFuture = date > today;
            entryDict.TryGetValue(date.Date, out var entry);

            cells.Add(new DayCell
            {
                Date = date,
                KetoPass = entry?.KetoPass,
                OmadPass = entry?.OmadPass,
                IsToday = date == today,
                IsFuture = isFuture,
                IsCurrentMonth = true
            });
        }

        Days = cells;
    }

    public async Task RefreshAsync()
    {
        await LoadMonthAsync(_currentMonth);
    }
}
