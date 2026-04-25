using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class MainPage : ContentPage
{
    private readonly CalendarViewModel _viewModel;

    public MainPage(CalendarViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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
}
