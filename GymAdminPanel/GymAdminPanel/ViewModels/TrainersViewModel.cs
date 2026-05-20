using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymAdminPanel.Models;
using GymAdminPanel.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GymAdminPanel.ViewModels;

public partial class TrainersViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly LocalizationService _localization = LocalizationService.Instance;
    private List<Trainer> _allTrainers = new();
    private bool _lastLoadFromCache;

    [ObservableProperty]
    private ObservableCollection<Trainer> _filteredTrainers = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _footerText = "";

    [ObservableProperty]
    private bool _isOffline;

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            ApplyFilter();
        }
    }

    public TrainersViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _localization.LanguageChanged += OnLanguageChanged;
        _ = LoadTrainersAsync();
    }

    [RelayCommand]
    private async Task LoadTrainersAsync()
    {
        IsLoading = true;
        StatusText = _localization.Translate("Trainers.Fetching");

        _allTrainers = await _apiService.GetRoleVerifiedTrainersAsync();
        _lastLoadFromCache = _apiService.LastResultFromCache;
        ApplyFilter();

        RefreshStatusText();
        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allTrainers
            : _allTrainers.Where(t =>
                (t.FirstName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.LastName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Specialization?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();

        FilteredTrainers = new ObservableCollection<Trainer>(filtered);
        FooterText = _localization.Format("Trainers.Footer", FilteredTrainers.Count, _allTrainers.Count);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyFilter();
        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        StatusText = _lastLoadFromCache
            ? _localization.Format("Trainers.OfflineLoaded", _allTrainers.Count)
            : _localization.Format("Trainers.Loaded", _allTrainers.Count);
    }
}
