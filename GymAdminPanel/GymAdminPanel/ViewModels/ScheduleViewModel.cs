using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GymAdminPanel.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<GymClass> _classes = new();

    [ObservableProperty]
    private GymClass? _selectedClass;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    [ObservableProperty]
    private bool _isAddFormVisible;

    [ObservableProperty]
    private string _newName = "";

    [ObservableProperty]
    private string _newDescription = "";

    [ObservableProperty]
    private DateTime _newStartDate = DateTime.Today;

    [ObservableProperty]
    private string _newStartTime = "09:00";

    [ObservableProperty]
    private string _newEndTime = "10:00";

    [ObservableProperty]
    private int _newMaxParticipants = 15;

    [ObservableProperty]
    private int _newLocationId = 1;

    [ObservableProperty]
    private bool _newPersonalTraining;

    public ScheduleViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadClassesAsync();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadClassesAsync();
    }

    [RelayCommand]
    private async Task LoadClassesAsync()
    {
        IsLoading = true;
        StatusText = $"Pobieranie zajęć na {SelectedDate:dd MMMM yyyy}...";

        var result = await _apiService.GetClassesByDateAsync(SelectedDate);
        Classes = new ObservableCollection<GymClass>(result);

        StatusText = Classes.Count > 0
            ? $"Zajęcia na {SelectedDate:dd MMMM yyyy}"
            : $"Brak zajęć na {SelectedDate:dd MMMM yyyy}";

        FooterText = $"Łącznie: {Classes.Count} zajęć · " +
                     $"Zapisanych uczestników: {Classes.Sum(c => c.CurrentParticipants)}";

        IsLoading = false;
    }

    [RelayCommand]
    private void PreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    private void NextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    private void GoToToday()
    {
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        IsAddFormVisible = !IsAddFormVisible;
        if (IsAddFormVisible)
        {
            NewName = "";
            NewDescription = "";
            NewStartDate = SelectedDate;
            NewStartTime = "09:00";
            NewEndTime = "10:00";
            NewMaxParticipants = 15;
            NewPersonalTraining = false;
        }
    }

    [RelayCommand]
    private async Task SaveNewClassAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            MessageBox.Show("Podaj nazwę zajęć.", "Walidacja");
            return;
        }

        if (!TimeSpan.TryParse(NewStartTime, out var startTs) ||
            !TimeSpan.TryParse(NewEndTime, out var endTs))
        {
            MessageBox.Show("Podaj godziny w formacie HH:mm (np. 09:00).", "Walidacja");
            return;
        }

        var request = new CreateClassRequest
        {
            Name = NewName,
            Description = NewDescription,
            StartTime = NewStartDate.Date + startTs,
            EndTime = NewStartDate.Date + endTs,
            MaxParticipants = NewMaxParticipants,
            LocationId = NewLocationId,
            PersonalTraining = NewPersonalTraining
        };

        IsLoading = true;
        var success = await _apiService.CreateClassAsync(request);

        if (success)
        {
            IsAddFormVisible = false;
            StatusText = $"Zajęcia \"{NewName}\" zostały dodane.";
            await LoadClassesAsync();
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteClassAsync(GymClass gymClass)
    {
        if (gymClass == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunąć zajęcia?\n\n" +
            $"Nazwa: {gymClass.Name}\n" +
            $"Trener: {gymClass.TrainerName}\n" +
            $"Godzina: {gymClass.TimeRange}",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsLoading = true;
        var success = await _apiService.DeleteClassAsync(gymClass.Id);

        if (success)
        {
            Classes.Remove(gymClass);
            StatusText = $"Zajęcia \"{gymClass.Name}\" zostały usunięte.";
            FooterText = $"Łącznie: {Classes.Count} zajęć";
        }
        else
        {
            StatusText = "Nie udało się usunąć zajęć.";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task ShowParticipantsAsync(GymClass gymClass)
    {
        if (gymClass == null) return;

        IsLoading = true;
        var participants = await _apiService.GetClassParticipantsAsync(gymClass.Id);
        IsLoading = false;

        if (participants.Count == 0)
        {
            MessageBox.Show("Brak zapisanych uczestników.", gymClass.Name);
            return;
        }

        var list = string.Join("\n", participants.Select((p, i) =>
            $"{i + 1}. {p.FullName}  ({p.Email})"));

        MessageBox.Show(
            $"Uczestnicy zajęć: {gymClass.Name}\n\n{list}",
            $"Uczestnicy ({participants.Count}/{gymClass.MaxParticipants})",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
