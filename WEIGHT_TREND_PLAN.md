# Plan: Weight Trend chart — limit default range + full-screen zoom

> Copy this file into the root of `c:\Source\Other\my-github\Keto-Tracker\` and ask
> Copilot to "implement the plan in WEIGHT_TREND_PLAN.md". It contains all
> investigation findings, your answers to clarifying questions, and the exact
> step-by-step implementation plan — no re-discovery or re-asking should be needed.

## Repo location

Implement in: `c:\Source\Other\my-github\Keto-Tracker\Keto-Tracker2`
(solution file: `c:\Source\Other\my-github\Keto-Tracker\Keto-Tracker2.sln`)

This is a .NET MAUI app (net9.0, targets android/ios/maccatalyst/windows), uses
CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`
partial classes), Shell navigation, and SQLite-net for persistence.

## Background / problem

The "Weight Trend" chart on the main dashboard currently shows **all** weight entries
ever recorded (up to 3 years back), with no date limiting — making it cramped and
hard to read. The user wants:

1. The default dashboard chart limited to a recent window (last 30 days) so it's
   readable.
2. The chart to be tappable, opening a full-screen view.
3. In full-screen mode: shows **all** collected data by default, with the ability to
   zoom in/out to shorter ranges.

## Decisions already made (do not re-ask these)

- **Default dashboard chart range**: Rolling last 30 days (today.AddDays(-29) ..
  today), independent of whichever calendar month is currently navigated/displayed
  in the calendar grid above it.
- **Full-screen presentation**: Modal page that slides up, with a Close button —
  implement via Shell's `Shell.PresentationMode="ModalAnimated"` attached property
  on the new page.
- **Zoom/pan interaction**: Simple **range buttons**: `30D / 90D / 1Y / All`. NOT
  freeform pinch-to-zoom/pan gestures (unreliable on desktop/mouse platforms like
  Windows).
- **Charting approach**: Keep the existing hand-rolled `Microsoft.Maui.Graphics`
  `IDrawable` chart. Do **not** add a new charting NuGet dependency.
- **Tap-to-inspect a single point** (tooltip with exact date/weight): **No**, skip
  this — not required.
- **Discoverability hint** on the dashboard chart: **Yes** — add a small caption or
  icon (e.g. "Tap to view full history ›") so users know it's tappable.
