using CommunityToolkit.Mvvm.ComponentModel;
using Keto_Tracker2.Models;

namespace Keto_Tracker2.ViewModels;

public partial class KetoneEntryViewModel : ObservableObject
{
    public KetoneEntry Model { get; }

    [ObservableProperty]
    private int _selectedTimeIndex;

    [ObservableProperty]
    private string _valueText;

    public List<string> TimeSlots => MealEntryViewModel.AllTimeSlots;

    public KetoneEntryViewModel(KetoneEntry model)
    {
        Model = model;
        _valueText = model.Value == 0 ? string.Empty : model.Value.ToString("0.##");
        int totalMins = (int)model.TimeOfDay.TotalMinutes;
        int rounded = (totalMins / 30) * 30;
        _selectedTimeIndex = Math.Clamp(rounded / 30, 0, MealEntryViewModel.AllTimeSlots.Count - 1);
    }

    public void SyncToModel()
    {
        Model.Value = double.TryParse(ValueText, out double v) ? v : 0;
        int totalMins = SelectedTimeIndex * 30;
        Model.TimeOfDay = TimeSpan.FromMinutes(totalMins);
    }
}
