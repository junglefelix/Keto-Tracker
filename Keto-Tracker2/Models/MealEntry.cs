using SQLite;

namespace Keto_Tracker2.Models;

public class MealEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int DayEntryId { get; set; }

    public TimeSpan TimeOfDay { get; set; }
    public string Description { get; set; } = string.Empty;
}