- Default selected range when the full-screen page first opens: **"All"** (per
  requirement #3 above), with 30D/90D/1Y as zoom-in options from there.

## Investigation findings (verified facts about the existing code)

- `ViewModels/CalendarViewModel.cs`:
  - `ComputeStatsAsync()` currently calls
    `_db.GetDayEntriesInRangeAsync(today.AddDays(-1095), today)` (3-year lookback)
    to compute Keto/OMAD streaks, and **also** derives the weight chart's points from
    that same 3-year-wide `allEntries` list — this is the root cause of the
    unreadable graph (no 30-day filtering exists today).
  - Has `[ObservableProperty] WeightChartDrawable _weightChart = new();` and
    `[ObservableProperty] bool _hasWeightData;`.
  - `LoadMonthAsync` builds the calendar grid `Days` for the navigated month and
    calls `ComputeStatsAsync()` each time (so stats/chart recompute on month nav —
    need to make sure the 30-day chart window is NOT tied to the navigated month,
    always "last 30 days from today").
- `ViewModels/WeightChartDrawable.cs`:
  - `IDrawable` with `public List<(DateTime Date, double Weight)> Points`.
  - `Draw()` computes padding/left/right/top/bottom, draws 3 horizontal grid lines,
    y-axis min/max labels (`F1` format), maps points to screen coords, draws
    line+dots, and draws x-axis day-of-month labels thinned to fit available width
    (`maxLabels = width/18f`, `step = ceil(count/maxLabels)`).
  - Currently x-axis labels always use `Points[i].Date.Day.ToString()` (just the day
    number) — fine for a 30-day view, but not great for multi-year "All" view.
- `Services/DatabaseService.cs`:
  - Lazy `InitAsync()` opens `SQLiteAsyncConnection` at
    `Path.Combine(FileSystem.AppDataDirectory, "ketotracker.db3")`, creates tables
    `DayEntry`, `MealEntry`, `KetoneEntry`.
  - Existing methods: `GetDayEntryAsync`, `GetOrCreateDayEntryAsync`,
    `SaveDayEntryAsync`, `GetDayEntriesForMonthAsync(year, month)`,
    `GetMealsForDayAsync` / `SaveMealAsync` / `DeleteMealAsync`,
    `GetKetoneForDayAsync` / `SaveKetoneAsync` / `DeleteKetoneAsync`,
    `GetDayEntriesInRangeAsync(from, to)` (inclusive, ordered by Date), plus
    `GetAllDataAsync()` / `ImportDataAsync()` for import/export.
  - No existing method to fetch *all* weight entries regardless of date range — need
    to add one.
- `Models/DayEntry.cs`: `Id` (PK autoincrement), `Date` (unique indexed), `KetoPass?`
  (bool?), `OmadPass?` (bool?), `WeightKg?` (double?), `Notes?` (string?).
- `Views/MainPage.xaml`:
  - Root `Grid RowDefinitions="Auto,*"` with header month-nav bar and a `ScrollView`
    containing: day-of-week header row, calendar `CollectionView`, a divider, a
    2-column stats `Grid` (Keto Streak / OMAD Streak cards), then the "Weight Trend"
    section: a `Border` (`BackgroundColor="#2D2D44"`, `CornerRadius="12"`) containing
    a `VerticalStackLayout` with a "Weight Trend" label, a `GraphicsView` bound to
    `WeightChart` (`HeightRequest="110"`, `IsVisible="{Binding HasWeightData}"`), and
    a "No weight data recorded yet" label shown via `InverseBool` converter on
    `HasWeightData`.
  - Uses converters registered in `ContentPage.Resources`: `DayCellToColor`,
    `DayCellToTextColor`, `OmadToRingColor`, `IsFutureToOpacity`, `InverseBool`.
- `Views/MainPage.xaml.cs`:
  - Constructor takes `CalendarViewModel` + `ImportExportService` via DI;
    `BindingContext = viewModel`.
  - `OnAppearing()` calls `await _viewModel.RefreshAsync()`.
  - `OnDayTapped` navigates via
    `await Shell.Current.GoToAsync($"DayDetailPage?dateTicks={cell.Date.Ticks}");`.
  - Also has swipe gesture handlers for month navigation and a menu (export/import).
- `AppShell.xaml` / `AppShell.xaml.cs`:
  - Single `ShellContent` for `MainPage` (`Shell.FlyoutBehavior="Disabled"`).
  - Routes registered in code-behind constructor via
    `Routing.RegisterRoute("DayDetailPage", typeof(Views.DayDetailPage));`.
- `MauiProgram.cs` DI registrations:
  - Services: `DatabaseService`, `ImportExportService` — `AddSingleton`.
  - ViewModels: `CalendarViewModel` — `AddSingleton`; `DayDetailViewModel` —
    `AddTransient`.
  - Pages: `MainPage` — `AddSingleton`; `DayDetailPage` — `AddTransient`.

## Step-by-step implementation plan

### 1. `Services/DatabaseService.cs` — add a method to fetch all weight history
Add a new method (near `GetDayEntriesInRangeAsync`):
```csharp
public async Task<List<DayEntry>> GetAllWeightEntriesAsync()
{
    await InitAsync();
    return await _db!.Table<DayEntry>()
        .Where(d => d.WeightKg != null)
        .OrderBy(d => d.Date)
        .ToListAsync();
}
```

### 2. `ViewModels/WeightChartDrawable.cs` — smarter axis labels for larger spans
Keep the existing rendering logic, but make the x-axis label format adapt to the
date span covered by `Points`, so the "All" / "1Y" full-screen views stay readable:
- Span ≤ 31 days → day-of-month number only (current behavior, unchanged).
- Span ≤ ~366 days → short label like `"MMM d"` (e.g. "Aug 13").
- Span > ~366 days → label like `"MMM yyyy"` (e.g. "Aug 2026"), thinned further since
  fewer distinct labels are needed.
Keep using the existing `maxLabels`/`step` thinning logic to avoid overlapping text,
just change the format string based on the computed span.

### 3. `ViewModels/CalendarViewModel.cs` — limit dashboard chart to last 30 days
In `ComputeStatsAsync()`:
- Keep the existing 3-year `allEntries` query for streak computation (unchanged
  logic for `KetoStreakDays` / `OmadStreakDays`).
- Add a new derived property `HasAnyWeightData` (bool) — true if any weight entry
  exists at all (e.g. `allEntries.Any(e => e.WeightKg.HasValue)`, or query
  `GetAllWeightEntriesAsync()` if you want it accurate beyond the 3-year lookback —
  3-year lookback is acceptable given this is a personal tracker app).
- Change the weight chart points to only include entries from the last 30 days:
  filter `allEntries` (or a fresh query) to `e.Date >= today.AddDays(-29)` before
  building `weightPoints`.
- `HasWeightData` should now mean "has weight data in the last 30 days" (keeps
  controlling whether the `GraphicsView` or the "no data" label is shown).
- Add `[ObservableProperty] bool _hasAnyWeightData;` — used to enable/disable the
  tap-to-expand gesture and the discoverability hint, even when the last 30 days
  happen to have no data but older data exists.

### 4. `Views/MainPage.xaml` — make the chart tappable + add discoverability hint
- Wrap the "Weight Trend" `Border` (or add a `TapGestureRecognizer` to it) so
  tapping navigates to the new full-screen page. Only enabled/wired when
  `HasAnyWeightData` is true (e.g. bind `IsEnabled` or gate in the tap handler).
- Add a small `Label` below the chart/inside the same card, e.g.
  `Text="Tap to view full history ›"`, small font, muted color (consistent with
  `#9E9EC8`/`#555570` palette already used), `IsVisible="{Binding HasAnyWeightData}"`.

### 5. `Views/MainPage.xaml.cs` — tap handler
Add a handler (e.g. `OnWeightChartTapped`) that navigates to the new modal route,
similar in style to `OnDayTapped`:
```csharp
private async void OnWeightChartTapped(object sender, TappedEventArgs e)
{
    if (!_viewModel.HasAnyWeightData) return;
    await Shell.Current.GoToAsync("WeightTrendPage");
}
```

### 6. New `ViewModels/WeightTrendViewModel.cs`
- Injects `DatabaseService`.
- Loads all weight history once via `GetAllWeightEntriesAsync()` on init/appearing,
  stores as the full point list.
- `[ObservableProperty] WeightChartDrawable _chart = new();`
- `[ObservableProperty] string _selectedRange = "All";` (or an enum) tracking which
  of `30D / 90D / 1Y / All` is active, defaulting to `"All"`.
- `[RelayCommand]` methods (or one command with a string parameter) for selecting a
  range — re-filter the in-memory full point list by the chosen window and rebuild
  `Chart = new WeightChartDrawable { Points = filteredPoints };` (no re-query needed
  since all data is already loaded).
- Expose a bool per range (e.g. `Is30DSelected`, `Is90DSelected`, `Is1YSelected`,
  `IsAllSelected`) or a converter, to visually highlight the active range button.
- Optionally expose `HasWeightData` (false → show an empty-state message instead of
  the chart, for consistency with the dashboard).

### 7. New `Views/WeightTrendPage.xaml` + `Views/WeightTrendPage.xaml.cs`
- `ContentPage` with `Shell.PresentationMode="ModalAnimated"` set on the root
  element so it slides up modally.
- Header row: title ("Weight Trend — Full History") + a Close button/icon that
  calls `await Shell.Current.GoToAsync("..");` (or `Navigation.PopModalAsync()`,
  whichever proves correct for the Shell modal route registration used).
- Row of 4 range-toggle buttons: `30D`, `90D`, `1Y`, `All`, bound to commands on
  `WeightTrendViewModel`, with the active one visually distinguished (e.g. different
  `BackgroundColor`/`TextColor`, matching existing dark theme palette
  `#2D2D44`/`#4FC3F7`/`#9E9EC8`).
- A larger `GraphicsView` bound to `Chart`, filling most of the screen (unlike the
  small 110-height dashboard version).
- Constructor takes `WeightTrendViewModel` via DI; `BindingContext = viewModel`;
  load data in `OnAppearing()`.

### 8. `AppShell.xaml.cs` — register the new route
Add alongside the existing `DayDetailPage` registration:
```csharp
Routing.RegisterRoute("WeightTrendPage", typeof(Views.WeightTrendPage));
```

### 9. `MauiProgram.cs` — DI registration
```csharp
// ViewModels
builder.Services.AddTransient<WeightTrendViewModel>();

// Pages
builder.Services.AddTransient<WeightTrendPage>();
```
(Transient, matching the pattern used for `DayDetailViewModel`/`DayDetailPage`,
since this is a detail/modal page rather than a persistent singleton like
`MainPage`.)

## Constraints / things to preserve

- No new NuGet package dependencies.
- No tap-to-inspect tooltip on individual points.
- Keep existing dark theme color palette consistent (`#1E1E2E`, `#2D2D44`,
  `#3D3D54`, `#9E9EC8`, `#4FC3F7`, `#4CAF50`, `#FFD700`, `#555570`).
- Don't change unrelated behavior: calendar month navigation, streak computation
  logic, import/export, day detail page, etc. should be untouched.
- Follow existing code conventions: CommunityToolkit.Mvvm source generators
  (`[ObservableProperty]`, `[RelayCommand]`), constructor DI, async/await patterns
  already used elsewhere in the file.

## Suggested validation after implementing

- Build the solution (`dotnet build Keto-Tracker2.sln`) and fix any compile errors.
- Manually sanity check: dashboard chart only shows last 30 days of data; tapping it
  opens the full-screen modal; full-screen defaults to "All" data; range buttons
  30D/90D/1Y correctly filter down; Close button returns to the dashboard.
