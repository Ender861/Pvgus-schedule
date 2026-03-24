using System.Text.Json;
using ScheduleApp.Models;

namespace ScheduleApp.Services;

public class ScheduleParser
{
    private readonly HttpClient _httpClient = new HttpClient();
    
    public async Task<List<ScheduleItem>> GetScheduleAsync(string groupId, DateTime startDate, DateTime endDate)
    {
        var url = $"https://lk.tolgas.ru/public-schedule/group?id={Uri.EscapeDataString(groupId)}&dateFrom={startDate:yyyy-MM-dd}&dateTo={endDate:yyyy-MM-dd}";
        
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var scheduleList = new List<ScheduleItem>();
            
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var item = new ScheduleItem
                    {
                        GroupId = groupId,
                        Date = DateTime.Parse(element.GetProperty("date").GetString() ?? ""),
                        Time = element.GetProperty("time").GetString() ?? "",
                        Subject = element.GetProperty("subject").GetString() ?? "",
                        Teacher = element.GetProperty("teacher").GetString() ?? "",
                        Room = element.GetProperty("room").GetString() ?? "",
                        Type = element.GetProperty("type").GetString() ?? ""
                    };
                    scheduleList.Add(item);
                }
            }
            
            return scheduleList;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
            return new List<ScheduleItem>();
        }
    }
}
