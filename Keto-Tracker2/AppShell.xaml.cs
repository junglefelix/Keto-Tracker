namespace Keto_Tracker2;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("DayDetailPage", typeof(Views.DayDetailPage));
    }
}
