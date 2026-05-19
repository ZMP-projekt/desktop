using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
    private bool _isOffline;

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

    public ScheduleViewModel(ApiService apiService)
    {
        _apiService = apiService;
        RefreshWeekDays();
        _ = LoadClassesAsync();
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

    [RelayCommand]
    private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);

    [RelayCommand]
    private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

    [RelayCommand]
    private void GoToToday() => SelectedDate = DateTime.Today;

    [RelayCommand]
    private void SelectDate(DateTime date) => SelectedDate = date.Date;

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
