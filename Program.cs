using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SQLite;
using System.Text.Json;

namespace ScheduleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}

public class App : Application
{
    public App()
    {
        MainPage = new NavigationPage(new MainPage());
    }
}

// ==================== МОДЕЛИ ====================
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

[Table("Homework")]
public class Homework
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; }
    public string Comment { get; set; } = string.Empty;
}

[Table("DailyNotes")]
public class DailyNote
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsImportant { get; set; }
}

// ==================== СЕРВИСЫ ====================
public class DatabaseService
{
    private SQLiteAsyncConnection _database;
    
    public DatabaseService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "schedule.db3");
        _database = new SQLiteAsyncConnection(dbPath);
        Task.Run(async () => await InitializeDatabase());
    }
    
    private async Task InitializeDatabase()
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
    
    public Task<int> SaveScheduleAsync(ScheduleItem item) => _database.InsertAsync(item);
    public Task<int> ClearScheduleAsync() => _database.DeleteAllAsync<ScheduleItem>();
    
    public Task<List<Homework>> GetHomeworkAsync(int scheduleId) =>
        _database.Table<Homework>().Where(h => h.ScheduleId == scheduleId).ToListAsync();
    
    public Task<List<Homework>> GetAllHomeworkAsync() =>
        _database.Table<Homework>().OrderBy(h => h.Deadline).ToListAsync();
    
    public Task<int> SaveHomeworkAsync(Homework homework) => _database.InsertAsync(homework);
    public Task<int> UpdateHomeworkAsync(Homework homework) => _database.UpdateAsync(homework);
    public Task<int> DeleteHomeworkAsync(int id) => _database.DeleteAsync<Homework>(id);
    
    public Task<List<DailyNote>> GetNotesAsync(DateTime date) =>
        _database.Table<DailyNote>().Where(n => n.Date.Date == date.Date).ToListAsync();
    
    public Task<int> SaveNoteAsync(DailyNote note) => _database.InsertAsync(note);
    public Task<int> UpdateNoteAsync(DailyNote note) => _database.UpdateAsync(note);
    public Task<int> DeleteNoteAsync(int id) => _database.DeleteAsync<DailyNote>(id);
}

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
                    scheduleList.Add(new ScheduleItem
                    {
                        GroupId = groupId,
                        Date = DateTime.Parse(element.GetProperty("date").GetString() ?? ""),
                        Time = element.GetProperty("time").GetString() ?? "",
                        Subject = element.GetProperty("subject").GetString() ?? "",
                        Teacher = element.GetProperty("teacher").GetString() ?? "",
                        Room = element.GetProperty("room").GetString() ?? "",
                        Type = element.GetProperty("type").GetString() ?? ""
                    });
                }
            }
            return scheduleList;
        }
        catch { return new List<ScheduleItem>(); }
    }
}

// ==================== СТРАНИЦЫ ====================
public class MainPage : TabbedPage
{
    public MainPage()
    {
        Children.Add(new SchedulePage { Title = "📅 Расписание" });
        Children.Add(new HomeworkPage { Title = "📝 ДЗ" });
        Children.Add(new DailyNotesPage { Title = "📋 Ежедневник" });
        Children.Add(new SettingsPage { Title = "⚙️ Настройки" });
    }
}

public class SchedulePage : ContentPage
{
    private DatabaseService _db = new DatabaseService();
    private DatePicker _datePicker;
    private CollectionView _scheduleList;
    
    public SchedulePage()
    {
        Title = "Расписание";
        _datePicker = new DatePicker { Date = DateTime.Today, Margin = 10 };
        _datePicker.DateSelected += OnDateSelected;
        
        _scheduleList = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var timeLabel = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, WidthRequest = 80 };
                timeLabel.SetBinding(Label.TextProperty, "Time");
                
                var subjectLabel = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
                subjectLabel.SetBinding(Label.TextProperty, "Subject");
                
                var teacherLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
                teacherLabel.SetBinding(Label.TextProperty, "Teacher");
                
                var roomLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
                roomLabel.SetBinding(Label.TextProperty, "Room");
                
                var typeLabel = new Label { FontSize = 12, TextColor = Colors.Blue };
                typeLabel.SetBinding(Label.TextProperty, "Type");
                
