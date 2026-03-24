using ScheduleApp.Services;

namespace ScheduleApp.Views;

public partial class SettingsPage : ContentPage
{
    private DatabaseService _dbService;
    private ScheduleParser _parser;
    private string _groupId;
    
    public SettingsPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _parser = new ScheduleParser();
        
        _groupId = Preferences.Get("group_id", "БЭ25");
        GroupEntry.Text = _groupId;
    }
    
    private async void OnUpdateScheduleClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GroupEntry.Text))
        {
            await DisplayAlert("Ошибка", "Введите номер группы", "OK");
            return;
        }
        
        _groupId = GroupEntry.Text.Trim();
        Preferences.Set("group_id", _groupId);
        
        var button = (Button)sender;
        button.IsEnabled = false;
        button.Text = "Загрузка...";
        
        try
        {
            var startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var endDate = startDate.AddDays(6);
            
            var schedule = await _parser.GetScheduleAsync(_groupId, startDate, endDate);
            
            if (schedule.Any())
            {
                await _dbService.ClearScheduleAsync();
                foreach (var item in schedule)
                {
                    await _dbService.SaveScheduleAsync(item);
                }
                await DisplayAlert("Успешно", $"Загружено {schedule.Count} пар", "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось загрузить расписание. Проверьте группу.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить расписание: {ex.Message}", "OK");
        }
        finally
        {
            button.IsEnabled = true;
            button.Text = "Обновить расписание";
        }
    }
    
    private async void OnClearScheduleClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Подтверждение", 
            "Вы уверены, что хотите очистить всё расписание?", 
            "Да", "Нет");
            
        if (confirm)
        {
            await _dbService.ClearScheduleAsync();
            await DisplayAlert("Готово", "Расписание очищено", "OK");
        }
    }
}
