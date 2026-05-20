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
    private readonly LocalizationService _localization = LocalizationService.Instance;

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

    private bool _lastLoadFromCache;

    [ObservableProperty]
    private ObservableCollection<ScheduleDayOption> _weekDays = new();

    [ObservableProperty]
    private ObservableCollection<string> _trainerFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> _locationFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> _typeFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> _availabilityFilters = new();

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

    private string _selectedTrainerFilter = LocalizationService.Instance.Translate("Schedule.AllTrainers");
    public string SelectedTrainerFilter
    {
        get => _selectedTrainerFilter;
        set
        {
            SetProperty(ref _selectedTrainerFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedLocationFilter = LocalizationService.Instance.Translate("Schedule.AllLocations");
    public string SelectedLocationFilter
    {
        get => _selectedLocationFilter;
        set
        {
            SetProperty(ref _selectedLocationFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedTypeFilter = LocalizationService.Instance.Translate("Schedule.AllTypes");
    public string SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            SetProperty(ref _selectedTypeFilter, value);
            ApplyFilters();
        }
    }

    private string _selectedAvailabilityFilter = LocalizationService.Instance.Translate("Schedule.AllSpots");
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
        _localization.LanguageChanged += OnLanguageChanged;
        RefreshStaticFilterOptions();
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
        StatusText = _localization.Format("Schedule.FetchingForDate", FormatSelectedDate());

        var result = await _apiService.GetClassesByDateAsync(SelectedDate);
        Classes = new ObservableCollection<GymClass>(result);
        _lastLoadFromCache = _apiService.LastResultFromCache;
        RefreshFilterOptions();
        ApplyFilters();

        RefreshStatusText();

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

        if (SelectedTrainerFilter != _localization.Translate("Schedule.AllTrainers"))
            filtered = filtered.Where(c => c.TrainerName == SelectedTrainerFilter);

        if (SelectedLocationFilter != _localization.Translate("Schedule.AllLocations"))
            filtered = filtered.Where(c => c.LocationName == SelectedLocationFilter);

        filtered = SelectedTypeFilter switch
        {
            var type when type == _localization.Translate("Schedule.GroupClasses") => filtered.Where(c => !c.PersonalTraining),
            var type when type == _localization.Translate("Schedule.PersonalTraining") => filtered.Where(c => c.PersonalTraining),
            _ => filtered
        };

        filtered = SelectedAvailabilityFilter switch
        {
            var availability when availability == _localization.Translate("Schedule.AvailableSpots") => filtered.Where(c => !c.IsFull),
            var availability when availability == _localization.Translate("Schedule.FullClasses") => filtered.Where(c => c.IsFull),
            _ => filtered
        };

        var result = filtered.OrderBy(c => c.StartTime).ToList();
        FilteredClasses = new ObservableCollection<GymClass>(result);

        FooterText = _localization.Format(
            "Schedule.Footer",
            FilteredClasses.Count,
            Classes.Count,
            FilteredClasses.Sum(c => c.CurrentParticipants));
    }

    private void RefreshFilterOptions()
    {
        TrainerFilters = new ObservableCollection<string>(
            new[] { _localization.Translate("Schedule.AllTrainers") }
                .Concat(Classes.Select(c => c.TrainerName)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .OrderBy(t => t)));

        LocationFilters = new ObservableCollection<string>(
            new[] { _localization.Translate("Schedule.AllLocations") }
                .Concat(Classes.Select(c => c.LocationName)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct()
                    .OrderBy(l => l)));

        if (!TrainerFilters.Contains(SelectedTrainerFilter))
            SelectedTrainerFilter = _localization.Translate("Schedule.AllTrainers");

        if (!LocationFilters.Contains(SelectedLocationFilter))
            SelectedLocationFilter = _localization.Translate("Schedule.AllLocations");
    }

    private void RefreshStaticFilterOptions()
    {
        var selectedTypeKey = ResolveSelectedTypeKey();
        var selectedAvailabilityKey = ResolveSelectedAvailabilityKey();

        TypeFilters = new ObservableCollection<string>
        {
            _localization.Translate("Schedule.AllTypes"),
            _localization.Translate("Schedule.GroupClasses"),
            _localization.Translate("Schedule.PersonalTraining")
        };

        AvailabilityFilters = new ObservableCollection<string>
        {
            _localization.Translate("Schedule.AllSpots"),
            _localization.Translate("Schedule.AvailableSpots"),
            _localization.Translate("Schedule.FullClasses")
        };

        SelectedTypeFilter = _localization.Translate(selectedTypeKey);

        SelectedAvailabilityFilter = _localization.Translate(selectedAvailabilityKey);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedTrainerWasAll = IsAllTrainersFilter(SelectedTrainerFilter);
        var selectedLocationWasAll = IsAllLocationsFilter(SelectedLocationFilter);
        var selectedTrainer = SelectedTrainerFilter;
        var selectedLocation = SelectedLocationFilter;

        RefreshStaticFilterOptions();
        RefreshWeekDays();
        RefreshFilterOptions();

        if (selectedTrainerWasAll)
            SelectedTrainerFilter = _localization.Translate("Schedule.AllTrainers");
        else if (TrainerFilters.Contains(selectedTrainer))
            SelectedTrainerFilter = selectedTrainer;

        if (selectedLocationWasAll)
            SelectedLocationFilter = _localization.Translate("Schedule.AllLocations");
        else if (LocationFilters.Contains(selectedLocation))
            SelectedLocationFilter = selectedLocation;

        ApplyFilters();
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        var dateText = FormatSelectedDate();
        StatusText = Classes.Count > 0
            ? _lastLoadFromCache
                ? _localization.Format("Schedule.OfflineForDate", dateText)
                : _localization.Format("Schedule.ClassesForDate", dateText)
            : _localization.Format("Schedule.NoClassesForDate", dateText);
    }

    private string FormatSelectedDate()
        => SelectedDate.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture);

    private string ResolveSelectedTypeKey()
    {
        if (MatchesAnyLanguage(SelectedTypeFilter, "Schedule.GroupClasses"))
            return "Schedule.GroupClasses";

        if (MatchesAnyLanguage(SelectedTypeFilter, "Schedule.PersonalTraining"))
            return "Schedule.PersonalTraining";

        return "Schedule.AllTypes";
    }

    private string ResolveSelectedAvailabilityKey()
    {
        if (MatchesAnyLanguage(SelectedAvailabilityFilter, "Schedule.AvailableSpots"))
            return "Schedule.AvailableSpots";

        if (MatchesAnyLanguage(SelectedAvailabilityFilter, "Schedule.FullClasses"))
            return "Schedule.FullClasses";

        return "Schedule.AllSpots";
    }

    private bool IsAllTrainersFilter(string value)
        => MatchesAnyLanguage(value, "Schedule.AllTrainers");

    private bool IsAllLocationsFilter(string value)
        => MatchesAnyLanguage(value, "Schedule.AllLocations");

    private bool MatchesAnyLanguage(string value, string key)
        => value == _localization.TranslateForLanguage(key, "pl") ||
           value == _localization.TranslateForLanguage(key, "en");

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
