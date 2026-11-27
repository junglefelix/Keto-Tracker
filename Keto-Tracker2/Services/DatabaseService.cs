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

    public async Task<List<KetoneEntry>> GetKetonesForDayAsync(int dayEntryId)
    {
        await InitAsync();
        return await _db!.Table<KetoneEntry>()
            .Where(k => k.DayEntryId == dayEntryId)
            .OrderBy(k => k.TimeOfDay)
            .ToListAsync();
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

    public async Task<List<DayEntry>> GetDayEntriesInRangeAsync(DateTime from, DateTime to)
    {
        await InitAsync();
        var start = from.Date;
        var end = to.Date;
        return await _db!.Table<DayEntry>()
            .Where(d => d.Date >= start && d.Date <= end)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<List<DayEntry>> GetAllWeightEntriesAsync()
    {
        await InitAsync();
        return await _db!.Table<DayEntry>()
            .Where(d => d.WeightKg != null)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    // ── Bulk operations for import/export ──────────────────────────────────

    public async Task<KetoTrackerData> GetAllDataAsync()
    {
        await InitAsync();

        var allDays = await _db!.Table<DayEntry>().ToListAsync();
        var data = new KetoTrackerData { ExportedAt = DateTime.UtcNow };

        foreach (var day in allDays)
        {
            var meals = await _db.Table<MealEntry>()
                .Where(m => m.DayEntryId == day.Id)
                .OrderBy(m => m.TimeOfDay)
                .ToListAsync();

            var ketones = await _db.Table<KetoneEntry>()
                .Where(k => k.DayEntryId == day.Id)
                .OrderBy(k => k.TimeOfDay)
                .ToListAsync();

            var record = new DayRecord
            {
                Date = day.Date,
                KetoPass = day.KetoPass,
                OmadPass = day.OmadPass,
                WeightKg = day.WeightKg,
                Notes = day.Notes,
                Meals = meals.Select(m => new MealRecord
                {
                    TimeOfDay = m.TimeOfDay,
                    Description = m.Description
                }).ToList(),
                Ketones = ketones.Select(k => new KetoneRecord
                {
                    Value = k.Value,
                    TimeOfDay = k.TimeOfDay
                }).ToList()
            };

            data.Days.Add(record);
        }

        return data;
    }

    public async Task ImportDataAsync(KetoTrackerData data)
    {
        await InitAsync();

        foreach (var record in data.Days)
        {
            // Upsert the day entry
            var existing = await _db!.Table<DayEntry>()
                .Where(d => d.Date == record.Date.Date)
                .FirstOrDefaultAsync();

            DayEntry dayEntry;
            if (existing is not null)
            {
                existing.KetoPass = record.KetoPass;
                existing.OmadPass = record.OmadPass;
                existing.WeightKg = record.WeightKg;
                existing.Notes = record.Notes;
                await _db.UpdateAsync(existing);
                dayEntry = existing;
            }
            else
            {
                dayEntry = new DayEntry
                {
                    Date = record.Date.Date,
                    KetoPass = record.KetoPass,
                    OmadPass = record.OmadPass,
                    WeightKg = record.WeightKg,
                    Notes = record.Notes
                };
                await _db.InsertAsync(dayEntry);
            }

            // Replace meals: delete existing, insert from record
            var existingMeals = await _db.Table<MealEntry>()
                .Where(m => m.DayEntryId == dayEntry.Id)
                .ToListAsync();
            foreach (var m in existingMeals)
                await _db.DeleteAsync(m);

            foreach (var mealRecord in record.Meals)
            {
                await _db.InsertAsync(new MealEntry
                {
                    DayEntryId = dayEntry.Id,
                    TimeOfDay = mealRecord.TimeOfDay,
                    Description = mealRecord.Description
                });
            }

            // Replace ketones: delete existing, insert from record
            var existingKetones = await _db.Table<KetoneEntry>()
                .Where(k => k.DayEntryId == dayEntry.Id)
                .ToListAsync();
            foreach (var k in existingKetones)
                await _db.DeleteAsync(k);

            // Older export files only have the singular "Ketone" field; newer ones use "Ketones".
            var ketoneRecords = new List<KetoneRecord>(record.Ketones);
            if (record.Ketone is not null && ketoneRecords.Count == 0)
                ketoneRecords.Add(record.Ketone);

            foreach (var ketoneRecord in ketoneRecords)
            {
                await _db.InsertAsync(new KetoneEntry
                {
                    DayEntryId = dayEntry.Id,
                    Value = ketoneRecord.Value,
                    TimeOfDay = ketoneRecord.TimeOfDay
                });
            }
        }
    }
}
