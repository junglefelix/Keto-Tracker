using System.Text.Json;
using Keto_Tracker2.Models;

namespace Keto_Tracker2.Services;

public class ImportExportService
{
    private readonly DatabaseService _db;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ImportExportService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// Exports all data to a JSON file in the cache directory and
    /// opens the platform share sheet so the user can save or send it.
    /// </summary>
    public async Task ExportAsync()
    {
        var data = await _db.GetAllDataAsync();

        if (data.Days.Count == 0)
            throw new InvalidOperationException("No data to export.");

        var json = JsonSerializer.Serialize(data, JsonOptions);

        var fileName = $"keto-tracker-{DateTime.Now:yyyy-MM-dd}.json";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(filePath, json);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export Keto Tracker Data",
            File = new ShareFile(filePath, "application/json")
        });
    }

    /// <summary>
    /// Opens the file picker so the user can choose a previously exported
    /// JSON file. All records in the file are upserted into the local DB.
    /// </summary>
    public async Task<int> ImportAsync()
    {
        var pickOptions = new PickOptions
        {
            PickerTitle = "Select a Keto Tracker export (.json)",
            FileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS,     new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI,   new[] { ".json" } },
                    { DevicePlatform.macOS,   new[] { "json" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.json" } },
                })
        };

        var result = await FilePicker.Default.PickAsync(pickOptions);
        if (result is null)
            return -1; // user cancelled

        KetoTrackerData? data;
        using (var stream = await result.OpenReadAsync())
        {
            data = await JsonSerializer.DeserializeAsync<KetoTrackerData>(stream, JsonOptions);
        }

        if (data is null)
            throw new InvalidOperationException("The selected file could not be read.");

        await _db.ImportDataAsync(data);
        return data.Days.Count;
    }
}
