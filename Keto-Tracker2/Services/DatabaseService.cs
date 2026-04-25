using SQLite;
using Keto_Tracker2.Models;

namespace Keto_Tracker2.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task InitAsync()
    {
        if (_db is not null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ketotracker.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<DayEntry>();
        await _db.CreateTableAsync<MealEntry>();
        await _db.CreateTableAsync<KetoneEntry>();
    }

    public async Task<DayEntry?> GetDayEntryAsync(DateTime date)
    {
        await InitAsync();
        var dateOnly = date.Date;
        return await _db!.Table<DayEntry>()
            .Where(d => d.Date == dateOnly)
            .FirstOrDefaultAsync();
    }

    public async Task<DayEntry> GetOrCreateDayEntryAsync(DateTime date)
    {
        await InitAsync();
        var existing = await GetDayEntryAsync(date);
        if (existing is not null) return existing;
        var entry = new DayEntry { Date = date.Date };
        await _db!.InsertAsync(entry);
        return entry;
    }

    public async Task SaveDayEntryAsync(DayEntry entry)
    {
        await InitAsync();
        if (entry.Id == 0)
            await _db!.InsertAsync(entry);
        else
            await _db!.UpdateAsync(entry);
    }

    public async Task<List<DayEntry>> GetDayEntriesForMonthAsync(int year, int month)
    {
        await InitAsync();
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _db!.Table<DayEntry>()
            .Where(d => d.Date >= start && d.Date < end)
            .ToListAsync();
    }

    public async Task<List<MealEntry>> GetMealsForDayAsync(int dayEntryId)
    {
        await InitAsync();
        return await _db!.Table<MealEntry>()
            .Where(m => m.DayEntryId == dayEntryId)
            .OrderBy(m => m.TimeOfDay)
            .ToListAsync();
    }

    public async Task SaveMealAsync(MealEntry meal)
    {
        await InitAsync();
        if (meal.Id == 0)
            await _db!.InsertAsync(meal);
        else
            await _db!.UpdateAsync(meal);
    }

    public async Task DeleteMealAsync(MealEntry meal)
    {
        await InitAsync();
        await _db!.DeleteAsync(meal);
    }

    public async Task<KetoneEntry?> GetKetoneForDayAsync(int dayEntryId)
    {
        await InitAsync();
        return await _db!.Table<KetoneEntry>()
            .Where(k => k.DayEntryId == dayEntryId)
            .FirstOrDefaultAsync();
    }

    public async Task SaveKetoneAsync(KetoneEntry ketone)
    {
        await InitAsync();
        if (ketone.Id == 0)
            await _db!.InsertAsync(ketone);
        else
            await _db!.UpdateAsync(ketone);
    }

    public async Task DeleteKetoneAsync(KetoneEntry ketone)
    {
        await InitAsync();
        await _db!.DeleteAsync(ketone);
    }
}
