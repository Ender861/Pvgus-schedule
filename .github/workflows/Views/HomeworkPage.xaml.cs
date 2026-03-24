using ScheduleApp.Models;
using ScheduleApp.Services;

namespace ScheduleApp.Views;

public partial class HomeworkPage : ContentPage
{
    private DatabaseService _dbService;
    
    public HomeworkPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHomework();
    }
    
    private async Task LoadHomework()
    {
        var homework = await _dbService.GetAllHomeworkAsync();
        HomeworkListView.ItemsSource = homework;
    }
    
    private async void OnHomeworkSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Homework selected)
        {
            var action = await DisplayActionSheet("Действия", "Отмена", null, "Редактировать", "Удалить");
            
            if (action == "Редактировать")
            {
                var newTask = await DisplayPromptAsync("Редактировать",
                    "Введите задание:",
                    initialValue: selected.Task);
                    
                if (!string.IsNullOrEmpty(newTask))
                {
                    selected.Task = newTask;
                    await _dbService.UpdateHomeworkAsync(selected);
                    await LoadHomework();
                }
            }
            else if (action == "Удалить")
            {
                await _dbService.DeleteHomeworkAsync(selected.Id);
                await LoadHomework();
            }
        }
        
        ((CollectionView)sender).SelectedItem = null;
    }
}
