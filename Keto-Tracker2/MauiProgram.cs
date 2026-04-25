using Microsoft.Extensions.Logging;
using Keto_Tracker2.Services;
using Keto_Tracker2.ViewModels;
using Keto_Tracker2.Views;

namespace Keto_Tracker2;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<DatabaseService>();

        // ViewModels
        builder.Services.AddSingleton<CalendarViewModel>();
        builder.Services.AddTransient<DayDetailViewModel>();

        // Pages
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<DayDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
