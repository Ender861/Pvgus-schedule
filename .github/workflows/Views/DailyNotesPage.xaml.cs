using ScheduleApp.Models;
using ScheduleApp.Services;

namespace ScheduleApp.Views;

public partial class DailyNotesPage : ContentPage
{
    private DatabaseService _dbService;
    
    public DailyNotesPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        DatePicker.Date = DateTime.Today;
    }
    
    private async void OnDateSelected(object sender, DateChangedEventArgs e)
    {
        await LoadNote(e.NewDate);
    }
    
    private async Task LoadNote(DateTime date)
    {
        var notes = await _dbService.GetNotesAsync(date);
        NoteEditor.Text = notes.FirstOrDefault()?.Content ?? "";
    }
    
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var existingNotes = await _dbService.GetNotesAsync(DatePicker.Date);
        var note = existingNotes.FirstOrDefault() ?? new DailyNote { Date = DatePicker.Date };
        
        note.Content = NoteEditor.Text;
        
        if (existingNotes.Any())
            await _dbService.UpdateNoteAsync(note);
        else
            await _dbService.SaveNoteAsync(note);
        
        await DisplayAlert("Успешно", "Заметка сохранена!", "OK");
    }
}
