using System.Text.RegularExpressions;
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
            
            var html = await response.Content.ReadAsStringAsync();
            var scheduleList = new List<ScheduleItem>();
            
            // Парсим HTML
            // Ищем блоки с парами (по паттерну)
            var lines = html.Split('\n');
            DateTime currentDate = startDate;
            string currentTime = "";
            string currentSubject = "";
            string currentTeacher = "";
            string currentRoom = "";
            
            foreach (var line in lines)
            {
                // Ищем дату (например, "24 марта 2026")
                var dateMatch = Regex.Match(line, @"(\d{1,2})\s+(\w+)\s+(\d{4})");
                if (dateMatch.Success)
                {
                    var day = int.Parse(dateMatch.Groups[1].Value);
                    var monthName = dateMatch.Groups[2].Value;
                    var year = int.Parse(dateMatch.Groups[3].Value);
                    var month = GetMonthNumber(monthName);
                    currentDate = new DateTime(year, month, day);
                }
                
                // Ищем время пары (например, "08:30 – 10:05")
                var timeMatch = Regex.Match(line, @"(\d{2}:\d{2})\s*–\s*(\d{2}:\d{2})");
                if (timeMatch.Success)
                {
                    currentTime = timeMatch.Groups[1].Value;
                }
                
                // Ищем предмет (обычно жирный текст после времени)
                var subjectMatch = Regex.Match(line, @"<strong>(.*?)</strong>");
                if (subjectMatch.Success && !string.IsNullOrWhiteSpace(subjectMatch.Groups[1].Value))
                {
                    currentSubject = subjectMatch.Groups[1].Value.Trim();
                }
                
                // Ищем аудиторию (например, "Г-408" или "Н-301")
                var roomMatch = Regex.Match(line, @"([А-Я]-?\d{3})");
                if (roomMatch.Success)
                {
                    currentRoom = roomMatch.Groups[1].Value;
                }
                
                // Ищем преподавателя
                var teacherMatch = Regex.Match(line, @"Преподаватель:\s*(.*?)(?:<|$)");
                if (teacherMatch.Success)
                {
                    currentTeacher = teacherMatch.Groups[1].Value.Trim();
                }
                
                // Если нашли время и предмет, создаём запись
                if (!string.IsNullOrEmpty(currentTime) && !string.IsNullOrEmpty(currentSubject))
                {
                    var item = new ScheduleItem
                    {
                        GroupId = groupId,
                        Date = currentDate,
                        Time = currentTime,
                        Subject = currentSubject,
                        Teacher = currentTeacher,
                        Room = currentRoom,
                        Type = GetLessonType(currentRoom, currentSubject)
                    };
                    
                    scheduleList.Add(item);
                    
                    // Сбрасываем временные переменные после сохранения
                    currentTime = "";
                    currentSubject = "";
                    currentTeacher = "";
                    currentRoom = "";
                }
            }
            
            // Удаляем дубликаты (если парсер создал несколько одинаковых)
            scheduleList = scheduleList
                .GroupBy(x => new { x.Date, x.Time, x.Subject })
                .Select(g => g.First())
                .ToList();
            
            return scheduleList;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки расписания: {ex.Message}");
            return new List<ScheduleItem>();
        }
    }
    
    private int GetMonthNumber(string monthName)
    {
        var months = new Dictionary<string, int>
        {
            {"января", 1}, {"февраля", 2}, {"марта", 3}, {"апреля", 4},
            {"мая", 5}, {"июня", 6}, {"июля", 7}, {"августа", 8},
            {"сентября", 9}, {"октября", 10}, {"ноября", 11}, {"декабря", 12}
        };
        
        return months.ContainsKey(monthName.ToLower()) ? months[monthName.ToLower()] : 1;
    }
    
    private string GetLessonType(string room, string subject)
    {
        if (room.Contains("Г-")) return "Практика";
        if (room.Contains("Н-")) return "Лекция";
        if (subject.Contains("лабораторн")) return "Лабораторная";
        return "Занятие";
    }
}
