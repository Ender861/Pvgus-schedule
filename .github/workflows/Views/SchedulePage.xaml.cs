using ScheduleApp.Models;
using ScheduleApp.Services;

namespace ScheduleApp.Views;

public partial class SchedulePage : ContentPage
{
    private DatabaseService _dbService;
    
    public SchedulePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        DatePicker.Date = DateTime.Today;
    }
    
    private async void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        await LoadSchedule(e.NewDate);
    }
    
    private async Task LoadSchedule(DateTime date)
    {
        var schedule = await _dbService.GetScheduleAsync(date);
        ScheduleListView.ItemsSource = schedule;
        
        if (!schedule.Any())
        {
            await DisplayAlert("Нет расписания", 
                "Расписание на эту дату не найдено. Обновите его в настройках.", "OK");
        }
    }
    
    private async void OnScheduleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ScheduleItem selectedItem)
        {
            var homework = await _dbService.GetHomeworkAsync(selectedItem.Id);
            var homeworkText = homework.FirstOrDefault()?.Task ?? "";
            
            var result = await DisplayPromptAsync("Домашнее задание",
                $"Предмет: {selectedItem.Subject}\nВведите задание:",
                initialValue: homeworkText);
            
            if (!string.IsNullOrEmpty(result))
            {
                var hw = homework.FirstOrDefault() ?? new Homework
                {
                    ScheduleId = selectedItem.Id,
                    Subject = selectedItem.Subject
                };
                
                hw.Task = result;
                hw.Deadline = selectedItem.Date;
                
                if (homework.Any())
                    await _dbService.UpdateHomeworkAsync(hw);
                else
                    await _dbService.SaveHomeworkAsync(hw);
                
                await DisplayAlert("Успешно", "Домашнее задание сохранено!", "OK");
            }
        }
        
        ((CollectionView)sender).SelectedItem = null;
    }
}
