using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class DayDetailPage : ContentPage
{
    public DayDetailPage(DayDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        if (BindingContext is DayDetailViewModel vm && vm.SaveCommand.CanExecute(null))
            vm.SaveCommand.Execute(null);
    }
}
