using SQLite;
using ScheduleApp.Models;

namespace ScheduleApp.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;
    
    public DatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        InitializeDatabase();
    }
    
    private async void InitializeDatabase()
    {
        await _database.CreateTableAsync<ScheduleItem>();
        await _database.CreateTableAsync<Homework>();
        await _database.CreateTableAsync<DailyNote>();
    }
    
    public Task<List<ScheduleItem>> GetScheduleAsync(DateTime date)
    {
        return _database.Table<ScheduleItem>()
            .Where(s => s.Date.Date == date.Date)
            .OrderBy(s => s.Time)
            .ToListAsync();
    }
    
    public Task<int> SaveScheduleAsync(ScheduleItem item)
    {
        return _database.InsertAsync(item);
    }
    
    public Task<int> ClearScheduleAsync()
    {
        return _database.DeleteAllAsync<ScheduleItem>();
    }
    
    public Task<List<Homework>> GetHomeworkAsync(int scheduleId)
    {
        return _database.Table<Homework>()
            .Where(h => h.ScheduleId == scheduleId)
            .ToListAsync();
    }
    
    public Task<List<Homework>> GetAllHomeworkAsync()
    {
        return _database.Table<Homework>()
            .OrderBy(h => h.Deadline)
            .ToListAsync();
    }
    
    public Task<int> SaveHomeworkAsync(Homework homework)
    {
        return _database.InsertAsync(homework);
    }
    
    public Task<int> UpdateHomeworkAsync(Homework homework)
    {
        return _database.UpdateAsync(homework);
    }
    
    public Task<int> DeleteHomeworkAsync(int id)
    {
        return _database.DeleteAsync<Homework>(id);
    }
    
    public Task<List<DailyNote>> GetNotesAsync(DateTime date)
    {
        return _database.Table<DailyNote>()
            .Where(n => n.Date.Date == date.Date)
            .ToListAsync();
    }
    
    public Task<int> SaveNoteAsync(DailyNote note)
    {
        return _database.InsertAsync(note);
    }
    
    public Task<int> UpdateNoteAsync(DailyNote note)
    {
        return _database.UpdateAsync(note);
    }
    
    public Task<int> DeleteNoteAsync(int id)
    {
        return _database.DeleteAsync<DailyNote>(id);
    }
}
