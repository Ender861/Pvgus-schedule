using SQLite;

namespace ScheduleApp.Models;

[Table("DailyNotes")]
public class DailyNote
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsImportant { get; set; }
}
