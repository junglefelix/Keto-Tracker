using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class DayDetailPage : ContentPage
{
    public DayDetailPage(DayDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
