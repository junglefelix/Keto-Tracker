using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keto_Tracker2.Models;
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

    [ObservableProperty]
    private int _ketoStreakDays;

    [ObservableProperty]
    private int _omadStreakDays;

    [ObservableProperty]
    private WeightChartDrawable _weightChart = new();

    [ObservableProperty]
    private bool _hasWeightData;

    [ObservableProperty]
    private bool _hasAnyWeightData;

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
                IsCurrentMonth = true,
                HasData = entry is not null && (
                    entry.KetoPass.HasValue ||
                    entry.OmadPass.HasValue ||
                    entry.WeightKg.HasValue ||
                    !string.IsNullOrEmpty(entry.Notes))
            });
        }

        Days = cells;
        await ComputeStatsAsync();
    }

    private async Task ComputeStatsAsync()
    {
        var today = DateTime.Today;
        var from = today.AddDays(-1095); // look back up to 3 years
        var allEntries = await _db.GetDayEntriesInRangeAsync(from, today);
        var entryDict = allEntries.ToDictionary(e => e.Date.Date);

        KetoStreakDays = ComputeStreak(entryDict, today.AddDays(-1), e => e.KetoPass);
        OmadStreakDays = ComputeStreak(entryDict, today.AddDays(-1), e => e.OmadPass);

        HasAnyWeightData = allEntries.Any(e => e.WeightKg.HasValue);

        // Dashboard chart only shows a rolling 30-day window; full history lives on WeightTrendPage.
        var chartFrom = today.AddDays(-29);
        var weightPoints = allEntries
            .Where(e => e.WeightKg.HasValue && e.Date.Date >= chartFrom)
            .OrderBy(e => e.Date)
            .Select(e => (e.Date, e.WeightKg!.Value))
            .ToList();

        HasWeightData = weightPoints.Count > 0;
        WeightChart = new WeightChartDrawable
        {
            Points = weightPoints,
            RangeStart = chartFrom,
            RangeEnd = today
        };
    }

    private static int ComputeStreak(
        Dictionary<DateTime, DayEntry> entries,
        DateTime from,
        Func<DayEntry, bool?> selector)
    {
        int streak = 0;
        for (var date = from; date >= from.AddDays(-1095); date = date.AddDays(-1))
        {
            if (entries.TryGetValue(date.Date, out var entry) && selector(entry) == true)
                streak++;
            else
                break;
        }
        return streak;
    }

    public async Task RefreshAsync()
    {
        await LoadMonthAsync(_currentMonth);
    }
}
