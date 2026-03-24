using SQLite;

namespace ScheduleApp.Models;

[Table("Schedule")]
public class ScheduleItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Time { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
