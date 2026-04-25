using SQLite;

namespace Keto_Tracker2.Models;

public class KetoneEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int DayEntryId { get; set; }

    public double Value { get; set; }
    public TimeSpan TimeOfDay { get; set; }
}
