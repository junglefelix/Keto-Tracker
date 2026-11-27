using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Keto_Tracker2.Models;
using Keto_Tracker2.Services;
using System.Collections.ObjectModel;

namespace Keto_Tracker2.ViewModels;

[QueryProperty(nameof(DateTicks), "dateTicks")]
public partial class DayDetailViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private DayEntry? _dayEntry;

    [ObservableProperty]
    private string _dateLabel = string.Empty;

    [ObservableProperty]
    private bool? _ketoPass;

    [ObservableProperty]
    private bool? _omadPass;

    [ObservableProperty]
    private string _weightText = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MealEntryViewModel> _meals = new();

    [ObservableProperty]
    private bool _canAddMeal = true;

    [ObservableProperty]
    private ObservableCollection<KetoneEntryViewModel> _ketones = new();

    [ObservableProperty]
    private bool _canAddKetone = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isSaved;

    public List<string> TimeSlots => MealEntryViewModel.AllTimeSlots;

    private long _dateTicks;
    public long DateTicks
    {
        get => _dateTicks;
        set
        {
            _dateTicks = value;
            _ = LoadAsync(new DateTime(value));
        }
    }

    public DayDetailViewModel(DatabaseService db)
    {
        _db = db;
    }

    private async Task LoadAsync(DateTime date)
    {
        IsBusy = true;
        try
        {
            DateLabel = date.ToString("dddd, MMMM d, yyyy");
            _dayEntry = await _db.GetOrCreateDayEntryAsync(date);

            KetoPass = _dayEntry.KetoPass;
            OmadPass = _dayEntry.OmadPass;
            WeightText = _dayEntry.WeightKg?.ToString("0.##") ?? string.Empty;
            Notes = _dayEntry.Notes ?? string.Empty;

            var mealModels = await _db.GetMealsForDayAsync(_dayEntry.Id);
            Meals.Clear();
            foreach (var m in mealModels)
                Meals.Add(new MealEntryViewModel(m));
            UpdateCanAddMeal();

            var ketoneModels = await _db.GetKetonesForDayAsync(_dayEntry.Id);
            Ketones.Clear();
            foreach (var k in ketoneModels)
                Ketones.Add(new KetoneEntryViewModel(k));
            UpdateCanAddKetone();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetKetoPass(string value)
    {
        bool newVal = value == "true";
        KetoPass = KetoPass == newVal ? null : newVal;
    }

    [RelayCommand]
    private void SetOmadPass(string value)
    {
        bool newVal = value == "true";
        OmadPass = OmadPass == newVal ? null : newVal;
    }

    [RelayCommand]
    private async Task AddMeal()
    {
        if (_dayEntry is null || Meals.Count >= 5) return;

        var model = new MealEntry
        {
            DayEntryId = _dayEntry.Id,
            TimeOfDay = TimeSpan.Zero,
            Description = string.Empty
        };
        await _db.SaveMealAsync(model);
        Meals.Add(new MealEntryViewModel(model));
        UpdateCanAddMeal();
    }

    [RelayCommand]
    private async Task RemoveMeal(MealEntryViewModel meal)
    {
        await _db.DeleteMealAsync(meal.Model);
        Meals.Remove(meal);
        UpdateCanAddMeal();
    }

    private void UpdateCanAddMeal() => CanAddMeal = Meals.Count < 5;

    [RelayCommand]
    private async Task AddKetone()
    {
        if (_dayEntry is null || Ketones.Count >= 5) return;

        var model = new KetoneEntry
        {
            DayEntryId = _dayEntry.Id,
            TimeOfDay = TimeSpan.Zero,
            Value = 0
        };
        await _db.SaveKetoneAsync(model);
        Ketones.Add(new KetoneEntryViewModel(model));
        UpdateCanAddKetone();
    }

    [RelayCommand]
    private async Task RemoveKetone(KetoneEntryViewModel ketone)
    {
        await _db.DeleteKetoneAsync(ketone.Model);
        Ketones.Remove(ketone);
        UpdateCanAddKetone();
    }

    private void UpdateCanAddKetone() => CanAddKetone = Ketones.Count < 5;

    [RelayCommand]
    private async Task Save()
    {
        if (_dayEntry is null) return;
        IsBusy = true;
        try
        {
            _dayEntry.KetoPass = KetoPass;
            _dayEntry.OmadPass = OmadPass;
            _dayEntry.WeightKg = double.TryParse(WeightText, out double w) ? w : null;
            _dayEntry.Notes = Notes;
            await _db.SaveDayEntryAsync(_dayEntry);

            foreach (var mealVm in Meals)
            {
                mealVm.SyncToModel();
                await _db.SaveMealAsync(mealVm.Model);
            }

            foreach (var ketoneVm in Ketones.ToList())
            {
                if (!string.IsNullOrWhiteSpace(ketoneVm.ValueText) && double.TryParse(ketoneVm.ValueText, out _))
                {
                    ketoneVm.SyncToModel();
                    await _db.SaveKetoneAsync(ketoneVm.Model);
                }
                else
                {
                    await _db.DeleteKetoneAsync(ketoneVm.Model);
                    Ketones.Remove(ketoneVm);
                }
            }
            UpdateCanAddKetone();

            IsSaved = true;
            await Task.Delay(1500);
            IsSaved = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
