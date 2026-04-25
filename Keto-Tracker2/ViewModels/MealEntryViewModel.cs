using CommunityToolkit.Mvvm.ComponentModel;
using Keto_Tracker2.Models;

namespace Keto_Tracker2.ViewModels;

public partial class MealEntryViewModel : ObservableObject
{
    public MealEntry Model { get; }

    [ObservableProperty]
    private int _selectedTimeIndex;

    [ObservableProperty]
    private string _description;

    public static List<string> AllTimeSlots { get; } = GenerateTimeSlots();
    public List<string> TimeSlots => AllTimeSlots;

    private static List<string> GenerateTimeSlots()
    {
        var slots = new List<string>();
        for (int h = 0; h < 24; h++)
            for (int m = 0; m < 60; m += 30)
                slots.Add($"{h:D2}:{m:D2}");
        return slots;
    }

    public MealEntryViewModel(MealEntry model)
    {
        Model = model;
        _description = model.Description;
        int totalMins = (int)model.TimeOfDay.TotalMinutes;
        int rounded = (totalMins / 30) * 30;
        _selectedTimeIndex = Math.Clamp(rounded / 30, 0, AllTimeSlots.Count - 1);
    }

    public void SyncToModel()
    {
        Model.Description = Description ?? string.Empty;
        int totalMins = SelectedTimeIndex * 30;
        Model.TimeOfDay = TimeSpan.FromMinutes(totalMins);
    }
}