                var layout = new StackLayout
                {
                    Children = { subjectLabel, teacherLabel, roomLabel, typeLabel },
                    Margin = new Thickness(10, 0, 0, 0)
                };
                
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.Children.Add(timeLabel);
                grid.Children.Add(layout);
                Grid.SetColumn(layout, 1);
                
                var border = new Border { Content = grid, Margin = 5, Padding = 10, StrokeThickness = 0, BackgroundColor = Colors.White };
                return border;
            })
        };
        _scheduleList.SelectionChanged += OnScheduleSelected;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.Children.Add(_datePicker);
        grid.Children.Add(_scheduleList);
        Grid.SetRow(_scheduleList, 1);
        
        Content = grid;
        
        _ = LoadSchedule(DateTime.Today);
    }
    
    private async void OnDateSelected(object? sender, DateChangedEventArgs e) => await LoadSchedule(e.NewDate);
    
    private async Task LoadSchedule(DateTime date)
    {
        _scheduleList.ItemsSource = await _db.GetScheduleAsync(date);
        var items = _scheduleList.ItemsSource as IEnumerable<object>;
        if (items == null || !items.Any())
        {
            await DisplayAlert("Нет расписания", "Обновите в настройках", "OK");
        }
    }
    
    private async void OnScheduleSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ScheduleItem item)
        {
            var hw = (await _db.GetHomeworkAsync(item.Id)).FirstOrDefault();
            var result = await DisplayPromptAsync("Домашнее задание", $"Предмет: {item.Subject}", initialValue: hw?.Task);
            if (!string.IsNullOrEmpty(result))
            {
                var newHw = hw ?? new Homework { ScheduleId = item.Id, Subject = item.Subject, Deadline = item.Date };
                newHw.Task = result;
                if (hw != null) await _db.UpdateHomeworkAsync(newHw);
                else await _db.SaveHomeworkAsync(newHw);
                await DisplayAlert("Успешно", "Сохранено!", "OK");
            }
        }
        ((CollectionView)sender).SelectedItem = null;
    }
}

public class HomeworkPage : ContentPage
{
    private DatabaseService _db = new DatabaseService();
    private CollectionView _list;
    
    public HomeworkPage()
    {
        Title = "Домашние задания";
        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var subject = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold };
                subject.SetBinding(Label.TextProperty, "Subject");
                
                var task = new Label { FontSize = 14 };
                task.SetBinding(Label.TextProperty, "Task");
                
                var deadline = new Label { FontSize = 12, TextColor = Colors.Gray };
                deadline.SetBinding(Label.TextProperty, new Binding("Deadline", stringFormat: "Срок: {0:dd.MM.yyyy}"));
                
                var check = new CheckBox { VerticalOptions = LayoutOptions.Center };
                check.SetBinding(CheckBox.IsCheckedProperty, "IsCompleted");
                
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.Children.Add(new StackLayout { Children = { subject, task, deadline } });
                grid.Children.Add(check);
                Grid.SetColumn(check, 1);
                
                var border = new Border { Content = grid, Margin = 5, Padding = 10, StrokeThickness = 0, BackgroundColor = Colors.White };
                return border;
            })
        };
        _list.SelectionChanged += OnSelected;
        Content = _list;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Load();
    }
    
    private async Task Load() => _list.ItemsSource = await _db.GetAllHomeworkAsync();
    
    private async void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Homework hw)
        {
            var action = await DisplayActionSheet("Действия", "Отмена", null, "Редактировать", "Удалить");
            if (action == "Редактировать")
            {
                var newTask = await DisplayPromptAsync("Редактировать", "Введите задание:", initialValue: hw.Task);
                if (!string.IsNullOrEmpty(newTask))
                {
                    hw.Task = newTask;
                    await _db.UpdateHomeworkAsync(hw);
                    await Load();
                }
            }
            else if (action == "Удалить")
            {
                await _db.DeleteHomeworkAsync(hw.Id);
                await Load();
            }
        }
        ((CollectionView)sender).SelectedItem = null;
    }
}

public class DailyNotesPage : ContentPage
{
    private DatabaseService _db = new DatabaseService();
    private DatePicker _datePicker;
    private Editor _editor;
    
