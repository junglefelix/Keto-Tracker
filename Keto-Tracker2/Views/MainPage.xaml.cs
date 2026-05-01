using Keto_Tracker2.Services;
using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class MainPage : ContentPage
{
    private readonly CalendarViewModel _viewModel;
    private readonly ImportExportService _importExport;

    public MainPage(CalendarViewModel viewModel, ImportExportService importExport)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _importExport = importExport;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshAsync();
    }

    private async void OnDayTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is DayCell cell && cell.IsCurrentMonth)
        {
            await Shell.Current.GoToAsync($"DayDetailPage?dateTicks={cell.Date.Ticks}");
        }
    }

    private void OnSwipedLeft(object sender, SwipedEventArgs e)
    {
        _viewModel.NextMonthCommand.Execute(null);
    }

    private void OnSwipedRight(object sender, SwipedEventArgs e)
    {
        _viewModel.PrevMonthCommand.Execute(null);
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet(
            "Keto Tracker",
            "Cancel",
            null,
            "Export Data",
            "Import Data");

        switch (action)
        {
            case "Export Data":
                await ExportDataAsync();
                break;
            case "Import Data":
                await ImportDataAsync();
                break;
        }
    }

    private async Task ExportDataAsync()
    {
        try
        {
            await _importExport.ExportAsync();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlert("Export", ex.Message, "OK");
        }
        catch (Exception)
        {
            await DisplayAlert("Export Failed", "An error occurred while exporting data.", "OK");
        }
    }

    private async Task ImportDataAsync()
    {
        var confirm = await DisplayAlert(
            "Import Data",
            "Importing will overwrite any existing data for days found in the file. Continue?",
            "Import", "Cancel");

        if (!confirm) return;

        try
        {
            var count = await _importExport.ImportAsync();
            if (count < 0) return; // user cancelled picker

            await DisplayAlert("Import Complete", $"Imported data for {count} day(s).", "OK");
            await _viewModel.RefreshAsync();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlert("Import Failed", ex.Message, "OK");
        }
        catch (Exception)
        {
            await DisplayAlert("Import Failed", "An error occurred while importing data.", "OK");
        }
    }
}

