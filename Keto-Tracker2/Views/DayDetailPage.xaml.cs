using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class DayDetailPage : ContentPage, ISwipeNavigable
{
    public DayDetailPage(DayDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        SwipeNavigation.Attach(RootGrid, this);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        if (BindingContext is DayDetailViewModel vm && vm.SaveCommand.CanExecute(null))
            vm.SaveCommand.Execute(null);
    }

    // Swipe left = next day, swipe right = previous day.
    public void OnSwipeLeft() => _ = ChangeDayAsync(+1);

    public void OnSwipeRight() => _ = ChangeDayAsync(-1);

    private bool _isChangingDay;

    private async Task ChangeDayAsync(int offsetDays)
    {
        if (BindingContext is not DayDetailViewModel vm || _isChangingDay) return;
        _isChangingDay = true;

        // Close the keyboard so the field being edited commits its value before saving.
        foreach (var view in RootGrid.GetVisualTreeDescendants().OfType<InputView>())
            if (view.IsFocused) view.Unfocus();

        // Short slide so it is clear that the day changed.
        double shift = offsetDays > 0 ? -40 : 40;
        await Task.WhenAll(RootGrid.FadeTo(0, 100), RootGrid.TranslateTo(shift, 0, 100));

        try
        {
            await vm.GoToDayAsync(offsetDays);
        }
        finally
        {
            RootGrid.TranslationX = -shift;
            await Task.WhenAll(RootGrid.FadeTo(1, 120), RootGrid.TranslateTo(0, 0, 120));
            _isChangingDay = false;
        }
    }
}
