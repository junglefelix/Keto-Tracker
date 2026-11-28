using Keto_Tracker2.Services;
using Keto_Tracker2.ViewModels;

namespace Keto_Tracker2.Views;

public partial class MainPage : ContentPage, ISwipeNavigable
{
    private readonly CalendarViewModel _viewModel;
    private readonly ImportExportService _importExport;
    private readonly DatabaseService _db;
    private readonly SpeechInputService _speech;
    private bool _voiceBusy;

    private const string VoiceHelp =
        "Примеры:\n" +
        "• «сегодня вешу 90», «мой вес 85,5»\n" +
        "• «в 15:00 ел печёнку с брокколи»\n" +
        "• «вчера в 7 вечера ужин салат»\n" +
        "• «кетоны 1,2», «кетоны в 8 утра 0,8»\n" +
        "• «кето ок», «омад провал»";

    public MainPage(CalendarViewModel viewModel, ImportExportService importExport,
                    DatabaseService db, SpeechInputService speech)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _importExport = importExport;
        _db = db;
        _speech = speech;
        BindingContext = viewModel;
        SwipeNavigation.Attach(RootGrid, this);
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

    private async void OnWeightChartTapped(object sender, TappedEventArgs e)
    {
        if (!_viewModel.HasAnyWeightData) return;
        await Shell.Current.GoToAsync("WeightTrendPage");
    }

    // Swipe left = next month, swipe right = previous month (arrows keep working too).
    public void OnSwipeLeft()
    {
        if (_viewModel.NextMonthCommand.CanExecute(null))
            _viewModel.NextMonthCommand.Execute(null);
    }

    public void OnSwipeRight()
    {
        if (_viewModel.PrevMonthCommand.CanExecute(null))
            _viewModel.PrevMonthCommand.Execute(null);
    }

    // ── Voice input ─────────────────────────────────────────────────────────

    private async void OnVoiceClicked(object sender, EventArgs e)
    {
        if (_voiceBusy) return;
        _voiceBusy = true;
        try
        {
            VoiceCommand? cmd;
            string text;

            if (_speech.IsSupported)
            {
                IReadOnlyList<string>? heard;
                try
                {
                    heard = await _speech.ListenAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Голосовой ввод", ex.Message, "OK");
                    return;
                }
                if (heard is null || heard.Count == 0) return; // cancelled or silence

                (cmd, text) = VoiceCommandParser.ParseBest(heard, DateTime.Now);
            }
            else
            {
                // No speech recogniser on this platform: type the same phrase instead.
                var typed = await DisplayPromptAsync("Быстрая запись", VoiceHelp, "Готово", "Отмена");
                if (string.IsNullOrWhiteSpace(typed)) return;
                text = typed;
                cmd = VoiceCommandParser.Parse(text, DateTime.Now);
            }

            // Show what was understood; the user can save, or fix the text and try again.
            while (true)
            {
                if (cmd is null)
                {
                    var fixedText = await DisplayPromptAsync(
                        "Не понял 🤔",
                        $"Услышал: «{text}»\n\nПоправьте текст или нажмите «Отмена».\n\n{VoiceHelp}",
                        "Готово", "Отмена", initialValue: text);
                    if (string.IsNullOrWhiteSpace(fixedText)) return;
                    text = fixedText;
                    cmd = VoiceCommandParser.Parse(text, DateTime.Now);
                    continue;
                }

                bool save = await DisplayAlert(
                    "Записать?",
                    $"{cmd.Describe(DateTime.Today)}\n\nУслышал: «{text}»",
                    "Сохранить", "Изменить");

                if (save)
                {
                    await _db.ApplyVoiceCommandAsync(cmd);
                    await _viewModel.RefreshAsync();
                    return;
                }

                var edited = await DisplayPromptAsync(
                    "Изменить", "Поправьте текст:", "Готово", "Отмена", initialValue: text);
                if (string.IsNullOrWhiteSpace(edited)) return;
                text = edited;
                cmd = VoiceCommandParser.Parse(text, DateTime.Now);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Голосовой ввод", $"Не удалось сохранить: {ex.Message}", "OK");
        }
        finally
        {
            _voiceBusy = false;
        }
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        string voiceItem = _speech.IsRussian
            ? "Voice language: Русский → English"
            : "Voice language: English → Русский";

        var action = await DisplayActionSheet(
            "Keto Tracker",
            "Cancel",
            null,
            "Export Data",
            "Import Data",
            voiceItem);

        if (action == voiceItem)
        {
            _speech.Language = _speech.IsRussian ? "en-US" : "ru-RU";
            return;
        }

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

