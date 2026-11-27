using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keto_Tracker2.Services;

namespace Keto_Tracker2.ViewModels;

public partial class WeightTrendViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private List<(DateTime Date, double Weight)> _allPoints = [];

    [ObservableProperty]
    private WeightChartDrawable _chart = new();

    [ObservableProperty]
    private bool _hasWeightData;

    [ObservableProperty]
    private string _selectedRange = "All";

    public bool Is30DSelected => SelectedRange == "30D";
    public bool Is90DSelected => SelectedRange == "90D";
    public bool Is1YSelected => SelectedRange == "1Y";
    public bool IsAllSelected => SelectedRange == "All";

    public WeightTrendViewModel(DatabaseService db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        var entries = await _db.GetAllWeightEntriesAsync();
        _allPoints = entries
            .Where(e => e.WeightKg.HasValue)
            .Select(e => (e.Date, e.WeightKg!.Value))
            .ToList();

        HasWeightData = _allPoints.Count > 0;
        ApplyRange();
    }

    [RelayCommand]
    private void SelectRange(string range)
    {
        if (SelectedRange == range) return;
        SelectedRange = range;
        ApplyRange();
    }

    partial void OnSelectedRangeChanged(string value)
    {
        OnPropertyChanged(nameof(Is30DSelected));
        OnPropertyChanged(nameof(Is90DSelected));
        OnPropertyChanged(nameof(Is1YSelected));
        OnPropertyChanged(nameof(IsAllSelected));
    }

    private void ApplyRange()
    {
        var today = DateTime.Today;
        var filtered = SelectedRange switch
        {
            "30D" => _allPoints.Where(p => p.Date.Date >= today.AddDays(-29)).ToList(),
            "90D" => _allPoints.Where(p => p.Date.Date >= today.AddDays(-89)).ToList(),
            "1Y" => _allPoints.Where(p => p.Date.Date >= today.AddDays(-364)).ToList(),
            _ => _allPoints
        };

        Chart = new WeightChartDrawable { Points = filtered };
    }
}