    public DailyNotesPage()
    {
        Title = "Ежедневник";
        _datePicker = new DatePicker { Date = DateTime.Today, Margin = 10 };
        _datePicker.DateSelected += OnDateSelected;
        
        _editor = new Editor { Placeholder = "Ваши заметки на день...", AutoSize = EditorAutoSizeOption.TextChanges, Margin = 10 };
        
        var saveBtn = new Button { Text = "Сохранить", BackgroundColor = Color.FromArgb("#512BD4"), TextColor = Colors.White, Margin = 10 };
        saveBtn.Clicked += OnSave;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(_datePicker);
        grid.Children.Add(_editor);
        grid.Children.Add(saveBtn);
        Grid.SetRow(_editor, 1);
        Grid.SetRow(saveBtn, 2);
        
        Content = grid;
        
        _ = LoadNote(DateTime.Today);
    }
    
    private async void OnDateSelected(object? sender, DateChangedEventArgs e) => await LoadNote(e.NewDate);
    
    private async Task LoadNote(DateTime date)
    {
        var notes = await _db.GetNotesAsync(date);
        _editor.Text = notes.FirstOrDefault()?.Content ?? "";
    }
    
    private async void OnSave(object? sender, EventArgs e)
    {
        var notes = await _db.GetNotesAsync(_datePicker.Date);
        var note = notes.FirstOrDefault() ?? new DailyNote { Date = _datePicker.Date };
        note.Content = _editor.Text;
        
        if (notes.Any()) await _db.UpdateNoteAsync(note);
        else await _db.SaveNoteAsync(note);
        
        await DisplayAlert("Успешно", "Заметка сохранена!", "OK");
    }
}

public class SettingsPage : ContentPage
{
    private DatabaseService _db = new DatabaseService();
    private ScheduleParser _parser = new ScheduleParser();
    private Entry _groupEntry;
    
    public SettingsPage()
    {
        Title = "Настройки";
        _groupEntry = new Entry { Placeholder = "Например: БЭ25", Text = Preferences.Get("group_id", "БЭ25") };
        
        var updateBtn = new Button { Text = "Обновить расписание", BackgroundColor = Color.FromArgb("#512BD4"), TextColor = Colors.White };
        updateBtn.Clicked += OnUpdate;
        
        var clearBtn = new Button { Text = "Очистить расписание", BackgroundColor = Color.FromArgb("#FF4136"), TextColor = Colors.White };
        clearBtn.Clicked += OnClear;
        
        var about = new Label { Text = "Расписание ПВГУС + Ежедневник\nВерсия 1.0", FontSize = 12, TextColor = Colors.Gray };
        
        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children = {
                    new Label { Text = "Ваша группа", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    _groupEntry,
                    new Label { Text = "Расписание", FontSize = 16, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0,20,0,0) },
                    updateBtn,
                    clearBtn,
                    new Label { Text = "О приложении", FontSize = 16, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0,20,0,0) },
                    about
                }
            }
        };
    }
    
    private async void OnUpdate(object? sender, EventArgs e)
    {
        var group = _groupEntry.Text?.Trim();
        if (string.IsNullOrEmpty(group))
        {
            await DisplayAlert("Ошибка", "Введите номер группы", "OK");
            return;
        }
        
        Preferences.Set("group_id", group);
        var btn = (Button)sender;
        btn.IsEnabled = false;
        btn.Text = "Загрузка...";
        
        try
        {
            var start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var end = start.AddDays(6);
            var schedule = await _parser.GetScheduleAsync(group, start, end);
            
            if (schedule.Any())
            {
                await _db.ClearScheduleAsync();
                foreach (var item in schedule) await _db.SaveScheduleAsync(item);
                await DisplayAlert("Успешно", $"Загружено {schedule.Count} пар", "OK");
            }
            else await DisplayAlert("Ошибка", "Не удалось загрузить расписание", "OK");
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
        finally { btn.IsEnabled = true; btn.Text = "Обновить расписание"; }
    }
    
    private async void OnClear(object? sender, EventArgs e)
    {
        if (await DisplayAlert("Подтверждение", "Очистить всё расписание?", "Да", "Нет"))
        {
            await _db.ClearScheduleAsync();
            await DisplayAlert("Готово", "Расписание очищено", "OK");
        }
    }
}
