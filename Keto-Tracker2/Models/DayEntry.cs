using SQLite;

namespace Keto_Tracker2.Models;

public class DayEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public DateTime Date { get; set; }

    public bool? KetoPass { get; set; }
    public bool? OmadPass { get; set; }
    public double? WeightKg { get; set; }
    public string? Notes { get; set; }
}
