using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private ObservableCollection<GymClass> _filteredClasses = new();

    [ObservableProperty]
    private GymClass? _selectedClass;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    [ObservableProperty]
    private ObservableCollection<ScheduleDayOption> _weekDays = new();

    [ObservableProperty]
    private ObservableCollection<string> _trainerFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> _locationFilters = new();

    public ObservableCollection<string> TypeFilters { get; } = new()
    {
        "Wszystkie typy",
        "Zajęcia grupowe",
        "Trening osobisty"
    };

    public ObservableCollection<string> AvailabilityFilters { get; } = new()
    {
        "Wszystkie miejsca",
        "Wolne miejsca",
        "Pełne zajęcia"
    };

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            ApplyFilters();
        }
    }

    private string _selectedTrainerFilter = "Wszyscy trenerzy";
    public string SelectedTrainerFilter
    {
        get => _selectedTrainerFilter;
        set
        {
            SetProperty(ref _selectedTrainerFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedLocationFilter = "Wszystkie lokalizacje";
    public string SelectedLocationFilter
    {
        get => _selectedLocationFilter;
        set
        {
            SetProperty(ref _selectedLocationFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedTypeFilter = "Wszystkie typy";
    public string SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            SetProperty(ref _selectedTypeFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedAvailabilityFilter = "Wszystkie miejsca";
    public string SelectedAvailabilityFilter
    {
        get => _selectedAvailabilityFilter;
        set
        {
            SetProperty(ref _selectedAvailabilityFilter, value);
            ApplyFilters();
        }
    }

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
    private bool _newPersonalTraining;

    [ObservableProperty]
    private ObservableCollection<Location> _locations = new();

    [ObservableProperty]
    private Location? _selectedLocation;

    [ObservableProperty]
    private ObservableCollection<Trainer> _trainers = new();

    [ObservableProperty]
    private Trainer? _selectedTrainer;

    [ObservableProperty]
    private bool _hasTrainers;

    public ScheduleViewModel(ApiService apiService)
    {
        _apiService = apiService;
        RefreshWeekDays();
        _ = LoadClassesAsync();
        _ = LoadLocationsAsync();
        _ = LoadTrainersAsync();
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        RefreshWeekDays();
        _ = LoadClassesAsync();
    }

    [RelayCommand]
    private async Task LoadClassesAsync()
    {
        IsLoading = true;
        StatusText = $"Pobieranie zajęć na {SelectedDate:dd MMMM yyyy}...";

        var result = await _apiService.GetClassesByDateAsync(SelectedDate);
        Classes = new ObservableCollection<GymClass>(result);
        RefreshFilterOptions();
        ApplyFilters();

        StatusText = Classes.Count > 0
            ? _apiService.LastResultFromCache
                ? $"Tryb offline: zajęcia na {SelectedDate:dd MMMM yyyy} z lokalnej kopii"
                : $"Zajęcia na {SelectedDate:dd MMMM yyyy}"
            : $"Brak zajęć na {SelectedDate:dd MMMM yyyy}";

        IsLoading = false;
    }

    private void ApplyFilters()
    {
        var filtered = Classes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(c =>
                (c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.TrainerName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.LocationName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (SelectedTrainerFilter != "Wszyscy trenerzy")
            filtered = filtered.Where(c => c.TrainerName == SelectedTrainerFilter);

        if (SelectedLocationFilter != "Wszystkie lokalizacje")
            filtered = filtered.Where(c => c.LocationName == SelectedLocationFilter);

        filtered = SelectedTypeFilter switch
        {
            "Zajęcia grupowe" => filtered.Where(c => !c.PersonalTraining),
            "Trening osobisty" => filtered.Where(c => c.PersonalTraining),
            _ => filtered
        };

        filtered = SelectedAvailabilityFilter switch
        {
            "Wolne miejsca" => filtered.Where(c => !c.IsFull),
            "Pełne zajęcia" => filtered.Where(c => c.IsFull),
            _ => filtered
        };

        var result = filtered.OrderBy(c => c.StartTime).ToList();
        FilteredClasses = new ObservableCollection<GymClass>(result);

        FooterText = $"Wyświetlono: {FilteredClasses.Count} z {Classes.Count} zajęć  ·  " +
                     $"Zapisanych uczestników: {FilteredClasses.Sum(c => c.CurrentParticipants)}";
    }

    private void RefreshFilterOptions()
    {
        TrainerFilters = new ObservableCollection<string>(
            new[] { "Wszyscy trenerzy" }
                .Concat(Classes.Select(c => c.TrainerName)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .OrderBy(t => t)));

        LocationFilters = new ObservableCollection<string>(
            new[] { "Wszystkie lokalizacje" }
                .Concat(Classes.Select(c => c.LocationName)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct()
                    .OrderBy(l => l)));

        if (!TrainerFilters.Contains(SelectedTrainerFilter))
            SelectedTrainerFilter = "Wszyscy trenerzy";

        if (!LocationFilters.Contains(SelectedLocationFilter))
            SelectedLocationFilter = "Wszystkie lokalizacje";
    }

    private void RefreshWeekDays()
    {
        var startOfWeek = SelectedDate.Date.AddDays(-GetMondayBasedDayOffset(SelectedDate));

        WeekDays = new ObservableCollection<ScheduleDayOption>(
            Enumerable.Range(0, 7).Select(i =>
            {
                var date = startOfWeek.AddDays(i);
                return new ScheduleDayOption
                {
                    Date = date,
                    Header = date.ToString("ddd", CultureInfo.CurrentCulture),
                    Subheader = date.ToString("dd.MM", CultureInfo.CurrentCulture),
                    IsSelected = date.Date == SelectedDate.Date
                };
            }));
    }

    private async Task LoadLocationsAsync()
    {
        var locs = await _apiService.GetLocationsAsync();
        Locations = new ObservableCollection<Location>(locs);
        if (Locations.Count > 0)
            SelectedLocation = Locations[0];
    }

    private async Task LoadTrainersAsync()
    {
        var trainers = await _apiService.GetTrainersAsync();
        Trainers = new ObservableCollection<Trainer>(trainers);
        HasTrainers = Trainers.Count > 0;
        if (HasTrainers)
            SelectedTrainer = Trainers[0];
    }

    [RelayCommand]
    private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);

    [RelayCommand]
    private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

    [RelayCommand]
    private void GoToToday() => SelectedDate = DateTime.Today;

    [RelayCommand]
    private void SelectDate(DateTime date) => SelectedDate = date.Date;

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
            if (Locations.Count > 0) SelectedLocation = Locations[0];
            if (Trainers.Count > 0) SelectedTrainer = Trainers[0];
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
        if (SelectedLocation == null)
        {
            MessageBox.Show("Wybierz lokalizację.", "Walidacja");
            return;
        }
        if (!TimeSpan.TryParse(NewStartTime, out var startTs) ||
            !TimeSpan.TryParse(NewEndTime, out var endTs))
        {
            MessageBox.Show("Podaj godziny w formacie HH:mm (np. 09:00).", "Walidacja");
            return;
        }
        if (endTs <= startTs)
        {
            MessageBox.Show("Godzina zakończenia musi być późniejsza niż rozpoczęcia.", "Walidacja");
            return;
        }

        var request = new CreateClassRequest
        {
            Name = NewName,
            Description = NewDescription,
            StartTime = NewStartDate.Date + startTs,
            EndTime = NewStartDate.Date + endTs,
            MaxParticipants = NewMaxParticipants,
            LocationId = SelectedLocation.Id,
            PersonalTraining = NewPersonalTraining,
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

        var confirm = MessageBox.Show(
            $"Czy na pewno chcesz usunąć zajęcia?\n\n" +
            $"Nazwa: {gymClass.Name}\n" +
            $"Trener: {gymClass.TrainerName}\n" +
            $"Godzina: {gymClass.TimeRange}",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        var success = await _apiService.DeleteClassAsync(gymClass.Id);

        if (success)
        {
            Classes.Remove(gymClass);
            ApplyFilters();
            StatusText = $"Zajęcia \"{gymClass.Name}\" zostały usunięte.";
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

    private static int GetMondayBasedDayOffset(DateTime date)
        => date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
}

public class ScheduleDayOption
{
    public DateTime Date { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Subheader { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
