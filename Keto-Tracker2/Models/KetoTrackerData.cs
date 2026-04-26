namespace Keto_Tracker2.Models;

/// <summary>
/// Root object for import/export of all user data.
/// </summary>
public class KetoTrackerData
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<DayRecord> Days { get; set; } = new();
}

public class DayRecord
{
    public DateTime Date { get; set; }
    public bool? KetoPass { get; set; }
    public bool? OmadPass { get; set; }
    public double? WeightKg { get; set; }
    public string? Notes { get; set; }
    public List<MealRecord> Meals { get; set; } = new();
    public KetoneRecord? Ketone { get; set; }
}

public class MealRecord
{
    public TimeSpan TimeOfDay { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class KetoneRecord
{
    public double Value { get; set; }
    public TimeSpan TimeOfDay { get; set; }
}
