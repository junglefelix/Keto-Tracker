using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class WeightTrendPage : ContentPage
{
    private readonly WeightTrendViewModel _viewModel;

    public WeightTrendPage(WeightTrendViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
